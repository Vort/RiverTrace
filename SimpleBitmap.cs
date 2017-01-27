using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RiverTrace
{
    class SimpleBitmap
    {
        public byte[] Data;
        public int Width;
        public int Height;

        public SimpleBitmap(byte[] fileData)
        {
            MemoryStream byteStream = new MemoryStream(fileData);
            if (Type.GetType("Mono.Runtime") == null)
                InitWin(byteStream);
            else
                InitMono(byteStream);
        }

        private void InitWin(MemoryStream byteStream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = byteStream;
            bitmapImage.EndInit();
            Width = bitmapImage.PixelWidth;
            Height = bitmapImage.PixelHeight;
            Data = new byte[Width * Height * 4];

            BitmapSource source = bitmapImage;
            if (source.Format != PixelFormats.Bgr32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            source.CopyPixels(Data, Width * 4, 0);
        }

        private void InitMono(MemoryStream byteStream)
        {
            Bitmap bmp = (Bitmap)Image.FromStream(byteStream);
            Width = bmp.Width;
            Height = bmp.Height;
            Data = new byte[Width * Height * 4];
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            Marshal.Copy(bmpData.Scan0, Data, 0, Data.Length);
            bmp.UnlockBits(bmpData);
        }

        public SimpleBitmap(int width, int height)
        {
            Data = new byte[width * height * 4];
            Width = width;
            Height = height;
        }

        public void WriteTo(string fileName)
        {
            if (Type.GetType("Mono.Runtime") == null)
                WriteToWin(fileName);
            else
                WriteToMono(fileName);
        }

        private void WriteToWin(string fileName)
        {
            var encoder = new TiffBitmapEncoder();
            encoder.Compression = TiffCompressOption.Rle;
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(
                Width, Height, 96, 96, PixelFormats.Bgr32, null, Data, Width * 4)));
            encoder.Save(File.Create(fileName));
        }

        private void WriteToMono(string fileName)
        {
            GCHandle pinnedArray = GCHandle.Alloc(Data, GCHandleType.Pinned);
            Bitmap bmp32 = new Bitmap(Width, Height, Width * 4,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                pinnedArray.AddrOfPinnedObject());
            Bitmap bmp24 = new Bitmap(Width, Height,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics gr = Graphics.FromImage(bmp24))
                gr.DrawImage(bmp32, new Rectangle(0, 0, Width, Height));
            bmp24.Save(fileName, ImageFormat.Tiff);
            pinnedArray.Free();
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
