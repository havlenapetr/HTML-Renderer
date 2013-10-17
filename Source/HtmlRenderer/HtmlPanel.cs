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
using System.ComponentModel;
using System.Windows.Forms;
using HtmlRenderer.Entities;
using HtmlRenderer.Parse;
using HtmlRenderer.Utils;

namespace HtmlRenderer
{
    /// <summary>
    /// Provides HTML rendering using the text property.<br/>
    /// WinForms control that will render html content in it's client rectangle.<br/>
    /// If <see cref="AutoScroll"/> is true and the layout of the html resulted in its content beyond the client bounds 
    /// of the panel it will show scrollbars (horizontal/vertical) allowing to scroll the content.<br/>
    /// If <see cref="AutoScroll"/> is false html content outside the client bounds will be clipped.<br/>
    /// The control will handle mouse and keyboard events on it to support html text selection, copy-paste and mouse clicks.<br/>
    /// <para>
    /// The major differential to use HtmlPanel or HtmlLabel is size and scrollbars.<br/>
    /// If the size of the control depends on the html content the HtmlLabel should be used.<br/>
    /// If the size is set by some kind of layout then HtmlPanel is more suitable, also shows scrollbars if the html contents is larger than the control client rectangle.<br/>
    /// </para>
    /// <para>
    /// <h4>AutoScroll:</h4>
    /// Allows showing scrollbars if html content is placed outside the visible boundaries of the panel.
    /// </para>
    /// <para>
    /// <h4>LinkClicked event:</h4>
    /// Raised when the user clicks on a link in the html.<br/>
    /// Allows canceling the execution of the link.
    /// </para>
    /// <para>
    /// <h4>StylesheetLoad event:</h4>
    /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
    /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// </para>
    /// <para>
    /// <h4>ImageLoad event:</h4>
    /// Raised when an image is about to be loaded by file path or URI.<br/>
    /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
    /// </para>
    /// <para>
    /// <h4>RenderError event:</h4>
    /// Raised when an error occurred during html rendering.<br/>
    /// </para>
    /// </summary>
    public class HtmlPanel : Control
    {

        #region Fields and Consts

        /// <summary>
        /// 
        /// </summary>
        private HtmlContainer _htmlContainer;

        /// <summary>
        /// the raw base stylesheet data used in the control
        /// </summary>
        private string _baseRawCssData;

        /// <summary>
        /// the base stylesheet data used in the control
        /// </summary>
        private CssData _baseCssData;

        private PointF _autoScrollPosition;

        private VScrollBar _scrollbar;

        private bool _flagResized;

        private readonly RenderThread _renderThread;

        #endregion


        /// <summary>
        /// Creates a new HtmlPanel and sets a basic css for it's styling.
        /// </summary>
        public HtmlPanel(bool asyncRendering)
        {
            BackColor = SystemColors.Window;
#if PC
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
#endif
            _autoScrollPosition = new PointF();
            _htmlContainer = new HtmlContainer();
            _htmlContainer.LinkClicked += OnLinkClicked;
            _htmlContainer.RenderError += OnRenderError;
            _htmlContainer.Refresh += OnRefresh;
            _htmlContainer.ScrollChange += OnScrollChange;
            _htmlContainer.StylesheetLoad += OnStylesheetLoad;
            _htmlContainer.ImageLoad += OnImageLoad;
            if (asyncRendering)
                _renderThread = new RenderThread(new PaintEventHandler(OnPaint));
        }

        public HtmlPanel()
            : this(false)
        {
        }

        /// <summary>
        /// Raised when the user clicks on a link in the html.<br/>
        /// Allows canceling the execution of the link.
        /// </summary>
        public event EventHandler LinkClicked;

        /// <summary>
        /// Raised when an error occurred during html rendering.<br/>
        /// </summary>
        public event EventHandler RenderError;

