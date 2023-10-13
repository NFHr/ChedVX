using ChedVX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI
{
    /// <summary>
    /// Represents a selection range.
    /// </summary>
    public struct SelectionRange
    {
        public static SelectionRange Empty = new SelectionRange()
        {
            StartTick = 0,
            Duration = 0,
        };

        private int startTick;
        private int duration;

        /// <summary>
        /// Sets the Tick that started the selection.
        /// </summary>
        public int StartTick
        {
            get { return startTick; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value must not be negative.");
                startTick = value;
            }
        }

        /// <summary>
        /// Sets the Tick that represents the offset from <see cref="StartTick"/> when the selection ends.
        /// If this value is negative, it means that the range before <see cref="StartTick"/> is selected.
        /// </summary>
        public int Duration
        {
            get { return duration; }
            set
            {
                duration = value;
            }
        }
    }
}
