using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using ShaderPluginGUI.Commands;

namespace ShaderPluginGUI
{
    public partial class DarkWindow : Window
    {
        #region DependencyProperty
        public static readonly DependencyProperty TitleBarBackgroundProperty = DependencyProperty.Register(
            nameof(TitleBarBackground), typeof(Brush),
            typeof(DarkWindow), new PropertyMetadata(Brushes.DarkGray));

        public static readonly DependencyProperty TitleBarForegroundProperty = DependencyProperty.Register(
            nameof(TitleBarForeground), typeof(Brush),
            typeof(DarkWindow), new PropertyMetadata(Brushes.WhiteSmoke));

        public static readonly DependencyProperty TitleBarFontSizeProperty = DependencyProperty.Register(
            nameof(TitleBarFontSize), typeof(double),
            typeof(DarkWindow), new PropertyMetadata(12.0));
        #endregion DependencyProperty

        #region Properties
        [Category("Appearance")]
        [Localizability(LocalizationCategory.None)]
        public Brush TitleBarBackground
        {
            get { return (Brush)GetValue(TitleBarBackgroundProperty); }
            set { SetValue(TitleBarBackgroundProperty, value); }
        }

        [Category("Appearance")]
        [Localizability(LocalizationCategory.None)]
        public Brush TitleBarForeground
        {
            get { return (Brush)GetValue(TitleBarForegroundProperty); }
            set { SetValue(TitleBarForegroundProperty, value); }
        }

        [Bindable(true)]
        [Category("Appearance")]
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double TitleBarFontSize
        {
            get { return (double)GetValue(TitleBarFontSizeProperty); }
            set { SetValue(TitleBarFontSizeProperty, value); }
        }
        #endregion Properties

        public bool NeedSetWindowChrome = true;

        public DarkWindow() : base()
        {
            SourceInitialized += Window_SourceInitialized;
            StateChanged += Window_StateChanged;

            if (Style == null)
            {
                ResourceDictionary resourceDict = new ResourceDictionary();
                resourceDict.Source = new Uri("/Styles/WindowStyle.xaml", UriKind.RelativeOrAbsolute);
                Style = resourceDict["Window.Dark"] as Style;
            }

            // Set the minimize command
            CommandBindings.Add(new CommandBinding(WindowCommands.MinimizeWindow, new ExecutedRoutedEventHandler(delegate (object sender, ExecutedRoutedEventArgs e)
            {
                WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
            }), new CanExecuteRoutedEventHandler(delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = ResizeMode != ResizeMode.NoResize;
            })));

            // Set the normalize command
            CommandBindings.Add(new CommandBinding(WindowCommands.NormalizeWindow, new ExecutedRoutedEventHandler(delegate (object sender, ExecutedRoutedEventArgs e)
            {
                WindowState = WindowState.Normal;
            }), new CanExecuteRoutedEventHandler(delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = ResizeMode != ResizeMode.NoResize;
            })));

