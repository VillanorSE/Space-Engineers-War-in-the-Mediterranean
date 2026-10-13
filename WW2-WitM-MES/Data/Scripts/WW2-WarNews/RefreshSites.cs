using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRageMath;

namespace WW2WitM.Refresh
{
    public enum SiteKind { Port, Airfield, Depot }

    // One WitM MES static encounter. Group is the SpawnGroup SubtypeId; Dir/Altitude are its conditions'
    // StaticEncounterPlanetDirection/Altitude, TriggerRadius its [TriggerRadius] (SpawnConditions-Ports.sbc,
    // SpawnConditions-Installations.sbc). Update the table when a site is added or moved: refresh, War News place names
    // (WarNewsPlaces) and supply (SupplySites) all read it. Depots (D-049) use exact coordinates read from their spawn
    // conditions (SpawnConditions-Depots-Sites.sbc) and are listed only once open (RefreshSites.Depots).
    public class RefreshSite
    {
        public readonly string Group;
        public readonly string Name;
        public readonly SiteKind Kind;
        public readonly Vector3D Dir;
        public readonly double Altitude;
        // Depots: the [StaticEncounterCoords:] position (planet direction is not used, knowledge mes_static_planet_altitude_water).
        public readonly Vector3D Coords;

        public RefreshSite(string group, string name, bool airfield, double x, double y, double z, double altitude)
        {
            Group = group;
            Name = name;
            Kind = airfield ? SiteKind.Airfield : SiteKind.Port;
            Dir = new Vector3D(x, y, z);
            Altitude = altitude;
        }

        // The planet centre is the world origin (the territory anchors equal their sites' directions times the radius).
        public RefreshSite(string group, string name, Vector3D coords)
        {
            Group = group;
            Name = name;
            Kind = SiteKind.Depot;
            Coords = coords;
            Dir = Vector3D.Normalize(coords);
        }

        public bool Airfield { get { return Kind == SiteKind.Airfield; } }
        public bool Depot { get { return Kind == SiteKind.Depot; } }
        // Sites that change hands by territory (airfields and depots); ports keep their faction.
        public bool Owned { get { return Kind != SiteKind.Port; } }
        public string Label { get { return (Kind == SiteKind.Airfield ? "Airfield " : Kind == SiteKind.Depot ? "Depot " : "Port ") + Name; } }
        public string Short { get { return Group.StartsWith("WW2-", System.StringComparison.Ordinal) ? Group.Substring(4) : Group; } }
        public double TriggerRadiusKm { get { return Kind == SiteKind.Port ? 65 : 20; } }
    }

    // A vehicle depot (D-049) and whether it is open: its spawn conditions have [StaticEncounter:true], coordinates, forward
    // and up, and the depot prefab exists. The C# sets WitM-DepotOpen-{group} from this, which the spawn conditions require.
    public class DepotSite
    {
        public readonly string Group;
        public readonly string Name;
        public Vector3D Coords;
        public bool Open;
        public string Problem = "";

        public DepotSite(string group, string name)
        {
            Group = group;
            Name = name;
        }
    }

    public static class RefreshSites
    {
        public const string DepotPrefab = "NPC-WW2-Depot";
        public const string DepotOpenPrefix = "WitM-DepotOpen-";

        // Keep in step with production repo tools/scripts/WW2-Tool-GenDepots.py (DEPOTS).
        public static readonly DepotSite[] Depots =
        {
            new DepotSite("WW2-Depot-Benghazi", "Benghazi"),
            new DepotSite("WW2-Depot-Tunis", "Tunis"),
            new DepotSite("WW2-Depot-Tobruk", "Tobruk"),
            new DepotSite("WW2-Depot-Abruzzo", "Abruzzo"),
            new DepotSite("WW2-Depot-Mascara", "Mascara"),
            new DepotSite("WW2-Depot-Sirte", "Sirte"),
        };

