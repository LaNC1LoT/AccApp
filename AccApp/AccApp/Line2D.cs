using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace AccApp
{
    public struct Line2D
    {
        public Line2D(Vector2 pt1, Vector2 pt2) : this()
        {
            Point1 = pt1;
            Point2 = pt2;

            Vector = Point2 - Point1;
            Normal = Vector2.Normalize(new Vector2(-Vector.Y, Vector.X));
        }

        public Vector2 Point1 { private set; get; }

        public Vector2 Point2 { private set; get; }

        public Vector2 Vector { private set; get; }

        public Vector2 Normal { private set; get; }

        public float Angle
        {
            get
            {
                return (float)Math.Atan2(Point2.Y - Point1.Y,
                                         Point2.X - Point1.X);
            }
        }

        public Line2D Shift(Vector2 shift)
        {
            return new Line2D(Point1 + shift, Point2 + shift);
        }

        public Line2D ShiftOut(Vector2 shift)
        {
            Line2D shifted = Shift(shift);
            Vector2 normalizedVector = Vector2.Normalize(Vector);
            float length = shift.Length();

            return new Line2D(shifted.Point1 - length * normalizedVector,
                              shifted.Point2 + length * normalizedVector);
        }

        public Vector2 Intersection(Line2D line)
        {

            IntersectTees(line, out float tThis, out float tThat);

            return Point1 + tThis * (Point2 - Point1);
        }

        public Vector2 SegmentIntersection(Line2D line)
        {
            IntersectTees(line, out float tThis, out float tThat);

            if (tThis < 0 || tThis > 1 || tThat < 0 || tThat > 1)
                return new Vector2(float.NaN, float.NaN);

            return Point1 + tThis * (Point2 - Point1);
        }

        void IntersectTees(Line2D line, out float tThis, out float tThat)
        {
            float den = line.Vector.Y * Vector.X - line.Vector.X * Vector.Y;

            tThis = (line.Vector.X * (Point1.Y - line.Point1.Y) -
                     line.Vector.Y * (Point1.X - line.Point1.X)) / den;

            tThat = (Vector.X * (Point1.Y - line.Point1.Y) -
                     Vector.Y * (Point1.X - line.Point1.X)) / den;
        }

        public override string ToString()
        {
            return string.Format("{0} --> {1}", Point1, Point2);
        }

        public static bool IsValid(Vector2 vector)
        {
            return !float.IsNaN(vector.X) && !float.IsInfinity(vector.X) &&
                   !float.IsNaN(vector.Y) && !float.IsInfinity(vector.Y);
        }
    }

    public struct MazeCell
    {
        public bool HasLeft { internal set; get; }

        public bool HasTop { internal set; get; }

        public bool HasRight { internal set; get; }

        public bool HasBottom { internal set; get; }

        public MazeCell(bool left, bool top, bool right, bool bottom) : this()
        {
            HasLeft = left;
            HasTop = top;
            HasRight = right;
            HasBottom = bottom;
        }
    }
}
