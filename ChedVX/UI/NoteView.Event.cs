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
using System.Windows.Forms;

namespace ChedVX.UI
{
    public partial class NoteView : Control
    {
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
                        int beforeLaneIndex = (int)note.TrackID;
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

                        RectangleF startRect = GetClickableRectFromNotePosition(note.StartTick, note.LaneIndex);

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
                        foreach (var note in Notes.FXs.Reverse().Where(q => q.StartTick >= HeadTick && q.StartTick + q.Duration <= tailTick))
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

                        var selectedNotes = GetSelectedNotes();
                        var dicChipNotes = selectedNotes.GetChips().ToDictionary(q => q, q => new MoveNoteTickOperation.NotePosition(q.StartTick));
                        var dicLongs = selectedNotes.GetLongs().ToDictionary(q => q, q => new MoveNoteTickOperation.NotePosition(q.StartTick));
                        var dicLasers = selectedNotes.Lasers.ToDictionary(q => q, q => new MoveNoteTickOperation.NotePosition(q.StartTick));

                        // move selection
                        return mouseMove.TakeUntil(mouseUp).Do(q =>
                        {
                            Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                            currentMatrix.Invert();
                            var scorePos = currentMatrix.TransformPoint(q.Location);

                            SelectedRange = new SelectionRange()
                            {
                                StartTick = startTick + Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y) - GetTickFromYPosition(startScorePos.Y)), -startTick - (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0)),
                                Duration = SelectedRange.Duration,
                            };

                            foreach (var item in dicChipNotes)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);

                            }

                            // Long notes are only for those that are included in the range, so we are not thinking of moving out of range.
                            foreach (var item in dicLongs)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                            }

                            foreach (var item in dicLasers)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                            }

                            Invalidate();
                        })
                        .Finally(() =>
                        {
                            var opChipNotes = dicChipNotes.Select(q =>
                            {
                                var after = new MoveNoteTickOperation.NotePosition(q.Key.StartTick);
                                return new MoveNoteTickOperation(q.Key, q.Value, after);
                            });

                            var opLongs = dicLongs.Select(q =>
                            {
                                var after = new MoveNoteTickOperation.NotePosition(q.Key.StartTick);
                                return new MoveNoteTickOperation(q.Key, q.Value, after);
                            });

                            var opLasers = dicLasers.Select(q =>
                            {
                                var after = new MoveNoteTickOperation.NotePosition(q.Key.StartTick);
                                return new MoveNoteTickOperation(q.Key, q.Value, after);
                            });

                            // Don't handle it when it is the same position
                            if (startTick == SelectedRange.StartTick) return;
                            OperationManager.Push(new CompositeOperation("Move notes' start ticks.", opChipNotes.Cast<IOperation>().Concat(opLongs).Concat(opLasers).ToList()));
                        });
                    }
                    else
                    {
                        // Time range selection
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
    }
}
