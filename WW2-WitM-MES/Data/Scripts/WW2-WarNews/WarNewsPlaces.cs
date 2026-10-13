using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // Names a world position after the nearest WitM site, by direction from the planet centre. Sites come from the shared table
    // RefreshSites.All; Rome is the territory anchor (Territory-Zones.sbc), normalised.
    public static class WarNewsPlaces
    {
        private struct Site
        {
            public string Name;
            public Vector3D Dir;
            public WW2WitM.Refresh.SiteKind Kind;

            public Site(string name, Vector3D dir, WW2WitM.Refresh.SiteKind kind)
            {
                Name = name;
                Dir = Vector3D.Normalize(dir);
                Kind = kind;
            }
        }

        private static readonly Site[] Sites = BuildSites();

        private static Site[] BuildSites()
        {
            var all = WW2WitM.Refresh.RefreshSites.All;
            var sites = new Site[all.Length + 1];
            for (int i = 0; i < all.Length; i++)
                sites[i] = new Site(all[i].Name, all[i].Dir, all[i].Kind);
            sites[all.Length] = new Site("Rome", new Vector3D(26.83, -20030.21, 22474.36), WW2WitM.Refresh.SiteKind.Port);
            return sites;
        }

        private static readonly Dictionary<string, string> AnchorNames = new Dictionary<string, string>
        {
            { "LaSpezia", "La Spezia" }, { "Rome", "Rome" }, { "Tripoli", "Tripoli" },
            { "Toulon", "Toulon" }, { "Oran", "Oran" }, { "Alexandria", "Alexandria" }, { "Civitavecchia", "Civitavecchia" },
        };

        private static readonly Dictionary<string, string> AnchorFactions = new Dictionary<string, string>
        {
            { "LaSpezia", "Gray" }, { "Rome", "Gray" }, { "Tripoli", "Gray" },
            { "Toulon", "Green" }, { "Oran", "Green" }, { "Alexandria", "Green" },
        };

        private static readonly List<MyPlanet> planets = new List<MyPlanet>();

        public static string Nearest(Vector3D position)
        {
            return Find(position, null);
        }

        public static string NearestAirfield(Vector3D position)
        {
            return Find(position, WW2WitM.Refresh.SiteKind.Airfield);
        }

        public static string NearestDepot(Vector3D position)
        {
            return Find(position, WW2WitM.Refresh.SiteKind.Depot);
        }

        public static string AnchorName(string id)
        {
            string name;
            return AnchorNames.TryGetValue(id, out name) ? name : id;
        }

        public static string AnchorFaction(string id)
        {
            string faction;
            return AnchorFactions.TryGetValue(id, out faction) ? faction : "";
        }

        public static void Clear()
        {
            planets.Clear();
        }

        private static string Find(Vector3D position, WW2WitM.Refresh.SiteKind? only)
        {
            var centre = PlanetCentre(position);
            var dir = position - centre;
            if (dir.LengthSquared() < 1)
                return "the front";
            dir = Vector3D.Normalize(dir);

            string best = "the front";
            double bestDot = -2;
            foreach (var s in Sites)
            {
                if (only.HasValue && s.Kind != only.Value)
                    continue;
                var d = Vector3D.Dot(dir, s.Dir);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = s.Name;
                }
            }
            return best;
        }

        private static Vector3D PlanetCentre(Vector3D position)
        {
            if (planets.Count == 0)
                MyAPIGateway.Entities.GetEntities(null, AddPlanet);

            MyPlanet nearest = null;
            double bestDist = double.MaxValue;
            foreach (var p in planets)
            {
                if (p == null || p.MarkedForClose)
                    continue;
                var dist = Vector3D.DistanceSquared(p.PositionComp.GetPosition(), position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = p;
                }
            }
            return nearest != null ? nearest.PositionComp.GetPosition() : Vector3D.Zero;
        }

        private static bool AddPlanet(IMyEntity e)
        {
            var p = e as MyPlanet;
            if (p != null)
                planets.Add(p);
            return false;
        }
    }
}