        private static readonly RefreshSite[] Fixed =
        {
            new RefreshSite("WW2-Port-LaSpezia", "La Spezia", false, 0.241848463, -0.902317349, 0.356837108, 0),
            new RefreshSite("WW2-Port-Toulon", "Toulon", false, 0.560029462, -0.797889702, 0.223022476, 0),
            new RefreshSite("WW2-Port-Oran", "Oran", false, 0.731680739, 0.279121172, -0.621879946, 0),
            new RefreshSite("WW2-Port-Tripoli", "Tripoli", false, -0.097008599, 0.688288288, 0.718921807, 0),
            new RefreshSite("WW2-Port-Alexandria", "Alexandria", false, -0.192091142579102, 0.859672486172863, -0.473354211410465, -1.13989923804911),
            new RefreshSite("WW2-Port-Valencia", "Valencia", false, 0.707906168868626, -0.307714528162304, -0.635752015518312, -2.60071226354557),
            new RefreshSite("WW2-Port-Civitavecchia", "Civitavecchia", false, 0.106072219319843, -0.699055920364547, 0.707155926576197, 0.174443636846263),
            new RefreshSite("WW2-Airfield-Gibraltar", "Gibraltar", true, 0.405268480561213, 0.29884657438491, -0.863972328053399, 2.38300709220857),
            new RefreshSite("WW2-Airfield-Bizerte", "Bizerte", true, 0.463732953259739, 0.0435520585467395, 0.884903930524293, 2.92544832559361),
            new RefreshSite("WW2-Airfield-Tripoli", "Tripoli", true, -0.0858730279300504, 0.699611068160377, 0.709344892405395, 0.0645265073144401),
            new RefreshSite("WW2-Airfield-Foggia", "Foggia", true, -0.433940790701937, -0.615315371008138, 0.658089951576603, 2.44283087217264),
            new RefreshSite("WW2-Airfield-Casablanca", "Casablanca", true, 0.132644238359419, 0.637365737443778, -0.759058905990042, 0.383512724933098),
            new RefreshSite("WW2-Airfield-Benghazi", "Benghazi", true, -0.609556573339072, 0.782094995327427, 0.129492093128965, -0.849765043556545),
        };

        // Ports and airfields, then the open depots.
        public static readonly RefreshSite[] All = Build();

        private static RefreshSite[] Build()
        {
            var list = new List<RefreshSite>(Fixed);
            try
            {
                CheckDepots();
            }
            catch (Exception e)
            {
                foreach (var d in Depots)
                {
                    d.Open = false;
                    d.Problem = "check failed: " + e.Message;
                }
            }
            foreach (var d in Depots)
                if (d.Open)
                    list.Add(new RefreshSite(d.Group, d.Name, d.Coords));
            return list.ToArray();
        }

        private static void CheckDepots()
        {
            var texts = new Dictionary<string, string>();
            foreach (var def in MyDefinitionManager.Static.GetEntityComponentDefinitions())
            {
                var id = def != null ? def.Id.SubtypeName : null;
                if (id != null && id.StartsWith("WW2-SpawnConditions-Depot-", StringComparison.Ordinal))
                    texts[id] = def.DescriptionText ?? "";
            }
            bool prefab = MyDefinitionManager.Static.GetPrefabDefinition(DepotPrefab) != null;
            var registered = MesStaticPositions();
            var problems = new List<string>();
            foreach (var d in Depots)
            {
                problems.Clear();
                string text;
                if (!texts.TryGetValue("WW2-SpawnConditions-Depot-" + d.Name, out text))
                    text = null;
                bool on = text != null && Tag(text, "[StaticEncounter:") == "true";
                d.Coords = Vec(text, "[StaticEncounterCoords:");
                if (text == null)
                    problems.Add("no spawn conditions WW2-SpawnConditions-Depot-" + d.Name);
                else if (!on)
                    problems.Add("[StaticEncounter:false]");
                if (d.Coords == Vector3D.Zero || Vec(text, "[TriggerCoords:") == Vector3D.Zero)
                    problems.Add(on ? "WARNING: static encounter on with empty coordinates; MES keeps the first position it registers" : "coordinates empty");
                if (Vec(text, "[StaticEncounterForward:") == Vector3D.Zero || Vec(text, "[StaticEncounterUp:") == Vector3D.Zero)
                    problems.Add("forward/up empty");
                if (text != null && Tag(text, "[StaticEncounterUsePlanetDirectionAndAltitude:") != "false")
                    problems.Add("[StaticEncounterUsePlanetDirectionAndAltitude:] must be false");
                if (!prefab)
                    problems.Add("no prefab " + DepotPrefab);
                Vector3D saved;
                if (on && d.Coords != Vector3D.Zero && registered.TryGetValue(d.Group, out saved) && Vector3D.Distance(saved, d.Coords) > 2)
                    problems.Add("MES REGISTERED THIS DEPOT AT OLD COORDINATES " + Show(saved) + " and never re-reads them (NpcManager.cs:127). "
                        + "Recovery: set [StaticEncounter:false], save and reload, then set it true, save and reload");
                d.Open = problems.Count == 0;
                d.Problem = string.Join(", ", problems);
            }
        }

