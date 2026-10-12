using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace WW2WitM.Refresh
{
    // One WitM MES static encounter. Group is the SpawnGroup SubtypeId; Dir/Altitude are its conditions'
    // StaticEncounterPlanetDirection/Altitude, TriggerRadius its [TriggerRadius] (SpawnConditions-Ports.sbc,
    // SpawnConditions-Installations.sbc). Update the table when a site is added or moved: refresh, War News place names
    // (WarNewsPlaces) and supply (SupplySites) all read it.
    public class RefreshSite
    {
        public readonly string Group;
        public readonly string Name;
        public readonly bool Airfield;
        public readonly Vector3D Dir;
        public readonly double Altitude;

        public RefreshSite(string group, string name, bool airfield, double x, double y, double z, double altitude)
        {
            Group = group;
            Name = name;
            Airfield = airfield;
            Dir = new Vector3D(x, y, z);
            Altitude = altitude;
        }

        public string Label { get { return (Airfield ? "Airfield " : "Port ") + Name; } }
        public string Short { get { return Group.StartsWith("WW2-", System.StringComparison.Ordinal) ? Group.Substring(4) : Group; } }
        public double TriggerRadiusKm { get { return Airfield ? 20 : 65; } }
    }

    public static class RefreshSites
    {
        public static readonly RefreshSite[] All =
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
    }

    // Read-only mirror of the fields WitM needs from MES's NpcData (World/NpcData.cs, ProtoMember numbers must match).
    // MES writes it base64 to the biggest grid of each spawned prefab under StorageTools.NpcDataKey (PrefabSpawner.GridsSpawned).
    // protobuf skips the fields not declared here.
    [ProtoContract]
    public class NpcDataLite
    {
        [ProtoMember(4)] public string SpawnGroupName;
        [ProtoMember(11)] public Vector3D StartCoords;
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
        [ProtoMember(23)] public bool IsValid;
    }
}
