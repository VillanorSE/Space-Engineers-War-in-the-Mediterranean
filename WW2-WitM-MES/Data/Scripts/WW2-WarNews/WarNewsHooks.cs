using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // Turns existing MES behavior actions into news (no SBC changes needed: MES reports every action it runs), and offers
    // custom actions for MES Events. Action ids are the WitM-MES SubtypeIds; keep this list in step when they are renamed.
    //
    //   WW2-Action-Airfield-{Unowned|Gray|Green}-Become{Gray|Green|Unowned}   Triggers-Airfield-Ownership.sbc
    //   WW2-WarLevel-Action-Level{Up|Down}{2..5}-Announce                    War-Level-Points.sbc (Civitavecchia only)
    //   WW2-Territory-Action-Radius{Grow|Shrink}{1..6}-{Anchor}              Triggers-Zone-Change.sbc
    //   WW2-Territory-Action-Loss-{Gray|Green}-{Type}                        Territory-Points.sbc (selected types only)
    //   WW2-Territory-Action-Delivery-{Gray|Green}-{Origin}-{Destination}    Territory-Points.sbc (cargo ships)
    //   WW2-Action-CarrierSpawn-{Aquila|Bearn}                               Triggers-CarrierSpawn.sbc
    //   WW2-Action-{BaseStrike|Retaliation}-OnSpawn-{Gray|Green}             Triggers-BaseStrike.sbc
    //
    // Custom actions for MES Events (arguments in MES order: strings, then ints, then Vector3D):
    //   WitM-AddNews     string text [, string faction]
    //   WitM-CreateGPS   string name, string description, int minutes, Vector3D position   (countdown marker, key = name)
    //   WitM-CreateMarker same arguments, plain marker without countdown
    //   WitM-RemoveGPS   string name
    public static class WarNewsHooks
    {
        private const double ConvoyGpsMinutes = 60;
        private const double CarrierGpsMinutes = 30;
        private static readonly Color ConvoyColor = new Color(120, 190, 255);
        private static readonly Color CarrierColor = new Color(255, 90, 60);
        private static readonly Color EventColor = new Color(255, 178, 96);

        private static readonly Dictionary<string, string> ShipTypes = new Dictionary<string, string>
        {
            { "Carrier", "aircraft carrier" }, { "Battleship", "battleship" }, { "HeavyCruiser", "heavy cruiser" },
            { "Cruiser", "cruiser" }, { "Destroyer", "destroyer" },
        };

        private static readonly Dictionary<string, double> lastByKey = new Dictionary<string, double>();
        private static Action<IMyRemoteControl, string, string, IMyEntity, Vector3D> watcher;

        public static void Register(MesApiLite mes)
        {
            watcher = OnAction;
            mes.BehaviorTriggerActivationWatcher(true, watcher);
            mes.RegisterCustomAction(true, "WitM-AddNews", AddNews);
            mes.RegisterCustomAction(true, "WitM-CreateGPS", CreateCountdown);
            mes.RegisterCustomAction(true, "WitM-CreateMarker", CreateMarker);
            mes.RegisterCustomAction(true, "WitM-RemoveGPS", RemoveGps);
            WarNewsSession.Log("MES hooks registered" + (mes.HasWatcher ? "" : " (no trigger watcher: behavior news is off)") + (mes.HasCustomActions ? "" : " (no custom actions)"));
        }

        public static void Unregister(MesApiLite mes)
        {
            if (mes == null)
                return;
            if (watcher != null)
                mes.BehaviorTriggerActivationWatcher(false, watcher);
            mes.RegisterCustomAction(false, "WitM-AddNews", null);
            mes.RegisterCustomAction(false, "WitM-CreateGPS", null);
            mes.RegisterCustomAction(false, "WitM-CreateMarker", null);
            mes.RegisterCustomAction(false, "WitM-RemoveGPS", null);
            watcher = null;
            lastByKey.Clear();
        }

        // ---------------- behavior actions ----------------

        private static void OnAction(IMyRemoteControl rc, string triggerId, string actionId, IMyEntity target, Vector3D waypoint)
        {
            try
            {
                if (rc == null || actionId == null || !actionId.StartsWith("WW2-", StringComparison.Ordinal))
                    return;
                Handle(rc, actionId);
            }
            catch (Exception e)
            {
                WarNewsSession.Log("action " + actionId + ": " + e);
            }
        }

        private static void Handle(IMyRemoteControl rc, string id)
        {
            var pos = rc.GetPosition();
            string rest;

            if (Cut(id, "WW2-Action-Airfield-", out rest))
            {
                // {From}-Become{To}
                var parts = rest.Split('-');
                if (parts.Length != 2 || !parts[1].StartsWith("Become", StringComparison.Ordinal))
                    return;
                var from = parts[0];
                var to = parts[1].Substring(6);
                var airfield = WarNewsPlaces.NearestAirfield(pos);
                if (from == "Unowned")
                    WarNews.Post(WarNewsText.AirfieldTaken(to, airfield), to);
                else if (to == "Unowned")
                    WarNews.Post(WarNewsText.AirfieldAbandoned(from, airfield), from);
                else
                    WarNews.Post(WarNewsText.AirfieldCaptured(to, from, airfield), to);
                return;
            }

            if (Cut(id, "WW2-WarLevel-Action-Level", out rest))
            {
                // Up{n}-Announce or Down{n}-Announce; only the Announce actions post, so a change is reported once.
                if (!rest.EndsWith("-Announce", StringComparison.Ordinal))
                    return;
                rest = rest.Substring(0, rest.Length - 9);
                int n;
                if (rest.StartsWith("Up", StringComparison.Ordinal) && int.TryParse(rest.Substring(2), out n))
                    WarNews.Post(WarNewsText.WarLevelUp(n));
                else if (rest.StartsWith("Down", StringComparison.Ordinal) && int.TryParse(rest.Substring(4), out n))
                    WarNews.Post(WarNewsText.WarLevelDown(n - 1));
                return;
            }

            if (Cut(id, "WW2-Territory-Action-Radius", out rest))
            {
                // Grow{k}-{Anchor}: tier k -> k+1, radius (k+1)*5 km. Shrink{k}-{Anchor}: tier k+1 -> k, radius k*5 km.
                var parts = rest.Split('-');
                if (parts.Length != 2)
                    return;
                bool grow = parts[0].StartsWith("Grow", StringComparison.Ordinal);
                int k;
                if (!int.TryParse(parts[0].Substring(grow ? 4 : 6), out k))
                    return;
                var faction = WarNewsPlaces.AnchorFaction(parts[1]);
                var anchor = WarNewsPlaces.AnchorName(parts[1]);
                if (grow)
                    WarNews.Post(WarNewsText.FrontAdvances(faction, anchor, (k + 1) * 5), faction);
                else
                    WarNews.Post(WarNewsText.FrontRetreats(faction, anchor, k * 5), faction);
                return;
            }

            if (Cut(id, "WW2-Territory-Action-Loss-", out rest))
            {
                // {Faction}-{Type}
                var parts = rest.Split('-');
                if (parts.Length != 2)
                    return;
                var faction = parts[0];
                var type = parts[1];
                var place = WarNewsPlaces.Nearest(pos);
                string shipType;
                if (ShipTypes.TryGetValue(type, out shipType))
                    WarNews.Post(WarNewsText.WarshipLost(faction, shipType, GridName(rc), place), faction);
                else if (type == "Factory")
                    WarNews.Post(WarNewsText.FactoryLost(faction, place), faction);
                else if (type == "Hangar")
                {
                    // Hangars and garages share WW2-Behavior-HangarGarage-* and the Hangar points group; their prefab grids are named "Hangar" / "Garage".
                    var gridName = rc.CubeGrid != null ? rc.CubeGrid.CustomName ?? "" : "";
                    if (gridName.IndexOf("Garage", StringComparison.OrdinalIgnoreCase) >= 0)
                        WarNews.Post(WarNewsText.GarageLost(faction, place), faction);
                    else
                        WarNews.Post(WarNewsText.HangarLost(faction, place), faction);
                }
                else if (type == "UtilityNaval")
                {
                    // Only cargo ships score as UtilityNaval (Paths-Nautical.sbc).
                    WarNews.Post(WarNewsText.ConvoySunk(faction, place), faction);
                    WarNews.SetMarker("convoy-" + rc.CubeGrid.EntityId, WarNewsText.ConvoySunkGpsName + " near " + place,
                        WarNewsText.ConvoySunkGpsDescription(faction), pos, ConvoyGpsMinutes, false, ConvoyColor);
                }
                return;
            }

            if (Cut(id, "WW2-Territory-Action-Delivery-", out rest))
            {
                // {Faction}-{Origin}-{Destination}; the air cargo action (Delivery-AirCargo-{Faction}) has a different shape and is skipped.
                var parts = rest.Split('-');
                if (parts.Length != 3 || (parts[0] != "Gray" && parts[0] != "Green"))
                    return;
                WarNews.Post(WarNewsText.ConvoyArrived(parts[0], WarNewsPlaces.AnchorName(parts[1]), WarNewsPlaces.AnchorName(parts[2])), parts[0]);
                return;
            }

            if (Cut(id, "WW2-Action-CarrierSpawn-", out rest))
            {
                // Fires on every launch; report each carrier at most once per GPS lifetime.
                var key = "carrier-" + rc.CubeGrid.EntityId;
                if (!Allow(key, CarrierGpsMinutes * 60))
                    return;
                var faction = rest == "Aquila" ? "Gray" : "Green";
                var name = GridName(rc);
                if (string.IsNullOrEmpty(name))
                    name = rest;
                WarNews.Post(WarNewsText.CarrierSighted(faction, name, WarNewsPlaces.Nearest(pos)), faction);
                WarNews.SetMarker(key, WarNewsText.CarrierGpsName(name), WarNewsText.CarrierGpsDescription(faction), pos, CarrierGpsMinutes, false, CarrierColor);
                return;
            }

            if (Cut(id, "WW2-Action-BaseStrike-OnSpawn-", out rest))
            {
                WarNews.Post(WarNewsText.BaseStrike(rest, WarNewsPlaces.Nearest(pos)), rest);
                return;
            }

            if (Cut(id, "WW2-Action-Retaliation-OnSpawn-", out rest))
            {
                WarNews.Post(WarNewsText.Retaliation(rest, WarNewsPlaces.Nearest(pos)), rest);
            }
        }

        // ---------------- MES Event custom actions ----------------

        private static void AddNews(object[] args)
        {
            try
            {
                var strings = Strings(args);
                if (strings.Count > 0)
                    WarNews.Post(strings[0], strings.Count > 1 ? strings[1] : "");
            }
            catch (Exception e)
            {
                WarNewsSession.Log("WitM-AddNews: " + e);
            }
        }

        private static void CreateCountdown(object[] args)
        {
            CreateGps(args, true);
        }

        private static void CreateMarker(object[] args)
        {
            CreateGps(args, false);
        }

        private static void CreateGps(object[] args, bool countdown)
        {
            try
            {
                var strings = Strings(args);
                int minutes = 0;
                Vector3D position = Vector3D.Zero;
                bool havePosition = false;
                foreach (var a in args)
                {
                    if (a is int) minutes = (int)a;
                    else if (a is Vector3D) { position = (Vector3D)a; havePosition = true; }
                }
                if (strings.Count < 1 || minutes <= 0 || !havePosition)
                {
                    WarNewsSession.Log("WitM-CreateGPS/Marker: needs a name, minutes > 0 and a position");
                    return;
                }
                WarNews.SetMarker(strings[0], strings[0], strings.Count > 1 ? strings[1] : "", position, minutes, countdown, EventColor);
            }
            catch (Exception e)
            {
                WarNewsSession.Log("WitM-CreateGPS: " + e);
            }
        }

        private static void RemoveGps(object[] args)
        {
            try
            {
                var strings = Strings(args);
                if (strings.Count > 0)
                    WarNews.RemoveMarker(strings[0]);
            }
            catch (Exception e)
            {
                WarNewsSession.Log("WitM-RemoveGPS: " + e);
            }
        }

        // ---------------- helpers ----------------

        private static List<string> Strings(object[] args)
        {
            var list = new List<string>();
            if (args == null)
                return list;
            foreach (var a in args)
            {
                var s = a as string;
                if (s != null) list.Add(s);
            }
            return list;
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

        private static bool Allow(string key, double seconds)
        {
            var now = WarNewsSession.Now();
            double last;
            if (lastByKey.TryGetValue(key, out last) && now - last < seconds)
                return false;
            lastByKey[key] = now;
            return true;
        }

        // NPC grid names come from the prefab ("NPC-WW2-Lanciere", "NPC-WW2-Le_Triomphant"): strip the prefix, underscores to spaces.
        private static string GridName(IMyRemoteControl rc)
        {
            var name = rc.CubeGrid != null ? rc.CubeGrid.CustomName : null;
            if (string.IsNullOrWhiteSpace(name))
                return "";
            if (name.StartsWith("NPC-WW2-", StringComparison.Ordinal)) name = name.Substring(8);
            else if (name.StartsWith("NPC-", StringComparison.Ordinal)) name = name.Substring(4);
            return name.Replace('_', ' ').Trim();
        }
    }
}
