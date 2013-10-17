using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using HtmlRenderer.Entities;

namespace HtmlRenderer
{
    public partial class HtmlPanelEx : UserControl
    {
        private HtmlRenderer.HtmlPanel htmlPanel1;
        private System.Windows.Forms.VScrollBar vScrollBar1;

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
        /// Raised when an mouse click occurred.<br/>
        /// </summary>
        public new event MouseEventHandler MouseDown;

        public string BaseStylesheet
        {
            set
            {
                htmlPanel1.BaseStylesheet = value;
            }
        }

        public new string Text
        {
            get
            {
                return htmlPanel1.Text;
            }
            set
            {
                htmlPanel1.Text = value;
            }
        }

        public HtmlPanelEx()
        {
            InitializeComponent();

            htmlPanel1.LinkClicked += new EventHandler(OnLinkClicked);
            htmlPanel1.RenderError += new EventHandler(OnRenderError);
            htmlPanel1.MouseDown += new MouseEventHandler(OnMouseDown);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            htmlPanel1.LinkClicked -= OnLinkClicked;
            htmlPanel1.RenderError -= OnRenderError;
            htmlPanel1.MouseDown -= OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (MouseDown != null)
            {
                MouseDown(sender, e);
            }
        }

        private void OnRenderError(object sender, EventArgs ev)
        {
            if (RenderError != null)
            {
                RenderError(sender, ev);
            }
        }

        private void OnLinkClicked(object sender, EventArgs ev)
        {
            HtmlLinkClickedEventArgs e = ev as HtmlLinkClickedEventArgs;
            if (e.Handled = LinkClicked != null)
            {
                LinkClicked(sender, e);
            }
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.htmlPanel1 = new HtmlRenderer.HtmlPanel();
            this.SuspendLayout();
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar1.LargeChange = 1;
            this.vScrollBar1.Location = new System.Drawing.Point(224, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(14, 272);
            this.vScrollBar1.TabIndex = 1;
            // 
            // htmlPanel1
            // 
            this.htmlPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.htmlPanel1.AvoidGeometryAntialias = false;
            this.htmlPanel1.AvoidImagesLateLoading = false;
            this.htmlPanel1.BackColor = System.Drawing.SystemColors.Window;
            this.htmlPanel1.BaseStylesheet = null;
            this.htmlPanel1.Location = new System.Drawing.Point(0, 0);
            this.htmlPanel1.Name = "htmlPanel1";
            this.htmlPanel1.Scrollbar = this.vScrollBar1;
            this.htmlPanel1.Size = new System.Drawing.Size(237, 272);
            this.htmlPanel1.TabIndex = 0;
            this.htmlPanel1.Text = "htmlPanel1";
            // 
            // HtmlPanelEx
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.vScrollBar1);
            this.Controls.Add(this.htmlPanel1);
            this.Name = "HtmlPanelEx";
            this.Size = new System.Drawing.Size(240, 272);
            this.ResumeLayout(false);

        }

        #endregion

        public RectangleF[] FindPlainText(String text, bool scrollToFirst)
        {
            return htmlPanel1.FindPlainText(text, scrollToFirst);
        }

        public bool ScrollTo(PointF p)
        {
            return htmlPanel1.ScrollTo(p);
        }
    }
}
