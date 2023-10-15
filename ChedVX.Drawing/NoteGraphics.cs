using ChedVX.Core.Effects;
using ChedVX.Core.Notes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Drawing
{
    public static class NoteGraphics
    {
        public static void DrawBTChip(this DrawingContext dc, RectangleF rect, bool hasEffect)
        {
            if (hasEffect)
                dc.Graphics.DrawNote(rect, dc.ColorProfile.FXBTColor, dc.ColorProfile.FXBTBorderColor);
            else
                dc.Graphics.DrawNote(rect, dc.ColorProfile.BTColor, dc.ColorProfile.BTBorderColor);
        }

        public static void DrawFXChip(this DrawingContext dc, RectangleF rect, bool hasEffect)
        {
            if (hasEffect)
                dc.Graphics.DrawNote(rect, dc.ColorProfile.FXChipColor, dc.ColorProfile.FXChipBorderColor);
            else
                dc.Graphics.DrawNote(rect, dc.ColorProfile.ChipColor, dc.ColorProfile.ChipBorderColor);
        }

        public static void DrawBTLong(this DrawingContext dc, RectangleF rect, bool hasEffect)
        {
            if (hasEffect)
                dc.Graphics.DrawNote(rect, dc.ColorProfile.FXBTLongColor, dc.ColorProfile.FXBTLongBorderColor);
            else
                dc.Graphics.DrawNote(rect, dc.ColorProfile.BTLongColor, dc.ColorProfile.BTLongBorderColor);
        }

        public static void DrawFXLong(this DrawingContext dc, RectangleF rect, EffectBase effect)
        {
            dc.Graphics.DrawNote(rect, dc.ColorProfile.FXLongColor, dc.ColorProfile.FXLongBorderColor);
        }



        public static void DrawLaserBegin(this DrawingContext dc, IEnumerable<SlideStepElement> steps, IEnumerable<float> visibleSteps, float noteHeight)
        {

        }

        /// <summary>
        /// Draw the Laser.
        /// </summary>
        /// <param name="dc"><see cref="DrawingContext"/></param> to process
        /// <param name="steps">List of all waypoint locations</param>
        /// <param name="visibleSteps">List of Y coordinates of visible step points</param>
        /// <param name="noteHeight">Note drawing height</param>
        public static void DrawLaser(this DrawingContext dc, IEnumerable<SlideStepElement> steps, IEnumerable<float> visibleSteps, float noteHeight, int laserSide)
        {
            var prevMode = dc.Graphics.SmoothingMode;
            dc.Graphics.SmoothingMode = SmoothingMode.AntiAlias;


            var orderedSteps = steps.OrderBy(p => p.Point.Y).ToList();
            var orderedVisibleSteps = visibleSteps.OrderBy(p => p).ToList();

            if (orderedSteps[0].Point.Y < orderedVisibleSteps[0] || orderedSteps[orderedSteps.Count - 1].Point.Y > orderedVisibleSteps[orderedVisibleSteps.Count - 1])
            {
                throw new ArgumentOutOfRangeException("visibleSteps", "visibleSteps must contain steps");
            }

            using (var path = new GraphicsPath())
            {
                var left = orderedSteps.Select(p => p.Point);
                var right = orderedSteps.Select(p => new PointF(p.Point.X + p.Width, p.Point.Y)).Reverse();
                path.AddPolygon(left.Concat(right).ToArray());

                float head = orderedVisibleSteps[0];
                float height = orderedVisibleSteps[orderedVisibleSteps.Count - 1] - head;
                var pathBounds = path.GetBounds();
                var blendBounds = new RectangleF(pathBounds.X, head, pathBounds.Width, height);
                using (var brush = new LinearGradientBrush(blendBounds, Color.Black, Color.Black, LinearGradientMode.Vertical))
                {
                    var heights = orderedVisibleSteps.Zip(orderedVisibleSteps.Skip(1), (p, q) => Tuple.Create(p, q - p));
                    var absPos = new[] { head }.Concat(heights.SelectMany(p => new[] { p.Item1 + p.Item2 * 0.3f, p.Item1 + p.Item2 * 0.7f, p.Item1 + p.Item2 }));
                    var blend = new ColorBlend()
                    {
                        Positions = absPos.Select(p => (p - head) / height).ToArray(),
                        //Colors = new[] { BackgroundEdgeColor }.Concat(Enumerable.Range(0, orderedVisibleSteps.Count - 1).SelectMany(p => new[] { BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor })).ToArray()
                    };
                    brush.InterpolationColors = blend;
                    dc.Graphics.FillPath(brush, path);
                }
            }

            dc.Graphics.SmoothingMode = prevMode;
        }

        public static GraphicsPath GetLaserBackgroundPath(float width1, float width2, float x1, float y1, float x2, float y2)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new PointF[]
            {
                new PointF(x1, y1),
                new PointF(x1 + width1, y1),
                new PointF(x2 + width2, y2),
                new PointF(x2, y2)
            });
            return path;
        }


        public static void DrawBorder(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawBorder(rect, dc.ColorProfile.BorderColor);
        }
    }

    public class SlideStepElement
    {
        public PointF Point { get; set; }
        public float Width { get; set; }
    }
}