            // Set the maximize command
            CommandBindings.Add(new CommandBinding(WindowCommands.MaximizeWindow, new ExecutedRoutedEventHandler(delegate (object sender, ExecutedRoutedEventArgs e)
            {
                WindowState = WindowState.Maximized;
            }), new CanExecuteRoutedEventHandler(delegate (object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = ResizeMode != ResizeMode.NoResize && ResizeMode != ResizeMode.CanMinimize;
            })));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, new ExecutedRoutedEventHandler(delegate (object sender, ExecutedRoutedEventArgs e)
            {
                Close();
            })));

            CommandBindings.Add(new CommandBinding(WindowCommands.IconMouseDown, OnIconMouseDown));
            CommandBindings.Add(new CommandBinding(WindowCommands.BarMouseDown, OnTitleBarMouseDown));
            CommandBindings.Add(new CommandBinding(WindowCommands.BarMouseUp, OnTitleBarMouseUp));
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Disable Minimize/Maximize buttons
            bool NoResizeMode = ResizeMode == ResizeMode.NoResize || ResizeMode == ResizeMode.CanMinimize;

            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            if (hWnd != IntPtr.Zero)
            {
                HwndSource.FromHwnd(hWnd)?.AddHook(WindowProc);
                if (NoResizeMode)
                {
                    IntPtr hMenu = GetSystemMenu(hWnd, false);
                    uint ButtonsMask = ResizeMode == ResizeMode.NoResize ? SC_MAXIMIZE | SC_MINIMIZE : SC_MAXIMIZE;
                    DeleteMenu(hMenu, ButtonsMask, MF_BYCOMMAND);
                }
            }

            if (NeedSetWindowChrome)
            {
                WindowChrome.SetWindowChrome(this, new WindowChrome
                {
                    GlassFrameThickness = new Thickness(0),
                    ResizeBorderThickness = NoResizeMode ? new Thickness(0) : new Thickness(8),
                    CaptionHeight = 0,
                    CornerRadius = new CornerRadius(0),
                    UseAeroCaptionButtons = false,
                });
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (NeedSetWindowChrome)
            {
                WindowChrome WindowChrome = WindowChrome.GetWindowChrome(this);
                if (WindowChrome != null)
                {
                    bool NoResizeMode = ResizeMode == ResizeMode.NoResize || ResizeMode == ResizeMode.CanMinimize;
                    WindowChrome.ResizeBorderThickness = NoResizeMode || WindowState != WindowState.Normal ? new Thickness(0) : new Thickness(8);
                }
            }
        }

        private void OnIconMouseDown(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e.Parameter as MouseButtonEventArgs).ChangedButton == MouseButton.Left)
            {
                WindowSystemMenu.OpenContextMenu(this);
            }
        }

        private void OnTitleBarMouseDown(object sender, ExecutedRoutedEventArgs e)
        {
            var me = e.Parameter as MouseButtonEventArgs;
            if (me.ChangedButton == MouseButton.Left)
            {
                if (me.ClickCount == 2 && ResizeMode != ResizeMode.NoResize && ResizeMode != ResizeMode.CanMinimize)
                {
                    WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                }
                else if (me.ClickCount == 1 && me.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
        }

        private void OnTitleBarMouseUp(object sender, ExecutedRoutedEventArgs e)
        {
            var me = e.Parameter as MouseButtonEventArgs;
            if (me.ChangedButton == MouseButton.Right)
            {
                WindowSystemMenu.OpenContextMenu(this);
            }
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        #region WinAPI
        #region Monitor Info
        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO)); // Need to initialize cbSize!

                if (GetMonitorInfo(monitor, monitorInfo))
                {
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;

                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMinTrackSize.X = (int)MinWidth;
                    mmi.ptMinTrackSize.Y = (int)MinHeight;
                    mmi.ptMaxSize.X = rcWorkArea.Width;
                    mmi.ptMaxSize.Y = rcWorkArea.Height;
                }
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            // Перед использованием обязательно: monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public int cbSize = 0;
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT(); // Рабочая область монитора (за вычетом панели задач)
            public int dwFlags = 0;
        }

        public const int WM_GETMINMAXINFO = 0x0024;
        public const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
        #endregion

        #region SystemMenu
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        // Enable/Disable Menu Item
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        // Remove Menu Item (if needed)
        [DllImport("user32.dll")]
        static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DrawMenuBar(IntPtr hWnd);

        const uint SC_SIZE = 0xF000;
        const uint SC_MOVE = 0xF010;
        const uint SC_MINIMIZE = 0xF020;
        const uint SC_MAXIMIZE = 0xF030;
        const uint SC_CLOSE = 0xF060;
        const uint SC_RESTORE = 0xF120;

        const uint MF_BYCOMMAND = 0x00000000;
        #endregion
        #endregion WinAPI
    }
}
