using System;
using System.Runtime.InteropServices;

namespace ShaderPlugin.PS_Structures
{
    //Size 8
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 16)]
    public struct PSRect
    {
        public short top;
        public short left;
        public short bottom;
        public short right;
    }
}
