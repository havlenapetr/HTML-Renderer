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
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HtmlRenderer.Dom;
using HtmlRenderer.Entities;
using HtmlRenderer.Handlers;
using HtmlRenderer.Parse;
using HtmlRenderer.Utils;

namespace HtmlRenderer
{
    /// <summary>
    /// Low level handling of Html Renderer logic, this class is used by <see cref="HtmlParser"/>, <see cref="HtmlLabel"/>, <see cref="HtmlToolTip"/> and <see cref="HtmlRender"/>.<br/>
    /// The class allows html layout and rendering without association to actual winforms control, those allowing to handle html rendering on any graphics object.<br/>
    /// Using this class will require the client to handle all propagations of mouse\keyboard events, layout/paint calls, scrolling offset, 
    /// location/size/rectangle handling and UI refresh requests.<br/>
    /// <para>
    /// <b>MaxSize and ActualSize:</b><br/>
    /// The max width and height of the rendered html.<br/>
    /// The max width will effect the html layout wrapping lines, resize images and tables where possible.<br/>
    /// The max height does NOT effect layout, but will not render outside it (clip).<br/>
    /// <see cref="ActualSize"/> can be exceed the max size by layout restrictions (unwrappable line, set image size, etc.).<br/>
    /// Set zero for unlimited (width\height separately).<br/>
    /// </para>
    /// <para>
    /// <b>ScrollOffset:</b><br/>
    /// This will adjust the rendered html by the given offset so the content will be "scrolled".<br/>
    /// Element that is rendered at location (50,100) with offset of (0,200) will not be rendered as it
    /// will be at -100 therefore outside the client rectangle of the control.
    /// </para>
    /// <para>
    /// <b>LinkClicked event</b><br/>
    /// Raised when the user clicks on a link in the html.<br/>
    /// Allows canceling the execution of the link.
    /// </para>
    /// <para>
    /// <b>StylesheetLoad event:</b><br/>
    /// Raised when aa stylesheet is about to be loaded by file path or URI by link element.<br/>
    /// This event allows to provide the stylesheet manually or provide new source (file or Uri) to load from.<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// </para>
    /// <para>
    /// <b>ImageLoad event:</b><br/>
    /// Raised when an image is about to be loaded by file path or URI.<br/>
    /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
    /// </para>
    /// <para>
    /// <b>Refresh event:</b><br/>
    /// Raised when html renderer requires refresh of the control hosting (invalidation and re-layout).<br/>
    /// There is no guarantee that the event will be raised on the main thread, it can be raised on thread-pool thread.
    /// </para>
    /// <para>
    /// <b>RenderError event:</b><br/>
    /// Raised when an error occurred during html rendering.<br/>
    /// </para>
    /// </summary>
    public sealed class HtmlContainer : IDisposable
    {
        #region Fields and Consts

        /// <summary>
        /// the root css box of the parsed html
        /// </summary>
        private CssBox _root;

        /// <summary>
        /// Handler for text selection in the html. 
        /// </summary>
        private SelectionHandler _selectionHandler;

        /// <summary>
        /// the text fore color use for selected text
        /// </summary>
        private Color _selectionForeColor;

        /// <summary>
        /// the backcolor to use for selected text
        /// </summary>
        private Color _selectionBackColor;

        /// <summary>
        /// the parsed stylesheet data used for handling the html
        /// </summary>
        private CssData _cssData;

        /// <summary>
        /// Is content selection is enabled for the rendered html (default - true).<br/>
        /// If set to 'false' the rendered html will be static only with ability to click on links.
        /// </summary>
        private bool _isSelectionEnabled = true;

        /// <summary>
        /// Is the build-in context menu enabled (default - true)
        /// </summary>
        private bool _isContextMenuEnabled = true;

        /// <summary>
        /// Gets or sets a value indicating if antialiasing should be avoided 
        /// for geometry like backgrounds and borders
        /// </summary>
        private bool _avoidGeometryAntialias;

