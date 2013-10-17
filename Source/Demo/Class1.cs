﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace HtmlRenderer.Demo
{
    /// <summary>
    /// Wrapper for GDI text rendering functions<br/>
    /// This class is not thread-safe as GDI function should be called from the UI thread.
    /// </summary>
    public sealed class TextRenderer2 : IDisposable
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
        /// cache of all the font used not to create same font again and again
        /// </summary>
        private static readonly Dictionary<string, Dictionary<float, Dictionary<FontStyle, IntPtr>>> _fontsCache = new Dictionary<string, Dictionary<float, Dictionary<FontStyle, IntPtr>>>(StringComparer.InvariantCultureIgnoreCase);

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
        public TextRenderer2(Graphics g)
        {
            _g = g;

            var clip = _g.Clip.GetHrgn(_g);

            _hdc = _g.GetHdc();
            SetBkMode(_hdc, 1);

            SelectClipRgn(_hdc, clip);

            DeleteObject(clip);
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
            GetTextExtentPoint32(_hdc, str, str.Length, ref size);
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
            GetTextExtentExPoint(_hdc, str, str.Length, (int)Math.Round(maxWidth), _charFit, _charFitWidth, ref size);
            charFit = _charFit[0];
            charFitWidth = charFit > 0 ? _charFitWidth[charFit - 1] : 0;
            return size;
        }

        /// <summary>
        /// Draw the given string using the given font and foreground color at given location.
        /// </summary>
        /// <param name="str">the string to draw</param>
        /// <param name="font">the font to use to draw the string</param>
        /// <param name="color">the text color to set</param>
        /// <param name="point">the location to start string draw (top-left)</param>
        public void DrawString(String str, Font font, Color color, Point point)
        {
            SetFont(font);
            SetTextColor(color);

            TextOut(_hdc, point.X, point.Y, str, str.Length);
        }

        /// <summary>
        /// Draw the given string using the given font and foreground color at given location.<br/>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd162498(v=vs.85).aspx.
        /// </summary>
        /// <param name="str">the string to draw</param>
        /// <param name="font">the font to use to draw the string</param>
        /// <param name="color">the text color to set</param>
        /// <param name="rect">the rectangle in which the text is to be formatted</param>
        /// <param name="flags">The method of formatting the text</param>
        public void DrawString(String str, Font font, Color color, Rectangle rect, TextFormatFlags flags)
        {
            SetFont(font);
            SetTextColor(color);

            var rect2 = new Rect(rect);
            DrawText(_hdc, str, str.Length, ref rect2, (uint)flags);
        }

        /// <summary>
        /// Release current HDC to be able to use <see cref="Graphics"/> methods.
        /// </summary>
        public void Dispose()
        {
            if (_hdc != IntPtr.Zero)
            {
                SelectClipRgn(_hdc, IntPtr.Zero);
                _g.ReleaseHdc(_hdc);
                _hdc = IntPtr.Zero;
            }
        }


        #region Private methods

        /// <summary>
        /// Set a resource (e.g. a font) for the specified device context.
        /// </summary>
        private void SetFont(Font font)
        {
            SelectObject(_hdc, GetCachedHFont(font));
        }

        /// <summary>
        /// Get cached unmanaged font handle for given font.<br/>
        /// </summary>
        /// <param name="font">the font to get unmanaged font handle for</param>
        /// <returns>handle to unmanaged font</returns>
        private static IntPtr GetCachedHFont(Font font)
        {
            IntPtr hfont = IntPtr.Zero;
            Dictionary<float, Dictionary<FontStyle, IntPtr>> dic1;
            if (_fontsCache.TryGetValue(font.Name, out dic1))
            {
                Dictionary<FontStyle, IntPtr> dic2;
                if (dic1.TryGetValue(font.Size, out dic2))
                {
                    dic2.TryGetValue(font.Style, out hfont);
                }
                else
                {
                    dic1[font.Size] = new Dictionary<FontStyle, IntPtr>();
                }
            }
            else
            {
                _fontsCache[font.Name] = new Dictionary<float, Dictionary<FontStyle, IntPtr>>();
                _fontsCache[font.Name][font.Size] = new Dictionary<FontStyle, IntPtr>();
            }

            if (hfont == IntPtr.Zero)
            {
                _fontsCache[font.Name][font.Size][font.Style] = hfont = font.ToHfont();
            }

            return hfont;
        }

        /// <summary>
        /// Set the text color of the device context.
        /// </summary>
        private void SetTextColor(Color color)
        {
            int rgb = ( color.B & 0xFF ) << 16 | ( color.G & 0xFF ) << 8 | color.R;
            SetTextColor(_hdc, rgb);
        }

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern int SelectObject(IntPtr hdc, IntPtr hgdiObj);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll", EntryPoint = "GetTextExtentPoint32W")]
        private static extern int GetTextExtentPoint32(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Size size);

        [DllImport("gdi32.dll", EntryPoint = "GetTextExtentExPointW")]
        private static extern bool GetTextExtentExPoint(IntPtr hDc, [MarshalAs(UnmanagedType.LPWStr)]string str, int nLength, int nMaxExtent, int[] lpnFit, int[] alpDx, ref Size size);

        [DllImport("gdi32.dll", EntryPoint = "TextOutW")]
        private static extern bool TextOut(IntPtr hdc, int x, int y, [MarshalAs(UnmanagedType.LPWStr)] string str, int len);

        [DllImport("user32.dll", EntryPoint = "DrawTextW")]
        private static extern int DrawText(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Rect rect, uint uFormat);

        [DllImport("gdi32.dll")]
        private static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // ReSharper disable NotAccessedField.Local
        private struct Rect
        {
            private int _left;
            private int _top;
            private int _right;
            private int _bottom;

            public Rect(Rectangle r)
            {
                _left = r.Left;
                _top = r.Top;
                _bottom = r.Bottom;
                _right = r.Right;
            }
        }
        // ReSharper restore NotAccessedField.Local

        #endregion
    }

    /// <summary>
    /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd162498(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum TextFormatFlags : uint
    {
        Default = 0x00000000,
        Center = 0x00000001,
        Right = 0x00000002,
        VCenter = 0x00000004,
        Bottom = 0x00000008,
        WordBreak = 0x00000010,
        SingleLine = 0x00000020,
        ExpandTabs = 0x00000040,
        TabStop = 0x00000080,
        NoClip = 0x00000100,
        ExternalLeading = 0x00000200,
        CalcRect = 0x00000400,
        NoPrefix = 0x00000800,
        Internal = 0x00001000,
        EditControl = 0x00002000,
        PathEllipsis = 0x00004000,
        EndEllipsis = 0x00008000,
        ModifyString = 0x00010000,
        RtlReading = 0x00020000,
        WordEllipsis = 0x00040000,
        NoFullWidthCharBreak = 0x00080000,
        HidePrefix = 0x00100000,
        ProfixOnly = 0x00200000,
    }

}
