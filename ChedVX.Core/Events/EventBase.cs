using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core.Events
{
    /// <summary>
    /// This class represents an event in a musical score.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}")]
    public abstract class EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int tick;

        /// <summary>
        /// Gets or sets the Tick value representing the position of this event.
        /// </summary>
        public int Tick
        {
            get { return tick; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "Tick must be greater than or equal to 0.");
                tick = value;
            }
        }
    }
}