        /// <summary>
        /// Gets or sets a value indicating if image loading only when visible should be avoided (default - false).<br/>
        /// </summary>
        private bool _avoidImagesLateLoading;

        /// <summary>
        /// the top-left most location of the rendered html
        /// </summary>
        private PointF _location;

        /// <summary>
        /// the max width and height of the rendered html, effects layout, actual size cannot exceed this values.<br/>
        /// Set zero for unlimited.<br/>
        /// </summary>
        private SizeF _maxSize;

        /// <summary>
        /// Gets or sets the scroll offset of the document for scroll controls
        /// </summary>
        private PointF _scrollOffset;

        /// <summary>
        /// The actual size of the rendered html (after layout)
        /// </summary>
        private SizeF _actualSize;

        private InterlockedMutex _lock;

        #endregion


        /// <summary>
        /// Raised when the user clicks on a link in the html.<br/>
        /// Allows canceling the execution of the link.
        /// </summary>
        public event EventHandler LinkClicked;

        /// <summary>
        /// Raised when html renderer requires refresh of the control hosting (invalidation and re-layout).
        /// </summary>
        /// <remarks>
        /// There is no guarantee that the event will be raised on the main thread, it can be raised on thread-pool thread.
        /// </remarks>
        public event EventHandler Refresh;

        /// <summary>
        /// Raised when Html Renderer request scroll to specific location.<br/>
        /// This can occur on document anchor click.
        /// </summary>
        public event EventHandler ScrollChange;

        /// <summary>
        /// Raised when an error occurred during html rendering.<br/>
        /// </summary>
        /// <remarks>
        /// There is no guarantee that the event will be raised on the main thread, it can be raised on thread-pool thread.
        /// </remarks>
        public event EventHandler RenderError;

        /// <summary>
        /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
        /// This event allows to provide the stylesheet manually or provide new source (file or Uri) to load from.<br/>
        /// If no alternative data is provided the original source will be used.<br/>
        /// </summary>
        public event EventHandler StylesheetLoad;

        /// <summary>
        /// Raised when an image is about to be loaded by file path or URI.<br/>
        /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
        /// </summary>
        public event EventHandler ImageLoad;

        /// <summary>
        /// the parsed stylesheet data used for handling the html
        /// </summary>
        public CssData CssData
        {
            get { return _cssData; }
        }

        /// <summary>
        /// Gets or sets a value indicating if anti-aliasing should be avoided for geometry like backgrounds and borders (default - false).
        /// </summary>
        public bool AvoidGeometryAntialias
        {
            get { return _avoidGeometryAntialias; }
            set { _avoidGeometryAntialias = value; }
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
            get { return _avoidImagesLateLoading; }
            set { _avoidImagesLateLoading = value; }
        }

        /// <summary>
        /// Is content selection is enabled for the rendered html (default - true).<br/>
        /// If set to 'false' the rendered html will be static only with ability to click on links.
        /// </summary>
        public bool IsSelectionEnabled
        {
            get { return _isSelectionEnabled; }
            set { _isSelectionEnabled = value; }
        }

        /// <summary>
        /// Is the build-in context menu enabled and will be shown on mouse right click (default - true)
        /// </summary>
        public bool IsContextMenuEnabled
        {
            get { return _isContextMenuEnabled; }
            set { _isContextMenuEnabled = value; }
        }

        /// <summary>
        /// The scroll offset of the html.<br/>
        /// This will adjust the rendered html by the given offset so the content will be "scrolled".<br/>
        /// </summary>
        /// <example>
        /// Element that is rendered at location (50,100) with offset of (0,200) will not be rendered as it
        /// will be at -100 therefore outside the rectangle of the control.
        /// </example>
        public PointF ScrollOffset
        {
            get { return _scrollOffset; }
            set { _scrollOffset = value; }
        }

