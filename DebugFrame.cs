using ColorMine.ColorSpaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RiverTrace
{
    class DebugFrame
    {
        private int pixelRange;
        private int angleSamples;
        private int radiusSamples;
        private double scanRadius;
        private SimpleBitmap sectorRgb;
        private SimpleBitmap sectorDiff;
        private SimpleBitmap polarTrans;
        private SimpleBitmap polarGrid;

        public int Height { get { return Math.Max(pixelRange, radiusSamples); } }

        public int Width { get { return pixelRange * 2 + angleSamples * 2; } }


        public DebugFrame(double scanRadius, int angleSamples, int radiusSamples)
        {
            this.scanRadius = scanRadius;
            this.angleSamples = angleSamples;
            this.radiusSamples = radiusSamples;
            pixelRange = (int)(scanRadius * 2) + 1;
            sectorRgb = new SimpleBitmap(pixelRange, pixelRange);
            sectorDiff = new SimpleBitmap(pixelRange, pixelRange);
            polarTrans = new SimpleBitmap(angleSamples, radiusSamples);
            polarGrid = new SimpleBitmap(angleSamples, radiusSamples);
        }

        private byte MultiplyIntensity(byte intensity, double factor)
        {
            int intensityMul = (int)(intensity * factor);
            if (intensityMul > 255)
                intensityMul = 255;
            return (byte)intensityMul;
        }

        public void SetPolarGrid(double[] anglesGrid)
        {
            double agm = radiusSamples / anglesGrid.Max();
            for (int i = 0; i < angleSamples; i++)
            {
                int h = (int)(agm * anglesGrid[i]);
                byte c1 = 255;
                byte c2 = (byte)((agm * anglesGrid[i] - h) * 255.0);
                if (i == angleSamples / 2)
                {
                    c1 = MultiplyIntensity(c1, 0.8);
                    c2 = MultiplyIntensity(c2, 0.8);
                }
                for (int j = 0; j < h; j++)
                    polarGrid.SetPixel(i, radiusSamples - j - 1, c1);
                if (c2 != 0)
                    polarGrid.SetPixel(i, radiusSamples - h - 1, c2);
            }
        }

        public void SetPolarTrans(int x, int y, double diff)
        {
            byte diffColor = (byte)Math.Round(diff * 255.0);
            if (x == angleSamples / 2)
                diffColor = MultiplyIntensity(diffColor, 1.25);
            polarTrans.SetPixel(x, radiusSamples - y - 1, diffColor);
        }

        public void FillSectors(Vector lastPoint,
            Vector lastDirection, TileMap tileMap, Lab waterColor)
        {
            Parallel.For(0, pixelRange, j =>
            {
                for (int i = 0; i < pixelRange; i++)
                {
                    int x = (int)(lastPoint.X + i - scanRadius);
                    int y = (int)(lastPoint.Y + j - scanRadius);
                    Vector pixelVector = new Vector(x, y) - lastPoint;

                    if (pixelVector.Length() > scanRadius)
                        continue;

                    double angle = lastDirection.AngleTo(pixelVector);
                    if (angle < -Config.Data.angleRange)
                        continue;
                    if (angle > Config.Data.angleRange)
                        continue;

                    Color c = tileMap.GetPixel(x, y);
                    sectorRgb.SetPixel(i, j, c);

                    double invDiff = Math.Max(1.0 -
                        Color.Difference(waterColor, c) / Config.Data.shoreContrast, 0.0);

                    byte diffColor = (byte)Math.Round(invDiff * 255.0);
                    sectorDiff.SetPixel(i, j, diffColor);
                }
            });
        }

        public void CopyTo(SimpleBitmap debugInfo, int index)
        {
            int polarOfs = 0;
            if (Height > radiusSamples)
                polarOfs = (Height - radiusSamples) / 2;

            sectorRgb.CopyTo(debugInfo, 0, index * Height);
            sectorDiff.CopyTo(debugInfo, pixelRange, index * Height);
            polarTrans.CopyTo(debugInfo, pixelRange * 2, index * Height + polarOfs);
            polarGrid.CopyTo(debugInfo, pixelRange * 2 + angleSamples, index * Height + polarOfs);
        }
    }
}
