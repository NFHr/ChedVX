using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static ChedVX.Core.Constants;

namespace ChedVX.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class LaserNote : NoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private float startPosition;
        [Newtonsoft.Json.JsonProperty]
        private float endPosition;
        [Newtonsoft.Json.JsonProperty]
        private Easing lineEasing;
        [Newtonsoft.Json.JsonProperty]
        private int scale;


        /// <summary>
        /// Set the start position of the laser note (Ranged [0,1], above 1 for april fool charts).
        /// </summary>
        public float StartPosition
        {
            get => startPosition;
            set
            {
                CheckPosition(value);
                startPosition = value;
            }
        }

        /// <summary>
        /// Set the End position of the laser note (Ranged [0,1], above 1 for april fool charts).
        /// </summary>
        public float EndPosition
        {
            get => endPosition;
            set
            {
                CheckPosition(value);
                endPosition = value;
            }
        }


        /// <summary>
        /// Set the scale of the laser note (1x or 2x).
        /// </summary>
        public int Scale
        {
            get => scale;
            set => scale = value;
        }

        /// <summary>
        /// Set the Shape of the laser note.
        /// </summary>
        public Easing LineEasing
        {
            get => lineEasing;
            set => lineEasing = value;
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

        public void SetPosition(int startPosition, int endPosition)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        public void SwitchColor()
        {
            LaneIndex = LaneIndex == 0 ? 1 : 0;
        }

        public void Flip()
        {
            float tmp = EndPosition;
            StartPosition = EndPosition;
            EndPosition = tmp;
        }

        public void Mirror()
        {
            StartPosition = 1 - StartPosition;
            EndPosition = 1 - EndPosition;
            SwitchColor();
        }

        protected void CheckPosition(float position)
        {
            if (0 > position)
                throw new ArgumentOutOfRangeException("Invalid position.");
        }
    }
}
