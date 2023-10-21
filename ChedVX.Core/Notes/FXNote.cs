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
        /// Lane index of the note.
        /// </summary>
        public override int LaneIndex
        {
            get => laneIndex;
            set
            {
                laneIndex = value switch
                {
                    1 => 0,
                    2 => 0,
                    3 => 1,
                    4 => 1,
                    _ => 2
                };
            }
        }
    }
}
