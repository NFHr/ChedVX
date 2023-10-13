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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChedVX.UI
{
    public partial class NoteView
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
    }
}
