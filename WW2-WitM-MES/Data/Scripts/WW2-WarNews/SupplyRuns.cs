using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using WW2WitM.Refresh;
using WW2WitM.WarNewsSystem;

namespace WW2WitM.Supply
{
    // Server only: the active supply runs for the "Supply Runs" LCD (SupplyRunsTss), from the truck and shipment ledgers
    // (SupplySession.State). A truck column is every live, undelivered truck of one route; its position is the truck with the
    // lowest start slot. Cargo ships are the loaded legs (planes are left out). WarNewsSession sends the list every 10 s.
    public static class SupplyRuns
    {
        private static readonly Dictionary<string, SupplyRun> byRoute = new Dictionary<string, SupplyRun>();
        private static readonly Dictionary<string, int> leadSlot = new Dictionary<string, int>();

        public static void Build(List<SupplyRun> runs)
        {
            runs.Clear();
            byRoute.Clear();
            leadSlot.Clear();
            var session = SupplySession.Instance;
            if (session == null || session.State == null)
                return;

            foreach (var t in session.State.Trucks)
            {
                if (t.Delivered)
                    continue;
                var grid = Live(t.EntityId);
                var route = SupplyConvoys.Find(t.Route);
                if (grid == null || route == null)
                    continue;
                SupplyRun run;
                if (!byRoute.TryGetValue(route.Id, out run))
                {
                    run = new SupplyRun { Faction = route.Faction, From = SiteName(route.Port), To = SiteName(route.Destination) };
                    byRoute[route.Id] = run;
                    leadSlot[route.Id] = int.MaxValue;
                    runs.Add(run);
                }
                run.Count++;
                int slot = Slot(SupplySites.SpawnGroupOf(grid));
                if (slot < leadSlot[route.Id])
                {
                    leadSlot[route.Id] = slot;
                    Place(run, grid.GetPosition(), route.Destination);
                }
            }

            foreach (var c in session.State.Shipments)
            {
                if (c.Plane || c.Delivered || c.Lost || string.IsNullOrEmpty(c.Destination))
                    continue;
                var grid = Live(c.EntityId);
                if (grid == null)
                    continue;
                var run = new SupplyRun { Faction = SupplySites.PortFaction(c.Origin), Ship = true, From = SiteName(c.Origin), To = SiteName(c.Destination), Count = 1 };
                Place(run, grid.GetPosition(), c.Destination);
                runs.Add(run);
            }
        }

        private static IMyCubeGrid Live(long entityId)
        {
            var grid = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;
            return grid == null || grid.MarkedForClose || grid.Closed ? null : grid;
        }

        // Start slot from the truck's spawn group WW2-Convoy-{Route}-{slot}; unknown slots sort last.
        private static int Slot(string group)
        {
            int dash = group != null ? group.LastIndexOf('-') : -1;
            int slot;
            return dash >= 0 && int.TryParse(group.Substring(dash + 1), out slot) ? slot : int.MaxValue - 1;
        }

        private static void Place(SupplyRun run, Vector3D position, string destination)
        {
            run.X = position.X;
            run.Y = position.Y;
            run.Z = position.Z;
            var site = SupplySites.Find(destination);
            Vector3D target;
            run.ToGo = site != null && SupplySites.Position(site, out target) ? Vector3D.Distance(position, target) : -1;
        }

        private static string SiteName(string group)
        {
            var site = SupplySites.Find(group);
            if (site == null)
                return group.StartsWith("WW2-") ? group.Substring(4) : group;
            if (site.Depot)
                return site.Name + " Depot";
            if (site.Airfield)
                return site.Name + " Airfield";
            return site.Name;
        }
    }
}
