using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChedVX.Core.Constants;

namespace ChedVX.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class FXNote : BTNote
    {
        /// <summary>
        /// Track ID of the note.
        /// </summary>
        public override Tracks TrackID
        {
            get
            {
                return LaneIndex switch
                {
                    0 => Tracks.FxL,
                    1 => Tracks.FxR,
                    _ => throw new ArgumentOutOfRangeException("Track", "No matchable Track ID for this lane index"),
                };
            }
        }

        /// <summary>
        /// Lane index of the note.
        /// </summary>
        public override int LaneIndex
        {
            get => laneIndex;
            set
            {
                laneIndex = value switch
                {
                    0 => 0,
                    1 => 0,
                    2 => 1,
                    3 => 1,
                    _ => throw new ArgumentOutOfRangeException("LaneIndex", "Index of lane out of range."),
                };
            }
        }
    }
}
