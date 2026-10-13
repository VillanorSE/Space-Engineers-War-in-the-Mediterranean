using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using WW2WitM.WarNewsSystem;

// WW2-WitM refresh command (production repo audits/mes-2026-10-10-refresh-command-*.md). Admin/Owner-only chat command that
// replaces the live port and airfield grids (the MES static encounters) with fresh spawns from the current definitions.
//   /witm refresh                                dry run: what would happen per site, and what blocks it. Changes nothing.
//   /witm refresh go [targets] [force] [again]   targets: all (default), ports, airfields, depots, a site name or spawn group.
//   /witm refresh status | cancel | help
//   /witm refresh behaviors                      explains why the behavior refresh is disabled (qa F5).
//   /witm supply ...                             handled by SupplySession.Command (same Admin/Owner check and reply path).
// Per site: safety scan, close the old grids, /MES.Debug.ResetThisStaticEncounters.<group>. If MES re-armed the encounter,
// ProcessStaticEncountersAtLocation and wait for the new grid. Installed MES never re-arms it (NpcManager.ResetThisStaticEncounter
// returns at the group's default non-static conditions profile, without saving), so the group is also removed from MES's saved
// unique list (MES-UniqueEncountersSpawned) and kept in RefreshState.AwaitReload: MES re-arms it when the world loads
// (NpcManager.LoadNewStaticEncounters) and this script forces the spawn about a minute after that load, after the same safety scan.
// An airfield that comes back unowned gets its old owner re-applied if that owner's territory covers it.
// Nothing a player owns, built, sits in or stands near is closed: a site with any such grid or player is skipped and reported.
// A site closed but not yet reset (save, crash, script error) is kept in RefreshState.Pending and finished at the next load.
namespace WW2WitM.Refresh
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class RefreshSession : MySessionComponentBase
    {
        public const string StateVariable = "WW2-Refresh-State";
        private const string CommandProfile = "WW2-Command-Refresh";
        private const string AirfieldOwnerCode = "WitM-AirfieldOwner-";
        private const string DepotOwnerCode = "WitM-DepotOwner-";
        private const string DepotBlockName = "Depot Block";
        private const string AirfieldBlockName = "Airfield Block";
        private const string MesUniqueVariable = "MES-UniqueEncountersSpawned";
        private const string MesStaticsVariable = "MES-ActiveStaticEncounters";
        internal static readonly Guid NpcDataKey =new Guid("29FFD684-13D7-4045-BF76-CD48BF80E36A");

        private const double FootprintMargin = 25;
        private const double AnchorClearance = 250;
        private const double NotFoundClearance = 300;
        private const double OwnerCommandRadius = 3000;
        private const double NewGridRadius = 3000;
        private const double StartCoordsTolerance = 2000;
        private const int StepTicks = 10;
        private const int StepTimeout = 1800;
        private const int RecoveryDelayTicks = 3600;
        private const int RespawnRetryTicks = 18000;
        private const long ResumeWindowTicks = TimeSpan.TicksPerHour * 12;

        private enum Step { Begin, WaitClosed, Reset, Spawn, WaitSpawn, OwnerWait, OwnerVerify, Gap }

        private class SiteScan
        {
            public RefreshSite Site;
            public readonly List<IMyCubeGrid> Grids = new List<IMyCubeGrid>();
            public readonly List<IMyCubeGrid> Debris = new List<IMyCubeGrid>();
            public readonly List<string> Blockers = new List<string>();
            public int MainGrids;
            public string PlayerPiece;
            public Vector3D Anchor;
            public BoundingBoxD Footprint;
            public bool HasFootprint;
            public bool NoAnchor;
            public string Owner = "";
            public string AlsoSpawns = "";
        }

        private class Target
        {
            public RefreshSite Site;
            public bool Explicit;
            public bool Recover;
            public bool Respawn;
            public string Owner = "";
        }

        public static RefreshSession Instance;

        private bool isServer;
        private bool chatHooked;
        private RefreshState state = new RefreshState();

        private readonly List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();
        private readonly List<IMyCharacter> characters = new List<IMyCharacter>();
        private readonly Dictionary<long, NpcDataLite> npcData = new Dictionary<long, NpcDataLite>();
        private readonly HashSet<long> unreadable = new HashSet<long>();
        private readonly HashSet<string> liveGroups = new HashSet<string>();
        private HashSet<string> knownGroups;
        private int keyedGrids;
        private int unknownGroupGrids;
        private readonly HashSet<string> unknownGroupNames = new HashSet<string>();
        private int unkeyedNpcGrids;
        private readonly Dictionary<RefreshSite, Vector3D> anchorCache = new Dictionary<RefreshSite, Vector3D>();
        private readonly List<IMyCubeGrid> groupBuffer = new List<IMyCubeGrid>();
        private readonly List<IMySlimBlock> blockBuffer = new List<IMySlimBlock>();
        private readonly List<IMyPlayer> players = new List<IMyPlayer>();
        private readonly List<string> commandProfiles = new List<string> { CommandProfile };
        private MyPlanet bestPlanet;
        private bool decodeErrorLogged;
        private double scanRadiusSq;
        private Vector3D scanCentre;

        private bool running;
        private bool cancel;
        private bool force;
        private bool skipDone;
        private bool allDone;
        private bool recoveryRun;
        private bool recoveryPending;
        private int idleTicks;
        private readonly List<Target> queue = new List<Target>();
        private int index;
        private Step step;
        private int ticks;
        private int tick;
        private int closedAt;
        private SiteScan current;
        private readonly HashSet<long> oldIds = new HashSet<long>();
        private long newGridId;
        private ulong adminSteam;
        private long adminIdentity;
        private readonly StringBuilder recoveryLog = new StringBuilder();
        private readonly HashSet<string> loadedRespawns = new HashSet<string>();
        private readonly Dictionary<string, string> respawnBlocked = new Dictionary<string, string>();

        public override void LoadData()
        {
            Instance = this;
            isServer = MyAPIGateway.Multiplayer.IsServer;
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEnteredSender += OnChat;
                chatHooked = true;
            }
        }

        public override void BeforeStart()
        {
            if (!isServer)
                return;
            LoadState();
            if (!string.IsNullOrEmpty(state.InProgress))
            {
                foreach (var site in RefreshSites.All)
                    if (site.Short == state.InProgress && !state.Pending.Contains(site.Group))
                        state.Pending.Add(site.Group);
                state.InProgress = "";
                SaveState();
            }
            if (state.Pending.Count > 0)
            {
                recoveryPending = true;
                Log("WARNING: the world was saved after " + string.Join(", ", state.Pending) + " was closed but before it was reset. It is reset automatically in about a minute and respawns after the next save and reload.");
            }
            foreach (var entry in state.AwaitReload)
                loadedRespawns.Add(AwaitGroup(entry));
            if (loadedRespawns.Count > 0)
            {
                recoveryPending = true;
                Log("reset before this load, respawned in about a minute unless a player or grid is in the way: " + string.Join(", ", loadedRespawns));
            }
        }

        protected override void UnloadData()
        {
            try
            {
                if (chatHooked)
                    MyAPIGateway.Utilities.MessageEnteredSender -= OnChat;
            }
            catch (Exception e)
            {
                Log("unload: " + e);
            }
            Instance = null;
        }

        public override void UpdateAfterSimulation()
        {
            if (!isServer)
                return;
            if (!running)
            {
                if (recoveryPending)
                    TryStartRecovery();
                return;
            }
            tick++;
            if (tick % StepTicks != 0)
                return;
            ticks += StepTicks;
            try
            {
                if (tick % 600 == 0)
                    WarNews.Mute(120);
                Advance();
            }
            catch (Exception e)
            {
                Log("run: " + e);
                OnRunError();
            }
        }

        // Reports the failing site and moves on; the run still ends at the end of the queue. If this handler throws too, the run stops.
        private void OnRunError()
        {
            try
            {
                var site = current != null ? current.Site : (step == Step.Begin && index >= 0 && index < queue.Count ? queue[index].Site : null);
                if (site != null)
                {
                    bool closedNotReset = state.Pending.Contains(site.Group);
                    Result(site, closedNotReset
                        ? "FAILED: script error after the old grids were closed. The reset is retried at the next load, or run /witm refresh go " + site.Short + " now; it respawns after a save and reload"
                        : "FAILED: script error, see the server log", false);
                }
                NextSite();
            }
            catch (Exception e)
            {
                running = false;
                current = null;
                state.Unfinished = true;
                SaveState();
                Log("run stopped, error handler failed: " + e);
            }
        }

        // ---------------- chat and network ----------------

        private void OnChat(ulong sender, string text, ref bool sendToOthers)
        {
            try
            {
                if (text == null || !text.StartsWith("/witm", StringComparison.OrdinalIgnoreCase))
                    return;
                sendToOthers = false;
                if (isServer)
                {
                    HandleCommand(sender, text);
                    return;
                }
                var packet = new WarNewsPacket { Kind = 4, Text = text };
                MyAPIGateway.Multiplayer.SendMessageToServer(WarNewsSession.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(packet));
            }
            catch (Exception e)
            {
                Log("chat: " + e);
            }
        }

        // Server: a /witm command from a client (WarNewsSession routes packet kind 4 here; sender is the trusted SteamId).
        public static void OnCommandPacket(ulong sender, string text)
        {
            var s = Instance;
            if (s != null && s.isServer)
                s.HandleCommand(sender, text);
        }

        // Client: a reply from the server (packet kind 5), or a local reply in single player / on the host.
        public static void ShowReply(string text, bool screen)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (screen)
                MyAPIGateway.Utilities.ShowMissionScreen("WitM","", "", text, null, "Close");
            else
                MyAPIGateway.Utilities.ShowMessage("WitM", text);
        }

        private void Reply(ulong steamId, string text, bool screen = false)
        {
            Log(text.Replace("\n", " | "));
            var local = MyAPIGateway.Session.Player;
            if (local != null && local.SteamUserId == steamId)
            {
                ShowReply(text, screen);
                return;
            }
            if (steamId == 0 || !MyAPIGateway.Multiplayer.MultiplayerActive)
                return;
            var packet = new WarNewsPacket { Kind = 5, Text = text, Screen = screen };
            MyAPIGateway.Multiplayer.SendMessageTo(WarNewsSession.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(packet), steamId);
        }

        // To the admin who started the run, or (recovery at load) to every online Admin/Owner.
        private void Tell(string text)
        {
            if (adminSteam != 0)
            {
                Reply(adminSteam, text);
                return;
            }
            Log(text);
            RefreshPlayers();
            bool mp = MyAPIGateway.Multiplayer.MultiplayerActive;
            foreach (var p in players)
            {
                if (!mp || p.PromoteLevel == MyPromoteLevel.Admin || p.PromoteLevel == MyPromoteLevel.Owner)
                {
                    var local = MyAPIGateway.Session.Player;
                    if (local != null && local.SteamUserId == p.SteamUserId)
                        ShowReply(text, false);
                    else if (mp)
                        MyAPIGateway.Multiplayer.SendMessageTo(WarNewsSession.NetChannel,
                            MyAPIGateway.Utilities.SerializeToBinary(new WarNewsPacket { Kind = 5, Text = text }), p.SteamUserId);
                }
            }
            players.Clear();
        }

        private void HandleCommand(ulong steamId, string text)
        {
            try
            {
                var player = FindPlayer(steamId);
                bool allowed = !MyAPIGateway.Multiplayer.MultiplayerActive
                    || (player != null && (player.PromoteLevel == MyPromoteLevel.Admin || player.PromoteLevel == MyPromoteLevel.Owner));
                if (!allowed)
                {
                    Log("denied /witm from " + steamId);
                    Reply(steamId, "Access denied: /witm commands need Admin or Owner rights.");
                    return;
                }

                var words = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2 && Is(words[1], "supply"))
                {
                    bool screen;
                    var reply = WW2WitM.Supply.SupplySession.Command(words, out screen);
                    Reply(steamId, reply, screen);
                    return;
                }
                if (words.Length < 2 || !Is(words[1], "refresh"))
                {
                    Reply(steamId, HelpText(), true);
                    return;
                }
                var sub = words.Length > 2 ? words[2].ToLowerInvariant() : "";
                if (sub == "")
                    DryRun(steamId);
                else if (sub == "go")
                    StartRun(steamId, player, words);
                else if (sub == "status")
                    Status(steamId);
                else if (sub == "cancel")
                    Cancel(steamId);
                else if (sub == "behaviors" || sub == "behaviours")
                    Behaviors(steamId);
                else
                    Reply(steamId, HelpText(), true);
            }
            catch (Exception e)
            {
                Log("command: " + e);
                Reply(steamId, "Error, see the server log: " + e.Message);
            }
        }

        private static string HelpText()
        {
            return "/witm refresh\n  Dry run: lists every port and airfield, what a refresh would do and what blocks it. Changes nothing.\n\n"
                + "/witm refresh go [all|ports|airfields|depots|<site>...] [force] [again]\n  Replaces the live grids with fresh spawns, one site at a time.\n"
                + "  <site>: a name (Toulon, Tripoli = port and airfield) or Port-Tripoli / Airfield-Tripoli. A bare name never picks a depot: use Depot-Benghazi or 'depots'.\n"
                + "  force: also delete NPC-owned debris without spawn data in the way (never anything a player owns, built, sits in or stands near).\n"
                + "  again: redo sites an unfinished run of the last 12 h already did (older runs are always redone).\n\n"
                + "  MES can only re-arm a reset site when the world loads: after 'go', save and reload; the sites respawn about a minute later.\n\n"
                + "/witm refresh status, /witm refresh cancel\n\n"
                + "/witm refresh behaviors\n  Explains why the behavior refresh is disabled.\n\n"
                + WW2WitM.Supply.SupplySession.HelpText();
        }

        // ---------------- dry run, status, cancel, behaviors ----------------

        private void DryRun(ulong steamId)
        {
            var mes = Mes();
            var sb = new StringBuilder();
            sb.Append("Dry run: nothing was changed.\n");
            if (mes == null || !mes.HasRefreshApi)
                sb.Append("WARNING: the MES API is not available yet, so 'go' would refuse to start.\n");
            if (running)
                sb.Append("A refresh is running now (/witm refresh status).\n");
            AppendNotes(sb);

            ScanWorld();
            anchorCache.Clear();
            if (NpcDataBroken())
                sb.Append("WARNING: ").Append(NpcDataBrokenText()).Append('\n');
            else if (NpcKeyMissing())
                sb.Append("WARNING: ").Append(NpcKeyMissingText()).Append(" 'force' is refused.\n");
            sb.Append('\n');
            sb.Append(RefreshSites.ClosedDepotLines());
            foreach (var site in RefreshSites.All)
            {
                var scan = ScanSite(site);
                string verdict, why;
                if (scan.MainGrids == 0 && AwaitOwner(site.Group) != null)
                    verdict = !loadedRespawns.Contains(site.Group) ? "RESET: respawns after the world is saved and reloaded"
                        : RespawnBlocked(scan, out why) ? "RESET before this load, WAITING to respawn: " + why
                        : "RESET before this load: respawned automatically, or by the next 'go'";
                else
                    Decide(scan, false, false, out verdict);
                sb.Append(site.Label).Append(": ").Append(verdict).Append('\n');
            }

            int level;
            if (!MyAPIGateway.Utilities.GetVariable("WarLevel", out level))
                level = 1;
            sb.Append("\nWar Level ").Append(level).Append(": 'go' marks levels 2-").Append(Math.Max(2, level))
                .Append(" as already announced (only where not set yet), so a refreshed Civitavecchia does not announce them again.\n");
            sb.Append("War News is muted during the run and for 60 s after. Airfields that re-flip post the MES capture chat and launch an offensive and an air-cargo flight.\n");
            sb.Append("The forced spawn also spawns every other pending MES static encounter (any mod) whose trigger radius covers the site; WitM ones are named above as 'may also spawn'.");
            Reply(steamId, sb.ToString(), true);
        }

        // Pending recoveries, the last recovery's result, and which sites a 'go' would skip.
        private void AppendNotes(StringBuilder sb)
        {
            if (state.Pending.Count > 0)
                sb.Append("WARNING: closed but not reset (the site is missing): ").Append(string.Join(", ", state.Pending))
                    .Append(". 'go' finishes them first").Append(running ? "" : "; otherwise it happens automatically after the next load").Append(".\n");
            var reload = ReloadList();
            if (reload.Length > 0)
                sb.Append("SAVE AND RELOAD the world to respawn: ").Append(reload).Append(" (MES re-arms a reset site only when the world loads).\n");
            if (respawnBlocked.Count > 0)
                sb.Append("WAITING to respawn after this load (checked every 5 min and by 'go'): ").Append(BlockedList()).Append('\n');
            if (!string.IsNullOrEmpty(state.RecoveryNote))
                sb.Append("Recovery at the last load: ").Append(state.RecoveryNote).Append('\n');
            if (state.Unfinished && state.Done.Count > 0)
            {
                if (RecentRun())
                    sb.Append("The last run (").Append(HoursAgo()).Append(" h ago) was not finished: 'go' SKIPS ").Append(string.Join(", ", state.Done))
                        .Append(" unless you add 'again'.\n");
                else
                    sb.Append("The last unfinished run is older than 12 h: 'go' redoes every site.\n");
            }
        }

        private void Status(ulong steamId)
        {
            var sb = new StringBuilder();
            if (running)
            {
                var label = index >= 0 && index < queue.Count ? queue[index].Site.Label : "-";
                sb.Append(recoveryRun ? "Recovery running: site " : "Running: site ").Append(Math.Min(index + 1, queue.Count)).Append(" of ").Append(queue.Count)
                    .Append(" (").Append(label).Append("), step ").Append(step.ToString()).Append(cancel ? ", cancelling" : "").Append(".\n\n");
            }
            else if (state.Lines.Count == 0)
            {
                sb.Append("No refresh has run in this world.\n");
            }
            else
            {
                sb.Append(state.Unfinished ? "Last run (not complete):\n\n" : "Last run (complete):\n\n");
            }
            foreach (var line in state.Lines)
                sb.Append(line).Append('\n');
            sb.Append('\n');
            AppendNotes(sb);
            Reply(steamId, sb.ToString(), true);
        }

        private void Cancel(ulong steamId)
        {
            if (!running)
            {
                Reply(steamId, "No refresh is running.");
                return;
            }
            cancel = true;
            Reply(steamId, "Cancelling: the current site finishes, the rest are left as they are.");
        }

        // qa F5: switching a behavior (PreserveTriggersOnBehaviorSwitch:false) loads the template's triggers fresh (StoredSettings.cs:253-257,
        // CoreBehavior.cs:1258-1270): spent MaxActions, toggled UseTrigger and running cooldowns are all reset, which re-fires escort spawns,
        // offensive/cargo/defense spawns, WarActivity counts and chats. The grid's AI also stays terminated until a reload
        // (CoreBehavior.cs:499, BehaviorManager.cs:197-198). Only passive behaviors with no such triggers would be safe, and refreshing those
        // gains nothing, so it is disabled and its SBC hooks were removed (D-045).
        private void Behaviors(ulong steamId)
        {
            Reply(steamId, "Behavior refresh: DISABLED\n\n"
                + "Switching a live NPC grid to its current behavior makes MES load every trigger fresh. One-shot triggers (escort spawns, "
                + "War Level hit counts, strike chats) re-arm and cooldowns restart, so ports, airfields, ships and planes would spawn and count "
                + "again; their AI would also stop until the world is reloaded. That can't be made safe from outside MES, so it is not offered.\n\n"
                + "Ports and airfields get their current behavior with '/witm refresh go'. Ships and planes pick it up when they respawn.", true);
        }

        // ---------------- run ----------------

        private void StartRun(ulong steamId, IMyPlayer player, string[] words)
        {
            if (running)
            {
                Reply(steamId, "A refresh is already running (/witm refresh status).");
                return;
            }
            var mes = Mes();
            if (mes == null || !mes.HasRefreshApi)
            {
                Reply(steamId, "The MES API is not available yet; try again in a minute.");
                return;
            }
            ScanWorld();
            if (NpcDataBroken())
            {
                Reply(steamId, "Refused: " + NpcDataBrokenText() + " Nothing was changed.");
                return;
            }

            queue.Clear();
            force = false;
            bool again = false;
            bool anyTarget = false;
            for (int i = 3; i < words.Length; i++)
            {
                var w = words[i];
                if (Is(w, "force")) { force = true; continue; }
                if (Is(w, "again")) { again = true; continue; }
                anyTarget = true;
                bool matched = false;
                foreach (var site in RefreshSites.All)
                {
                    bool all = Is(w, "all");
                    bool kind = (Is(w, "ports") && !site.Owned) || (Is(w, "airfields") && site.Airfield) || (Is(w, "depots") && site.Depot);
                    bool named = Is(w, site.Group) || Is("WW2-" + w, site.Group)
                        || (!site.Depot && (Is(w, site.Name.Replace(" ", "")) || Is(w, site.Name)));
                    if (!all && !kind && !named)
                        continue;
                    matched = true;
                    AddTarget(site, named);
                }
                if (!matched)
                {
                    queue.Clear();
                    Reply(steamId, "Unknown target '" + w + "'. Use all, ports, airfields, depots or a site name (/witm refresh help).");
                    return;
                }
            }
            if (force && NpcKeyMissing())
            {
                queue.Clear();
                Reply(steamId, "Refused 'force': " + NpcKeyMissingText() + " Nothing was changed.");
                return;
            }
            if (!anyTarget)
                foreach (var site in RefreshSites.All)
                    AddTarget(site, false);
            int recovering = QueuePending() + QueueRespawns();

            var sb = new StringBuilder();
            bool recent = RecentRun();
            skipDone = state.Unfinished && !again && recent && state.Done.Count > 0;
            if (skipDone)
            {
                var skipped = new List<string>();
                foreach (var t in queue)
                    if (!t.Recover && !t.Respawn && state.Done.Contains(t.Site.Group))
                        skipped.Add(t.Site.Label);
                if (skipped.Count > 0)
                    sb.Append("\nSKIPPING ").Append(skipped.Count).Append(" site(s) the unfinished run of ").Append(HoursAgo()).Append(" h ago already refreshed: ")
                        .Append(string.Join(", ", skipped)).Append(". Add 'again' to redo them.");
            }
            else
            {
                if (state.Unfinished && !again && state.Done.Count > 0)
                    sb.Append("\nThe last unfinished run is older than 12 h, so every site is redone.");
                state.Done.Clear();
            }
            if (recovering > 0)
                sb.Append("\nFirst finishing ").Append(recovering).Append(" site(s) that were closed but not reset, or reset before this load.");
            if (!string.IsNullOrEmpty(state.RecoveryNote))
                sb.Append("\nRecovery at the last load: ").Append(state.RecoveryNote);
            state.RecoveryNote = "";
            state.Lines.Clear();
            state.Unfinished = true;
            state.StartedUtcTicks = DateTime.UtcNow.Ticks;
            SaveState();
            SeedWarLevelAnnounce();

            adminSteam = steamId;
            adminIdentity = player != null ? player.IdentityId : 0;
            BeginRun(false);
            Reply(steamId, "Refresh started: " + queue.Count + " site(s)" + (force ? ", force" : "") + ". Progress follows; /witm refresh status or cancel." + sb.ToString());
        }

        // Puts every closed-but-not-reset site at the front of the queue, as an explicit recovery target.
        private int QueuePending()
        {
            int n = 0;
            for (int p = state.Pending.Count - 1; p >= 0; p--)
            {
                RefreshSite site = null;
                foreach (var s in RefreshSites.All)
                    if (s.Group == state.Pending[p])
                        site = s;
                if (site == null)
                {
                    state.Pending.RemoveAt(p);
                    continue;
                }
                Target found = null;
                foreach (var t in queue)
                    if (t.Site == site)
                        found = t;
                if (found != null)
                    queue.Remove(found);
                queue.Insert(0, new Target { Site = site, Explicit = true, Recover = true });
                n++;
            }
            return n;
        }

        private void BeginRun(bool recovery)
        {
            recoveryRun = recovery;
            recoveryPending = false;
            cancel = false;
            allDone = true;
            anchorCache.Clear();
            current = null;
            index = 0;
            SetStep(Step.Begin);
            running = true;
            WarNews.Mute(120);
        }

        // At load, a site that was closed but not reset: wait for MES and a minute of game time, then finish it without force.
        private void TryStartRecovery()
        {
            idleTicks++;
            if (idleTicks < RecoveryDelayTicks || idleTicks % 600 != 0)
                return;
            try
            {
                var mes = Mes();
                if (mes == null || !mes.HasRefreshApi)
                    return;
                queue.Clear();
                if (QueuePending() + QueueRespawns() == 0)
                {
                    recoveryPending = false;
                    SaveState();
                    return;
                }
                ScanWorld();
                if (NpcDataBroken())
                {
                    recoveryPending = false;
                    var labels = QueueLabels();
                    queue.Clear();
                    Tell("WitM refresh: " + labels + " can't be finished after this load: " + NpcDataBrokenText());
                    return;
                }
                if (StillBlocked())
                {
                    queue.Clear();
                    idleTicks = RecoveryDelayTicks - RespawnRetryTicks;
                    return;
                }
                force = false;
                skipDone = false;
                adminSteam = 0;
                adminIdentity = 0;
                recoveryLog.Clear();
                BeginRun(true);
                Tell("WitM refresh: finishing " + queue.Count + " site(s) from before this load: " + QueueLabels() + ".");
            }
            catch (Exception e)
            {
                recoveryPending = false;
                Log("recovery: " + e);
            }
        }

        private void AddTarget(RefreshSite site, bool named)
        {
            foreach (var t in queue)
            {
                if (t.Site != site)
                    continue;
                t.Explicit |= named;
                return;
            }
            queue.Add(new Target { Site = site, Explicit = named });
        }

        private void SetStep(Step s)
        {
            step = s;
            ticks = 0;
        }

        private void Advance()
        {
            var mes = Mes();
            switch (step)
            {
                case Step.Begin:
                    BeginSite();
                    break;

                case Step.WaitClosed:
                    if (AllClosed())
                    {
                        if (closedAt < 0)
                            closedAt = ticks;
                        if (ticks - closedAt >= 60)
                            SetStep(Step.Reset);
                    }
                    else if (ticks >= 600)
                    {
                        bool someClosed = AnyClosed();
                        if (!someClosed)
                            Unpend(current.Site);
                        Result(current.Site, "FAILED: the old grids did not close within 10 s; the encounter was not reset"
                            + (someClosed ? ". Some of its grids are gone: the reset is retried at the next load, or run /witm refresh go " + current.Site.Short : ""), false);
                        NextSite();
                    }
                    break;

                case Step.Reset:
                    if (mes == null)
                    {
                        if (ticks >= StepTimeout)
                        {
                            Result(current.Site, "FAILED: the MES API went away after the old grids were closed. The reset is retried at the next load, or run /witm refresh go "
                                + current.Site.Short, false);
                            NextSite();
                        }
                        break;
                    }
                    ResetEncounter(mes, current.Site.Group);
                    Unpend(current.Site);
                    if (MesArmed(current.Site.Group))
                    {
                        SetStep(Step.Spawn);
                        break;
                    }
                    {
                        string error;
                        if (!ForgetUnique(current.Site.Group, out error))
                        {
                            Result(current.Site, "FAILED: closed and reset, but MES's saved unique list could not be updated (" + error
                                + "), so it will not respawn even after a reload. See the refresh change record", false);
                            NextSite();
                            break;
                        }
                        AddAwait(current.Site.Group, current.Owner);
                        state.Done.Add(current.Site.Group);
                        Result(current.Site, "RESET: closed; MES re-arms it only when the world loads, so it respawns about a minute after a save and reload"
                            + (current.Site.Owned && (current.Owner == "Gray" || current.Owner == "Green") ? " (owner " + current.Owner + " is re-applied then)" : ""), true);
                        NextSite();
                    }
                    break;

                case Step.Spawn:
                    if (ticks < 30)
                        break;
                    if (mes == null)
                    {
                        if (ticks >= StepTimeout)
                        {
                            state.Done.Add(current.Site.Group);
                            Result(current.Site, "PENDING: reset done but the MES API went away before the spawn. It spawns when a player comes within "
                                + current.Site.TriggerRadiusKm + " km", true);
                            NextSite();
                        }
                        break;
                    }
                    mes.ProcessStaticEncountersAtLocation(current.Anchor);
                    SetStep(Step.WaitSpawn);
                    break;

                case Step.WaitSpawn:
                    if (ticks % 60 == 0)
                    {
                        var grid = FindNewGrid();
                        if (grid != null)
                        {
                            newGridId = grid.EntityId;
                            state.Done.Add(current.Site.Group);
                            if (current.Site.Owned && (current.Owner == "Gray" || current.Owner == "Green"))
                            {
                                SetStep(Step.OwnerWait);
                                break;
                            }
                            Result(current.Site, "REFRESHED (new grid " + newGridId + ")" + (current.Site.Owned ? ", owner was " + Show(current.Owner) + ", re-derived by territory" : "") + Also(), true);
                            NextSite();
                            break;
                        }
                    }
                    if (ticks == 600 && mes != null)
                        mes.ProcessStaticEncountersAtLocation(current.Anchor);
                    if (ticks >= StepTimeout)
                    {
                        state.Done.Add(current.Site.Group);
                        Result(current.Site, "PENDING: reset done but no spawn within 30 s. It spawns when a player comes within "
                            + current.Site.TriggerRadiusKm + " km; check /MES.IGLSD.SpawnRecord" + Also(), true);
                        NextSite();
                    }
                    break;

                case Step.OwnerWait:
                    if (ticks < 1800)
                        break;
                    {
                        var owner = OwnerOfGrid(newGridId);
                        if (owner == current.Owner)
                        {
                            Result(current.Site, "REFRESHED (new grid " + newGridId + "), owner " + owner + " kept by territory" + Also(), true);
                            NextSite();
                        }
                        else if (owner == "Unowned" && mes != null)
                        {
                            var grid = MyAPIGateway.Entities.GetEntityById(newGridId);
                            var pos = grid != null ? grid.PositionComp.WorldAABB.Center : current.Anchor;
                            mes.SendBehaviorCommand(commandProfiles, pos, (current.Site.Depot ? DepotOwnerCode : AirfieldOwnerCode) + current.Owner, OwnerCommandRadius, 0);
                            SetStep(Step.OwnerVerify);
                        }
                        else
                        {
                            Result(current.Site, "REFRESHED (new grid " + newGridId + "); OWNER differs: was " + current.Owner + ", now " + Show(owner) + Also(), true);
                            NextSite();
                        }
                    }
                    break;

                case Step.OwnerVerify:
                    if (ticks < 900)
                        break;
                    {
                        var owner = OwnerOfGrid(newGridId);
                        if (owner == current.Owner)
                            Result(current.Site, "REFRESHED (new grid " + newGridId + "), OWNER re-applied " + owner + Also(), true);
                        else
                            Result(current.Site, "REFRESHED (new grid " + newGridId + "); OWNER differs: was " + current.Owner + ", now " + Show(owner)
                                + " (the old owner's territory no longer covers it)" + Also(), true);
                        NextSite();
                    }
                    break;

                case Step.Gap:
                    if (ticks < 180)
                        break;
                    index++;
                    if (cancel || index >= queue.Count)
                        Finish();
                    else
                        SetStep(Step.Begin);
                    break;
            }
        }

        private void BeginSite()
        {
            current = null;
            var target = queue[index];
            var site = target.Site;

            if (target.Respawn)
            {
                BeginRespawn(target);
                return;
            }
            if (skipDone && !target.Recover && state.Done.Contains(site.Group))
            {
                Result(site, "SKIPPED: already refreshed by the unfinished run of " + HoursAgo() + " h ago (add 'again' to redo)", true);
                SkipToGap();
                return;
            }

            ScanWorld();
            if (NpcDataBroken())
            {
                Result(site, "FAILED: " + NpcDataBrokenText() + " Nothing was changed.", false);
                SkipToGap();
                return;
            }
            if (force && NpcKeyMissing())
            {
                Result(site, "FAILED: " + NpcKeyMissingText() + " 'force' is refused; nothing was changed.", false);
                SkipToGap();
                return;
            }
            var scan = ScanSite(site);
            if (AwaitOwner(site.Group) != null && !loadedRespawns.Contains(site.Group))
            {
                if (scan.MainGrids == 0)
                {
                    Result(site, "WAITING: already reset in this session; it respawns after the world is saved and reloaded", true);
                    SkipToGap();
                    return;
                }
                RemoveAwait(site.Group);
                SaveState();
            }
            if (target.Recover && scan.MainGrids > 0)
            {
                Unpend(site);
                Result(site, "RECOVERED: the site is present (it was closed but not reset before a save); nothing to do", true);
                SkipToGap();
                return;
            }
            string verdict;
            if (!Decide(scan, target.Explicit, force, out verdict))
            {
                Result(site, (target.Recover ? "STILL MISSING (closed but not reset): " : "") + verdict, false);
                SkipToGap();
                return;
            }

            WarNews.Mute(120);
            current = scan;
            closedAt = -1;
            newGridId = 0;
            oldIds.Clear();
            foreach (var g in scan.Grids)
                oldIds.Add(g.EntityId);
            if (force)
                foreach (var g in scan.Debris)
                    oldIds.Add(g.EntityId);
            if (!state.Pending.Contains(site.Group))
                state.Pending.Add(site.Group);
            SaveState();

            foreach (var g in scan.Grids)
                if (!g.MarkedForClose) g.Close();
            if (force)
                foreach (var g in scan.Debris)
                    if (!g.MarkedForClose) g.Close();
            Log(site.Label + ": closed " + scan.Grids.Count + " grid(s)" + (force ? " and " + scan.Debris.Count + " debris" : ""));
            SetStep(Step.WaitClosed);
        }

        private void SkipToGap()
        {
            SetStep(Step.Gap);
            ticks = 170;
        }

        private void Unpend(RefreshSite site)
        {
            if (state.Pending.Remove(site.Group))
                SaveState();
        }

        private void NextSite()
        {
            current = null;
            SaveState();
            SetStep(Step.Gap);
        }

        private void Finish()
        {
            running = false;
            current = null;
            try
            {
                for (int i = index; i < queue.Count; i++)
                {
                    if (i < 0)
                        continue;
                    Result(queue[i].Site, "CANCELLED: left as it is", false);
                }
                if (recoveryRun)
                {
                    state.RecoveryNote = recoveryLog.ToString();
                    state.Unfinished |= !allDone;
                }
                else
                {
                    state.Unfinished = !allDone;
                }
                SaveState();
                if (respawnBlocked.Count > 0)
                {
                    recoveryPending = true;
                    idleTicks = RecoveryDelayTicks - RespawnRetryTicks;
                }
                WarNews.Mute(60);
                Tell((recoveryRun ? "Refresh recovery finished" : "Refresh finished") + (allDone ? "" : " with sites left to do (/witm refresh status)")
                    + ". War News stays muted for 60 s." + PendingWarning());
            }
            catch (Exception e)
            {
                Log("finish: " + e);
            }
        }

        private string PendingWarning()
        {
            var reload = ReloadList();
            return (state.Pending.Count > 0 ? " WARNING: closed but not reset: " + string.Join(", ", state.Pending) + " (finished at the next load or the next 'go')." : "")
                + (reload.Length > 0 ? " SAVE AND RELOAD the world to respawn: " + reload + "." : "")
                + (respawnBlocked.Count > 0 ? " WAITING to respawn (checked every 5 min): " + BlockedList() + "." : "");
        }

        private string BlockedList()
        {
            var sb = new StringBuilder();
            foreach (var kv in respawnBlocked)
                sb.Append(sb.Length > 0 ? "; " : "").Append(kv.Key).Append(" (").Append(kv.Value).Append(')');
            return sb.ToString();
        }

        // Sites reset in this session that wait for a save and reload (MES re-arms static encounters only at load).
        private string ReloadList()
        {
            var sb = new StringBuilder();
            foreach (var entry in state.AwaitReload)
            {
                var group = AwaitGroup(entry);
                if (loadedRespawns.Contains(group))
                    continue;
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(group);
            }
            return sb.ToString();
        }

        private string QueueLabels()
        {
            var sb = new StringBuilder();
            foreach (var t in queue)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(t.Site.Label);
            }
            return sb.ToString();
        }

        // ---------------- MES static encounter bookkeeping ----------------

        // /MES.Debug.ResetThisStaticEncounters removes ONE entry of the group from MES's in-memory UniqueGroupsSpawned (List.Remove,
        // NpcManager.cs:286) and, in installed MES, returns without saving. MES saves that list on every other change (SpawnRequest.cs:574-575,
        // NpcManager.UpdateStaticEncounters). A reset of a group not in memory does nothing (NpcManager.cs:284), so at least one is sent;
        // it stops once MES arms the group, since a fixed MES adds a StaticEncounter per reset without a duplicate check (qa L1).
        private void ResetEncounter(MesApiLite mes, string group)
        {
            int count = Math.Max(1, CountUnique(group));
            for (int i = 0; i < count; i++)
            {
                mes.ChatCommand("/MES.Debug.ResetThisStaticEncounters." + group, MatrixD.CreateTranslation(current.Anchor), adminIdentity, adminSteam);
                if (MesArmed(group))
                    break;
            }
        }

        // Entries of the group in MES's saved unique list, or -1 if it can't be read.
        private int CountUnique(string group)
        {
            var list = ReadMesList<string>(MesUniqueVariable);
            if (list == null)
                return -1;
            int n = 0;
            foreach (var g in list)
                if (g == group)
                    n++;
            return n;
        }

        // True if MES's saved static encounters hold a valid one for the group (a fixed ResetThisStaticEncounter saves it at once).
        private static bool MesArmed(string group)
        {
            var list = ReadMesList<StaticEncounterLite>(MesStaticsVariable);
            if (list == null)
                return false;
            foreach (var e in list)
                if (e != null && e.IsValid && e.SpawnGroupName == group)
                    return true;
            return false;
        }

        // Removes the group from MES's saved unique list, so NpcManager.LoadNewStaticEncounters re-arms it at the next load.
        // MES's in-memory list no longer has it (the reset removed it), so MES's own later saves keep it out.
        private bool ForgetUnique(string group, out string error)
        {
            error = "";
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable(MesUniqueVariable, out text) || string.IsNullOrEmpty(text))
                    return true;
                var list = MyAPIGateway.Utilities.SerializeFromBinary<List<string>>(Convert.FromBase64String(text));
                if (list == null)
                {
                    error = "unreadable";
                    return false;
                }
                int removed = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] == group)
                    {
                        list.RemoveAt(i);
                        removed++;
                    }
                }
                if (removed > 0)
                    MyAPIGateway.Utilities.SetVariable(MesUniqueVariable, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(list)));
                Log("removed " + group + " from " + MesUniqueVariable + " (" + removed + " entr" + (removed == 1 ? "y" : "ies") + ")");
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Log("could not update " + MesUniqueVariable + ": " + e);
                return false;
            }
        }

        private static List<T> ReadMesList<T>(string variable)
        {
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable(variable, out text))
                    return new List<T>();
                if (string.IsNullOrEmpty(text))
                    return new List<T>();
                return MyAPIGateway.Utilities.SerializeFromBinary<List<T>>(Convert.FromBase64String(text)) ?? new List<T>();
            }
            catch (Exception e)
            {
                Log("could not read " + variable + ": " + e.Message);
                return null;
            }
        }

        private static string AwaitGroup(string entry)
        {
            int bar = entry.IndexOf('|');
            return bar < 0 ? entry : entry.Substring(0, bar);
        }

        // The saved owner of a site waiting for a reload ("" if none), or null if it isn't waiting.
        private string AwaitOwner(string group)
        {
            foreach (var entry in state.AwaitReload)
            {
                if (AwaitGroup(entry) != group)
                    continue;
                int bar = entry.IndexOf('|');
                return bar < 0 ? "" : entry.Substring(bar + 1);
            }
            return null;
        }

        private void AddAwait(string group, string owner)
        {
            var old = AwaitOwner(group);
            if (string.IsNullOrEmpty(owner) && old != null)
                owner = old;
            RemoveAwait(group);
            state.AwaitReload.Add(group + "|" + (owner ?? ""));
            SaveState();
        }

        private void RemoveAwait(string group)
        {
            for (int i = state.AwaitReload.Count - 1; i >= 0; i--)
                if (AwaitGroup(state.AwaitReload[i]) == group)
                    state.AwaitReload.RemoveAt(i);
        }

        // Puts every site reset before this load at the front of the queue, to force its spawn now that MES has re-armed it.
        private int QueueRespawns()
        {
            int n = 0;
            for (int p = state.AwaitReload.Count - 1; p >= 0; p--)
            {
                var group = AwaitGroup(state.AwaitReload[p]);
                if (!loadedRespawns.Contains(group))
                    continue;
                RefreshSite site = null;
                foreach (var s in RefreshSites.All)
                    if (s.Group == group)
                        site = s;
                if (site == null)
                {
                    state.AwaitReload.RemoveAt(p);
                    loadedRespawns.Remove(group);
                    respawnBlocked.Remove(group);
                    continue;
                }
                Target found = null;
                foreach (var t in queue)
                    if (t.Site == site)
                        found = t;
                if (found != null)
                    queue.Remove(found);
                queue.Insert(0, new Target { Site = site, Explicit = true, Respawn = true, Owner = AwaitOwner(group) ?? "" });
                n++;
            }
            return n;
        }

        // A site reset before this load: MES re-armed it at load (or already spawned it for a nearby player). Force the spawn and wait for it.
        private void BeginRespawn(Target target)
        {
            var site = target.Site;
            ScanWorld();
            if (NpcDataBroken())
            {
                DropRespawn(site.Group);
                Result(site, "FAILED (respawn after reload): " + NpcDataBrokenText(), false);
                SkipToGap();
                return;
            }
            var scan = ScanSite(site);
            if (scan.NoAnchor)
            {
                DropRespawn(site.Group);
                Result(site, "FAILED (respawn after reload): no planet found, so the site's position is unknown", false);
                SkipToGap();
                return;
            }
            string why;
            if (RespawnBlocked(scan, out why))
            {
                respawnBlocked[site.Group] = why;
                Result(site, "WAITING to respawn after this load, " + why + ". Checked again every 5 min, and by the next 'go'", true);
                SkipToGap();
                return;
            }
            DropRespawn(site.Group);
            current = scan;
            current.Owner = target.Owner ?? "";
            oldIds.Clear();
            newGridId = 0;
            if (scan.MainGrids > 0)
            {
                SetStep(Step.WaitSpawn);
                return;
            }
            if (!MesArmed(site.Group))
            {
                current = null;
                Result(site, "FAILED: MES did not re-arm it at this load (still in " + MesUniqueVariable + "?); check /MES.IGLSD.SpawnRecord, then run 'go " + site.Short + "' and reload again", false);
                SkipToGap();
                return;
            }
            SetStep(Step.Spawn);
        }

        private void DropRespawn(string group)
        {
            RemoveAwait(group);
            loadedRespawns.Remove(group);
            respawnBlocked.Remove(group);
            SaveState();
        }

        // qa L3: a missing site waiting to respawn is not forced while 'go' (no force) would skip it: a player, a non-NPC grid or NPC debris in the way.
        private bool RespawnBlocked(SiteScan scan, out string why)
        {
            why = "";
            string verdict;
            if (scan.NoAnchor || scan.MainGrids > 0 || Decide(scan, true, false, out verdict))
                return false;
            why = verdict.StartsWith("SKIPPED", StringComparison.Ordinal) ? verdict.Substring(7).TrimStart(':', ' ') : verdict;
            why = why.Replace("; add 'force' to delete them", "; remove them by hand");
            return true;
        }

        // At a recovery check: true if every queued site is a respawn that was blocked and still is, so no run starts (no chat, no mute).
        private bool StillBlocked()
        {
            foreach (var t in queue)
            {
                string why;
                if (!t.Respawn || !respawnBlocked.ContainsKey(t.Site.Group) || !RespawnBlocked(ScanSite(t.Site), out why))
                    return false;
                respawnBlocked[t.Site.Group] = why;
            }
            return queue.Count > 0;
        }

        // Records one site's outcome in the saved report and tells the admin. ok = the site needs no further run.
        private void Result(RefreshSite site, string text, bool ok)
        {
            if (!ok)
                allDone = false;
            var line = site.Label + ": " + text;
            state.Lines.Add(line);
            if (recoveryRun)
                recoveryLog.Append(recoveryLog.Length > 0 ? " | " : "").Append(line);
            SaveState();
            Tell(line);
        }

        private string Also()
        {
            return current == null || current.AlsoSpawns.Length == 0 ? "" : "; may also have spawned pending " + current.AlsoSpawns;
        }

        // ---------------- safety scan ----------------

        // Returns true if the site may be refreshed; verdict describes the plan or why it is skipped.
        private bool Decide(SiteScan scan, bool explicitTarget, bool allowDebris, out string verdict)
        {
            if (scan.NoAnchor)
            {
                verdict = "FAILED: no planet found, so the site's position is unknown; nothing was checked or changed";
                return false;
            }
            if (scan.PlayerPiece != null)
            {
                verdict = "SKIPPED: " + scan.PlayerPiece;
                return false;
            }
            if (scan.Blockers.Count > 0)
            {
                verdict = "SKIPPED blocked: " + string.Join(", ", scan.Blockers);
                return false;
            }
            if (scan.Debris.Count > 0 && !allowDebris)
            {
                verdict = "SKIPPED blocked: " + scan.Debris.Count + " NPC-owned piece(s) without spawn data in the way (" + Names(scan.Debris) + "); add 'force' to delete them";
                return false;
            }
            var also = scan.AlsoSpawns.Length > 0 ? "; may also spawn pending " + scan.AlsoSpawns : "";
            if (scan.MainGrids == 0)
            {
                if (explicitTarget)
                {
                    verdict = "NOT FOUND, resetting it so MES spawns it again" + also;
                    return true;
                }
                verdict = "NOT FOUND: no live grid. 'go " + scan.Site.Short + "' resets it so it respawns (after a save and reload)" + also;
                return false;
            }
            var sb = new StringBuilder();
            sb.Append("would refresh: ").Append(scan.Grids.Count).Append(" grid(s)");
            if (scan.MainGrids > 1)
                sb.Append(", ").Append(scan.MainGrids).Append(" copies of the site");
            if (scan.Debris.Count > 0)
                sb.Append(", plus ").Append(scan.Debris.Count).Append(" NPC debris (force)");
            if (scan.Site.Owned)
                sb.Append(", owner ").Append(Show(scan.Owner));
            sb.Append(also);
            verdict = sb.ToString();
            return true;
        }

        // Collects grids (all, or only those whose centre is within radius of centre) and live characters, and decodes MES NpcData.
        private void ScanWorld(double radius = 0, Vector3D centre = default(Vector3D))
        {
            allGrids.Clear();
            characters.Clear();
            npcData.Clear();
            unreadable.Clear();
            liveGroups.Clear();
            keyedGrids = 0;
            unknownGroupGrids = 0;
            unknownGroupNames.Clear();
            unkeyedNpcGrids = 0;
            if (knownGroups == null)
                LoadKnownGroups();
            scanRadiusSq = radius > 0 ? radius * radius : 0;
            scanCentre = centre;
            MyAPIGateway.Entities.GetEntities(null, CollectEntity);
            if (keyedGrids == 0 && scanRadiusSq == 0)
            {
                foreach (var g in allGrids)
                {
                    if (g.BigOwners.Count + g.SmallOwners.Count > 0 && NonNpcReason(g, false) == null && ++unkeyedNpcGrids >= 3)
                        break;
                }
            }
            RefreshPlayers();
            foreach (var p in players)
            {
                var ch = p.Character;
                if (ch != null && !ch.IsDead && !characters.Contains(ch))
                    characters.Add(ch);
            }
            players.Clear();
        }

        private bool CollectEntity(IMyEntity e)
        {
            var ch = e as IMyCharacter;
            if (ch != null)
            {
                if (!ch.MarkedForClose && !ch.IsDead && !ch.IsBot)
                    characters.Add(ch);
                return false;
            }
            var grid = e as IMyCubeGrid;
            if (grid == null || grid.MarkedForClose)
                return false;
            if (scanRadiusSq > 0 && Vector3D.DistanceSquared(grid.PositionComp.WorldAABB.Center, scanCentre) > scanRadiusSq)
                return false;
            allGrids.Add(grid);
            if (grid.Storage == null || !grid.Storage.ContainsKey(NpcDataKey))
                return false;
            keyedGrids++;
            var data = ReadNpcData(grid);
            if (data != null && !string.IsNullOrEmpty(data.SpawnGroupName) && knownGroups.Contains(data.SpawnGroupName))
            {
                npcData[grid.EntityId] = data;
                liveGroups.Add(data.SpawnGroupName);
            }
            else
            {
                unreadable.Add(grid.EntityId);
                if (data != null && !string.IsNullOrEmpty(data.SpawnGroupName))
                {
                    unknownGroupGrids++;
                    unknownGroupNames.Add(data.SpawnGroupName);
                }
            }
            return false;
        }

        private void LoadKnownGroups()
        {
            knownGroups = new HashSet<string>();
            foreach (var g in MyDefinitionManager.Static.GetSpawnGroupDefinitions())
                if (g != null)
                    knownGroups.Add(g.Id.SubtypeName);
        }

        // qa F6: if most grids carrying MES spawn data decode to nothing or to an unknown spawn group, MES has probably changed the format.
        private bool NpcDataBroken()
        {
            return unreadable.Count >= 3 && unreadable.Count * 2 >= keyedGrids;
        }

        private string NpcDataBrokenText()
        {
            var sb = new StringBuilder();
            sb.Append(unreadable.Count).Append(" of ").Append(keyedGrids).Append(" NPC grids have MES spawn data this script can't use (");
            int undecoded = unreadable.Count - unknownGroupGrids;
            if (undecoded > 0)
                sb.Append(undecoded).Append(" unreadable: MES format changed?").Append(unknownGroupGrids > 0 ? "; " : "");
            if (unknownGroupGrids > 0)
            {
                var names = new List<string>(unknownGroupNames);
                sb.Append(unknownGroupGrids).Append(" name spawn groups that no longer exist, e.g. ").Append(string.Join(", ", names.GetRange(0, Math.Min(3, names.Count))))
                    .Append(": renamed or removed in a mod update?");
            }
            sb.Append("), so no site can be identified safely.");
            return sb.ToString();
        }

        // qa D2: no grid carries MES spawn data under the key this script knows, yet NPC-owned grids exist: MES has probably changed its
        // storage key (Spawning/Manipulation/StorageTools.cs), so every site would read NOT FOUND and its own grids would look like debris.
        private bool NpcKeyMissing()
        {
            return keyedGrids == 0 && unkeyedNpcGrids >= 3;
        }

        private string NpcKeyMissingText()
        {
            return "no grid in the world carries MES spawn data under the key this script reads, but NPC-owned grids exist (MES storage key changed?), so NPC debris can't be told apart from the sites.";
        }

        private NpcDataLite ReadNpcData(IMyCubeGrid grid)
        {
            string text;
            if (!grid.Storage.TryGetValue(NpcDataKey, out text) || string.IsNullOrEmpty(text))
                return null;
            try
            {
                return MyAPIGateway.Utilities.SerializeFromBinary<NpcDataLite>(Convert.FromBase64String(text));
            }
            catch (Exception e)
            {
                if (!decodeErrorLogged)
                {
                    decodeErrorLogged = true;
                    Log("could not read MES NpcData (MES format changed?): " + e.Message);
                }
                return null;
            }
        }

        private SiteScan ScanSite(RefreshSite site)
        {
            var scan = new SiteScan { Site = site };
            var siteIds = new HashSet<long>();
            bool haveAnchor = false;

            foreach (var g in allGrids)
            {
                NpcDataLite data;
                if (!npcData.TryGetValue(g.EntityId, out data) || data.SpawnGroupName != site.Group)
                    continue;
                // MES can write spawn data to every grid of one prefab (PrefabSpawner.GridsSpawned): count attached grids once.
                if (!siteIds.Contains(g.EntityId))
                    scan.MainGrids++;
                if (!haveAnchor && data.StartCoords.LengthSquared() > 1
                    && Vector3D.Distance(data.StartCoords, g.PositionComp.WorldAABB.Center) < StartCoordsTolerance)
                {
                    scan.Anchor = data.StartCoords;
                    haveAnchor = true;
                }
                groupBuffer.Clear();
                MyAPIGateway.GridGroups.GetGroup(g, GridLinkTypeEnum.Mechanical, groupBuffer);
                if (groupBuffer.Count == 0)
                    groupBuffer.Add(g);
                foreach (var m in groupBuffer)
                {
                    if (!siteIds.Add(m.EntityId))
                        continue;
                    scan.Grids.Add(m);
                    var box = m.PositionComp.WorldAABB;
                    if (!scan.HasFootprint)
                    {
                        scan.Footprint = box;
                        scan.HasFootprint = true;
                    }
                    else
                    {
                        scan.Footprint = scan.Footprint.Include(ref box);
                    }
                }
                groupBuffer.Clear();
            }

            if (!haveAnchor && scan.HasFootprint)
                scan.Anchor = scan.Footprint.Center;
            else if (!haveAnchor && !ComputeAnchor(site, out scan.Anchor))
            {
                scan.NoAnchor = true;
                return scan;
            }
            if (scan.HasFootprint)
                scan.Footprint = scan.Footprint.Inflate(FootprintMargin);

            // A site grid that is not NPC-only, or is attached and carries another encounter's (or unreadable) spawn data, stops the site.
            foreach (var g in scan.Grids)
            {
                NpcDataLite data;
                string reason = NonNpcReason(g, true);
                if (reason == null && unreadable.Contains(g.EntityId))
                    reason = "unreadable MES spawn data";
                if (reason == null && npcData.TryGetValue(g.EntityId, out data) && data.SpawnGroupName != site.Group)
                    reason = "belongs to " + data.SpawnGroupName;
                if (reason != null)
                {
                    scan.PlayerPiece = "'" + GridName(g) + "' attached to the site: " + reason;
                    break;
                }
            }
            if (site.Owned && scan.Grids.Count > 0)
                scan.Owner = OwnerOf(scan.Grids);

            foreach (var g in allGrids)
            {
                if (siteIds.Contains(g.EntityId))
                    continue;
                var box = g.PositionComp.WorldAABB;
                var dist = Vector3D.Distance(box.Center, scan.Anchor);
                bool near = scan.HasFootprint ? dist < AnchorClearance || scan.Footprint.Intersects(box) : dist < NotFoundClearance;
                if (!near)
                    continue;
                if (IsNpcDebris(g))
                    scan.Debris.Add(g);
                else
                    scan.Blockers.Add(DescribeGrid(g, dist));
            }

            // Any non-bot character in the footprint, or in/near a debris piece force would delete (qa F2).
            foreach (var ch in characters)
            {
                var pos = ch.GetPosition();
                bool inside = scan.HasFootprint ? scan.Footprint.Contains(pos) != ContainmentType.Disjoint : Vector3D.Distance(pos, scan.Anchor) < NotFoundClearance;
                if (inside)
                {
                    scan.Blockers.Add("player " + ch.DisplayName);
                    continue;
                }
                foreach (var d in scan.Debris)
                {
                    if (d.PositionComp.WorldAABB.Inflate(FootprintMargin).Contains(pos) != ContainmentType.Disjoint)
                    {
                        scan.Blockers.Add("player " + ch.DisplayName + " at '" + GridName(d) + "'");
                        break;
                    }
                }
            }

            scan.AlsoSpawns = OtherPendingInRange(site, scan.Anchor);
            return scan;
        }

        // qa F9: other WitM sites with no live grid whose trigger radius covers this anchor; ProcessStaticEncountersAtLocation spawns them too
        // (API/LocalAPI.cs:429-440, Spawning/StaticEncounter.cs:371-377), if MES has them pending.
        private string OtherPendingInRange(RefreshSite site, Vector3D anchor)
        {
            string list = "";
            foreach (var s in RefreshSites.All)
            {
                if (s == site || liveGroups.Contains(s.Group))
                    continue;
                Vector3D other;
                if (ComputeAnchor(s, out other) && Vector3D.Distance(other, anchor) < s.TriggerRadiusKm * 1000)
                    list += (list.Length > 0 ? ", " : "") + s.Label;
            }
            return list;
        }

        // A grid without MES spawn data, owned by NPCs only, nothing player-built or player-occupied, and no attached grid that fails the same
        // test or carries spawn data (split-off pieces, hand-placed copies).
        private bool IsNpcDebris(IMyCubeGrid grid)
        {
            if (!IsCleanNpcPiece(grid))
                return false;
            groupBuffer.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, groupBuffer);
            bool clean = true;
            foreach (var m in groupBuffer)
            {
                if (m != grid && !IsCleanNpcPiece(m))
                {
                    clean = false;
                    break;
                }
            }
            groupBuffer.Clear();
            return clean;
        }

        private bool IsCleanNpcPiece(IMyCubeGrid grid)
        {
            if (grid.Storage != null && grid.Storage.ContainsKey(NpcDataKey))
                return false;
            if (grid.BigOwners.Count + grid.SmallOwners.Count == 0)
                return false;
            return NonNpcReason(grid, true) == null;
        }

        // Why a grid is not purely NPC property, or null. qa F1: both ownership lists (MES reads both: EntityEvaluator.cs:243-268,
        // OwnershipHelper.cs:52-55); an owner counts as NPC only if it has no SteamId and its faction is all-NPC (MES classifies NPC factions by
        // IsEveryoneNpc, SpawnGroupManager.cs:720). With blocks: also a block built by a player or a seat with a non-bot character in it.
        private string NonNpcReason(IMyCubeGrid grid, bool blocks)
        {
            foreach (var o in grid.BigOwners)
                if (o != 0 && !IsNpcIdentity(o))
                    return "owned by " + IdentityName(o);
            foreach (var o in grid.SmallOwners)
                if (o != 0 && !IsNpcIdentity(o))
                    return "block owned by " + IdentityName(o);
            if (!blocks)
                return null;
            string reason = null;
            blockBuffer.Clear();
            grid.GetBlocks(blockBuffer);
            foreach (var b in blockBuffer)
            {
                if (b.BuiltBy != 0 && MyAPIGateway.Players.TryGetSteamId(b.BuiltBy) != 0)
                {
                    reason = "block built by " + IdentityName(b.BuiltBy);
                    break;
                }
                var seat = b.FatBlock as IMyShipController;
                if (seat != null && seat.Pilot != null && !seat.Pilot.IsBot)
                {
                    reason = "occupied by " + seat.Pilot.DisplayName;
                    break;
                }
            }
            blockBuffer.Clear();
            return reason;
        }

        private static bool IsNpcIdentity(long id)
        {
            if (MyAPIGateway.Players.TryGetSteamId(id) != 0)
                return false;
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(id);
            return faction != null && faction.IsEveryoneNpc();
        }

        private static string IdentityName(long id)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(id);
            var steam = MyAPIGateway.Players.TryGetSteamId(id);
            return (steam != 0 ? "player " : "identity ") + id + (faction != null ? " [" + faction.Tag + "]" : "");
        }

        private string DescribeGrid(IMyCubeGrid grid, double dist)
        {
            string kind;
            NpcDataLite data;
            var reason = NonNpcReason(grid, false);
            if (reason != null)
                kind = reason;
            else if (npcData.TryGetValue(grid.EntityId, out data))
                kind = "NPC " + data.SpawnGroupName;
            else if (unreadable.Contains(grid.EntityId))
                kind = "NPC, unreadable spawn data";
            else if (grid.BigOwners.Count + grid.SmallOwners.Count == 0)
                kind = "no owner";
            else
                kind = NpcPieceReason(grid);
            return "'" + GridName(grid) + "' (" + kind + ", " + (int)dist + " m)";
        }

        // qa D4: why an NPC-owned grid without spawn data is not debris.
        private string NpcPieceReason(IMyCubeGrid grid)
        {
            var own = NonNpcReason(grid, true);
            if (own != null)
                return "NPC-owned, " + own;
            string reason = "NPC-owned, attached to an unowned or non-NPC grid";
            groupBuffer.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, groupBuffer);
            foreach (var m in groupBuffer)
            {
                NpcDataLite data;
                if (m == grid)
                    continue;
                if (npcData.TryGetValue(m.EntityId, out data))
                {
                    reason = "part of NPC " + data.SpawnGroupName;
                    break;
                }
                if (unreadable.Contains(m.EntityId))
                {
                    reason = "part of an NPC grid with unusable spawn data";
                    break;
                }
            }
            groupBuffer.Clear();
            return reason;
        }

        private static string GridName(IMyCubeGrid grid)
        {
            return string.IsNullOrEmpty(grid.CustomName) ? grid.EntityId.ToString() : grid.CustomName;
        }

        private static string Names(List<IMyCubeGrid> grids)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < grids.Count && i < 5; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(GridName(grids[i]));
            }
            if (grids.Count > 5)
                sb.Append(", ...");
            return sb.ToString();
        }

        private bool AllClosed()
        {
            foreach (var g in current.Grids)
                if (!g.Closed) return false;
            if (force)
                foreach (var g in current.Debris)
                    if (!g.Closed) return false;
            return true;
        }

        private bool AnyClosed()
        {
            foreach (var g in current.Grids)
                if (g.Closed || g.MarkedForClose) return true;
            return false;
        }

        private IMyCubeGrid FindNewGrid()
        {
            ScanWorld(NewGridRadius, current.Anchor);
            foreach (var g in allGrids)
            {
                if (oldIds.Contains(g.EntityId))
                    continue;
                NpcDataLite data;
                if (npcData.TryGetValue(g.EntityId, out data) && data.SpawnGroupName == current.Site.Group)
                    return g;
            }
            return null;
        }

        // Owner of the site's "Airfield Block" / "Depot Block" (the blocks the ownership flip actions change): Gray, Green, Unowned, or "" if none.
        private string OwnerOf(List<IMyCubeGrid> grids)
        {
            foreach (var g in grids)
            {
                blockBuffer.Clear();
                g.GetBlocks(blockBuffer, IsAirfieldBlock);
                if (blockBuffer.Count == 0)
                    continue;
                var owner = blockBuffer[0].OwnerId;
                blockBuffer.Clear();
                return FactionName(owner);
            }
            return "";
        }

        private string OwnerOfGrid(long gridId)
        {
            var grid = MyAPIGateway.Entities.GetEntityById(gridId) as IMyCubeGrid;
            if (grid == null)
                return "";
            var grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, grids);
            if (grids.Count == 0)
                grids.Add(grid);
            return OwnerOf(grids);
        }

        private static bool IsAirfieldBlock(IMySlimBlock block)
        {
            var terminal = block.FatBlock as IMyTerminalBlock;
            return terminal != null && (terminal.CustomName == AirfieldBlockName || terminal.CustomName == DepotBlockName);
        }

        private static string FactionName(long owner)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
            if (faction == null)
                return owner == 0 ? "nobody" : "?";
            if (faction.Tag == "GRAY") return "Gray";
            if (faction.Tag == "GREEN") return "Green";
            if (faction.Tag == "UNOWNED") return "Unowned";
            return faction.Tag;
        }

        private static string Show(string owner)
        {
            return string.IsNullOrEmpty(owner) ? "unknown" : owner;
        }

        // Where MES places the site when the grid's own NpcData has no start coordinates: StaticEncounter.InitStaticEncounter
        // (surface under Dir, then Dir * Altitude). Uses the terrain surface; MES uses the water surface on water planets.
        // False if no planet is found (qa D1). Cached per run (qa D5): cleared at each dry run and run start.
        private bool ComputeAnchor(RefreshSite site, out Vector3D anchor)
        {
            if (site.Depot)
            {
                anchor = site.Coords;
                return true;
            }
            if (bestPlanet == null || bestPlanet.MarkedForClose)
            {
                bestPlanet = null;
                anchorCache.Clear();
                MyAPIGateway.Entities.GetEntities(null, CollectPlanet);
            }
            if (bestPlanet == null)
            {
                anchor = Vector3D.Zero;
                return false;
            }
            if (anchorCache.TryGetValue(site, out anchor))
                return true;
            var centre = bestPlanet.PositionComp.WorldAABB.Center;
            var surface = bestPlanet.GetClosestSurfacePointGlobal(centre + site.Dir * 10000);
            anchor = surface + site.Dir * site.Altitude;
            anchorCache[site] = anchor;
            return true;
        }

        private bool CollectPlanet(IMyEntity e)
        {
            var planet = e as MyPlanet;
            if (planet != null && (bestPlanet == null || planet.MaximumRadius > bestPlanet.MaximumRadius))
                bestPlanet = planet;
            return false;
        }

        // War Level announcements are once per level (WW2-WarLevel{N}Announced, War-Level-Points.sbc). Worlds from before that
        // change have no flags yet: mark the levels already reached so a refreshed Civitavecchia does not announce them again.
        private static void SeedWarLevelAnnounce()
        {
            int level;
            if (!MyAPIGateway.Utilities.GetVariable("WarLevel", out level))
                level = 1;
            for (int n = 2; n <= 5; n++)
            {
                var name = "WW2-WarLevel" + n + "Announced";
                bool value;
                if (!MyAPIGateway.Utilities.GetVariable(name, out value))
                    MyAPIGateway.Utilities.SetVariable(name, n <= level);
            }
        }

        // ---------------- helpers ----------------

        // qa F3: an unfinished run's Done list only counts within 12 h of its start; later 'go's redo everything.
        private bool RecentRun()
        {
            var age = DateTime.UtcNow.Ticks - state.StartedUtcTicks;
            return state.StartedUtcTicks > 0 && age >= 0 && age < ResumeWindowTicks;
        }

        private string HoursAgo()
        {
            return Math.Round((DateTime.UtcNow.Ticks - state.StartedUtcTicks) / (double)TimeSpan.TicksPerHour, 1).ToString();
        }

        private static MesApiLite Mes()
        {
            var news = WarNewsSession.Instance;
            return news != null ? news.Mes : null;
        }

        private IMyPlayer FindPlayer(ulong steamId)
        {
            RefreshPlayers();
            IMyPlayer found = null;
            foreach (var p in players)
            {
                if (p.SteamUserId == steamId)
                {
                    found = p;
                    break;
                }
            }
            players.Clear();
            if (found == null && MyAPIGateway.Session.Player != null && MyAPIGateway.Session.Player.SteamUserId == steamId)
                found = MyAPIGateway.Session.Player;
            return found;
        }

        private void RefreshPlayers()
        {
            players.Clear();
            MyAPIGateway.Players.GetPlayers(players, IsHuman);
        }

        private static bool IsHuman(IMyPlayer p)
        {
            return p != null && !p.IsBot && p.SteamUserId != 0;
        }

        private static bool Is(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private void LoadState()
        {
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable(StateVariable, out text) || string.IsNullOrEmpty(text))
                    return;
                var loaded = MyAPIGateway.Utilities.SerializeFromBinary<RefreshState>(Convert.FromBase64String(text));
                if (loaded != null)
                    state = loaded;
                if (state.Pending == null)
                    state.Pending = new List<string>();
                if (state.Done == null)
                    state.Done = new List<string>();
                if (state.Lines == null)
                    state.Lines = new List<string>();
                if (state.AwaitReload == null)
                    state.AwaitReload = new List<string>();
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
                MyAPIGateway.Utilities.SetVariable(StateVariable, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(state)));
            }
            catch (Exception e)
            {
                Log("save: " + e);
            }
        }

        private static void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("WW2-WitM Refresh: " + msg);
        }
    }
}
