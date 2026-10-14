using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using WW2WitM.Refresh;

namespace WW2WitM.Supply
{
    // Supply rules (D-035, D-041, D-045, D-047, D-049; production repo design/war-layer-phase1-build-plan.md) and the site data supply needs
    // on top of the shared site table RefreshSites.All.
    public static class SupplySites
    {
        public const int Normal = 0, Low = 1, Full = 2;

        // Ports (D-047): Home box cap 200 (bands on Home), Onward box cap 400. Start values for a new port are an agent choice.
        public const int PortStart = 120, PortCap = 200, PortLow = 60, PortFull = 180, PortUpkeep = 4;
        public const int PortOnwardStart = 80, PortOnwardCap = 400;
        public const double PortOnwardShare = 0.5;
        public const int AirfieldStart = 60, AirfieldCap = 100, AirfieldLow = 30, AirfieldFull = 80, AirfieldUpkeep = 2;
        public const int CapturedStart = 20;
        public const int HqProduction = 20, AirCargoDelivery = 20, AirCargoCost = 10;
        public const int DefenseCost = 3, SingleCost = 4, BomberCost = 5, GroupPlaneCost = 3;
        // Each defending plane a port scrambles (WW2-Action-DefenseSpawn-Port-{F}), from its Home box; not refunded.
        public const int PortDefenseCost = 3;
        public const double ZoneStep = 5000;
        public const double AirCargoDeliveryRange = 5000;

        // Supply ports: spawn group -> { faction, territory anchor whose zone they feed }. Valencia is neutral and has no supply.
        private static readonly Dictionary<string, string[]> Ports = new Dictionary<string, string[]>
        {
            { "WW2-Port-LaSpezia", new[] { "Gray", "LaSpezia" } },
            { "WW2-Port-Tripoli", new[] { "Gray", "Tripoli" } },
            { "WW2-Port-Civitavecchia", new[] { "Gray", "Rome" } },
            { "WW2-Port-Toulon", new[] { "Green", "Toulon" } },
            { "WW2-Port-Oran", new[] { "Green", "Oran" } },
            { "WW2-Port-Alexandria", new[] { "Green", "Alexandria" } },
        };

        // Supply production (D-047): the HQs put it into Onward and top their Home box up from it.
        private static readonly HashSet<string> Hqs = new HashSet<string> { "WW2-Port-LaSpezia", "WW2-Port-Toulon" };

        // Cargo ship spawn group WW2-CargoShip-{Origin}-{Destination} -> load delivered; it costs a third of that at the HQ.
        // Return legs (to an HQ) are not listed: they carry and cost nothing.
        private static readonly Dictionary<string, int> ShipLoads = new Dictionary<string, int>
        {
            { "WW2-CargoShip-LaSpezia-Civitavecchia", 15 },
            { "WW2-CargoShip-LaSpezia-Tripoli", 60 },
            { "WW2-CargoShip-Toulon-Oran", 60 },
            { "WW2-CargoShip-Toulon-Alexandria", 90 },
        };

        // Territory zone centres ([Coordinates:] in Territory-Zones.sbc); radius 5 km plus 5 km per {Anchor}_Tier{k}Grown (Triggers-Zone-Change.sbc).
        private static readonly Dictionary<string, Vector3D> Anchors = new Dictionary<string, Vector3D>
        {
            { "LaSpezia", new Vector3D(7280.0, -27161.11, 10741.33) },
            { "Toulon", new Vector3D(16857.73, -24017.68, 6713.31) },
            { "Oran", new Vector3D(22024.69, 8401.97, -18719.52) },
            { "Tripoli", new Vector3D(-2920.1, 20718.51, 21640.62) },
            { "Alexandria", new Vector3D(-5782.01, 25876.45, -14248.13) },
            { "Rome", new Vector3D(26.83, -20030.21, 22474.36) },
        };

        // Plane groups an airfield sortie can spawn (WW2-Spawn-OffenseSpawn-*, WW2-Spawn-DefenseSpawn-*); keep in step with those spawners.
        private static readonly HashSet<string> SortieGroups = new HashSet<string>
        {
            "NPC-WW2-Re2000", "NPC-WW2-Re2001", "NPC-WW2-FC20", "NPC-WW2-Ba65", "NPC-WW2-SM79Bomber",
            "NPC-WW2-Ba65-Pair", "NPC-WW2-Re2000-Flight", "NPC-WW2-Re2001-Flight",
            "NPC-WW2-MS406", "NPC-WW2-Potez630", "NPC-WW2-V-156-F_Bomber", "NPC-WW2-MS406-Flight",
            "NPC-WW2-D520", "NPC-WW2-D520-Flight",
        };

        private static readonly HashSet<string> Bombers = new HashSet<string> { "NPC-WW2-SM79Bomber", "NPC-WW2-V-156-F_Bomber" };

