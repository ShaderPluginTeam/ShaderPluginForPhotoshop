using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace ShaderPluginGUI
{
    public partial class AboutWindow : DarkWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            Version pluginVersion = Assembly.GetExecutingAssembly().GetName().Version;
            runVersion.Text = pluginVersion.Major + "." + pluginVersion.Minor;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
