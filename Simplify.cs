using System;
using System.Collections.Generic;

namespace RiverTrace
{
    // Source:
    //   https://www.codeproject.com/articles/18936/a-csharp-implementation-of-douglas-peucker-line-ap
    class Simplify
    {
        public static List<Vector> DouglasPeuckerReduction(List<Vector> points, double tolerance)
        {
            if (points == null || points.Count < 3)
                return points;

            int firstPoint = 0;
            int lastPoint = points.Count - 1;
            List<int> pointIndexsToKeep = new List<int>();

            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            while (points[firstPoint].Equals(points[lastPoint]))
                lastPoint--;

            DouglasPeuckerReduction(points, firstPoint, lastPoint,
                tolerance, ref pointIndexsToKeep);

            List<Vector> returnPoints = new List<Vector>();
            pointIndexsToKeep.Sort();
            foreach (int index in pointIndexsToKeep)
                returnPoints.Add(points[index]);

            return returnPoints;
        }

        private static void DouglasPeuckerReduction(List<Vector> points,
            int firstPoint, int lastPoint, double tolerance, ref List<int> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++)
            {
                double distance = PerpendicularDistance(
                    points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint,
                indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest,
                lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        public static double PerpendicularDistance(Vector point1, Vector point2, Vector point)
        {
            double area = Math.Abs(.5 * (point1.X * point2.Y + point2.X *
                point.Y + point.X * point1.Y - point2.X * point1.Y - point.X *
                point2.Y - point1.X * point.Y));
            double bottom = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) +
                Math.Pow(point1.Y - point2.Y, 2));
            double height = area / bottom * 2;
            return height;
        }
    }
}