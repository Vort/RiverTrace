using System;

namespace RiverTrace
{
    class Projection
    {
        private static int tileSize;
        private static double initialResolution;
        private static double originShift;

        static Projection()
        {
            tileSize = 256;
            initialResolution = 2 * Math.PI * 6378137 / tileSize;
            originShift = 2 * Math.PI * 6378137 / 2.0;
        }

        public static void DegToPix(double lat, double lon,
            int zoom, out double x, out double y)
        {
            double mx = lon * originShift / 180.0;
            double my = Math.Log(Math.Tan((90 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
            my = my * originShift / 180.0;

            double res = initialResolution / (1 << zoom);
            x = (mx + originShift) / res;
            y = (my + originShift) / res;
        }

        public static void PixToTile(int x, int y, int zoom,
            out int tileIndexX, out int tileIndexY,
            out int tileOffsetX, out int tileOffsetY)
        {
            tileIndexX = x / tileSize;
            tileIndexY = y / tileSize;
            tileOffsetX = x % tileSize;
            tileOffsetY = tileSize - (y % tileSize) - 1;
            tileIndexY = (1 << zoom) - 1 - tileIndexY;
        }

        public static void PixToDeg(double x, double y,
            int zoom, out double lat, out double lon)
        {
            double res = initialResolution / (1 << zoom);
            double mx = x * res - originShift;
            double my = y * res - originShift;
            lon = (mx / originShift) * 180.0;
            lat = (my / originShift) * 180.0;

            lat = 180 / Math.PI * (2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);
        }
    }
}
