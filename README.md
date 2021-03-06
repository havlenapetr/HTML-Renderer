HTML Renderer
=============

Originally hosted on [CodePlex](https://htmlrenderer.codeplex.com/)

The rich formating power of HTML on your WinForms applications without **WebBrowser** control or **MSHTML**.

The library is **100% managed code** without any external dependencies, the only requirement is **.NET 2.0 or higher**.

![Demo](http://download-codeplex.sec.s-msft.com/Download?ProjectName=HtmlRenderer&DownloadId=636137)

**Features**
 1. 100% managed code.
 3. No external dependencies.
 3. Single and small dll (250K).
 4. Supports .NET and .NETCF 2.0 and 3.0, 3.5 or higher.
 
**What it is not**
 * 

**NuGet package install**
 1. PM> Install-Package HtmlRenderer.WinForms
 2. Or from NuGet UI

**Manual install**
 1. Download the binaries.
 2. Reference the proper .NET release (2.0, 3.0, 3.5, 4.0, 4.5) in your project.

**Usage**
 1. Add HtmlRenderer to Visual Studio Toolbox (drag-drop the dll on it).
 2. Drag-n-drop HtmlPanel, HtmlLabel or HtmlToolTip from the Toolbox.
 3. Set the *Text* property with your html.
