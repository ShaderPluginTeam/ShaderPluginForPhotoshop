namespace ShaderPluginGUI
{
    public static class ShaderIDs
    {
        public const int Image = 0;
        public const int BufferA = 1;
        public const int BufferB = 2;
        public const int BufferC = 3;
        public const int BufferD = 4;

        public const int ConvertImageFomat = 5;

        public const int View = 6;
        public const int ViewLine = 7;
        public const int ViewGrid = 8;

        public const int MAX = 9;

        public static string[] Names = new string[]
        {
            "Image",
            "BufferA",
            "BufferB",
            "BufferC",
            "BufferD",
            "ConvertImageFomat",
            "View",
            "ViewLine",
            "ViewGrid"
        };
    }

    public static class TextureIDs
    {
        public const int OriginalImage = 0;
        public const int ProcessedImage = 1;
        public const int BufferA = 2;
        public const int BufferB = 3;
        public const int BufferC = 4;
        public const int BufferD = 5;
        public const int MAX = 6;

        public static int FromTextureUnitID(int TextureUnitID)
        {
            switch (TextureUnitID)
            {
                default:
                case TextureUnitIDs.OriginalImage: return OriginalImage;
                case TextureUnitIDs.BufferA: return BufferA;
                case TextureUnitIDs.BufferB: return BufferB;
                case TextureUnitIDs.BufferC: return BufferC;
                case TextureUnitIDs.BufferD: return BufferD;
            }
        }

        public static int FromFrameBufferID(int FrameBufferID)
        {
            switch (FrameBufferID)
            {
                default:
                case FrameBufferIDs.ProcessedImage: return ProcessedImage;
                case FrameBufferIDs.BufferA: return BufferA;
                case FrameBufferIDs.BufferB: return BufferB;
                case FrameBufferIDs.BufferC: return BufferC;
                case FrameBufferIDs.BufferD: return BufferD;
            }
        }

        public static int FromTabControlMainTabIndex(int TabControlMainTabIndex)
        {
            switch (TabControlMainTabIndex)
            {
                default:
                case TabControlMainTabIndexes.Image: return ProcessedImage;
                case TabControlMainTabIndexes.BufferA: return BufferA;
                case TabControlMainTabIndexes.BufferB: return BufferB;
                case TabControlMainTabIndexes.BufferC: return BufferC;
                case TabControlMainTabIndexes.BufferD: return BufferD;
            }
        }
    }

    public static class TextureUnitIDs
    {
        public const int OriginalImage = 0;
        public const int BufferA = 1;
        public const int BufferB = 2;
        public const int BufferC = 3;
        public const int BufferD = 4;
        public const int MAX = 5;

        public static int FromTextureID(int TextureID)
        {
            switch (TextureID)
            {
                case TextureIDs.OriginalImage: return OriginalImage;
                case TextureIDs.BufferA: return BufferA;
                case TextureIDs.BufferB: return BufferB;
                case TextureIDs.BufferC: return BufferC;
                case TextureIDs.BufferD: return BufferD;
                default: return -1;
            }
        }
    }

    public static class FrameBufferIDs
    {
        public const int ProcessedImage = 0;
        public const int BufferA = 1;
        public const int BufferB = 2;
        public const int BufferC = 3;
        public const int BufferD = 4;
        public const int MAX = 5;

        public static int FromTextureID(int TextureID)
        {
            switch (TextureID)
            {
                case TextureIDs.ProcessedImage: return ProcessedImage;
                case TextureIDs.BufferA: return BufferA;
                case TextureIDs.BufferB: return BufferB;
                case TextureIDs.BufferC: return BufferC;
                case TextureIDs.BufferD: return BufferD;
                default: return -1;
            }
        }

        public static string[] Names = new string[]
        {
            "Processed Image",
            "Buffer A",
            "Buffer B",
            "Buffer C",
            "Buffer D"
        };
    }

    // User Interface
    public static class TabControlMainTabIndexes
    {
        public const int Common = 0;
        public const int BufferA = 1;
        public const int BufferB = 2;
        public const int BufferC = 3;
        public const int BufferD = 4;
        public const int Image = 5;
        public const int Settings = 6;
        public const int MAX = 5;
    }
}