﻿// "Therefore those skilled at the unorthodox
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

        /// <summary>
        /// used for <see cref="MeasureString(string,System.Drawing.Font,float,out int,out int)"/> calculation.
        /// </summary>
        private static readonly int[] _charFit = new int[1];

        /// <summary>
        /// used for <see cref="MeasureString(string,System.Drawing.Font,float,out int,out int)"/> calculation.
        /// </summary>
        private static readonly int[] _charFitWidth = new int[1000];

        /// <summary>
        /// The wrapped WinForms graphics object
        /// </summary>
        private readonly Graphics _g;

        /// <summary>
        /// the initialized HDC used
        /// </summary>
        private IntPtr _hdc;

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        public WinGraphics(Graphics g)
        {
            _g = g;
        }

        /// <summary>
        /// Gets the bounding clipping region of this graphics.
        /// </summary>
        /// <returns>The bounding rectangle for the clipping region</returns>
        public RectangleF GetClip()
        {
            if( _hdc == IntPtr.Zero )
            {
                return _g.ClipBounds;
            }
            else
            {
                Rectangle lprc;
                Win32Utils.GetClipBox(_hdc, out lprc);
                return lprc;
            } 
        }

        /// <summary>
        /// Sets the clipping region of this <see cref="T:System.Drawing.Graphics"/> to the result of the specified operation combining the current clip region and the rectangle specified by a <see cref="T:System.Drawing.RectangleF"/> structure.
        /// </summary>
        /// <param name="rect"><see cref="T:System.Drawing.RectangleF"/> structure to combine. </param>
        /// <param name="combineMode">Member of the <see cref="T:System.Drawing.Drawing2D.CombineMode"/> enumeration that specifies the combining operation to use. </param>
        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            ReleaseHdc();
            _g.SetClip(rect, combineMode);
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
            SetFont(font);

            var size = new Size();
            Win32Utils.GetTextExtentPoint32(_hdc, str, str.Length, ref size);
            return size;
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
            SetFont(font);

            var size = new Size();
            Win32Utils.GetTextExtentExPoint(_hdc, str, str.Length, (int)Math.Round(maxWidth), _charFit, _charFitWidth, ref size);
            charFit = _charFit[0];
            charFitWidth = charFit > 0 ? _charFitWidth[charFit-1] : 0;
            return size;
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
            SetFont(font);
            SetTextColor(color);

            Win32Utils.TextOut(_hdc, (int)Math.Round(point.X), (int)Math.Round(point.Y), str, str.Length);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ReleaseHdc();
        }
        

        #region Delegate graphics methods

        /// <summary>
        /// Gets or sets the rendering quality for this <see cref="T:System.Drawing.Graphics"/>.
        /// </summary>
        /// <returns>
        /// One of the <see cref="T:System.Drawing.Drawing2D.SmoothingMode"/> values.
        /// </returns>
        /// <PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public SmoothingMode SmoothingMode
        {
            get
            {
                ReleaseHdc();
                return _g.SmoothingMode;
            }
            set
            {
                ReleaseHdc();
                _g.SmoothingMode = value;
            }
        }

        /// <summary>
        /// Draws a line connecting the two points specified by the coordinate pairs.
        /// </summary>
        /// <param name="pen"><see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the line. </param><param name="x1">The x-coordinate of the first point. </param><param name="y1">The y-coordinate of the first point. </param><param name="x2">The x-coordinate of the second point. </param><param name="y2">The y-coordinate of the second point. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.</exception>
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            ReleaseHdc();
            _g.DrawLine(pen, (int)x1, (int)y1, (int)x2, (int) y2);
        }

        /// <summary>
        /// Draws a rectangle specified by a coordinate pair, a width, and a height.
        /// </summary>
        /// <param name="pen">A <see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the rectangle. </param><param name="x">The x-coordinate of the upper-left corner of the rectangle to draw. </param><param name="y">The y-coordinate of the upper-left corner of the rectangle to draw. </param><param name="width">The width of the rectangle to draw. </param><param name="height">The height of the rectangle to draw. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.</exception>
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            ReleaseHdc();
            _g.DrawRectangle(pen, (int)x, (int)y, (int)width, (int)height);
        }

        public void FillRectangle(Brush getSolidBrush, float left, float top, float width, float height)
        {
            ReleaseHdc();
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
            ReleaseHdc();
            _g.DrawImage(image, RenderUtils.ToRect(destRect), RenderUtils.ToRect(srcRect), GraphicsUnit.Pixel);
        }

        /// <summary>
        /// Draws the specified <see cref="T:System.Drawing.Image"/> at the specified location and with the specified size.
        /// </summary>
        /// <param name="image"><see cref="T:System.Drawing.Image"/> to draw. </param><param name="destRect"><see cref="T:System.Drawing.Rectangle"/> structure that specifies the location and size of the drawn image. </param><exception cref="T:System.ArgumentNullException"><paramref name="image"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void DrawImage(Image image, RectangleF destRect)
        {
            ReleaseHdc();
            _g.DrawImage(image, (int) destRect.X, (int) destRect.Y);
        }

        /// <summary>
        /// Draws a <see cref="T:System.Drawing.Drawing2D.GraphicsPath"/>.
        /// </summary>
        /// <param name="pen"><see cref="T:System.Drawing.Pen"/> that determines the color, width, and style of the path. </param><param name="path"><see cref="T:System.Drawing.Drawing2D.GraphicsPath"/> to draw. </param><exception cref="T:System.ArgumentNullException"><paramref name="pen"/> is null.-or-<paramref name="path"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            _g.DrawPath(pen, path);
        }

        /// <summary>
        /// Fills the interior of a <see cref="T:System.Drawing.Drawing2D.GraphicsPath"/>.
        /// </summary>
        /// <param name="brush"><see cref="T:System.Drawing.Brush"/> that determines the characteristics of the fill. </param><param name="path"><see cref="T:System.Drawing.Drawing2D.GraphicsPath"/> that represents the path to fill. </param><exception cref="T:System.ArgumentNullException"><paramref name="brush"/> is null.-or-<paramref name="path"/> is null.</exception><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/></PermissionSet>
        public void FillPath(Brush brush, GraphicsPath path)
        {
            ReleaseHdc();
            _g.FillPath(brush, path);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points specified by <see cref="T:System.Drawing.PointF"/> structures.
        /// </summary>
        /// <param name="brush"><see cref="T:System.Drawing.Brush"/> that determines the characteristics of the fill. </param><param name="points">Array of <see cref="T:System.Drawing.PointF"/> structures that represent the vertices of the polygon to fill. </param><exception cref="T:System.ArgumentNullException"><paramref name="brush"/> is null.-or-<paramref name="points"/> is null.</exception>
        public void FillPolygon(Brush brush, PointF[] points)
        {
            ReleaseHdc();

            Point[] newPoints = new Point[points.Length];
            for(int i = 0; i< points.Length; i++)
            {
                newPoints[i] = new Point((int)points[i].X, (int)points[i].Y);
            }
            _g.FillPolygon(brush, newPoints);
        }

        #endregion


        #region Private methods

        /// <summary>
        /// Init HDC for the current graphics object to be used to call GDI directly.
        /// </summary>
        private void InitHdc()
        {
            if(_hdc == IntPtr.Zero)
            {
                var clip = _g.Clip.GetHrgn(_g);

                _hdc = _g.GetHdc();
                Win32Utils.SetBkMode(_hdc, 1);

                Win32Utils.SelectClipRgn(_hdc, clip);

                Win32Utils.DeleteObject(clip);
            }
        }

        /// <summary>
        /// Release current HDC to be able to use <see cref="Graphics"/> methods.
        /// </summary>
        private void ReleaseHdc()
        {
            if (_hdc != IntPtr.Zero)
            {
                Win32Utils.SelectClipRgn(_hdc, IntPtr.Zero);
                _g.ReleaseHdc(_hdc);
                _hdc = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Set a resource (e.g. a font) for the specified device context.
        /// WARNING: Calling Font.ToHfont() many times without releasing the font handle crashes the app.
        /// </summary>
        private void SetFont(Font font)
        {
            InitHdc();
            Win32Utils.SelectObject(_hdc, FontsUtils.GetCachedHFont(font));
        }

        /// <summary>
        /// Set the text color of the device context.
        /// </summary>
        private void SetTextColor(Color color)
        {
            InitHdc();
            int rgb = ( color.B & 0xFF ) << 16 | ( color.G & 0xFF ) << 8 | color.R;
            Win32Utils.SetTextColor(_hdc, rgb);
        }

        #endregion
    }
}