using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRageMath;

namespace WW2WitM.Supply
{
    // Supply truck convoys, port -> airfield (D-047) or vehicle depot (D-049; production repo design/war-layer-phase2-supply-convoys.md,
    // design/vehicle-depots.md). MES side:
    // Behavior/Convoys/*.sbc, Triggers-Convoy-Port.sbc, SpawnGroups-Convoys.sbc. A port's route trigger runs only while
    // WitM-ConvoyOpen-{Route} is true: the route is filled in (truck prefab, 5 start slots, road waypoints), the airfield exists
    // and is held by the port's side, and the port's Onward box can load the whole column. Each truck loads from its port's
    // Onward box when it spawns, credits the airfield on WW2-Convoy-Action-Delivered, and is refunded if MES removes it intact
    // first. While the airfield is not held by the route's side, WitM-ConvoyRecall-{truck spawn group} makes the trucks on the
    // road despawn (Convoy-Trucks.sbc), which refunds them.
    public class ConvoyRoute
    {
        public readonly string Id;
        public readonly string Port;
        // The site the trucks supply: WW2-Airfield-{name}, or WW2-Depot-{name} for a depot route (id {Port}-{name}Depot).
        public readonly string Destination;
        public readonly string Faction;
        public bool Ready;
        public bool Open;
        public string Problem = "";

        public ConvoyRoute(string port, string destination, string faction, bool depot = false)
        {
            Id = port + "-" + destination + (depot ? "Depot" : "");
            Port = "WW2-Port-" + port;
            Destination = (depot ? "WW2-Depot-" : "WW2-Airfield-") + destination;
            Faction = faction;
        }

        public string Prefab { get { return Faction == "Gray" ? SupplyConvoys.GrayTruck : SupplyConvoys.GreenTruck; } }
    }

    public static class SupplyConvoys
    {
        // D-047: a truck loads 4 from its port's Onward box and delivers 10.
        public const int TruckCost = 4, TruckDelivery = 10;

        public const string GrayTruck = "NPC-WW2-Fiat626-Convoy", GreenTruck = "NPC-WW2-RenaultAHN-Convoy";
        public const string GroupPrefix = "WW2-Convoy-";
        public const string OpenPrefix = "WitM-ConvoyOpen-";
        public const string RecallPrefix = "WitM-ConvoyRecall-";
        public const string DeliveredAction = "WW2-Convoy-Action-Delivered";
        public const int Slots = 5;

        // The owner's route list (D-047); every listed route runs while it is open. Keep in step with Triggers-Convoy-Port.sbc,
        // Convoy-Routes.sbc and SpawnGroups-Convoys.sbc (production repo tools/scripts/WW2-Tool-GenConvoys.py).
        // Tripoli-Tobruk stays closed until a WW2-Airfield-Tobruk site exists in RefreshSites.All; a depot route until its depot opens.
        public static readonly ConvoyRoute[] Routes =
        {
            new ConvoyRoute("Civitavecchia", "Foggia", "Gray"),
            new ConvoyRoute("Tripoli", "Bizerte", "Gray"),
            new ConvoyRoute("Tripoli", "Benghazi", "Gray"),
            new ConvoyRoute("Tripoli", "Tobruk", "Gray"),
            new ConvoyRoute("Toulon", "Gibraltar", "Green"),
            new ConvoyRoute("Oran", "Bizerte", "Green"),
            new ConvoyRoute("Alexandria", "Casablanca", "Green"),
            new ConvoyRoute("Alexandria", "Benghazi", "Green"),
            new ConvoyRoute("Tripoli", "Benghazi", "Gray", true),
            new ConvoyRoute("Alexandria", "Benghazi", "Green", true),
            new ConvoyRoute("Tripoli", "Tunis", "Gray", true),
            new ConvoyRoute("Oran", "Tunis", "Green", true),
            new ConvoyRoute("Tripoli", "Tobruk", "Gray", true),
            new ConvoyRoute("Alexandria", "Tobruk", "Green", true),
            new ConvoyRoute("Civitavecchia", "Abruzzo", "Gray", true),
            new ConvoyRoute("Oran", "Mascara", "Green", true),
            new ConvoyRoute("Tripoli", "Sirte", "Gray", true),
            new ConvoyRoute("Alexandria", "Sirte", "Green", true),
        };

