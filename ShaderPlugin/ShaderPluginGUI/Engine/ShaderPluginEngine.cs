using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using ShaderPlugin.PS_Structures;

namespace ShaderPluginGUI
{
    public class ShaderPluginEngine
    {
        // Base System info
        public string GPUVendor, GPURenderer, OpenGLVersion, ShadingLanguageVersion;

        public DrawModes DrawMode = DrawModes.RGBA;
        public MultiPassBuffersDrawMode MultiPassBufferDrawMode = MultiPassBuffersDrawMode.NoBuffers;
        public Color4 GLClearColor = new Color4(0.25f, 0.25f, 0.25f, 0f);
        public Color4 PreviewSplitterColor = new Color4(0.7f, 0.7f, 0.7f, 0f);
        public float PreviewSplitterLineWidth = 2f;
        public Color4 GridColor1 = new Color4(0.8f, 0.8f, 0.8f, 0f);
        public Color4 GridColor2 = new Color4(1f, 1f, 1f, 0f);
        public float GridSize = 16f;

        public int TextureWidth, TextureHeight;
        public int[] Textures = new int[TextureIDs.MAX];
        public int[] FrameBuffers = new int[FrameBufferIDs.MAX];
        public ShaderProgram[] Shaders = new ShaderProgram[ShaderIDs.MAX];
        public event ShaderCompileErrorHandler ShaderError;

        public Matrix4 MVPMatrix = Matrix4.Identity;

        int VAO, VBO_Vertexes, VBO_UV;
        Vector2[] VBO_VertexData = new Vector2[4];

