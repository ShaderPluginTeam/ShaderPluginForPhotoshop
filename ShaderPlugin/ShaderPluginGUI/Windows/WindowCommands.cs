using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShaderPluginGUI.Commands
{
    static public class WindowCommands
    {
        #region Routed Commands
        /// <summary>
        /// Command used to maximize the window
        /// </summary>
        public readonly static RoutedUICommand MaximizeWindow = new RoutedUICommand("Maximize a window.", nameof(MaximizeWindow), typeof(Window));

        /// <summary>
        /// Command used to minimize the window
        /// </summary>
        public readonly static RoutedUICommand MinimizeWindow = new RoutedUICommand("Minimize a window.", nameof(MinimizeWindow), typeof(Window));

        /// <summary>
        /// Command used to normalize the window
        /// </summary>
        public readonly static RoutedUICommand NormalizeWindow = new RoutedUICommand("Set a window to normal.", nameof(NormalizeWindow), typeof(Window));

        /// <summary>
        /// Command used to handle click on application bar icon
        /// </summary>
        public readonly static RoutedUICommand IconMouseDown = new RoutedUICommand("When Icon was clicked.", nameof(IconMouseDown), typeof(DockPanel));

        public readonly static RoutedUICommand BarMouseUp = new RoutedUICommand("Mouse Up on Title Bar.", nameof(BarMouseUp), typeof(DockPanel));
        public readonly static RoutedUICommand BarMouseDown = new RoutedUICommand("Mouse Down on Title Bar.", nameof(BarMouseDown), typeof(DockPanel)); 
        #endregion
    }
}