        // Depot ground units (D-049): spawn groups WW2-DepotUnit-{Defense|Patrol}-WL{n}-{Side} (production repo
        // tools/scripts/WW2-Tool-GenDepots.py). Charged at spawn by prefab: armored car 2, tank 3 (defense) / 4 (patrol).
        // Depots use the airfield supply constants (start 60, cap 100, Low < 30, Full >= 80, upkeep 2/h, captured 20).
        public const string DepotUnitPrefix = "WW2-DepotUnit-";
        public const int DepotCarCost = 2, DepotTankDefenseCost = 3, DepotTankPatrolCost = 4;
        private static readonly HashSet<string> ArmoredCars = new HashSet<string> { "NPC-WW2-AB41", "NPC-WW2-Panhard178" };
        // Territory points a held depot adds each hour to its owner's nearest anchor (D-049 "holding gives territory points";
        // AGENT number, owner to confirm). Counters are capped by Triggers-Zone-Change.sbc.
        public const int DepotHoldingPointsPerHour = 0;

        private static readonly Dictionary<string, string> AnchorSides = new Dictionary<string, string>
        {
            { "LaSpezia", "Gray" }, { "Tripoli", "Gray" }, { "Rome", "Gray" },
            { "Toulon", "Green" }, { "Oran", "Green" }, { "Alexandria", "Green" },
        };

        public static readonly List<RefreshSite> Tracked = BuildTracked();

        private static readonly Dictionary<string, Vector3D> positions = new Dictionary<string, Vector3D>();
        private static readonly Dictionary<long, string> gridSites = new Dictionary<long, string>();
        private static MyPlanet planet;

        private static List<RefreshSite> BuildTracked()
        {
            var list = new List<RefreshSite>();
            foreach (var s in RefreshSites.All)
                if (s.Owned || Ports.ContainsKey(s.Group))
                    list.Add(s);
            return list;
        }

        public static void Clear()
        {
            positions.Clear();
            gridSites.Clear();
            planet = null;
        }

        public static RefreshSite Find(string group)
        {
            foreach (var s in Tracked)
                if (s.Group == group)
                    return s;
            return null;
        }

        public static bool IsPort(string group)
        {
            return Ports.ContainsKey(group);
        }

        public static bool IsDepot(string group)
        {
            var s = group != null ? Find(group) : null;
            return s != null && s.Depot;
        }

        public static bool IsDepotUnitGroup(string group)
        {
            return group != null && group.StartsWith(DepotUnitPrefix, StringComparison.Ordinal);
        }

        public static bool IsDepotPatrolGroup(string group)
        {
            return IsDepotUnitGroup(group) && group.StartsWith(DepotUnitPrefix + "Patrol-", StringComparison.Ordinal);
        }

        public static int DepotUnitCost(string prefab, bool patrol)
        {
            if (prefab != null && ArmoredCars.Contains(prefab))
                return DepotCarCost;
            return patrol ? DepotTankPatrolCost : DepotTankDefenseCost;
        }

        // The owner's territory anchor nearest a site, for depot holding points; "" if none.
        public static string NearestAnchor(Vector3D position, string side)
        {
            string best = "";
            double bestDist = double.MaxValue;
            foreach (var a in Anchors)
            {
                string s;
                if (!AnchorSides.TryGetValue(a.Key, out s) || s != side)
                    continue;
                var d = Vector3D.DistanceSquared(a.Value, position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = a.Key;
                }
            }
            return best;
        }

        public static string PortFaction(string group)
        {
            string[] p;
            return Ports.TryGetValue(group, out p) ? p[0] : "";
        }

        public static string PortAnchor(string group)
        {
            string[] p;
            return Ports.TryGetValue(group, out p) ? p[1] : "";
        }

        public static bool IsHq(string group)
        {
            return group != null && Hqs.Contains(group);
        }

        // A cargo ship's load and its origin/destination port groups, or false for a return leg or another group.
        public static bool ShipLoad(string group, out int load, out string origin, out string destination)
        {
            origin = destination = null;
            if (group == null || !ShipLoads.TryGetValue(group, out load))
            {
                load = 0;
                return false;
            }
            var parts = group.Split('-');
            origin = "WW2-Port-" + parts[2];
            destination = "WW2-Port-" + parts[3];
            return true;
        }

        public static int ShipCost(int load) { return load / 3; }

        public static int Cap(string group) { return IsPort(group) ? PortCap : AirfieldCap; }
        public static int Start(string group) { return IsPort(group) ? PortStart : AirfieldStart; }
        public static int LowBelow(string group) { return IsPort(group) ? PortLow : AirfieldLow; }
        public static int FullAt(string group) { return IsPort(group) ? PortFull : AirfieldFull; }

