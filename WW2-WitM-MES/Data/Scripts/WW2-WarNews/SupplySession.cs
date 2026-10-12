using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using WW2WitM.Refresh;
using WW2WitM.WarNewsSystem;

// WW2-WitM supply, war layer phase 1 (D-035, D-045; production repo design/war-layer-phase1-build-plan.md).
// Server only. Each port and airfield holds a stock of supply, saved per site so it survives /witm refresh and respawns.
// Hourly: upkeep, and each port that is not Low feeds the owned same-side airfields in its territory zone for which it is the
// nearest such port. Cargo ships and air cargo deliver; airfield sorties cost supply and are refunded if they come back.
// MES reads the result through per-site sandbox booleans (Supply-Conditions.sbc) and two store price counters.
//   /witm supply                       table of sites, stock, band, owner and feeder
//   /witm supply set <site> <n>        set a site's stock
//   /witm supply hour                  run one hourly tick now
namespace WW2WitM.Supply
{
    // Entry points for later war-layer phases (reserves, battles). Server only; return defaults elsewhere.
    public static class SupplyApi
    {
        public static int GetStock(string site)
        {
            var s = SupplySession.Instance != null ? SupplySession.Instance.Get(site) : null;
            return s != null ? s.Stock : 0;
        }

        public static int GetBand(string site)
        {
            var s = SupplySession.Instance != null ? SupplySession.Instance.Get(site) : null;
            return s != null ? s.Band : SupplySites.Normal;
        }

        public static string GetOwner(string site)
        {
            var s = SupplySession.Instance != null ? SupplySession.Instance.Get(site) : null;
            return s != null ? s.Owner : "";
        }