        /// <summary>
        /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
        /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
        /// If no alternative data is provided the original source will be used.<br/>
        /// </summary>
        public event EventHandler StylesheetLoad;

        /// <summary>
        /// Raised when an image is about to be loaded by file path or URI.<br/>
        /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
        /// </summary>
        public event EventHandler ImageLoad;

        /// <summary>
        /// Gets or sets a value indicating if anti-aliasing should be avoided for geometry like backgrounds and borders (default - false).
        /// </summary>
        public bool AvoidGeometryAntialias
        {
            get { return _htmlContainer.AvoidGeometryAntialias; }
            set { _htmlContainer.AvoidGeometryAntialias = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating if image loading only when visible should be avoided (default - false).<br/>
        /// True - images are loaded as soon as the html is parsed.<br/>
        /// False - images that are not visible because of scroll location are not loaded until they are scrolled to.
        /// </summary>
        /// <remarks>
        /// Images late loading improve performance if the page contains image outside the visible scroll area, especially if there is large 
        /// amount of images, as all image loading is delayed (downloading and loading into memory).<br/>
        /// Late image loading may effect the layout and actual size as image without set size will not have actual size until they are loaded
        /// resulting in layout change during user scroll.<br/>
        /// Early image loading may also effect the layout if image without known size above the current scroll location are loaded as they
        /// will push the html elements down.
        /// </remarks>
        public bool AvoidImagesLateLoading
        {
            get { return _htmlContainer.AvoidImagesLateLoading; }
            set { _htmlContainer.AvoidImagesLateLoading = value; }
        }

        [EditorBrowsable(EditorBrowsableState.Always)]
        public VScrollBar Scrollbar
        {
            get { return _scrollbar; }
            set
            {
                if (_scrollbar != null)
                {
                    _scrollbar.ValueChanged -= OnScrollbarValueChanged;
                }
                _scrollbar = value;
                _scrollbar.ValueChanged += new EventHandler(OnScrollbarValueChanged);
            }
        }

        private void OnScrollbarValueChanged(object sender, EventArgs e)
        {
            ScrollTo(new PointF(.0f, -1.0f *
                ((_scrollbar.Value * _htmlContainer.ActualSizeLocked.Height) / 100 - ClientSize.Height)), null);
        }

        /// <summary>
        /// Is content selection is enabled for the rendered html (default - true).<br/>
        /// If set to 'false' the rendered html will be static only with ability to click on links.
        /// </summary>
        [DefaultValue(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
#if PC
        [Browsable(true)]
        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Is content selection is enabled for the rendered html.")]
#endif
        public bool IsSelectionEnabled
        {
            get { return _htmlContainer.IsSelectionEnabled; }
            set { _htmlContainer.IsSelectionEnabled = value; }
        }

        /// <summary>
        /// Is the build-in context menu enabled and will be shown on mouse right click (default - true)
        /// </summary>
        [DefaultValue(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
#if PC
        [Browsable(true)]
        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Is the build-in context menu enabled and will be shown on mouse right click.")]
#endif
        public bool IsContextMenuEnabled
        {
            get { return _htmlContainer.IsContextMenuEnabled; }
            set { _htmlContainer.IsContextMenuEnabled = value; }
        }

        /// <summary>
        /// Set base stylesheet to be used by html rendered in the panel.
        /// </summary>
#if PC
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Set base stylesheet to be used by html rendered in the control.")]
#endif
        public string BaseStylesheet
        {
            get { return _baseRawCssData; }
            set
            {
                _baseRawCssData = value;
                _baseCssData = CssParser.ParseStyleSheet(value, true);
            }
        }

        /// <summary>
        /// Gets or sets the text of this panel
        /// </summary>
#if PC
        [Browsable(true)]
        [Description("Sets the html of this control.")]
#endif
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
#if !CF_1_0 && !CF_2_0
                if (!IsDisposed)
#endif
                {
                    _htmlContainer.SetHtml(Text, _baseCssData);
                    OnResize(EventArgs.Empty);
                    if (!ScrollTo(PointF.Empty, _scrollbar))
                    {
                        Invalidate();
                    }
                }
            }
        }

        public RectangleF[] FindPlainText(String text, bool scrollToFirst)
        {
            bool invalidated = false;

            RectangleF[] rects = _htmlContainer.FindPlainText(text);
            if (rects.Length > 0)
            {
                invalidated = ScrollTo(new PointF(.0f, rects[0].Y * -1.0f));
            }
            if (!invalidated)
            {
                Invalidate();
            }

            return rects;
        }

        public bool ScrollTo(PointF p)
        {
            return ScrollTo(p, _scrollbar);
        }

        private bool ScrollTo(PointF p, VScrollBar scrollbar)
        {
            SizeF currSize = _htmlContainer.ActualSizeLocked;
            float min = 0.0f;
            float max = (currSize.Height - ClientSize.Height) * -1.0f;

            if (p.Y >= min)
            {
                p.Y = min;
            }
            else if (p.Y < max)
            {
                p.Y = max;
            }

            if (_autoScrollPosition.Equals(p))
            {
                return false;
            }

            if (scrollbar != null)
            {
                int pageMax = (int)Math.Round(currSize.Height);
                if (pageMax > 0)
                {
                    int pagePos = ((int)Math.Round(Math.Abs(p.Y) + ClientSize.Height) * 100) / pageMax;
                    scrollbar.Value = pagePos;
                }
            }

            _autoScrollPosition = p;
            Invalidate();
            return true;
        }

        /// <summary>
        /// Get html from the current DOM tree with inline style.
        /// </summary>
        /// <returns>generated html</returns>
        public string GetHtml()
        {
            return _htmlContainer != null ? _htmlContainer.GetHtml() : null;
        }

        #region Private methods

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            if (_renderThread != null)
            {
                _renderThread.Stop();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _flagResized = true;
            if (_renderThread != null)
                _renderThread.ScreenSize = new Size(Width, Height);
        }

        /// <summary>
        /// Perform paint of the html in the control.
        /// </summary>
        protected void OnPaint(object parent, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            if (_flagResized)
            {
                _htmlContainer.MaxSizeLocked = new SizeF(e.ClipRectangle.Width, 0);
                _htmlContainer.PerformLayoutLocked(e.Graphics, false);
                _flagResized = false;
            }
            _htmlContainer.ScrollOffsetLocked = _autoScrollPosition;
            _htmlContainer.PerformPaintLocked(g, false);
        }

        /// <summary>
        /// Perform paint of the html in the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_htmlContainer.ActualSizeLocked.Equals(SizeF.Empty) || _renderThread == null)
            {
                OnPaint(this, e);
            }
            else
            {
                _renderThread.Flush(this, e.Graphics);
            }

            if (_scrollbar != null)
            {
                int max = (int)Math.Round(_htmlContainer.ActualSizeLocked.Height);
                _scrollbar.Minimum = (ClientSize.Height * 100) / max;
                _scrollbar.Maximum = 100;

                if (Height >= max && _scrollbar.Visible)
                {
                    _scrollbar.Hide();
                    Width += _scrollbar.Width;
                }
                else if (Height < max && !_scrollbar.Visible)
                {
                    _scrollbar.Show();
                    Width -= _scrollbar.Width;
                }
            }
        }

        /// <summary>
        /// Set focus on the control for keyboard scrrollbars handling.
        /// </summary>
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
        }

        /// <summary>
        /// Handle mouse move to handle hover cursor and text selection. 
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleMouseMove(this, e);
        }

#if PC
        /// <summary>
        /// Handle mouse leave to handle cursor change.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleMouseLeave(this);
        }
#endif

