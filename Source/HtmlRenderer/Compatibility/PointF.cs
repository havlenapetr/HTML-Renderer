using System;

namespace System.Drawing
{
    public struct PointF
    {
        // Summary:
        //     Represents a new instance of the System.Drawing.PointF class with member
        //     data left uninitialized.
        public static readonly PointF Empty = new PointF(.0f, .0f);

        public float X;
        public float Y;

        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
