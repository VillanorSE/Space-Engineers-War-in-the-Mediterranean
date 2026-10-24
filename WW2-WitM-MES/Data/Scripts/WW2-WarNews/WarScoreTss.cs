using System;
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
    // "War Score" LCD script: only the two faction scores in two columns, as large as the screen allows. Same synced scores as the
    // War News LCD's score line (WarNews.GrayScore / GreenScore).
    [MyTextSurfaceScript("WW2WitM_WarScore", WarNewsText.ScoreLcdScriptName)]
    public class WarScoreTss : MyTSSCommon
    {
        private const string Font = "White";
        private const float TitleScale = 1.4f;
        private const float MaxScoreScale = 3f;
        private const float NameRatio = 0.75f;
        private const float Margin = 12f;

        private readonly IMyTerminalBlock terminalBlock;
        private readonly StringBuilder measure = new StringBuilder();

        public override ScriptUpdate NeedsUpdate { get { return ScriptUpdate.Update100; } }

        public WarScoreTss(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
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
                MyLog.Default.WriteLineAndConsole("WW2-WitM War Score LCD: " + e);
            }
        }

        private void Draw()
        {
            var screen = Surface.SurfaceSize;
            var corner = (Surface.TextureSize - screen) * 0.5f;
            var fg = Surface.ScriptForegroundColor;
            var grayName = WarNewsText.Nation("Gray");
            var greenName = WarNewsText.Nation("Green");
            var grayScore = WarNews.GrayScore.ToString();
            var greenScore = WarNews.GreenScore.ToString();

            // Two columns (Italy left, France right), each with the name above the score, centred in its column.
            float column = (screen.X - 2 * Margin) * 0.5f;
            float titleHeight = Size("Ag", TitleScale).Y * 1.4f;
            float rowsHeight = screen.Y - 2 * Margin - titleHeight;

            // Largest score scale at which every name and score fits its column (with a gap) and both rows fit the height.
            float widest = Math.Max(Math.Max(Size(grayName, NameRatio).X, Size(greenName, NameRatio).X),
                                    Math.Max(Size(grayScore, 1f).X, Size(greenScore, 1f).X));
            float line = Size("Ag", 1f).Y;
            float scale = Math.Min(MaxScoreScale, Math.Min(column * 0.85f / Math.Max(widest, 1f), rowsHeight / ((1 + NameRatio) * line)));
            float nameRow = Size("Ag", scale * NameRatio).Y;
            float scoreRow = Size("Ag", scale).Y;
            float y = Margin + titleHeight + (rowsHeight - nameRow - scoreRow) * 0.5f;
            float left = Margin + column * 0.5f;
            float right = Margin + column * 1.5f;
            var grayColor = WarNewsTss.FactionColor("Gray", fg);
            var greenColor = WarNewsTss.FactionColor("Green", fg);

            using (var frame = Surface.DrawFrame())
            {
                AddText(frame, WarNewsText.ScoreLcdTitle, corner + new Vector2(screen.X * 0.5f, Margin), fg, TitleScale);
                AddText(frame, grayName, corner + new Vector2(left, y), grayColor, scale * NameRatio);
                AddText(frame, greenName, corner + new Vector2(right, y), greenColor, scale * NameRatio);
                AddText(frame, grayScore, corner + new Vector2(left, y + nameRow), grayColor, scale);
                AddText(frame, greenScore, corner + new Vector2(right, y + nameRow), greenColor, scale);
            }
        }

        private static void AddText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale)
        {
            var sprite = MySprite.CreateText(text, Font, color, scale, TextAlignment.CENTER);
            sprite.Position = position;
            frame.Add(sprite);
        }

        private Vector2 Size(string text, float scale)
        {
            measure.Clear().Append(text);
            return Surface.MeasureStringInPixels(measure, Font, scale);
        }
    }
}
