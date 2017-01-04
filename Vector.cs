using System;

namespace RiverTrace
{
    class Vector
    {
        public Vector()
        {
            X = 0.0;
            Y = 0.0;
        }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector operator *(Vector v, double l)
        {
            return new Vector(v.X * l, v.Y * l);
        }

        public void Normalize()
        {
            double l = Math.Sqrt(X * X + Y * Y);
            X /= l;
            Y /= l;
        }

        public Vector Rotated(double degrees)
        {
            double rad = degrees * (Math.PI / 180);
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);
            return new Vector(
                X * c - Y * s,
                X * s + Y * c);
        }

        public double X;
        public double Y;
    }
}
