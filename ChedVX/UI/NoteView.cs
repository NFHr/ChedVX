using ChedVX.Core;
using ChedVX.Core.Events;
using ChedVX.Core.Notes;
using ChedVX.Drawing;
using ChedVX.UI.Operations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ChedVX.UI
{
    public partial class NoteView : Control
    {
        public NoteView(OperationManager manager)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Opaque, true);

            OperationManager = manager;

            QuantizeTick = UnitBeatTick;

            colorProfile = new ColorProfile()
            {
                BorderColor = new GradientColor(Color.FromArgb(160, 160, 160), Color.FromArgb(208, 208, 208)),

                BTColor = new GradientColor(Color.FromArgb(254, 255, 252), Color.FromArgb(254, 255, 252)),
                BTBorderColor = new GradientColor(Color.FromArgb(151, 153, 150), Color.FromArgb(151, 153, 150)),

                FXBTColor = new GradientColor(Color.FromArgb(70, 168, 168), Color.FromArgb(0, 216, 255)),
                FXBTBorderColor = new GradientColor(Color.FromArgb(54, 135, 150), Color.FromArgb(171, 231, 235)),

                ChipColor = new GradientColor(Color.FromArgb(225, 148, 27), Color.FromArgb(225, 148, 27)),
                ChipBorderColor = new GradientColor(Color.FromArgb(255, 181, 0), Color.FromArgb(255, 181, 0)),

                FXChipColor = new GradientColor(Color.FromArgb(255, 190, 19), Color.FromArgb(255, 148, 27)),
                FXChipBorderColor = new GradientColor(Color.FromArgb(255, 236, 0), Color.FromArgb(255, 181, 0)),
               
                BTLongColor = new GradientColor(Color.FromArgb(209, 210, 207), Color.FromArgb(246, 248, 245)),
                BTLongBorderColor = new GradientColor(Color.FromArgb(151, 153, 150), Color.FromArgb(151, 153, 150)),

                FXBTLongColor = new GradientColor(Color.FromArgb(143, 209, 220), Color.FromArgb(246, 248, 245)),
                FXBTLongBorderColor = new GradientColor(Color.FromArgb(151, 153, 150), Color.FromArgb(151, 153, 150)),

                FXLongColor = new GradientColor(Color.FromArgb(130, 255, 200, 7), Color.FromArgb(130, 255, 160, 7)),
                FXLongBorderColor = new GradientColor(Color.FromArgb(180, 255, 179, 61), Color.FromArgb(180, 255, 179, 61)),

                LaserLColor = new GradientColor(Color.FromArgb(100, 68, 106, 250), Color.FromArgb(130, 69, 107, 249)),
                LaserLBorderColor = new GradientColor(Color.FromArgb(160, 66, 119, 252), Color.FromArgb(160, 66, 119, 252)),

                LaserRColor = new GradientColor(Color.FromArgb(100, 250, 68, 172), Color.FromArgb(130, 249, 67, 172)),
                LaserRBorderColor = new GradientColor(Color.FromArgb(160, 252, 66, 185), Color.FromArgb(160, 252, 66, 185)),

            };
            InitializeHandlers();
        }

        private void InitializeHandlers()
        {
            var mouseDown = this.MouseDownAsObservable();
            var mouseUp = this.MouseUpAsObservable();
            var mouseMove = this.MouseMoveAsObservable();

            var editSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Edit)
                .SelectMany(p =>
                {
                    int tailTick = TailTick;
                    var from = p.Location;
                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Location);

                    // TODO: outside of the drawing area
                    RectangleF scoreRect = new RectangleF(0, GetYPositionFromTick(HeadTick), LaneWidth, GetYPositionFromTick(TailTick) - GetYPositionFromTick(HeadTick));
                    if (!scoreRect.Contains(scorePos)) return Observable.Empty<MouseEventArgs>();

                    // Shouldn't be supported in edit mode
                    IObservable<MouseEventArgs> moveNoteHandler(NoteBase note)
                    {
                        int beforeLaneIndex = (int) note.TrackID;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                note.StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndex = beforeLaneIndex + xdiff;
                                note.LaneIndex = Math.Min(Constants.LanesCount, Math.Max(0, laneIndex));
                                Cursor.Current = Cursors.SizeAll;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    }


                    IObservable<MouseEventArgs> DurationHandler(NoteBase note)
                    {
                        return mouseMove.TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                note.Duration = (int)Math.Max(QuantizeTick, GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - note.StartTick);
                                Cursor.Current = Cursors.SizeNS;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    }


                    // Do nothing if is clicked inside a note or in the range of the long note
                    IObservable<MouseEventArgs> NoteHandler(NoteBase note)
                    {
                        return null;
                    }

                    // Do nothing if is clicked in the range of the laser
                    IObservable<MouseEventArgs> MoveLaserHandler(LaserNote note)
                    {

                        RectangleF startRect = GetClickableRectFromNotePosition(note.StartTick, note.LaneIndex, note.Duration);

                        if (startRect.Contains(scorePos))
                        {
                            /*
                            int beforeLaneIndex = slide.StartNote.LaneIndex;
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    slide.StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforeLaneIndex + xdiff;
                                    slide.StartLaneIndex = Math.Min(Constants.LanesCount - slide.StartWidth - rightStepLaneIndexOffset, Math.Max(-leftStepLaneIndexOffset, laneIndex));
                                    Cursor.Current = Cursors.SizeAll;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = slide.StartWidth;
                                    var afterPos = new MoveSlideOperation.NotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveSlideOperation(slide, beforePos, afterPos));
                                });
                            */
                        }

                        return null;
                    }


                    IObservable<MouseEventArgs> surfaceNotesHandler()
                    {
                        foreach (var note in Notes.FXs.Reverse().Where(q => q.StartTick >= HeadTick && q.StartTick+q.Duration <= tailTick))
                        {
                            var subscription = NoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.BTs.Reverse().Where(q => q.StartTick >= HeadTick && q.StartTick + q.Duration <= tailTick))
                        {
                            var subscription = NoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Lasers.Reverse().Where(q => q.StartTick >= HeadTick && q.StartTick + q.Duration <= tailTick))
                        {
                            var subscription = MoveLaserHandler(note);
                            if (subscription != null) return subscription;
                        }

                        return null;
                    }


                    // If there is nothing, add it!
                    if ((NoteType.FXChip | NoteType.BTChip).HasFlag(NewNoteType))
                    {
                        NoteBase newNote = null;
                        IOperation op = null;
                        switch (NewNoteType)
                        {
                            case NoteType.FXChip:
                                var fxNote = new FXNote();
                                Notes.Add(fxNote);
                                newNote = fxNote;
                                op = new InsertFXOperation(Notes, fxNote);
                                break;

                            case NoteType.BTChip:
                                var btNote = new BTNote();
                                Notes.Add(btNote);
                                newNote = btNote;
                                op = new InsertBTOperation(Notes, btNote);
                                break;

                        }
                        newNote.StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0);
                        newNote.LaneIndex = GetNewNoteLaneIndex(scorePos.X);
                        newNote.Duration = 0;
                        Invalidate();
                        return moveNoteHandler(newNote)
                            .Finally(() => OperationManager.Push(op));
                    }
                    else
                    {
                        switch (NewNoteType)
                        {
                            case NoteType.FXLong:
                                var fxLong = new FXNote
                                {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Duration = (int)QuantizeTick
                                };
                                fxLong.LaneIndex = GetNewNoteLaneIndex(scorePos.X);
                                Notes.Add(fxLong);
                                Invalidate();
                                return DurationHandler(fxLong)
                                    .Finally(() => OperationManager.Push(new InsertFXOperation(Notes, fxLong)));

                            case NoteType.BTLong:
                                var btLong = new FXNote
                                {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Duration = (int)QuantizeTick
                                };
                                btLong.LaneIndex = GetNewNoteLaneIndex(scorePos.X);
                                Notes.Add(btLong);
                                Invalidate();
                                return DurationHandler(btLong)
                                    .Finally(() => OperationManager.Push(new InsertBTOperation(Notes, btLong)));

                            case NoteType.LaserLeft:
                                // Clicked on the body of laser
                                /*
                                foreach (var note in Notes.Slides.Reverse())
                                {
                                    var bg = new Slide.TapBase[] { note.StartNote }.Concat(note.StepNotes.OrderBy(q => q.Tick)).ToList();
                                    for (int i = 0; i < bg.Count - 1; i++)
                                    {
                                        // 描画時のコードコピペつらい
                                        var path = NoteGraphics.GetSlideBackgroundPath(
                                            (UnitLaneWidth + BorderThickness) * bg[i].Width - BorderThickness,
                                            (UnitLaneWidth + BorderThickness) * bg[i + 1].Width - BorderThickness,
                                            (UnitLaneWidth + BorderThickness) * bg[i].LaneIndex,
                                            GetYPositionFromTick(bg[i].Tick),
                                            (UnitLaneWidth + BorderThickness) * bg[i + 1].LaneIndex,
                                            GetYPositionFromTick(bg[i + 1].Tick));
                                        if (path.PathPoints.ContainsPoint(scorePos))
                                        {
                                            int tickOffset = GetQuantizedTick(GetTickFromYPosition(scorePos.Y)) - note.StartTick;
                                            // 同一Tickに追加させない
                                            if (tickOffset != 0 && !note.StepNotes.Any(q => q.TickOffset == tickOffset))
                                            {
                                                int width = note.StepNotes.OrderBy(q => q.TickOffset).LastOrDefault(q => q.TickOffset <= tickOffset)?.Width ?? note.StartWidth;
                                                int laneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - width / 2;
                                                laneIndex = Math.Min(Constants.LanesCount - width, Math.Max(0, laneIndex));
                                                var newStep = new Slide.StepTap(note)
                                                {
                                                    TickOffset = tickOffset,
                                                    IsVisible = IsNewSlideStepVisible
                                                };
                                                newStep.SetPosition(laneIndex - note.StartLaneIndex, width - note.StartWidth);
                                                note.StepNotes.Add(newStep);
                                                Invalidate();
                                                return moveSlideStepNoteHandler(newStep)
                                                    .Finally(() => OperationManager.Push(new InsertSlideStepNoteOperation(note, newStep)));
                                            }
                                        }
                                    }
                                }
                                */
                                // NEW SLIDE
                                // TODO: Placing slam
                                var laserL = new LaserNote
                                {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Duration = (int)QuantizeTick,
                                    LaneIndex = 0
                                };
                                Notes.Add(laserL);
                                Invalidate();
                                return MoveLaserHandler(laserL)
                                    .Finally(() => OperationManager.Push(new InsertLaserOperation(Notes, laserL)));
                            case NoteType.LaserRight:
                                var laserR = new LaserNote
                            {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Duration = (int)QuantizeTick,
                                    LaneIndex = 0
                                };
                                Notes.Add(laserR);
                                Invalidate();
                                return MoveLaserHandler(laserR)
                                    .Finally(() => OperationManager.Push(new InsertLaserOperation(Notes, laserR)));
                        }
                    }
                    return Observable.Empty<MouseEventArgs>();
                }).Subscribe(p => Invalidate());


            IObservable<MouseEventArgs> rangeSelection(PointF startPos)
            {
                SelectedRange = new SelectionRange()
                {
                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startPos.Y)), 0),
                    Duration = 0,
                };

                return mouseMove.TakeUntil(mouseUp)
                    .Do(q =>
                    {
                        Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                        currentMatrix.Invert();
                        var scorePos = currentMatrix.TransformPoint(q.Location);
                        int endTick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y));

                        SelectedRange = new SelectionRange()
                        {
                            StartTick = SelectedRange.StartTick,
                            Duration = endTick - SelectedRange.StartTick,
                        };
                    });
            }

            var eraseSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Erase || (p.Button == MouseButtons.Right && EditMode == EditMode.Edit))
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);
                    return rangeSelection(startScorePos)
                        .Count()
                        .Zip(mouseUp, (q, r) => new { Pos = r.Location, Count = q });
                })
                .Do(p =>
                {
                    if (p.Count > 0) // Selected by dragging
                    {
                        RemoveSelectedNotes();
                        SelectedRange = SelectionRange.Empty;
                        return;
                    }

                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Pos);


                    foreach (var btNote in Notes.BTs.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(btNote.StartTick, btNote.LaneIndex);
                        if (rect.Contains(scorePos))
                        {
                            var op = new RemoveBTOperation(Notes, btNote);
                            Notes.Remove(btNote);
                            OperationManager.Push(op);
                            return;
                        }
                    }

                    foreach (var fxNote in Notes.FXs.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(fxNote.StartTick, fxNote.LaneIndex);
                        if (rect.Contains(scorePos))
                        {
                            var op = new RemoveBTOperation(Notes, fxNote);
                            Notes.Remove(fxNote);
                            OperationManager.Push(op);
                            return;
                        }
                    }


                    foreach (var laserNote in Notes.Lasers.Reverse())
                    {
                        RectangleF startRect = GetClickableRectFromNotePosition(laserNote.StartTick, (int)laserNote.StartPosition);
                        if (startRect.Contains(scorePos))
                        {
                            var op = new RemoveLaserOperation(Notes, laserNote);
                            Notes.Remove(laserNote);
                            OperationManager.Push(op);
                            return;
                        }
                    }
                })
                .Subscribe(p => Invalidate());

            var selectSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => (p.Button == MouseButtons.Left && EditMode == EditMode.Select))
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);

                    if (GetSelectionRect().Contains(Point.Ceiling(startScorePos)))
                    {
                        int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
                        int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
                        int startTick = SelectedRange.StartTick;
                        int startLaneIndex = SelectedRange.StartLaneIndex;
                        int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

                        var selectedNotes = GetSelectedNotes();
                        var dicShortNotes = selectedNotes.GetShortNotes().ToDictionary(q => q, q => new MoveNoteOperation.NotePosition(q.StartTick, q.LaneIndex));
                        var dicHolds = selectedNotes.Holds.ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.StartTick, q.LaneIndex, q.Width));
                        var dicSlides = selectedNotes.Slides.ToDictionary(q => q, q => new MoveSlideOperation.NotePosition(q.StartTick, q.StartLaneIndex, q.StartWidth));

                        // move selection
                        return mouseMove.TakeUntil(mouseUp).Do(q =>
                        {
                            Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                            currentMatrix.Invert();
                            var scorePos = currentMatrix.TransformPoint(q.Location);

                            int xdiff = (int)((scorePos.X - startScorePos.X) / (UnitLaneWidth + BorderThickness));
                            int laneIndex = startLaneIndex + xdiff;

                            SelectedRange = new SelectionRange()
                            {
                                StartTick = startTick + Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y) - GetTickFromYPosition(startScorePos.Y)), -startTick - (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0)),
                                Duration = SelectedRange.Duration,
                                StartLaneIndex = Math.Min(Math.Max(laneIndex, 0), Constants.LanesCount - SelectedRange.SelectedLanesCount),
                                SelectedLanesCount = SelectedRange.SelectedLanesCount
                            };

                            foreach (var item in dicShortNotes)
                            {
                                item.Key.Tick = item.Value.Tick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            // Long notes are only for those that are included in the range, so we are not thinking of moving out of range.
                            foreach (var item in dicHolds)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            foreach (var item in dicSlides)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                                item.Key.StartLaneIndex = item.Value.StartLaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            Invalidate();
                        })
                        .Finally(() =>
                        {
                            var opShortNotes = dicShortNotes.Select(q =>
                            {
                                var after = new MoveNoteOperation.NotePosition(q.Key.Tick, q.Key.LaneIndex);
                                return new MoveShortNoteOperation(q.Key, q.Value, after);
                            });

                            var opHolds = dicHolds.Select(q =>
                            {
                                var after = new MoveHoldOperation.NotePosition(q.Key.StartTick, q.Key.LaneIndex, q.Key.Width);
                                return new MoveHoldOperation(q.Key, q.Value, after);
                            });

                            var opSlides = dicSlides.Select(q =>
                            {
                                var after = new MoveSlideOperation.NotePosition(q.Key.StartTick, q.Key.StartLaneIndex, q.Key.StartWidth);
                                return new MoveSlideOperation(q.Key, q.Value, after);
                            });

                            // 同じ位置に戻ってきたら操作扱いにしない
                            if (startTick == SelectedRange.StartTick && startLaneIndex == SelectedRange.StartLaneIndex) return;
                            OperationManager.Push(new CompositeOperation("ノーツの移動", opShortNotes.Cast<IOperation>().Concat(opHolds).Concat(opSlides).ToList()));
                        });
                    }
                    else
                    {
                        // 範囲選択
                        CurrentTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startScorePos.Y)), 0);
                        return rangeSelection(startScorePos);
                    }
                }).Subscribe();

            Subscriptions.Add(editSubscription);
            Subscriptions.Add(eraseSubscription);
            Subscriptions.Add(selectSubscription);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Matrix matrix = GetDrawingMatrix(new Matrix());
            matrix.Invert();

            if (EditMode == EditMode.Select && Editable)
            {
                var scorePos = matrix.TransformPoint(e.Location);
                Cursor = GetSelectionRect().Contains(scorePos) ? Cursors.SizeAll : Cursors.Default;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button == MouseButtons.Right)
            {
                EditMode = EditMode == EditMode.Edit ? EditMode.Select : EditMode.Edit;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            // Y軸の正方向をTick増加方向として描画 (y = 0 はコントロール下端)
            // コントロールの中心に描画したいなら後でTranslateしといてね
            var prevMatrix = pe.Graphics.Transform;
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix);

            var dc = new DrawingContext(pe.Graphics, ColorProfile);

            float laneWidth = LaneWidth;
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // レーン分割線描画
            using (var lightPen = new Pen(LaneBorderLightColor, BorderThickness))
            using (var darkPen = new Pen(LaneBorderDarkColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(i % 2 == 0 ? lightPen : darkPen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // 時間ガイドの描画
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

            // ノート描画
            var holds = Notes.Holds.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            // ロングノーツ背景
            // HOLD
            foreach (var hold in holds)
            {
                dc.DrawHoldBackground(new RectangleF(
                    (UnitLaneWidth + BorderThickness) * hold.LaneIndex + BorderThickness,
                    GetYPositionFromTick(hold.StartTick),
                    (UnitLaneWidth + BorderThickness) * hold.Width - BorderThickness,
                    GetYPositionFromTick(hold.Duration) - GetYPositionFromTick(0)
                    ));
            }

            // SLIDE
            var slides = Notes.Slides.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
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

            var airs = Notes.Airs.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick).ToList();
            var airActions = Notes.AirActions.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();

            // AIR-ACTION(ガイド線)
            foreach (var note in airActions)
            {
                dc.DrawAirHoldLine(
                    (UnitLaneWidth + BorderThickness) * (note.ParentNote.LaneIndex + note.ParentNote.Width / 2f),
                    GetYPositionFromTick(note.StartTick),
                    GetYPositionFromTick(note.StartTick + note.GetDuration()),
                    ShortNoteHeight);
            }

            // ロングノーツ終点AIR
            foreach (var note in airs)
            {
                if (!(note.ParentNote is LongNoteTapBase)) continue;
                RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
                dc.DrawAirStep(rect);
            }

            // 中継点
            foreach (var hold in holds)
            {
                if (Notes.GetReferencedAir(hold.EndNote).Count() > 0) continue; // AIR付き終点
                dc.DrawHoldEnd(GetRectFromNotePosition(hold.StartTick + hold.Duration, hold.LaneIndex, hold.Width));
            }

            foreach (var slide in slides)
            {
                foreach (var step in slide.StepNotes.OrderBy(p => p.TickOffset))
                {
                    if (!Editable && !step.IsVisible) continue;
                    if (Notes.GetReferencedAir(step).Count() > 0) break; // AIR付き終点
                    RectangleF rect = GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                    if (step.IsVisible) dc.DrawSlideStep(rect);
                    else dc.DrawBorder(rect);
                }
            }

            // 始点
            foreach (var hold in holds)
            {
                dc.DrawHoldBegin(GetRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width));
            }

            foreach (var slide in slides)
            {
                dc.DrawSlideBegin(GetRectFromNotePosition(slide.StartTick, slide.StartNote.LaneIndex, slide.StartWidth));
            }

            // TAP, ExTAP, FLICK, DAMAGE
            foreach (var note in Notes.Flicks.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawFlick(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Taps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawTap(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.ExTaps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawExTap(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Damages.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawDamage(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            // AIR-ACTION(ActionNote)
            foreach (var action in airActions)
            {
                foreach (var note in action.ActionNotes)
                {
                    dc.DrawAirAction(GetRectFromNotePosition(action.StartTick + note.Offset, action.ParentNote.LaneIndex, action.ParentNote.Width).Expand(-ShortNoteHeight * 0.28f));
                }
            }

            // AIR
            foreach (var note in airs)
            {
                RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
                dc.DrawAir(rect, note.VerticalDirection, note.HorizontalDirection);
            }

            // 選択範囲描画
            if (Editable) DrawSelectionRange(pe.Graphics);

            // Y軸反転させずにTick = 0をY軸原点とする座標系へ
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix, false);

            using (var font = new Font("MS Gothic", 8))
            {
                SizeF strSize = pe.Graphics.MeasureString("000", font);

                // 小節番号描画
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

                // BPM描画
                using (var bpmBrush = new SolidBrush(Color.FromArgb(0, 192, 0)))
                {
                    foreach (var item in ScoreEvents.BpmChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(Regex.Replace(item.Bpm.ToString(), @"\.0$", "").PadLeft(3), font, Brushes.Lime, point);
                    }
                }

                // 拍子記号描画
                using (var sigBrush = new SolidBrush(Color.FromArgb(216, 116, 0)))
                {
                    foreach (var item in sigs.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0}/{1}", item.Numerator, item.Denominator), font, sigBrush, point);
                    }
                }

                // ハイスピ描画
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

        private Matrix GetDrawingMatrix(Matrix baseMatrix)
        {
            return GetDrawingMatrix(baseMatrix, true);
        }

        private Matrix GetDrawingMatrix(Matrix baseMatrix, bool flipY)
        {
            Matrix matrix = baseMatrix.Clone();
            if (flipY)
            {
                // 反転してY軸増加方向を時間軸に
                matrix.Scale(1, -1);
            }
            // ずれたコントロール高さ分を補正
            matrix.Translate(0, ClientSize.Height - 1, MatrixOrder.Append);
            // 水平方向に対して中央に寄せる
            matrix.Translate((ClientSize.Width - LaneWidth) / 2, 0);

            return matrix;
        }

        private float GetYPositionFromTick(int tick)
        {
            return (tick - HeadTick) * UnitBeatHeight / UnitBeatTick;
        }

        protected int GetTickFromYPosition(float y)
        {
            return (int)(y * UnitBeatTick / UnitBeatHeight) + HeadTick;
        }

        protected int GetQuantizedTick(int tick)
        {
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            int head = 0;
            for (int i = 0; i < sigs.Count; i++)
            {
                int barTick = UnitBeatTick * 4 * sigs[i].Numerator / sigs[i].Denominator;

                if (i < sigs.Count - 1)
                {
                    int nextHead = head + (sigs[i + 1].Tick - head) / barTick * barTick;
                    if (tick >= nextHead)
                    {
                        head = nextHead;
                        continue;
                    }
                }

                int headBarTick = head + (tick - head) / barTick * barTick;
                int offsetCount = (int)Math.Round((float)(tick - headBarTick) / QuantizeTick);
                int maxOffsetCount = (int)(barTick / QuantizeTick);
                int remnantTick = barTick - (int)(maxOffsetCount * QuantizeTick);
                return headBarTick + ((tick - headBarTick >= barTick - remnantTick / 2) ? barTick : (int)(offsetCount * QuantizeTick));
            }

            throw new InvalidOperationException();
        }

        private RectangleF GetRectFromNotePosition(int tick, int laneIndex)
        {
            return new RectangleF(
                (UnitLaneWidth + BorderThickness) * laneIndex + BorderThickness,
                GetYPositionFromTick(tick) - ShortNoteHeight / 2,
                (UnitLaneWidth + BorderThickness) - BorderThickness,
                ShortNoteHeight
                );
        }

        private RectangleF GetClickableRectFromNotePosition(int tick, int laneIndex)
        {
            return GetRectFromNotePosition(tick, laneIndex).Expand(1, 3);
        }

        private int GetNewNoteLaneIndex(float xpos)
        {
            int newNoteLaneIndex = (int)Math.Round(xpos / (UnitLaneWidth + BorderThickness));
            return Math.Min(Constants.LanesCount, Math.Max(0, newNoteLaneIndex));
        }


        private Rectangle GetSelectionRect()
        {
            int minTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick + SelectedRange.Duration : SelectedRange.StartTick;
            int maxTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick : SelectedRange.StartTick + SelectedRange.Duration;
            var start = new Point(SelectedRange.StartLaneIndex * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(minTick) - ShortNoteHeight);
            var end = new Point((SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount) * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(maxTick) + ShortNoteHeight);
            return new Rectangle(start.X, start.Y, end.X - start.X, end.Y - start.Y);
        }

        protected void DrawSelectionRange(Graphics g)
        {
            Rectangle selectedRect = GetSelectionRect();
            g.DrawXorRectangle(PenStyles.Dot, g.Transform.TransformPoint(selectedRect.Location), g.Transform.TransformPoint(selectedRect.Location + selectedRect.Size));
        }

        public Core.NoteCollection GetSelectedNotes()
        {
            int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
            int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
            int startLaneIndex = SelectedRange.StartLaneIndex;
            int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

            var c = new Core.NoteCollection();

            bool contained(IAirable p) => p.Tick >= minTick && p.Tick <= maxTick & p.LaneIndex >= startLaneIndex && p.LaneIndex + p.Width <= endLaneIndex;
            c.Taps.AddRange(Notes.Taps.Where(p => contained(p)));
            c.ExTaps.AddRange(Notes.ExTaps.Where(p => contained(p)));
            c.Flicks.AddRange(Notes.Flicks.Where(p => contained(p)));
            c.Damages.AddRange(Notes.Damages.Where(p => contained(p)));
            c.Holds.AddRange(Notes.Holds.Where(p => p.StartTick >= minTick && p.StartTick + p.Duration <= maxTick && p.LaneIndex >= startLaneIndex && p.LaneIndex + p.Width <= endLaneIndex));
            c.Slides.AddRange(Notes.Slides.Where(p => p.StartTick >= minTick && p.StartTick + p.GetDuration() <= maxTick && p.StartLaneIndex >= startLaneIndex && p.StartLaneIndex + p.StartWidth <= endLaneIndex && p.StepNotes.All(r => r.LaneIndex >= startLaneIndex && r.LaneIndex + r.Width <= endLaneIndex)));

            var airables = c.GetShortNotes().Cast<IAirable>()
                .Concat(c.Holds.Select(p => p.EndNote))
                .Concat(c.Slides.SelectMany(p => p.StepNotes))
                .ToList();
            c.Airs.AddRange(airables.SelectMany(p => Notes.GetReferencedAir(p)));
            // AIR-ACTIONはとりあえず全コピー
            c.AirActions.AddRange(airables.SelectMany(p => Notes.GetReferencedAirAction(p)));
            return c;
        }

        public void SelectAll()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = Notes.GetLastTick(),
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void SelectToEnd()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = CurrentTick,
                Duration = Notes.GetLastTick() - CurrentTick,
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void SelectToBeginning()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = CurrentTick,
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void CutSelectedNotes()
        {
            CopySelectedNotes();
            RemoveSelectedNotes();
        }

        public void CopySelectedNotes()
        {
            var data = new SelectionData(SelectedRange.StartTick + Math.Min(SelectedRange.Duration, 0), UnitBeatTick, GetSelectedNotes());
            Clipboard.SetDataObject(data, true);
        }

        public void PasteNotes()
        {
            var op = PasteNotes(p => { });
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        public void PasteFlippedNotes()
        {
            var op = PasteNotes(p => FlipNotes(p.SelectedNotes));
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// クリップボードにコピーされたノーツをペーストしてその操作を表す<see cref="IOperation"/>を返します。
        /// ペーストするノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="action">選択データに対して適用するアクション</param>
        /// <returns>ペースト操作を表す<see cref="IOperation"/></returns>
        protected IOperation PasteNotes(Action<SelectionData> action)
        {
            var obj = Clipboard.GetDataObject();
            if (obj == null || !obj.GetDataPresent(typeof(SelectionData))) return null;

            var data = obj.GetData(typeof(SelectionData)) as SelectionData;
            if (data.IsEmpty) return null;

            double tickFactor = UnitBeatTick / (double)data.TicksPerBeat;
            int originTick = (int)(data.StartTick * tickFactor);
            if (data.TicksPerBeat != UnitBeatTick)
                data.SelectedNotes.UpdateTicksPerBeat(tickFactor);

            foreach (var note in data.SelectedNotes.GetShortNotes())
            {
                note.StartTick = note.StartTick - originTick + CurrentTick;
            }

            foreach (var hold in data.SelectedNotes.Holds)
            {
                hold.StartTick = hold.StartTick - originTick + CurrentTick;
            }

            foreach (var slide in data.SelectedNotes.Slides)
            {
                slide.StartTick = slide.StartTick - originTick + CurrentTick;
            }

            foreach (var airAction in data.SelectedNotes.AirActions)
            {
                // AIR-ACTIONの親ノート復元できないんやった……クソ設計だわ……
                var notes = airAction.ActionNotes.Select(p => new AirAction.ActionNote(airAction) { Offset = p.Offset }).ToList();
                airAction.ActionNotes.Clear();
                airAction.ActionNotes.AddRange(notes);
            }

            action(data);

            var op = data.SelectedNotes.Taps.Select(p => new InsertTapOperation(Notes, p)).Cast<IOperation>()
                .Concat(data.SelectedNotes.ExTaps.Select(p => new InsertExTapOperation(Notes, p)))
                .Concat(data.SelectedNotes.Flicks.Select(p => new InsertFlickOperation(Notes, p)))
                .Concat(data.SelectedNotes.Damages.Select(p => new InsertDamageOperation(Notes, p)))
                .Concat(data.SelectedNotes.Holds.Select(p => new InsertHoldOperation(Notes, p)))
                .Concat(data.SelectedNotes.Slides.Select(p => new InsertSlideOperation(Notes, p)))
                .Concat(data.SelectedNotes.Airs.Select(p => new InsertAirOperation(Notes, p)))
                .Concat(data.SelectedNotes.AirActions.Select(p => new InsertAirActionOperation(Notes, p)));
            var composite = new CompositeOperation("クリップボードからペースト", op.ToList());
            composite.Redo(); // 追加書くの面倒になったので許せ
            return composite;
        }

        public void RemoveSelectedNotes()
        {
            var selected = GetSelectedNotes();

            var airs = selected.Airs.ToList().Select(p =>
            {
                Notes.Remove(p);
                return new RemoveAirOperation(Notes, p);
            });
            var airActions = selected.AirActions.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveAirActionOperation(Notes, p);
            }).ToList();

            var taps = selected.Taps.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveTapOperation(Notes, p);
            });
            var extaps = selected.ExTaps.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveExTapOperation(Notes, p);
            });
            var flicks = selected.Flicks.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveFlickOperation(Notes, p);
            });
            var damages = selected.Damages.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveDamageOperation(Notes, p);
            });
            var holds = selected.Holds.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveHoldOperation(Notes, p);
            });
            var slides = selected.Slides.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveSlideOperation(Notes, p);
            });

            var opList = taps.Cast<IOperation>().Concat(extaps).Concat(flicks).Concat(damages)
                .Concat(holds).Concat(slides)
                .Concat(airs).Concat(airActions)
                .ToList();

            if (opList.Count == 0) return;
            OperationManager.Push(new CompositeOperation("選択範囲内ノーツ削除", opList));
            Invalidate();
        }

        public void RemoveSelectedEvents()
        {
            int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
            int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
            bool isContained(EventBase p) => p.Tick != 0 && minTick <= p.Tick && maxTick >= p.Tick;
            var events = ScoreEvents;

            var bpmOp = events.BpmChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<BpmChangeEvent>(events.BpmChangeEvents, p);
            });

            var speedOp = events.HighSpeedChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<HighSpeedChangeEvent>(events.HighSpeedChangeEvents, p);
            });

            var signatureOp = events.TimeSignatureChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<TimeSignatureChangeEvent>(events.TimeSignatureChangeEvents, p);
            });

            OperationManager.InvokeAndPush(new CompositeOperation("イベント削除", bpmOp.Cast<IOperation>().Concat(speedOp).Concat(signatureOp).ToList()));
            Invalidate();
        }

        public void FlipSelectedNotes()
        {
            var op = FlipNotes(GetSelectedNotes());
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// 指定のコレクション内のノーツを反転してその操作を表す<see cref="IOperation"/>を返します。
        /// 反転するノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="notes">反転対象となるノーツを含む<see cref="Core.NoteCollection"/></param>
        /// <returns>反転操作を表す<see cref="IOperation"/></returns>
        protected IOperation FlipNotes(Core.NoteCollection notes)
        {
            var dicShortNotes = notes.GetShortNotes().ToDictionary(q => q, q => new MoveNoteOperation.NotePosition(q.StartTick, q.LaneIndex));
            var dicHolds = notes.Holds.ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.StartTick, q.LaneIndex, q.Width));
            var dicSlides = notes.Slides;
            var referenced = new NoteCollection(notes);
            var airs = notes.GetShortNotes().Cast<IAirable>()
                .Concat(notes.Holds.Select(p => p.EndNote))
                .Concat(notes.Slides.Select(p => p.StepNotes.OrderByDescending(q => q.TickOffset).First()))
                .SelectMany(p => referenced.GetReferencedAir(p));

            var opShortNotes = dicShortNotes.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex - p.Key.Width;
                var after = new MoveNoteOperation.NotePosition(p.Key.Tick, p.Key.LaneIndex);
                return new MoveShortNoteOperation(p.Key, p.Value, after);
            });

            var opHolds = dicHolds.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex - p.Key.Width;
                var after = new MoveHoldOperation.NotePosition(p.Key.StartTick, p.Key.LaneIndex, p.Key.Width);
                return new MoveHoldOperation(p.Key, p.Value, after);
            });

            var opSlides = dicSlides.Select(p =>
            {
                p.Flip();
                return new FlipSlideOperation(p);
            });

            var opAirs = airs.Select(p =>
            {
                p.Flip();
                return new FlipAirHorizontalDirectionOperation(p);
            });

            var opList = opShortNotes.Cast<IOperation>().Concat(opHolds).Concat(opSlides).Concat(opAirs).ToList();
            return opList.Count == 0 ? null : new CompositeOperation("ノーツの反転", opList);
        }


        public void Initialize()
        {
            SelectedRange = SelectionRange.Empty;
            CurrentTick = SelectedRange.StartTick;
            Invalidate();
        }

        public void Initialize(Score score)
        {
            Initialize();
            UpdateScore(score);
        }

        public void UpdateScore(Score score)
        {
            UnitBeatTick = score.TicksPerBeat;
            if (NoteCollectionCache.ContainsKey(score))
            {
                Notes = NoteCollectionCache[score];
            }
            else
            {
                Notes = new NoteCollection(score.Notes);
                NoteCollectionCache.Add(score, Notes);
            }
            ScoreEvents = score.Events;
            Invalidate();
        }

        public class NoteCollection
        {
            public event EventHandler NoteChanged;

            private Core.NoteCollection source = new Core.NoteCollection();

            public IReadOnlyCollection<FXNote> FXs { get { return source.FXs; } }
            public IReadOnlyCollection<BTNote> BTs { get { return source.BTs; } }
            public IReadOnlyCollection<LaserNote> Lasers { get { return source.Lasers; } }


            public NoteCollection(Core.NoteCollection src)
            {
                Load(src);
            }

            public void Add(FXNote note)
            {
                source.FXs.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(BTNote note)
            {
                source.BTs.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(LaserNote note)
            {
                source.Lasers.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }



            public void Remove(FXNote note)
            {
                source.FXs.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(BTNote note)
            {
                source.BTs.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(LaserNote note)
            {
                source.Lasers.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public int GetLastTick()
            {
                var Notes = FXs.Cast<NoteBase>().Concat(BTs).Concat(Lasers).ToList();
                int lastNoteTick = Notes.Count == 0 ? 0 : Notes.Max(p => p.StartTick + p.Duration);
                return lastNoteTick;
            }


            public void Load(Core.NoteCollection collection)
            {
                Clear();

                foreach (var note in collection.BTs) Add(note);
                foreach (var note in collection.FXs) Add(note);
                foreach (var note in collection.Lasers) Add(note);
            }

            public void Clear()
            {
                source = new Core.NoteCollection();

                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void UpdateTicksPerBeat(double factor)
            {
                source.UpdateTicksPerBeat(factor);
            }
        }
    }
}
