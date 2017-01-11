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

        public static Vector operator /(Vector v, double l)
        {
            return new Vector(v.X / l, v.Y / l);
        }

        public void Normalize()
        {
            double l = Length();
            X /= l;
            Y /= l;
        }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        public Vector Rotated(double degrees)
        {
            double rad = DegToRad(degrees);
            double c = Math.Cos(rad);
            double s = Math.Sin(rad);
            return new Vector(
                X * c - Y * s,
                X * s + Y * c);
        }

        public double AngleTo(Vector p2)
        {
            double angle = RadToDeg(Math.Atan2(p2.Y, p2.X) - Math.Atan2(Y, X));
            if (angle < -180.0)
                angle += 360.0;
            else if (angle > 180.0)
                angle -= 360.0;
            return angle;
        }

        public static double DegToRad(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static double RadToDeg(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        public double X;
        public double Y;
    }
}
