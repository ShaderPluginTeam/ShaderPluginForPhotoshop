using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

using AvalonEditExtensions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;

using OpenTK.Graphics.OpenGL;

using Path = System.IO.Path;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

using PixelFormatGL = OpenTK.Graphics.OpenGL.PixelFormat;
using PixelFormatWPF = System.Windows.Media.PixelFormat;

namespace ShaderPluginGUI
{
    public partial class MainWindow : DarkWindow
    {
        static string WindowTitle = "Shader Plugin";

        #region Commands
        public static readonly RoutedCommand CopyImageCommand = new RoutedCommand();
        public static readonly RoutedCommand PasteImageCommand = new RoutedCommand();
        public static readonly RoutedCommand SystemInfoCommand = new RoutedCommand();
        public static readonly RoutedCommand ResetTimeCommand = new RoutedCommand();
        public static readonly RoutedCommand RenderLoopToggleCommand = new RoutedCommand();
        public static readonly RoutedCommand CompileCommand = new RoutedCommand();
        public static readonly RoutedCommand ApplyCommand = new RoutedCommand();
        #endregion Commands

        SettingsXML Settings;
        ShaderStateXML ShaderState;
        SaveFileDialog saveFileDialog;
        OpenFileDialog openFileDialog;

        int ProgrammaticEventFlag = 0; // Counter, preventing event calls when instigated from code
        readonly Regex FloatCheckerRegex = new Regex(@"^\d*\.?\d*$");

        #region Errors, Syntax Highlight, Foldings
        List<ShaderError> LastShaderErrors = new List<ShaderError>();
        ErrorLineBackgroundRenderer ErrorLineBGRendererCommonCode;
        ErrorLineBackgroundRenderer ErrorLineBGRendererImage_VS, ErrorLineBGRendererImage_FS;
        ErrorLineBackgroundRenderer ErrorLineBGRendererBufferA_VS, ErrorLineBGRendererBufferA_FS;
        ErrorLineBackgroundRenderer ErrorLineBGRendererBufferB_VS, ErrorLineBGRendererBufferB_FS;
        ErrorLineBackgroundRenderer ErrorLineBGRendererBufferC_VS, ErrorLineBGRendererBufferC_FS;
        ErrorLineBackgroundRenderer ErrorLineBGRendererBufferD_VS, ErrorLineBGRendererBufferD_FS;

        FoldingExtension FoldingExtension = new FoldingExtension();
        #endregion

        public MainWindow() : base()
        {
            InitializeComponent();

            if (Program.PhotoshopRunLastFilterEnabled)
            {
                NeedSetWindowChrome = false; // Prevent set WindowChrome and crash when applying last Filter in Photoshop

                // Don't show window for 1 frame
                Visibility = Visibility.Hidden; // Visibility not working, use AllowsTransparency(
                AllowsTransparency = true;
                Opacity = 0;
            }

            #region Debug mode
            if (Program.filterRecord == null && Program.DebugImage != null)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            #endregion Debug mode

            #region Recover window size
            if (Properties.Settings.Default.Width > 0)
            {
                Width = Properties.Settings.Default.Width;
            }

            if (Properties.Settings.Default.Height > 0)
            {
                Height = Properties.Settings.Default.Height;
            }
            #endregion

            #region Install AvalonEdit Extensions
            void InstallAvalonEditExtensions(params TextEditor[] TextEditors)
            {
                foreach (TextEditor TextEditor in TextEditors)
                {
                    SearchPanel.Install(TextEditor);
                    GoToLineWindow.Install(TextEditor, this);
                    FoldingExtension.Install(TextEditor);
                }
            }

            InstallAvalonEditExtensions(textEditorCommonCode, textEditorImage_VS, textEditorImage_FS, textEditorBufferA_VS, textEditorBufferA_FS,
                textEditorBufferB_VS, textEditorBufferB_FS, textEditorBufferC_VS, textEditorBufferC_FS, textEditorBufferD_VS, textEditorBufferD_FS);
            #endregion

            #region Shader Error and Error Lines
            ErrorLineBGRendererCommonCode = new ErrorLineBackgroundRenderer(textEditorCommonCode);
            ErrorLineBGRendererImage_VS = new ErrorLineBackgroundRenderer(textEditorImage_VS);
            ErrorLineBGRendererImage_FS = new ErrorLineBackgroundRenderer(textEditorImage_FS);
            ErrorLineBGRendererBufferA_VS = new ErrorLineBackgroundRenderer(textEditorBufferA_VS);
            ErrorLineBGRendererBufferA_FS = new ErrorLineBackgroundRenderer(textEditorBufferA_FS);
            ErrorLineBGRendererBufferB_VS = new ErrorLineBackgroundRenderer(textEditorBufferB_VS);
            ErrorLineBGRendererBufferB_FS = new ErrorLineBackgroundRenderer(textEditorBufferB_FS);
            ErrorLineBGRendererBufferC_VS = new ErrorLineBackgroundRenderer(textEditorBufferC_VS);
            ErrorLineBGRendererBufferC_FS = new ErrorLineBackgroundRenderer(textEditorBufferC_FS);
            ErrorLineBGRendererBufferD_VS = new ErrorLineBackgroundRenderer(textEditorBufferD_VS);
            ErrorLineBGRendererBufferD_FS = new ErrorLineBackgroundRenderer(textEditorBufferD_FS);
            #endregion

            #region Fix Enums, set it to ComboBoxes as ItemsSource
            comboBox_Image_Buffers.ItemsSource = (MultiPassBuffers[])Enum.GetValues(typeof(MultiPassBuffers));

            // Settings
            comboBox_PreviewViewMode.ItemsSource = (DrawModes[])Enum.GetValues(typeof(DrawModes));
            comboBox_PreviewViewMode.SelectedItem = DrawModes.RGBA;
            comboBox_MagFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMagFilters();
            comboBox_MinFilter.ItemsSource = OpenGLComboBoxItemSource.GetTextureMinFilters(true);
            #endregion

            #region Highlighting
            using (MemoryStream ms = new MemoryStream(Properties.Resources.GLSL_Hightlight))
            {
                using (XmlTextReader reader = new XmlTextReader(ms))
                {
                    IHighlightingDefinition Hightlight = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    textEditorCommonCode.SyntaxHighlighting = Hightlight;
                    textEditorImage_VS.SyntaxHighlighting = Hightlight;
                    textEditorImage_FS.SyntaxHighlighting = Hightlight;
                    textEditorBufferA_VS.SyntaxHighlighting = Hightlight;
                    textEditorBufferA_FS.SyntaxHighlighting = Hightlight;
                    textEditorBufferB_VS.SyntaxHighlighting = Hightlight;
                    textEditorBufferB_FS.SyntaxHighlighting = Hightlight;
                    textEditorBufferC_VS.SyntaxHighlighting = Hightlight;
                    textEditorBufferC_FS.SyntaxHighlighting = Hightlight;
                    textEditorBufferD_VS.SyntaxHighlighting = Hightlight;
                    textEditorBufferD_FS.SyntaxHighlighting = Hightlight;
                }
            }
            #endregion

            #region File Dialogs
            openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                InitialDirectory = Program.ShadersFolderPath,
                Filter = "All Shaders|*.shader;*.fs;*.ps;*.vs|Shader Files (*.shader)|*.shader|Fragment Shader (*.fs,*.ps)|*.fs;*.ps|Vertex Shader (*.vs)|*.vs|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            saveFileDialog = new SaveFileDialog
            {
                RestoreDirectory = true,
                InitialDirectory = Program.ShadersFolderPath,
                Filter = "Shader Files (*.shader)|*.shader|Fragment Shader (*.fs)|*.fs|Vertex Shader (*.vs)|*.vs|All Files (*.*)|*.*",
                FilterIndex = 1
            };
            #endregion
        }

