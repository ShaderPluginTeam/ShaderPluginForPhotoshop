using System;
using System.Runtime.InteropServices;

namespace ShaderPlugin.PS_Structures
{
    //Size 8
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VPoint
    {
        public int V;
        public int H;
    }
}
