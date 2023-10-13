using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    /// <summary>
    /// This class represents chart data.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Score
    {
        [Newtonsoft.Json.JsonProperty]
        private int ticksPerBeat = 480;
        [Newtonsoft.Json.JsonProperty]
        private NoteCollection notes = new NoteCollection();
        [Newtonsoft.Json.JsonProperty]
        private EventCollection events = new EventCollection();

        /// <summary>
        /// Set the ticks per beat.
        /// </summary>
        public int TicksPerBeat
        {
            get { return ticksPerBeat; }
            set { ticksPerBeat = value; }
        }

        /// <summary>
        /// A collection that stores notes.
        /// </summary>
        public NoteCollection Notes
        {
            get { return notes; }
            set { notes = value; }
        }

        /// <summary>
        /// A collection that stores events
        /// </summary>
        public EventCollection Events
        {
            get { return events; }
            set { events = value; }
        }

        public void UpdateTicksPerBeat(int value)
        {
            double factor = value / TicksPerBeat;
            Notes.UpdateTicksPerBeat(factor);
            Events.UpdateTicksPerBeat(factor);
            TicksPerBeat = value;
        }

        public Score Clone()
        {
            var score = Newtonsoft.Json.JsonConvert.DeserializeObject<Score>(Newtonsoft.Json.JsonConvert.SerializeObject(this, ScoreBook.SerializerSettings));
            return score;
        }
    }
}
