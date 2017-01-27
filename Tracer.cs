using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RiverTrace
{
    class Tracer
    {
        private double riverWidthPx;
        private double riverWidthM;
        private double scanRadius;
        private Lab waterColor;
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
            if (!string.IsNullOrWhiteSpace(Config.Data.imageSourceName))
                Console.WriteLine("    <tag k='source:position' v='" + Config.Data.imageSourceName + "' />");
            Console.WriteLine("    <tag k='width' v='" + Math.Round(riverWidthM).ToString() + "' />");
            Console.WriteLine("    <tag k='waterway' v='river' />");
            Console.WriteLine("  </way>");
            Console.WriteLine("</osm>");
        }

        void CalcSampleDimensions(Vector startPoint, Vector direction)
        {
            int pickCount = 5;
            int maxHalfWidth = 50;

            Vector[] sideDirs = new Vector[] { direction.Rotated(-90), direction.Rotated(90) };

            Vector p1 = startPoint + sideDirs[0] * maxHalfWidth;
            Vector p2 = startPoint + sideDirs[1] * maxHalfWidth;
            Vector p3 = startPoint + direction * pickCount + sideDirs[0] * maxHalfWidth;
            Vector p4 = startPoint + direction * pickCount + sideDirs[1] * maxHalfWidth;

            int minX = (int)Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X);
            int minY = (int)Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y);
            int maxX = (int)Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X);
            int maxY = (int)Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y);

            tileMap.Load(minX - 4, minY - 4, maxX - minX + 8, maxY - minY + 8);

            int waterColorR = 0;
            int waterColorG = 0;
            int waterColorB = 0;

            Vector pickPoint1 = startPoint;
            for (int i = 0; i < pickCount; i++)
            {
                Color c = tileMap.GetPixel(pickPoint1.X, pickPoint1.Y);
                waterColorR += c.R;
                waterColorG += c.G;
                waterColorB += c.B;
                pickPoint1 += direction;
            }
            waterColor = new Color(
                (byte)(waterColorR / pickCount),
                (byte)(waterColorG / pickCount),
                (byte)(waterColorB / pickCount)).ToLab();

            pickPoint1 = startPoint;
            double[] riverHalfWidth = new double[2];
            for (int i = 0; i < pickCount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Vector pickPoint2 = pickPoint1;
                    for (int k = 0; k < maxHalfWidth; k++)
                    {
                        pickPoint2 += sideDirs[j];
                        Color checkColor = tileMap.GetPixel(pickPoint2.X, pickPoint2.Y);
                        double diff = Color.Difference(waterColor, checkColor);
                        if (diff > Config.Data.shoreContrast)
                            break;
                        riverHalfWidth[j] += 1.0;
                    }
                }
                pickPoint1 += direction;
            }
            riverHalfWidth[0] /= pickCount;
            riverHalfWidth[1] /= pickCount;
            riverWidthPx = riverHalfWidth[0] + riverHalfWidth[1] + 1.0;
            scanRadius = riverWidthPx * Config.Data.scanRadiusScale;

            Vector wp1 = startPoint + sideDirs[0] * (riverWidthPx / 2.0);
            Vector wp2 = startPoint + sideDirs[1] * (riverWidthPx / 2.0);
            riverWidthM = Projection.Distance(wp1, wp2, Config.Data.zoom);
        }

        private double[] Integrate(double[] samples, int sampleCount)
        {
            if (sampleCount == 1)
                return samples;

            int padCount = sampleCount / 2;

            double[] paddedSamples = new double[padCount * 2 + samples.Length];
            Array.Copy(samples, 0, paddedSamples, padCount, samples.Length);

            for (int i = 0; i < padCount; i++)
                paddedSamples[i] = samples[0];
            for (int i = 0; i < padCount; i++)
                paddedSamples[samples.Length + padCount + i] = samples[samples.Length - 1];

            double[] result = new double[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < sampleCount; j++)
                    sum += paddedSamples[i + j];
                result[i] = sum / sampleCount;
            }
            return result;
        }

        public Tracer()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            tileMap = new TileMap(Config.Data.zoom);

            Vector p1 = new Vector();
            Vector p2 = new Vector();
            Projection.DegToPix(Config.Data.lat1, Config.Data.lon1, Config.Data.zoom, out p1.X, out p1.Y);
            Projection.DegToPix(Config.Data.lat2, Config.Data.lon2, Config.Data.zoom, out p2.X, out p2.Y);

            Vector lastDirection = p2 - p1;
            lastDirection.Normalize();

            CalcSampleDimensions(p1, lastDirection);

            int angleSamples = (int)Math.Ceiling(scanRadius * Math.PI *
                Config.Data.resamplingFactor * Config.Data.angleRange / 90.0);
            int radiusSamples = (int)Math.Ceiling(scanRadius * Config.Data.resamplingFactor);

            var way = new List<Vector>();
            way.Add(p1);

            var debugFrames = new List<DebugFrame>();

            Vector lastPoint = p1;
            for (int z = 0; z < Config.Data.iterationCount; z++)
            {
                tileMap.Load(
                    (int)(lastPoint.X - scanRadius) - 4,
                    (int)(lastPoint.Y - scanRadius) - 4,
                    (int)(scanRadius * 2) + 8,
                    (int)(scanRadius * 2) + 8);

                DebugFrame debugFrame = null;
                double[] anglesGrid = new double[angleSamples];
                if (Config.Data.debug)
                {
                    debugFrame = new DebugFrame(scanRadius, angleSamples, radiusSamples);
                    debugFrame.FillSectors(lastPoint, lastDirection, tileMap, waterColor);
                }

                Vector startV = lastDirection.Rotated(-Config.Data.angleRange);
                Parallel.For(0, angleSamples, i =>
                {
                    Vector advV = startV.Rotated(i / (double)angleSamples * Config.Data.angleRange * 2.0);
                    for (int j = 0; j < radiusSamples; j++)
                    {
                        Vector p = lastPoint + advV * (j + 0.5) / Config.Data.resamplingFactor;
                        double diff = Color.Difference(waterColor, tileMap.GetPixel(p.X, p.Y));
                        double normDiff = Math.Max(1.0 - diff / Config.Data.shoreContrast, 0.0);
                        normDiff *= (j + 0.5) / radiusSamples;
                        if (Config.Data.debug)
                            debugFrame.SetPolarTrans(i, j, normDiff);
                        anglesGrid[i] += normDiff;
                    }
                });

                int integrateSamples = (int)(riverWidthPx * Config.Data.noiseReduction);
                if (integrateSamples % 2 == 0)
                    integrateSamples++;
                anglesGrid = Integrate(anglesGrid, integrateSamples);

                double anglesGridMax = anglesGrid.Max();
                if (anglesGridMax == 0.0)
                    break;

                double bestAngle = anglesGrid.ToList().IndexOf(anglesGridMax) /
                     (double)angleSamples * Config.Data.angleRange * 2.0 - Config.Data.angleRange;
                lastDirection = lastDirection.Rotated(bestAngle);
                lastPoint += lastDirection * scanRadius * Config.Data.advanceRate;

                if (!Intersection.Check(way, lastPoint))
                    break;

                if (Config.Data.debug)
                {
                    debugFrame.SetPolarGrid(anglesGrid);
                    debugFrames.Add(debugFrame);
                }

                way.Add(lastPoint);
            }

            if (Config.Data.simplificationStrength > 0.0)
            {
                way = Simplify.DouglasPeuckerReduction(way,
                    riverWidthPx * Config.Data.simplificationStrength);
            }

            way.Reverse();
            sw.Stop();

            WriteOsm(way);

            for (int i = 0; i < 25; i++)
                Console.WriteLine();
            Console.WriteLine("<!--");
            Console.WriteLine("Elapsed = {0:0.000} sec", sw.Elapsed.TotalSeconds);
            Console.WriteLine("-->");

            if (Config.Data.debug)
            {
                SimpleBitmap debugInfo = new SimpleBitmap(
                    debugFrames[0].Width, debugFrames[0].Height * debugFrames.Count);
                for (int i = 0; i < debugFrames.Count; i++)
                    debugFrames[i].CopyTo(debugInfo, i);
                debugInfo.WriteTo("debug_info.tiff");
            }
        }
    }
}