        static Vector2[] VBO_UVData = new Vector2[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

        // Keys uniform
        uint[] KeyStates = new uint[24]; // 256 keys (1 bit each) packed into 8 integers for KeyDown [0..7], KeyPress [8..15], KeyToggle [16..23].

        // Color uniforms
        ColorRGBA PhotoshopColorBG = new ColorRGBA(1f, 1f, 1f, 1f);
        ColorRGBA PhotoshopColorFG = new ColorRGBA(0f, 0f, 0f, 1f);

        // Mouse uniforms
        public Vector2 MouseCoords = Vector2.Zero;
        public Vector4 MouseState = Vector4.Zero;

        public RenderLoopTickArgs TickData;

        #region Properties
        MultiPassBuffers _MultiPassBuffers = MultiPassBuffers.NoBuffers;
        
        bool useMipMaps = false;
        TextureMagFilter preview_MagFilter = TextureMagFilter.Nearest;
        TextureMinFilter preview_MinFilter = TextureMinFilter.LinearMipmapLinear;
        #endregion

        public ShaderPluginEngine()
        {
            // Get Base Info
            GPUVendor = GL.GetString(StringName.Vendor);
            GPURenderer = GL.GetString(StringName.Renderer);
            OpenGLVersion = GL.GetString(StringName.Version);
            ShadingLanguageVersion = GL.GetString(StringName.ShadingLanguageVersion);

            #region Shaders
            void CreateAndCompileShader(int ShaderID, String CodeVS, String CodeFS)
            {
                if (ShaderID < 0 || ShaderID >= ShaderIDs.MAX)
                {
                    throw new ArgumentOutOfRangeException(nameof(ShaderID));
                }

                string ShaderName = ShaderIDs.Names[ShaderID];
                ShaderProgram Shader = new ShaderProgram(ShaderName, CodeVS, CodeFS);
                Shader.CompileError += OnShaderError;

                if (Shader.CompileShader())
                {
                    Shaders[ShaderID]?.Free();
                    Shaders[ShaderID] = Shader;
                }
                else
                {
                    Shader.Free();
                }
            }

            CreateAndCompileShader(ShaderIDs.View, Properties.Resources.Shader_View_VS, Properties.Resources.Shader_View_FS);
            CreateAndCompileShader(ShaderIDs.ViewLine, Properties.Resources.Shader_ViewLine_VS, Properties.Resources.Shader_ViewLine_FS);
            CreateAndCompileShader(ShaderIDs.ViewGrid, Properties.Resources.Shader_View_VS, Properties.Resources.Shader_ViewGrid_FS);
            CreateAndCompileShader(ShaderIDs.ConvertImageFomat, Properties.Resources.Shader_ConvertImageFomat_VS, Properties.Resources.Shader_ConvertImageFomat_FS);
            #endregion

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            VBO_Vertexes = GL.GenBuffer();
            VBO_UV = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.BufferData(BufferTarget.ArrayBuffer, VBO_VertexData.Length * Vector2.SizeInBytes, VBO_VertexData, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_UV);
            GL.BufferData(BufferTarget.ArrayBuffer, VBO_UVData.Length * Vector2.SizeInBytes, VBO_UVData, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public bool CompileShaders(ShaderStateXML ShaderState, bool ForceRecompile)
        {
            if (ShaderState == null)
            {
                throw new ArgumentNullException(nameof(ShaderState));
            }

            bool Result = true;

            for (int ShaderID = ShaderIDs.Image; ShaderID < ShaderIDs.BufferA + (int)MultiPassBuffers; ShaderID++)
            {
                ShaderStateBuffer State = ShaderState[ShaderID];
                if (State == null)
                {
                    throw new NullReferenceException(nameof(State));
                }

                if (!ForceRecompile) // Skip recompile if shaders are equal
                {
                    ShaderProgram CompiledShader = Shaders[ShaderID];
                    if (CompiledShader != null &&
                        CompiledShader.WasSuccessfullyCompiled &&
                        CompiledShader.CodeVS == State.Shader_VS &&
                        CompiledShader.CodeFS == State.Shader_FS &&
                        CompiledShader.CommonCode == ShaderState.CommonCode)
                    {
                        continue;
                    }
                }

                String ShaderName = ShaderIDs.Names[ShaderID];
                ShaderProgram Shader = new ShaderProgram(ShaderName, State.Shader_VS, State.Shader_FS, ShaderState.CommonCode);
                Shader.CompileError += OnShaderError;

                if (Shader.CompileShader())
                {
                    Shaders[ShaderID]?.Free();
                    Shaders[ShaderID] = Shader;
                }
                else
                {
                    Shader.Free();
                    Result = false;
                }
            }

            return Result;
        }

        private void OnShaderError(ShaderError[] Errors)
        {
            ShaderError?.Invoke(Errors);
        }

        public void DrawViewport(float X1, float X2, float Y1, float Y2, float PreviewPosition = 0.5f)
        {
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.ClearColor(GLClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            int[] ViewportSizes = new int[4];
            GL.GetInteger(GetPName.Viewport, ViewportSizes);
            int ViewportWidth = ViewportSizes[2];
            float PreviewPos = PreviewPosition * 2f - 1f;

            #region Draw Line
            // Update Vertexes For Line Render
            VBO_VertexData = new Vector2[] { new Vector2(PreviewPos, -1), new Vector2(PreviewPos, 1) };
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, VBO_VertexData.Length * Vector2.SizeInBytes, VBO_VertexData);

            GL.LineWidth(PreviewSplitterLineWidth);

            ShaderProgram ShaderViewLine = Shaders[ShaderIDs.ViewLine];
            GL.UseProgram(ShaderViewLine.ProgramID);

            GL.Uniform4(ShaderViewLine.GetUniform("LineColor"), PreviewSplitterColor);

            GL.BindVertexArray(VAO);
            ShaderViewLine.EnableVertexAttribArrays();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.VertexAttribPointer(ShaderViewLine.GetAttribute("v_Position"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.DrawArrays(PrimitiveType.Lines, 0, 2);

            ShaderViewLine.DisableVertexAttribArrays();
            GL.BindVertexArray(0);
            #endregion

            #region Draw Grid
            // Update Vertexes
            VBO_VertexData = new Vector2[] { new Vector2(X1, Y1), new Vector2(X2, Y1), new Vector2(X1, Y2), new Vector2(X2, Y2) };
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.BufferData(BufferTarget.ArrayBuffer, VBO_VertexData.Length * Vector2.SizeInBytes, VBO_VertexData, BufferUsageHint.DynamicDraw);

            ShaderProgram ShaderViewGrid = Shaders[ShaderIDs.ViewGrid];
            GL.UseProgram(ShaderViewGrid.ProgramID);

            Vector2 TextureSize = new Vector2(TextureWidth, TextureHeight);
            float Zoom = Math.Abs(X2 - X1) / TextureWidth;

            GL.UniformMatrix4(ShaderViewGrid.GetUniform("MVP"), false, ref MVPMatrix);
            GL.Uniform2(ShaderViewGrid.GetUniform("Size"), TextureSize / Math.Max(2, GridSize) * Zoom);
            GL.Uniform4(ShaderViewGrid.GetUniform("ColorDark"), GridColor1);
            GL.Uniform4(ShaderViewGrid.GetUniform("ColorLight"), GridColor2);

            GL.BindVertexArray(VAO);
            ShaderViewGrid.EnableVertexAttribArrays();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.VertexAttribPointer(ShaderViewGrid.GetAttribute("v_Position"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_UV);
            GL.VertexAttribPointer(ShaderViewGrid.GetAttribute("v_UV"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            ShaderViewGrid.DisableVertexAttribArrays();
            GL.BindVertexArray(0);
            #endregion

            #region Draw Image
            // Update Vertexes
            VBO_VertexData = new Vector2[] { new Vector2(X1, Y1), new Vector2(X2, Y1), new Vector2(X1, Y2), new Vector2(X2, Y2) };
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.BufferData(BufferTarget.ArrayBuffer, VBO_VertexData.Length * Vector2.SizeInBytes, VBO_VertexData, BufferUsageHint.DynamicDraw);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            for (int TextureID = TextureIDs.OriginalImage; TextureID < TextureIDs.BufferA + (int)MultiPassBuffers; TextureID++)
            {
                int Texture = Textures[TextureID];
                if (GL.IsTexture(Texture))
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + TextureID);
                    GL.BindTexture(TextureTarget.Texture2D, Texture);

                    TextureMinFilter MinFilter = Preview_MinFilter;
                    if (!UseMipMaps)
                    {
                        switch (MinFilter)
                        {
                            case TextureMinFilter.NearestMipmapNearest:
                            case TextureMinFilter.NearestMipmapLinear:
                                MinFilter = TextureMinFilter.Nearest;
                                break;

                            case TextureMinFilter.LinearMipmapNearest:
                            case TextureMinFilter.LinearMipmapLinear:
                                MinFilter = TextureMinFilter.Linear;
                                break;
                        }
                    }

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)Preview_MagFilter);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)MinFilter);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }
            }

            ShaderProgram ShaderView = Shaders[ShaderIDs.View];
            GL.UseProgram(ShaderView.ProgramID);

            GL.UniformMatrix4(ShaderView.GetUniform("MVP"), false, ref MVPMatrix);
            GL.Uniform2(ShaderView.GetUniform("DrawMode"), (int)DrawMode, (int)MultiPassBufferDrawMode);

            PreviewPos = MathHelper.Clamp((ViewportWidth * (PreviewPosition - 0.5f) - X1) / (X2 - X1), 0f, 1f);
            GL.Uniform1(ShaderView.GetUniform("PreviewPosition"), PreviewPos);

            GL.BindVertexArray(VAO);
            ShaderView.EnableVertexAttribArrays();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_Vertexes);
            GL.VertexAttribPointer(ShaderView.GetAttribute("v_Position"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_UV);
            GL.VertexAttribPointer(ShaderView.GetAttribute("v_UV"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            ShaderView.DisableVertexAttribArrays();
            GL.BindVertexArray(0);
            #endregion
        }

        public void ClearFrameBuffers()
        {
            for (int i = 0; i < (int)MultiPassBuffers; i++)
            {
                int FrameBufferID = FrameBufferIDs.BufferA + i;
                int FrameBuffer = FrameBuffers[FrameBufferID];
                if (GL.IsFramebuffer(FrameBuffer))
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
                }

                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        public void DrawFrameBuffers(ShaderStateXML ShaderState)
        {
            if (ShaderState == null)
            {
                return;
            }

            int[] ViewportSizes = new int[4];
            GL.GetInteger(GetPName.Viewport, ViewportSizes);
            GL.Viewport(0, 0, TextureWidth, TextureHeight);

            for (int i = 0; i < (int)MultiPassBuffers; i++)
            {
                int FrameBufferID = FrameBufferIDs.BufferA + i;
                DrawFrameBuffer(FrameBufferID, ShaderState, ViewportSizes[2], ViewportSizes[3]);
            }

            DrawFrameBuffer(FrameBufferIDs.ProcessedImage, ShaderState, ViewportSizes[2], ViewportSizes[3]);

            GL.Viewport(ViewportSizes[0], ViewportSizes[1], ViewportSizes[2], ViewportSizes[3]);

            if (UseMipMaps)
            {
                for (int TextureID = TextureIDs.ProcessedImage; TextureID < TextureIDs.BufferA + (int)MultiPassBuffers; TextureID++)
                {
                    int Texture = Textures[TextureID];
                    if (GL.IsTexture(Texture))
                    {
                        GL.BindTexture(TextureTarget.Texture2D, Texture);
                        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                    }
                }
            }
        }

        private void DrawFrameBuffer(int FrameBufferID, ShaderStateXML ShaderState, int ViewportWidth, int ViewportHeight)
        {
            if (FrameBufferID < 0 || FrameBufferID >= FrameBufferIDs.MAX)
            {
                throw new ArgumentOutOfRangeException(nameof(FrameBufferID));
            }

            if (ShaderState == null)
            {
                throw new ArgumentNullException(nameof(ShaderState));
            }

            int ShaderID = FrameBufferID - FrameBufferIDs.BufferA + ShaderIDs.BufferA;
            ShaderProgram Shader = Shaders[ShaderID];
            if (Shader == null)
            {
                return; // Just return, Shader can have some errors and will not be compiled.
                // throw new NullReferenceException(nameof(Shader));
            }

            ShaderStateBuffer BufferState = ShaderState[ShaderID];
            if (BufferState == null)
            {
                throw new NullReferenceException(nameof(BufferState));
            }

            int FrameBuffer = FrameBuffers[FrameBufferID];
            if (GL.IsFramebuffer(FrameBuffer))
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
            }

            // Drawing Setup
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            // GL.ClearColor(0, 0, 0, 0);
            // GL.Clear(ClearBufferMask.ColorBufferBit);

            // Set Shader
            GL.UseProgram(Shader.ProgramID);
            SetShaderUniforms(Shader, ViewportWidth, ViewportHeight); // Set Parameters

            // Bind Textures
            int TextureUnitsCount = TextureUnitIDs.BufferA + (int)MultiPassBuffers;
            for (int TextureUnitID = TextureUnitIDs.OriginalImage; TextureUnitID < TextureUnitsCount; TextureUnitID++)
            {
                int Texture = Textures[TextureIDs.FromTextureUnitID(TextureUnitID)];
                if (GL.IsTexture(Texture))
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + TextureUnitID);
                    GL.BindTexture(TextureTarget.Texture2D, Texture);

                    ShaderStateTextureParams TextureFilter = BufferState.GetTextureFilter(TextureUnitID);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureFilter.TextureMagFilter);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureFilter.TextureMinFilter);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureFilter.TextureWrapModeS);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureFilter.TextureWrapModeT);
                }
            }

