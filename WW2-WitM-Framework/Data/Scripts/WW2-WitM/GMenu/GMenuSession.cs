using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace WW2WitM.GMenu
{
    // WitM G menu: sorts every block into the tabs of Data/GMenu/BlockCategories.sbc using Data/GMenu/GMenuRules.txt
    // and hides every other block category. Modelled on Block Category Cleaner (Workshop 2173774682). Runs on every
    // client when a world loads; the G menu reads category ItemIds each time it opens.
    // Preview without the game: se-mod-production tools/gmenu/gmenu_preview.py, which must match the rule matching here.
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GMenuSession : MySessionComponentBase
    {
        const string RulesFile = "Data/GMenu/GMenuRules.txt";
        const string LogPrefix = "WW2-WitM G menu: ";

        class Tab
        {
            public string Key;
            public string Name;
            public MyGuiBlockCategoryDefinition Category;
            public int Count;
        }

        class Rule
        {
            public int Line;
            public Tab Tab;
            public string Category;    // incat rule: blocks listed in this category
            public HashSet<string> Mods;  // null = any
            public HashSet<string> Types; // null = any
            public string[] Subtypes;     // lower case, * wildcard
            public bool ByName;           // rulename: Subtypes are matched against the display name
        }

        readonly List<Tab> tabs = new List<Tab>();
        readonly Dictionary<string, Tab> tabByKey = new Dictionary<string, Tab>();
        readonly HashSet<string> keep = new HashSet<string>();
        readonly List<KeyValuePair<string, string>> moves = new List<KeyValuePair<string, string>>();
        readonly List<Rule> rules = new List<Rule>();
        readonly Dictionary<string, List<string>> defines = new Dictionary<string, List<string>>();

        public override void BeforeStart()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            try
            {
                if (LoadRules())
                    Apply();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole(LogPrefix + "error: " + e);
            }
        }

        bool LoadRules()
        {
            if (!MyAPIGateway.Utilities.FileExistsInModLocation(RulesFile, ModContext.ModItem))
            {
                MyLog.Default.WriteLineAndConsole(LogPrefix + RulesFile + " not found, G menu left as it is");
                return false;
            }
            int n = 0;
            using (TextReader reader = MyAPIGateway.Utilities.ReadFileInModLocation(RulesFile, ModContext.ModItem))
            {
                string raw;
                while ((raw = reader.ReadLine()) != null)
                {
                    n++;
                    int hash = raw.IndexOf('#');
                    string line = (hash >= 0 ? raw.Substring(0, hash) : raw).Trim();
                    if (line.Length == 0)
                        continue;
                    string rest;
                    string kind = First(line, out rest).ToLowerInvariant();
                    string a, b, c;
                    switch (kind)
                    {
                        case "tab":
                            a = First(rest, out b);
                            var tab = new Tab { Key = a, Name = b };
                            tabs.Add(tab);
                            tabByKey[a] = tab;
                            break;
                        case "keep":
                            keep.Add(rest);
                            break;
                        case "move":
                            a = First(rest, out b);
                            moves.Add(new KeyValuePair<string, string>(a, b));
                            break;
                        case "define":
                            a = First(rest, out b);
                            defines[a] = SplitList(b);
                            break;
                        case "incat":
                            a = First(rest, out b);
                            AddRule(n, a, new Rule { Category = b });
                            break;
                        case "rule":
                        case "rulename":
                            a = First(rest, out rest);
                            b = First(rest, out rest);
                            c = First(rest, out rest);
                            if (rest.Length == 0)
                            {
                                Warn(n, "rule needs <tab> <mods> <types> <subtypes>");
                                break;
                            }
                            var subs = SplitList(rest);
                            for (int i = 0; i < subs.Count; i++)
                                subs[i] = subs[i].ToLowerInvariant();
                            AddRule(n, a, new Rule { Mods = AnyOrSet(SplitList(b)), Types = AnyOrSet(SplitList(c)), Subtypes = subs.ToArray(), ByName = kind == "rulename" });
                            break;
                        default:
                            Warn(n, "unknown line kind '" + kind + "'");
                            break;
                    }
                }
            }
            return tabs.Count > 0;
        }

        void AddRule(int line, string tabKey, Rule rule)
        {
            Tab tab;
            if (!tabByKey.TryGetValue(tabKey, out tab))
            {
                Warn(line, "unknown tab '" + tabKey + "'");
                return;
            }
            rule.Line = line;
            rule.Tab = tab;
            rules.Add(rule);
        }

        void Apply()
        {
            var categories = MyDefinitionManager.Static.GetCategories();
            foreach (var tab in tabs)
            {
                if (!categories.TryGetValue(tab.Name, out tab.Category))
                    MyLog.Default.WriteLineAndConsole(LogPrefix + "tab category " + tab.Name + " not loaded");
            }

            // Hide every category that isn't a WitM tab or kept. IsBlockCategory is what the G menu checks; the other
            // flags cover creative and survival. Their ItemIds stay, so incat rules still work if the game reuses
            // these definitions for the next world.
            int hidden = 0;
            var tabNames = new HashSet<string>();
            foreach (var tab in tabs)
                tabNames.Add(tab.Name);
            var moved = new HashSet<string>();
            foreach (var move in moves)
            {
                MyGuiBlockCategoryDefinition from, to;
                if (!categories.TryGetValue(move.Key, out from) || !categories.TryGetValue(move.Value, out to))
                {
                    MyLog.Default.WriteLineAndConsole(LogPrefix + "move " + move.Key + " -> " + move.Value + ": category not loaded");
                    continue;
                }
                to.ItemIds.UnionWith(from.ItemIds);
                moved.Add(move.Key);
            }
            foreach (var cat in categories.Values)
            {
                if (!moved.Contains(cat.Name) && (tabNames.Contains(cat.Name) || keep.Contains(cat.Name) || cat.IsShipCategory || cat.IsToolCategory || cat.IsAnimationCategory))
                    continue;
                Hide(cat);
                hidden++;
            }

            var incats = new Dictionary<string, MyGuiBlockCategoryDefinition>();
            foreach (var rule in rules)
            {
                MyGuiBlockCategoryDefinition cat;
                if (rule.Category != null && !incats.ContainsKey(rule.Category))
                {
                    if (categories.TryGetValue(rule.Category, out cat))
                        incats[rule.Category] = cat;
                    else
                        Warn(rule.Line, "category '" + rule.Category + "' not loaded");
                }
            }

            var tabOf = new Dictionary<MyDefinitionId, Tab>();
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var block = def as MyCubeBlockDefinition;
                if (block == null)
                    continue;
                string type = block.Id.TypeId.ToString().Replace("MyObjectBuilder_", "");
                string sub = block.Id.SubtypeName ?? "";
                Rule rule = Classify(ModKey(block), type, sub, block.DisplayNameText ?? "", incats);
                if (rule != null)
                    tabOf[block.Id] = rule.Tab;
            }

            // A variant group shows as one icon: put the whole group in the tab of its icon block (PrimaryGUIBlock = Blocks[0]).
            foreach (var pair in MyDefinitionManager.Static.GetBlockVariantGroupDefinitions())
            {
                var blocks = pair.Value.Blocks;
                Tab tab;
                if (blocks == null || blocks.Length == 0 || blocks[0] == null || !tabOf.TryGetValue(blocks[0].Id, out tab))
                    continue;
                foreach (var member in blocks)
                {
                    if (member != null)
                        tabOf[member.Id] = tab;
                }
            }

            foreach (var tab in tabs)
            {
                if (tab.Category != null)
                    tab.Category.ItemIds.Clear();
            }
            foreach (var pair in tabOf)
            {
                if (pair.Value.Category == null)
                    continue;
                // Same text MyGuiBlockCategoryDefinition.HasItem compares against.
                pair.Value.Category.ItemIds.Add(pair.Key.ToString().Replace("MyObjectBuilder_", ""));
                pair.Value.Count++;
            }

            var summary = new System.Text.StringBuilder();
            foreach (var tab in tabs)
                summary.Append(tab.Key).Append(' ').Append(tab.Count).Append(", ");
            MyLog.Default.WriteLineAndConsole(LogPrefix + hidden + " categories hidden; " + summary.ToString().TrimEnd(',', ' '));
        }

        static void Hide(MyGuiBlockCategoryDefinition cat)
        {
            cat.IsBlockCategory = false;
            cat.Public = false;
            cat.ShowInCreative = false;
            cat.AvailableInSurvival = false;
        }

        Rule Classify(string mod, string type, string sub, string name, Dictionary<string, MyGuiBlockCategoryDefinition> incats)
        {
            string lower = sub.ToLowerInvariant();
            string lowerName = name.ToLowerInvariant();
            foreach (var rule in rules)
            {
                if (rule.Category != null)
                {
                    MyGuiBlockCategoryDefinition cat;
                    if (incats.TryGetValue(rule.Category, out cat) && (cat.ItemIds.Contains(type + "/" + sub) || cat.ItemIds.Contains(sub)))
                        return rule;
                    continue;
                }
                if (rule.Mods != null && !rule.Mods.Contains(mod))
                    continue;
                if (rule.Types != null && !rule.Types.Contains(type))
                    continue;
                foreach (var pattern in rule.Subtypes)
                {
                    if (Wild(pattern, rule.ByName ? lowerName : lower))
                        return rule;
                }
            }
            return null;
        }

        // Workshop ID, the local mod folder name, or "vanilla".
        static string ModKey(MyCubeBlockDefinition block)
        {
            var context = block.Context;
            if (context == null || context.IsBaseGame)
                return "vanilla";
            if (context.ModItem.PublishedFileId != 0)
                return context.ModItem.PublishedFileId.ToString();
            return context.ModItem.Name ?? "";
        }

        // '*' is the only wildcard; both arguments lower case.
        static bool Wild(string pattern, string text)
        {
            string[] parts = pattern.Split('*');
            if (parts.Length == 1)
                return pattern == text;
            if (!text.StartsWith(parts[0], StringComparison.Ordinal))
                return false;
            int pos = parts[0].Length;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                int at = text.IndexOf(parts[i], pos, StringComparison.Ordinal);
                if (at < 0)
                    return false;
                pos = at + parts[i].Length;
            }
            string last = parts[parts.Length - 1];
            return text.Length - pos >= last.Length && text.EndsWith(last, StringComparison.Ordinal);
        }

        static string First(string s, out string rest)
        {
            s = s.Trim();
            int i = 0;
            while (i < s.Length && !char.IsWhiteSpace(s[i]))
                i++;
            rest = s.Substring(i).Trim();
            return s.Substring(0, i);
        }

        List<string> SplitList(string s)
        {
            var list = new List<string>();
            foreach (var part in s.Split(','))
            {
                string item = part.Trim();
                if (item.Length == 0)
                    continue;
                List<string> defined;
                if (item.StartsWith("@") && defines.TryGetValue(item.Substring(1), out defined))
                    list.AddRange(defined);
                else
                    list.Add(item);
            }
            return list;
        }

        static HashSet<string> AnyOrSet(List<string> items)
        {
            return items.Contains("*") ? null : new HashSet<string>(items);
        }

        static void Warn(int line, string message)
        {
            MyLog.Default.WriteLineAndConsole(LogPrefix + RulesFile + " line " + line + ": " + message);
        }
    }
}
