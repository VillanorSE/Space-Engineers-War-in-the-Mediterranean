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
    // "War News" LCD script: pick it in any LCD's Script list (players' own LCDs included). Draws client-side from the
    // feed WarNewsSession keeps in sync, newest first, as many items as fit.
    [MyTextSurfaceScript("WW2WitM_WarNews", WarNewsText.LcdScriptName)]
    public class WarNewsTss : MyTSSCommon
    {
        private const string Font = "White";
        private const float TitleScale = 0.9f;
        private const float StampScale = 0.55f;
        private const float TextScale = 0.65f;
        private const float Margin = 12f;

        private readonly IMyTerminalBlock terminalBlock;
        private readonly StringBuilder measure = new StringBuilder();
        private readonly List<string> lines = new List<string>();

        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update100; } }

        public WarNewsTss(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                MyLog.Default.WriteLineAndConsole("WW2-WitM War News LCD: " + e);
            }
        }

        private void Draw()
        {
            var screen = Surface.SurfaceSize;
            var corner = (Surface.TextureSize - screen) * 0.5f;
            var fg = Surface.ScriptForegroundColor;
            var dim = fg * 0.6f;
            float width = screen.X - 2 * Margin;
            float y = Margin;

            using (var frame = Surface.DrawFrame())
            {
                AddText(frame, WarNewsText.LcdTitle, corner + new Vector2(Margin, y), fg, TitleScale);
                y += LineHeight(TitleScale) * 1.4f;

                var items = WarNews.Items;
                if (items.Count == 0)
                {
                    AddText(frame, WarNewsText.LcdEmpty, corner + new Vector2(Margin, y), dim, TextScale);
                    return;
                }

                foreach (var item in items)
                {
                    Wrap(item.Text, width, TextScale);
                    float needed = LineHeight(StampScale) + lines.Count * LineHeight(TextScale);
                    if (y + needed > screen.Y - Margin)
                        break;

                    AddText(frame, item.Stamp, corner + new Vector2(Margin, y), dim, StampScale);
                    y += LineHeight(StampScale);
                    foreach (var line in lines)
                    {
                        AddText(frame, line, corner + new Vector2(Margin, y), FactionColor(item.Faction, fg), TextScale);
                        y += LineHeight(TextScale);
                    }
                    y += LineHeight(TextScale) * 0.5f;
                }
            }
        }

        private static Color FactionColor(string faction, Color fallback)
        {
            if (faction == "Gray") return new Color(200, 200, 200);
            if (faction == "Green") return new Color(150, 220, 150);
            return fallback;
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
