using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RiverTrace
{
    class Tracer
    {
        private double riverWidthM;
        private double scanRadius;
        private Color waterColor;
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
            Console.WriteLine("    <tag k='width' v='" + Math.Round(riverWidthM).ToString() + "' />");
            Console.WriteLine("    <tag k='waterway' v='river' />");
            Console.WriteLine("  </way>");
            Console.WriteLine("</osm>");
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

                if (i == 0)
                    waterColor = refColor;
                else
                    waterColor = new Color((byte)((waterColor.R + refColor.R) / 2),
                        (byte)((waterColor.G + refColor.G) / 2),
                        (byte)((waterColor.B + refColor.B) / 2));

                for (int j = 0; j < 2; j++)
                {
                    Vector pickPoint2 = pickPoint1;
                    for (int k = 0; k < 50; k++)
                    {
                        pickPoint2 += sideDirs[j];
                        Color checkColor = tileMap.GetPixel(pickPoint2.X, pickPoint2.Y);
                        double diff = refColor.DifferenceTo(checkColor);
                        if (diff > Config.Data.shoreContrast)
                            break;
                        riverHalfWidth[j] += 1.0;
                    }
                }
                pickPoint1 += direction;
            }
            riverHalfWidth[0] /= pickCount;
            riverHalfWidth[1] /= pickCount;
            double riverWidthPx = riverHalfWidth[0] + riverHalfWidth[1] + 1.0;
            scanRadius = riverWidthPx * Config.Data.scanRadiusScale;

            Vector wp1 = startPoint + sideDirs[0] * (riverWidthPx / 2.0);
            Vector wp2 = startPoint + sideDirs[1] * (riverWidthPx / 2.0);
            riverWidthM = Projection.Distance(wp1, wp2, Config.Data.zoom);
        }

        public Tracer()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

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

            int pixelRange = (int)(scanRadius * 2) + 1;

            List<SimpleBitmap> samples = new List<SimpleBitmap>();

            Vector lastPoint = p1;
            for (int z = 0; z < Config.Data.iterationCount; z++)
            {
                SimpleBitmap sb = null;
                if (Config.Data.debug)
                    sb = new SimpleBitmap(pixelRange * 2, pixelRange);
                var angles = new Dictionary<double, double>();
                for (int i = 0; i < pixelRange; i++)
                {
                    for (int j = 0; j < pixelRange; j++)
                    {
                        int x = (int)(lastPoint.X + i - scanRadius);
                        int y = (int)(lastPoint.Y + j - scanRadius);
                        Vector pixelVector = new Vector(x, y) - lastPoint;
                        double pixelVectorLen = pixelVector.Length();

                        if (pixelVector.Length() < 1.0)
                            continue;
                        if (pixelVectorLen > scanRadius)
                            continue;

                        double angle = lastDirection.AngleTo(pixelVector);
                        if (angle < -Config.Data.angleRange)
                            continue;
                        if (angle > Config.Data.angleRange)
                            continue;

                        Color c = tileMap.GetPixel(x, y);
                        if (Config.Data.debug)
                            sb.SetPixel(i, j, c);

                        double invDiff = Math.Max(1.0 -
                            waterColor.DifferenceTo(c) / Config.Data.shoreContrast, 0.0);

                        if (Config.Data.debug)
                        {
                            byte diffColor = (byte)Math.Round(invDiff * 255.0);
                            sb.SetPixel(i + pixelRange, j, new Color(diffColor, diffColor, diffColor));
                        }

                        if (invDiff != 0.0)
                        {
                            if (!angles.ContainsKey(angle))
                                angles[angle] = 0.0;
                            angles[angle] += invDiff;
                        }
                    }
                }

                samples.Add(sb);

                if (angles.Count == 0)
                    break;

                double[] anglesGrid = new double[(int)Math.Round(
                    Config.Data.angleRange * 2 / Config.Data.angleStep + 1)];
                foreach (var kv in angles)
                {
                    anglesGrid[(int)Math.Round((kv.Key +
                        Config.Data.angleRange) / Config.Data.angleStep)] += kv.Value;
                }

                double bestAngle = (anglesGrid.ToList().IndexOf(anglesGrid.Max()) *
                    Config.Data.angleStep - Config.Data.angleRange);
                lastDirection = lastDirection.Rotated(bestAngle);
                lastPoint += lastDirection * scanRadius * Config.Data.advanceRate;
                way.Add(lastPoint);
            }
            sw.Stop();

            way.Reverse();
            WriteOsm(way);

            if (Config.Data.debug)
            {
                SimpleBitmap sampleChain = new SimpleBitmap(
                    samples[0].Width, samples[0].Height * samples.Count);
                for (int i = 0; i < samples.Count; i++)
                    samples[i].CopyTo(sampleChain, i * samples[0].Height);
                sampleChain.WriteTo("sample_chain.png");
                for (int i = 0; i < 50; i++)
                    Console.WriteLine();
                Console.WriteLine("<!--");
                Console.WriteLine("Elapsed = " + sw.Elapsed.TotalSeconds);
                Console.WriteLine("-->");
            }
        }
    }
}
