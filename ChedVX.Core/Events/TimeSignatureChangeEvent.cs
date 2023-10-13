using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core.Events
{
    /// <summary>
    /// Represents a change in time signature.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}, Value = {Numerator} / {Denominator}")]
    public class TimeSignatureChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int numerator;
        [Newtonsoft.Json.JsonProperty]
        private int denominatorExponent;

        /// <summary>
        /// Set the time signature numerator.
        /// </summary>
        public int Numerator
        {
            get { return numerator; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value must be greater than 0.");
                numerator = value;
            }
        }

        /// <summary>
        /// Gets the denominator of the time signature.
        /// </summary>
        public int Denominator
        {
            get
            {
                int p = 1;
                for (int i = 0; i < DenominatorExponent; i++) p *= 2;
                return p;
            }
        }

        /// <summary>
        /// Sets the exponent of the base 2 denominator of the time signature.
        /// </summary>
        public int DenominatorExponent
        {
            get { return denominatorExponent; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value must be positive.");
                denominatorExponent = value;
            }
        }
    }
}
