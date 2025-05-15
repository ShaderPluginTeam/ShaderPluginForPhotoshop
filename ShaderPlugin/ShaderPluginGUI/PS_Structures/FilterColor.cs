using System;
using System.Runtime.InteropServices;

namespace ShaderPlugin.PS_Structures
{
    //Size 4
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 16)]
    public struct FilterColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }
}
