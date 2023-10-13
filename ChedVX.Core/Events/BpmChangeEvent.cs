using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core.Events
{
    /// <summary>
    /// This class represents a BPM change event.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}, Value = {Bpm}, Stop: {4}")]
    public class BpmChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private double bpm;
        [Newtonsoft.Json.JsonProperty]
        private bool isStop;

        public double Bpm
        {
            get { return bpm; }
            set { bpm = value; }
        }

        public bool IsStop
        {
            get { return isStop; }
            set { isStop = value; }
        }
    }
}
