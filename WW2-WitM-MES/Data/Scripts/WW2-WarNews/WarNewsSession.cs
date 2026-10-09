using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

// WW2-WitM War News (D-036)
//
// The server owns a short news feed and a list of GPS markers (plain or countdown). Both are saved in one sandbox
// variable. Clients get the feed by asking the server once after they join (packet kind 3 -> kind 1) and then each new
// item as it is posted (kind 2). GPS markers are server-side only: the server adds them to each player's GPS list.
// Other WitM code (the war layer, D-035) posts through the static WarNews class; MES Events post through the custom
// actions in WarNewsHooks. The "War News" LCD script (WarNewsTss) draws the feed on any LCD.
namespace WW2WitM.WarNewsSystem
{
    // Public entry points. Server-side only; calls on a client are ignored.
    public static class WarNews
    {
        public static bool IsServer { get { return WarNewsSession.Instance != null && WarNewsSession.Instance.IsServer; } }

        // Adds a bulletin item. faction is "Gray", "Green" or "" (only used for colouring). Returns false if ignored.
        public static bool Post(string text, string faction = "")
        {
            var s = WarNewsSession.Instance;
            return s != null && s.Post(text, faction);
        }

        // Shows a GPS marker to all players for the given minutes. A countdown marker shows the time left after its name.
        // Setting an existing key replaces that marker.
        public static void SetMarker(string key, string name, string description, Vector3D position, double minutes, bool countdown, Color color)
        {
            var s = WarNewsSession.Instance;
            if (s != null) s.SetMarker(key, name, description, position, minutes, countdown, color);
        }

        public static void RemoveMarker(string key)
        {
            var s = WarNewsSession.Instance;
            if (s != null) s.RemoveMarker(key);
        }

        // The current feed, newest first (server and clients). FeedVersion increases on every change.
        public static IList<NewsItem> Items { get { return WarNewsSession.Instance != null ? WarNewsSession.Instance.Items : EmptyItems; } }
        public static int FeedVersion { get { return WarNewsSession.Instance != null ? WarNewsSession.Instance.FeedVersion : 0; } }

        private static readonly List<NewsItem> EmptyItems = new List<NewsItem>();
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WarNewsSession : MySessionComponentBase
    {
        public const ushort NetChannel = 47251;
        public const string StateVariable = "WW2-WarNews-State";
        public const int MaxItems = 20;
        private const int MarkerUpdateTicks = 600;
        private const double RetiredKeepSeconds = 48 * 3600;
        private const double DuplicateWindowSeconds = 60;
        private const int ClockVersion = 2;
        private static readonly DateTime ClockBase = new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static WarNewsSession Instance;

        public bool IsServer;
        public readonly List<NewsItem> Items = new List<NewsItem>();
        public int FeedVersion;

        private readonly List<MarkerState> markers = new List<MarkerState>();
        private readonly List<RetiredName> retired = new List<RetiredName>();
        private readonly Dictionary<string, HashSet<long>> markerSentTo = new Dictionary<string, HashSet<long>>();
        private readonly HashSet<long> purgedPlayers = new HashSet<long>();
        private readonly HashSet<long> online = new HashSet<long>();
        private readonly List<IMyPlayer> players = new List<IMyPlayer>();
        private readonly List<IMyGps> gpsBuffer = new List<IMyGps>();

        private MesApiLite mes;
        private bool hooksRegistered;
        private bool feedRequested;
        private int tick;
        private string lastText = "";
        private double lastTextTime = -1000;

        public override void LoadData()
        {
            Instance = this;
            IsServer = MyAPIGateway.Multiplayer.IsServer;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetChannel, OnMessage);

            if (IsServer)
                mes = new MesApiLite();
        }

