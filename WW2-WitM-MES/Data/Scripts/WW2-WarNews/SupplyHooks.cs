using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using WW2WitM.WarNewsSystem;

namespace WW2WitM.Supply
{
    // Turns existing MES actions into supply changes (no SBC needed to observe them). Action ids are WitM-MES SubtypeIds:
    //   WW2-Action-Airfield-{From}-Become{To}                     Triggers-Airfield-Ownership.sbc
    //   WW2-Action-OffenseSpawn-{F}[-WL4]                          Triggers-Airfield-OffensiveSpawn.sbc (the -Pick action only relays)
    //   WW2-Action-DefenseSpawn-{F}                                Triggers-DefensiveSpawn.sbc (charged only from an airfield)
    //   WW2-Action-Airfield-AirCargoSpawn-{F}                      Triggers-Airfield-AirCargo.sbc
    //   WW2-Action-Crash-Air                                       Triggers-Crash-Air.sbc (marks a plane lost)
    //   WW2-Territory-Action-Delivery-{F}-{Origin}-{Destination}   Territory-Points.sbc (cargo ships)
    //   WW2-Territory-Action-Delivery-AirCargo-{F}                 Territory-Points.sbc
    //   WW2-Store-Action-UpdateSales-*                             Encounters/Store/*.sbc
    // MES queues behavior spawns and runs them later (BehaviorSpawnHelper), so a sortie action only opens a pending entry; the
    // next matching plane spawns near that airfield within 60 s are charged to it.
    public static class SupplyHooks
    {
        private const double PendingSeconds = 60;
        private const double MatchRange = 3500;

        private enum Kind { Offense, Defense, AirCargo }

        private class Pending
        {
            public string Site;
            public Kind Kind;
            public Vector3D Origin;
            public double Time;
            public string Group;
            public int Remaining;
        }

        private static readonly List<Pending> pending = new List<Pending>();
        private static readonly HashSet<long> registered = new HashSet<long>();
        private static readonly List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        private static readonly List<IMyCubeGrid> groupBuffer = new List<IMyCubeGrid>();
        private static readonly Func<IMySlimBlock, bool> isRemote = IsRemote;
        private static Action<IMyRemoteControl, string, string, IMyEntity, Vector3D> watcher;
        private static Action<IMyCubeGrid> spawnAction;
        private static Action<IMyCubeGrid, string> despawnAction;

        public static void Register(MesApiLite mes)
        {
            watcher = OnAction;
            spawnAction = OnSpawn;
            despawnAction = OnDespawn;
            mes.BehaviorTriggerActivationWatcher(true, watcher);
            mes.RegisterSuccessfulSpawnAction(spawnAction, true);
            Maintain();
        }

        public static void Unregister(MesApiLite mes)
        {
            if (mes != null)
            {
                if (watcher != null) mes.BehaviorTriggerActivationWatcher(false, watcher);
                if (spawnAction != null) mes.RegisterSuccessfulSpawnAction(spawnAction, false);
            }
            watcher = null;
            spawnAction = null;
            despawnAction = null;
            pending.Clear();
            registered.Clear();
        }

        // ---------------- MES actions ----------------

        private static void OnAction(IMyRemoteControl rc, string triggerId, string actionId, IMyEntity target, Vector3D waypoint)
        {
            try
            {
                if (rc == null || actionId == null || !actionId.StartsWith("WW2-", StringComparison.Ordinal))
                    return;
                var s = SupplySession.Instance;
                if (s == null)
                    return;
                string rest;

                if (Cut(actionId, "WW2-Store-Action-UpdateSales-", out rest))
                {
                    s.StoreAction(rc, actionId);
                    return;
                }
                if (actionId == "WW2-Action-Crash-Air")
                {
                    MarkLost(rc.CubeGrid);
                    return;
                }
                if (Cut(actionId, "WW2-Action-Airfield-", out rest))
                {
                    int b = rest.IndexOf("-Become", StringComparison.Ordinal);
                    if (b > 0)
                    {
                        var to = rest.Substring(b + 7);
                        if (to == "Gray" || to == "Green" || to == "Unowned")
                            s.OwnerChanged(SupplySites.SiteOf(rc.CubeGrid), to == "Unowned" ? "" : to);
                    }
                    else if (rest.StartsWith("AirCargoSpawn-", StringComparison.Ordinal))
                    {
                        AddPending(rc, Kind.AirCargo);
                    }
                    return;
                }
                if (Cut(actionId, "WW2-Action-OffenseSpawn-", out rest))
                {
                    if (!rest.EndsWith("-Pick", StringComparison.Ordinal))
                        AddPending(rc, Kind.Offense);
                    return;
                }
                if (Cut(actionId, "WW2-Action-DefenseSpawn-", out rest))
                {
                    if (rest == "Gray" || rest == "Green")
                        AddPending(rc, Kind.Defense);
                    return;
                }
                if (Cut(actionId, "WW2-Territory-Action-Delivery-", out rest))
                {
                    string faction;
                    if (Cut(rest, "AirCargo-", out faction))
                    {
                        s.AirCargoDelivered(rc.GetPosition(), faction);
                        return;
                    }
                    var parts = rest.Split('-');
                    if (parts.Length == 3)
                        s.ShipDelivered(parts[2]);
                }
            }
            catch (Exception e)
            {
                SupplySession.Log("action " + actionId + ": " + e);
            }
        }

