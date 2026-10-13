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
    //   WW2-Action-Depot-{From}-Become{To}                        Triggers-Depot-Ownership.sbc
    //   WW2-Action-OffenseSpawn-{F}[-WL4]                          Triggers-Airfield-OffensiveSpawn.sbc (the -Pick action only relays)
    //   WW2-Action-DefenseSpawn-{F}                                Triggers-DefensiveSpawn.sbc (charged only from an airfield)
    //   WW2-Action-DefenseSpawn-Port-{F}                           Triggers-DefensiveSpawn-Port.sbc (port Home, not refunded)
    //   WW2-Action-Airfield-AirCargoSpawn-{F}                      Triggers-Airfield-AirCargo.sbc
    //   WW2-Action-Crash-Air                                       Triggers-Crash-Air.sbc (marks a plane lost)
    //   WW2-Territory-Action-Delivery-{F}-{Origin}-{Destination}   Territory-Points.sbc (cargo ships)
    //   WW2-Territory-Action-Delivery-AirCargo-{F}                 Territory-Points.sbc
    //   WW2-Store-Action-UpdateSales-*                             Encounters/Store/*.sbc
    //   WW2-Convoy-Action-Delivered                                Convoy-Trucks.sbc (route from the truck's spawn group)
    // Depot ground units (WW2-DepotUnit-* spawn groups) are charged at spawn to the depot within MatchRange; patrol units are
    // refunded like sorties when MES removes them intact, defense units are not refunded (D-049).
    // MES queues behavior spawns and runs them later (BehaviorSpawnHelper), so a sortie action only opens a pending entry; the
    // next matching plane spawns near that site within 60 s are charged to it. Cargo ships are charged by spawn group.
    // Destroyed vs removed: MES calls despawn watchers only for grids its cleanup closes (Tasks/GridCleanup.cs:61-70, reached
    // from ForceDespawn and the cleanup rules); a grid destroyed in play closes without it and Maintain drops it unrefunded.
    public static class SupplyHooks
    {
        private const double PendingSeconds = 60;
        private const double MatchRange = 3500;

        private enum Kind { Offense, Defense, AirCargo, PortDefense }

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

                if (actionId == SupplyConvoys.DeliveredAction)
                {
                    TruckDelivered(rc.CubeGrid);
                    return;
                }
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
                if (Cut(actionId, "WW2-Action-Depot-", out rest))
                {
                    int b = rest.IndexOf("-Become", StringComparison.Ordinal);
                    if (b > 0)
                    {
                        var to = rest.Substring(b + 7);
                        if (to == "Gray" || to == "Green" || to == "Unowned")
                            s.OwnerChanged(SupplySites.SiteOf(rc.CubeGrid), to == "Unowned" ? "" : to);
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
                    else if (rest == "Port-Gray" || rest == "Port-Green")
                        AddPending(rc, Kind.PortDefense);
                    return;
                }
                if (Cut(actionId, "WW2-Territory-Action-Delivery-", out rest))
                {
                    string faction;
                    if (Cut(rest, "AirCargo-", out faction))
                    {
                        var plane = FindShipment(rc.CubeGrid);
                        if (plane != null && plane.Delivered)
                            return;
                        if (s.AirCargoDelivered(rc.GetPosition(), faction) && plane != null)
                            plane.Delivered = true;
                        return;
                    }
                    var parts = rest.Split('-');
                    if (parts.Length == 3)
                        s.ShipDelivered(FindShipment(rc.CubeGrid), parts[2]);
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
            if (site.Length == 0 || SupplySites.IsPort(site) != (kind == Kind.PortDefense))
                return;
            pending.Add(new Pending { Site = site, Kind = kind, Origin = rc.GetPosition(), Time = WarNewsSession.Now() });
        }

        // ---------------- spawns and despawns ----------------

        private static void OnSpawn(IMyCubeGrid grid)
        {
            try
            {
                if (grid == null)
                    return;
                var group = SupplySites.SpawnGroupOf(grid);
                if (TruckSpawned(grid, group) || ShipSpawned(grid, group) || DepotUnitSpawned(grid, group) || pending.Count == 0)
                    return;
                Expire(WarNewsSession.Now());
                if (pending.Count == 0)
                    return;
                bool cargo = SupplySites.IsAirCargoGroup(group);
                if (!cargo && !SupplySites.IsSortieGroup(group))
                    return;

                var pos = grid.PositionComp.WorldAABB.Center;
                bool portMarked = !cargo && SupplySites.IsPortDefenseSpawn(grid);
                Pending best = null, bestPort = null;
                double bestDist = MatchRange * MatchRange, bestPortDist = bestDist;
                foreach (var p in pending)
                {
                    if ((p.Kind == Kind.AirCargo) != cargo || (p.Group != null && p.Group != group))
                        continue;
                    if (p.Kind == Kind.PortDefense && SupplySites.PlaneCount(group) > 1)
                        continue;
                    var d = Vector3D.DistanceSquared(p.Origin, pos);
                    if (p.Kind == Kind.PortDefense)
                    {
                        if (d < bestPortDist)
                        {
                            bestPortDist = d;
                            bestPort = p;
                        }
                    }
                    else if (d < bestDist)
                    {
                        bestDist = d;
                        best = p;
                    }
                }
                // Port defense planes carry the spawner's marker; the rest are airfield sorties. Unmarked (spawner from before the
                // marker, until the port reloads): distance only, and only when no airfield sortie is pending nearby.
                if (portMarked || best == null)
                    best = bestPort;
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
                    : best.Kind == Kind.PortDefense ? SupplySites.PortDefenseCost
                    : best.Kind == Kind.Defense ? SupplySites.DefenseCost : SupplySites.OffenseCost(group);
                session.Change(best.Site, -cost, best.Kind.ToString().ToLowerInvariant() + " " + group);
                if (best.Kind == Kind.PortDefense)
                    return;
                if (cargo)
                {
                    session.State.Shipments.Add(new Shipment { EntityId = grid.EntityId, Origin = best.Site, Cost = cost, Plane = true });
                    Watch(grid);
                    return;
                }
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
                        SupplySession.Log(so.Site + ": sortie lost, no refund");
                    else
                        session.Change(so.Site, so.Cost, "refund, sortie despawned intact (" + (source ?? "") + ")");
                    return;
                }
                var trucks = session.State.Trucks;
                for (int i = trucks.Count - 1; i >= 0; i--)
                {
                    var t = trucks[i];
                    if (t.EntityId != grid.EntityId)
                        continue;
                    trucks.RemoveAt(i);
                    registered.Remove(t.EntityId);
                    var route = SupplyConvoys.Find(t.Route);
                    if (t.Delivered || route == null)
                        return;
                    if (RemoteWorking(grid))
                        session.ChangeOnward(route.Port, t.Cost, "refund, convoy truck on " + route.Id + " removed before delivering (" + (source ?? "") + ")");
                    else
                        SupplySession.Log(route.Port + ": convoy truck lost, no refund");
                    return;
                }
                var shipments = session.State.Shipments;
                for (int i = shipments.Count - 1; i >= 0; i--)
                {
                    var c = shipments[i];
                    if (c.EntityId != grid.EntityId)
                        continue;
                    shipments.RemoveAt(i);
                    registered.Remove(c.EntityId);
                    var what = c.Plane ? "cargo plane" : "cargo ship";
                    if (c.Delivered)
                        return;
                    if (c.Lost || !RemoteWorking(grid))
                        SupplySession.Log(c.Origin + ": " + what + " lost, no refund");
                    else if (c.Plane)
                        session.Change(c.Origin, c.Cost, "refund, cargo plane despawned without delivering (" + (source ?? "") + ")");
                    else
                        session.ChangeOnward(c.Origin, c.Cost, "refund, cargo ship despawned without delivering (" + (source ?? "") + ")");
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
            if (session == null || grid == null)
                return;
            var plane = FindShipment(grid);
            if (plane != null)
            {
                plane.Lost = true;
                return;
            }
            if (session.State.Sorties.Count == 0)
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

        // The ledger entry of a cargo ship or plane, by the grid or any grid mechanically linked to it (MES keeps its data on the
        // biggest grid, PrefabSpawner.GridsSpawned), or null.
        private static Shipment FindShipment(IMyCubeGrid grid)
        {
            var session = SupplySession.Instance;
            if (session == null || grid == null || session.State.Shipments.Count == 0)
                return null;
            var c = ShipmentOf(session.State.Shipments, grid.EntityId);
            if (c != null)
                return c;
            groupBuffer.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, groupBuffer);
            foreach (var g in groupBuffer)
            {
                c = ShipmentOf(session.State.Shipments, g.EntityId);
                if (c != null)
                    break;
            }
            groupBuffer.Clear();
            return c;
        }

        private static Shipment ShipmentOf(List<Shipment> list, long entityId)
        {
            foreach (var c in list)
                if (c.EntityId == entityId)
                    return c;
            return null;
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
            var trucks = session.State.Trucks;
            for (int i = trucks.Count - 1; i >= 0; i--)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(trucks[i].EntityId) as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Closed)
                {
                    if (!trucks[i].Delivered)
                        SupplySession.Log("convoy truck on " + trucks[i].Route + " destroyed");
                    registered.Remove(trucks[i].EntityId);
                    trucks.RemoveAt(i);
                    continue;
                }
                if (!registered.Contains(grid.EntityId))
                    Watch(grid);
            }
            var shipments = session.State.Shipments;
            for (int i = shipments.Count - 1; i >= 0; i--)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(shipments[i].EntityId) as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Closed)
                {
                    if (!shipments[i].Delivered)
                        SupplySession.Log((shipments[i].Plane ? "cargo plane from " : "cargo ship from ") + shipments[i].Origin + " destroyed");
                    registered.Remove(shipments[i].EntityId);
                    shipments.RemoveAt(i);
                    continue;
                }
                if (!registered.Contains(grid.EntityId))
                    Watch(grid);
            }
        }

        // ---------------- cargo ships ----------------

        // A WW2-CargoShip-{HQ}-{Port} ship spawned: load it from the HQ's Onward box at a third of its load. Return legs carry
        // nothing and are not tracked. A short Onward box sends the ship part-loaded. True if the grid is a supply ship.
        private static bool ShipSpawned(IMyCubeGrid grid, string group)
        {
            int load;
            string origin, destination;
            if (!SupplySites.ShipLoad(group, out load, out origin, out destination))
                return false;
            var session = SupplySession.Instance;
            var hq = session != null ? session.Get(origin) : null;
            if (hq == null)
                return true;
            int cost = Math.Min(SupplySites.ShipCost(load), hq.Onward);
            int carried = SupplySites.ShipCost(load) > 0 ? load * cost / SupplySites.ShipCost(load) : 0;
            if (cost > 0)
                session.ChangeOnward(origin, -cost, "cargo ship " + group + " loaded " + carried);
            else
                SupplySession.Log(origin + ": cargo ship " + group + " sails empty, onward is 0");
            session.State.Shipments.Add(new Shipment { EntityId = grid.EntityId, Origin = origin, Destination = destination, Cost = cost, Load = carried });
            Watch(grid);
            return true;
        }

        // ---------------- convoy trucks ----------------

        // A WW2-Convoy-{Route}-{slot} truck spawned: load it from its port's Onward box. True if the grid is a convoy truck.
        private static bool TruckSpawned(IMyCubeGrid grid, string group)
        {
            var route = SupplyConvoys.RouteOfGroup(group);
            if (route == null)
                return false;
            var session = SupplySession.Instance;
            if (session == null)
                return true;
            var port = session.Get(route.Port);
            int cost = port != null ? Math.Min(SupplyConvoys.TruckCost, port.Onward) : 0;
            session.ChangeOnward(route.Port, -cost, "convoy truck " + group);
            session.State.Trucks.Add(new Truck { EntityId = grid.EntityId, Route = route.Id, Cost = cost });
            Watch(grid);
            return true;
        }

        // WW2-Convoy-Action-Delivered from a truck at the end of its road: credit the airfield once, if the truck's side holds it.
        private static void TruckDelivered(IMyCubeGrid grid)
        {
            var session = SupplySession.Instance;
            if (session == null || grid == null)
                return;
            Truck truck = null;
            foreach (var t in session.State.Trucks)
                if (t.EntityId == grid.EntityId)
                    truck = t;
            var route = truck != null ? SupplyConvoys.Find(truck.Route) : SupplyConvoys.RouteOfGroup(SupplySites.SpawnGroupOf(grid));
            if (route == null || (truck != null && truck.Delivered))
                return;
            // A truck its port could only part-load delivers in proportion; one missing from the ledger delivers in full.
            int delivery = truck != null ? SupplyConvoys.TruckDelivery * truck.Cost / SupplyConvoys.TruckCost : SupplyConvoys.TruckDelivery;
            if (truck == null)
            {
                truck = new Truck { EntityId = grid.EntityId, Route = route.Id };
                session.State.Trucks.Add(truck);
                Watch(grid);
            }
            var airfield = session.Get(route.Destination);
            if (airfield == null || airfield.Owner != route.Faction)
            {
                SupplySession.Log("convoy delivery on " + route.Id + " not credited: airfield is " + (airfield == null || airfield.Owner.Length == 0 ? "unowned" : "held by " + airfield.Owner) + "; refunded to the port when the truck leaves");
                return;
            }
            truck.Delivered = true;
            if (delivery > 0)
                session.Change(route.Destination, delivery, "convoy delivery from " + route.Port);
        }

        // ---------------- depot ground units ----------------

        // A WW2-DepotUnit-* grid spawned: charge the nearest depot within MatchRange by the unit's prefab; patrol units go into the
        // sortie ledger so an intact despawn refunds them. True if the grid is a depot unit.
        private static bool DepotUnitSpawned(IMyCubeGrid grid, string group)
        {
            if (!SupplySites.IsDepotUnitGroup(group))
                return false;
            var session = SupplySession.Instance;
            if (session == null)
                return true;
            var pos = grid.PositionComp.WorldAABB.Center;
            string site = null;
            double best = MatchRange * MatchRange;
            foreach (var t in SupplySites.Tracked)
            {
                Vector3D p;
                if (!t.Depot || !SupplySites.Position(t, out p))
                    continue;
                var d = Vector3D.DistanceSquared(p, pos);
                if (d < best)
                {
                    best = d;
                    site = t.Group;
                }
            }
            if (site == null)
            {
                SupplySession.Log("depot unit " + group + " spawned with no open depot within " + MatchRange + " m; not charged");
                return true;
            }
            bool patrol = SupplySites.IsDepotPatrolGroup(group);
            var prefab = SupplySites.PrefabOf(grid);
            int cost = SupplySites.DepotUnitCost(prefab, patrol);
            session.Change(site, -cost, (patrol ? "patrol " : "defense ") + (prefab ?? group));
            if (patrol)
            {
                session.State.Sorties.Add(new Sortie { EntityId = grid.EntityId, Site = site, Cost = cost });
                Watch(grid);
            }
            return true;
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
