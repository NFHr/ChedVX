using ChedVX.Core.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChedVX.Core.Constants;

namespace ChedVX.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class BTNote : NoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private EffectBase effect;

        /// <summary>
        /// Hit Fx of the note.
        /// </summary>
        public EffectBase Effect
        {
            get => effect;
            set => effect = value;
        }


        /// <summary>
        /// Track ID of the note.
        /// </summary>
        public override Tracks TrackID
        {
            get
            {
                return LaneIndex switch
                {
                    0 => Tracks.A,
                    1 => Tracks.B,
                    2 => Tracks.C,
                    3 => Tracks.D,
                    _ => throw new ArgumentOutOfRangeException("Track", "No matchable Track ID for this lane index"),
                };
            }
        }

        public override int LaneIndex
        {
            get => laneIndex;

            set
            {
                if (value < 0 || value > 4) throw new ArgumentOutOfRangeException("value", "value must be non-negative.");
                LaneIndex = value;
            }
        }

        public bool HasEffect => effect is EffectBase;

    }
}
