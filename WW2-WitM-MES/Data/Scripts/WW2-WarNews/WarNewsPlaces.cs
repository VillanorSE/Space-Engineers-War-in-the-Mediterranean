using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // Names a world position after the nearest WitM site, by direction from the planet centre.
    // Directions are the sites' StaticEncounterPlanetDirection (SpawnConditions-Ports.sbc, SpawnConditions-Installations.sbc);
    // Rome is the territory anchor (Territory-Zones.sbc), normalised. Update this table when a site moves or is added.
    public static class WarNewsPlaces
    {
        private struct Site
        {
            public string Name;
            public Vector3D Dir;
            public bool Airfield;

            public Site(string name, double x, double y, double z, bool airfield)
            {
                Name = name;
                Dir = Vector3D.Normalize(new Vector3D(x, y, z));
                Airfield = airfield;
            }
        }

        private static readonly Site[] Sites =
        {
            new Site("La Spezia", 0.241848463, -0.902317349, 0.356837108, false),
            new Site("Toulon", 0.560029462, -0.797889702, 0.223022476, false),
            new Site("Oran", 0.731680739, 0.279121172, -0.621879946, false),
            new Site("Tripoli", -0.097008599, 0.688288288, 0.718921807, false),
            new Site("Alexandria", -0.192091142579102, 0.859672486172863, -0.473354211410465, false),
            new Site("Valencia", 0.707906168868626, -0.307714528162304, -0.635752015518312, false),
            new Site("Civitavecchia", 0.106072219319843, -0.699055920364547, 0.707155926576197, false),
            new Site("Rome", 26.83, -20030.21, 22474.36, false),
            new Site("Gibraltar", 0.405268480561213, 0.29884657438491, -0.863972328053399, true),
            new Site("Bizerte", 0.463732953259739, 0.0435520585467395, 0.884903930524293, true),
            new Site("Tripoli", -0.0858730279300504, 0.699611068160377, 0.709344892405395, true),
            new Site("Foggia", -0.433940790701937, -0.615315371008138, 0.658089951576603, true),
            new Site("Casablanca", 0.132644238359419, 0.637365737443778, -0.759058905990042, true),
        };

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
            return Find(position, false);
        }

        public static string NearestAirfield(Vector3D position)
        {
            return Find(position, true);
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

        private static string Find(Vector3D position, bool airfieldsOnly)
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
                if (airfieldsOnly && !s.Airfield)
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
