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

        public static Color BiLerp(Color c11, Color c12,
            Color c21, Color c22, double fx, double fy)
        {
            double ifx = 1.0 - fx;
            double ify = 1.0 - fy;
            byte r = (byte)(
                (c11.R * ifx + c21.R * fx) * ify +
                (c12.R * ifx + c22.R * fx) * fy + 0.5);
            byte g = (byte)(
                (c11.G * ifx + c21.G * fx) * ify +
                (c12.G * ifx + c22.G * fx) * fy + 0.5);
            byte b = (byte)(
                (c11.B * ifx + c21.B * fx) * ify +
                (c12.B * ifx + c22.B * fx) * fy + 0.5);
            return new Color(r, g, b);
        }

        public byte R;
        public byte G;
        public byte B;
    }
}
