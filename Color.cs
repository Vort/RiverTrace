using System;

namespace RiverTrace
{
    struct Lab
    {
        public double L;
        public double A;
        public double B;
    }

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
            double r = R * (1.0 / 255.0);
            double g = G * (1.0 / 255.0);
            double b = B * (1.0 / 255.0);

            r = (r > 0.04045) ? Math.Exp(Math.Log((r + 0.055) * (1.0 / 1.055)) * 2.4) : r * (1.0 / 12.92);
            g = (g > 0.04045) ? Math.Exp(Math.Log((g + 0.055) * (1.0 / 1.055)) * 2.4) : g * (1.0 / 12.92);
            b = (b > 0.04045) ? Math.Exp(Math.Log((b + 0.055) * (1.0 / 1.055)) * 2.4) : b * (1.0 / 12.92);

            double x = (r * 0.4124 + g * 0.3576 + b * 0.1805) * (1.0 / 0.95047);
            double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            double z = (r * 0.0193 + g * 0.1192 + b * 0.9505) * (1.0 / 1.08883);

            x = (x > 0.008856) ? Math.Exp(Math.Log(x) * (1.0 / 3)) : (x * 7.787) + 16.0 / 116;
            y = (y > 0.008856) ? Math.Exp(Math.Log(y) * (1.0 / 3)) : (y * 7.787) + 16.0 / 116;
            z = (z > 0.008856) ? Math.Exp(Math.Log(z) * (1.0 / 3)) : (z * 7.787) + 16.0 / 116;

            return new Lab
            {
                L = (116.0 * y) - 16.0,
                A = 500.0 * (x - y),
                B = 200.0 * (y - z)
            };
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
