using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RiverTrace
{
    class ColorLerp
    {
        public static Color Lerp(Color c1, Color c2, double minV, double maxV, double V)
        {
            return Lerp(c1, c2, (V - minV) / (maxV - minV));
        }

        public static Color Lerp(Color c1, Color c2, double x)
        {
            double omx = 1 - x;
            return new Color(
                (byte)(c1.R * omx + c2.R * x),
                (byte)(c1.G * omx + c2.G * x),
                (byte)(c1.B * omx + c2.B * x));
        }
    }

    class SimpleBitmap
    {
        public byte[] Data;
        public readonly int Width;
        public readonly int Height;

        public SimpleBitmap(byte[] fileData)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(fileData);
            bitmapImage.EndInit();
            Width = bitmapImage.PixelWidth;
            Height = bitmapImage.PixelHeight;
            Data = new byte[Width * Height * 4];
            bitmapImage.CopyPixels(Data, Width * 4, 0);
        }

        public SimpleBitmap(int width, int height)
        {
            Data = new byte[width * height * 4];
            Width = width;
            Height = height;
        }

        public void WriteTo(string fileName)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(GetBitmap()));
            encoder.Save(File.Create(fileName));
        }

        public BitmapSource GetBitmap()
        {
            return BitmapSource.Create(Width, Height,
                96, 96, PixelFormats.Bgr32, null, Data, Width * 4);
        }

        public Color GetPixel(int x, int y)
        {
            int offset = (Width * y + x) * 4;
            return new Color(Data[offset + 2], Data[offset + 1], Data[offset]);
        }

        public void SetPixel(int x, int y, Color c)
        {
            int offset = (Width * y + x) * 4;
            Data[offset] = c.R;
            Data[offset + 1] = c.G;
            Data[offset + 2] = c.B;
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            int offset = (Width * y + x) * 4;
            Data[offset] = r;
            Data[offset + 1] = g;
            Data[offset + 2] = b;
        }

        public void CopyTo(SimpleBitmap dest, int destY)
        {
            Array.Copy(Data, 0, dest.Data, destY * Width * 4, Width * Height * 4);
        }
    }
}
