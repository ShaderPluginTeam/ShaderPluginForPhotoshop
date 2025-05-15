using System;
using System.Runtime.InteropServices;

namespace ShaderPlugin.PS_Structures
{
    //Size 6
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 8)]
    public struct PSRGBColor
    {
        public ushort red;
        public ushort green;
        public ushort blue;
    }
}
