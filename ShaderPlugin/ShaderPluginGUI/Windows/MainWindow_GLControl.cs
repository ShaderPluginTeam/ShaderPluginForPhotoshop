using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace ShaderPluginGUI
{
    public partial class MainWindow
    {
        ShaderPluginEngine Engine;
        RenderLoop RenderLoop;

        int Zoom_Index = 0;
        List<float> Zoom_List = new List<float>();
        Point Mouse_LocationOld = new Point();
        PointF Zoom_CenterPoint = new PointF();
        float PreviewPosition = 0.5f;

        // Prevent KeyPress repear
        private HashSet<Keys> PressedKeys = new HashSet<Keys>();

        private void GlControl_Load(object sender, EventArgs e)
        {
            glControl.MakeCurrent();

            // Init Engine
            Engine = new ShaderPluginEngine();

            // Init RenderLoop
            RenderLoop = new RenderLoop(Dispatcher, 60.0, false);
            RenderLoop.Render = (TickEventArgs) =>
            {
                Dispatcher?.Invoke((RenderLoopTickDelegate)RenderLoop_Render, DispatcherPriority.Send, TickEventArgs);
            };
        }

        private void GlControl_HandleDestroyed(object sender, EventArgs e)
        {
            RenderLoop?.Stop();
            RenderLoop = null;

            SaveLastShaders();
            Engine.Free(); // Free all: Shaders, Textures...
            Engine = null;
        }

        private void GlControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (Engine == null) return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    UpdateMouseCoordsUniform(e.Location, MouseUniformState.Pressed);

                    if (toggleButton_PixelInfoPopup.IsChecked == true)
                    {
                        pixelInfoPopup.MouseLocation = e.Location;
                        UpdatePixelInfo();
                        pixelInfoPopup.IsOpen = true;
                        glControl.Cursor = Cursors.Cross;
                    }
                    break;

                case MouseButtons.Middle:
                    glControl.Cursor = Cursors.Hand;
                    PreviewPosition = e.Location.X / (float)glControl.ClientSize.Width;
                    glControl.Invalidate();
                    break;

                case MouseButtons.Right:
                    Mouse_LocationOld = e.Location;
                    glControl.Cursor = Cursors.SizeAll;
                    break;

                case MouseButtons.XButton1:
                    ZoomUpdate(false);
                    break;

                case MouseButtons.XButton2:
                    for (int i = 0; i < ZoomList.Count; i++)
                    {
                        if (ZoomList[i] == 1.0f)
                        {
                            ZoomListIndex = i;
                            ZoomUpdate(true);
                        }
                    }
                    break;
            }
        }

        private void GlControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (Engine == null) return;

            if (e.Button == MouseButtons.Left)
            {
                UpdateMouseCoordsUniform(e.Location, MouseUniformState.Released);
            }

            if (pixelInfoPopup.IsOpen)
            {
                pixelInfoPopup.IsOpen = false;
            }

            glControl.Cursor = Cursors.Default;
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (Engine == null) return;

            UpdateMouseCoordsUniform(e.Location, e.Button == MouseButtons.Left ? MouseUniformState.Move | MouseUniformState.Pressed : MouseUniformState.Move);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (pixelInfoPopup.IsOpen)
                    {
                        pixelInfoPopup.MouseLocation = e.Location;
                        UpdatePixelInfo();
                        pixelInfoPopup.HorizontalOffset = e.X + 14;
                        pixelInfoPopup.VerticalOffset = e.Y + 24;
                    }
                    break;

                case MouseButtons.Middle:
                    glControl.Cursor = Cursors.Hand;
                    PreviewPosition = e.Location.X / (float)glControl.ClientSize.Width;
                    glControl.Invalidate();
                    break;

                case MouseButtons.Right:
                    glControl.Cursor = Cursors.SizeAll;
                    MoveImage(e.Location);
                    break;
            }
        }

        private void GlControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0 && Zoom_Index < Zoom_List.Count - 1)
            {
                Zoom_Index++;
            }
            else if (e.Delta < 0 && Zoom_Index > 0)
            {
                Zoom_Index--;
            }

            MoveImage(Mouse_LocationOld);
        }

        private void GlControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    e.IsInputKey = true;
                    break;
            }
        }

        private void GlControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Engine == null) return;

            if (!PressedKeys.Contains(e.KeyCode))
            {
                PressedKeys.Add(e.KeyCode);
                Engine.SetKeyboardKeyState((int)e.KeyCode, true);
            }
        }

        private void GlControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (Engine == null) return;

            PressedKeys.Remove(e.KeyCode);
            Engine.SetKeyboardKeyState((int)e.KeyCode, false);
        }

        private void GlControl_Resize(object sender, EventArgs e)
        {
            if (Engine == null) return;

            if (glControl.ClientSize.Width <= 0 || glControl.ClientSize.Height <= 0)
            {
                return;
            }

            glControl.MakeCurrent();

            GL.Viewport(0, 0, glControl.ClientSize.Width, glControl.ClientSize.Height);
            Engine.MVPMatrix = Matrix4.CreateOrthographic(glControl.ClientSize.Width, glControl.ClientSize.Height, -1f, 1f); // MV - Identity
            ZoomUpdate(true);

            InvalidateVisual();
        }

        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            if (Engine == null) return;

            float Zoom = GetZoom;
            float X1 = -Zoom_CenterPoint.X * Zoom;
            float X2 = X1 + Engine.TextureWidth * Zoom;
            float Y2 = Zoom_CenterPoint.Y * Zoom;
            float Y1 = Y2 - Engine.TextureHeight * Zoom;

            glControl.MakeCurrent();
            Engine.DrawViewport(X1, X2, Y1, Y2, PreviewPosition);
            glControl.SwapBuffers();
        }

        void RenderLoop_Render(RenderLoopTickArgs TickEventArgs)
        {
            if (RenderLoop == null || Engine == null)
            {
                return; // User can close window, but Dispatcher will call this function anyway
            }

            try
            {
                Engine.TickData = TickEventArgs;
                textBlockRenderStats.Text = $"Time: {TickEventArgs.RunningTime:0.00}   FPS: {TickEventArgs.AverageFPS:0.0}";

                if (ShaderState != null)
                {
                    Engine.DrawFrameBuffers(ShaderState);
                    glControl.Invalidate();
                }

                if (pixelInfoPopup != null && pixelInfoPopup.IsOpen)
                {
                    UpdatePixelInfo();
                }
            }
            catch (Exception ex)
            {
                RenderLoop?.Stop();
                toggleButton_RenderLoop_Play.IsChecked = false;
                throw ex;
            }
        }

        bool SaveLastShaders()
        {
            return ShaderStateXML.Save(MakeShaderStateforSaving(), Path.Combine(Program.StartupPath, ShaderStateXML.LastShaderFile));
        }

        private void MoveImage(Point NewMouseCoords)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            #region Zoom_CenterPoint
            Zoom_CenterPoint = new PointF(
                Zoom_CenterPoint.X - (NewMouseCoords.X - Mouse_LocationOld.X) / GetZoom,
                Zoom_CenterPoint.Y - (NewMouseCoords.Y - Mouse_LocationOld.Y) / GetZoom);
            Mouse_LocationOld = NewMouseCoords;

            float WidthZ = glControl.ClientSize.Width / GetZoom;
            float HeightZ = glControl.ClientSize.Height / GetZoom;
            float Half_WidthZ = WidthZ * 0.5f;
            float Half_HeightZ = HeightZ * 0.5f;

            if (Engine.TextureWidth > WidthZ)
            {
                if (Zoom_CenterPoint.X - Half_WidthZ < 0f)
                {
                    Zoom_CenterPoint.X = Half_WidthZ;
                }

                if (Zoom_CenterPoint.X + Half_WidthZ > Engine.TextureWidth)
                {
                    Zoom_CenterPoint.X = Engine.TextureWidth - Half_WidthZ;
                }
            }
            else
            {
                Zoom_CenterPoint.X = Engine.TextureWidth * 0.5f;
            }

            if (Engine.TextureHeight > HeightZ)
            {
                if (Zoom_CenterPoint.Y - Half_HeightZ < 0f)
                {
                    Zoom_CenterPoint.Y = Half_HeightZ;
                }

                if (Zoom_CenterPoint.Y + Half_HeightZ > Engine.TextureHeight)
                {
                    Zoom_CenterPoint.Y = Engine.TextureHeight - Half_HeightZ;
                }
            }
            else
            {
                Zoom_CenterPoint.Y = Engine.TextureHeight * 0.5f;
            }
            #endregion

            glControl.Invalidate();
            InvalidateVisual();
        }

        private void UpdateMouseCoordsUniform(Point MouseLocation, MouseUniformState MouseState)
        {
            if (Engine == null)
            {
                return;
            }

            float Zoom = GetZoom;
            float X1 = -Zoom_CenterPoint.X * Zoom;
            float X2 = X1 + Engine.TextureWidth * Zoom;
            float Y2 = Zoom_CenterPoint.Y * Zoom;
            float Y1 = Y2 - Engine.TextureHeight * Zoom;

            float PosX = MouseLocation.X - glControl.ClientRectangle.Width * 0.5f;
            float PosY = glControl.ClientRectangle.Height * 0.5f - MouseLocation.Y;

            if (X1 != X2 && Y1 != Y2) // UV * glControl.ClientRectangle.Size
            {
                float X = (PosX - X1) / (X2 - X1) * glControl.ClientRectangle.Width;
                float Y = (PosY - Y1) / (Y2 - Y1) * glControl.ClientRectangle.Height;

                switch (MouseState)
                {
                    case MouseUniformState.Pressed:
                        Engine.MouseState.X = X;
                        Engine.MouseState.Y = Y;
                        Engine.MouseState.Z = X;
                        Engine.MouseState.W = Y;
                        break;

                    case MouseUniformState.Released:
                        Engine.MouseState.X = X;
                        Engine.MouseState.Y = Y;
                        Engine.MouseState.Z = -Engine.MouseState.Z;

                        if (Engine.MouseState.W >= 0.0f)
                        {
                            Engine.MouseState.W = -Engine.MouseState.W;
                        }
                        break;

                    case MouseUniformState.Move | MouseUniformState.Pressed:
                        Engine.MouseState.X = X;
                        Engine.MouseState.Y = Y;
                        Engine.MouseCoords.X = X;
                        Engine.MouseCoords.Y = Y;

                        if (Engine.MouseState.W >= 0.0f)
                        {
                            Engine.MouseState.W = -Engine.MouseState.W;
                        }
                        break;

                    case MouseUniformState.Move:
                        Engine.MouseCoords.X = X;
                        Engine.MouseCoords.Y = Y;
                        break;
                }
            }
        }

        private void UpdatePixelInfo()
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            float Zoom = GetZoom;
            float X1 = -Zoom_CenterPoint.X * Zoom;
            float X2 = X1 + Engine.TextureWidth * Zoom;
            float Y2 = Zoom_CenterPoint.Y * Zoom;
            float Y1 = Y2 - Engine.TextureHeight * Zoom;

            Point MouseLocation = pixelInfoPopup.MouseLocation;
            float PosX = MouseLocation.X - glControl.ClientRectangle.Width * 0.5f;
            float PosY = glControl.ClientRectangle.Height * 0.5f - MouseLocation.Y;

            if (PosX < X1 || PosX > X2 || PosY < Y1 || PosY > Y2)
            {
                pixelInfoPopup.PixelColor = ColorRGBA.Empty;
                pixelInfoPopup.PixelUV = Vector2.Zero;
                pixelInfoPopup.PixelPosition = Vector2.Zero;
                return;
            }
            float x = MathHelper.Clamp((PosX - X1) / (X2 - X1), 0.0f, 1.0f);
            float y = MathHelper.Clamp((PosY - Y1) / (Y2 - Y1), 0.0f, 1.0f);
            pixelInfoPopup.PixelUV = new Vector2(x, y);

            int TexW_MinusOne = Engine.TextureWidth - 1;
            int TexH_MinusOne = Engine.TextureHeight - 1;
            int X = MathHelper.Clamp((int)(x * Engine.TextureWidth), 0, TexW_MinusOne);
            int Y = MathHelper.Clamp((int)(y * Engine.TextureHeight), 0, TexH_MinusOne);
            pixelInfoPopup.PixelPosition = new Vector2(X, TexH_MinusOne - Y);

            float[] PixelColor = new float[4]; // RGBA
            int TextureID = (PreviewPosition >= x ? TextureIDs.FromTabControlMainTabIndex(tabControlMain.SelectedIndex) : TextureIDs.OriginalImage);
            GL.GetTextureSubImage(Engine.Textures[TextureID], 0, X, Y, 0, 1, 1, 1, PixelFormat.Rgba, PixelType.Float, sizeof(float) * PixelColor.Length, PixelColor);
            pixelInfoPopup.PixelColor = new ColorRGBA(PixelColor[0], PixelColor[1], PixelColor[2], PixelColor[3]);
        }

        #region Zoom
        public PointF ZoomCenterPoint
        {
            get
            {
                return new PointF(Zoom_CenterPoint.X / Engine.TextureWidth, Zoom_CenterPoint.Y / Engine.TextureHeight);
            }
            set
            {
                Zoom_CenterPoint.X = value.X * Engine.TextureWidth;
                Zoom_CenterPoint.Y = value.Y * Engine.TextureHeight;
                MoveImage(Mouse_LocationOld);
            }
        }

        public List<float> ZoomList
        {
            get { return Zoom_List; }
            set
            {
                Zoom_List = value;

                if (Zoom_Index > Zoom_List.Count - 1)
                {
                    Zoom_Index = Zoom_List.Count - 1;
                }
            }
        }

        public int ZoomListIndex
        {
            get { return Zoom_Index; }
            set
            {
                if (value < 0)
                {
                    Zoom_Index = 0;
                }
                else if (value > Zoom_List.Count - 1)
                {
                    Zoom_Index = Zoom_List.Count - 1;
                }
                else
                {
                    Zoom_Index = value;
                }
            }
        }

        public float GetZoom
        {
            get
            {
                if (Zoom_Index < 0 || Zoom_Index >= Zoom_List.Count)
                {
                    return 1f;
                }

                return Zoom_List[Zoom_Index];
            }
        }

        public float StretchZoom { get; private set; } = 0f;

        public void ZoomUpdate(bool RestoreLastZoom = false)
        {
            // Save Old Zoom!
            float LastZoom = GetZoom;
            bool IsLastZoomIsStretch = (Zoom_Index == Zoom_List.IndexOf(StretchZoom));

            Zoom_List.Clear();
            Zoom_Index = 0;

            // StretchZoom
            float DeltaW = glControl.ClientSize.Width / (float)Engine.TextureWidth;
            float DeltaH = glControl.ClientSize.Height / (float)Engine.TextureHeight;
            StretchZoom = (DeltaW > DeltaH ? DeltaH : DeltaW);
            Zoom_List.Add(StretchZoom);

            float MaxZoom = 128f;
            float MinZoom = (float)Math.Pow(2.0, Math.Ceiling(Math.Log(64.0 / Math.Max(Engine.TextureWidth, Engine.TextureHeight), 2.0)));

            // 1, 2, 4, 8 ... MaxZoom
            for (float i = 1f; i <= MaxZoom; i *= 2f)
            {
                Zoom_List.Add(i);
            }

            // 0.5, 0.25, 0.125 ... MinZoom
            for (float i = 0.5f; i >= MinZoom; i *= 0.5f)
            {
                Zoom_List.Add(i);
            }

            Zoom_List = Zoom_List.OrderBy(z => z).Distinct().ToList();

            if (RestoreLastZoom && !IsLastZoomIsStretch) // Restore Last Zoom
            {
                int MinDeltaIndex = 0;
                float MinDelta = float.MaxValue;
                for (int i = 0; i < Zoom_List.Count; i++)
                {
                    float Delta = Math.Abs(Zoom_List[i] - LastZoom);
                    if (Delta < MinDelta)
                    {
                        MinDeltaIndex = i;
                        MinDelta = Delta;
                    }
                }
                Zoom_Index = MinDeltaIndex;
            }
            else
            {
                Zoom_Index = Zoom_List.IndexOf(StretchZoom);
            }

            MoveImage(Mouse_LocationOld);
        }
        #endregion
    }
}