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
using System.Runtime.InteropServices;

namespace HtmlRenderer.Utils
{
    /// <summary>
    /// Utility for Win32 API.
    /// </summary>
    internal static class WinCEUtils
    {
        [DllImport("coredll.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("coredll.dll")]
        public static extern IntPtr WindowFromDC(IntPtr hdc);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <remarks>
        /// In conformance with conventions for the RECT structure, the bottom-right coordinates of the returned rectangle are exclusive. In other words, 
        /// the pixel at (right, bottom) lies immediately outside the rectangle.
        /// </remarks>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("coredll.dll")]
        public static extern int GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <remarks>
        /// In conformance with conventions for the RECT structure, the bottom-right coordinates of the returned rectangle are exclusive. In other words, 
        /// the pixel at (right, bottom) lies immediately outside the rectangle.
        /// </remarks>
        /// <param name="handle">A handle to the window.</param>
        /// <returns>RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</returns>
        public static Rectangle GetWindowRectangle(IntPtr handle)
        {
            Rectangle rect;
            GetWindowRect(handle, out rect);
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        [DllImport("coredll.dll")]
        public static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("coredll.dll")]
        public static extern int SelectObject(IntPtr hdc, IntPtr hgdiObj);

        [DllImport("coredll.dll")]
        public static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("coredll.dll", EntryPoint = "GetTextExtentPoint32W")]
        public static extern int GetTextExtentPoint32(IntPtr hdc, [MarshalAs(UnmanagedType.LPWStr)] string str, int len, ref Size size);

        [DllImport("coredll.dll", EntryPoint = "GetTextExtentExPointW")]
        public static extern bool GetTextExtentExPoint(IntPtr hDc, [MarshalAs(UnmanagedType.LPWStr)]string str, int nLength, int nMaxExtent, int[] lpnFit, int[] alpDx, ref Size size);

        [DllImport("coredll.dll", EntryPoint = "TextOutW")]
        public static extern bool TextOut(IntPtr hdc, int x, int y, [MarshalAs(UnmanagedType.LPWStr)] string str, int len);

        [DllImport("coredll.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("coredll.dll")]
        public static extern int GetClipBox(IntPtr hdc, out Rectangle lprc);

        [DllImport("coredll.dll")]
        public static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);

        [DllImport("coredll.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
