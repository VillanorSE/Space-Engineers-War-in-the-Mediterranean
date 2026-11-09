using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using WW2WitM.WarNewsSystem;

namespace WW2WitM.Supply
{
    // Disabled NPC ground vehicles stop (owner 2026-11-07). Every MES-spawned NPC-WW2-* grid with suspension blocks is watched:
    // it is disabled once none of its hydrogen engines is working (destroyed, damaged below functional, off or dry), and, for
    // wheeled vehicles (every suspension a "Wheel" subtype), once a suspension loses its wheel or is destroyed. A disabled
    // vehicle gets the MES command WW2_Ground_Disabled (WW2-Ground-Command-Disabled, 3 m radius from its own Remote Control,
    // same owner, so only that grid hears it); its Depot / Convoy trigger stops it for good (Passive, dampeners off; turrets
    // keep firing on their own). Its supply ledger entry is marked lost, so it is not refunded when MES removes it.
    // An engine counts only after it has been seen working once, so a grid that spawns without a working engine is left alone.
    public static class GroundDisable
    {
        private const int CheckTicks = 120;          // 2 s
        private const int ScanTicks = 1800;          // 30 s: look for new ground vehicles
        private const double SettleSeconds = 10;     // ignore a grid this long after first seen (blocks still initialising)
        private const double ResendSeconds = 10;
        private const int MaxSends = 3;
        private const string CommandProfile = "WW2-Ground-Command-Disabled";
        private const string PrefabPrefix = "NPC-WW2-";

        private class Watched
        {
            public double FirstSeen;
            public bool Wheeled;
            public int Wheels;          // suspensions with a wheel attached when first seen
            public bool EngineSeen;
            public bool Disabled;
            public int Sends;
            public double LastSend;
        }

        private static readonly Dictionary<long, Watched> watched = new Dictionary<long, Watched>();
        private static readonly List<long> drop = new List<long>();
        private static readonly HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        private static readonly List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        private static readonly List<string> commandIds = new List<string> { CommandProfile };

        public static void Unload()
        {
            watched.Clear();
            entities.Clear();
            blocks.Clear();
        }

        public static void Update(int tick)
        {
            if (tick % ScanTicks == 0)
                Scan();
            if (tick % CheckTicks == 0)
                Check();
        }

        private static void Scan()
        {
            entities.Clear();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);
            var now = WarNewsSession.Now();
            foreach (var e in entities)
            {
                var grid = (IMyCubeGrid)e;
                if (watched.ContainsKey(grid.EntityId) || grid.MarkedForClose || grid.Closed || grid.IsStatic)
                    continue;
                var prefab = SupplySites.PrefabOf(grid);
                if (prefab == null || !prefab.StartsWith(PrefabPrefix, StringComparison.Ordinal))
                    continue;
                int suspensions = 0, wheels = 0;
                bool allWheel = true;
                blocks.Clear();
                grid.GetBlocks(blocks, b => b.FatBlock is IMyMotorSuspension);
                foreach (var b in blocks)
                {
                    var s = (IMyMotorSuspension)b.FatBlock;
                    suspensions++;
                    if (s.IsAttached)
                        wheels++;
                    if (s.BlockDefinition.SubtypeId.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) < 0)
                        allWheel = false;
                }
                if (suspensions == 0)
                    continue;
                watched[grid.EntityId] = new Watched { FirstSeen = now, Wheeled = allWheel, Wheels = wheels };
            }
            entities.Clear();
        }

        private static void Check()
        {
            var now = WarNewsSession.Now();
            drop.Clear();
            foreach (var kv in watched)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(kv.Key) as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Closed)
                {
                    drop.Add(kv.Key);
                    continue;
                }
                var w = kv.Value;
                if (w.Disabled)
                {
                    if (w.Sends < MaxSends && now - w.LastSend >= ResendSeconds)
                        Send(grid, w, now);
                    continue;
                }
                if (now - w.FirstSeen < SettleSeconds)
                    continue;

                int engines = 0, wheels = 0;
                IMyRemoteControl rc = null;
                blocks.Clear();
                grid.GetBlocks(blocks, b => b.FatBlock != null);
                foreach (var b in blocks)
                {
                    var fat = b.FatBlock;
                    if (fat is IMyMotorSuspension)
                    {
                        var s = (IMyMotorSuspension)fat;
                        if (s.IsFunctional && s.IsAttached)
                            wheels++;
                    }
                    else if (fat.BlockDefinition.TypeIdString == "MyObjectBuilder_HydrogenEngine")
                    {
                        if (fat.IsFunctional && fat.IsWorking)
                            engines++;
                    }
                    else if (rc == null && fat is IMyRemoteControl)
                        rc = (IMyRemoteControl)fat;
                }
                blocks.Clear();
                if (engines > 0)
                    w.EngineSeen = true;

                string why = null;
                if (w.EngineSeen && engines == 0)
                    why = "no working engine";
                else if (w.Wheeled && wheels < w.Wheels)
                    why = "lost a wheel (" + wheels + " of " + w.Wheels + ")";
                if (why == null)
                    continue;

                w.Disabled = true;
                MarkLost(grid.EntityId);
                SupplySession.Log("ground vehicle " + (SupplySites.PrefabOf(grid) ?? grid.CustomName) + " disabled: " + why);
                if (rc != null)
                    Send(grid, w, now);
            }
            foreach (var id in drop)
                watched.Remove(id);
        }

        private static void Send(IMyCubeGrid grid, Watched w, double now)
        {
            w.Sends++;
            w.LastSend = now;
            var mes = SupplySession.Mes();
            if (mes == null || !mes.Ready)
                return;
            blocks.Clear();
            grid.GetBlocks(blocks, b => b.FatBlock is IMyRemoteControl);
            var rc = blocks.Count > 0 ? (IMyRemoteControl)blocks[0].FatBlock : null;
            blocks.Clear();
            if (rc == null)
                return;
            mes.SendBehaviorCommand(commandIds, rc.GetPosition(), "", -1, rc.OwnerId);
        }

        // Owner 2026-11-07: a disabled vehicle is lost, never refunded.
        private static void MarkLost(long entityId)
        {
            var session = SupplySession.Instance;
            if (session == null)
                return;
            foreach (var so in session.State.Sorties)
                if (so.EntityId == entityId)
                    so.Lost = true;
            foreach (var t in session.State.Trucks)
                if (t.EntityId == entityId)
                    t.Lost = true;
        }
    }
}
