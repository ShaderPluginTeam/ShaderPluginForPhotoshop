using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ShaderPluginGUI
{
    static class WindowSystemMenu
    {
//#if WINDOWS
        const int WM_SYSCOMMAND = 0x112;
        const uint TPM_LEFTALIGN = 0x0000;
        const uint TPM_RETURNCMD = 0x0100;
        const UInt32 MF_ENABLED = 0x00000000;
        const UInt32 MF_GRAYED = 0x00000001;
        internal const UInt32 SC_MAXIMIZE = 0xF030;
        internal const UInt32 SC_RESTORE = 0xF120;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
//#endif

        public static void OpenContextMenu(Window window)
        {
//#if WINDOWS
            WindowInteropHelper helper = new WindowInteropHelper(window);
            IntPtr callingWindow = helper.Handle;
            IntPtr wMenu = GetSystemMenu(callingWindow, false);
            
            // Display the menu
            if (window.WindowState == WindowState.Maximized)
            {
                EnableMenuItem(wMenu, SC_MAXIMIZE, MF_GRAYED);
                EnableMenuItem(wMenu, SC_RESTORE, MF_ENABLED);
            }
            else
            {
                EnableMenuItem(wMenu, SC_MAXIMIZE, MF_ENABLED);
                EnableMenuItem(wMenu, SC_RESTORE, MF_GRAYED);
            }

            var point =  window.PointToScreen(Mouse.GetPosition(window));
            int command = TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD, (int)point.X, (int)point.Y, callingWindow, IntPtr.Zero);
            if (command == 0)
            {
                return;
            }

            PostMessage(callingWindow, WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
//#endif
        }
    }
}