        // Adds (or with a negative amount removes) supply, clamped to 0..capacity. Returns false if the site is unknown.
        public static bool Add(string site, int amount, string reason)
        {
            return SupplySession.Instance != null && SupplySession.Instance.Change(site, amount, reason);
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SupplySession : MySessionComponentBase
    {
        public const string StateVariable = "WW2-Supply-State";
        public const string OfferPctVariable = "WitM-StoreOfferPct";
        public const string OrderPctVariable = "WitM-StoreOrderPct";
        public const string DefensePrefix = "WitM-SupplyDefense-";
        public const string OffensePrefix = "WitM-SupplyOffense-";
        public const string LowPrefix = "WitM-SupplyLow-";
        public const string NormalPrefix = "WitM-SupplyNormal-";
        public const string FullPrefix = "WitM-SupplyFull-";
        public const string StockPrefix = "WitM-Supply-";
        private const string GoodsStoreName = "Store-Port-Goods";
        private const int StateVersion = 1;
        private const int MaxCatchUpHours = 3;
        private const double NewsGapSeconds = 7200;
        private const int MaintainTicks = 600;

        private struct StockPass
        {
            public long GridId;
            public double Multiplier;
        }

        public static SupplySession Instance;

        private bool isServer;
        private bool started;
        private int tick;
        private bool storeCountersSet;
        private SupplyState state = new SupplyState();
        private readonly Dictionary<string, SiteStock> sites = new Dictionary<string, SiteStock>();
        private readonly HashSet<string> createdThisLoad = new HashSet<string>();
        private readonly List<KeyValuePair<SiteStock, SiteStock>> feeds = new List<KeyValuePair<SiteStock, SiteStock>>();
        private readonly List<StockPass> stockPasses = new List<StockPass>();
        private readonly List<IMyCubeGrid> gridBuffer = new List<IMyCubeGrid>();
        private readonly List<IMySlimBlock> blockBuffer = new List<IMySlimBlock>();
        private readonly List<IMyStoreItem> storeItems = new List<IMyStoreItem>();

        public SupplyState State { get { return state; } }

        public override void LoadData()
        {
            Instance = this;
            isServer = MyAPIGateway.Multiplayer.IsServer;
        }

        // After LoadData so the saved game clock and sandbox variables are restored.
        public override void BeforeStart()
        {
            if (!isServer)
                return;
            LoadState();
            EnsureSites();
            foreach (var s in state.Sites)
                Publish(s);
        }

        protected override void UnloadData()
        {
            try
            {
                if (started) SupplyHooks.Unregister(Mes());
                SupplySites.Clear();
            }
            catch (Exception e)
            {
                Log("unload: " + e);
            }
            Instance = null;
        }

        public override void SaveData()
        {
            if (isServer) SaveState();
        }

        public override void UpdateAfterSimulation()
        {
            if (!isServer)
                return;
            try
            {
                tick++;
                if (storeCountersSet)
                {
                    MyAPIGateway.Utilities.SetVariable(OfferPctVariable, 100);
                    MyAPIGateway.Utilities.SetVariable(OrderPctVariable, 100);
                    storeCountersSet = false;
                }
                if (stockPasses.Count > 0)
                    ApplyStockPasses();

                if (!started)
                {
                    var mes = Mes();
                    if (mes == null || !mes.Ready)
                        return;
                    started = true;
                    SupplyHooks.Register(mes);
                    ScanOwners();
                    int ports = 0, airfields = 0;
                    foreach (var s in state.Sites)
                    {
                        if (SupplySites.IsPort(s.Site)) ports++;
                        else airfields++;
                    }
                    Log("loaded, " + ports + " ports " + airfields + " airfields" + (mes.HasSpawnApi ? "" : " (MES spawn API missing: sorties are not charged)"));
                }

                if (tick % MaintainTicks == 0)
                {
                    SupplyHooks.Maintain();
                    CheckHour();
                }
            }
            catch (Exception e)
            {
                Log("update: " + e);
            }
        }

        // ---------------- sites ----------------

        public SiteStock Get(string site)
        {
            SiteStock s;
            return site != null && sites.TryGetValue(site, out s) ? s : null;
        }

        private void EnsureSites()
        {
            sites.Clear();
            foreach (var s in state.Sites)
                if (s != null && !string.IsNullOrEmpty(s.Site))
                    sites[s.Site] = s;
            foreach (var site in SupplySites.Tracked)
            {
                SiteStock s;
                if (!sites.TryGetValue(site.Group, out s))
                {
                    s = new SiteStock { Site = site.Group, Stock = SupplySites.Start(site.Group) };
                    s.Band = SupplySites.ComputeBand(site.Group, s.Stock, SupplySites.Normal);
                    state.Sites.Add(s);
                    sites[site.Group] = s;
                    createdThisLoad.Add(site.Group);
                }
                if (SupplySites.IsPort(site.Group))
                {
                    s.Owner = SupplySites.PortFaction(site.Group);
                    s.LastOwner = s.Owner;
                    s.EverOwned = true;
                }
            }
        }

        // Airfield owners from the live grids' "Airfield Block" (as /witm refresh reads them). New sites take the owner as found.
        private void ScanOwners()
        {
            var seen = new HashSet<string>();
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                var grid = e as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || grid.Storage == null)
                    return false;
                var site = SupplySites.SiteOf(grid);
                if (site.Length == 0 || SupplySites.IsPort(site) || seen.Contains(site))
                    return false;
                var owner = AirfieldOwner(grid);
                if (owner == null)
                    return false;
                seen.Add(site);
                var s = Get(site);
                if (s == null)
                    return false;
                if (createdThisLoad.Contains(site))
                {
                    s.Owner = owner;
                    s.LastOwner = owner;
                    s.EverOwned = owner.Length > 0;
                }
                else if (s.Owner != owner)
                {
                    Log(site + ": owner was " + Show(s.Owner) + " in the saved supply state, the grid says " + Show(owner));
                    OwnerChanged(site, owner);
                }
                return false;
            });
            createdThisLoad.Clear();
            foreach (var s in state.Sites)
                Publish(s);
        }

