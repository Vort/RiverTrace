using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RiverTrace
{
    class Tracer
    {
        private Cie1976Comparison cie;
        private int sampleWidth;
        private int sampleLength;
        private TileMap tileMap;

        void WriteOsm(List<Vector> result)
        {
            Console.WriteLine("<?xml version='1.0' encoding='UTF-8'?>");
            Console.WriteLine("<osm version='0.6'>");

            double lat;
            double lon;

            for (int i = 0; i < result.Count; i++)
            {
                int nodeId = -i - 2;
                Projection.PixToDeg(result[i].X, result[i].Y, Config.Data.zoom, out lat, out lon);
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
            Console.WriteLine("    <tag k='source:zoomlevel' v='" + Config.Data.zoom + "' />");
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

        void CalcSampleDimensions(Vector startPoint, Vector direction)
        {
            int pickCount = 5;
            Vector pickPoint1 = startPoint;
            double[] riverHalfWidth = new double[2];
            Vector[] sideDirs = new Vector[] { direction.Rotated(-90), direction.Rotated(90) };
            for (int i = 0; i < pickCount; i++)
            {
                Color refColor = tileMap.GetPixel(pickPoint1.X, pickPoint1.Y);
                for (int j = 0; j < 2; j++)
                {
                    Vector pickPoint2 = pickPoint1;
                    for (int k = 0; k < 50; k++)
                    {
                        pickPoint2 += sideDirs[j];
                        Color checkColor = tileMap.GetPixel(pickPoint2.X, pickPoint2.Y);
                        double diff = GetColorDifference(refColor, checkColor);
                        if (diff > Config.Data.shoreContrast)
                            break;
                        riverHalfWidth[j] += 1.0;
                    }
                }
                pickPoint1 += direction;
            }
            riverHalfWidth[0] /= pickCount;
            riverHalfWidth[1] /= pickCount;
            double riverWidth = riverHalfWidth[0] + riverHalfWidth[1] + 1.0;
            sampleWidth = Math.Max((int)Math.Ceiling(riverWidth * Config.Data.sampleWidthScale), 5);
            sampleLength = Math.Max((int)Math.Ceiling(riverWidth * Config.Data.sampleLengthScale), 3);
        }

        SimpleBitmap GetSample(Vector origin, Vector direction)
        {
            SimpleBitmap sample = new SimpleBitmap(sampleWidth, sampleLength);

            Vector dv = direction.Rotated(-90);
            for (int i = 0; i < sampleWidth; i++)
                for (int j = 0; j < sampleLength; j++)
                {
                    int xs = sampleWidth / 2 - i;
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

        public Tracer()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            cie = new Cie1976Comparison();
            tileMap = new TileMap(Config.Data.zoom);

            var way = new List<Vector>();

            Vector p1 = new Vector();
            Vector p2 = new Vector();
            Projection.DegToPix(Config.Data.lat1, Config.Data.lon1, Config.Data.zoom, out p1.X, out p1.Y);
            Projection.DegToPix(Config.Data.lat2, Config.Data.lon2, Config.Data.zoom, out p2.X, out p2.Y);

            way.Add(p1);
            Vector lastDirection = p2 - p1;
            lastDirection.Normalize();

            CalcSampleDimensions(p1, lastDirection);

            List<SimpleBitmap> samples = new List<SimpleBitmap>();
            SimpleBitmap firstSample = GetSample(p1, lastDirection);
            SimpleBitmap avgSample = firstSample;
            Vector lastPoint = p1 + lastDirection * sampleLength;
            way.Add(lastPoint);
            samples.Add(firstSample);

            double totalDiff = 0.0;
            for (int i = 0; i < Config.Data.iterationCount; i++)
            {
                SimpleBitmap bestSample;
                double bestDiff;
                Vector bestVector;
                double bestAngle;
                GetBestAngle(lastPoint, lastDirection, avgSample, -50.0, 50.0, 5.0,
                    out bestSample, out bestDiff, out bestVector, out bestAngle);
                GetBestAngle(lastPoint, lastDirection, avgSample, bestAngle - 4.0, bestAngle + 4.0, 1.0,
                    out bestSample, out bestDiff, out bestVector, out bestAngle);

                if (bestDiff > Config.Data.maxDifference)
                    break;

                totalDiff += bestDiff;

                samples.Add(bestSample);
                avgSample = GetAvgSample(firstSample, bestSample);

                lastDirection = bestVector;
                lastPoint = lastPoint + lastDirection * sampleLength;
                way.Add(lastPoint);
            }
            sw.Stop();

            way.Reverse();
            WriteOsm(way);

            if (Config.Data.debug)
            {
                SimpleBitmap sampleChain = new SimpleBitmap(
                    sampleWidth, sampleLength * samples.Count);
                for (int i = 0; i < samples.Count; i++)
                    samples[i].CopyTo(sampleChain, i * sampleLength);
                sampleChain.WriteTo("sample_chain.png");

                for (int i = 0; i < 50; i++)
                    Console.WriteLine();
                Console.WriteLine("<!--");
                Console.WriteLine("Total diff = " + totalDiff);
                Console.WriteLine("Elapsed = " + sw.Elapsed.TotalSeconds);
                Console.WriteLine("-->");
            }
        }
    }
}
