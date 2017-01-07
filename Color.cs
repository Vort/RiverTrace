using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;

namespace RiverTrace
{
    struct Color
    {
        private static Cie1976Comparison cie;

        static Color()
        {
            cie = new Cie1976Comparison();
        }

        public Color(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public double DifferenceTo(Color c)
        {
            Rgb rgb1 = new Rgb { R = R, G = G, B = B };
            Rgb rgb2 = new Rgb { R = c.R, G = c.G, B = c.B };
            return rgb1.Compare(rgb2, cie);
        }

        public byte R;
        public byte G;
        public byte B;
    }
}
