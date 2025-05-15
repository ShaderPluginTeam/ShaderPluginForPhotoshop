using System;
using System.Reflection;
using System.Text;
using System.Windows.Input;

using AvalonEditExtensions;
using ICSharpCode.AvalonEdit.Search;

namespace ShaderPluginGUI
{
    public partial class SystemInfoWindow : DarkWindow
    {
        public SystemInfoWindow(ShaderPluginEngine Engine) : base()
        {
            InitializeComponent();

            // Install AvalonEdit Extensions
            SearchPanel.Install(textEditorInfo.TextArea);
            GoToLineWindow.Install(textEditorInfo, this);

            textEditorInfo.Text = GetSystemInfo(Engine);
        }

        private string GetSystemInfo(ShaderPluginEngine Engine)
        {
            const int LineLen = 78;
            const char LineChar = '═';
            string TopStr = "╔══╗".Insert(2, new string(LineChar, LineLen - 4));
            string CtrStr = "╠══╣".Insert(2, new string(LineChar, LineLen - 4));
            string BtmStr = "╚══╝".Insert(2, new string(LineChar, LineLen - 4));

            string CreateHeaderStr(string Text) => $"║ {Text.PadLeft((LineLen + Text.Length) / 2 - 2).PadRight(LineLen - 4)} ║";
            string CreateContentStr(string Text) => $"║ {Text.PadRight(LineLen - 4)} ║";

            Version PluginVersion = Assembly.GetExecutingAssembly().GetName().Version;

            StringBuilder SB = new StringBuilder();
            SB.AppendLine(TopStr);
            SB.AppendLine(CreateContentStr(String.Empty));
            SB.AppendLine(CreateHeaderStr($"Shader Plugin (for Photoshop) v.{PluginVersion.Major}.{PluginVersion.Minor}"));
            SB.AppendLine(CreateContentStr(String.Empty));
            SB.Append(BtmStr);

            if (Engine != null)
            {
                SB.AppendLine();
                SB.AppendLine();
                SB.AppendLine(TopStr);
                SB.AppendLine(CreateHeaderStr("System Info:"));
                SB.AppendLine(CtrStr);
                SB.AppendLine(CreateContentStr("GPU Vendor: " + Engine.GPUVendor));
                SB.AppendLine(CreateContentStr("GPU Renderer: " + Engine.GPURenderer));
                SB.AppendLine(CreateContentStr("OpenGL Version: " + Engine.OpenGLVersion));
                SB.AppendLine(CreateContentStr("Shading Language Version: " + Engine.ShadingLanguageVersion));
                SB.AppendLine(BtmStr);

                SB.AppendLine();
                SB.AppendLine(TopStr);
                SB.AppendLine(CreateHeaderStr("Supported OpenGL Extensions:"));
                SB.AppendLine(CtrStr);
                foreach (var GLExtension in GLExtensions.Extensions)
                {
                    SB.AppendLine(CreateContentStr(GLExtension));
                }
                SB.Append(BtmStr);
            }

            return SB.ToString();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
