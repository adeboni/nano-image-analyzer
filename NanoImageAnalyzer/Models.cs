using System;
using System.Drawing;

namespace NanoImageAnalyzer
{
    public class ScaleObject
    {
        public Point A, B;
        public double PhysicalLength;

        public double Scale()
        {
            return PhysicalLength / Math.Sqrt((B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y));
        }
    }

    public class DrawingObject 
    {
        public Point A, B;
        public double Scale;
        public DrawingType Type;

        public double Length()
        {
            return Math.Sqrt((B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y)) * Scale;
        }

        public double Radius()
        {
            return Math.Sqrt((B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y)) / 2;
        }

        public Point Midpoint()
        {
            return new Point((A.X + B.X) / 2, (A.Y + B.Y) / 2);
        }
    }

    public enum DrawingType
    {
        Line,
        Circle
    }
}