        public PointF ScrollOffsetLocked
        {
            get
            {
                using (AutoLock l = new AutoLock(_lock))
                {
                    return _scrollOffset;
                }
            }
            set
            {
                using (AutoLock l = new AutoLock(_lock))
                {
                    _scrollOffset = value;
                }
            }
        }

        /// <summary>
        /// The top-left most location of the rendered html.<br/>
        /// This will offset the top-left corner of the rendered html.
        /// </summary>
        public PointF Location
        {
            get { return _location; }
            set { _location = value; }
        }

        /// <summary>
        /// The max width and height of the rendered html.<br/>
        /// The max width will effect the html layout wrapping lines, resize images and tables where possible.<br/>
        /// The max height does NOT effect layout, but will not render outside it (clip).<br/>
        /// <see cref="ActualSize"/> can be exceed the max size by layout restrictions (unwrappable line, set image size, etc.).<br/>
        /// Set zero for unlimited (width\height separately).<br/>
        /// </summary>
        public SizeF MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        public SizeF MaxSizeLocked
        {
            set
            {
                using (AutoLock l = new AutoLock(_lock))
                {
                    _maxSize = value;
                }
            }
        }

        /// <summary>
        /// The actual size of the rendered html (after layout)
        /// </summary>
        public SizeF ActualSize
        {
            get { return _actualSize; }
            internal set { _actualSize = value; }
        }

        public SizeF ActualSizeLocked
        {
            get
            {
                using (AutoLock l = new AutoLock(_lock))
                {
                    return _actualSize;
                }
            }
        }

        /// <summary>
        /// the root css box of the parsed html
        /// </summary>
        internal CssBox Root
        {
            get { return _root; }
        }

        /// <summary>
        /// the text fore color use for selected text
        /// </summary>
        internal Color SelectionForeColor
        {
            get { return _selectionForeColor; }
            set { _selectionForeColor = value; }
        }

        /// <summary>
        /// the back-color to use for selected text
        /// </summary>
        internal Color SelectionBackColor
        {
            get { return _selectionBackColor; }
            set { _selectionBackColor = value; }
        }

        public HtmlContainer()
        {
#if PROFILE
            String logFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.
                GetExecutingAssembly().GetModules()[0].FullyQualifiedName), "profile.txt");
            ODP.Diagnostics.LogFn.AddListener(new ODP.Diagnostics.
                LogFileTraceListener(logFile, 2000000));
#endif
            _lock = new InterlockedMutex("HtmlContainer.Mutex");
        }

        /// <summary>
        /// Init with optional document and stylesheet.
        /// </summary>
        /// <param name="htmlSource">the html to init with, init empty if not given</param>
        /// <param name="baseCssData">optional: the stylesheet to init with, init default if not given</param>
        public void SetHtml(string htmlSource, CssData baseCssData)
        {
            if (_root != null)
            {
                _root.Dispose();
                _root = null;
                if (_selectionHandler != null)
                    _selectionHandler.Dispose();
                _selectionHandler = null;
            }

            if (!string.IsNullOrEmpty(htmlSource))
            {
                _cssData = baseCssData ?? CssUtils.DefaultCssData;

                _root = DomParser.GenerateCssTree(htmlSource, this, ref _cssData);
                if (_root != null)
                {
                    _selectionHandler = new SelectionHandler(_root);
                }
            }
        }

        public string GetHtml()
        {
            return GetHtml(HtmlGenerationStyle.Inline);
        }

        /// <summary>
        /// Get html from the current DOM tree with style if requested.
        /// </summary>
        /// <param name="styleGen">Optional: controls the way styles are generated when html is generated (default: <see cref="HtmlGenerationStyle.Inline"/>)</param>
        /// <returns>generated html</returns>
        private string GetHtml(HtmlGenerationStyle styleGen)
        {
            return DomUtils.GenerateHtml(_root, styleGen);
        }

        /// <summary>
        /// Get attribute value of element at the given x,y location by given key.<br/>
        /// If more than one element exist with the attribute at the location the inner most is returned.
        /// </summary>
        /// <param name="location">the location to find the attribute at</param>
        /// <param name="attribute">the attribute key to get value by</param>
        /// <returns>found attribute value or null if not found</returns>
        public string GetAttributeAt(Point location, string attribute)
        {
            ArgChecker.AssertArgNotNullOrEmpty(attribute, "attribute");

            var cssBox = DomUtils.GetCssBox(_root, OffsetByScroll(location));
            return cssBox != null ? DomUtils.GetAttribute(cssBox, attribute) : null;
        }

        /// <summary>
        /// Get css link href at the given x,y location.
        /// </summary>
        /// <param name="location">the location to find the link at</param>
        /// <returns>css link href if exists or null</returns>
        public string GetLinkAt(Point location)
        {
            var link = DomUtils.GetLinkBox(_root, OffsetByScroll(location));
            return link != null ? link.HrefLink : null;
        }

        public RectangleF[] FindPlainText(string text)
        {
            CssRect[] cssRects = DomUtils.SelectPlainText(_root, _selectionHandler, text);
            RectangleF[] rects = new RectangleF[cssRects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                rects[i] = new RectangleF(
                    cssRects[i].Rectangle.X,
                    cssRects[i].Rectangle.Y,
                    cssRects[i].Rectangle.Width,
                    cssRects[i].Rectangle.Height);
            }
            return rects;
        }

        public void PerformLayoutLocked(Graphics g, bool profile)
        {
            using (AutoLock l = new AutoLock(_lock))
            {
                PerformLayout(g, profile);
            }
        }

        public void PerformLayout(Graphics g)
        {
            PerformLayout(g, false);
        }

        /// <summary>
        /// Measures the bounds of box and children, recursively.
        /// </summary>
        /// <param name="g">Device context to draw</param>
