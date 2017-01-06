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

        public static double Distance(Vector p1, Vector p2, int zoom)
        {
            double lat1;
            double lon1;
            double lat2;
            double lon2;
            Projection.PixToDeg(p1.X, p1.Y, zoom, out lat1, out lon1);
            Projection.PixToDeg(p2.X, p2.Y, zoom, out lat2, out lon2);
            return Distance(lat1, lon1, lat2, lon2);
        }

        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double r = 6371000.0;
            double lat1rad = Vector.DegToRad(lat1);
            double lat2rad = Vector.DegToRad(lat2);
            double deltaLatRad = Vector.DegToRad(lat2 - lat1);
            double deltaLonRad = Vector.DegToRad(lon2 - lon1);

            double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1rad) * Math.Cos(lat2rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return r * c;
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
