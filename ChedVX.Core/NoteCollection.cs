using ChedVX.Core.Notes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    /// <summary>
    /// A class that represents a collection that stores Notes.
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class NoteCollection
    {
        [Newtonsoft.Json.JsonProperty]
        private List<LaserNote> lasers;
        [Newtonsoft.Json.JsonProperty]
        private List<FXNote> fxs;
        [Newtonsoft.Json.JsonProperty]
        private List<BTNote> bts;

        public List<LaserNote> Lasers
        {
            get => lasers;
            set => lasers = value;
        }

        public List<FXNote> FXs
        {
            get => fxs;
            set => fxs = value;
        }

        public List<BTNote> BTs
        {
            get => bts;
            set => bts = value;
        }


        public NoteCollection()
        {
            Lasers = new List<LaserNote>();
            FXs = new List<FXNote>();
            BTs = new List<BTNote>();
        }

        public NoteCollection(NoteCollection collection)
        {
            Lasers = collection.Lasers.ToList();
            FXs = collection.FXs.ToList();
            BTs = collection.BTs.ToList();

        }


        public IEnumerable<NoteBase> GetChips()
        {
            return FXs.Cast<NoteBase>().Concat(BTs).Where(note => note.IsChip()); ;
        }


        public IEnumerable<NoteBase> GetNotes()
        {
            return FXs.Cast<NoteBase>().Concat(BTs);
        }

        public void UpdateTicksPerBeat(double factor)
        {
            foreach (var note in GetNotes())
            {
                note.StartTick = (int)(note.StartTick * factor);
                note.Duration = (int)(note.Duration * factor);
            }
        }
    }
}