        private void SettingsApply()
        {
            comboBox_MagFilter.SelectedItem = Engine.Preview_MagFilter = Settings.PreviewMagFilter;
            comboBox_MinFilter.SelectedItem = Engine.Preview_MinFilter = Settings.PreviewMinFilter;

            checkBox_ForceCompileWhenApply.IsChecked = Settings.ForceCompileWhenApply;

            Engine.GLClearColor = Settings.BackgroundColor;
            rectangle_BGColor.Fill = new SolidColorBrush(Settings.BackgroundColor.WithoutAlpha);

            Engine.PreviewSplitterLineWidth = Settings.PreviewSplitter.LineWidth;
            Engine.PreviewSplitterColor = Settings.PreviewSplitter.LineColor;
            rectangle_PreviewSplitterColor.Fill = new SolidColorBrush(Settings.PreviewSplitter.LineColor.WithoutAlpha);

            Engine.GridColor1 = Settings.Grid.GridColor1;
            rectangle_GridColor1.Fill = new SolidColorBrush(Settings.Grid.GridColor1.WithoutAlpha);

            Engine.GridColor2 = Settings.Grid.GridColor2;
            rectangle_GridColor2.Fill = new SolidColorBrush(Settings.Grid.GridColor2.WithoutAlpha);

            Engine.GridSize = Settings.Grid.GridSize;
            tbGridSize.Text = Engine.GridSize.ToString();

            glControl.Invalidate();
        }

        private void SetDefaultShaders(bool InitialWindowOpening)
        {
            textEditorCommonCode.Text = Properties.Resources.Shader_CommonCode;
            textEditorImage_VS.Text = Properties.Resources.Shader_Edit_VS;
            textEditorImage_FS.Text = InitialWindowOpening ? Properties.Resources.Shader_Edit_FS : Properties.Resources.Shader_EditNew_FS;
            textEditorBufferA_VS.Text = textEditorBufferB_VS.Text = textEditorBufferC_VS.Text = textEditorBufferD_VS.Text = Properties.Resources.Shader_Edit_VS;
            textEditorBufferA_FS.Text = textEditorBufferB_FS.Text = textEditorBufferC_FS.Text = textEditorBufferD_FS.Text = Properties.Resources.Shader_Edit_FS;

            FoldingExtension.CollapseCommentFoldings(textEditorCommonCode, textEditorImage_VS, textEditorImage_FS,
                textEditorBufferA_VS, textEditorBufferA_FS, textEditorBufferB_VS, textEditorBufferB_FS,
                textEditorBufferC_VS, textEditorBufferC_FS, textEditorBufferD_VS, textEditorBufferD_FS);
        }

