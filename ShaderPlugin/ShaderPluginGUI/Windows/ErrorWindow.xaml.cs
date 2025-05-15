using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using AvalonEditExtensions;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;

namespace ShaderPluginGUI
{
    public partial class ErrorWindow : DarkWindow
    {
        public ErrorWindow(List<ShaderError> ShaderErrors) : base()
        {
            InitializeComponent();

            #region Install AvalonEdit Extensions
            void InstallAvalonEditExtensions(TextEditor TextEditor)
            {
                SearchPanel.Install(TextEditor);
                GoToLineWindow.Install(TextEditor, this);
            }

            InstallAvalonEditExtensions(textEditorImage);
            InstallAvalonEditExtensions(textEditorBufferA);
            InstallAvalonEditExtensions(textEditorBufferB);
            InstallAvalonEditExtensions(textEditorBufferC);
            InstallAvalonEditExtensions(textEditorBufferD);
            InstallAvalonEditExtensions(textEditorCommonCode);
            #endregion

            GenerateShaderLog(ShaderErrors);

            #region Tab Visibility
            tabItemImage.Visibility = textEditorImage.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            tabItemBufferA.Visibility = textEditorBufferA.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            tabItemBufferB.Visibility = textEditorBufferB.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            tabItemBufferC.Visibility = textEditorBufferC.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            tabItemBufferD.Visibility = textEditorBufferD.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            tabItemCommon.Visibility = textEditorCommonCode.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (tabItemImage.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemImage;
            }
            else if (tabItemBufferA.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemBufferA;
            }
            else if (tabItemBufferB.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemBufferB;
            }
            else if (tabItemBufferC.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemBufferC;
            }
            else if (tabItemBufferD.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemBufferD;
            }
            else if (tabItemCommon.Visibility == Visibility.Visible)
            {
                tabControlErrors.SelectedItem = tabItemCommon;
            }
            #endregion Tab Visibility
        }

        void GenerateShaderLog(List<ShaderError> ShaderErrors)
        {
            foreach (ShaderError Error in ShaderErrors)
            {
                string ErrorStr = $"\"{Error.ShaderName}\" {Error.ErrorType} Info:{Environment.NewLine}{Error.InfoLog}{Environment.NewLine}";

                if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.Image])
                {
                    textEditorImage.Text += ErrorStr;
                }
                else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferA])
                {
                    textEditorBufferA.Text += ErrorStr;
                }
                else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferB])
                {
                    textEditorBufferB.Text += ErrorStr;
                }
                else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferC])
                {
                    textEditorBufferC.Text += ErrorStr;
                }
                else if (Error.ShaderName == ShaderIDs.Names[ShaderIDs.BufferD])
                {
                    textEditorBufferD.Text += ErrorStr;
                }
                else
                {
                    textEditorCommonCode.Text += ErrorStr;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
