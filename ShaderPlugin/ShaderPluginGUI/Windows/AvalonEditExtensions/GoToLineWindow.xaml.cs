using System;
using System.Windows;
using System.Windows.Input;

using ShaderPluginGUI;
using ICSharpCode.AvalonEdit;
using System.Windows.Controls;

namespace AvalonEditExtensions
{
    public partial class GoToLineWindow : DarkWindow
    {
        public static readonly RoutedCommand GoToLineCommand = new RoutedCommand("GoToLine", typeof(GoToLineWindow),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) }
        );

        private TextEditor TextEditor;

        public GoToLineWindow(TextEditor TextEditor)
        {
            InitializeComponent();

            this.TextEditor = TextEditor;
            if (TextEditor == null)
            {
                throw new ArgumentNullException(nameof(TextEditor));
            }

            int LinesCount = TextEditor.Document.LineCount;
            runLastLineNum.Text = LinesCount.ToString();
        }

        public static void Install(TextEditor TextEditor, Window OwnerWindow = null)
        {
            if (TextEditor == null)
            {
                throw new ArgumentNullException(nameof(TextEditor));
            }

            TextEditor.CommandBindings.Add(new CommandBinding(GoToLineCommand, (s, e) =>
            {
                GoToLineWindow Window = new GoToLineWindow(TextEditor);
                Window.Owner = OwnerWindow;
                Window.ShowDialog();
            }));
        }

        public static void Uninstall(TextEditor TextEditor)
        {
            if (TextEditor == null)
            {
                return;
            }

            for (int i = TextEditor.CommandBindings.Count - 1; i >= 0; i--)
            {
                if (TextEditor.CommandBindings[i].Command == GoToLineCommand)
                {
                    TextEditor.CommandBindings.RemoveAt(i);
                    break; // We have only one command
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ButtonCancel_Click(sender, e);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TextEditor?.TextArea.Focus();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (CanGoToLine(out int LineNumber))
            {
                GoToLine(LineNumber);
                DialogResult = true;
                Close();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void GoToLine(int LineNumber)
        {
            TextEditor.TextArea.Caret.Line = LineNumber;
            TextEditor.TextArea.Caret.Column = 0;
            TextEditor.TextArea.ClearSelection();
            TextEditor.TextArea.Caret.BringCaretToView();

            // show caret even if the editor does not have the Keyboard Focus
            TextEditor.TextArea.Caret.Show();
        }

        bool CanGoToLine(out int LineNumber)
        {
            if (int.TryParse(textBoxLineNumber.Text, out LineNumber))
            {
                int LinesCount = TextEditor.Document.LineCount;
                if (LineNumber > 0 && LineNumber <= LinesCount)
                {
                    return true;
                }
            }

            return false;
        }

        private void TextBoxLineNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void TextBoxLineNumber_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string GridSizeStr = (string)e.DataObject.GetData(typeof(string));
                if (int.TryParse(GridSizeStr, out int _))
                {
                    return;
                }
            }

            e.CancelCommand();
        }

        private void TextBoxLineNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            buttonOK.IsEnabled = CanGoToLine(out _);
        }
    }
}
