using System.Collections.Generic;

namespace RiverTrace
{
    class Intersection
    {
        private static bool LinesIntersect(Vector a, Vector b, Vector c, Vector d)
        {
            // Source:
            //   http://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect#comment19165734_565282

            Vector CmP = new Vector(c.X - a.X, c.Y - a.Y);
            Vector r = new Vector(b.X - a.X, b.Y - a.Y);
            Vector s = new Vector(d.X - c.X, d.Y - c.Y);

            double CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            double CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            double rxs = r.X * s.Y - r.Y * s.X;

            if (CmPxr == 0.0)
            {
                return ((c.X - a.X < 0.0) != (c.X - b.X < 0.0)) ||
                    ((c.Y - a.Y < 0.0) != (c.Y - b.Y < 0.0));
            }

            if (rxs == 0.0)
                return false;

            double rxsr = 1.0 / rxs;
            double t = CmPxs * rxsr;
            double u = CmPxr * rxsr;

            return (t >= 0.0) && (t <= 1.0) && (u >= 0.0) && (u <= 1.0);
        }

        public static bool Check(List<Vector> way, Vector point)
        {
            if (way.Count < 3)
                return true;
            Vector lastPoint = way[way.Count - 1];
            for (int i = 0; i < way.Count - 2; i++)
                if (LinesIntersect(way[i], way[i + 1], lastPoint, point))
                    return false;
            return true;
        }
    }
}
