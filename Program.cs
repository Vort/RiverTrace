using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace RiverTrace
{
    class Program
    {
        private Cie1976Comparison cie;
        private int sampleWidth;
        private int sampleLength;
        private double maxDifference;
        private TileMap tileMap;

        Program()
        {
            int zoom = 15;

            var result = GetTrace(
                64.9035622, 52.2209184,
                64.9033165, 52.2211652,
                zoom);

            WriteOsm(result, zoom);
        }

        void WriteOsm(List<Vector> result, int zoom)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
            Console.WriteLine("<osm version=\"0.6\">");

            double lat;
            double lon;

            for (int i = 0; i < result.Count; i++)
            {
                int nodeId = -i - 2;
                Projection.PixToDeg(result[i].X, result[i].Y, zoom, out lat, out lon);
                Console.WriteLine(
                    "  <node id='" + nodeId +
                    "' lat='" + lat +
                    "' lon='" + lon +
                    "' version='1' />");
            }
            Console.WriteLine("  <way id='-1' version='1'>");
            for (int i = 0; i < result.Count; i++)
            {
                int nodeId = -i - 2;
                Console.WriteLine("    <nd ref='" + nodeId + "' />");
            }
            Console.WriteLine("    <tag k='source:tracer' v='RiverTrace' />");
            Console.WriteLine("    <tag k='source:zoomlevel' v='" + zoom + "' />");
            Console.WriteLine("    <tag k='waterway' v='river' />");
            Console.WriteLine("  </way>");
            Console.WriteLine("</osm>");
        }

        double GetColorDifference(Color c1, Color c2)
        {
            Rgb rgb1 = new Rgb { R = c1.R, G = c1.G, B = c1.B };
            Rgb rgb2 = new Rgb { R = c2.R, G = c2.G, B = c2.B };
            return rgb1.Compare(rgb2, cie);
        }

        void CalcSampleDimensions(Vector p, Vector direction)
        {
            double shoreContrast = 10.0;

            int pickCount = 5;
            Vector pickPoint1 = p;
            double riverHalfWidth1 = 0.0;
            double riverHalfWidth2 = 0.0;
            Vector dir1 = direction.Rotated(-90);
            Vector dir2 = direction.Rotated(90);
            for (int i = 0; i < pickCount; i++)
            {
                Vector pickPoint2 = pickPoint1;
                Color refColor = tileMap.GetPixel(pickPoint1.X, pickPoint1.Y);
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 50; k++)
                    {
                        if (j == 0)
                            riverHalfWidth1++;
                        else
                            riverHalfWidth2++;
                        if (j == 0)
                            pickPoint2 += dir1;
                        else
                            pickPoint2 += dir2;
                        double diff = GetColorDifference(refColor,
                            tileMap.GetPixel(pickPoint2.X, pickPoint2.Y));
                        if (diff > shoreContrast)
                            break;
                    }
                }
                pickPoint1 += direction;
            }
            riverHalfWidth1 /= pickCount;
            riverHalfWidth2 /= pickCount;
            double riverWidth = riverHalfWidth1 + riverHalfWidth2;
            sampleWidth = (int)(riverWidth * 1.5);
            sampleLength = sampleWidth / 2;
        }

        SimpleBitmap GetSample(Vector origin, Vector direction)
        {
            SimpleBitmap sample = new SimpleBitmap(sampleWidth, sampleLength);

            Vector dv = new Vector(direction.Y, -direction.X);
            for (int i = 0; i < sampleWidth; i++)
                for (int j = 0; j < sampleLength; j++)
                {
                    int xs = i - sampleWidth / 2;
                    int ys = j;
                    double x = xs * dv.X - ys * dv.Y + origin.X;
                    double y = xs * dv.Y + ys * dv.X + origin.Y;
                    sample.SetPixel(i, j, tileMap.GetPixel(x, y));
                }

            return sample;
        }

        double GetSampleDifference(SimpleBitmap s1, SimpleBitmap s2)
        {
            double totalDelta = 0.0;
            for (int i = 0; i < sampleWidth; i++)
                for (int j = 0; j < sampleLength; j++)
                {
                    Color c1 = s1.GetPixel(i, j);
                    Color c2 = s2.GetPixel(i, j);
                    double pixelDelta = GetColorDifference(c1, c2);
                    totalDelta += pixelDelta;
                }
            return totalDelta / (sampleWidth * sampleLength);
        }

        void GetBestAngle(Vector lastPoint, Vector lastVector, SimpleBitmap avgSample,
            double minAngle, double maxAngle, double step, out SimpleBitmap bestSample,
            out double bestDiff, out Vector bestVector, out double bestAngle)
        {
            SimpleBitmap localBestSample = null;
            double localBestDiff = double.MaxValue;
            Vector localBestVector = null;
            double localBestAngle = 0.0;
            var lockObj = new object();

            int stepCount = (int)Math.Round((maxAngle - minAngle) / step) + 1;
            Parallel.For(0, stepCount, i =>
            {
                double angle = minAngle + i * step;
                Vector rv = lastVector.Rotated(angle);
                SimpleBitmap candidateSample = GetSample(lastPoint, rv);
                double diff = GetSampleDifference(avgSample, candidateSample);
                lock (lockObj)
                {
                    localBestDiff = Math.Min(localBestDiff, diff);
                    if (diff == localBestDiff)
                    {
                        localBestSample = candidateSample;
                        localBestVector = rv;
                        localBestAngle = angle;
                    }
                }
            });
            bestVector = localBestVector;
            bestSample = localBestSample;
            bestDiff = localBestDiff;
            bestAngle = localBestAngle;
        }

        SimpleBitmap GetAvgSample(SimpleBitmap s1, SimpleBitmap s2)
        {
            SimpleBitmap sample = new SimpleBitmap(sampleWidth, sampleLength);
            for (int i = 0; i < sampleWidth; i++)
                for (int j = 0; j < sampleLength; j++)
                {
                    Color c1 = s1.GetPixel(i, j);
                    Color c2 = s2.GetPixel(i, j);
                    sample.SetPixel(i, j,
                        (byte)((c1.R + c2.R) / 2),
                        (byte)((c1.G + c2.G) / 2),
                        (byte)((c1.B + c2.B) / 2));
                }
            return sample;
        }

        List<Vector> GetTrace(double lat1, double lon1, double lat2, double lon2, int zoom)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            cie = new Cie1976Comparison();
            tileMap = new TileMap(zoom);

            var result = new List<Vector>();

            Vector p1 = new Vector();
            Vector p2 = new Vector();
            Projection.DegToPix(lat1, lon1, zoom, out p1.X, out p1.Y);
            Projection.DegToPix(lat2, lon2, zoom, out p2.X, out p2.Y);

            result.Add(p1);
            Vector lastDirection = p2 - p1;
            lastDirection.Normalize();

            CalcSampleDimensions(p1, lastDirection);

            maxDifference = 28.0;

            SimpleBitmap firstSample = GetSample(p1, lastDirection);
            SimpleBitmap avgSample = firstSample;
            Vector lastPoint = p1 + lastDirection * sampleLength;
            result.Add(lastPoint);

            double totalDiff = 0.0;

            for (int i = 0; i < 550; i++)
            {
                SimpleBitmap bestSample;
                double bestDiff;
                Vector bestVector;
                double bestAngle;
                GetBestAngle(lastPoint, lastDirection, avgSample, -50.0, 50.0, 5.0,
                    out bestSample, out bestDiff, out bestVector, out bestAngle);
                GetBestAngle(lastPoint, lastDirection, avgSample, bestAngle - 4.0, bestAngle + 4.0, 1.0,
                    out bestSample, out bestDiff, out bestVector, out bestAngle);

                if (bestDiff > maxDifference)
                    break;

                totalDiff += bestDiff;

                avgSample = GetAvgSample(firstSample, bestSample);

                lastDirection = bestVector;
                lastPoint = lastPoint + lastDirection * sampleLength;
                result.Add(lastPoint);
            }
            sw.Stop();

            /*
            Console.WriteLine("Total diff = " + totalDiff);
            Console.WriteLine("Elapsed = " + sw.Elapsed.TotalSeconds);
            */

            result.Reverse();
            return result;
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
