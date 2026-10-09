namespace WW2WitM.WarNewsSystem
{
    // Every player-facing string of the war news (D-036, wording approved by the owner 2026-10-06; source:
    // se-mod-production projects/war-in-the-mediterranean/design/news-and-strike-wording-draft.md).
    // Faction ids are "Gray" (Italian) and "Green" (French); headlines name the nation, not the faction colour.
    public static class WarNewsText
    {
        public const string LcdScriptName = "War News";
        public const string LcdTitle = "WAR BULLETIN";
        public const string LcdEmpty = "No dispatches yet.";

        // Adjective: "Italian bombers", "the French carrier Bearn".
        public static string Adj(string faction)
        {
            if (faction == "Gray") return "Italian";
            if (faction == "Green") return "French";
            return "unidentified";
        }

        // Plural noun for the side: "from the Italians", "from the French".
        public static string People(string faction)
        {
            if (faction == "Gray") return "the Italians";
            if (faction == "Green") return "the French";
            return "unidentified forces";
        }

        // "A"/"An" for the word that follows ("An Italian garage", "A French hangar").
        public static string A(string nextWord)
        {
            if (string.IsNullOrEmpty(nextWord)) return "A";
            return "AEIOUaeiou".IndexOf(nextWord[0]) >= 0 ? "An" : "A";
        }

        private static string AnAdj(string faction, bool lower = false)
        {
            var adj = Adj(faction);
            var article = A(adj);
            return (lower ? article.ToLowerInvariant() : article) + " " + adj;
        }

        public static string Stamp(int day, int hour, int minute)
        {
            return "Day " + day + ", " + hour.ToString("00") + ":" + minute.ToString("00");
        }

        // Airfields
        public static string AirfieldTaken(string newOwner, string airfield)
        {
            return "AIRFIELD OCCUPIED - " + Adj(newOwner) + " forces have moved into " + airfield + " airfield.";
        }

        public static string AirfieldCaptured(string newOwner, string oldOwner, string airfield)
        {
            return "AIRFIELD CHANGES HANDS - " + Adj(newOwner) + " forces have taken " + airfield + " airfield from " + People(oldOwner) + ".";
        }

        public static string AirfieldAbandoned(string oldOwner, string airfield)
        {
            return "AIRFIELD ABANDONED - " + Adj(oldOwner) + " forces have withdrawn from " + airfield + " airfield.";
        }

        // War Level (owner's wording, 2026-10-06)
        public static string WarLevelUp(int level)
        {
            switch (level)
            {
                case 2: return "THE WAR WIDENS - War Level 2. Both commands are committing more resources to the fight.";
                case 3: return "THE WAR INTENSIFIES - War Level 3. Strike aircraft have been deployed for action. Naval activity grows more deadly.";
                case 4: return "THE WAR INTENSIFIES - War Level 4. Planes have been ordered to take formations. Larger ships have been ordered to the area.";
                case 5: return "TOTAL WAR - War Level 5. Carriers and battleships have been deployed.";
                default: return "THE WAR INTENSIFIES - War Level " + level + ".";
            }
        }

        public static string WarLevelDown(int level)
        {
            return "THE FIGHTING EASES - War Level " + level + ".";
        }

        // Territory
        public static string FrontAdvances(string faction, string anchor, int km)
        {
            return "FRONT ADVANCES - " + Adj(faction) + " lines around " + anchor + " push out to " + km + " km.";
        }

        public static string FrontRetreats(string faction, string anchor, int km)
        {
            return "FRONT FALLS BACK - " + Adj(faction) + " lines around " + anchor + " pull back to " + km + " km.";
        }

        // Losses
        public static string WarshipLost(string faction, string shipType, string shipName, string place)
        {
            if (string.IsNullOrEmpty(shipName))
                return "WARSHIP SUNK - " + AnAdj(faction) + " " + shipType + " has gone down near " + place + ".";
            return "WARSHIP SUNK - The " + Adj(faction) + " " + shipType + " " + shipName + " has gone down near " + place + ".";
        }

        public static string FactoryLost(string faction, string place)
        {
            return "FACTORY DESTROYED - " + AnAdj(faction) + " aircraft factory near " + place + " has been destroyed.";
        }

        public static string HangarLost(string faction, string place)
        {
            return "HANGAR DESTROYED - " + AnAdj(faction) + " hangar near " + place + " has been destroyed.";
        }

        public static string GarageLost(string faction, string place)
        {
            return "GARAGE DESTROYED - " + AnAdj(faction) + " garage near " + place + " has been destroyed.";
        }

        public static string ConvoySunk(string faction, string place)
        {
            return "CONVOY SUNK - " + AnAdj(faction) + " supply ship has been sunk near " + place + ".";
        }

        public const string ConvoySunkGpsName = "Convoy sunk";

        public static string ConvoySunkGpsDescription(string faction)
        {
            return AnAdj(faction) + " supply ship went down here. It may be worth investigating.";
        }

        // Logistics
        public static string ConvoyArrived(string faction, string origin, string destination)
        {
            return "CONVOY ARRIVES - " + AnAdj(faction) + " supply ship from " + origin + " has docked at " + destination + ".";
        }

        // Carriers
        public static string CarrierSighted(string faction, string carrierName, string place)
        {
            return "CARRIER SIGHTED - The " + Adj(faction) + " carrier " + carrierName + " is launching aircraft near " + place + ".";
        }

        public static string CarrierGpsName(string carrierName)
        {
            return "Carrier sighted: " + carrierName;
        }

        public static string CarrierGpsDescription(string faction)
        {
            return "Last reported position of " + AnAdj(faction, true) + " aircraft carrier.";
        }

        // Strikes
        public static string BaseStrike(string faction, string place)
        {
            return "AIR RAID - " + Adj(faction) + " bombers are attacking an enemy outpost near " + place + ".";
        }

        public static string Retaliation(string faction, string place)
        {
            return "REPRISAL - " + Adj(faction) + " aircraft are hunting the forces who struck their installation near " + place + ".";
        }
    }
}
