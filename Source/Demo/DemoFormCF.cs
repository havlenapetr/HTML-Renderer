using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace HtmlRenderer.Demo
{
    public class DemoForm : Form
    {

        private void InitializeComponent()
        {
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSearchPrev = new System.Windows.Forms.Button();
            this.btnSearchNext = new System.Windows.Forms.Button();
            this.htmlPanelEx1 = new HtmlRenderer.HtmlPanelEx();
            this.SuspendLayout();
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(0, 0);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(175, 20);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // btnSearchPrev
            // 
            this.btnSearchPrev.Location = new System.Drawing.Point(175, 0);
            this.btnSearchPrev.Name = "btnSearchPrev";
            this.btnSearchPrev.Size = new System.Drawing.Size(32, 23);
            this.btnSearchPrev.TabIndex = 2;
            this.btnSearchPrev.Text = "<<";
            this.btnSearchPrev.Click += new System.EventHandler(this.btnSearchPrev_Click);
            // 
            // btnSearchNext
            // 
            this.btnSearchNext.Location = new System.Drawing.Point(206, 0);
            this.btnSearchNext.Name = "btnSearchNext";
            this.btnSearchNext.Size = new System.Drawing.Size(32, 23);
            this.btnSearchNext.TabIndex = 2;
            this.btnSearchNext.Text = ">>";
            this.btnSearchNext.Click += new System.EventHandler(this.btnSearchNext_Click);
            // 
            // htmlPanelEx1
            // 
            this.htmlPanelEx1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.htmlPanelEx1.Location = new System.Drawing.Point(0, 21);
            this.htmlPanelEx1.Name = "htmlPanelEx1";
            this.htmlPanelEx1.Size = new System.Drawing.Size(238, 274);
            this.htmlPanelEx1.TabIndex = 3;
            this.htmlPanelEx1.LinkClicked += new System.EventHandler(this.htmlPanel1_LinkClicked);
            // 
            // DemoForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(238, 295);
            this.Controls.Add(this.htmlPanelEx1);
            this.Controls.Add(this.btnSearchNext);
            this.Controls.Add(this.btnSearchPrev);
            this.Controls.Add(this.txtSearch);
            this.Name = "DemoForm";
            this.ResumeLayout(false);
        }

        private const String TEST_HTML =
            "<!DOCTYPE html PUBLIC \"-//IETF//DTD HTML 2.0//EN\">" +
            "<html>" +
            "<head>" +
            "<title>Hello World Demonstration Document</title>" +
            "</head>" +
            "<body>" +
            "<h1>Hello, World!</h1>" +
            "<p>" +
            "This is a minimal \"hello world\" HTML document. It demonstrates the" +
            "basic structure of an HTML file and anchors." +
            "</p>" +
            "<p>" +
            "For more information, see the HTML Station at: <a href= " +
            "\"http://www.december.com/html/\">http://www.december.com/html/</a>" +
            "</p>" +
            "<hr>" +
            "<address>" +
            "&copy; <a href=\"http://www.december.com/john/\">John December</a> (<a" +
            " href=\"mailto:john@december.com\">john@december.com</a>) / 2001-04-06" +
            "</address>" +
            "</body>" +
            "</html>";

        private TextBox txtSearch;
        private Button btnSearchPrev;
        private Button btnSearchNext;

        private String url;
        private int currentRect;
        private HtmlPanelEx htmlPanelEx1;
        private RectangleF[] rects;

        public DemoForm()
        {
            InitializeComponent();

#if PC
            String root = @"c:\projekty\ODPLib\bin\Debug\PopPC\SdCard\predpisy";
#else
            String root = "\\SD Card\\data\\predpisy";
#endif
            try
            {
                htmlPanelEx1.BaseStylesheet = Path.Combine(root, "CommonStyleSheet.css");
                url = LoadLocalPage(Path.Combine(root, "index.htm"));
            }
            catch (FileNotFoundException)
            {
                htmlPanelEx1.Text = TEST_HTML;
            }
        }

        private String LoadLocalPage(String path)
        {
            if (path.StartsWith("file:\\") ||
                path.StartsWith("file://"))
            {
                path = path.Remove(0, 6);
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            String html = String.Empty;
            using (StreamReader s = new StreamReader(path))
            {
                html = s.ReadToEnd();
            }

            htmlPanelEx1.Text = html;
            return path;
        }

        private void htmlPanel1_LinkClicked(object sender, EventArgs ev)
        {
            HtmlRenderer.Entities.HtmlLinkClickedEventArgs e = ev as HtmlRenderer.Entities.HtmlLinkClickedEventArgs;
            String dir = Path.GetDirectoryName(url);
            String href = e.Link.Replace("/", "\\");
            if (href == null || href.Length <= 0)
            {
                href = "index.htm";
            }
            url = LoadLocalPage(Path.Combine(dir, href));
            new Uri(@"file://" + url);
            e.Handled = true;
        }

        private void htmlPanel1_RenderError(object sender, EventArgs ev)
        {
            HtmlRenderer.Entities.HtmlRenderErrorEventArgs e = ev as HtmlRenderer.Entities.HtmlRenderErrorEventArgs;
            if(e.Exception != null)
            {
                MessageBox.Show(e.Exception.ToString(), String.Format("{0} [{1}]", e.Message, e.Type));
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            rects = htmlPanelEx1.FindPlainText(txtSearch.Text, true);
            currentRect = 0;
        }

        private void btnSearchPrev_Click(object sender, EventArgs e)
        {
            if (rects == null || rects.Length == 0)
            {
                return;
            }

            currentRect--;
            if (currentRect < 0)
            {
                currentRect = rects.Length - 1;
            }
            htmlPanelEx1.ScrollTo(new PointF(.0f, rects[currentRect].Y * -1.0f));
        }

        private void btnSearchNext_Click(object sender, EventArgs e)
        {
            if (rects == null || rects.Length == 0)
            {
                return;
            }

            currentRect++;
            if (currentRect >= rects.Length)
            {
                currentRect = 0;
            }
            htmlPanelEx1.ScrollTo(new PointF(.0f, rects[currentRect].Y * -1.0f));
        }
    }
}