        private static void AddPending(IMyRemoteControl rc, Kind kind)
        {
            var site = SupplySites.SiteOf(rc.CubeGrid);
            if (site.Length == 0 || SupplySites.IsPort(site))
                return;
            pending.Add(new Pending { Site = site, Kind = kind, Origin = rc.GetPosition(), Time = WarNewsSession.Now() });
        }

        // ---------------- spawns and despawns ----------------

        private static void OnSpawn(IMyCubeGrid grid)
        {
            try
            {
                if (pending.Count == 0 || grid == null)
                    return;
                Expire(WarNewsSession.Now());
                if (pending.Count == 0)
                    return;
                var group = SupplySites.SpawnGroupOf(grid);
                bool cargo = SupplySites.IsAirCargoGroup(group);
                if (!cargo && !SupplySites.IsSortieGroup(group))
                    return;

                var pos = grid.PositionComp.WorldAABB.Center;
                Pending best = null;
                double bestDist = MatchRange * MatchRange;
                foreach (var p in pending)
                {
                    if ((p.Kind == Kind.AirCargo) != cargo || (p.Group != null && p.Group != group))
                        continue;
                    var d = Vector3D.DistanceSquared(p.Origin, pos);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                    }
                }
                if (best == null)
                    return;
                if (best.Group == null)
                {
                    best.Group = group;
                    best.Remaining = cargo ? 1 : SupplySites.PlaneCount(group);
                }
                if (--best.Remaining <= 0)
                    pending.Remove(best);

                var session = SupplySession.Instance;
                if (session == null)
                    return;
                int cost = best.Kind == Kind.AirCargo ? SupplySites.AirCargoCost
                    : best.Kind == Kind.Defense ? SupplySites.DefenseCost : SupplySites.OffenseCost(group);
                session.Change(best.Site, -cost, best.Kind.ToString().ToLowerInvariant() + " " + group);
                if (cargo)
                    return;
                session.State.Sorties.Add(new Sortie { EntityId = grid.EntityId, Site = best.Site, Cost = cost });
                Watch(grid);
            }
            catch (Exception e)
            {
                SupplySession.Log("spawn: " + e);
            }
        }

        private static void Watch(IMyCubeGrid grid)
        {
            var mes = SupplySession.Mes();
            if (mes != null && despawnAction != null && mes.RegisterDespawnWatcher(grid, despawnAction))
                registered.Add(grid.EntityId);
        }

        // MES removed the grid: refund unless it was shot down or its Remote Control is no longer working.
        private static void OnDespawn(IMyCubeGrid grid, string source)
        {
            try
            {
                var session = SupplySession.Instance;
                if (session == null || grid == null)
                    return;
                var sorties = session.State.Sorties;
                for (int i = sorties.Count - 1; i >= 0; i--)
                {
                    var so = sorties[i];
                    if (so.EntityId != grid.EntityId)
                        continue;
                    sorties.RemoveAt(i);
                    registered.Remove(so.EntityId);
                    if (so.Lost || !RemoteWorking(grid))
                        SupplySession.Log(so.Site + ": sortie plane lost, no refund");
                    else
                        session.Change(so.Site, so.Cost, "refund, plane despawned (" + (source ?? "") + ")");
                    return;
                }
            }
            catch (Exception e)
            {
                SupplySession.Log("despawn: " + e);
            }
        }

        private static void MarkLost(IMyCubeGrid grid)
        {
            var session = SupplySession.Instance;
            if (session == null || grid == null || session.State.Sorties.Count == 0)
                return;
            if (Mark(session.State.Sorties, grid.EntityId))
                return;
            groupBuffer.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, groupBuffer);
            foreach (var g in groupBuffer)
                if (Mark(session.State.Sorties, g.EntityId))
                    break;
            groupBuffer.Clear();
        }

        private static bool Mark(List<Sortie> sorties, long entityId)
        {
            foreach (var so in sorties)
            {
                if (so.EntityId != entityId)
                    continue;
                so.Lost = true;
                return true;
            }
            return false;
        }

        private static bool RemoteWorking(IMyCubeGrid grid)
        {
            blocks.Clear();
            grid.GetBlocks(blocks, isRemote);
            bool working = false;
            foreach (var b in blocks)
            {
                if (b.FatBlock != null && b.FatBlock.IsWorking)
                {
                    working = true;
                    break;
                }
            }
            blocks.Clear();
            return working;
        }

        private static bool IsRemote(IMySlimBlock block)
        {
            return block.FatBlock is IMyRemoteControl;
        }

        // Every 10 s: drop stale pending sorties, forget planes that no longer exist (destroyed: no refund), and register despawn
        // watchers again for saved sorties (MES does not save them, NpcData.cs [ProtoIgnore] DespawnActions).
        public static void Maintain()
        {
            Expire(WarNewsSession.Now());
            var session = SupplySession.Instance;
            if (session == null)
                return;
            var sorties = session.State.Sorties;
            for (int i = sorties.Count - 1; i >= 0; i--)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(sorties[i].EntityId) as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Closed)
                {
                    registered.Remove(sorties[i].EntityId);
                    sorties.RemoveAt(i);
                    continue;
                }
                if (!registered.Contains(grid.EntityId))
                    Watch(grid);
            }
        }

        private static void Expire(double now)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
                if (now - pending[i].Time > PendingSeconds || now < pending[i].Time)
                    pending.RemoveAt(i);
        }

        private static bool Cut(string id, string prefix, out string rest)
        {
            if (id.StartsWith(prefix, StringComparison.Ordinal))
            {
                rest = id.Substring(prefix.Length);
                return true;
            }
            rest = null;
            return false;
        }
    }
}
