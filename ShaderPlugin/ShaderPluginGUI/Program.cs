using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using ShaderPlugin.PS_Structures;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ShaderPluginGUI
{
    public static class Program
    {
        public static PSPluginErrorCodes Result = PSPluginErrorCodes.UserCanceledError; // Canceled by User
        public static string StartupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ShaderPlugin for Photoshop");
        public static string ShadersFolderPath = Path.Combine(StartupPath, "Shaders");
        public static FilterRecordM filterRecord;
        public static IntPtr PhotoshopWindowPointer;
        public static IntPtr LastParamsPtr;

        // RunWithoutPhotoshop Debug Image
        public static WriteableBitmap DebugImage = null;

        public static short Main(IntPtr PhotoshopWindowHandle, IntPtr FilterRecordPtr, IntPtr LastParamsPointer)
        {
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = typeof(MainWindow).Assembly; // Set assembly for Resources}
            }

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; //For fix parsing values like "0.5" and "0,5"

            #region Unhandled Exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                ThreadExceptionDialog ExceptionDialog = new ThreadExceptionDialog((Exception)e.ExceptionObject);
                ExceptionDialog.Scale(new SizeF(1.5f, 1.5f));
                ExceptionDialog.ShowDialog();
            };

            Dispatcher.CurrentDispatcher.UnhandledException += (sender, e) =>
            {
                ThreadExceptionDialog ExceptionDialog = new ThreadExceptionDialog(e.Exception);
                ExceptionDialog.Scale(new SizeF(1.5f, 1.5f));
                ExceptionDialog.ShowDialog();
                e.Handled = true; // Prevents close all (app can fully crash Photoshop)
            };
            #endregion Unhandled Exceptions

            Result = PSPluginErrorCodes.UserCanceledError;

            if (!Directory.Exists(StartupPath))
            {
                try
                {
                    Directory.CreateDirectory(StartupPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show("Try to run Photoshop with Admin rights.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    return (int)PSPluginErrorCodes.WriteError;
                }
            }

            if (!Directory.Exists(ShadersFolderPath))
            {
                ShadersFolderPath = StartupPath;
            }

            try
            {
                PhotoshopWindowPointer = PhotoshopWindowHandle;
                filterRecord = FilterRecordPtr != IntPtr.Zero ? FilterRecordM.Load(FilterRecordPtr) : null;
                LastParamsPtr = LastParamsPointer;

                MainWindow MainWindow = new MainWindow();
                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(MainWindow)
                {
                    Owner = PhotoshopWindowHandle
                };
                MainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ThreadExceptionDialog ExceptionDialog = new ThreadExceptionDialog(ex);
                ExceptionDialog.Scale(new SizeF(1.5f, 1.5f));
                ExceptionDialog.ShowDialog();
            }

            return (short)Result;
        }

        /// <summary>
        /// Need apply plugin with last params or just open plugin window.
        /// </summary>
        public static bool PhotoshopRunLastFilterEnabled
        {
            get
            {
                if (LastParamsPtr != IntPtr.Zero)
                {
                    byte[] Bytes = new byte[1]; // Must be same as at C++ part
                    Marshal.Copy(LastParamsPtr, Bytes, 0, Bytes.Length);
                    return (Bytes[0] != 0); // First byte - "Last Filter"
                }
                return false;
            }
            set
            {
                if (LastParamsPtr != IntPtr.Zero)
                {
                    Marshal.Copy(new byte[] { (byte)(value ? 1 : 0) }, 0, LastParamsPtr, 1);
                }
            }
        }
    }
}