            // Draw
            GL.BindVertexArray(VAO);
            Shader.EnableVertexAttribArrays();

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_UV);
            GL.VertexAttribPointer(Shader.GetAttribute("v_UV"), 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            Shader.DisableVertexAttribArrays();
            GL.BindVertexArray(0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public int RegenerateFrameBuffer(int FrameBufferID)
        {
            if (FrameBufferID < 0 || FrameBufferID >= FrameBuffers.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(FrameBufferID));
            }

            int TextureID = TextureIDs.FromFrameBufferID(FrameBufferID);
            int Texture = Textures[TextureID];
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            int FrameBuffer = FrameBuffers[FrameBufferID];
            if (GL.IsFramebuffer(FrameBuffer))
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.DeleteFramebuffer(FrameBuffer);
            }

            FrameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture, 0);

            if (!CheckFramebuffer(FrameBuffer, FrameBufferIDs.Names[FrameBufferID])) // Check Framebuffer
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.DeleteFramebuffer(FrameBuffer);
                return -1;
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            FrameBuffers[FrameBufferID] = FrameBuffer;
            return FrameBuffer;
        }

        private bool CheckFramebuffer(int FBO, string Name)
        {
            FramebufferErrorCode FramebufferStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (FramebufferStatus != FramebufferErrorCode.FramebufferComplete)
            {
                MessageBox.Show($"FBO error: {FramebufferStatus}.", $"Framebuffer \"{Name}\"", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void SetShaderUniforms(ShaderProgram Shader, int ViewportWidth, int ViewportHeight)
        {
            if (Shader == null)
            {
                throw new ArgumentNullException(nameof(Shader));
            }

            // Photoshop Background Color [0..1]
            int UnioformID = Shader.GetUniform("iColorBG");
            if (UnioformID >= 0)
            {
                GL.Uniform3(UnioformID, PhotoshopColorBG);
            }

            // Photoshop Foreground Color [0..1]
            UnioformID = Shader.GetUniform("iColorFG");
            if (UnioformID >= 0)
            {
                GL.Uniform3(UnioformID, PhotoshopColorFG);
            }

            // Image Size [vec2, ivec2]
            UnioformID = Shader.GetUniform("iImageSize");
            if (UnioformID >= 0)
            {
                GL.Uniform2(UnioformID, TextureWidth, TextureHeight);
                GL.Uniform2(UnioformID, (float)TextureWidth, (float)TextureHeight);
            }

            // Viewport Size, also can use iResolution. [vec2, ivec2]
            UnioformID = Shader.GetUniform("iViewSize");
            if (UnioformID >= 0)
            {
                GL.Uniform2(UnioformID, ViewportWidth, ViewportHeight);
                GL.Uniform2(UnioformID, (float)ViewportWidth, (float)ViewportHeight);
            }

            // Viewport Size, also can use iViewSize. [vec2, ivec2]
            UnioformID = Shader.GetUniform("iResolution"); // Same as iViewSize
            if (UnioformID >= 0)
            {
                GL.Uniform2(UnioformID, ViewportWidth, ViewportHeight);
                GL.Uniform2(UnioformID, (float)ViewportWidth, (float)ViewportHeight);
            }

            // Random values [0 .. 1]
            UnioformID = Shader.GetUniform("iRandom");
            if (UnioformID >= 0)
            {
                Random R = new Random();
                GL.Uniform4(UnioformID, (float)R.NextDouble(), (float)R.NextDouble(), (float)R.NextDouble(), (float)R.NextDouble());
            }

            // Year, Month, Day, Time in seconds
            UnioformID = Shader.GetUniform("iDate");
            if (UnioformID >= 0)
            {
                DateTime dateTime = DateTime.Now;
                GL.Uniform4(UnioformID, dateTime.Year, dateTime.Month, dateTime.Day, (float)dateTime.TimeOfDay.TotalSeconds);
            }

            // Running time (sec)
            UnioformID = Shader.GetUniform("iTime");
            if (UnioformID >= 0)
            {
                GL.Uniform1(UnioformID, (float)TickData.RunningTime);
            }

            // Frame render time (sec)
            UnioformID = Shader.GetUniform("iTimeDelta");
            if (UnioformID >= 0)
            {
                GL.Uniform1(UnioformID, (float)TickData.FrameTime);
            }

            // Frame rate (FPS) [float, int]
            UnioformID = Shader.GetUniform("iFrameRate");
            if (UnioformID >= 0)
            {
                GL.Uniform1(UnioformID, (float)TickData.FPS);
                GL.Uniform1(UnioformID, (int)TickData.FPS);
            }

            // Frame Number [float, int]
            UnioformID = Shader.GetUniform("iFrame");
            if (UnioformID >= 0)
            {
                GL.Uniform1(UnioformID, (float)TickData.FrameNumber);
                GL.Uniform1(UnioformID, TickData.FrameNumber);
            }

            // xy: Current position (if LMB Pressed), zw: Pressed position (signs: move, one frame). [vec4, ivec4]
            UnioformID = Shader.GetUniform("iMouse");
            if (UnioformID >= 0)
            {
                GL.Uniform4(UnioformID, (int)MouseState.X, (int)MouseState.Y, (int)MouseState.Z, (int)MouseState.W);
                GL.Uniform4(UnioformID, MouseState);

                if (MouseState != Vector4.Zero && MouseState.W >= 0.0)
                {
                    MouseState.W = -MouseState.W;
                }
            }

            // Current Mouse Position, update every frame [vec2, ivec2].
            UnioformID = Shader.GetUniform("iMouseCoords");
            if (UnioformID >= 0)
            {
                GL.Uniform2(UnioformID, (int)MouseCoords.X, (int)MouseCoords.Y);
                GL.Uniform2(UnioformID, MouseCoords);
            }

            UnioformID = Shader.GetUniform("iKeyStates[0]");
            if (UnioformID >= 0)
            {
                GL.Uniform1(UnioformID, KeyStates.Length, KeyStates);
                for (int i = 8; i < 16; i++)
                {
                    KeyStates[i] = 0u;
                }
            }
        }

        public bool GenerateMipMaps(int TextureID)
        {
            int SrcTexture = Textures[TextureID];
            if (!GL.IsTexture(SrcTexture))
            {
                return false;
            }

            GL.BindTexture(TextureTarget.Texture2D, SrcTexture);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return true;
        }

        public bool DeleteMipMaps(int TextureID)
        {
            int SrcTexture = Textures[TextureID];
            if (!GL.IsTexture(SrcTexture))
            {
                return false;
            }

            GL.BindTexture(TextureTarget.Texture2D, SrcTexture);

            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int Texture_Width);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int Texture_Height);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureInternalFormat, out int Texture_InternalFormat);

            int FrameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, SrcTexture, 0);

            int DstTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, DstTexture);
            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, (InternalFormat)Texture_InternalFormat, 0, 0, Texture_Width, Texture_Height, 0);

            GL.DeleteTexture(SrcTexture);
            Textures[TextureID] = DstTexture;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DeleteFramebuffer(FrameBuffer);
            return true;
        }

        public int PrepateTexture(int SrcTexture, TexturePrepareMode PrepareMode, bool CopySrcData, bool GenMipMaps)
        {
            if (!GL.IsTexture(SrcTexture))
            {
                return -1;
            }

            GL.BindTexture(TextureTarget.Texture2D, SrcTexture);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int TexWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int TexHeight);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            int DstTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, DstTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            if (TexWidth * TexHeight * 4 * (long)sizeof(float) > UInt32.MaxValue) // More then 4 Gb (16K images, 8-bit only)
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, TexWidth, TexHeight, 0, PixelFormat.Rgba, PixelType.Byte, new byte[TexWidth * 4, TexHeight]);
            }
            else
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, TexWidth, TexHeight, 0, PixelFormat.Rgba, PixelType.Float, new float[TexWidth * 4, TexHeight]);
            }

            if (CopySrcData)
            {
                int FrameBuffer = GL.GenFramebuffer(); // Frame buffer renderer for editing
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, DstTexture, 0);

                // Check Framebuffer
                FramebufferErrorCode FramebufferStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (FramebufferStatus != FramebufferErrorCode.FramebufferComplete)
                {
                    MessageBox.Show("FBO error: " + FramebufferStatus.ToString(), "PrepateTexture", MessageBoxButton.OK, MessageBoxImage.Error);

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.DeleteFramebuffer(FrameBuffer);

                    if (GL.IsTexture(DstTexture))
                    {
                        GL.DeleteTexture(DstTexture);
                    }

                    return -1;
                }

                // Setup Viewport
                int[] ViewportSizes = new int[4];
                GL.GetInteger(GetPName.Viewport, ViewportSizes);
                GL.Viewport(0, 0, TexWidth, TexHeight);

                // Draw
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.Blend);
                GL.ClearColor(Color4.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                ShaderProgram ShaderConvertImageFomat = Shaders[ShaderIDs.ConvertImageFomat];
                GL.UseProgram(ShaderConvertImageFomat.ProgramID);
                // Set Parameters
                GL.Uniform1(ShaderConvertImageFomat.GetUniform("EditingMode"), (int)PrepareMode);

                // Bind Texture
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, SrcTexture);

                GL.BindVertexArray(VAO);
                ShaderConvertImageFomat.EnableVertexAttribArrays();

                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_UV);
                GL.VertexAttribPointer(ShaderConvertImageFomat.GetAttribute("v_UV"), 2, VertexAttribPointerType.Float, false, 0, 0);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

                ShaderConvertImageFomat.DisableVertexAttribArrays();
                GL.BindVertexArray(0);

                // Free
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.DeleteFramebuffer(FrameBuffer);
                GL.Viewport(ViewportSizes[0], ViewportSizes[1], ViewportSizes[2], ViewportSizes[3]);
            }

            if (GenMipMaps)
            {
                GL.BindTexture(TextureTarget.Texture2D, DstTexture);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }

            return DstTexture;
        }

        public void SetKeyboardKeyState(int Key, bool IsPressed)
        {
            int Pressed_Index = Key / 32;
            int PressedKeyBit = Key % 32;
            uint KeyBit = (1u << PressedKeyBit);

            if (IsPressed)
            {
                KeyStates[Pressed_Index] |= KeyBit;         // Is key Pressed? (current state)
                KeyStates[Pressed_Index + 8] |= KeyBit;     // Pressed momentary (for one tick)
                KeyStates[Pressed_Index + 16] ^= KeyBit;    // Toggle state by KeyPress
            }
            else
            {
                KeyStates[Pressed_Index] &= ~KeyBit;
                KeyStates[Pressed_Index + 8] &= ~KeyBit;
            }
        }

        public void ResetInputStates()
        {
            MouseState = Vector4.Zero;
            MouseCoords = Vector2.Zero;

            for (int i = 0; i < KeyStates.Length; i++)
            {
                KeyStates[i] = 0;
            }
        }

        public bool GetTextureFromPhotoshop()
        {
            var filterRecord = Program.filterRecord;
            if (filterRecord == null)
            {
                return false;
            }

            var bigDoc = filterRecord.bigDocumentData;

            if (bigDoc.floatCoord32.V < 0)
            {
                bigDoc.inRect32 = new VRect()
                {
                    Left = (short)-bigDoc.floatCoord32.H,
                    Top = (short)-bigDoc.floatCoord32.V,
                    Right = (short)(bigDoc.wholeSize32.H - bigDoc.floatCoord32.H),
                    Bottom = (short)(bigDoc.wholeSize32.V - bigDoc.floatCoord32.V)
                };
            }
            else
            {
                bigDoc.inRect32 = bigDoc.filterRect32;
            }

            bigDoc.PluginUsing32BitCoordinates = 1;
            filterRecord.bigDocumentData = bigDoc;
            //max channel
            int channels = filterRecord.planes;
            filterRecord.inLoPlane = 0;
            filterRecord.inHiPlane = (short)(channels - 1);
            filterRecord.Write();
            filterRecord.advanceState();
            filterRecord.Read();

            bigDoc = filterRecord.bigDocumentData;

            TextureWidth = bigDoc.inRect32.Right - bigDoc.inRect32.Left;
            TextureHeight = bigDoc.inRect32.Bottom - bigDoc.inRect32.Top;
            int RowBytes = filterRecord.inRowBytes;             // Row bytes (with padding)
            int Channels = filterRecord.planes;                 // Channels count (from 1, can be bigger then 4)
            int ColorChannels = Math.Max(filterRecord.inLayerPlanes,
                 Math.Min((int)filterRecord.inNonLayerPlanes, 3));      //Color channels without alpha on image or mask
            bool haveTransparency = (filterRecord.inTransparencyMask == 1) &&
                ColorChannels != Channels ||
                filterRecord.inNonLayerPlanes >= 4;                     //Is there alpha channel? If masks selected - always false


            int Depth = filterRecord.depth;                             // Depth bits (default 8)
            int DepthBytes = Depth / 8;                                 // Bytes per channel (default 1)

            int ImageChannels = ColorChannels +
                (haveTransparency ? 1 : 0);                             // OpenGL texture channels count [1 .. 4]

            int SrcChannelsSizeBytes = Channels * DepthBytes;           // Source texture channels components size in bytes
            int DstChannelsSizeBytes = ImageChannels * DepthBytes;      // OpenGL texture channels components size in bytes
            int DstRowBytes = TextureWidth * DstChannelsSizeBytes;      // OpenGL texture row size in bytes

            IntPtr PixelsPointer = filterRecord.inData;

            byte[] BytesRGBA = new byte[DstRowBytes * TextureHeight];

            Parallel.For(0, TextureHeight, i =>
            {
                int IndexSource = i * RowBytes;
                int IndexDestination = i * DstRowBytes;
                Parallel.For(0, TextureWidth, j =>
                {
                    Marshal.Copy(PixelsPointer + IndexSource + j * SrcChannelsSizeBytes, BytesRGBA, IndexDestination + j * DstChannelsSizeBytes, DstChannelsSizeBytes);
                });
            });

            PixelFormat pixelFormat = TexFormatConverter.GetGLPixelFormat(ColorChannels, haveTransparency);
            PixelInternalFormat pixelInternalFormat = TexFormatConverter.GetGLPixelInternalFormat(Depth, ColorChannels, haveTransparency);
            PixelType pixelType = TexFormatConverter.GetGLPixelTypeFromPhotoshop(Depth);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            if (GL.IsTexture(Textures[TextureIDs.OriginalImage]))
            {
                GL.DeleteTexture(Textures[TextureIDs.OriginalImage]);
            }

            Textures[TextureIDs.OriginalImage] = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Textures[TextureIDs.OriginalImage]);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, pixelInternalFormat, TextureWidth, TextureHeight, 0, pixelFormat, pixelType, BytesRGBA);

            #region Convert all to RGBA32F
            if (GL.IsTexture(Textures[TextureIDs.ProcessedImage]))
            {
                GL.DeleteTexture(Textures[TextureIDs.ProcessedImage]);
            }

            TexturePrepareMode PrepModeMask = (Depth == 16 ? TexturePrepareMode.Depth16_PS_To_GL : TexturePrepareMode.Nothing);
            PrepModeMask |= TexturePrepareMode.FlipUV_Y;

            switch (pixelFormat)
            {
                case PixelFormat.Red:
                    PrepModeMask |= TexturePrepareMode.RXXX_TO_RRR1;
                    break;

                case PixelFormat.Rg:
                    PrepModeMask |= (haveTransparency ? TexturePrepareMode.RAXX_TO_RRRA : TexturePrepareMode.RGXX_TO_RG01);
                    break;

                case PixelFormat.Rgb:
                    PrepModeMask |= (haveTransparency ? TexturePrepareMode.RGAX_TO_RG0A : TexturePrepareMode.RGBX_TO_RGB1);
                    break;

                default:
                case PixelFormat.Rgba:
                    PrepModeMask |= TexturePrepareMode.RGBA_TO_RGBA;
                    break;
            }

            Textures[TextureIDs.ProcessedImage] = PrepateTexture(Textures[TextureIDs.OriginalImage], PrepModeMask, true, UseMipMaps);

            if (GL.IsTexture(Textures[TextureIDs.OriginalImage]))
            {
                GL.DeleteTexture(Textures[TextureIDs.OriginalImage]);
            }

            Textures[TextureIDs.OriginalImage] = PrepateTexture(Textures[TextureIDs.ProcessedImage], TexturePrepareMode.Nothing, true, UseMipMaps);
            #endregion

            GC.Collect();
            return true;
        }

        public void ApplyToPhotoshop()
        {
            var filterRecord = Program.filterRecord;
            if (filterRecord == null)
            {
                return;
            }

            var bigDoc = filterRecord.bigDocumentData;

            int Width = bigDoc.inRect32.Right - bigDoc.inRect32.Left;
            int Height = bigDoc.inRect32.Bottom - bigDoc.inRect32.Top;
            int RowBytes = filterRecord.inRowBytes;             // Row bytes (with padding)
            int Channels = filterRecord.planes;                 // Channels count (from 1, can be bigger then 4)
            int ColorChannels = Math.Max(filterRecord.inLayerPlanes,
                 Math.Min((int)filterRecord.inNonLayerPlanes, 3));                 //Color channels without alpha on image or mask
            bool haveTransparency = (filterRecord.inTransparencyMask == 1) &&
                ColorChannels != Channels
                || filterRecord.inNonLayerPlanes >= 4; //Is there alpha channel? If masks selected - always false


            int Depth = filterRecord.depth;                             // Depth bits (default 8)
            int DepthBytes = Depth / 8;                                 // Bytes per channel (default 1)

            int ImageChannels = ColorChannels +
                (haveTransparency ? 1 : 0);                             // OpenGL texture channels count [1 .. 4]

            int DstChannelsSizeBytes = Channels * DepthBytes;           // Destination texture channels components size in bytes
            int SrcChannelsSizeBytes = 4 * DepthBytes;                  // OpenGL texture RGBA channels components size in bytes
            int SrcRowBytes = Width * SrcChannelsSizeBytes;             // OpenGL texture row size in bytes


            filterRecord.outRect = filterRecord.inRect;
            bigDoc.outRect32 = filterRecord.bigDocumentData.inRect32;
            bigDoc.PluginUsing32BitCoordinates = 1;
            filterRecord.bigDocumentData = bigDoc;
            filterRecord.outLoPlane = filterRecord.inLoPlane;
            filterRecord.outHiPlane = filterRecord.inHiPlane;

            filterRecord.Write();
            filterRecord.advanceState();

            filterRecord.Read();
            IntPtr outDataPtr = filterRecord.outData;

            PixelFormat pixelFormat = TexFormatConverter.GetGLPixelFormat(ColorChannels, haveTransparency);
            PixelType pixelType = TexFormatConverter.GetGLPixelTypeFromPhotoshop(Depth);

            #region Convert RGBA32F to PS format
            if (GL.IsTexture(Textures[TextureIDs.OriginalImage]))
            {
                GL.DeleteTexture(Textures[TextureIDs.OriginalImage]);
            }

            TexturePrepareMode PrepModeMask = (Depth == 16 ? TexturePrepareMode.Depth16_GL_To_PS : TexturePrepareMode.Nothing);
            PrepModeMask |= TexturePrepareMode.FlipUV_Y;

            switch (pixelFormat)
            {
                default:
                    PrepModeMask |= TexturePrepareMode.RGBA_TO_RGBA;
                    break;

                case PixelFormat.Rg:
                    PrepModeMask |= (haveTransparency ? TexturePrepareMode.RGBA_TO_RAGB : TexturePrepareMode.Nothing);
                    break;

                case PixelFormat.Rgb:
                    PrepModeMask |= (haveTransparency ? TexturePrepareMode.RGBA_TO_RGAB : TexturePrepareMode.Nothing);
                    break;
            }

            Textures[TextureIDs.OriginalImage] = PrepateTexture(Textures[TextureIDs.ProcessedImage], PrepModeMask, true, false);
            #endregion

            byte[] BytesRGBA = new byte[SrcRowBytes * Height];

            GL.BindTexture(TextureTarget.Texture2D, Textures[TextureIDs.OriginalImage]);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, pixelType, BytesRGBA);

            Parallel.For(0, Height, i =>
            {
                int IndexSource = i * SrcRowBytes; // OpenGL Data
                int IndexDestination = i * RowBytes; // Photoshop Data
                Parallel.For(0, Width, j =>
                {
                    Marshal.Copy(BytesRGBA, IndexSource + j * SrcChannelsSizeBytes, outDataPtr + IndexDestination + j * DstChannelsSizeBytes, DstChannelsSizeBytes);
                });
            });

            GC.Collect();
        }

        public bool RunColorPicker(ColorRGBA InColor, out ColorRGBA ResultColor)
        {
            ResultColor = InColor;

            ColorServicesInfo service = ColorServicesInfo.Instantiate();
            service.colorComponents[0] = InColor.RByte;
            service.colorComponents[1] = InColor.GByte;
            service.colorComponents[2] = InColor.BByte;
            service.colorComponents[3] = InColor.AByte;

            if (Program.filterRecord == null || Program.filterRecord.colorServices(ref service) < 0)
            {
                return false;
            }

            ResultColor = new ColorRGBA((byte)service.colorComponents[0], (byte)service.colorComponents[1], (byte)service.colorComponents[2], (byte)service.colorComponents[3]);
            return true;
        }

        public void GetPhotoshopBackgroundForegroundColors()
        {
            ColorServicesInfo SrvBG = ColorServicesInfo.Instantiate();
            ColorServicesInfo SrvFG = ColorServicesInfo.Instantiate();
            SrvBG.selector = SrvFG.selector = ColorServicesConsts.plugIncolorServicesGetSpecialColor;

            SrvBG.selectorParameter.specialColorID = ColorServicesConsts.plugIncolorServicesBackgroundColor;
            SrvFG.selectorParameter.specialColorID = ColorServicesConsts.plugIncolorServicesForegroundColor;

            if (Program.filterRecord == null || Program.filterRecord.colorServices(ref SrvBG) < 0 || Program.filterRecord.colorServices(ref SrvFG) < 0)
            {
                return;
            }

            PhotoshopColorBG = new ColorRGBA((byte)SrvBG.colorComponents[0], (byte)SrvBG.colorComponents[1], (byte)SrvBG.colorComponents[2], (byte)SrvBG.colorComponents[3]);
            PhotoshopColorFG = new ColorRGBA((byte)SrvFG.colorComponents[0], (byte)SrvFG.colorComponents[1], (byte)SrvFG.colorComponents[2], (byte)SrvFG.colorComponents[3]);
        }

        #region Properties
        public TextureMagFilter Preview_MagFilter
        {
            get { return preview_MagFilter; }
            set
            {
                preview_MagFilter = value;

                for (int TextureID = TextureIDs.ProcessedImage; TextureID < TextureIDs.BufferA + (int)MultiPassBuffers; TextureID++)
                {
                    int Texture = Textures[TextureID];
                    if (GL.IsTexture(Texture))
                    {
                        GL.BindTexture(TextureTarget.Texture2D, Texture);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)preview_MagFilter);
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                    }
                }
            }
        }

        public TextureMinFilter Preview_MinFilter
        {
            get { return preview_MinFilter; }
            set
            {
                preview_MinFilter = value;

                for (int TextureID = TextureIDs.ProcessedImage; TextureID < TextureIDs.BufferA + (int)MultiPassBuffers; TextureID++)
                {
                    int Texture = Textures[TextureID];
                    if (GL.IsTexture(Texture))
                    {
                        GL.BindTexture(TextureTarget.Texture2D, Texture);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)preview_MinFilter);
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                    }
                }
            }
        }

        public bool UseMipMaps
        {
            get { return useMipMaps; }
            set
            {
                if (useMipMaps != value)
                {
                    useMipMaps = value;

                    for (int TextureID = TextureIDs.OriginalImage; TextureID < TextureIDs.BufferA + (int)MultiPassBuffers; TextureID++)
                    {
                        int Texture = Textures[TextureID];
                        if (GL.IsTexture(Texture))
                        {
                            if (value)
                            {
                                GenerateMipMaps(TextureID);
                            }
                            else
                            {
                                DeleteMipMaps(TextureID);

                                if (TextureID != TextureIDs.OriginalImage)
                                {
                                    RegenerateFrameBuffer(FrameBufferIDs.FromTextureID(TextureID));
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool UseFramebufferSrgb
        {
            get
            {
                GL.GetBoolean(GetPName.FramebufferSrgb, out bool IsSrgb);
                return IsSrgb;
            }
            set
            {
                if (value)
                {
                    GL.Enable(EnableCap.FramebufferSrgb);
                }
                else
                {
                    GL.Disable(EnableCap.FramebufferSrgb);
                }
            }
        }

        public MultiPassBuffers MultiPassBuffers
        {
            get
            {
                return _MultiPassBuffers;
            }
            set
            {
                int CurrentCount = (int)_MultiPassBuffers;
                int NeededCount = (int)value;

                if (CurrentCount < NeededCount) // Add FBOs
                {
                    for (int i = CurrentCount; i < NeededCount; i++)
                    {
                        int TextureID = TextureIDs.BufferA + i;
                        int Texture = Textures[TextureID];
                        if (GL.IsTexture(Texture))
                        {
                            GL.DeleteTexture(Texture);
                        }

                        Texture = PrepateTexture(Textures[TextureIDs.OriginalImage], TexturePrepareMode.Nothing, false, false);
                        Textures[TextureID] = Texture;

                        int FrameBufferID = FrameBufferIDs.BufferA + i;
                        RegenerateFrameBuffer(FrameBufferID);
                    }
                }
                else if (CurrentCount > NeededCount) // Remove FBOs
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.BindTexture(TextureTarget.Texture2D, 0);

                    for (int i = NeededCount; i < CurrentCount; i++)
                    {
                        int FrameBufferID = FrameBufferIDs.BufferA + i;
                        int FrameBuffer = FrameBuffers[FrameBufferID];
                        if (GL.IsFramebuffer(FrameBuffer))
                        {
                            GL.DeleteFramebuffer(FrameBuffer);
                            FrameBuffers[FrameBufferID] = -1;
                        }

                        int TextureID = TextureIDs.BufferA + i;
                        int Texture = Textures[TextureID];
                        if (GL.IsTexture(Texture))
                        {
                            GL.DeleteTexture(Texture);
                            Textures[TextureID] = -1;
                        }
                    }
                }
                _MultiPassBuffers = value;
            }
        }
        #endregion

        public void Free()
        {
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(VAO);
            VAO = -1;

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(VBO_Vertexes);
            GL.DeleteBuffer(VBO_UV);
            VBO_Vertexes = VBO_UV = -1;

            GL.UseProgram(0);
            ShaderError = null;
            for (int ShaderID = 0; ShaderID < Shaders.Length; ShaderID++)
            {
                Shaders[ShaderID]?.Free();
                Shaders[ShaderID] = null;
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            for (int FrameBufferID = 0; FrameBufferID < FrameBuffers.Length; FrameBufferID++)
            {
                int FrameBuffer = FrameBuffers[FrameBufferID];
                if (GL.IsFramebuffer(FrameBuffer))
                {
                    GL.DeleteFramebuffer(FrameBuffer);
                    FrameBuffers[FrameBufferID] = -1;
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            for (int TextureID = 0; TextureID < Textures.Length; TextureID++)
            {
                int Texture = Textures[TextureID];
                if (GL.IsTexture(Texture))
                {
                    GL.DeleteTexture(Texture);
                    Textures[TextureID] = -1;
                }
            }

            GC.Collect();
        }
    }
}