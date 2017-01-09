using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RiverTrace
{
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
            Data[offset + 2] = c.R;
            Data[offset + 1] = c.G;
            Data[offset] = c.B;
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b)
        {
            int offset = (Width * y + x) * 4;
            Data[offset + 2] = r;
            Data[offset + 1] = g;
            Data[offset] = b;
        }

        public void CopyTo(SimpleBitmap dest, int destY)
        {
            Array.Copy(Data, 0, dest.Data, destY * Width * 4, Width * Height * 4);
        }
    }
}
