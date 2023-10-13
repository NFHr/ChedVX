using ChedVX.Core;
using ChedVX.Core.Events;
using ChedVX.Core.Notes;
using ChedVX.Drawing;
using ChedVX.UI.Operations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChedVX.UI
{


    public enum EditMode
    {
        Select,
        Edit,
        Erase,
    }

    [Flags]
    public enum NoteType
    {
        Cursor = 0,
        BTChip = 1 << 1,
        BTLong = 1 << 2,
        FXChip = 1 << 3,
        FXLong = 1 << 4,
        LaserLeft = 1 << 5,
        LaserRight = 1 << 6,
    }

    [Serializable]
    public class SelectionData
    {
        private string serializedText = null;

        [NonSerialized]
        private InnerData Data;

        public int StartTick
        {
            get
            {
                CheckRestored();
                return Data.StartTick;
            }
        }

        public Core.NoteCollection SelectedNotes
        {
            get
            {
                CheckRestored();
                return Data.SelectedNotes;
            }
        }

        public bool IsEmpty
        {
            get
            {
                CheckRestored();
                return SelectedNotes.GetShortNotes().Count() == 0 && SelectedNotes.Holds.Count == 0 && SelectedNotes.Slides.Count == 0 && SelectedNotes.Airs.Count == 0 && SelectedNotes.AirActions.Count == 0;
            }
        }

        public int TicksPerBeat
        {
            get
            {
                CheckRestored();
                return Data.TicksPerBeat;
            }
        }

        public SelectionData()
        {
        }

        public SelectionData(int startTick, int ticksPerBeat, NoteCollection notes)
        {
            Data = new InnerData(startTick, ticksPerBeat, notes);
            serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(Data, SerializerSettings);
        }

        protected void CheckRestored()
        {
            if (Data == null) Restore();
        }

        protected void Restore()
        {
            Data = Newtonsoft.Json.JsonConvert.DeserializeObject<InnerData>(serializedText, SerializerSettings);
        }

        protected static Newtonsoft.Json.JsonSerializerSettings SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { IgnoreSerializableAttribute = true }
        };

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        protected class InnerData
        {
            [Newtonsoft.Json.JsonProperty]
            private int startTick;

            [Newtonsoft.Json.JsonProperty]
            private int ticksPerBeat;

            [Newtonsoft.Json.JsonProperty]
            private NoteCollection selectedNotes;

            public int StartTick => startTick;
            public int TicksPerBeat => ticksPerBeat;
            public NoteCollection SelectedNotes => selectedNotes;

            public InnerData(int startTick, int ticksPerBeat, NoteCollection notes)
            {
                this.startTick = startTick;
                this.ticksPerBeat = ticksPerBeat;
                selectedNotes = notes;
            }
        }
    }

    internal static class UIExtensions
}
