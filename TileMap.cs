using System;
using System.Collections.Generic;
using System.IO;

namespace RiverTrace
{
    class TileMap
    {
        public readonly int Zoom;

        private Dictionary<long, SimpleBitmap> tiles;

        public TileMap(int zoom)
        {
            Zoom = zoom;
            tiles = new Dictionary<long, SimpleBitmap>();
        }

        private static string GetTileFileName(int tileIndexX, int tileIndexY, int zoom)
        {
            return Path.Combine("cache", tileIndexX + "_" + tileIndexY + "_" + zoom + ".jpeg");
        }

        public Color GetPixel(double x, double y)
        {
            double flx = Math.Floor(x);
            double fly = Math.Floor(y);
            double frx = x - flx;
            double fry = y - fly;
            int flxi = (int)flx;
            int flyi = (int)fly;

            Color c11 = GetPixel(flxi - 1, flyi - 1);
            Color c12 = GetPixel(flxi - 1, flyi);
            Color c21 = GetPixel(flxi, flyi - 1);
            Color c22 = GetPixel(flxi, flyi);

            return Color.Lerp(
                Color.Lerp(c11, c21, frx),
                Color.Lerp(c12, c22, frx), fry);
        }

        public Color GetPixel(int x, int y)
        {
            int tileIndexX;
            int tileIndexY;
            int tileOffsetX;
            int tileOffsetY;
            Projection.PixToTile(x, y, Zoom,
                out tileIndexX, out tileIndexY,
                out tileOffsetX, out tileOffsetY);

            long tileIndex = ((long)tileIndexY << 32) + tileIndexX;

            SimpleBitmap tile;
            lock (tiles)
            {
                if (!tiles.TryGetValue(tileIndex, out tile))
                {
                    byte[] data;

                    string fileName = GetTileFileName(tileIndexX, tileIndexY, Zoom);
                    if (File.Exists(fileName))
                        data = File.ReadAllBytes(fileName);
                    else
                    {
                        data = Bing.GetTile(tileIndexX, tileIndexY, Zoom);
                        Directory.CreateDirectory("cache");
                        File.WriteAllBytes(fileName, data);
                    }

                    tile = new SimpleBitmap(data);
                    tiles[tileIndex] = tile;
                }
            }

            return tile.GetPixel(tileOffsetX, tileOffsetY);
        }
    }
}
