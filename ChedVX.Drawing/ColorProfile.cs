using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.Drawing
{
    public class GradientColor
    {
        public Color StartColor { get; set; }
        public Color EndColor { get; set; }

        public GradientColor(Color darkColor, Color lightColor)
        {
            StartColor = darkColor;
            EndColor = lightColor;
        }
    }

    public class ColorProfile
    {
        public GradientColor BorderColor { get; set; }

        public GradientColor BTColor { get; set; }
        public GradientColor BTBorderColor { get; set; }

        public GradientColor FXBTColor { get; set; }
        public GradientColor FXBTBorderColor { get; set; }

        public GradientColor ChipColor { get; set; }
        public GradientColor ChipBorderColor { get; set; }

        public GradientColor FXChipColor { get; set; }
        public GradientColor FXChipBorderColor { get; set; }

        public GradientColor BTLongColor { get; set; }
        public GradientColor BTLongBorderColor { get; set; }

        public GradientColor FXBTLongColor { get; set; }
        public GradientColor FXBTLongBorderColor { get; set; }

        public GradientColor FXLongColor { get; set; }
        public GradientColor FXLongBorderColor { get; set; }

        public GradientColor LaserLColor { get; set; }
        public GradientColor LaserLBorderColor { get; set; }

        public GradientColor LaserRColor { get; set; }
        public GradientColor LaserRBorderColor { get; set; }
    }
}
