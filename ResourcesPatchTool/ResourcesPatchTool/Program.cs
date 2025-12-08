using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DiffMatchPatch;

namespace ResourcesPatchTool
{
    internal class Program
    {
        static diff_match_patch DMP = new diff_match_patch();

        static void Main(string[] args)
        {
            Console.Title = "Resource Patch Tool";

            string CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DirectoryInfo PluginRootDirectoryInfo = TryGetPluginRootDirectoryInfo(CurrentDirectory);
            string PluginRootDirectory = PluginRootDirectoryInfo.FullName;
            if (!Directory.Exists(PluginRootDirectory))
            {
                Console.WriteLine("Can't get plugin root directory.");
                Console.ReadKey();
                return;
            }

            string SDK_Dir = Path.Combine(PluginRootDirectory, "pluginsdk");
            if (!Directory.Exists(SDK_Dir))
            {
                Console.WriteLine("Can't get pluginsdk directory.");
                Console.ReadKey();
                return;
            }

            string ResourceFileFromSDK = Path.Combine(SDK_Dir, @"samplecode\colorpicker\nearestbase\common\NearestBase.r");
            if (!File.Exists(ResourceFileFromSDK))
            {
                Console.WriteLine("Can't find \"NearestBase.r\" file in SDK folder, check SDK.");
                Console.WriteLine($"Path: \"{ResourceFileFromSDK}\".");
                Console.ReadKey();
                return;
            }

            string SDKResourceStr = File.ReadAllText(ResourceFileFromSDK, Encoding.UTF8);

            string ResPatchFile = Path.Combine(PluginRootDirectory, @"ShaderPlugin\ShaderPlugin\Common\ShaderPlugin.r_patch");
            string ResourceFile = Path.Combine(PluginRootDirectory, @"ShaderPlugin\ShaderPlugin\Common\ShaderPlugin.r");

        Menu:
            Console.Clear();
            Console.WriteLine("Resource Patch Tool Menu:");

            bool ResPatchExist = File.Exists(ResPatchFile);
            bool ResourceExist = File.Exists(ResourceFile);

            if (ResPatchExist)
            {
                Console.WriteLine("1 - Generate Resource file.");
            }

            if (ResourceExist)
            {
                Console.WriteLine("2 - Make patch form Resource file.");
            }

            Console.WriteLine("ESC - Exit.");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1: // Apply patch, create Resource file
                    if (!ResPatchExist) goto Menu;

                    Console.Clear();
                    Console.WriteLine("Generate Resource file:");
                    string ResPatchStr = File.ReadAllText(ResPatchFile, Encoding.UTF8).Replace("\r\n", "\n");
                    if (!ApplyPatch(SDKResourceStr, ResPatchStr, out string PatchedText))
                    {
                        Console.WriteLine("Can't apply patch to \"NearestBase.r\"!");
                        Console.ReadKey();
                        return;
                    }

                    if (ResourceExist)
                    {
                        Console.WriteLine($"Override \"ShaderPlugin.r\" (y/n)?");
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.Y:
                            case ConsoleKey.Enter:
                                break;

                            case ConsoleKey.N:
                            case ConsoleKey.Escape:
                                goto Menu;
                        }
                    }

                    File.WriteAllText(ResourceFile, PatchedText);
                    Console.WriteLine("\"ShaderPlugin.r\" successfully created.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;

                case ConsoleKey.D2: // Create patch from existing resource
                    if (!ResourceExist) goto Menu;

                    Console.Clear();
                    Console.WriteLine("Make patch form Resource file:");
                    string ResourceStr = File.ReadAllText(ResourceFile, Encoding.UTF8);
                    string PatchStr = CreatePatch(SDKResourceStr, ResourceStr);

                    if (ResPatchExist)
                    {
                        Console.WriteLine($"Override \"ShaderPlugin.r_patch\" (y/n)?");
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.Y:
                            case ConsoleKey.Enter:
                                break;

                            case ConsoleKey.N:
                            case ConsoleKey.Escape:
                                goto Menu;
                        }
                    }

                    File.WriteAllText(ResPatchFile, PatchStr);
                    Console.WriteLine("\"ShaderPlugin.r_patch\" successfully created.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;

                case ConsoleKey.Escape:
                    return;
            }
        }

        public static DirectoryInfo TryGetPluginRootDirectoryInfo(string CurrentPath = null)
        {
            DirectoryInfo directory = new DirectoryInfo(CurrentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetDirectories("pluginsdk").Any())
            {
                directory = directory.Parent;
            }

            return directory;
        }

        static string CreatePatch(string OriginalText, string NewText)
        {
            DMP.Diff_Timeout = 0.0f;
            List<Diff> Diffs = DMP.diff_main(OriginalText, NewText);

            // Cleanup
            DMP.diff_cleanupSemantic(Diffs);
            DMP.diff_cleanupEfficiency(Diffs);

            List<Patch> Patches = DMP.patch_make(Diffs);

            return DMP.patch_toText(Patches);
        }

        static bool ApplyPatch(string OriginalText, string Patch, out string PatchedText)
        {
            List<Patch> Patches = DMP.patch_fromText(Patch);

            DMP.Patch_DeleteThreshold = 0.5f;
            DMP.Match_Threshold = 0.7f;
            DMP.Match_Distance = 1000;
            object[] Results = DMP.patch_apply(Patches, OriginalText);

            PatchedText = Results[0] as string;
            bool[] PatchResults = Results[1] as bool[];

            for (int i = 0; i < PatchResults.Length; i++)
            {
                if (!PatchResults[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
