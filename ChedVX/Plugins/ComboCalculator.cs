using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

using ChedVX.Core;
using ChedVX.Core.Notes;
using ChedVX.Localization;

namespace ChedVX.Plugins
{
    public class ComboCalculator : IScorePlugin
    {
        public string DisplayName => PluginStrings.ComboCalculator;

        public void Run(IScorePluginArgs args)
        {
            var score = args.GetCurrentScore();
            var combo = CalculateCombo(score);
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("Total Combo: {0}", combo.Total));
            sb.AppendLine(string.Format("CHIP: {0}", combo.Chip));
            sb.AppendLine(string.Format("LONG: {0}", combo.Long));
            sb.AppendLine(string.Format("VOL: {0}", combo.Vol));

            MessageBox.Show(sb.ToString(), DisplayName);
        }

        protected ComboDetails CalculateCombo(Score score)
        {
            // use "Hold" since "Long" is conflicted with data type "long".
            var Holds = score.Notes.FXs.Cast<NoteBase>().Concat(score.Notes.BTs).Where(p => !p.IsChip);

            var combo = new ComboDetails();
            combo.Chip += new int[]
            {
                score.Notes.BTs.Select(p => p.IsChip).Count(),
                score.Notes.FXs.Select(p => p.IsChip).Count(),
            }.Sum();


            int barTick = 4 * score.TicksPerBeat;
            var bpmEvents = score.Events.BpmChangeEvents.OrderBy(p => p.Tick).ToList();

            // double getHeadBpmAt(int tick) => (bpmEvents.LastOrDefault(p => p.Tick <= tick) ?? bpmEvents[0]).Bpm;
            // double getTailBpmAt(int tick) => (bpmEvents.LastOrDefault(p => p.Tick < tick) ?? bpmEvents[0]).Bpm;            
            int comboDivider(double bpm) => bpm < 255 ? 16 : 8;

            // Find the offset from startTick that counts as a combo
            List<int> calcComboTicks(int startTick, int duration)
            {
                var tickList = new List<int>();
                int head = 0;
                int bpmIndex = 0;

                while (head < duration)
                {
                    while (bpmIndex + 1 < bpmEvents.Count && startTick + head >= bpmEvents[bpmIndex + 1].Tick) bpmIndex++;
                    int interval = barTick / comboDivider(bpmEvents[bpmIndex].Bpm);
                    head += interval;
                    tickList.Add(head);
                }
                return tickList;
            }

            foreach (var hold in Holds)        
            {
                var tickList = new HashSet<int>(calcComboTicks(hold.StartTick, hold.Duration));
                combo.Long += tickList.Count;
            }

            foreach (var laser in score.Notes.Lasers.Where(p => !p.IsChip))
            {
                var tickList = new HashSet<int>(calcComboTicks(laser.StartTick, laser.Duration));
                combo.Vol += tickList.Count;
            }

            combo.Vol += score.Notes.Lasers.Where(p => p.IsChip).Count();

            return combo;
        }

        public struct ComboDetails
        {
            public int Total => Chip + Long + Vol;
            public int Chip { get; set; }
            public int Long { get; set; }
            public int Vol { get; set; }
        }
    }
}