        // State is loaded here rather than in LoadData so the game clock (GameDateTime) has been restored from the save.
        public override void BeforeStart()
        {
            if (IsServer)
                LoadState();
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetChannel, OnMessage);
                if (hooksRegistered) WarNewsHooks.Unregister(mes);
                if (mes != null) mes.Unload();
                WarNewsPlaces.Clear();
            }
            catch (Exception e)
            {
                Log("unload: " + e);
            }
            Instance = null;
        }

        public override void SaveData()
        {
            if (IsServer) SaveState();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                tick++;

                if (!IsServer)
                {
                    if (!feedRequested && tick > 120 && MyAPIGateway.Session.Player != null)
                    {
                        feedRequested = true;
                        var packet = new WarNewsPacket { Kind = 3 };
                        MyAPIGateway.Multiplayer.SendMessageToServer(NetChannel, MyAPIGateway.Utilities.SerializeToBinary(packet));
                    }
                    return;
                }

                if (!hooksRegistered && mes != null && mes.Ready)
                {
                    WarNewsHooks.Register(mes);
                    hooksRegistered = true;
                }

                if (tick % MarkerUpdateTicks == 0)
                    UpdateMarkers();
            }
            catch (Exception e)
            {
                Log("update: " + e);
            }
        }

        // ---------------- feed ----------------

        public bool Post(string text, string faction)
        {
            if (!IsServer || string.IsNullOrWhiteSpace(text))
                return false;

            var now = Now();
            if (text == lastText && now - lastTextTime < DuplicateWindowSeconds)
                return false;
            lastText = text;
            lastTextTime = now;

            var item = new NewsItem { Stamp = Stamp(), Text = text, Faction = faction ?? "" };
            Items.Insert(0, item);
            while (Items.Count > MaxItems)
                Items.RemoveAt(Items.Count - 1);
            FeedVersion++;
            SaveState();
            Log("news: " + text);

            if (MyAPIGateway.Multiplayer.MultiplayerActive)
            {
                var packet = new WarNewsPacket { Kind = 2 };
                packet.Items.Add(item);
                MyAPIGateway.Multiplayer.SendMessageToOthers(NetChannel, MyAPIGateway.Utilities.SerializeToBinary(packet));
            }
            return true;
        }

        private void OnMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            try
            {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<WarNewsPacket>(data);
                if (packet == null)
                    return;

                if (IsServer)
                {
                    if (packet.Kind != 3)
                        return;
                    var reply = new WarNewsPacket { Kind = 1 };
                    reply.Items.AddRange(Items);
                    MyAPIGateway.Multiplayer.SendMessageTo(NetChannel, MyAPIGateway.Utilities.SerializeToBinary(reply), sender);
                    return;
                }

                if (!fromServer || packet.Items == null)
                    return;

                if (packet.Kind == 1)
                {
                    Items.Clear();
                    Items.AddRange(packet.Items);
                }
                else if (packet.Kind == 2)
                {
                    for (int i = packet.Items.Count - 1; i >= 0; i--)
                        Items.Insert(0, packet.Items[i]);
                    while (Items.Count > MaxItems)
                        Items.RemoveAt(Items.Count - 1);
                }
                FeedVersion++;
            }
            catch (Exception e)
            {
                Log("message: " + e);
            }
        }

        // ---------------- GPS markers ----------------

        public void SetMarker(string key, string name, string description, Vector3D position, double minutes, bool countdown, Color color)
        {
            if (!IsServer || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(name) || minutes <= 0)
                return;

            RemoveMarker(key);
            name = UniqueName(name);
            markers.Add(new MarkerState
            {
                Key = key, Name = name, Description = description ?? "",
                X = position.X, Y = position.Y, Z = position.Z,
                EndSeconds = Now() + minutes * 60, Countdown = countdown, Color = color.PackedValue
            });
            SaveState();
            UpdateMarkers();
        }

        public void RemoveMarker(string key)
        {
            if (!IsServer)
                return;

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                if (markers[i].Key != key)
                    continue;
                RetireMarker(markers[i]);
                markers.RemoveAt(i);
                SaveState();
            }
        }

        // GPS cleanup matches by name prefix, so no active marker name may be a prefix of another.
        private string UniqueName(string name)
        {
            var candidate = name;
            for (int n = 2; n < 100; n++)
            {
                bool clash = false;
                foreach (var m in markers)
                {
                    if (m.Name.StartsWith(candidate, StringComparison.Ordinal) || candidate.StartsWith(m.Name, StringComparison.Ordinal))
                    {
                        clash = true;
                        break;
                    }
                }
                if (!clash)
                    return candidate;
                candidate = "(" + n + ") " + name;
            }
            return candidate;
        }

        private void RetireMarker(MarkerState m)
        {
            RefreshPlayers();
            foreach (var p in players)
                RemoveGpsByPrefix(p.IdentityId, m.Name, null);
            retired.Add(new RetiredName { Name = m.Name, UntilSeconds = Now() + RetiredKeepSeconds });
            markerSentTo.Remove(m.Key);
        }

        private void UpdateMarkers()
        {
            var now = Now();
            bool dirty = false;
            RefreshPlayers();

            for (int i = retired.Count - 1; i >= 0; i--)
            {
                if (retired[i].UntilSeconds < now) { retired.RemoveAt(i); dirty = true; }
            }

            // Forget players who left, so a rejoin in the same session purges stale markers and gets the current ones again.
            online.Clear();
            foreach (var p in players)
                online.Add(p.IdentityId);
            purgedPlayers.RemoveWhere(id => !online.Contains(id));
            foreach (var sentSet in markerSentTo.Values)
                sentSet.RemoveWhere(id => !online.Contains(id));

            // Players seen for the first time this session lose stale WitM markers they kept while offline.
            foreach (var p in players)
            {
                if (purgedPlayers.Contains(p.IdentityId))
                    continue;
                purgedPlayers.Add(p.IdentityId);
                foreach (var r in retired)
                    RemoveGpsByPrefix(p.IdentityId, r.Name, null);
                foreach (var m in markers)
                    RemoveGpsByPrefix(p.IdentityId, m.Name, null);
            }

            for (int i = markers.Count - 1; i >= 0; i--)
            {
                var m = markers[i];
                if (now >= m.EndSeconds)
                {
                    RetireMarker(m);
                    markers.RemoveAt(i);
                    dirty = true;
                    continue;
                }

                var label = m.Countdown ? m.Name + " - " + Remaining(m.EndSeconds - now) : m.Name;
                HashSet<long> sent;
                if (!markerSentTo.TryGetValue(m.Key, out sent))
                {
                    sent = new HashSet<long>();
                    markerSentTo[m.Key] = sent;
                }
                if (label != m.ShownLabel)
                {
                    m.ShownLabel = label;
                    sent.Clear();
                    dirty = true;
                }

                foreach (var p in players)
                {
                    if (sent.Contains(p.IdentityId))
                        continue;
                    RemoveGpsByPrefix(p.IdentityId, m.Name, label);
                    var gps = MyAPIGateway.Session.GPS.Create(label, m.Description, new Vector3D(m.X, m.Y, m.Z), true, false);
                    gps.GPSColor = new Color(m.Color);
                    MyAPIGateway.Session.GPS.AddGps(p.IdentityId, gps);
                    sent.Add(p.IdentityId);
                }
            }

            if (dirty)
                SaveState();
        }

        private void RemoveGpsByPrefix(long identityId, string prefix, string keep)
        {
            gpsBuffer.Clear();
            MyAPIGateway.Session.GPS.GetGpsList(identityId, gpsBuffer);
            foreach (var g in gpsBuffer)
            {
                if (g.Name == null || !g.Name.StartsWith(prefix, StringComparison.Ordinal) || g.Name == keep)
                    continue;
                MyAPIGateway.Session.GPS.RemoveGps(identityId, g.Hash);
            }
            gpsBuffer.Clear();
        }

        private void RefreshPlayers()
        {
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && !p.IsBot && p.SteamUserId != 0);
        }

        private static string Remaining(double seconds)
        {
            if (seconds >= 3600)
            {
                int h = (int)(seconds / 3600);
                int m = ((int)((seconds % 3600) / 60)) / 15 * 15;
                return m > 0 ? h + "h" + m.ToString("00") + "m" : h + "h";
            }
            if (seconds >= 600)
                return ((int)(seconds / 60)) / 5 * 5 + "m";
            return Math.Max(1, (int)Math.Ceiling(seconds / 60)) + "m";
        }

        // ---------------- state ----------------

        private void LoadState()
        {
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable(StateVariable, out text) || string.IsNullOrEmpty(text))
                    return;
                var state = MyAPIGateway.Utilities.SerializeFromBinary<WarNewsState>(Convert.FromBase64String(text));
                if (state == null)
                    return;
                if (state.Clock != ClockVersion)
                {
                    // One-time migration from the old per-session clock: its times and stamps are meaningless, so drop the feed and the
                    // markers, and keep the marker names as retired so players lose those GPS entries when next seen.
                    if (state.Markers != null)
                        foreach (var m in state.Markers)
                            retired.Add(new RetiredName { Name = m.Name, UntilSeconds = Now() + RetiredKeepSeconds });
                    if (state.Retired != null)
                        foreach (var r in state.Retired)
                            retired.Add(new RetiredName { Name = r.Name, UntilSeconds = Now() + RetiredKeepSeconds });
                    Log("cleared news state saved on the old clock (one-time migration)");
                    SaveState();
                    return;
                }
                if (state.Items != null) Items.AddRange(state.Items);
                if (state.Markers != null) markers.AddRange(state.Markers);
                if (state.Retired != null) retired.AddRange(state.Retired);
                foreach (var m in markers)
                    m.ShownLabel = "";
                FeedVersion++;
            }
            catch (Exception e)
            {
                Log("load: " + e);
            }
        }

        private void SaveState()
        {
            try
            {
                var state = new WarNewsState { Clock = ClockVersion };
                state.Items.AddRange(Items);
                state.Markers.AddRange(markers);
                state.Retired.AddRange(retired);
                MyAPIGateway.Utilities.SetVariable(StateVariable, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(state)));
            }
            catch (Exception e)
            {
                Log("save: " + e);
            }
        }

        // ---------------- helpers ----------------

        // Saved game clock: GameDateTime = 2081-01-01 + ElapsedGameTime, which the checkpoint saves and restores (decompiled
        // MySession.GameDateTime, GetCheckpoint, Load). ElapsedPlayTime resets on every load (qa 2026-10-06 F1).
        public static double Now()
        {
            return (MyAPIGateway.Session.GameDateTime - ClockBase).TotalSeconds;
        }

        private static string Stamp()
        {
            var t = MyAPIGateway.Session.GameDateTime - ClockBase;
            return WarNewsText.Stamp((int)t.TotalDays + 1, t.Hours, t.Minutes);
        }

        public static void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("WW2-WitM War News: " + msg);
        }
    }
}