        private static bool published;

        public static void Clear()
        {
            published = false;
        }

        public static ConvoyRoute Find(string id)
        {
            foreach (var r in Routes)
                if (r.Id == id)
                    return r;
            return null;
        }

        // Spawn group WW2-Convoy-{Route}-{slot} -> its route, or null.
        public static ConvoyRoute RouteOfGroup(string group)
        {
            if (group == null || !group.StartsWith(GroupPrefix, StringComparison.Ordinal))
                return null;
            int dash = group.LastIndexOf('-');
            if (dash <= GroupPrefix.Length)
                return null;
            return Find(group.Substring(GroupPrefix.Length, dash - GroupPrefix.Length));
        }

        // Once per load: a route is ready when its truck prefab exists, its Lead and Follow behaviors carry the same non-empty
        // [CustomWaypoints:] list and all 5 start slots have coordinates. Unready routes never open (a missing start waypoint would
        // otherwise reach MES's spawn task as null).
        public static void CheckReady()
        {
            var texts = new Dictionary<string, string>();
            foreach (var def in MyDefinitionManager.Static.GetEntityComponentDefinitions())
            {
                if (def == null || string.IsNullOrEmpty(def.DescriptionText))
                    continue;
                var id = def.Id.SubtypeName;
                if (id != null && (id.StartsWith("WW2-Behavior-Convoy-", StringComparison.Ordinal) || id.StartsWith("WW2-Convoy-Start-", StringComparison.Ordinal)))
                    texts[id] = def.DescriptionText;
            }
            int ready = 0;
            var problems = new List<string>();
            foreach (var r in Routes)
            {
                problems.Clear();
                if (SupplySites.Find(r.Destination) == null)
                    problems.Add(r.Destination.StartsWith("WW2-Depot-", StringComparison.Ordinal)
                        ? r.Destination + " is not open (position or prefab missing)" : "no airfield site " + r.Destination + " (RefreshSites)");
                if (MyDefinitionManager.Static.GetPrefabDefinition(r.Prefab) == null)
                    problems.Add("no prefab " + r.Prefab);
                int bad, badFollow;
                var lead = Waypoints(texts, "WW2-Behavior-Convoy-" + r.Id + "-Lead", out bad);
                var follow = Waypoints(texts, "WW2-Behavior-Convoy-" + r.Id + "-Follow", out badFollow);
                bad += badFollow;
                if (bad > 0)
                    problems.Add(bad + " road waypoint(s) MES cannot read (format {X:1 Y:2 Z:3},{X:..} with no spaces around commas)");
                if (lead.Count == 0)
                    problems.Add("no road waypoints");
                else if (!Same(lead, follow))
                    problems.Add("Follow waypoints differ from Lead");
                for (int k = 1; k <= Slots; k++)
                    if (Start(texts, "WW2-Convoy-Start-" + r.Id + "-" + k) == Vector3D.Zero)
                        problems.Add("start slot " + k + " empty");
                r.Ready = problems.Count == 0;
                r.Problem = r.Ready ? "" : string.Join(", ", problems);
                if (r.Ready)
                    ready++;
            }
            SupplySession.Log("convoys: " + ready + " of " + Routes.Length + " routes filled in");
        }

        // Parsed as MES does (Helpers/TagParse.cs TagVector3DListCheck + FixVectorString): no trimming, one brace stripped per side,
        // and an item Vector3D.TryParse rejects (a leading space, for one) is skipped silently by MES; counted here as bad.
        private static List<Vector3D> Waypoints(Dictionary<string, string> texts, string id, out int bad)
        {
            var list = new List<Vector3D>();
            bad = 0;
            var value = TagValue(texts, id, "[CustomWaypoints:");
            if (value == null)
                return list;
            foreach (var item in value.Split(','))
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;
                var s = item;
                if (s.Length > 0 && s[0] == '{')
                    s = s.Substring(1);
                if (s.Length > 0 && s[s.Length - 1] == '}')
                    s = s.Substring(0, s.Length - 1);
                Vector3D v;
                if (Vector3D.TryParse(s, out v))
                    list.Add(v);
                else
                    bad++;
            }
            return list;
        }