        // MES's saved static encounters (sandbox MES-ActiveStaticEncounters): group -> the exact position it will spawn at.
        private static Dictionary<string, Vector3D> MesStaticPositions()
        {
            var result = new Dictionary<string, Vector3D>();
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable("MES-ActiveStaticEncounters", out text) || string.IsNullOrEmpty(text))
                    return result;
                var list = MyAPIGateway.Utilities.SerializeFromBinary<List<StaticEncounterLite>>(Convert.FromBase64String(text));
                if (list != null)
                    foreach (var e in list)
                        if (e != null && e.IsValid && e.SpawnGroupName != null)
                            result[e.SpawnGroupName] = e.ExactLocationCoords;
            }
            catch (Exception)
            {
            }
            return result;
        }

        private static string Show(Vector3D v)
        {
            return "{X:" + v.X.ToString("0.#") + " Y:" + v.Y.ToString("0.#") + " Z:" + v.Z.ToString("0.#") + "}";
        }

        // Closed depots and why, for /witm supply and the /witm refresh dry run.
        public static string ClosedDepotLines()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var d in Depots)
                if (!d.Open)
                    sb.Append("Depot ").Append(d.Name).Append(": closed, ").Append(d.Problem).Append('\n');
            return sb.ToString();
        }

        // A tag's value up to its closing bracket, lower-cased and trimmed, or null.
        private static string Tag(string text, string tag)
        {
            if (text == null)
                return null;
            int i = text.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return null;
            int start = i + tag.Length;
            int end = text.IndexOf(']', start);
            return end < 0 ? null : text.Substring(start, end - start).Trim().ToLowerInvariant();
        }

        private static Vector3D Vec(string text, string tag)
        {
            if (text == null)
                return Vector3D.Zero;
            int i = text.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return Vector3D.Zero;
            int start = i + tag.Length;
            int end = text.IndexOf('}', start);
            if (end < 0)
                return Vector3D.Zero;
            Vector3D v;
            return Vector3D.TryParse(text.Substring(start, end - start).Trim().TrimStart('{'), out v) ? v : Vector3D.Zero;
        }

        public static RefreshSite Find(string group)
        {
            foreach (var s in All)
                if (s.Group == group)
                    return s;
            return null;
        }
    }

    // Read-only mirror of the fields WitM needs from MES's NpcData (World/NpcData.cs, ProtoMember numbers must match).
    // MES writes it base64 to the biggest grid of each spawned prefab under StorageTools.NpcDataKey (PrefabSpawner.GridsSpawned).
    // protobuf skips the fields not declared here.
    [ProtoContract]
    public class NpcDataLite
    {
        [ProtoMember(4)] public string SpawnGroupName;
        [ProtoMember(7)] public string OriginalPrefabId;
        [ProtoMember(11)] public Vector3D StartCoords;
        [ProtoMember(47)] public Dictionary<string, int> CustomCountersVariables;
    }

    // Saved in the sandbox variable RefreshSession.StateVariable: the last run's report and which sites it finished.
    // AwaitReload: "group|owner" of sites reset in a session where MES could not re-arm them; respawned after the next load.
    // Defaults must be false/0/empty: protobuf does not write them, so another initializer would win on load.
    [ProtoContract]
    public class RefreshState
    {
        [ProtoMember(1)] public List<string> Lines = new List<string>();
        [ProtoMember(2)] public List<string> Done = new List<string>();
        [ProtoMember(3)] public bool Unfinished;
        [ProtoMember(4)] public string InProgress = "";
        [ProtoMember(5)] public long StartedUtcTicks;
        [ProtoMember(6)] public List<string> Pending = new List<string>();
        [ProtoMember(7)] public string RecoveryNote = "";
        [ProtoMember(8)] public List<string> AwaitReload = new List<string>();
    }

    // Read-only mirror of MES's StaticEncounter (Spawning/StaticEncounter.cs) as saved in the sandbox variable
    // MES-ActiveStaticEncounters (World/NpcManager.cs UpdateStaticEncounters). ProtoMember numbers must match.
    [ProtoContract]
    public class StaticEncounterLite
    {
        [ProtoMember(2)] public string SpawnGroupName;
        [ProtoMember(14)] public Vector3D ExactLocationCoords;
        [ProtoMember(23)] public bool IsValid;
    }
}
