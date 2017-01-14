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
            var encoder = new TiffBitmapEncoder();
            encoder.Compression = TiffCompressOption.Rle;
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

        public void SetPixel(int x, int y, byte intensity)
        {
            int offset = (Width * y + x) * 4;
            Data[offset + 2] = intensity;
            Data[offset + 1] = intensity;
            Data[offset] = intensity;
        }

        public void CopyTo(SimpleBitmap dest, int destX, int destY)
        {
            for (int y = 0; y < Height; y++)
            {
                Array.Copy(Data, y * Width * 4,
                    dest.Data, ((y + destY) * dest.Width + destX) * 4, Width * 4);
            }
        }
    }
}
