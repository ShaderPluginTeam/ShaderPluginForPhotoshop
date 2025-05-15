using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace TestWithoutPhotoshop
{
    public static class Program
    {
        static Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();

        [STAThread]
        static void Main(string[] Args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            string AssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string StartupPath = AssemblyLocation.Replace("TestWithoutPhotoshop" + Path.DirectorySeparatorChar, "");
            Environment.CurrentDirectory = StartupPath;

            Assembly ResourceAssembly = typeof(ShaderPluginGUI.MainWindow).Assembly;
            if (Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = ResourceAssembly; // Set assembly for Resources}
            }
            else // Already set (can't set directly without exception)
            {
                FieldInfo field = typeof(Application).GetField("_resourceAssembly", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    field.SetValue(null, ResourceAssembly);
                }

                field = typeof(BaseUriHelper).GetField("_resourceAssembly", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    field.SetValue(null, ResourceAssembly);
                }
            }

            // Run Main function like from Photoshop
            string TestImagePath = Path.Combine(AssemblyLocation, @"..\..\..\..\TestImages\LennaTest.png");
            BitmapImage BmpImage = new BitmapImage(new Uri(TestImagePath));
            ShaderPluginGUI.Program.DebugImage = new WriteableBitmap(BmpImage);
            ShaderPluginGUI.Program.Main(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string[] AssemblyNameParts = args.Name.Split(',');
            if (AssemblyNameParts.Length <= 0)
            {
                return null;
            }

            string Name = AssemblyNameParts[0];
            if (Name.Contains(".resources"))
            {
                return null;
            }

            if (Name.Contains(".XmlSerializers"))
            {
                return typeof(object).Assembly; // Dummy return avoids exception
            }

            string AssemblyPath = Path.Combine(Environment.CurrentDirectory, Name + ".dll");
            if (LoadedAssemblies.ContainsKey(AssemblyPath))
            {
                return LoadedAssemblies[AssemblyPath];
            }
            else if (File.Exists(AssemblyPath))
            {
                Assembly LoadedAssembly = Assembly.LoadFrom(AssemblyPath);
                LoadedAssemblies.Add(AssemblyPath, LoadedAssembly);
                return LoadedAssembly;
            }
            else
            {
                return null;
            }
        }
    }
}
