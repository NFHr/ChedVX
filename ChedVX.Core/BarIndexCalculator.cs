using ChedVX.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    /// <summary>
    /// This class calculates the measure position corresponding to the Tick value from a time signature change event.
    /// </summary>
    public class BarIndexCalculator
    {
        private int TicksPerBeat { get; }
        private int BarTick => TicksPerBeat * 4;
        private IReadOnlyCollection<TimeSignatureItem> ReversedTimeSignatures { get; }

        /// <summary>
        /// Gets a collection of valid time signature change events sorted by time.
        /// </summary>
        public IEnumerable<TimeSignatureItem> TimeSignatures => ReversedTimeSignatures.Reverse();

        /// <summary>
        /// Initialize an instance of <see cref="BarIndexCalculator"/> from TicksPerBeat and time signature change events.
        /// </summary>
        /// <param name="ticksPerBeat">TicksPerBeat of music score</param>
        /// <param name="sigs">List of <see cref="TimeSignatureChangeEvent"/> representing time signature change events</param>
        public BarIndexCalculator(int ticksPerBeat, IEnumerable<TimeSignatureChangeEvent> sigs)
        {
            TicksPerBeat = ticksPerBeat;
            var ordered = sigs.OrderBy(p => p.Tick).ToList();
            var dic = new SortedDictionary<int, TimeSignatureItem>();
            int pos = 0;
            int barIndex = 0;

            for (int i = 0; i < ordered.Count; i++)
            {
                // Events not placed at the beginning of the measure
                if (pos != ordered[i].Tick) throw new InvalidTimeSignatureException($"TimeSignatureChangeEvent does not align at the head of bars (Tick: {ordered[i].Tick}).", ordered[i].Tick);
                var item = new TimeSignatureItem(barIndex, ordered[i]);

                // Add in reverse time order
                if (dic.ContainsKey(-pos)) throw new InvalidTimeSignatureException($"TimeSignatureChangeEvents duplicated (Tick: {ordered[i].Tick}).", ordered[i].Tick);
                else dic.Add(-pos, item);

                if (i < ordered.Count - 1)
                {
                    int barLength = BarTick * ordered[i].Numerator / ordered[i].Denominator;
                    int duration = ordered[i + 1].Tick - pos;
                    pos += duration / barLength * barLength;
                    barIndex += duration / barLength;
                }
            }

            ReversedTimeSignatures = dic.Values.ToList();
        }

        /// <summary>
        /// Get the bar position corresponding to the specified Tick.
        /// </summary>
        /// <param name="tick">Tick to get measure position</param>
        /// <returns>Represents the bar position corresponding to the Tick<see cref="BarPosition"/></returns>
        public BarPosition GetBarPositionFromTick(int tick)
        {
            foreach (var item in ReversedTimeSignatures)
            {
                if (tick < item.StartTick) continue;
                var sig = item.TimeSignature;
                int barLength = BarTick * sig.Numerator / sig.Denominator;
                int ticksFromSignature = tick - item.StartTick;
                int barsCount = ticksFromSignature / barLength;
                int barIndex = item.StartBarIndex + barsCount;
                int tickOffset = ticksFromSignature - barsCount * barLength;
                return new BarPosition(barIndex, tickOffset);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Get the time signature corresponding to the specified measure.
        /// </summary>
        /// <param name="barIndex">The bar position for which the time signature is to be found. This parameter is 0-based. </param>
        /// <returns>Represents the time signature corresponding to the bar position <see cref="TimeSignatureChangeEvent"/></returns>
        public TimeSignatureChangeEvent GetTimeSignatureFromBarIndex(int barIndex)
        {
            foreach (var item in ReversedTimeSignatures)
            {
                if (barIndex < item.StartBarIndex) continue;
                return item.TimeSignature;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Represents the measure position corresponding to the Tick.
        /// </summary>
        public class BarPosition
        {
            /// <summary>
            /// Get the measure index. This field is 0-based.
            /// </summary>
            public int BarIndex { get; }

            /// <summary>
            /// Represents the Tick offset in measures.
            /// </summary>
            public int TickOffset { get; }

            public BarPosition(int barIndex, int tickOffset)
            {
                BarIndex = barIndex;
                TickOffset = tickOffset;
            }
        }

        /// <summary>
        /// A class that represents the tick position and bar position corresponding to a time signature change event.
        /// </summary>
        public class TimeSignatureItem
        {
            /// <summary>
            /// Get the Tick position corresponding to the time signature change event.
            /// </summary>
            public int StartTick => TimeSignature.Tick;

            /// <summary>
            /// Get the bar position corresponding to the time signature change event. This field is 0-based.
            /// </summary>
            public int StartBarIndex { get; }

            /// <summary>
            /// Get the time signature change event associated with this <see cref="TimeSignatureItem"/>.
            /// </summary>
            public TimeSignatureChangeEvent TimeSignature { get; }

            public TimeSignatureItem(int startBarIndex, TimeSignatureChangeEvent timeSignature)
            {
                StartBarIndex = startBarIndex;
                TimeSignature = timeSignature;
            }
        }
    }
}
