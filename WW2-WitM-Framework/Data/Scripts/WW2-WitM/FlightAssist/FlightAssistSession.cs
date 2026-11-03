using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.ModAPI;
using VRage.Utils;

namespace WW2WitM.FlightAssist
{
    // Flight Assist for player planes: runs on the server (and in single player); see audits/mes-2026-10-11-flight-assist-mod-applied.md.
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class FlightAssistSession : MySessionComponentBase
    {
        const ushort NetChannel = 47263;
        const byte PacketCommand = 1;
        const byte PacketReply = 2;
        const byte CmdStatus = 0, CmdOn = 1, CmdOff = 2, CmdToggle = 3, CmdLog = 4, CmdServerOn = 5, CmdServerOff = 6;
        // Sandbox variables, saved with the world: pilots who turned it off (Steam IDs, comma-separated), and the admin switch.
        const string OffVariable = "WitM-FlightAssistOff";
        const string ServerOffVariable = "WitM-FlightAssistServerOff";
        const int ScanTicks = 10;
        const int CheckTicks = 120;
        const string AirCorePrefix = "WW2-Core-Air-";
        const string GroundCorePrefix = "WW2-Core-Ground-";
        const string NavalCorePrefix = "WW2-Core-Naval-";
        static readonly Guid MesNpcDataKey = new Guid("29FFD684-13D7-4045-BF76-CD48BF80E36A");
        static readonly Guid MesBehaviorKey = new Guid("FF814A67-AEC3-4DF0-ADC4-A9B239FA954F");

        bool isServer;
        bool chatHooked;
        bool loadCleanupDone;
        bool serverOff;
        int tick;
        int errors;

        readonly List<Plane> planes = new List<Plane>();
        readonly Dictionary<long, Plane> planeByGrid = new Dictionary<long, Plane>();
        readonly HashSet<ulong> assistOff = new HashSet<ulong>();
        readonly HashSet<ulong> logOn = new HashSet<ulong>();
        readonly List<IMyPlayer> players = new List<IMyPlayer>();
        readonly List<IMyCubeGrid> groupGrids = new List<IMyCubeGrid>();
        readonly List<IMySlimBlock> blockScratch = new List<IMySlimBlock>();
        readonly Queue<IMyCubeGrid> addedGrids = new Queue<IMyCubeGrid>();
        readonly MyIni ini = new MyIni();
        readonly Dictionary<long, AssistSettings> settingsCache = new Dictionary<long, AssistSettings>();
        readonly Dictionary<long, string> settingsText = new Dictionary<long, string>();

        public override void LoadData()
        {
            isServer = MyAPIGateway.Multiplayer.IsServer;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetChannel, OnMessage);
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.MessageEnteredSender += OnChat;
                chatHooked = true;
            }
            if (isServer)
                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
        }

        protected override void UnloadData()
        {
            try
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetChannel, OnMessage);
                if (chatHooked)
                    MyAPIGateway.Utilities.MessageEnteredSender -= OnChat;
                if (isServer)
                {
                    MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
                    foreach (var p in planes)
                        p.Release();
                }
            }
            catch (Exception e)
            {
                Log("unload: " + e);
            }
            planes.Clear();
            planeByGrid.Clear();
        }

        public override void UpdateBeforeSimulation()
        {
            if (!isServer)
                return;
            try
            {
                tick++;
                if (!loadCleanupDone)
                {
                    loadCleanupDone = true;
                    LoadChoices();
                    var all = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(all, e => e is IMyCubeGrid);
                    foreach (var e in all)
                        addedGrids.Enqueue((IMyCubeGrid)e);
                }
                if (addedGrids.Count > 0)
                    CleanupAddedGrids();

                if (tick % ScanTicks == 0)
                    ScanPilots();

                for (int i = planes.Count - 1; i >= 0; i--)
                {
                    var p = planes[i];
                    if (!p.Tick(!serverOff && !assistOff.Contains(p.PilotSteamId), logOn.Contains(p.PilotSteamId)))
                        DropAt(i);
                }
            }
            catch (Exception e)
            {
                if (++errors <= 5)
                    Log("update: " + e);
                foreach (var p in planes)
                {
                    try { p.Release(); } catch { }
                }
                planes.Clear();
                planeByGrid.Clear();
            }
        }

        void DropAt(int i)
        {
            var p = planes[i];
            p.Release();
            planeByGrid.Remove(p.Grid.EntityId);
            if (p.Controller != null)
            {
                settingsCache.Remove(p.Controller.EntityId);
                settingsText.Remove(p.Controller.EntityId);
            }
            planes.RemoveAtFast(i);
        }

        void ScanPilots()
        {
            foreach (var p in planes)
                p.SeenThisScan = false;

            players.Clear();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var player in players)
            {
                if (player == null || player.IsBot || player.SteamUserId == 0 || player.Controller == null)
                    continue;
                var controller = player.Controller.ControlledEntity as IMyShipController;
                if (controller == null || controller.CubeGrid == null || !controller.IsUnderControl || !controller.CanControlShip)
                    continue;
                var grid = controller.CubeGrid;
                // Only the seat the engine lets fly the grid group; a gunner or co-pilot seat never becomes the pilot.
                var flying = grid.ControlSystem != null && grid.ControlSystem.CurrentShipController != null
                    ? grid.ControlSystem.CurrentShipController.Entity as IMyShipController : null;
                if (flying != null && flying.EntityId != controller.EntityId)
                    continue;
                Plane plane;
                if (!planeByGrid.TryGetValue(grid.EntityId, out plane))
                {
                    plane = new Plane { Grid = grid, Controller = controller, PilotSteamId = player.SteamUserId };
                    planeByGrid[grid.EntityId] = plane;
                    planes.Add(plane);
                    Check(plane);
                }
                else if (plane.SeenThisScan)
                {
                    continue;
                }
                else if (plane.Controller != controller || plane.PilotSteamId != player.SteamUserId)
                {
                    plane.Release();
                    plane.Controller = controller;
                    plane.PilotSteamId = player.SteamUserId;
                    Check(plane);
                }
                else if (tick >= plane.NextCheckTick)
                {
                    Check(plane);
                }
                plane.SeenThisScan = true;
            }
            players.Clear();

            for (int i = planes.Count - 1; i >= 0; i--)
                if (!planes[i].SeenThisScan)
                    DropAt(i);
        }

        // Opt-in: an air core on the grid group, or a [Flight Assist] section in the cockpit; never ground/naval cores or NPC grids.
        void Check(Plane plane)
        {
            plane.NextCheckTick = tick + CheckTicks;
            var grid = plane.Grid;
            // MES's NpcData tag travels in blueprints and Player prefabs, so ownership and an active MES behavior decide, not that tag.
            bool air = false, surface = false, mes = false;

            groupGrids.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, groupGrids);
            if (groupGrids.Count == 0)
                groupGrids.Add(grid);
            if (!plane.Engaged)
                plane.Gyros.Clear();
            foreach (var g in groupGrids)
            {
                blockScratch.Clear();
                g.GetBlocks(blockScratch, b => b.FatBlock != null);
                foreach (var slim in blockScratch)
                {
                    var block = slim.FatBlock;
                    string subtype = block.BlockDefinition.SubtypeId;
                    if (subtype.StartsWith(AirCorePrefix, StringComparison.Ordinal))
                        air = true;
                    else if (subtype.StartsWith(GroundCorePrefix, StringComparison.Ordinal) || subtype.StartsWith(NavalCorePrefix, StringComparison.Ordinal))
                        surface = true;
                    var rc = block as IMyRemoteControl;
                    string behavior;
                    if (rc != null && rc.Storage != null && rc.Storage.TryGetValue(MesBehaviorKey, out behavior) && !string.IsNullOrEmpty(behavior))
                        mes = true;
                    var gyro = block as IMyGyro;
                    if (gyro != null && g == grid && !plane.Engaged)
                        plane.Gyros.Add(gyro);
                }
            }
            blockScratch.Clear();
            groupGrids.Clear();

            bool section = ReadSettings(plane);
            bool npcOwned = false;
            foreach (var owner in grid.BigOwners)
                if (owner != 0 && MyAPIGateway.Players.TryGetSteamId(owner) == 0)
                    npcOwned = true;

            if (surface) { plane.Eligible = false; plane.Reason = "not a plane (ground or naval core)"; }
            else if (mes || npcOwned) { plane.Eligible = false; plane.Reason = "NPC grid (MES autopilot)"; }
            else if (!air && !section) { plane.Eligible = false; plane.Reason = "no air core and no [Flight Assist] section in the cockpit"; }
            else if (!plane.Settings.Enabled) { plane.Eligible = false; plane.Reason = "Enabled=false in the cockpit's [Flight Assist] section"; }
            else if (plane.Gyros.Count == 0) { plane.Eligible = false; plane.Reason = "no gyro on the plane's main grid"; }
            else { plane.Eligible = true; plane.Reason = air ? "air core" : "[Flight Assist] section"; }
            if (!plane.Eligible && plane.Engaged)
                plane.Release();
        }

        bool ReadSettings(Plane plane)
        {
            var c = plane.Controller;
            string text = c.CustomData ?? "";
            AssistSettings cached;
            string old;
            if (settingsCache.TryGetValue(c.EntityId, out cached) && settingsText.TryGetValue(c.EntityId, out old) && string.Equals(old, text))
            {
                plane.Settings = cached ?? AssistSettings.Default;
                return cached != null;
            }
            AssistSettings parsed = null;
            if (text.IndexOf("[" + AssistSettings.Section + "]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ini.Clear();
                if (ini.TryParse(text) && ini.ContainsSection(AssistSettings.Section))
                    parsed = AssistSettings.Build(ini);
                ini.Clear();
            }
            settingsCache[c.EntityId] = parsed;
            settingsText[c.EntityId] = text;
            plane.Settings = parsed ?? AssistSettings.Default;
            return parsed != null;
        }

        // Clears gyro override left in a saved or pasted player plane (the save happens with the assist engaged).
        void OnEntityAdd(IMyEntity entity)
        {
            var grid = entity as IMyCubeGrid;
            if (grid != null)
                addedGrids.Enqueue(grid);
        }

        void CleanupAddedGrids()
        {
            int budget = 20;
            while (addedGrids.Count > 0 && budget-- > 0)
            {
                var grid = addedGrids.Dequeue();
                if (grid == null || grid.Closed || grid.MarkedForClose || grid.Physics == null || planeByGrid.ContainsKey(grid.EntityId))
                    continue;
                if (grid.Storage != null && grid.Storage.ContainsKey(MesNpcDataKey))
                    continue;
                bool npc = false;
                foreach (var owner in grid.BigOwners)
                    if (owner != 0 && MyAPIGateway.Players.TryGetSteamId(owner) == 0)
                        npc = true;
                if (npc)
                    continue;
                bool air = false, overridden = false;
                blockScratch.Clear();
                grid.GetBlocks(blockScratch, b => b.FatBlock != null);
                foreach (var slim in blockScratch)
                {
                    var block = slim.FatBlock;
                    if (block.BlockDefinition.SubtypeId.StartsWith(AirCorePrefix, StringComparison.Ordinal))
                        air = true;
                    var gyro = block as IMyGyro;
                    if (gyro != null && gyro.GyroOverride)
                        overridden = true;
                }
                if (air && overridden)
                {
                    foreach (var slim in blockScratch)
                        Plane.ReleaseGyro(slim.FatBlock as IMyGyro);
                    Log("cleared leftover gyro override on " + grid.DisplayName);
                }
                blockScratch.Clear();
            }
        }

        // ---- /assist command: each pilot turns it on or off for themselves; the choice is saved with the world. ----
        // An admin can turn it off for everyone with /assist server off.

        void OnChat(ulong sender, string text, ref bool sendToOthers)
        {
            try
            {
                if (text == null)
                    return;
                var words = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                    return;
                string head = words[0].ToLowerInvariant();
                if (head != "/assist" && head != "/flightassist")
                    return;
                sendToOthers = false;
                string arg = words.Length > 1 ? words[1].ToLowerInvariant() : "";
                byte cmd;
                if (arg == "") cmd = CmdToggle;
                else if (arg == "on") cmd = CmdOn;
                else if (arg == "off") cmd = CmdOff;
                else if (arg == "status") cmd = CmdStatus;
                else if (arg == "log") cmd = CmdLog;
                else if (arg == "server" && words.Length > 2 && words[2].ToLowerInvariant() == "on") cmd = CmdServerOn;
                else if (arg == "server" && words.Length > 2 && words[2].ToLowerInvariant() == "off") cmd = CmdServerOff;
                else
                {
                    Show("/assist toggles Flight Assist for you. /assist on, /assist off, /assist status, /assist log (flight log above 100 m/s to the game log). Admins: /assist server on or off (for everyone).");
                    return;
                }
                if (isServer)
                {
                    var local = MyAPIGateway.Session.Player;
                    Show(Command(local != null ? local.SteamUserId : sender, cmd));
                    return;
                }
                MyAPIGateway.Multiplayer.SendMessageToServer(NetChannel, new byte[] { PacketCommand, cmd });
            }
            catch (Exception e)
            {
                Log("chat: " + e);
            }
        }

        void OnMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            try
            {
                if (data == null || data.Length < 1)
                    return;
                if (data[0] == PacketCommand && isServer && !fromServer && data.Length == 2 && data[1] <= CmdServerOff)
                {
                    string reply = Command(sender, data[1]);
                    var bytes = Encoding.UTF8.GetBytes(reply);
                    var packet = new byte[bytes.Length + 1];
                    packet[0] = PacketReply;
                    Array.Copy(bytes, 0, packet, 1, bytes.Length);
                    MyAPIGateway.Multiplayer.SendMessageTo(NetChannel, packet, sender);
                }
                else if (data[0] == PacketReply && fromServer && !isServer)
                {
                    Show(Encoding.UTF8.GetString(data, 1, data.Length - 1));
                }
            }
            catch (Exception e)
            {
                Log("message: " + e);
            }
        }

        string Command(ulong steamId, byte cmd)
        {
            if (steamId == 0)
                return "Flight Assist: no player.";
            if (cmd == CmdServerOn || cmd == CmdServerOff)
            {
                if (MyAPIGateway.Session.GetUserPromoteLevel(steamId) < MyPromoteLevel.Admin)
                    return "Only an admin can turn Flight Assist on or off for everyone.";
                serverOff = cmd == CmdServerOff;
                MyAPIGateway.Utilities.SetVariable(ServerOffVariable, serverOff);
                Log("admin " + steamId + " turned it " + (serverOff ? "off" : "on") + " for everyone");
                return "Flight Assist is " + (serverOff ? "OFF for everyone (each pilot's own choice is kept for when it is back on)." : "ON again (each pilot's own choice applies).");
            }
            bool wasOff = assistOff.Contains(steamId);
            if (cmd == CmdOn) assistOff.Remove(steamId);
            else if (cmd == CmdOff) assistOff.Add(steamId);
            else if (cmd == CmdToggle)
            {
                if (!assistOff.Remove(steamId))
                    assistOff.Add(steamId);
            }
            if (cmd == CmdLog)
            {
                bool on = logOn.Add(steamId);
                if (!on)
                    logOn.Remove(steamId);
                WW2WitM.Aerodynamics.Wing.TrackLift = logOn.Count > 0;
                if (logOn.Count == 0)
                    WW2WitM.Aerodynamics.Wing.GridLift.Clear();
                Log("player " + steamId + " turned the flight log " + (on ? "on" : "off"));
                return "Flight log " + (on ? "ON: every 0.1 s above 100 m/s to the game log (SpaceEngineers_*.log). /assist log again to stop." : "OFF.");
            }
            if (cmd != CmdStatus)
                Log("player " + steamId + " turned it " + (assistOff.Contains(steamId) ? "off" : "on"));
            if (wasOff != assistOff.Contains(steamId))
                SaveChoices();

            var sb = new StringBuilder();
            sb.Append("Flight Assist is ").Append(assistOff.Contains(steamId) ? "OFF" : "ON").Append(" for you.");
            if (serverOff)
                sb.Append(" An admin has turned it off for everyone.");
            foreach (var p in planes)
            {
                if (p.PilotSteamId != steamId)
                    continue;
                sb.Append(" This plane: ");
                if (!p.Eligible) sb.Append("not assisted, ").Append(p.Reason).Append('.');
                else if (p.Engaged) sb.Append("assisting (").Append(p.Reason).Append(").");
                else sb.Append("ready, engages above ").Append(p.Settings.MinSpeed.ToString("0")).Append(" m/s (").Append(p.Reason).Append(").");
                if (cmd == CmdStatus)
                    sb.Append(p.Stats.Report());
                break;
            }
            return sb.ToString();
        }

        void LoadChoices()
        {
            MyAPIGateway.Utilities.GetVariable(ServerOffVariable, out serverOff);
            string text;
            if (!MyAPIGateway.Utilities.GetVariable(OffVariable, out text) || string.IsNullOrEmpty(text))
                return;
            foreach (var part in text.Split(','))
            {
                ulong id;
                if (ulong.TryParse(part, out id) && id != 0)
                    assistOff.Add(id);
            }
        }

        void SaveChoices()
        {
            MyAPIGateway.Utilities.SetVariable(OffVariable, string.Join(",", assistOff));
        }

        static void Show(string text)
        {
            MyAPIGateway.Utilities.ShowMessage("Flight Assist", text);
        }

        static void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("WW2-WitM Flight Assist: " + msg);
        }
    }
}