        /// <summary>
        /// Handle mouse down to handle selection. 
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleMouseDown(this, e);
        }

        /// <summary>
        /// Handle mouse up to handle selection and link click. 
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleMouseUp(this, e);
        }

#if PC
        /// <summary>
        /// Handle mouse double click to select word under the mouse. 
        /// </summary>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleMouseDoubleClick(this, e);
        }
#endif

        /// <summary>
        /// Handle key down event for selection, copy and scrollbars handling.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_htmlContainer != null)
                _htmlContainer.HandleKeyDownLocked(this, e);

            float min = 0.0f;
            float max = (_htmlContainer.ActualSizeLocked.Height - ClientSize.Height) * -1.0f;

            PointF p = new PointF(_autoScrollPosition.X, _autoScrollPosition.Y);
            switch (e.KeyValue)
            {
#if PC
                case 0x28:
#else
                case 0x27:
#endif
                    p.Y -= 70;
                    break;

#if PC
                case 0x26:
#else
                case 0x25:
#endif
                    p.Y += 70;
                    break;

#if PC
                case 0x21:
#else
                case 0xbd:
#endif
                    p.Y = min;
                    break;

#if PC
                case 0x22:
#else
                case 0xbe:
#endif
                    p.Y = max;
                    break;
            }

            ScrollTo(p);
        }

        /// <summary>
        /// Propagate the LinkClicked event from root container.
        /// </summary>
        private void OnLinkClicked(object sender, EventArgs ev)
        {
            HtmlLinkClickedEventArgs e = ev as HtmlLinkClickedEventArgs;
            if (LinkClicked != null)
            {
                LinkClicked(this, e);
            }
        }

        /// <summary>
        /// Propagate the Render Error event from root container.
        /// </summary>
        private void OnRenderError(object sender, EventArgs ev)
        {
            HtmlRenderErrorEventArgs e = ev as HtmlRenderErrorEventArgs;
            if (RenderError != null)
            {
                if (InvokeRequired)
                    Invoke(RenderError, this, e);
                else
                    RenderError(this, e);
            }
        }

        /// <summary>
        /// Propagate the stylesheet load event from root container.
        /// </summary>
        private void OnStylesheetLoad(object sender, EventArgs ev)
        {
            HtmlStylesheetLoadEventArgs e = ev as HtmlStylesheetLoadEventArgs;
            if (StylesheetLoad != null)
            {
                StylesheetLoad(this, e);
            }
        }

        /// <summary>
        /// Propagate the image load event from root container.
        /// </summary>
        private void OnImageLoad(object sender, EventArgs ev)
        {
            HtmlImageLoadEventArgs e = ev as HtmlImageLoadEventArgs;
            if (ImageLoad != null)
            {
                ImageLoad(this, e);
            }
        }

        /// <summary>
        /// Handle html renderer invalidate and re-layout as requested.
        /// </summary>
        private void OnRefresh(object sender, EventArgs ev)
        {
            HtmlRefreshEventArgs e = ev as HtmlRefreshEventArgs;
#if PC
            if (e.Layout)
            {
                if (InvokeRequired)
                    Invoke(new MethodInvoker(PerformLayout));
                else
                    PerformLayout();
            }
            if (InvokeRequired)
                Invoke(new MethodInvoker(Invalidate));
            else
                Invalidate();
#else
            Invalidate();
#endif
        }

        /// <summary>
        /// On html renderer scroll request adjust the scrolling of the panel to the requested location.
        /// </summary>
        private void OnScrollChange(object sender, EventArgs ev)
        {
            HtmlScrollEventArgs e = ev as HtmlScrollEventArgs;
            ScrollTo(new PointF(e.Location.X, e.Location.Y), _scrollbar);
        }

