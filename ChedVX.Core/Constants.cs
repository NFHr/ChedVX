using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Core
{
    public static class Constants
    {
        public static int LanesCount = 4;

        public enum Easing
        {
            Linear,
            SineIn,
            SineOut,
            Beizier
        };

        public enum Tracks
        {
            LaserL = 1,
            FxL = 2,
            A = 3,
            B = 4,
            C = 5,
            D = 6,
            FxR = 7,
            LaserR = 8,
        };
    }
}