        // A site leaves Low only at 120% of the Low threshold.
        public static int ComputeBand(string group, int stock, int oldBand)
        {
            if (stock >= FullAt(group))
                return Full;
            var low = LowBelow(group);
            if (oldBand == Low)
                return stock * 5 >= low * 6 ? Normal : Low;
            return stock < low ? Low : Normal;
        }

        public static string BandName(int band)
        {
            return band == Low ? "Low" : band == Full ? "Full" : "Normal";
        }

        // Zone radius as MES has it now: every zone starts at 5 km (Territory-Zones.sbc) and each Grow adds 5 km and sets its tier bool.
        public static double ZoneRadius(string anchor)
        {
            int tiers = 0;
            for (int k = 1; k <= 6; k++)
            {
                bool grown;
                if (MyAPIGateway.Utilities.GetVariable(anchor + "_Tier" + k + "Grown", out grown) && grown)
                    tiers++;
            }
            return ZoneStep * (1 + tiers);
        }

        public static bool InZone(string anchor, Vector3D position)
        {
            Vector3D centre;
            if (!Anchors.TryGetValue(anchor, out centre))
                return false;
            var r = ZoneRadius(anchor);
            return Vector3D.DistanceSquared(position, centre) < r * r;
        }

        public static bool IsSortieGroup(string group)
        {
            return group != null && SortieGroups.Contains(group);
        }

        public static bool IsAirCargoGroup(string group)
        {
            return group != null && group.StartsWith("WW2-AirCargo-", StringComparison.Ordinal);
        }

        public static int PlaneCount(string group)
        {
            if (group.EndsWith("-Flight", StringComparison.Ordinal)) return 3;
            if (group.EndsWith("-Pair", StringComparison.Ordinal)) return 2;
            return 1;
        }

        public static int OffenseCost(string group)
        {
            if (PlaneCount(group) > 1) return GroupPlaneCost;
            return Bombers.Contains(group) ? BomberCost : SingleCost;
        }

        // Where MES places the site: the surface under its direction, raised by its altitude (as RefreshSession.ComputeAnchor).
        public static bool Position(RefreshSite site, out Vector3D position)
        {
            if (site.Depot)
            {
                position = site.Coords;
                return true;
            }
            if (positions.TryGetValue(site.Group, out position))
                return true;
            if (planet == null || planet.MarkedForClose)
            {
                planet = null;
                MyAPIGateway.Entities.GetEntities(null, CollectPlanet);
            }
            if (planet == null)
                return false;
            var centre = planet.PositionComp.WorldAABB.Center;
            position = planet.GetClosestSurfacePointGlobal(centre + site.Dir * 10000) + site.Dir * site.Altitude;
            positions[site.Group] = position;
            return true;
        }

        private static bool CollectPlanet(IMyEntity e)
        {
            var p = e as MyPlanet;
            if (p != null && (planet == null || p.MaximumRadius > planet.MaximumRadius))
                planet = p;
            return false;
        }

        // The tracked site a grid belongs to (its MES spawn group), or "" if none. Cached per grid.
        public static string SiteOf(IMyCubeGrid grid)
        {
            if (grid == null)
                return "";
            string site;
            if (gridSites.TryGetValue(grid.EntityId, out site))
                return site;
            var group = SpawnGroupOf(grid);
            site = group != null && Find(group) != null ? group : "";
            gridSites[grid.EntityId] = site;
            return site;
        }

        // The MES spawn group stored on the grid (NpcData, see RefreshSites.cs NpcDataLite), or null.
        public static string SpawnGroupOf(IMyCubeGrid grid)
        {
            var data = NpcDataOf(grid);
            return data != null ? data.SpawnGroupName : null;
        }

        // Set by the port defense spawners ([CustomCountersVariables:WitMPortDefense,1], Triggers-DefensiveSpawn-Port.sbc); MES copies
        // a spawner's counter variables onto the spawned NpcData (Spawning/PrefabSpawner.cs, BehaviorSpawnHelper.cs).
        public const string PortDefenseMarker = "WitMPortDefense";

        public static bool IsPortDefenseSpawn(IMyCubeGrid grid)
        {
            var data = NpcDataOf(grid);
            int v;
            return data != null && data.CustomCountersVariables != null && data.CustomCountersVariables.TryGetValue(PortDefenseMarker, out v) && v > 0;
        }

        // The prefab MES spawned the grid from (NpcData OriginalPrefabId), or null.
        public static string PrefabOf(IMyCubeGrid grid)
        {
            var data = NpcDataOf(grid);
            return data != null ? data.OriginalPrefabId : null;
        }

        private static NpcDataLite NpcDataOf(IMyCubeGrid grid)
        {
            string text;
            if (grid == null || grid.Storage == null || !grid.Storage.TryGetValue(RefreshSession.NpcDataKey, out text) || string.IsNullOrEmpty(text))
                return null;
            try
            {
                return MyAPIGateway.Utilities.SerializeFromBinary<NpcDataLite>(Convert.FromBase64String(text));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