        // "Gray", "Green", "" (UNOWNED or another faction), or null if the grid has no Airfield Block.
        private string AirfieldOwner(IMyCubeGrid grid)
        {
            gridBuffer.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, gridBuffer);
            if (gridBuffer.Count == 0)
                gridBuffer.Add(grid);
            blockBuffer.Clear();
            foreach (var g in gridBuffer)
            {
                g.GetBlocks(blockBuffer, IsAirfieldBlock);
                if (blockBuffer.Count > 0)
                    break;
            }
            gridBuffer.Clear();
            if (blockBuffer.Count == 0)
                return null;
            var owner = blockBuffer[0].OwnerId;
            blockBuffer.Clear();
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
            if (faction == null) return "";
            if (faction.Tag == "GRAY") return "Gray";
            if (faction.Tag == "GREEN") return "Green";
            return "";
        }

        private static bool IsAirfieldBlock(IMySlimBlock block)
        {
            var terminal = block.FatBlock as IMyTerminalBlock;
            return terminal != null && terminal.CustomName == "Airfield Block";
        }

        // An airfield changed hands (WW2-Action-Airfield-{From}-Become{To}); to is "Gray", "Green" or "" for unowned.
        // Captured from the enemy, or taken from unowned by a new owner: starts Low. The first claim after the site first
        // spawns and a re-take by the last owner (/witm refresh, respawn) keep the stock. Unowned: stock frozen.
        public void OwnerChanged(string site, string to)
        {
            var s = Get(site);
            if (s == null || SupplySites.IsPort(site) || s.Owner == to)
                return;
            if (to.Length == 0)
            {
                s.LastOwner = s.Owner;
                s.Owner = "";
                Log(site + ": abandoned, stock " + s.Stock + " frozen");
            }
            else
            {
                bool keep = s.Owner.Length == 0 && (!s.EverOwned || s.LastOwner == to);
                if (!keep)
                    s.Stock = SupplySites.CapturedStart;
                Log(site + ": now " + to + (keep ? ", stock kept at " + s.Stock : ", captured, stock " + s.Stock));
                s.Owner = to;
                s.LastOwner = to;
                s.EverOwned = true;
            }
            UpdateSite(s);
        }

        public bool Change(string site, int amount, string reason)
        {
            var s = Get(site);
            if (s == null)
                return false;
            Set(s, s.Stock + amount);
            if (!string.IsNullOrEmpty(reason))
                Log(site + ": " + (amount >= 0 ? "+" : "") + amount + " " + reason + ", now " + s.Stock);
            UpdateSite(s);
            return true;
        }

        private static void Set(SiteStock s, int stock)
        {
            s.Stock = Math.Max(0, Math.Min(SupplySites.Cap(s.Site), stock));
        }

        // Recomputes the band (posting news on a change into or out of Low) and publishes the site's sandbox values.
        private void UpdateSite(SiteStock s)
        {
            var band = SupplySites.ComputeBand(s.Site, s.Stock, s.Band);
            if (band != s.Band)
            {
                bool wasLow = s.Band == SupplySites.Low;
                s.Band = band;
                if (band == SupplySites.Low)
                    NewsLow(s);
                else if (wasLow)
                    NewsRecovered(s);
            }
            Publish(s);
        }

        private void NewsLow(SiteStock s)
        {
            var now = WarNewsSession.Now();
            if (s.NewsLow || s.Owner.Length == 0 || (s.LastNewsSeconds > 0 && now - s.LastNewsSeconds < NewsGapSeconds))
                return;
            if (WarNews.Post(WarNewsText.SupplyLow(s.Owner, NewsName(s.Site)), s.Owner))
            {
                s.NewsLow = true;
                s.LastNewsSeconds = now;
            }
        }

        private void NewsRecovered(SiteStock s)
        {
            if (!s.NewsLow)
                return;
            s.NewsLow = false;
            WarNews.Post(WarNewsText.SupplyRecovered(NewsName(s.Site)), s.Owner);
        }

        private static string NewsName(string group)
        {
            var site = SupplySites.Find(group);
            if (site == null)
                return group;
            return site.Airfield ? site.Name + " airfield" : site.Name;
        }

        // Read by MES trigger conditions through {SpawnGroupName} (ConditionProfile.cs CheckTrueSandboxBooleans). Saved with the world.
        private static void Publish(SiteStock s)
        {
            var u = MyAPIGateway.Utilities;
            u.SetVariable(StockPrefix + s.Site, s.Stock);
            u.SetVariable(LowPrefix + s.Site, s.Band == SupplySites.Low);
            u.SetVariable(NormalPrefix + s.Site, s.Band == SupplySites.Normal);
            u.SetVariable(FullPrefix + s.Site, s.Band == SupplySites.Full);
            if (SupplySites.IsPort(s.Site))
                return;
            u.SetVariable(DefensePrefix + s.Site, s.Stock >= SupplySites.DefenseCost);
            u.SetVariable(OffensePrefix + s.Site, s.Band != SupplySites.Low);
        }

        // ---------------- hourly flows ----------------

        private void CheckHour()
        {
            var now = WarNewsSession.Now();
            if (state.LastHourSeconds <= 0 || now < state.LastHourSeconds)
            {
                state.LastHourSeconds = now;
                return;
            }
            int hours = (int)((now - state.LastHourSeconds) / 3600);
            if (hours <= 0)
                return;
            state.LastHourSeconds += hours * 3600.0;
            for (int i = 0; i < Math.Min(hours, MaxCatchUpHours); i++)
                HourTick();
        }

        public void HourTick()
        {
            // Feeding uses the bands as they stood at the start of the hour.
            feeds.Clear();
            foreach (var s in state.Sites)
            {
                if (SupplySites.IsPort(s.Site) || s.Owner.Length == 0 || SupplySites.Find(s.Site) == null)
                    continue;
                var port = FeederOf(s);
                if (port != null && port.Band != SupplySites.Low)
                    feeds.Add(new KeyValuePair<SiteStock, SiteStock>(s, port));
            }

            foreach (var s in state.Sites)
            {
                if (SupplySites.Find(s.Site) == null)
                    continue;
                if (SupplySites.IsPort(s.Site))
                    Set(s, s.Stock - SupplySites.PortUpkeep);
                else if (s.Owner.Length > 0)
                    Set(s, s.Stock - SupplySites.AirfieldUpkeep);
            }

            foreach (var f in feeds)
            {
                var airfield = f.Key;
                var port = f.Value;
                int amount = Math.Min(SupplySites.Feed, Math.Min(port.Stock, SupplySites.AirfieldCap - airfield.Stock));
                if (amount <= 0)
                    continue;
                port.Stock -= amount;
                airfield.Stock += amount;
            }
            feeds.Clear();

            foreach (var s in state.Sites)
                if (SupplySites.Find(s.Site) != null)
                    UpdateSite(s);
            Log("hour: " + Summary());
        }

        // The nearest same-side supply port whose territory zone covers the airfield (D-045: fed by that port only), or null.
        public SiteStock FeederOf(SiteStock airfield)
        {
            var afSite = SupplySites.Find(airfield.Site);
            Vector3D afPos;
            if (afSite == null || airfield.Owner.Length == 0 || !SupplySites.Position(afSite, out afPos))
                return null;
            SiteStock best = null;
            double bestDist = double.MaxValue;
            foreach (var site in SupplySites.Tracked)
            {
                if (!SupplySites.IsPort(site.Group) || SupplySites.PortFaction(site.Group) != airfield.Owner)
                    continue;
                if (!SupplySites.InZone(SupplySites.PortAnchor(site.Group), afPos))
                    continue;
                Vector3D portPos;
                if (!SupplySites.Position(site, out portPos))
                    continue;
                var d = Vector3D.DistanceSquared(afPos, portPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = Get(site.Group);
                }
            }
            return best;
        }

        // ---------------- deliveries ----------------

        // WW2-Territory-Action-Delivery-{F}-{Origin}-{Destination}: the destination port gets a ship's load.
        public void ShipDelivered(string destinationAnchor)
        {
            Change("WW2-Port-" + destinationAnchor, SupplySites.ShipDelivery, "cargo ship delivery");
        }

        // WW2-Territory-Action-Delivery-AirCargo-{F}: the nearest airfield within 5 km of the plane, if F owns it.
        public void AirCargoDelivered(Vector3D position, string faction)
        {
            SiteStock best = null;
            double bestDist = SupplySites.AirCargoDeliveryRange * SupplySites.AirCargoDeliveryRange;
            foreach (var site in SupplySites.Tracked)
            {
                Vector3D pos;
                if (!site.Airfield || !SupplySites.Position(site, out pos))
                    continue;
                var d = Vector3D.DistanceSquared(pos, position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = Get(site.Group);
                }
            }
            if (best == null || best.Owner != faction)
            {
                Log("air cargo delivery (" + faction + ") not credited: " + (best == null ? "no airfield within 5 km" : best.Site + " is owned by " + Show(best.Owner)));
                return;
            }
            Change(best.Site, SupplySites.AirCargoDelivery, "air cargo delivery");
        }

        // ---------------- store prices ----------------

        // Called from the MES action watcher just before MES applies a WW2-Store-Action-UpdateSales-* store profile in the same call
        // (ActionSystem.cs: watcher, then ApplyStoreProfiles). Store items read these counters as a price percentage (StoreItems.cs GetPrice).
        public void StoreAction(IMyRemoteControl rc, string actionId)
        {
            var s = Get(SupplySites.SiteOf(rc.CubeGrid));
            int band = s != null ? s.Band : SupplySites.Normal;
            int offer = band == SupplySites.Low ? 130 : band == SupplySites.Full ? 85 : 100;
            int order = band == SupplySites.Full ? 90 : 100;
            MyAPIGateway.Utilities.SetVariable(OfferPctVariable, offer);
            MyAPIGateway.Utilities.SetVariable(OrderPctVariable, order);
            storeCountersSet = true;
            if (s != null && band != SupplySites.Normal && actionId == "WW2-Store-Action-UpdateSales-PortGoods")
                stockPasses.Add(new StockPass { GridId = rc.CubeGrid.EntityId, Multiplier = band == SupplySites.Low ? 0.5 : 1.5 });
        }

        // The tick after a port goods refresh: half the offered stock when Low, half again more when Full.
        private void ApplyStockPasses()
        {
            foreach (var pass in stockPasses)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(pass.GridId) as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose)
                    continue;
                gridBuffer.Clear();
                MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, gridBuffer);
                if (gridBuffer.Count == 0)
                    gridBuffer.Add(grid);
                foreach (var g in gridBuffer)
                {
                    blockBuffer.Clear();
                    g.GetBlocks(blockBuffer, IsGoodsStore);
                    foreach (var b in blockBuffer)
                    {
                        var store = b.FatBlock as IMyStoreBlock;
                        if (store == null)
                            continue;
                        storeItems.Clear();
                        store.GetStoreItems(storeItems);
                        foreach (var item in storeItems)
                            if (item.StoreItemType == StoreItemTypes.Offer && item.Amount > 0)
                                item.Amount = Math.Max(1, (int)(item.Amount * pass.Multiplier));
                        storeItems.Clear();
                    }
                    blockBuffer.Clear();
                }
                gridBuffer.Clear();
            }
            stockPasses.Clear();
        }

        private static bool IsGoodsStore(IMySlimBlock block)
        {
            var terminal = block.FatBlock as IMyTerminalBlock;
            return terminal != null && terminal.CustomName == GoodsStoreName;
        }

        // ---------------- commands ----------------

        // /witm supply ..., routed from RefreshSession.HandleCommand after its Admin/Owner check.
        public static string Command(string[] words, out bool screen)
        {
            screen = false;
            var s = Instance;
            if (s == null || !s.isServer)
                return "Supply is not running.";
            var sub = words.Length > 2 ? words[2].ToLowerInvariant() : "";
            if (sub == "")
            {
                screen = true;
                return s.Table();
            }
            if (sub == "hour")
            {
                s.HourTick();
                screen = true;
                return "Ran one hourly tick.\n\n" + s.Table();
            }
            if (sub == "set")
                return s.SetCommand(words);
            screen = true;
            return HelpText();
        }

        public static string HelpText()
        {
            return "/witm supply\n  Lists every port and airfield: stock, band, owner and which port feeds it.\n\n"
                + "/witm supply set <site> <amount>\n  <site>: a name (La Spezia, Bizerte) or Port-Tripoli / Airfield-Tripoli.\n\n"
                + "/witm supply hour\n  Runs one hourly tick now (upkeep and port feeding).";
        }

        private string SetCommand(string[] words)
        {
            int amount;
            if (words.Length < 5 || !int.TryParse(words[words.Length - 1], out amount))
                return "Usage: /witm supply set <site> <amount>";
            var name = string.Join(" ", words, 3, words.Length - 4);
            RefreshSite match = null;
            int matches = 0;
            foreach (var site in SupplySites.Tracked)
            {
                var kind = site.Airfield ? "Airfield-" : "Port-";
                var compact = site.Name.Replace(" ", "");
                if (Is(name, site.Group) || Is("WW2-" + name, site.Group) || Is(name, site.Name) || Is(name, compact) || Is(name, kind + compact) || Is(name, site.Label))
                {
                    match = site;
                    matches++;
                }
            }
            if (matches == 0)
                return "Unknown site '" + name + "'. /witm supply lists them.";
            if (matches > 1)
                return "'" + name + "' is both a port and an airfield: use Port-" + match.Name.Replace(" ", "") + " or Airfield-" + match.Name.Replace(" ", "") + ".";
            var stock = Get(match.Group);
            Set(stock, amount);
            Log(match.Group + ": set to " + stock.Stock + " by command");
            UpdateSite(stock);
            return match.Label + ": " + stock.Stock + " (" + SupplySites.BandName(stock.Band) + ").";
        }

        private string Table()
        {
            var sb = new StringBuilder();
            var next = state.LastHourSeconds > 0 ? Math.Max(0, (int)((state.LastHourSeconds + 3600 - WarNewsSession.Now()) / 60)) : 60;
            sb.Append("Supply. Next hourly tick in about ").Append(next).Append(" min (game time).\n\n");
            foreach (var site in SupplySites.Tracked)
            {
                var s = Get(site.Group);
                if (s == null)
                    continue;
                sb.Append(site.Label).Append(": ").Append(s.Stock).Append('/').Append(SupplySites.Cap(site.Group))
                    .Append(' ').Append(SupplySites.BandName(s.Band)).Append(", ").Append(s.Owner.Length > 0 ? s.Owner : "unowned");
                if (site.Airfield)
                {
                    var feeder = FeederOf(s);
                    var feederSite = feeder != null ? SupplySites.Find(feeder.Site) : null;
                    sb.Append(", fed by ").Append(feederSite != null ? feederSite.Label + (feeder.Band == SupplySites.Low ? " (Low: not feeding)" : "") : "no port");
                }
                else
                {
                    var anchor = SupplySites.PortAnchor(site.Group);
                    sb.Append(", zone ").Append(anchor).Append(' ').Append((int)(SupplySites.ZoneRadius(anchor) / 1000)).Append(" km");
                }
                sb.Append('\n');
            }
            sb.Append("\nSorties in the air: ").Append(state.Sorties.Count).Append('.');
            return sb.ToString();
        }

        private string Summary()
        {
            var sb = new StringBuilder();
            foreach (var s in state.Sites)
                sb.Append(s.Site.StartsWith("WW2-", StringComparison.Ordinal) ? s.Site.Substring(4) : s.Site).Append(' ').Append(s.Stock).Append(' ');
            return sb.ToString();
        }

        // ---------------- state ----------------

        private void LoadState()
        {
            try
            {
                string text;
                if (!MyAPIGateway.Utilities.GetVariable(StateVariable, out text) || string.IsNullOrEmpty(text))
                    return;
                var loaded = MyAPIGateway.Utilities.SerializeFromBinary<SupplyState>(Convert.FromBase64String(text));
                if (loaded == null)
                    return;
                state = loaded;
                if (state.Sites == null) state.Sites = new List<SiteStock>();
                if (state.Sorties == null) state.Sorties = new List<Sortie>();
                foreach (var s in state.Sites)
                {
                    if (s.Site == null) s.Site = "";
                    if (s.Owner == null) s.Owner = "";
                    if (s.LastOwner == null) s.LastOwner = "";
                }
            }
            catch (Exception e)
            {
                Log("load: " + e);
            }
        }

        private void SaveState()
        {
            try
            {
                state.Version = StateVersion;
                MyAPIGateway.Utilities.SetVariable(StateVariable, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(state)));
            }
            catch (Exception e)
            {
                Log("save: " + e);
            }
        }

        // ---------------- helpers ----------------

        public static MesApiLite Mes()
        {
            var news = WarNewsSession.Instance;
            return news != null ? news.Mes : null;
        }

        private static string Show(string owner)
        {
            return string.IsNullOrEmpty(owner) ? "unowned" : owner;
        }

        private static bool Is(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("WW2-WitM Supply: " + msg);
        }
    }
}