#if PROFILE
        public void PerformLayout(Graphics g, bool profile)
#else
        private void PerformLayout(Graphics g, bool profile)
#endif
        {
            ArgChecker.AssertArgNotNull(g, "g");

            if (_root != null)
            {
                using (ArgChecker.Profile("HtmlContainer.PerformLayout", profile))
                {
                    using (var ig = new WinGraphics(g))
                    {
                        _actualSize = SizeF.Empty;

                        // if width is not restricted we set it to large value to get the actual later
                        _root.Size = new SizeF(_maxSize.Width > 0 ? _maxSize.Width : 99999, 0);
                        _root.Location = _location;
                        _root.PerformLayout(ig, profile);

                        if (_maxSize.Width <= 0.1)
                        {
                            // in case the width is not restricted we need to double layout, first will find the width so second can layout by it (center alignment)
                            _root.Size = new SizeF((int)Math.Ceiling(_actualSize.Width), 0);
                            _actualSize = SizeF.Empty;
                            _root.PerformLayout(ig, profile);
                        }
                    }
                }
            }
        }

        public void PerformPaintLocked(Graphics g, bool profile)
        {
            using (AutoLock l = new AutoLock(_lock))
            {
                PerformPaint(g, profile);
            }
        }

        public void PerformPaint(Graphics g)
        {
            PerformPaint(g, false);
        }

        /// <summary>
        /// Render the html using the given device.
        /// </summary>
        /// <param name="g">the device to use to render</param>
#if PROFILE
        public void PerformPaint(Graphics g, bool profile)
#else
        private void PerformPaint(Graphics g, bool profile)