#if PC
        /// <summary>
        /// Used to add arrow keys to the handled keys in <see cref="OnKeyDown"/>.
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }
#endif

        /// <summary>
        /// Release the html container resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_htmlContainer != null)
            {
                _htmlContainer.LinkClicked -= OnLinkClicked;
                _htmlContainer.RenderError -= OnRenderError;
                _htmlContainer.Refresh -= OnRefresh;
                _htmlContainer.ScrollChange -= OnScrollChange;
                _htmlContainer.StylesheetLoad -= OnStylesheetLoad;
                _htmlContainer.ImageLoad -= OnImageLoad;
                _htmlContainer.Dispose();
                _htmlContainer = null;
            }
            base.Dispose(disposing);
        }

        #region Hide not relevant properties from designer

        /// <summary>
        /// Not applicable.
        /// </summary>
#if PC
        [Browsable(false)]
#endif
        public override Font Font
        {
            get { return base.Font; }
            set { base.Font = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
#if PC
        [Browsable(false)]
#endif
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
#if PC
        [Browsable(false)]
        public override bool AllowDrop
        {
            get { return base.AllowDrop; }
            set { base.AllowDrop = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override RightToLeft RightToLeft
        {
            get { return base.RightToLeft; }
            set { base.RightToLeft = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override Cursor Cursor
        {
            get { return base.Cursor; }
            set { base.Cursor = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public new bool UseWaitCursor
        {
            get { return base.UseWaitCursor; }
            set { base.UseWaitCursor = value; }
        }
#endif

        #endregion

        #endregion

        private class RenderThread
        {
            private Size _screenSize;

            private Bitmap _bmpScreen;
            private Bitmap _bmpRender;
            private Exception _error;
            private PaintEventHandler _handler;

            private volatile Object _lock;
            private volatile bool _started;
            private volatile bool _stopping;
            private System.Threading.Thread _thread;

            public RenderThread(PaintEventHandler handler)
            {
                _lock = new Object();
                _handler = handler;
                _thread = new System.Threading.Thread(
                     new System.Threading.ThreadStart(ThreadProc));
                _thread.Name = "HtmlPandel.RenderThread";
            }

            public Exception Error
            {
                get
                {
                    return _error;
                }
            }

            public Size ScreenSize
            {
                get
                {
                    return _screenSize;
                }
                set
                {
                    _screenSize = value;
                }
            }

            public void Start()
            {
                if (_started)
                    return;
                _thread.Start();
                _started = true;
            }

            public void Stop()
            {
                if (!_started)
                    return;
                _stopping = true;
                _thread.Join();
            }

            public void Flush(Control c, Graphics g)
            {
                if (!_started)
                {
                    Start();
                    return;
                }
                lock (_lock)
                {
                    if (_bmpScreen != null)
                    {
                        g.DrawImage(_bmpScreen, 0, 0);
                    }
                }
            }

            private static Bitmap PrepareBitmap(Bitmap bmp, Size size)
            {
                if (bmp != null)
                {
                    if (size.Width == bmp.Width &&
                        size.Height == bmp.Height)
                    {
                        return bmp;
                    }
                    bmp.Dispose();
                }
                return new Bitmap(size.Width, size.Height);
            }

            private void ThreadProc()
            {
                while (!_stopping)
                {
                    //Console.WriteLine("Draw - Start");
                    try
                    {
                        using (Graphics g = Graphics.FromImage(
                            (_bmpRender = PrepareBitmap(_bmpRender, _screenSize))))
                        {
                            _handler(this, new PaintEventArgs(g, new Rectangle(
                                0, 0, _screenSize.Width, _screenSize.Height)));
                        }

                        //Console.WriteLine("DrawFlip - Start");
                        lock (_lock)
                        {
                            _bmpScreen = _bmpRender.Clone() as Bitmap;
                        }
                        //Console.WriteLine("DrawFlip - End");
                    }
                    catch (Exception ex)
                    {
                        _error = ex;
                        _stopping = true;
                    }

                    System.Threading.Thread.Sleep(10);
                }
                //Console.WriteLine("RenderThread.ThreadProc - End[ " + _error + "]");
            }
        }
    }
}