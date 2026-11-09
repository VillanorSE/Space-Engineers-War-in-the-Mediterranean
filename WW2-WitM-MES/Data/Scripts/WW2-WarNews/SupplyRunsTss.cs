using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // "Supply Runs" LCD script: the active truck columns and cargo ships of the LCD owner's side (route, trucks, distance to go,
    // GPS of the lead) so players can escort them; the other side's runs show only as "Reports of <nation> cargo ships at sea
    // originating from <port>" / "... truck convoys on the roads out of <port>" (owner 2026-11-03). The side is the owner's faction if it is GRAY or GREEN, else whichever of the two the owner's reputation
    // is friendly with (/alliance, D-017). Draws client-side from WarNews.Runs (synced every 10 s).
    [MyTextSurfaceScript("WW2WitM_SupplyRuns", LcdScriptName)]
    public class SupplyRunsTss : MyTSSCommon
    {
        public const string LcdScriptName = "Supply Runs";
        private const string Font = "White";
        private const float TitleScale = 0.9f;
        private const float TextScale = 0.65f;
        private const float SmallScale = 0.55f;
        private const float Margin = 12f;
        // SE shows a faction as friendly above 500 reputation.
        private const int FriendlyRep = 500;

        private readonly IMyTerminalBlock terminalBlock;
        private readonly StringBuilder measure = new StringBuilder();
        private readonly List<string> lines = new List<string>();
        private readonly HashSet<string> reported = new HashSet<string>();

        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update100; } }

        public SupplyRunsTss(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            terminalBlock = (IMyTerminalBlock)block;
            terminalBlock.OnMarkForClose += BlockMarkedForClose;
        }

        public override void Dispose()
        {
            base.Dispose();
            terminalBlock.OnMarkForClose -= BlockMarkedForClose;
        }

        private void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        public override void Run()
        {
            try
            {
                base.Run();
                Draw();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("WW2-WitM Supply Runs LCD: " + e);
            }
        }

        private void Draw()
        {
            var screen = Surface.SurfaceSize;
            var corner = (Surface.TextureSize - screen) * 0.5f;
            var fg = Surface.ScriptForegroundColor;
            var dim = fg * 0.6f;
            float width = screen.X - 2 * Margin;
            float bottom = screen.Y - Margin;
            float y = Margin;
            var side = Side();

            using (var frame = Surface.DrawFrame())
            {
                var title = side.Length > 0 ? "SUPPLY RUNS - " + WarNewsText.Nation(side).ToUpperInvariant() : "SUPPLY RUNS";
                AddText(frame, title, corner + new Vector2(Margin, y), fg, TitleScale);
                y += LineHeight(TitleScale) * 1.4f;

                if (side.Length == 0)
                {
                    Block(frame, "Pick a side with /alliance GRAY or /alliance GREEN to see your supply runs.", corner, ref y, width, bottom, dim, TextScale);
                    return;
                }

                var color = WarNewsTss.FactionColor(side, fg);
                bool any = false;
                foreach (var run in WarNews.Runs)
                {
                    if (run.Faction != side)
                        continue;
                    any = true;
                    var head = (run.Ship ? "Cargo ship  " : "Trucks  ") + run.From + " -> " + run.To;
                    var info = run.Ship ? "" : run.Count + (run.Count == 1 ? " truck" : " trucks");
                    if (run.ToGo >= 0)
                        info += (info.Length > 0 ? ", " : "") + (run.ToGo / 1000).ToString("0.0") + " km to go";
                    var gps = "GPS " + run.X.ToString("0") + " " + run.Y.ToString("0") + " " + run.Z.ToString("0");
                    if (!Block(frame, head, corner, ref y, width, bottom, color, TextScale)
                        || (info.Length > 0 && !Block(frame, info, corner, ref y, width, bottom, fg, SmallScale))
                        || !Block(frame, gps, corner, ref y, width, bottom, dim, SmallScale))
                        return;
                    y += LineHeight(TextScale) * 0.5f;
                }
                if (!any && !Block(frame, "No active supply runs.", corner, ref y, width, bottom, dim, TextScale))
                    return;
                y += LineHeight(TextScale) * 0.5f;

                // The other side: only where its runs came from, one line per kind and origin.
                reported.Clear();
                foreach (var run in WarNews.Runs)
                {
                    if (run.Faction == side || run.Faction.Length == 0)
                        continue;
                    var text = run.Ship ? "Reports of " + Adjective(run.Faction) + " cargo ships at sea originating from " + run.From
                        : "Reports of " + Adjective(run.Faction) + " truck convoys on the roads out of " + run.From;
                    if (!reported.Add(text))
                        continue;
                    if (!Block(frame, text, corner, ref y, width, bottom, WarNewsTss.FactionColor(run.Faction, fg), TextScale))
                        return;
                    y += LineHeight(TextScale) * 0.3f;
                }
            }
        }

        // "Gray" / "Green" for the block owner's side, or "" if the owner has picked neither.
        private string Side()
        {
            long owner = terminalBlock.OwnerId;
            if (owner == 0)
                return "";
            var factions = MyAPIGateway.Session.Factions;
            var own = factions.TryGetPlayerFaction(owner);
            if (own != null && own.Tag == "GRAY")
                return "Gray";
            if (own != null && own.Tag == "GREEN")
                return "Green";
            var gray = factions.TryGetFactionByTag("GRAY");
            var green = factions.TryGetFactionByTag("GREEN");
            int toGray = Rep(owner, own, gray);
            int toGreen = Rep(owner, own, green);
            if (toGray > FriendlyRep && toGray >= toGreen)
                return "Gray";
            if (toGreen > FriendlyRep)
                return "Green";
            return "";
        }

        // The higher of the owner's personal rep and their faction's rep with the side: /alliance sets the faction rep, and
        // personal rep can be reset at load.
        private static int Rep(long owner, IMyFaction own, IMyFaction side)
        {
            if (side == null)
                return 0;
            var factions = MyAPIGateway.Session.Factions;
            int rep = factions.GetReputationBetweenPlayerAndFaction(owner, side.FactionId);
            if (own != null)
                rep = Math.Max(rep, factions.GetReputationBetweenFactions(own.FactionId, side.FactionId));
            return rep;
        }

        private static string Adjective(string faction)
        {
            if (faction == "Gray") return "Italian";
            if (faction == "Green") return "French";
            return faction;
        }

        // Draws wrapped text; false (and nothing drawn) when it no longer fits on the screen.
        private bool Block(MySpriteDrawFrame frame, string text, Vector2 corner, ref float y, float width, float bottom, Color color, float scale)
        {
            Wrap(text, width, scale);
            float height = LineHeight(scale);
            if (y + lines.Count * height > bottom)
                return false;
            foreach (var line in lines)
            {
                AddText(frame, line, corner + new Vector2(Margin, y), color, scale);
                y += height;
            }
            return true;
        }

        private static void AddText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale)
        {
            var sprite = MySprite.CreateText(text, Font, color, scale, TextAlignment.LEFT);
            sprite.Position = position;
            frame.Add(sprite);
        }

        private float LineHeight(float scale)
        {
            measure.Clear().Append("Ag");
            return Surface.MeasureStringInPixels(measure, Font, scale).Y;
        }

        private float TextWidth(string text, float scale)
        {
            measure.Clear().Append(text);
            return Surface.MeasureStringInPixels(measure, Font, scale).X;
        }

        private void Wrap(string text, float width, float scale)
        {
            lines.Clear();
            var current = "";
            foreach (var word in (text ?? "").Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && TextWidth(candidate, scale) > width)
                {
                    lines.Add(current);
                    current = word;
                }
                else
                {
                    current = candidate;
                }
            }
            if (current.Length > 0)
                lines.Add(current);
        }
    }
}
