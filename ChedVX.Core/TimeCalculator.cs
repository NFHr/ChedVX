using ChedVX.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    /// <summary>
    /// This class calculates the time corresponding to the Tick value from the BPM change event.
    /// </summary>
    public class TimeCalculator
    {
        protected int TicksPerBeat { get; }
        // Sorted by time
        protected List<(int Tick, double Bpm, double Time)> BpmDefinitions { get; } = new List<(int Tick, double Bpm, double Time)>();

        public TimeCalculator(int ticksPerBeat, IEnumerable<BpmChangeEvent> bpms)
        {
            TicksPerBeat = ticksPerBeat;
            double time = 0;
            var ordered = bpms.OrderBy(p => p.Tick).ToList();
            if (ordered[0].Tick != 0) throw new ArgumentException("Initial BpmChangeEvent was not found.", "bpms");
            BpmDefinitions.Add((0, ordered[0].Bpm, 0));
            for (int i = 1; i < ordered.Count; i++)
            {
                time += GetDuration(ordered[i - 1].Bpm, ordered[i].Tick - ordered[i - 1].Tick);
                BpmDefinitions.Add((ordered[i].Tick, ordered[i].Bpm, time));
            }
        }

        public double GetTimeFromTick(int tick)
        {
            for (int i = BpmDefinitions.Count - 1; 0 <= i; i--)
            {
                if (tick < BpmDefinitions[i].Tick) continue;
                return BpmDefinitions[i].Time + GetDuration(BpmDefinitions[i].Bpm, tick - BpmDefinitions[i].Tick);
            }
            // Negative time goes back at the initial BPM
            return GetDuration(BpmDefinitions[0].Bpm, tick);
        }

        public int GetTickFromTime(double time)
        {
            for (int i = BpmDefinitions.Count - 1; 0 <= i; i--)
            {
                if (time < BpmDefinitions[i].Time) continue;
                return BpmDefinitions[i].Tick + GetDurationInTick(BpmDefinitions[i].Bpm, time - BpmDefinitions[i].Time);
            }
            // Negative time goes back at the initial BPM
            return GetDurationInTick(BpmDefinitions[0].Bpm, time);
        }

        // => durationTick * (60 / bpm) / TicksPerBeat;
        protected double GetDuration(double bpm, int durationTick) => durationTick * 60 / bpm / TicksPerBeat;

        // => TicksPerBeat * duration * (bpm / 60)
        protected int GetDurationInTick(double bpm, double duration) => (int)(TicksPerBeat * duration * bpm / 60);
    }
}
