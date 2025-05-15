using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TestInPhotoshop
{
    internal class Program
    {
        static string[] FilesToCopy = new string[]
        {
            "CLRProxy.dll",
            "ICSharpCode.AvalonEdit.dll",
            "OpenTK.GLControl.dll",
            "OpenTK.dll",
            "ShaderPlugin.8bf",
            "ShaderPlugin.pdb",
            "ShaderPluginGUI.dll",
        };

        static void Main(string[] args)
        {
            TryToKillPhotoshopProcesses();

            Task.Delay(500).Wait();

            string CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo SolutionDirectoryInfo = TryGetSolutionDirectoryInfo(CurrentDirectory);
            string BuildDir = Path.Combine(SolutionDirectoryInfo.FullName, "bin", Environment.Is64BitOperatingSystem ? "x64" : "x86");

#if DEBUG
            BuildDir = Path.Combine(BuildDir, "Debug");
#else
            BuildDir = Path.Combine(BuildDir, "Release");
#endif

            string[] PluginsFolders = GetPluginsFolders();
            foreach (string PluginsFolder in PluginsFolders)
            {
                string ShaderPluginFolder = Path.Combine(PluginsFolder, "ShaderPlugin");
                Console.WriteLine("Copy from: \"{0}\" to: \"{1}\".", BuildDir, ShaderPluginFolder);
                Console.WriteLine();

                if (!Directory.Exists(ShaderPluginFolder))
                {
                    Directory.CreateDirectory(ShaderPluginFolder);
                }

                foreach (string CurrentFile in FilesToCopy)
                {
                    string SrcFile = Path.Combine(BuildDir, CurrentFile);

                    if (File.Exists(SrcFile))
                    {
                        string DstFile = Path.Combine(ShaderPluginFolder, CurrentFile);
                        File.Copy(SrcFile, DstFile, true);
                        Console.WriteLine("Copied: {0}", CurrentFile);
                    }
                    else
                    {
                        Console.WriteLine("File not exist: {0}", CurrentFile);
                    }
                }

                Console.WriteLine();

                string PhotoshopPath = Path.Combine(new DirectoryInfo(PluginsFolder).Parent.FullName, "Photoshop.exe");
                if (File.Exists(PhotoshopPath))
                {
                    // Console.WriteLine("Press Enter for Run Adobe Photoshop:");
                    // Console.ReadKey();

                    string TestFile = Path.Combine(SolutionDirectoryInfo.FullName, "TestImages\\LennaTest.png");
                    ProcessStartInfo StartInfo = new ProcessStartInfo(PhotoshopPath, TestFile);
                    StartInfo.WorkingDirectory = Path.GetDirectoryName(TestFile);
                    Process.Start(StartInfo);
                }
                else
                {
                    Console.WriteLine("Can't find Photoshop executable!");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
        }

        static void TryToKillPhotoshopProcesses()
        {
            Process[] ActivePSProcesses = Process.GetProcesses().Where(P => P.ProcessName == "Photoshop").ToArray();
            foreach (Process PhotoshopProcess in ActivePSProcesses)
            {
                try
                {
                    PhotoshopProcess.Kill();
                }
                catch { }
            }
        }

        static string[] GetPluginsFolders()
        {
            List<string> PluginsFolders = new List<string>();
            RegistryView CurrentRegistryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            RegistryKey LocalMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, CurrentRegistryView);

            RegistryKey PhotoshopRecords = LocalMachine.OpenSubKey("SOFTWARE\\Adobe\\Photoshop");
            if (PhotoshopRecords != null)
            {
                string[] PhotoshopVersions = PhotoshopRecords.GetSubKeyNames();
                foreach (string PhotoshopVersion in PhotoshopVersions)
                {
                    RegistryKey PluginsRecord = PhotoshopRecords.OpenSubKey(PhotoshopVersion + "\\PluginPath");
                    if (PluginsRecord != null)
                    {
                        string PluginsPath = (string)PluginsRecord.GetValue(string.Empty);
                        if (Directory.Exists(PluginsPath))
                        {
                            PluginsFolders.Add(PluginsPath);
                        }
                    }
                }
            }

            return PluginsFolders.ToArray();
        }

        public static DirectoryInfo TryGetSolutionDirectoryInfo(string CurrentPath = null)
        {
            DirectoryInfo directory = new DirectoryInfo(CurrentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }

            return directory;
        }
    }
}