        private static Vector3D Start(Dictionary<string, string> texts, string id)
        {
            var value = TagValue(texts, id, "[Coordinates:");
            Vector3D v;
            if (value == null || !Vector3D.TryParse(value.Trim().Trim('{', '}'), out v))
                return Vector3D.Zero;
            return v;
        }

        // The text after the tag name up to the closing bracket on the same line, or null.
        private static string TagValue(Dictionary<string, string> texts, string id, string tag)
        {
            string text;
            if (!texts.TryGetValue(id, out text))
                return null;
            int i = text.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return null;
            int start = i + tag.Length;
            int end = text.IndexOf('\n', start);
            var line = (end < 0 ? text.Substring(start) : text.Substring(start, end - start)).TrimEnd();
            return line.EndsWith("]", StringComparison.Ordinal) ? line.Substring(0, line.Length - 1) : line;
        }

        private static bool Same(List<Vector3D> a, List<Vector3D> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
                if (Vector3D.DistanceSquared(a[i], b[i]) > 1)
                    return false;
            return true;
        }

        // Every 10 s: a ready route is open while its side holds the destination and the port's Onward box can load the column.
        // Its trucks are recalled while the side does not hold the destination.
        public static void Publish()
        {
            var session = SupplySession.Instance;
            if (session == null)
                return;
            int need = TruckCost * ConvoySize();
            foreach (var r in Routes)
            {
                bool was = r.Open;
                var airfield = session.Get(r.Destination);
                var port = session.Get(r.Port);
                bool held = airfield != null && airfield.Owner == r.Faction;
                r.Open = r.Ready && held && port != null && port.Onward >= need;
                if (r.Open != was && published)
                    SupplySession.Log("convoy route " + r.Id + (r.Open ? " open" : " closed"));
                var u = MyAPIGateway.Utilities;
                u.SetVariable(OpenPrefix + r.Id, r.Open);
                bool recall = airfield != null && !held;
                for (int k = 1; k <= Slots; k++)
                    u.SetVariable(RecallPrefix + GroupPrefix + r.Id + "-" + k, recall);
            }
            published = true;
        }

        // Trucks per convoy at the current War Level (slots 3-5 need WL3-5, SpawnConditions-Convoys.sbc): 2/2/3/4/5.
        public static int ConvoySize()
        {
            int level;
            if (!MyAPIGateway.Utilities.GetVariable("WarLevel", out level))
                level = 1;
            return Math.Max(2, Math.Min(Slots, level));
        }

        // For /witm supply: the routes that can supply an airfield or depot for its current owner.
        public static string RoutesTo(string airfield, string owner)
        {
            var sb = new StringBuilder();
            foreach (var r in Routes)
            {
                if (r.Destination != airfield || r.Faction != owner)
                    continue;
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(r.Id).Append(r.Open ? " (open)" : r.Ready ? " (waiting)" : " (not filled in)");
            }
            return sb.Length > 0 ? sb.ToString() : "none";
        }

        // Lines for /witm supply.
        public static void Describe(StringBuilder sb, int trucks)
        {
            int size = ConvoySize();
            sb.Append("\n\nConvoy routes (trucks on the road: ").Append(trucks).Append("; ").Append(size).Append(" trucks per convoy, needs ")
                .Append(TruckCost * size).Append(" onward at the port):\n");
            var session = SupplySession.Instance;
            foreach (var r in Routes)
            {
                sb.Append(r.Id).Append(" (").Append(r.Faction).Append("): ");
                if (!r.Ready)
                    sb.Append("not filled in: ").Append(r.Problem);
                else if (r.Open)
                    sb.Append("open");
                else
                {
                    var airfield = session != null ? session.Get(r.Destination) : null;
                    sb.Append("ready, closed: ").Append(airfield == null || airfield.Owner != r.Faction ? "destination not held" : "port onward too low");
                }
                sb.Append('\n');
            }
        }
    }
}
