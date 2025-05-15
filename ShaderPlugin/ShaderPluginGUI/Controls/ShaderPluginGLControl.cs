using OpenTK;
using OpenTK.Graphics;

namespace ShaderPluginGUI.Controls
{
    public class ShaderPluginGLControl : GLControl
    {
        public ShaderPluginGLControl() : base(GraphicsMode.Default, 3, 2, GraphicsContextFlags.Default)
        {
            // Create OpenGL Context Before Init GLControl (OpenTK bug)
            // Fix OpenTK bug when OpenGL context creation not works correctly from first time
            // Mb it's use fallback OpenGL 2.1.2 or context from Adobe Photoshop?
            GLControl DummyGLControl = new GLControl();
            DummyGLControl.MakeCurrent();
            DummyGLControl.Dispose();
            DummyGLControl = null;
        }
    }
}