        private void SetDefaultTextureParameters()
        {
            ProgrammaticEventFlag++;

            comboBox_Image_Buffers.SelectedItem = MultiPassBuffers.NoBuffers;
            checkBox_UseMipMaps.IsChecked = false;

            // Image
            bufferConfig_Image_Image.SetDefaultParameters();
            bufferConfig_Image_BufferA.SetDefaultParameters();
            bufferConfig_Image_BufferB.SetDefaultParameters();
            bufferConfig_Image_BufferC.SetDefaultParameters();
            bufferConfig_Image_BufferD.SetDefaultParameters();

            // Buffer A
            bufferConfig_BufferA_Image.SetDefaultParameters();
            bufferConfig_BufferA_BufferA.SetDefaultParameters();
            bufferConfig_BufferA_BufferB.SetDefaultParameters();
            bufferConfig_BufferA_BufferC.SetDefaultParameters();
            bufferConfig_BufferA_BufferD.SetDefaultParameters();

            // Buffer B
            bufferConfig_BufferB_Image.SetDefaultParameters();
            bufferConfig_BufferB_BufferA.SetDefaultParameters();
            bufferConfig_BufferB_BufferB.SetDefaultParameters();
            bufferConfig_BufferB_BufferC.SetDefaultParameters();
            bufferConfig_BufferB_BufferD.SetDefaultParameters();

            // Buffer C
            bufferConfig_BufferC_Image.SetDefaultParameters();
            bufferConfig_BufferC_BufferA.SetDefaultParameters();
            bufferConfig_BufferC_BufferB.SetDefaultParameters();
            bufferConfig_BufferC_BufferC.SetDefaultParameters();
            bufferConfig_BufferC_BufferD.SetDefaultParameters();

            // Buffer D
            bufferConfig_BufferD_Image.SetDefaultParameters();
            bufferConfig_BufferD_BufferA.SetDefaultParameters();
            bufferConfig_BufferD_BufferB.SetDefaultParameters();
            bufferConfig_BufferD_BufferC.SetDefaultParameters();
            bufferConfig_BufferD_BufferD.SetDefaultParameters();

            ProgrammaticEventFlag--;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Recover window State (Maximized Window not centered if it's will be in Constructor)
            if (Properties.Settings.Default.WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            #endregion

            glControl.MakeCurrent();

            // Settings
            Settings = SettingsXML.LoadOrDefault();
            SettingsApply();

            if (!Engine.GetTextureFromPhotoshop() && Program.DebugImage != null)
            {
                // Load Debug Image
                Engine.TextureWidth = Program.DebugImage.PixelWidth;
                Engine.TextureHeight = Program.DebugImage.PixelHeight;
                GetTextureFromWriteableBitmap(Program.DebugImage);
            }

            Engine.RegenerateFrameBuffer(FrameBufferIDs.ProcessedImage);
            Engine.GetPhotoshopBackgroundForegroundColors();
            Engine.ShaderError += OnShaderError;

            SetDefaultTextureParameters();
            SetDefaultShaders(true);

            GlControl_Resize(sender, e);
            ZoomUpdate(false);
            InvalidateVisual(); // Invelidate UI
            CompileShader(true); // Compile default Shader before last used (pass through shader)

            #region Load Last Shader File
            ShaderStateXML shaderXML = ShaderStateXML.Load(Path.Combine(Program.StartupPath, ShaderStateXML.LastShaderFile));
            if (shaderXML != null)
            {
                ApplyLoadedShaderXML(shaderXML);
            }
            else
            {
                SetDefaultShaders(false); // Set some example shader, like "New" button pressed
            }
            #endregion

            if (Program.PhotoshopRunLastFilterEnabled) // Need "Last Filter"
            {
                if (CompileShader(true))
                {
                    Engine.ApplyToPhotoshop();
                    Program.Result = PSPluginErrorCodes.NoError;
                }
                else
                {
                    Program.Result = PSPluginErrorCodes.ParamError;
                }

                Close();
                return;
            }

            checkBox_sRGB.IsChecked = Program.filterRecord?.depth == 32;

            PostLoadShader(); // Try Compile and start Render Loop
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.WindowState = WindowState;
            InvalidateVisual();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.Width = Width;
                Properties.Settings.Default.Height = Height;
            }

            InvalidateVisual();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RenderLoop?.Stop();
            RenderLoop = null;

            Properties.Settings.Default.Save();
        }

        private void OnShaderError(ShaderError[] Errors)
        {
            // Save Error for Error Window
            LastShaderErrors.AddRange(Errors);

            // Show error at StatusBar
            ShaderErrorType FirstErrorType = Errors[0].ErrorType;
            ShaderError FirstError = Errors[0];

            if (FirstError.ErrorLines.Count > 0 && FirstErrorType != ShaderErrorType.LoadFromString)
            {
                var FirstErrLine = FirstError.ErrorLines.First();
                statusBarError.Text = $"{FirstErrorType} shader error at Line {FirstErrLine.Key}: {FirstErrLine.Value}";
            }
            else
            {
                statusBarError.Text = FirstError.InfoLog.
                    Replace("\r", "").
                    Replace("\n", " ").
                    Replace("------------- ", String.Empty). // "Fragment info"
                    Replace("----------- ", String.Empty).   // "Vertex info"
                    Replace("  ", " ");
            }

            // Error Lines Highlight
            foreach (ShaderError Error in Errors)
            {
                switch (Error.ErrorType)
                {
                    case ShaderErrorType.Vertex:
                        if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.Image])
                        {
                            ErrorLineBGRendererImage_VS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferA])
                        {
                            ErrorLineBGRendererBufferA_VS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferB])
                        {
                            ErrorLineBGRendererBufferB_VS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferC])
                        {
                            ErrorLineBGRendererBufferC_VS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferD])
                        {
                            ErrorLineBGRendererBufferD_VS.SetErrorLines(Error.ErrorLines);
                        }
                        break;

                    case ShaderErrorType.Fragment:

                        if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.Image])
                        {
                            ErrorLineBGRendererImage_FS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferA])
                        {
                            ErrorLineBGRendererBufferA_FS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferB])
                        {
                            ErrorLineBGRendererBufferB_FS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferC])
                        {
                            ErrorLineBGRendererBufferC_FS.SetErrorLines(Error.ErrorLines);
                        }
                        else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferD])
                        {
                            ErrorLineBGRendererBufferD_FS.SetErrorLines(Error.ErrorLines);
                        }
                        break;

                    case ShaderErrorType.CommonInclude:
                        ErrorLineBGRendererCommonCode.SetErrorLines(Error.ErrorLinesCommonInclude);
                        break;
                }
            }
        }

        private bool LoadShaderData(string FileName)
        {
            RenderLoop?.Stop();

            int Index = openFileDialog.FilterIndex;
            if (Index == 1 || Path.GetExtension(FileName).ToLowerInvariant() == ".shader")
            {
                Index = 2;
            }

            switch (Index)
            {
                case 2:
                    ShaderStateXML shaderXML = ShaderStateXML.Load(FileName);
                    if (shaderXML == null)
                    {
                        return false;
                    }

                    Title = $"{WindowTitle} - {Path.GetFileNameWithoutExtension(FileName)}";
                    ApplyLoadedShaderXML(shaderXML);
                    break;

                default:
                    string ShaderStr = File.ReadAllText(FileName);

                    void SetLoadedShaderText(TextEditor TextEditor)
                    {
                        TextEditor.Text = ShaderStr;
                        FoldingExtension.CollapseCommentFoldings(TextEditor);
                    }

                    switch (tabControlMain.SelectedIndex)
                    {
                        case TabControlMainTabIndexes.Common:
                            SetLoadedShaderText(textEditorCommonCode);
                            break;

                        case TabControlMainTabIndexes.BufferA:
                            SetLoadedShaderText(tabControlBufferA.SelectedIndex == 0 ? textEditorBufferA_VS : textEditorBufferA_FS);
                            break;

                        case TabControlMainTabIndexes.BufferB:
                            SetLoadedShaderText(tabControlBufferB.SelectedIndex == 0 ? textEditorBufferB_VS : textEditorBufferB_FS);
                            break;

                        case TabControlMainTabIndexes.BufferC:
                            SetLoadedShaderText(tabControlBufferC.SelectedIndex == 0 ? textEditorBufferC_VS : textEditorBufferC_FS);
                            break;

                        case TabControlMainTabIndexes.BufferD:
                            SetLoadedShaderText(tabControlBufferD.SelectedIndex == 0 ? textEditorBufferD_VS : textEditorBufferD_FS);
                            break;

                        default:
                        case TabControlMainTabIndexes.Image:
                            SetLoadedShaderText(tabControlImage.SelectedIndex == 0 ? textEditorImage_VS : textEditorImage_FS);
                            break;
                    }
                    break;
            }

            PostLoadShader(); // Try Compile and start Render Loop
            return true;
        }

        private bool ApplyLoadedShaderXML(ShaderStateXML InShaderState)
        {
            if (InShaderState == null)
            {
                return false;
            }

            ShaderState = InShaderState;

            glControl.MakeCurrent();

            ProgrammaticEventFlag++;

            tabControlMain.SelectedIndex = TabControlMainTabIndexes.Image;
            checkBox_AutoCompileAfterLoading.IsChecked = ShaderState.AutoCompileAfterLoading;
            checkBox_StartRenderLoopAfterLoading.IsChecked = toggleButton_RenderLoop_Play.IsChecked = ShaderState.AutoPlayAfterLoading;

            void SetLoadedShaderText(TextEditor TextEditor, string ShaderCode)
            {
                TextEditor.Text = ShaderCode;
                FoldingExtension.CollapseCommentFoldings(TextEditor);
            }

            SetLoadedShaderText(textEditorCommonCode, ShaderState.CommonCode);
            SetLoadedShaderText(textEditorImage_VS, ShaderState.Image.Shader_VS);
            SetLoadedShaderText(textEditorImage_FS, ShaderState.Image.Shader_FS);

            SetDefaultTextureParameters();

            Engine.MultiPassBufferDrawMode = MultiPassBuffersDrawMode.NoBuffers;
            comboBox_Image_Buffers.SelectedItem = ShaderState.MultiPassBuffers;
            checkBox_UseMipMaps.IsChecked = ShaderState.UseMipMaps;

            bufferConfig_Image_Image.TextureParams = ShaderState.Image.TextureFilter;
            bufferConfig_Image_BufferA.TextureParams = ShaderState.Image.TextureFilterBufferA;
            bufferConfig_Image_BufferB.TextureParams = ShaderState.Image.TextureFilterBufferB;
            bufferConfig_Image_BufferC.TextureParams = ShaderState.Image.TextureFilterBufferC;
            bufferConfig_Image_BufferD.TextureParams = ShaderState.Image.TextureFilterBufferD;

            int MultiPassBuffersCount = (int)ShaderState.MultiPassBuffers;
            if (MultiPassBuffersCount > 0)
            {
                if (ShaderState.BufferA != null)
                {
                    SetLoadedShaderText(textEditorBufferA_VS, ShaderState.BufferA.Shader_VS);
                    SetLoadedShaderText(textEditorBufferA_FS, ShaderState.BufferA.Shader_FS);

                    bufferConfig_BufferA_Image.TextureParams = ShaderState.BufferA.TextureFilter;
                    bufferConfig_BufferA_BufferA.TextureParams = ShaderState.BufferA.TextureFilterBufferA;
                    bufferConfig_BufferA_BufferB.TextureParams = ShaderState.BufferA.TextureFilterBufferB;
                    bufferConfig_BufferA_BufferC.TextureParams = ShaderState.BufferA.TextureFilterBufferC;
                    bufferConfig_BufferA_BufferD.TextureParams = ShaderState.BufferA.TextureFilterBufferD;
                }
                else
                {
                    MessageBox.Show("shaderXML.BufferA == null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (MultiPassBuffersCount > 1)
                {
                    if (ShaderState.BufferB != null)
                    {
                        SetLoadedShaderText(textEditorBufferB_VS, ShaderState.BufferB.Shader_VS);
                        SetLoadedShaderText(textEditorBufferB_FS, ShaderState.BufferB.Shader_FS);

                        bufferConfig_BufferB_Image.TextureParams = ShaderState.BufferB.TextureFilter;
                        bufferConfig_BufferB_BufferA.TextureParams = ShaderState.BufferB.TextureFilterBufferA;
                        bufferConfig_BufferB_BufferB.TextureParams = ShaderState.BufferB.TextureFilterBufferB;
                        bufferConfig_BufferB_BufferC.TextureParams = ShaderState.BufferB.TextureFilterBufferC;
                        bufferConfig_BufferB_BufferD.TextureParams = ShaderState.BufferB.TextureFilterBufferD;
                    }
                    else
                    {
                        MessageBox.Show("shaderXML.BufferB == null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (MultiPassBuffersCount > 2)
                    {
                        if (ShaderState.BufferC != null)
                        {
                            SetLoadedShaderText(textEditorBufferC_VS, ShaderState.BufferC.Shader_VS);
                            SetLoadedShaderText(textEditorBufferC_FS, ShaderState.BufferC.Shader_FS);

                            bufferConfig_BufferC_Image.TextureParams = ShaderState.BufferC.TextureFilter;
                            bufferConfig_BufferC_BufferA.TextureParams = ShaderState.BufferC.TextureFilterBufferA;
                            bufferConfig_BufferC_BufferB.TextureParams = ShaderState.BufferC.TextureFilterBufferB;
                            bufferConfig_BufferC_BufferC.TextureParams = ShaderState.BufferC.TextureFilterBufferC;
                            bufferConfig_BufferC_BufferD.TextureParams = ShaderState.BufferC.TextureFilterBufferD;
                        }
                        else
                        {
                            MessageBox.Show("shaderXML.BufferC == null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        if (MultiPassBuffersCount > 3)
                        {
                            if (ShaderState.BufferD != null)
                            {
                                SetLoadedShaderText(textEditorBufferD_VS, ShaderState.BufferD.Shader_VS);
                                SetLoadedShaderText(textEditorBufferD_FS, ShaderState.BufferD.Shader_FS);

                                bufferConfig_BufferD_Image.TextureParams = ShaderState.BufferD.TextureFilter;
                                bufferConfig_BufferD_BufferA.TextureParams = ShaderState.BufferD.TextureFilterBufferA;
                                bufferConfig_BufferD_BufferB.TextureParams = ShaderState.BufferD.TextureFilterBufferB;
                                bufferConfig_BufferD_BufferC.TextureParams = ShaderState.BufferD.TextureFilterBufferC;
                                bufferConfig_BufferD_BufferD.TextureParams = ShaderState.BufferD.TextureFilterBufferD;
                            }
                            else
                            {
                                MessageBox.Show("shaderXML.BufferD == null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }

            Engine.MultiPassBuffers = ShaderState.MultiPassBuffers;
            ProgrammaticEventFlag--;
            return true;
        }

        // Try Compile and start Render Loop
        private void PostLoadShader()
        {
            iconPresenterCompileStatus.SetIconFromResource("Icon.StatusInformationOutline");
            statusBarError.Text = "Shaders need to be compiled!";

            // Auto-Compile shader
            if (checkBox_AutoCompileAfterLoading.IsChecked == true)
            {
                CompileShader(true); // Compile loaded shader
            }

            ResetDrawState(false);

            // Auto-start RenderLoop
            if (checkBox_StartRenderLoopAfterLoading.IsChecked == true)
            {
                toggleButton_RenderLoop_Play.IsChecked = true;
                RenderLoop?.Start();
            }
        }

        private bool CompileShader(bool ForceRecompile)
        {
            // Temporaly pause RenderLoop
            bool NeedResumeRenderLoop = RenderLoop != null && RenderLoop.IsRunning;
            if (NeedResumeRenderLoop)
            {
                RenderLoop.Stop();
            }

            ShaderState = MakeShaderStateforSaving();

            // Clear Errors at StatusBar and ErrorLines Highlight
            iconPresenterCompileStatus.SetIconFromResource("Icon.StatusInformationOutline");
            statusBarError.Text = "Compile Shaders...";
            LastShaderErrors.Clear();

            ErrorLineBGRendererCommonCode.ClearErrors();
            ErrorLineBGRendererImage_VS.ClearErrors();
            ErrorLineBGRendererImage_FS.ClearErrors();
            ErrorLineBGRendererBufferA_VS.ClearErrors();
            ErrorLineBGRendererBufferA_FS.ClearErrors();
            ErrorLineBGRendererBufferB_VS.ClearErrors();
            ErrorLineBGRendererBufferB_FS.ClearErrors();
            ErrorLineBGRendererBufferC_VS.ClearErrors();
            ErrorLineBGRendererBufferC_FS.ClearErrors();
            ErrorLineBGRendererBufferD_VS.ClearErrors();
            ErrorLineBGRendererBufferD_FS.ClearErrors();

            glControl.MakeCurrent();

            Stopwatch StopWatch = new Stopwatch();
            StopWatch.Start();

            bool Result = Engine.CompileShaders(ShaderState, ForceRecompile);

            StopWatch.Stop();
            if (Result)
            {
                if (StopWatch.Elapsed.TotalSeconds < 0.001)
                {
                    statusBarError.Text = "Shaders compiled successfully.";
                }
                else
                {
                    statusBarError.Text = $"Shaders compiled successfully in {StopWatch.Elapsed.TotalSeconds:0.000} sec.";
                }

                Engine.DrawFrameBuffers(ShaderState);
                glControl.Invalidate();
            }

            iconPresenterCompileStatus.SetIconFromResource(Result ? "Icon.StatusOKOutline" : "Icon.StatusInvalidOutline");

            // Resume RendedLoop
            if (NeedResumeRenderLoop)
            {
                RenderLoop?.Start();
            }

            return Result;
        }

        private ShaderStateXML MakeShaderStateforSaving()
        {
            ShaderStateXML NewShaderState = new ShaderStateXML();
            NewShaderState.AutoCompileAfterLoading = checkBox_AutoCompileAfterLoading.IsChecked.Value;
            NewShaderState.AutoPlayAfterLoading = checkBox_StartRenderLoopAfterLoading.IsChecked.Value;
            NewShaderState.UseMipMaps = checkBox_UseMipMaps.IsChecked.Value;
            NewShaderState.MultiPassBuffers = Engine.MultiPassBuffers;
            NewShaderState.CommonCode = textEditorCommonCode.Text;

            NewShaderState.Image = new ShaderStateBuffer()
            {
                Shader_VS = textEditorImage_VS.Text,
                Shader_FS = textEditorImage_FS.Text,
                TextureFilter = bufferConfig_Image_Image.TextureParams,
                TextureFilterBufferA = bufferConfig_Image_BufferA.TextureParams,
                TextureFilterBufferB = bufferConfig_Image_BufferB.TextureParams,
                TextureFilterBufferC = bufferConfig_Image_BufferC.TextureParams,
                TextureFilterBufferD = bufferConfig_Image_BufferD.TextureParams
            };

            int MultiPassBuffersCount = (int)Engine.MultiPassBuffers;
            if (MultiPassBuffersCount > 0)
            {
                NewShaderState.BufferA = new ShaderStateBuffer()
                {
                    Shader_VS = textEditorBufferA_VS.Text,
                    Shader_FS = textEditorBufferA_FS.Text,
                    TextureFilter = bufferConfig_BufferA_Image.TextureParams,
                    TextureFilterBufferA = bufferConfig_BufferA_BufferA.TextureParams,
                    TextureFilterBufferB = bufferConfig_BufferA_BufferB.TextureParams,
                    TextureFilterBufferC = bufferConfig_BufferA_BufferC.TextureParams,
                    TextureFilterBufferD = bufferConfig_BufferA_BufferD.TextureParams
                };

                if (MultiPassBuffersCount > 1)
                {
                    NewShaderState.BufferB = new ShaderStateBuffer()
                    {
                        Shader_VS = textEditorBufferB_VS.Text,
                        Shader_FS = textEditorBufferB_FS.Text,
                        TextureFilter = bufferConfig_BufferB_Image.TextureParams,
                        TextureFilterBufferA = bufferConfig_BufferB_BufferA.TextureParams,
                        TextureFilterBufferB = bufferConfig_BufferB_BufferB.TextureParams,
                        TextureFilterBufferC = bufferConfig_BufferB_BufferC.TextureParams,
                        TextureFilterBufferD = bufferConfig_BufferB_BufferD.TextureParams
                    };

                    if (MultiPassBuffersCount > 2)
                    {
                        NewShaderState.BufferC = new ShaderStateBuffer()
                        {
                            Shader_VS = textEditorBufferC_VS.Text,
                            Shader_FS = textEditorBufferC_FS.Text,
                            TextureFilter = bufferConfig_BufferC_Image.TextureParams,
                            TextureFilterBufferA = bufferConfig_BufferC_BufferA.TextureParams,
                            TextureFilterBufferB = bufferConfig_BufferC_BufferB.TextureParams,
                            TextureFilterBufferC = bufferConfig_BufferC_BufferC.TextureParams,
                            TextureFilterBufferD = bufferConfig_BufferC_BufferD.TextureParams
                        };

                        if (MultiPassBuffersCount > 3)
                        {
                            NewShaderState.BufferD = new ShaderStateBuffer()
                            {
                                Shader_VS = textEditorBufferD_VS.Text,
                                Shader_FS = textEditorBufferD_FS.Text,
                                TextureFilter = bufferConfig_BufferD_Image.TextureParams,
                                TextureFilterBufferA = bufferConfig_BufferD_BufferA.TextureParams,
                                TextureFilterBufferB = bufferConfig_BufferD_BufferB.TextureParams,
                                TextureFilterBufferC = bufferConfig_BufferD_BufferC.TextureParams,
                                TextureFilterBufferD = bufferConfig_BufferD_BufferD.TextureParams
                            };
                        }
                    }
                }
            }

            return NewShaderState;
        }

        private void UpdateShaderState()
        {
            if (ProgrammaticEventFlag > 0 || Engine == null) return;

            ShaderStateXML ShaderStateForSaving = MakeShaderStateforSaving();
            if (ShaderState != ShaderStateForSaving)
            {
                ShaderState = ShaderStateForSaving;
                glControl.MakeCurrent();
                Engine.DrawFrameBuffers(ShaderStateForSaving);
                glControl.Invalidate();
            }
        }

        private void ResetDrawState(bool Redraw)
        {
            // Temporaly pause RenderLoop
            bool NeedResumeRenderLoop = RenderLoop != null && RenderLoop.IsRunning;
            if (NeedResumeRenderLoop)
            {
                RenderLoop.Stop();
            }

            Engine.ClearFrameBuffers();
            Engine.ResetInputStates();
            RenderLoop?.Reset(Redraw);

            // Resume RendedLoop
            if (NeedResumeRenderLoop)
            {
                RenderLoop?.Start();
            }
        }

        private bool GetTextureFromWriteableBitmap(WriteableBitmap wBmp)
        {
            if (wBmp == null)
            {
                return false;
            }

            // Convert Format
            if (wBmp.Format != PixelFormats.Bgra32)
            {
                FormatConvertedBitmap FormatConvertedBmp = new FormatConvertedBitmap(wBmp, PixelFormats.Bgra32, null, 0.0);
                WriteableBitmap ConvertedBmp = new WriteableBitmap(FormatConvertedBmp);

                wBmp = null;
                GC.Collect();
                wBmp = ConvertedBmp;
            }

            glControl.MakeCurrent();

            // Remove OriginalImage, Image Textures
            GL.BindTexture(TextureTarget.Texture2D, 0);
            for (int TextureID = TextureIDs.OriginalImage; TextureID <= TextureIDs.ProcessedImage; TextureID++)
            {
                int Texture = Engine.Textures[TextureID];
                if (GL.IsTexture(Texture))
                {
                    GL.DeleteTexture(Texture);
                    Engine.Textures[TextureID] = -1;
                }
            }

            int TextureOriginal = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureOriginal);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);

            PixelFormatGL GL_PF = TexFormatConverter.GetGLPixelFormat(wBmp.Format);
            PixelInternalFormat GL_PIF = TexFormatConverter.GetGLPixelInternalFormat(wBmp.Format);

            wBmp.Lock();
            GL.TexImage2D(TextureTarget.Texture2D, 0, GL_PIF, wBmp.PixelWidth, wBmp.PixelHeight, 0, GL_PF, PixelType.UnsignedByte, wBmp.BackBuffer);
            wBmp.Unlock();

            int TextureProcessed = Engine.PrepateTexture(TextureOriginal, TexturePrepareMode.FlipUV_Y, true, Engine.UseMipMaps);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(TextureOriginal);
            TextureOriginal = Engine.PrepateTexture(TextureProcessed, TexturePrepareMode.Nothing, true, Engine.UseMipMaps);
            Engine.Textures[TextureIDs.OriginalImage] = TextureOriginal;
            Engine.Textures[TextureIDs.ProcessedImage] = TextureProcessed;

            return true;
        }

        #region Panel Buttons
        private void Button_New_Click(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("Create new shader?", "New shader", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                RenderLoop?.Stop();
                glControl.MakeCurrent();

                tabControlMain.SelectedIndex = TabControlMainTabIndexes.Image;
                Engine.MultiPassBuffers = MultiPassBuffers.NoBuffers;
                Engine.MultiPassBufferDrawMode = MultiPassBuffersDrawMode.NoBuffers;
                checkBox_AutoCompileAfterLoading.IsChecked = true;
                checkBox_StartRenderLoopAfterLoading.IsChecked = toggleButton_RenderLoop_Play.IsChecked = true;
                checkBox_UseMipMaps.IsChecked = false;

                SetDefaultTextureParameters();
                SetDefaultShaders(false);

                Title = WindowTitle;
                openFileDialog.FileName = saveFileDialog.FileName = String.Empty;

                CompileShader(true);
                ResetDrawState(false);
                RenderLoop?.Start(true);
            }
        }

        private void Button_Open_Click(object sender, ExecutedRoutedEventArgs e)
        {
            string DirectoryName = openFileDialog.InitialDirectory;
            string FileName = String.Empty;

            try
            {
                DirectoryName = String.IsNullOrEmpty(openFileDialog.FileName) ? String.Empty : Path.GetDirectoryName(openFileDialog.FileName);
                FileName = Path.GetFileName(openFileDialog.FileName);

                if (Directory.Exists(DirectoryName))
                {
                    openFileDialog.InitialDirectory = DirectoryName;
                    openFileDialog.FileName = FileName;
                }
            }
            catch { }

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                saveFileDialog.InitialDirectory = DirectoryName;
                saveFileDialog.FileName = openFileDialog.FileName;

                if (!LoadShaderData(openFileDialog.FileName))
                {
                    MessageBox.Show($"Maybe \"{FileName}\" is not shader file!",
                        "File opening error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Save_Click(object sender, ExecutedRoutedEventArgs e)
        {
            string DirectoryName = saveFileDialog.InitialDirectory;
            string FileName = String.Empty;

            try
            {
                DirectoryName = Path.GetDirectoryName(saveFileDialog.FileName);
                FileName = Path.GetFileName(saveFileDialog.FileName);

                if (Directory.Exists(DirectoryName))
                {
                    saveFileDialog.InitialDirectory = DirectoryName;
                    saveFileDialog.FileName = FileName;
                }
            }
            catch { }

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                openFileDialog.InitialDirectory = DirectoryName;
                openFileDialog.FileName = saveFileDialog.FileName;

                switch (saveFileDialog.FilterIndex)
                {
                    default:
                    case 1:
                        if (!ShaderStateXML.Save(MakeShaderStateforSaving(), saveFileDialog.FileName))
                        {
                            MessageBox.Show("Error while saving \"" + Path.GetFileName(FileName) + "\" shader file!",
                                "File saving error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        break;

                    case 2:
                        File.WriteAllText(saveFileDialog.FileName, textEditorImage_FS.Text);
                        break;

                    case 3:
                        File.WriteAllText(saveFileDialog.FileName, textEditorImage_VS.Text);
                        break;

                    case 4:
                        string ShaderStr = String.Empty;
                        switch (tabControlMain.SelectedIndex)
                        {
                            case TabControlMainTabIndexes.Common:
                                ShaderStr = textEditorCommonCode.Text;
                                break;

                            case TabControlMainTabIndexes.BufferA:
                                ShaderStr = (tabControlBufferA.SelectedIndex == 0 ? textEditorBufferA_VS : textEditorBufferA_FS).Text;
                                break;

                            case TabControlMainTabIndexes.BufferB:
                                ShaderStr = (tabControlBufferB.SelectedIndex == 0 ? textEditorBufferB_VS : textEditorBufferB_FS).Text;
                                break;

                            case TabControlMainTabIndexes.BufferC:
                                ShaderStr = (tabControlBufferC.SelectedIndex == 0 ? textEditorBufferC_VS : textEditorBufferC_FS).Text;
                                break;

                            case TabControlMainTabIndexes.BufferD:
                                ShaderStr = (tabControlBufferD.SelectedIndex == 0 ? textEditorBufferD_VS : textEditorBufferD_FS).Text;
                                break;

                            default:
                            case TabControlMainTabIndexes.Image:
                                ShaderStr = (tabControlImage.SelectedIndex == 0 ? textEditorImage_VS : textEditorImage_FS).Text;
                                break;
                        }
                        File.WriteAllText(saveFileDialog.FileName, ShaderStr);
                        break;
                }
            }
        }

        private void Button_Copy_Click(object sender, ExecutedRoutedEventArgs e)
        {
            glControl.MakeCurrent();

            int TextureSrc = Engine.Textures[TextureIDs.ProcessedImage];
            if (!GL.IsTexture(TextureSrc))
            {
                return;
            }

            int TextureDst = Engine.PrepateTexture(TextureSrc, TexturePrepareMode.FlipUV_Y, true, false);
            if (!GL.IsTexture(TextureDst))
            {
                return;
            }

            GL.BindTexture(TextureTarget.Texture2D, TextureDst);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 4);

            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth, out int TextureWidth);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, out int TextureHeight);
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureInternalFormat, out int TextureInternalFormat);

            // WPF
            PixelFormatWPF PF_WPF = TexFormatConverter.GetWPFPixelFormat((PixelInternalFormat)TextureInternalFormat);
            PixelFormatGL PF_GL = TexFormatConverter.GetGLPixelFormat(PF_WPF);

            WriteableBitmap wBmp = new WriteableBitmap(TextureWidth, TextureHeight, 96.0, 96.0, PF_WPF, null);
            wBmp.Lock();
            GL.GetTexImage(TextureTarget.Texture2D, 0, PF_GL, PixelType.UnsignedByte, wBmp.BackBuffer);
            wBmp.Unlock();

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(TextureDst);

            using (MemoryStream MemStream = new MemoryStream())
            {
                PngBitmapEncoder PngEncoder = new PngBitmapEncoder();
                PngEncoder.Frames.Add(BitmapFrame.Create(wBmp));
                PngEncoder.Save(MemStream);

                DataObject ClipboardDataObject = new DataObject();
                ClipboardDataObject.SetData(DataFormats.Bitmap, wBmp); // For legacy apps
                ClipboardDataObject.SetData("PNG", MemStream); // Transparency support
                Clipboard.SetDataObject(ClipboardDataObject, true);
            }

            wBmp = null;
            GC.Collect();
        }

        private void Button_Paste_Click(object sender, ExecutedRoutedEventArgs e)
        {
            BitmapSource bmpSrc = null;
            IDataObject DataObject = Clipboard.GetDataObject();
            if (DataObject != null && DataObject.GetDataPresent("PNG"))
            {
                MemoryStream MemStream = DataObject.GetData("PNG") as MemoryStream;
                if (MemStream != null)
                {
                    PngBitmapDecoder PngDecoder = new PngBitmapDecoder(MemStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    bmpSrc = PngDecoder.Frames[0];
                    MemStream.Dispose();
                }
            }

            // Fallback, if no PNG in Clipboard, read Bitmap (can loose transparency)
            if (bmpSrc == null && Clipboard.ContainsImage())
            {
                bmpSrc = Clipboard.GetImage();
            }

            WriteableBitmap wBmp = bmpSrc != null ? new WriteableBitmap(bmpSrc) : null;
            if (wBmp == null)
            {
                return;
            }

            // Rescale to original size
            if (wBmp.PixelWidth != Engine.TextureWidth || wBmp.PixelHeight != Engine.TextureHeight)
            {
                double ScaleX = (double)Engine.TextureWidth / wBmp.PixelWidth;
                double ScaleY = (double)Engine.TextureHeight / wBmp.PixelHeight;
                TransformedBitmap ScaledBitmap = new TransformedBitmap(wBmp, new ScaleTransform(ScaleX, ScaleY));
                WriteableBitmap ScaledWritableBitmap = new WriteableBitmap(ScaledBitmap);

                wBmp = null;
                GC.Collect();
                wBmp = ScaledWritableBitmap;
            }

            glControl.MakeCurrent();
            GetTextureFromWriteableBitmap(wBmp);

            wBmp = null;
            bmpSrc = null;
            GC.Collect();

            // Regenerate Framebuffers
            for (int FrameBufferID = FrameBufferIDs.ProcessedImage; FrameBufferID < FrameBufferIDs.BufferA + (int)Engine.MultiPassBuffers; FrameBufferID++)
            {
                Engine.RegenerateFrameBuffer(FrameBufferID);
            }

            glControl.Invalidate();
            return;
        }

        private void Button_SystemInfo_Click(object sender, ExecutedRoutedEventArgs e)
        {
            SystemInfoWindow SysInfoWindow = new SystemInfoWindow(Engine);
            SysInfoWindow.Owner = this;
            SysInfoWindow.ShowDialog();
        }

        private void Button_About_Click(object sender, ExecutedRoutedEventArgs e)
        {
            AboutWindow AboutWindow = new AboutWindow();
            AboutWindow.Owner = this;
            AboutWindow.ShowDialog();
        }
        #endregion

        #region Controls
        private void ToggleButton_RenderLoop_ResetTime_Click(object sender, RoutedEventArgs e)
        {
            toggleButton_RenderLoop_ResetTime.IsChecked = false;
            ResetDrawState(true);
        }

        private void ToggleButton_RenderLoop_Play_Click(object sender, RoutedEventArgs e)
        {
            // Precess Command and anotther calls like button toggle logic
            if (sender != toggleButton_RenderLoop_Play)
            {
                toggleButton_RenderLoop_Play.IsChecked = !toggleButton_RenderLoop_Play.IsChecked.Value;
            }

            if (toggleButton_RenderLoop_Play.IsChecked == true)
            {
                RenderLoop?.Start();
            }
            else
            {
                RenderLoop?.Stop();
            }
        }

        private void BtnCompile_Click(object sender, RoutedEventArgs e)
        {
            CompileShader(false);
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            glControl.MakeCurrent();

            if (CompileShader(Settings.ForceCompileWhenApply))
            {
                RenderLoop?.Stop();

                Engine.ApplyToPhotoshop();

                if (!Program.PhotoshopRunLastFilterEnabled && SaveLastShaders())
                {
                    Program.PhotoshopRunLastFilterEnabled = true;
                }

                Program.Result = PSPluginErrorCodes.NoError;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void StatusBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (LastShaderErrors.Count > 0)
            {
                ErrorWindow ShadersErrorWindow = new ErrorWindow(LastShaderErrors);
                ShadersErrorWindow.Owner = this;
                ShadersErrorWindow.ShowDialog();
            }
        }

        #region Tabs: Image, Buffers
        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            if (tabControlMain.SelectedIndex >= TabControlMainTabIndexes.BufferA && tabControlMain.SelectedIndex <= TabControlMainTabIndexes.BufferD)
            {
                Engine.MultiPassBufferDrawMode = tabControlMain.SelectedIndex - TabControlMainTabIndexes.BufferA + MultiPassBuffersDrawMode.BufferA;
            }
            else
            {
                Engine.MultiPassBufferDrawMode = MultiPassBuffersDrawMode.NoBuffers;
            }

            glControl.Invalidate();
        }

        private void ComboBox_Image_Buffers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            Engine.MultiPassBuffers = (MultiPassBuffers)comboBox_Image_Buffers.SelectedItem;
            int MultiPassBuffersCount = (int)Engine.MultiPassBuffers;

            Visibility BufferA_Visibility = (MultiPassBuffersCount >= FrameBufferIDs.BufferA ? Visibility.Visible : Visibility.Collapsed);
            Visibility BufferB_Visibility = (MultiPassBuffersCount >= FrameBufferIDs.BufferB ? Visibility.Visible : Visibility.Collapsed);
            Visibility BufferC_Visibility = (MultiPassBuffersCount >= FrameBufferIDs.BufferC ? Visibility.Visible : Visibility.Collapsed);
            Visibility BufferD_Visibility = (MultiPassBuffersCount >= FrameBufferIDs.BufferD ? Visibility.Visible : Visibility.Collapsed);

            tabItemBufferA.Visibility = BufferA_Visibility;
            tabItemBufferB.Visibility = BufferB_Visibility;
            tabItemBufferC.Visibility = BufferC_Visibility;
            tabItemBufferD.Visibility = BufferD_Visibility;

            bufferConfig_Image_BufferA.Visibility = BufferA_Visibility;
            bufferConfig_Image_BufferB.Visibility = BufferB_Visibility;
            bufferConfig_Image_BufferC.Visibility = BufferC_Visibility;
            bufferConfig_Image_BufferD.Visibility = BufferD_Visibility;

            bufferConfig_BufferA_BufferA.Visibility = BufferA_Visibility;
            bufferConfig_BufferA_BufferB.Visibility = BufferB_Visibility;
            bufferConfig_BufferA_BufferC.Visibility = BufferC_Visibility;
            bufferConfig_BufferA_BufferD.Visibility = BufferD_Visibility;

            bufferConfig_BufferB_BufferA.Visibility = BufferA_Visibility;
            bufferConfig_BufferB_BufferB.Visibility = BufferB_Visibility;
            bufferConfig_BufferB_BufferC.Visibility = BufferC_Visibility;
            bufferConfig_BufferB_BufferD.Visibility = BufferD_Visibility;

            bufferConfig_BufferC_BufferA.Visibility = BufferA_Visibility;
            bufferConfig_BufferC_BufferB.Visibility = BufferB_Visibility;
            bufferConfig_BufferC_BufferC.Visibility = BufferC_Visibility;
            bufferConfig_BufferC_BufferD.Visibility = BufferD_Visibility;

            bufferConfig_BufferD_BufferA.Visibility = BufferA_Visibility;
            bufferConfig_BufferD_BufferB.Visibility = BufferB_Visibility;
            bufferConfig_BufferD_BufferC.Visibility = BufferC_Visibility;
            bufferConfig_BufferD_BufferD.Visibility = BufferD_Visibility;

            UpdateShaderState();
        }

        private void CheckBox_StartRenderLoopAfterLoading_Checked(object sender, RoutedEventArgs e)
        {
            checkBox_AutoCompileAfterLoading.IsChecked = true;
        }

        private void CheckBox_UseMipMaps_CheckStateChanged(object sender, RoutedEventArgs e)
        {
            if (ProgrammaticEventFlag > 0)
            {
                return;
            }

            bool UseMipMaps = checkBox_UseMipMaps.IsChecked.Value;
            bufferConfig_BufferA_Image.UseMipMaps = UseMipMaps;
            bufferConfig_BufferB_Image.UseMipMaps = UseMipMaps;
            bufferConfig_BufferC_Image.UseMipMaps = UseMipMaps;
            bufferConfig_BufferD_Image.UseMipMaps = UseMipMaps;
            bufferConfig_Image_Image.UseMipMaps = UseMipMaps;

            ShaderState.UseMipMaps = Engine.UseMipMaps = UseMipMaps;
            glControl.Invalidate();
        }

        private void ComboBox_TextureParameters_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateShaderState();
        }

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            if (sender == textEditorCommonCode) FoldingExtension.UpdateFoldings(textEditorCommonCode);
            else if (sender == textEditorImage_VS) FoldingExtension.UpdateFoldings(textEditorImage_VS);
            else if (sender == textEditorImage_FS) FoldingExtension.UpdateFoldings(textEditorImage_FS);
            else if (sender == textEditorBufferA_VS) FoldingExtension.UpdateFoldings(textEditorBufferA_VS);
            else if (sender == textEditorBufferA_FS) FoldingExtension.UpdateFoldings(textEditorBufferA_FS);
            else if (sender == textEditorBufferB_VS) FoldingExtension.UpdateFoldings(textEditorBufferB_VS);
            else if (sender == textEditorBufferB_FS) FoldingExtension.UpdateFoldings(textEditorBufferB_FS);
            else if (sender == textEditorBufferC_VS) FoldingExtension.UpdateFoldings(textEditorBufferC_VS);
            else if (sender == textEditorBufferC_FS) FoldingExtension.UpdateFoldings(textEditorBufferC_FS);
            else if (sender == textEditorBufferD_VS) FoldingExtension.UpdateFoldings(textEditorBufferD_VS);
            else if (sender == textEditorBufferD_FS) FoldingExtension.UpdateFoldings(textEditorBufferD_FS);
        }
        #endregion

        #region Settings
        private void ComboBox_PreviewViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();
            Engine.DrawMode = (DrawModes)comboBox_PreviewViewMode?.SelectedItem;
            glControl.Invalidate();
        }

        private void ComboBox_PreviewMagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();
            Engine.Preview_MagFilter = (TextureMagFilter)comboBox_MagFilter?.SelectedItem;
            glControl.Invalidate();
        }

        private void ComboBox_PreviewMinFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();
            Engine.Preview_MinFilter = (TextureMinFilter)comboBox_MinFilter?.SelectedItem;
            glControl.Invalidate();
        }

        private void CheckBox_ForceCompileWhenApply_Check(object sender, RoutedEventArgs e)
        {
            Settings.ForceCompileWhenApply = checkBox_ForceCompileWhenApply.IsChecked.Value;
        }

        private void CheckBox_sRGB_Check(object sender, RoutedEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            Engine.UseFramebufferSrgb = checkBox_sRGB.IsChecked.Value;
            glControl.Invalidate();
        }

        private void Rectangle_PreviewSplitterColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            if (Engine.RunColorPicker(Engine.PreviewSplitterColor, out ColorRGBA ResultColor))
            {
                rectangle_PreviewSplitterColor.Fill = new SolidColorBrush(ResultColor.WithoutAlpha);
                Engine.PreviewSplitterColor = ResultColor;
                glControl.Invalidate();
            }
        }

        private void Rectangle_BGColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            if (Engine.RunColorPicker(Engine.GLClearColor, out ColorRGBA ResultColor))
            {
                rectangle_BGColor.Fill = new SolidColorBrush(ResultColor.WithoutAlpha);
                Engine.GLClearColor = ResultColor;
                glControl.Invalidate();
            }
        }

        private void Rectangle_GridColor1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            if (Engine.RunColorPicker(Engine.GridColor1, out ColorRGBA ResultColor))
            {
                rectangle_GridColor1.Fill = new SolidColorBrush(ResultColor.WithoutAlpha);
                Engine.GridColor1 = ResultColor;
                glControl.Invalidate();
            }
        }

        private void Rectangle_GridColor2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Engine == null) return;

            glControl.MakeCurrent();

            if (Engine.RunColorPicker(Engine.GridColor2, out ColorRGBA ResultColor))
            {
                rectangle_GridColor2.Fill = new SolidColorBrush(ResultColor.WithoutAlpha);
                Engine.GridColor2 = ResultColor;
                glControl.Invalidate();
            }
        }

        private void TbGridSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Engine == null) return;

            if (float.TryParse(tbGridSize.Text, out float GridSize))
            {
                Engine.GridSize = GridSize;
            }

            glControl.Invalidate();
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.PreviewMagFilter = Engine.Preview_MagFilter;
            Settings.PreviewMinFilter = Engine.Preview_MinFilter;

            Settings.BackgroundColor = Engine.GLClearColor;

            Settings.PreviewSplitter.LineWidth = Engine.PreviewSplitterLineWidth;
            Settings.PreviewSplitter.LineColor = Engine.PreviewSplitterColor;

            Settings.Grid.GridColor1 = Engine.GridColor1;
            Settings.Grid.GridColor2 = Engine.GridColor2;
            Settings.Grid.GridSize = Engine.GridSize;

            Settings.Save();
        }

        private void BtnDefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings = new SettingsXML();
            SettingsApply();
        }

        private void TbGridSize_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !FloatCheckerRegex.IsMatch(e.Text);
            if ((sender as TextBox).Text.Length >= 4)
            {
                e.Handled = true; // Handled - ignore input
            }
        }

        private void TbGridSize_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string GridSizeStr = (string)e.DataObject.GetData(typeof(string));
                if (FloatCheckerRegex.IsMatch(GridSizeStr))
                {
                    return;
                }
            }

            e.CancelCommand();
        }
        #endregion
        #endregion
    }
}