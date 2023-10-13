using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Drawing
{
    internal static class ComponentGraphics
    {
        public static void DrawNote(this Graphics g, RectangleF rect, GradientColor foregroundColors, GradientColor borderColors)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect, foregroundColors.StartColor, foregroundColors.EndColor, LinearGradientMode.Horizontal))
            {
                g.FillRectangle(brush, rect);
            }

            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), borderColors.StartColor, borderColors.EndColor, LinearGradientMode.Vertical))
            {
                using (var pen = new Pen(brush, borderWidth))
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
            DrawBorder(g, rect, borderColors);
        }


        public static void DrawBorder(this Graphics g, RectangleF rect, GradientColor colors)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), colors.StartColor, colors.EndColor, LinearGradientMode.Horizontal))
            {
                using (var pen = new Pen(brush, borderWidth))
                {
                    using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }
    }
}
