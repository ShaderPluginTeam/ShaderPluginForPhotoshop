using System;
using System.Runtime.InteropServices;

namespace ShaderPlugin.PS_Structures
{
    //Size 16
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VRect
    {
        public int Top;
        public int Left;
        public int Bottom;
        public int Right;
    }
}