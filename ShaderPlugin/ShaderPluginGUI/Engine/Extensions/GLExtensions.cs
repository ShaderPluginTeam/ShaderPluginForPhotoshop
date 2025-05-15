using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace ShaderPluginGUI
{
    // Supported OpenGL Extensions
    public static class GLExtensions
    {
        public static HashSet<string> Extensions = new HashSet<string>();

        // Cache all Supported OpenGL Extensions
        public static void Init()
        {
            Extensions.Clear();

            int NumExtensions = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < NumExtensions; i++)
            {
                string Extension = GL.GetString(StringNameIndexed.Extensions, i);
                Extensions.Add(Extension);
            }
        }

        // Check is Extension supported
        public static bool IsSupported(string Extension)
        {
            if (Extensions.Count == 0)
            {
                Init();
            }

            return Extensions.Contains(Extension);
        }

        // Return all supported extension
        public static string[] GetExtensions()
        {
            if (Extensions.Count == 0)
            {
                Init();
            }

            return Extensions.ToArray();
        }
    }
}
