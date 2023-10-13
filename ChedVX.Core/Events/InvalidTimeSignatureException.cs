using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core.Events
{
    /// <summary>
    /// Exception thrown when the time signature definition is invalid.
    /// </summary>
    [Serializable]
    public class InvalidTimeSignatureException : Exception
    {
        private static readonly string TickPropertyValue = "tick";

        /// <summary>
        /// Gets the Tick value representing the position of the invalid time signature definition.
        /// </summary>
        public int Tick { get; }

        public InvalidTimeSignatureException() : base()
        {
        }

        public InvalidTimeSignatureException(string message) : base(message)
        {
        }

        public InvalidTimeSignatureException(string message, Exception inner) : base(message, inner)
        {
        }

        public InvalidTimeSignatureException(string message, int tick) : this(message, tick, null)
        {
        }

        public InvalidTimeSignatureException(string message, int tick, Exception innerException) : base(message, innerException)
        {
            Tick = tick;
        }

        protected InvalidTimeSignatureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null) return;
            Tick = info.GetInt32(TickPropertyValue);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            if (info == null) return;

            info.AddValue(TickPropertyValue, Tick);
        }
    }
}
