// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they bagin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using HtmlRenderer.Utils;

namespace HtmlRenderer
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class WinGraphics : IGraphics
    {
        #region Fields and Consts

        private static readonly TextCache _textCache = new TextCache(15);

        private static readonly SolidBrush _brush = new SolidBrush(Color.Empty);

        /// <summary>
        /// The wrapped WinForms graphics object
        /// </summary>
        private readonly Graphics _g;

        private PointF _y;

        private RectangleF _clip;

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        public WinGraphics(Graphics g)
        {
            _g = g;
            _clip = g.ClipBounds;
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                return SmoothingMode.None;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets the bounding clipping region of this graphics.
        /// </summary>
        /// <returns>The bounding rectangle for the clipping region</returns>
        public RectangleF GetClip()
        {
            return _clip;
        }

        /// <summary>
        /// Sets the clipping region of this <see cref="T:System.Drawing.Graphics"/> to the result of the specified operation combining the current clip region and the rectangle specified by a <see cref="T:System.Drawing.RectangleF"/> structure.
        /// </summary>
        /// <param name="rect"><see cref="T:System.Drawing.RectangleF"/> structure to combine. </param>
        /// <param name="combineMode">Member of the <see cref="T:System.Drawing.Drawing2D.CombineMode"/> enumeration that specifies the combining operation to use. </param>
        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            _clip = rect;
        }

        /// <summary>
        /// Measure the width and height of string <paramref name="str"/> when drawn on device context HDC
        /// using the given font <paramref name="font"/>.
        /// </summary>
        /// <param name="str">the string to measure</param>
        /// <param name="font">the font to measure string with</param>
        /// <returns>the size of the string</returns>
        public Size MeasureString(string str, Font font)
        {
            SizeF s = _g.MeasureString(str, font);
            return new Size((int)Math.Round(s.Width), (int)Math.Round(s.Height));
        }

        /// <summary>
        /// Measure the width and height of string <paramref name="str"/> when drawn on device context HDC
        /// using the given font <paramref name="font"/>.<br/>
        /// Restrict the width of the string and get the number of characters able to fit in the restriction and
        /// the width those characters take.
        /// </summary>
        /// <param name="str">the string to measure</param>
        /// <param name="font">the font to measure string with</param>
        /// <param name="maxWidth">the max width to render the string in</param>
        /// <param name="charFit">the number of characters that will fit under <see cref="maxWidth"/> restriction</param>
        /// <param name="charFitWidth"></param>
        /// <returns>the size of the string</returns>
        public Size MeasureString(string str, Font font, float maxWidth, out int charFit, out int charFitWidth)
        {
            charFit = charFitWidth = 0;
            return MeasureString(str, font);
        }

        /// <summary>
        /// Draw the given string using the given font and foreground color at given location.
        /// </summary>
        /// <param name="str">the string to draw</param>
        /// <param name="font">the font to use to draw the string</param>
        /// <param name="color">the text color to set</param>
        /// <param name="point">the location to start string draw (top-left)</param>
        public void DrawString(String str, Font font, Color color, PointF point)
        {
            if (!_textCache.EqualsLine((int)Math.Round(point.Y)))
            {
                _textCache.Flush(_g, _brush);
            }
            _textCache.Add(str, font, color, point);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _textCache.Flush(_g, _brush);
        }

        private class TextCacheItem
        {
            private String mText;
            private Font mFont;
            private Color mColor;
            private Point mPoint;

            public String Text
            {
                get
                {
                    return mText;
                }
            }

            public Font Font
            {
                get
                {
                    return mFont;
                }
            }

            public Color Color
            {
                get
                {
                    return mColor;
                }
            }

            public Point Point
            {
                get
                {
                    return mPoint;
                }
            }

            public TextCacheItem(String text, Font font, Color color, PointF point)
            {
                Init(text, font, color, point);
            }

            public void Init(String text, Font font, Color color, PointF point)
            {
                mText = text;
                mFont = font;
                mColor = color;
                mPoint = new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
            }

            public bool EqualsLine(TextCacheItem item)
            {
                return Font.Equals(item.Font) && Color.Equals(item.Color) && Point.Y == item.Point.Y;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                TextCacheItem item = obj as TextCacheItem;
                if (item == null)
                {
                    return false;
                }
                return Text.Equals(item.Text) && Font.Equals(item.Font) &&
                    Color.Equals(item.Color) && Point.Equals(item.Point);
            }
        }

        private class TextCache
        {
            private int mSize;
            private TextCacheItem[] mStack;

            public TextCache(int count)
            {
                mStack = new TextCacheItem[count];
            }

            public bool EqualsLine(int y)
            {
                if (mSize <= 0)
                {
                    return false;
                }
                return mStack[0].Point.Y == y;
            }

            public void Add(String text, Font font, Color color, PointF point)
            {
                if (mSize == mStack.Length)
                {
                    TextCacheItem[] stack = new TextCacheItem[mStack.Length + mStack.Length / 3];
                    Array.Copy(mStack, stack, mStack.Length);
                    mStack = stack;
                }
                if (mStack[mSize] == null)
                {
                    mStack[mSize] = new TextCacheItem(text, font, color, point);
                }
                else
                {
                    mStack[mSize].Init(text, font, color, point);
                }
                mSize++;
            }

            public void Flush(Graphics g, SolidBrush brush)
            {
                if (mSize <= 0)
                {
                    return;
                }
                if (brush.Color != mStack[0].Color)
                {
                    brush.Color = mStack[0].Color;
                }

                String text = String.Empty;
                for (int i = 0; i < mSize; i++)
                {
                    text += mStack[i].Text + " ";
                }

                g.DrawString(text.Remove(text.Length - 1, 1), mStack[0].Font, brush,
                    mStack[0].Point.X, mStack[0].Point.Y);
                mSize = 0;
            }
        }

        #region Delegate graphics methods

        /// <summary>
        /// Draws a line connecting the two points specified by the coordinate pairs.
        /// </summary>
        /// <param name="pen"><see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the line. </param><param name="x1">The x-coordinate of the first point. </param><param name="y1">The y-coordinate of the first point. </param><param name="x2">The x-coordinate of the second point. </param><param name="y2">The y-coordinate of the second point. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.</exception>
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (RenderUtils.RectContains(_clip, x1, y1) || RenderUtils.RectContains(_clip, x2, y2))
                _g.DrawLine(pen, (int)x1, (int)y1, (int)x2, (int) y2);
        }

        /// <summary>
        /// Draws a rectangle specified by a coordinate pair, a width, and a height.
        /// </summary>
        /// <param name="pen">A <see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the rectangle. </param><param name="x">The x-coordinate of the upper-left corner of the rectangle to draw. </param><param name="y">The y-coordinate of the upper-left corner of the rectangle to draw. </param><param name="width">The width of the rectangle to draw. </param><param name="height">The height of the rectangle to draw. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.</exception>
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (RenderUtils.RectIntersect(_clip, x, y, width, height) != RectangleF.Empty)
                _g.DrawRectangle(pen, (int)x, (int)y, (int)width, (int)height);
        }

        public void FillRectangle(Brush getSolidBrush, float left, float top, float width, float height)
        {
            if (RenderUtils.RectIntersect(_clip, left, top, width, height) != RectangleF.Empty)
                _g.FillRectangle(getSolidBrush, (int)left, (int)top, (int)width, (int)height);
        }

        /// <summary>
        /// Draws the specified portion of the specified <see cref="T:System.Drawing.Image"/> at the specified location and with the specified size.
        /// </summary>
        /// <param name="image"><see cref="T:System.Drawing.Image"/> to draw. </param>
        /// <param name="destRect"><see cref="T:System.Drawing.RectangleF"/> structure that specifies the location and size of the drawn image. The image is scaled to fit the rectangle. </param>
        /// <param name="srcRect"><see cref="T:System.Drawing.RectangleF"/> structure that specifies the portion of the <paramref name="image"/> object to draw. </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="image"/> is null.</exception>
        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect)
        {
            if (RenderUtils.RectIntersect(_clip, destRect) != RectangleF.Empty)
                _g.DrawImage(image, RenderUtils.ToRect(destRect), RenderUtils.ToRect(srcRect), GraphicsUnit.Pixel);
        }

        /// <summary>
        /// Draws the specified <see cref="T:System.Drawing.Image"/> at the specified location and with the specified size.
        /// </summary>
        /// <param name="image"><see cref="T:System.Drawing.Image"/> to draw. </param><param name="destRect"><see cref="T:System.Drawing.Rectangle"/> structure that specifies the location and size of the drawn image. </param><exception cref="T:System.ArgumentNullException"><paramref name="image"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void DrawImage(Image image, RectangleF destRect)
        {
            if (RenderUtils.RectIntersect(_clip, destRect) != RectangleF.Empty)
                _g.DrawImage(image, (int) destRect.X, (int) destRect.Y);
        }

        /// <summary>
        /// Draws a <see cref="T:System.Drawing.Drawing2D.GraphicsPath"/>.
        /// </summary>
        /// <param name="pen"><see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the path. </param><param name="path"><see cref="T:System.Drawing.Drawing2D.GraphicsPath"/> to draw. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.-or-<paramref name="path"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            Point[] newPoints = new Point[path.PointCount];
            for(int i = 0; i< path.PointCount; i++)
            {
                newPoints[i] = new Point((int)path.PathPoints[i].X, (int)path.PathPoints[i].Y);
            }
            _g.DrawPolygon(pen, newPoints);
        }

        /// <summary>
        /// Fills the interior of a <see cref="T:System.Drawing.Drawing2D.GraphicsPath"/>.
        /// </summary>
        /// <param name="brush"><see cref="T:System.Drawing.Brush"/> that determines the characteristics of the fill. </param><param name="path"><see cref="T:System.Drawing.Drawing2D.GraphicsPath"/> that represents the path to fill. </param><exception cref="T:System.ArgumentNullException"><paramref name="brush"/> is null.-or-<paramref name="path"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void FillPath(Brush brush, GraphicsPath path)
        {
            FillPolygon(brush, path.PathPoints);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points specified by <see cref="T:System.Drawing.PointF"/> structures.
        /// </summary>
        /// <param name="brush"><see cref="T:System.Drawing.Brush"/> that determines the characteristics of the fill. </param><param name="points">Array of <see cref="T:System.Drawing.PointF"/> structures that represent the vertices of the polygon to fill. </param><exception cref="T:System.ArgumentNullException"><paramref name="brush"/> is null.-or-<paramref name="points"/> is null.</exception>
        public void FillPolygon(Brush brush, PointF[] points)
        {
            Point[] newPoints = new Point[points.Length];
            for(int i = 0; i< points.Length; i++)
            {
                newPoints[i] = new Point((int)points[i].X, (int)points[i].Y);
            }
            _g.FillPolygon(brush, newPoints);
        }

        #endregion

   }
}