using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChedVX.Core.Constants;

namespace ChedVX.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class NoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int startTick;

        [Newtonsoft.Json.JsonProperty]
        private int duration;

        [Newtonsoft.Json.JsonProperty]
        protected int laneIndex;

        /// <summary>
        /// Tick that represents the note position.
        /// </summary>
        public int StartTick
        {
            get => startTick;
            set
            {
                if (startTick == value) return;
                if (startTick < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                startTick = value;
            }
        }

        /// <summary>
        /// Duration of the note. (Chip / Slam when duration=0)
        /// </summary>
        public int Duration
        {
            get => duration;
            set
            {
                if (duration == value) return;
                if (duration < 0) throw new ArgumentOutOfRangeException("value", "value must be non-negative.");
                duration = value;
            }
        }

        /// <summary>
        /// Lane index of the note.
        /// </summary>
        public virtual int LaneIndex { get => laneIndex; set => laneIndex = value; }

        /// <summary>
        /// Track ID of the note.
        /// </summary>
        public virtual Tracks TrackID { get; }


        /// <summary>
        /// Return whether the note is a chip or slam (i.e. dutarion == 0)
        /// </summary>
        public bool IsChip => Duration == 0;

    }


}
