using ChedVX.Core;
using ChedVX.Core.Events;
using ChedVX.Core.Notes;
using ChedVX.Drawing;
using ChedVX.UI.Operations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public event EventHandler UnitLaneWidthChanged;
        public event EventHandler UnitBeatHeightChanged;
        public event EventHandler HeadTickChanged;
        public event EventHandler EditModeChanged;
        public event EventHandler SelectedRangeChanged;
        public event EventHandler NewNoteTypeChanged;
        //public event EventHandler AirDirectionChanged;
        public event EventHandler DragScroll;

        private Color barLineColor = Color.FromArgb(160, 160, 160);
        private Color beatLineColor = Color.FromArgb(80, 80, 80);
        private Color laneBorderLightColor = Color.FromArgb(60, 60, 60);
        private Color laneBorderDarkColor = Color.FromArgb(30, 30, 30);
        private ColorProfile colorProfile;
        private int unitLaneWidth = 12;
        private int shortNoteHeight = 5;
        private int unitBeatTick = 480;
        private float unitBeatHeight = 120;

        private int headTick = 0;
        private bool editable = true;
        private EditMode editMode = EditMode.Edit;
        private int currentTick = 0;
        private SelectionRange selectedRange = SelectionRange.Empty;
        private NoteType newNoteType;

        /// <summary>
        /// Set the color of the measure separator line.
        /// </summary>
        public Color BarLineColor
        {
            get => barLineColor;
            set
            {
                barLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Set the color of the guide line for one beat.
        /// </summary>
        public Color BeatLineColor
        {
            get => beatLineColor;
            set
            {
                beatLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Set the main color of the lane guide line.
        /// </summary>
        public Color LaneBorderLightColor
        {
            get => laneBorderLightColor;
            set
            {
                laneBorderLightColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Set the secondary color of the lane guide line.
        /// </summary>
        public Color LaneBorderDarkColor
        {
            get => laneBorderDarkColor;
            set
            {
                laneBorderDarkColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Get the <see cref="ChedVX.Drawing.ColorProfile"/> used for drawing in Notes.
        /// </summary>
        public ColorProfile ColorProfile
        {
            get { return colorProfile; }
        }

        /// <summary>
        /// Set the display width per lane.
        /// </summary>
        public int UnitLaneWidth
        {
            get => unitLaneWidth;
            set
            {
                unitLaneWidth = value;
                Invalidate();
                UnitLaneWidthChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Get the visible width of the lane.
        /// </summary>
        public int LaneWidth
        {
            get { return UnitLaneWidth * Constants.LanesCount + BorderThickness * (Constants.LanesCount - 1); }
        }

        /// <summary>
        /// Get the width of the lane guide line.
        /// </summary>
        public int BorderThickness => UnitLaneWidth < 5 ? 0 : 1;

        /// <summary>
        /// Set the display height of short notes.
        /// </summary>
        public int ShortNoteHeight
        {
            get => shortNoteHeight;
            set
            {
                shortNoteHeight = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Set the number of ticks per beat.
        /// </summary>
        public int UnitBeatTick
        {
            get => unitBeatTick;
            set
            {
                unitBeatTick = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Set the display height per beat.
        /// </summary>
        public float UnitBeatHeight
        {
            get => unitBeatHeight;
            set
            {
                // It draws nicely with multiples of 6
                unitBeatHeight = value;
                Invalidate();
                UnitBeatHeightChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Specify the number of ticks to quantize.
        /// </summary>
        public double QuantizeTick { get; set; }

        /// <summary>
        /// Set the Tick at the start of the display.
        /// </summary>
        public int HeadTick
        {
            get => headTick;
            set
            {
                if (headTick == value) return;
                headTick = value;
                HeadTickChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// Get the Tick at the end of the display.
        /// </summary>
        public int TailTick
        {
            get { return HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight); }
        }

        /// <summary>
        /// Get the Tick to be used for the display margin at the start of the score.
        /// </summary>
        public int PaddingHeadTick
        {
            get { return UnitBeatTick / 8; }
        }

        /// <summary>
        /// Sets a value indicating whether Notes is editable.
        /// </summary>
        public bool Editable
        {
            get => editable;
            set
            {
                editable = value;
                Cursor = value ? Cursors.Default : Cursors.No;
            }
        }

        /// <summary>
        /// Set the edit mode.
        /// </summary>
        public EditMode EditMode
        {
            get => editMode;
            set
            {
                editMode = value;
                EditModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Set the current Tick.
        /// </summary>
        public int CurrentTick
        {
            get => currentTick;
            set
            {
                currentTick = value;
                if (currentTick < HeadTick || currentTick > TailTick)
                {
                    HeadTick = currentTick;
                    DragScroll?.Invoke(this, EventArgs.Empty);
                }
                Invalidate();
            }
        }

        /// <summary>
        /// Set the current selection.
        /// </summary>
        public SelectionRange SelectedRange
        {
            get => selectedRange;
            set
            {
                selectedRange = value;
                SelectedRangeChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// Set the note type to add.
        /// </summary>
        public NoteType NewNoteType
        {
            get => newNoteType;
            set
            {
                int bits = (int)value;
                bool isSingle = bits != 0 && (bits & (bits - 1)) == 0;
                if (!isSingle) throw new ArgumentException("value", "value must be single bit.");
                newNoteType = value;
                NewNoteTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Set the ratio of the note edge to the note width to be included in the collision determination.
        /// </summary>
        public float EdgeHitWidthRate { get; set; } = 0.2f;

        /// <summary>
        /// Get the lower limit of the hit detection width at the edge of the note.
        /// </summary>
        public float MinimumEdgeHitWidth => UnitLaneWidth * 0.4f;

        protected int LastWidth { get; set; } = 4;

        public NoteCollection Notes { get; private set; } = new NoteCollection(new Core.NoteCollection());

        public EventCollection ScoreEvents { get; set; } = new EventCollection();

        protected OperationManager OperationManager { get; }

        protected CompositeDisposable Subscriptions { get; } = new CompositeDisposable();

        private Dictionary<Score, NoteCollection> NoteCollectionCache { get; } = new Dictionary<Score, NoteCollection>();

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
            var start = new Point(UnitLaneWidth + BorderThickness, (int)GetYPositionFromTick(minTick) - ShortNoteHeight);
            var end = new Point(UnitLaneWidth + BorderThickness, (int)GetYPositionFromTick(maxTick) + ShortNoteHeight);
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

            var c = new Core.NoteCollection();

            bool contained(NoteBase p) => p.StartTick >= minTick && p.StartTick <= maxTick;

            // Short Notes
            c.FXs.AddRange(Notes.FXs.Where(p => contained(p)));
            c.BTs.AddRange(Notes.BTs.Where(p => contained(p)));
            c.Lasers.AddRange(Notes.Lasers.Where(p => contained(p)));

            // Long Notes
            c.FXs.AddRange(Notes.FXs.Where(p => p.StartTick >= minTick && p.StartTick + p.Duration <= maxTick && !p.IsChip));
            c.BTs.AddRange(Notes.BTs.Where(p => p.StartTick >= minTick && p.StartTick + p.Duration <= maxTick && !p.IsChip));
            c.Lasers.AddRange(Notes.Lasers.Where(p => p.StartTick >= minTick && p.StartTick + p.Duration <= maxTick && !p.IsChip));

            return c;
        }

        public void SelectAll()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = Notes.GetLastTick(),
            };
        }

        public void SelectToEnd()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = CurrentTick,
                Duration = Notes.GetLastTick() - CurrentTick,
            };
        }

        public void SelectToBeginning()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = CurrentTick,
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
        /// Pastes a note copied to the clipboard and returns<see cref="IOperation"/> representing the operation.
        /// If there are no notes to paste, return null.
        /// </summary>
        /// <param name = "action" > action to apply to selected data</param>
        /// <returns><see cref = "IOperation" /></ returns >
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

            foreach (var note in data.SelectedNotes.GetChips())
            {
                note.StartTick = note.StartTick - originTick + CurrentTick;
            }

            foreach (var hold in data.SelectedNotes.GetLongs())
            {
                hold.StartTick = hold.StartTick - originTick + CurrentTick;
            }

            foreach (var lasers in data.SelectedNotes.GetLasers())
            {
                lasers.StartTick = lasers.StartTick - originTick + CurrentTick;
            }

            action(data);

            var op = data.SelectedNotes.FXs.Select(p => new InsertFXOperation(Notes, p)).Cast<IOperation>()
                .Concat(data.SelectedNotes.BTs.Select(p => new InsertBTOperation(Notes, p)))
                .Concat(data.SelectedNotes.Lasers.Select(p => new InsertLaserOperation(Notes, p)));

            var composite = new CompositeOperation("Paste from clipboard", op.ToList());
            composite.Redo();
            return composite;
        }

        public void RemoveSelectedNotes()
        {
            var selected = GetSelectedNotes();

            var bts = selected.BTs.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveBTOperation(Notes, p);
            });
            var fxs = selected.FXs.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveFXOperation(Notes, p);
            }).ToList();

            var lasers = selected.Lasers.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveLaserOperation(Notes, p);
            });

            var opList = bts.Cast<IOperation>().Concat(fxs).Concat(fxs).Concat(lasers)
                .ToList();

            if (opList.Count == 0) return;
            OperationManager.Push(new CompositeOperation("Delete Notes in Selection", opList));
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

            OperationManager.InvokeAndPush(new CompositeOperation("Event Deletion", bpmOp.Cast<IOperation>().Concat(speedOp).Concat(signatureOp).ToList()));
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
        /// Reverses the notes in the specified collection and returns<see cref="IOperation"/> representing the operation.
        /// If there are no notes to flip, return null.
        /// </summary>
        /// <param name = "notes" > contains the notes to be reversed<see cref="Core.NoteCollection"/></param>
        /// <returns><see cref = "IOperation" /> representing the reverse operation</returns>
        protected IOperation FlipNotes(Core.NoteCollection notes)
        {
            /*
            var dicChips = notes.GetChips().ToDictionary(q => q, q => new MoveNoteTickOperation.NotePosition(q.StartTick));
            var dicLongs = notes.GetLongs().ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.StartTick, q.LaneIndex, q.Width));
            var dicLasers = notes.GetLasers();
            var referenced = new NoteCollection(notes);
            var airs = notes.GetShortNotes().Cast<IAirable>()
                .Concat(notes.Holds.Select(p => p.EndNote))
                .Concat(notes.Slides.Select(p => p.StepNotes.OrderByDescending(q => q.TickOffset).First()))
                .SelectMany(p => referenced.GetReferencedAir(p));

            var opShortNotes = dicShortNotes.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex - p.Key.Width;
                var after = new MoveNoteTickOperation.NotePosition(p.Key.Tick, p.Key.LaneIndex);
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
            */
            return null;
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

    public enum EditMode
    {
        Select,
        Edit,
        Erase,
    }

    [Flags]
    public enum NoteType
    {
        Cursor = 0,
        BTChip = 1 << 1,
        BTLong = 1 << 2,
        FXChip = 1 << 3,
        FXLong = 1 << 4,
        LaserLeft = 1 << 5,
        LaserRight = 1 << 6,
    }

    [Serializable]
    public class SelectionData
    {
        private string serializedText = null;

        [NonSerialized]
        private InnerData Data;

        public int StartTick
        {
            get
            {
                CheckRestored();
                return Data.StartTick;
            }
        }

        public Core.NoteCollection SelectedNotes
        {
            get
            {
                CheckRestored();
                return Data.SelectedNotes;
            }
        }

        public bool IsEmpty
        {
            get
            {
                CheckRestored();
                return SelectedNotes.GetChips().Count() == 0 && SelectedNotes.GetLongs().Count() == 0 && SelectedNotes.GetLasers().Count() == 0;
            }
        }

        public int TicksPerBeat
        {
            get
            {
                CheckRestored();
                return Data.TicksPerBeat;
            }
        }

        public SelectionData()
        {
        }

        public SelectionData(int startTick, int ticksPerBeat, NoteCollection notes)
        {
            Data = new InnerData(startTick, ticksPerBeat, notes);
            serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(Data, SerializerSettings);
        }

        protected void CheckRestored()
        {
            if (Data == null) Restore();
        }

        protected void Restore()
        {
            Data = Newtonsoft.Json.JsonConvert.DeserializeObject<InnerData>(serializedText, SerializerSettings);
        }

        protected static Newtonsoft.Json.JsonSerializerSettings SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { IgnoreSerializableAttribute = true }
        };

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        protected class InnerData
        {
            [Newtonsoft.Json.JsonProperty]
            private int startTick;

            [Newtonsoft.Json.JsonProperty]
            private int ticksPerBeat;

            [Newtonsoft.Json.JsonProperty]
            private NoteCollection selectedNotes;

            public int StartTick => startTick;
            public int TicksPerBeat => ticksPerBeat;
            public NoteCollection SelectedNotes => selectedNotes;

            public InnerData(int startTick, int ticksPerBeat, NoteCollection notes)
            {
                this.startTick = startTick;
                this.ticksPerBeat = ticksPerBeat;
                selectedNotes = notes;
            }
        }
    }

    internal static class UIExtensions
    {
        public static Core.NoteCollection Reposit(this NoteView.NoteCollection collection)
        {
            var res = new NoteCollection();
            res.BTs = collection.BTs.ToList();
            res.FXs = collection.FXs.ToList();
            res.Lasers = collection.Lasers.ToList();
            return res;
        }
    }

}
