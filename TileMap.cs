﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace RiverTrace
{
    class TileMap
    {
        public readonly int Zoom;

        private string cacheDir;
        private Dictionary<long, SimpleBitmap> tiles;
        private ImageSource imageSource;

        public TileMap(int zoom)
        {
            Zoom = zoom;
            tiles = new Dictionary<long, SimpleBitmap>();
            if (Config.Data.imageSourceProtocol == ImageSourceProtocol.bing)
                imageSource = new Bing();
            else if (Config.Data.imageSourceProtocol == ImageSourceProtocol.tms)
                imageSource = new Tms(Config.Data.imageSourceUrl);
            else
                throw new Exception("Protocol is not supported");
            cacheDir = Path.Combine("cache", Config.Data.imageSourceName);
        }

        public Color GetPixel(double x, double y)
        {
            x -= 0.5;
            y -= 0.5;
            double flx = Math.Floor(x);
            double fly = Math.Floor(y);
            double frx = x - flx;
            double fry = y - fly;
            int flxi = (int)flx;
            int flyi = (int)fly;

            Color c11 = GetPixel(flxi, flyi);
            Color c12 = GetPixel(flxi, flyi + 1);
            Color c21 = GetPixel(flxi + 1, flyi);
            Color c22 = GetPixel(flxi + 1, flyi + 1);

            return Color.BiLerp(c11, c12, c21, c22, frx, fry);
        }

        private void Load(int tileIndexX, int tileIndexY)
        {
            long tileIndex = ((long)tileIndexY << 32) + tileIndexX;
            if (!tiles.ContainsKey(tileIndex))
            {
                string foundFileName = null;
                string fileNameBase = tileIndexX + "_" + tileIndexY + "_" + Zoom;
                if (Config.Data.enableCaching)
                {
                    if (Directory.Exists(cacheDir))
                    {
                        string[] fileList = Directory.GetFiles(cacheDir, fileNameBase + ".*");
                        if (fileList.Length != 0)
                            foundFileName = fileList[0];
                    }
                }

                byte[] data;
                if (foundFileName != null)
                    data = File.ReadAllBytes(foundFileName);
                else
                {
                    data = imageSource.GetTile(tileIndexX, tileIndexY, Zoom);
                    if (Config.Data.enableCaching)
                    {
                        string fileExt = "bin";
                        if (data.Take(3).SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }))
                            fileExt = "jpeg";
                        else if (data.Take(8).SequenceEqual(new byte[] {
                            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
                        {
                            fileExt = "png";
                        }
                        Directory.CreateDirectory(cacheDir);
                        File.WriteAllBytes(Path.Combine(
                            cacheDir, fileNameBase + "." + fileExt), data);
                    }
                }
                tiles[tileIndex] = new SimpleBitmap(data);
            }
        }

        public void Load(int minX, int minY, int width, int height)
        {
            int tileIndexX1;
            int tileIndexY1;
            int tileIndexX2;
            int tileIndexY2;
            int trash;
            Projection.PixToTile(minX, minY,
                out tileIndexX1, out tileIndexY1,
                out trash, out trash);
            Projection.PixToTile(minX + width - 1, minY + height - 1,
                out tileIndexX2, out tileIndexY2,
                out trash, out trash);

            int tileCountX = tileIndexX2 - tileIndexX1 + 1;
            int tileCountY = tileIndexY2 - tileIndexY1 + 1;

            for (int i = 0; i < tileCountX; i++)
                for (int j = 0; j < tileCountY; j++)
                    Load(tileIndexX1 + i, tileIndexY1 + j);
        }

        public Color GetPixel(int x, int y)
        {
            int tileIndexX;
            int tileIndexY;
            int tileOffsetX;
            int tileOffsetY;
            Projection.PixToTile(x, y,
                out tileIndexX, out tileIndexY,
                out tileOffsetX, out tileOffsetY);

            long tileIndex = ((long)tileIndexY << 32) + tileIndexX;
            return tiles[tileIndex].GetPixel(tileOffsetX, tileOffsetY);
        }
    }
}
