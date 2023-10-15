using ChedVX.Core;
using ChedVX.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace ChedVX.UI
{
    public partial class NoteView
    {
        protected override void OnPaint(PaintEventArgs pe)
        {
            // TODO: Implement multiple lanes.
            base.OnPaint(pe);

            // Drawing the positive direction of the Y axis as tick increase (y = 0 is the bottom of control), [To be changed]
            var prevMatrix = pe.Graphics.Transform;
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix);

            var dc = new DrawingContext(pe.Graphics, ColorProfile);

            float laneWidth = LaneWidth;
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // Lane dividing line drawing
            using (var lightPen = new Pen(LaneBorderLightColor, BorderThickness))
            using (var darkPen = new Pen(LaneBorderDarkColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(i % 2 == 0 ? lightPen : darkPen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // Time cursor Drawing
            // そのイベントが含まれる小節(ただし[小節開始Tick, 小節開始Tick + 小節Tick)の範囲)からその拍子を適用
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            using (var beatPen = new Pen(BeatLineColor, BorderThickness))
            using (var barPen = new Pen(BarLineColor, BorderThickness))
            {
                // 最初の拍子
                int firstBarLength = UnitBeatTick * 4 * sigs[0].Numerator / sigs[0].Denominator;
                int barTick = UnitBeatTick * 4;

                for (int i = HeadTick / (barTick / sigs[0].Denominator); sigs.Count < 2 || i * barTick / sigs[0].Denominator < sigs[1].Tick / firstBarLength * firstBarLength; i++)
                {
                    int tick = i * barTick / sigs[0].Denominator;
                    float y = GetYPositionFromTick(tick);
                    pe.Graphics.DrawLine(i % sigs[0].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    if (tick > tailTick) break;
                }

                // その後の拍子
                int pos = 0;
                for (int j = 1; j < sigs.Count; j++)
                {
                    int prevBarLength = barTick * sigs[j - 1].Numerator / sigs[j - 1].Denominator;
                    int currentBarLength = barTick * sigs[j].Numerator / sigs[j].Denominator;
                    pos += (sigs[j].Tick - pos) / prevBarLength * prevBarLength;
                    if (pos > tailTick) break;
                    for (int i = HeadTick - pos < 0 ? 0 : (HeadTick - pos) / (barTick / sigs[j].Denominator); pos + i * (barTick / sigs[j].Denominator) < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * barTick / sigs[j].Denominator >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;
                        float y = GetYPositionFromTick(pos + i * barTick / sigs[j].Denominator);
                        pe.Graphics.DrawLine(i % sigs[j].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    }
                }
            }

            using (var posPen = new Pen(Color.FromArgb(196, 0, 0)))
            {
                float y = GetYPositionFromTick(CurrentTick);
                pe.Graphics.DrawLine(posPen, -UnitLaneWidth * 2, y, laneWidth, y);
            }

            // Draw Notes
            var BTLongs = Notes.BTs.Where(p => p.StartTick <= tailTick && p.StartTick + p.Duration >= HeadTick && !p.IsChip).ToList();

            // Button Chip Note
            foreach (var note in Notes.BTs.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick && p.IsChip))
            {
                dc.DrawBTChip(GetRectFromNotePosition(note.StartTick, note.LaneIndex), note.HasEffect);
            }

            // Button Long Note
            foreach (var hold in BTLongs)
            {
                dc.DrawBTLong(new RectangleF(
                    (UnitLaneWidth + BorderThickness) * hold.LaneIndex + BorderThickness,
                    GetYPositionFromTick(hold.StartTick),
                    (UnitLaneWidth + BorderThickness) - BorderThickness,
                    GetYPositionFromTick(hold.Duration) - GetYPositionFromTick(0)
                    ), hold.HasEffect);
            }

            // FX Chip Note
            foreach (var note in Notes.FXs.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick && p.IsChip))
            {
                dc.DrawFXChip(GetRectFromNotePosition(note.StartTick, note.LaneIndex), note.HasEffect);
            }

            // FX Long Note
            foreach (var note in Notes.FXs.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick && p.IsChip))
            {
                dc.DrawFXLong(GetRectFromNotePosition(note.StartTick, note.LaneIndex), note.Effect);
            }


            // Lasers
            /*
            var lasers = Notes.Lasers.Where(p => p.StartTick <= tailTick && p.StartTick + p.Duration >= HeadTick).ToList();
            foreach (var slide in slides)
            {
                var bg = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.OrderBy(p => p.Tick)).ToList();
                var visibleSteps = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.Where(p => p.IsVisible).OrderBy(p => p.Tick)).ToList();

                int stepHead = bg.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? bg[0].Tick;
                int stepTail = bg.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? bg[bg.Count - 1].Tick;
                int visibleHead = visibleSteps.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? visibleSteps[0].Tick;
                int visibleTail = visibleSteps.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? visibleSteps[visibleSteps.Count - 1].Tick;

                var steps = bg
                    .Where(p => p.Tick >= stepHead && p.Tick <= stepTail)
                    .Select(p => new SlideStepElement()
                    {
                        Point = new PointF((UnitLaneWidth + BorderThickness) * p.LaneIndex, GetYPositionFromTick(p.Tick)),
                        Width = (UnitLaneWidth + BorderThickness) * p.Width - BorderThickness
                    });
                var visibleStepPos = visibleSteps
                    .Where(p => p.Tick >= visibleHead && p.Tick <= visibleTail)
                    .Select(p => GetYPositionFromTick(p.Tick));

                if (stepHead == stepTail) continue;
                dc.DrawSlideBackground(steps, visibleStepPos, ShortNoteHeight);
            }
            */

            // Selection Range
            if (Editable) DrawSelectionRange(pe.Graphics);

            // a coordinate system with Tick = 0 as the Y-axis origin without reversing the Y-axis
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix, false);

            using (var font = new Font("MS Gothic", 8))
            {
                SizeF strSize = pe.Graphics.MeasureString("000", font);

                // Time Line Drawing
                int barTick = UnitBeatTick * 4;
                int barCount = 0;
                int pos = 0;

                for (int j = 0; j < sigs.Count; j++)
                {
                    if (pos > tailTick) break;
                    int currentBarLength = (UnitBeatTick * 4) * sigs[j].Numerator / sigs[j].Denominator;
                    for (int i = 0; pos + i * currentBarLength < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * currentBarLength >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;

                        int tick = pos + i * currentBarLength;
                        barCount++;
                        if (tick < HeadTick) continue;
                        var point = new PointF(-strSize.Width, -GetYPositionFromTick(tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0:000}", barCount), font, Brushes.White, point);
                    }

                    if (j < sigs.Count - 1)
                        pos += (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength;
                }

                float rightBase = (UnitLaneWidth + BorderThickness) * Constants.LanesCount + strSize.Width / 3;

                // BPM Drawing
                using (var bpmBrush = new SolidBrush(Color.FromArgb(0, 192, 0)))
                {
                    foreach (var item in ScoreEvents.BpmChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(Regex.Replace(item.Bpm.ToString(), @"\.0$", "").PadLeft(3), font, Brushes.Lime, point);
                    }
                }

                // Time Signature Drawing
                using (var sigBrush = new SolidBrush(Color.FromArgb(216, 116, 0)))
                {
                    foreach (var item in sigs.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0}/{1}", item.Numerator, item.Denominator), font, sigBrush, point);
                    }
                }

                // Tick Drawing
                using (var highSpeedBrush = new SolidBrush(Color.FromArgb(216, 0, 64)))
                {
                    foreach (var item in ScoreEvents.HighSpeedChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width * 2, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("x{0: 0.00;-0.00}", item.SpeedRatio), font, highSpeedBrush, point);
                    }
                }
            }

            pe.Graphics.Transform = prevMatrix;
        }
    }
}
