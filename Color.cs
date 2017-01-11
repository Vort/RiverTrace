using ColorMine.ColorSpaces;
using System;

namespace RiverTrace
{
    struct Color
    {
        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public Lab ToLab()
        {
            return new Rgb { R = R, G = G, B = B }.To<Lab>();
        }

        public static double Difference(Lab c1, Color c2)
        {
            Lab c2lab = c2.ToLab();
            double dl = c1.L - c2lab.L;
            double da = c1.A - c2lab.A;
            double db = c1.B - c2lab.B;
            return Math.Sqrt(dl * dl + da * da + db * db);
        }

        public static Color Lerp(Color c1, Color c2, double x)
        {
            double omx = 1 - x;
            return new Color(
                (byte)(c1.R * omx + c2.R * x),
                (byte)(c1.G * omx + c2.G * x),
                (byte)(c1.B * omx + c2.B * x));
        }

        public byte R;
        public byte G;
        public byte B;
    }
}