#endif
        {
            ArgChecker.AssertArgNotNull(g, "g");

            using (ArgChecker.Profile("HtmlContainer.PerformPaint", profile))
            {
                using (var ig = new WinGraphics(g))
                {
                    RectangleF prevClip = RectangleF.Empty;
                    if (_maxSize.Height > 0)
                    {
                        prevClip = ig.GetClip();
                        ig.SetClip(new RectangleF(_location.X, _location.Y, _maxSize.Width, _maxSize.Height), CombineMode.Replace);
                    }

                    if (_root != null)
                    {
                        _root.Paint(ig, profile);
                    }

                    if (prevClip != RectangleF.Empty)
                    {
                        ig.SetClip(prevClip, CombineMode.Replace);
                    }
                }
            }

            ArgChecker.ProfileDump();
        }

        /// <summary>
        /// Handle mouse down to handle selection.
        /// </summary>
        /// <param name="parent">the control hosting the html to invalidate</param>
        /// <param name="e">the mouse event args</param>
        public void HandleMouseDown(Control parent, MouseEventArgs e)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");
            ArgChecker.AssertArgNotNull(e, "e");

            try
            {
                if (_selectionHandler != null)
                    _selectionHandler.HandleMouseDown(parent, OffsetByScroll(new Point(e.X, e.Y)),
                        IsMouseInContainer(new Point(e.X, e.Y)));
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed mouse down handle", ex);
            }
        }

        /// <summary>
        /// Handle mouse up to handle selection and link click.
        /// </summary>
        /// <param name="parent">the control hosting the html to invalidate</param>
        /// <param name="e">the mouse event args</param>
        public void HandleMouseUp(Control parent, MouseEventArgs e)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");
            ArgChecker.AssertArgNotNull(e, "e");

            try
            {
                if (_selectionHandler != null && IsMouseInContainer(new Point(e.X, e.Y)))
                {
                    var ignore = _selectionHandler.HandleMouseUp(parent, e.Button);
                    if (!ignore && (e.Button & MouseButtons.Left) != 0)
                    {
                        var loc = OffsetByScroll(new Point(e.X, e.Y));
                        var link = DomUtils.GetLinkBox(_root, loc);
                        if (link != null)
                        {
                            HandleLinkClicked(link);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed mouse up handle", ex);
            }
        }

        /// <summary>
        /// Handle mouse double click to select word under the mouse.
        /// </summary>
        /// <param name="parent">the control hosting the html to set cursor and invalidate</param>
        /// <param name="e">mouse event args</param>
        public void HandleMouseDoubleClick(Control parent, MouseEventArgs e)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");
            ArgChecker.AssertArgNotNull(e, "e");

            try
            {
                if (_selectionHandler != null && IsMouseInContainer(new Point(e.X, e.Y)))
                    _selectionHandler.SelectWord(parent, OffsetByScroll(new Point(e.X, e.Y)));
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed mouse double click handle", ex);
            }
        }

        /// <summary>
        /// Handle mouse move to handle hover cursor and text selection.
        /// </summary>
        /// <param name="parent">the control hosting the html to set cursor and invalidate</param>
        /// <param name="e">the mouse event args</param>
        public void HandleMouseMove(Control parent, MouseEventArgs e)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");
            ArgChecker.AssertArgNotNull(e, "e");

            try
            {
                if (_selectionHandler != null && IsMouseInContainer(new Point(e.X, e.Y)))
                    _selectionHandler.HandleMouseMove(parent, OffsetByScroll(new Point(e.X, e.Y)));
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed mouse move handle", ex);
            }
        }

        /// <summary>
        /// Handle mouse leave to handle hover cursor.
        /// </summary>
        /// <param name="parent">the control hosting the html to set cursor and invalidate</param>
        public void HandleMouseLeave(Control parent)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");

            try
            {
                if (_selectionHandler != null)
                    _selectionHandler.HandleMouseLeave(parent);
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed mouse leave handle", ex);
            }
        }

        public void HandleKeyDownLocked(Control parent, KeyEventArgs e)
        {
            using (AutoLock l = new AutoLock(_lock))
            {
                HandleKeyDown(parent, e);
            }
        }

        /// <summary>
        /// Handle key down event for selection and copy.
        /// </summary>
        /// <param name="parent">the control hosting the html to invalidate</param>
        /// <param name="e">the pressed key</param>
        public void HandleKeyDown(Control parent, KeyEventArgs e)
        {
            ArgChecker.AssertArgNotNull(parent, "parent");
            ArgChecker.AssertArgNotNull(e, "e");

            try
            {
                if (e.Control && _selectionHandler != null)
                {
                    // select all
                    if (e.KeyCode == Keys.A)
                    {
                        _selectionHandler.SelectAll(parent);
                    }

                    // copy currently selected text
                    if (e.KeyCode == Keys.C)
                    {
                        _selectionHandler.CopySelectedHtml();
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.KeyboardMouse, "Failed key down handle", ex);
            }
        }

        /// <summary>
        /// Raise the stylesheet load event with the given event args.
        /// </summary>
        /// <param name="args">the event args</param>
        internal void RaiseHtmlStylesheetLoadEvent(HtmlStylesheetLoadEventArgs args)
        {

            try
            {
                if (StylesheetLoad != null)
                {
                    StylesheetLoad(this, args);
                }
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.CssParsing, "Failed stylesheet load event", ex);
            }
        }

        /// <summary>
        /// Raise the image load event with the given event args.
        /// </summary>
        /// <param name="args">the event args</param>
        internal void RaiseHtmlImageLoadEvent(HtmlImageLoadEventArgs args)
        {
            try
            {
                if (ImageLoad != null)
                {
                    ImageLoad(this, args);
                }
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.Image, "Failed image load event", ex);
            }
        }

        /// <summary>
        /// Request invalidation and re-layout of the control hosting the renderer.
        /// </summary>
        /// <param name="layout">is re-layout is required for the refresh</param>
        internal void RequestRefresh(bool layout)
        {
            try
            {
                if (Refresh != null)
                {
                    Refresh(this, new HtmlRefreshEventArgs(layout));
                }
            }
            catch (Exception ex)
            {
                ReportError(HtmlRenderErrorType.General, "Failed refresh request", ex);
            }
        }

        internal void ReportError(HtmlRenderErrorType type, string message)
        {
            ReportError(type, message, null);
        }

        /// <summary>
        /// Report error in html render process.
        /// </summary>
        /// <param name="type">the type of error to report</param>
        /// <param name="message">the error message</param>
        /// <param name="exception">optional: the exception that occured</param>
        internal void ReportError(HtmlRenderErrorType type, string message, Exception exception)
        {
            try
            {
                if (RenderError != null)
                {
                    RenderError(this, new HtmlRenderErrorEventArgs(type, message, exception));
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Handle link clicked going over <see cref="LinkClicked"/> event and using <see cref="Process.Start()"/> if not canceled.
        /// </summary>
        /// <param name="link">the link that was clicked</param>
        internal void HandleLinkClicked(CssBox link)
        {
            if (LinkClicked != null)
            {
                var args = new HtmlLinkClickedEventArgs(link.HrefLink, link.HtmlTag.Attributes);
                LinkClicked(this, args);
                if (args.Handled)
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(link.HrefLink))
            {
                if (link.HrefLink.StartsWith("#"))
                {
                    if (ScrollChange != null)
                    {
                        var linkBox = DomUtils.GetBoxById(_root, link.HrefLink.Substring(1));
                        if (linkBox != null)
                        {
                            RectangleF rect = CommonUtils.GetFirstValueOrDefault(linkBox.Rectangles);
                            ScrollChange(this, new HtmlScrollEventArgs(RenderUtils.PointRound(new PointF(rect.X, rect.Y))));
                        }
                    }
                }
                else
                {
                    var nfo = new ProcessStartInfo(link.HrefLink, "");
                    nfo.UseShellExecute = true;
                    Process.Start(nfo);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            using (AutoLock l = new AutoLock(_lock))
            {
                Dispose(true);
            }
        }


        #region Private methods

        /// <summary>
        /// Adjust the offset of the given location by the current scroll offset.
        /// </summary>
        /// <param name="location">the location to adjust</param>
        /// <returns>the adjusted location</returns>
        private Point OffsetByScroll(Point location)
        {
            location.Offset(-(int)_scrollOffset.X, -(int)_scrollOffset.Y);
            return location;
        }

        /// <summary>
        /// Check if the mouse is currently on the html container.<br/>
        /// Relevant if the html container is not filled in the hosted control (location is not zero and the size is not the full size of the control).
        /// </summary>
        private bool IsMouseInContainer(Point location)
        {
            return location.X >= _location.X && location.X <= _location.X + _actualSize.Width && location.Y >= _location.Y + _scrollOffset.Y && location.Y <= _location.Y + _scrollOffset.Y + _actualSize.Height;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private void Dispose(bool all)
        {
            try
            {
                if (all)
                {
                    LinkClicked = null;
                    Refresh = null;
                    RenderError = null;
                    StylesheetLoad = null;
                    ImageLoad = null;
                }

                _cssData = null;
                if (_root != null)
                    _root.Dispose();
                _root = null;
                if (_selectionHandler != null)
                    _selectionHandler.Dispose();
                _selectionHandler = null;
            }
            catch
            {
            }
        }

        #endregion


        private interface Lock
        {
            void Lock();
            void Unlock();
        }

        private class InterlockedMutex : Lock
        {
#if !PC
		    const string kernel32 = "coredll";
#else
            const string kernel32 = "kernel32";
#endif

            [System.Runtime.InteropServices.DllImport(kernel32)]
            private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

            [System.Runtime.InteropServices.DllImport(kernel32)]
            private static extern bool SetEvent(IntPtr hHandle);

            [System.Runtime.InteropServices.DllImport(kernel32)]
            private static extern Int32 WaitForSingleObject(IntPtr hHandle, Int32 dwMilliseconds);

            [System.Runtime.InteropServices.DllImport(kernel32)]
            private static extern bool CloseHandle(IntPtr hObject);

            private const int FLAG_NONE = 0;
            private const int FLAG_PROCESSING = 1;
            private const int FLAG_WAITING = 2;

            private readonly IntPtr _event;
            private int _state;

            public InterlockedMutex(String name)
            {
                _event = CreateEvent(IntPtr.Zero, false, false, name);
                _state = FLAG_NONE;
            }

            ~InterlockedMutex()
            {
                CloseHandle(_event);
            }

            private int ObtainState()
            {
                int state = -1;

                for (int i = 0; i < 10; i++)
                {
                    state = Interlocked.Exchange(ref _state, -1);
                    if (state != -1)
                        break;
                    Thread.Sleep(300);
                }
                if (state == -1)
                    throw new Exception("Unable to obtain state!");

                return state;
            }

            public void Lock()
            {
                int state = ObtainState();
                //Console.WriteLine("Lock.State: " + state);
                if (state == 0)
                {
                    //Console.WriteLine("Lock.Processing");
                    Interlocked.Exchange(ref _state, FLAG_PROCESSING);
                }
                else
                {
                    //Console.WriteLine("Lock.WaitForSingleObject");
                    Interlocked.Exchange(ref _state, state | FLAG_WAITING);
                    WaitForSingleObject(_event, 0xFFFFFF);
                    Lock();
                }
            }

            public void Unlock()
            {
                int state = ObtainState();
                //Console.WriteLine("Unlock.State: " + state);
                if ((state & FLAG_WAITING) == FLAG_WAITING)
                {
                    //Console.WriteLine("Unlock.SetEvent");
                    SetEvent(_event);
                }
                else
                {
                    //Console.WriteLine("Unlock.None");
                    Interlocked.Exchange(ref _state, FLAG_NONE);
                }
            }
        }

        private struct AutoLock : IDisposable
        {
            private readonly Lock _lock;

            public AutoLock(Lock l)
            {
                _lock = l;
                _lock.Lock();
            }

            public AutoLock(AutoLock self)
            {
                _lock = self._lock;
            }

            #region IDisposable Members

            public void Dispose()
            {
                _lock.Unlock();
            }

            #endregion
        }
    }
}