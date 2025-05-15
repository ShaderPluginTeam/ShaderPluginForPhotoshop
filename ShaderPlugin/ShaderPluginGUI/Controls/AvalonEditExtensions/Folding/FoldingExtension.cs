using System;
using System.Collections.Generic;
using System.Windows.Input;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace AvalonEditExtensions
{
    public class FoldingExtension
    {
        public Dictionary<TextEditor, FoldingManager> FoldingManagers = new Dictionary<TextEditor, FoldingManager>();
        public static readonly RoutedCommand FoldSelectedCommand = new RoutedCommand("FoldSelected", typeof(FoldingExtension), new InputGestureCollection
        {
            new KeyGesture(Key.M, ModifierKeys.Control)
        });

        public void Install(TextEditor TextEditor)
        {
            if (TextEditor == null)
            {
                throw new ArgumentNullException(nameof(TextEditor));
            }

            if (FoldingManagers.ContainsKey(TextEditor))
            {
                return;
            }

            FoldingManagers.Add(TextEditor, FoldingManager.Install(TextEditor.TextArea));

            TextEditor.CommandBindings.Add(new CommandBinding(FoldSelectedCommand, (sender, args) =>
            {
                if (sender is TextEditor textEditor)
                {
                    FoldSelected(textEditor, true);
                }
            }));
        }

        public void Uninstall(TextEditor TextEditor)
        {
            if (TextEditor == null)
            {
                return;
            }

            if (FoldingManagers.TryGetValue(TextEditor, out var FoldingManager))
            {
                FoldingManager.Uninstall(FoldingManager);
                FoldingManagers.Remove(TextEditor);
            }

            for (int i = TextEditor.CommandBindings.Count - 1; i >= 0; i--)
            {
                if (TextEditor.CommandBindings[i].Command == FoldSelectedCommand)
                {
                    TextEditor.CommandBindings.RemoveAt(i);
                    break; // We have only one command yet
                }
            }
        }

        public void UpdateFoldings(TextEditor TextEditor)
        {
            if (TextEditor == null)
            {
                throw new ArgumentNullException(nameof(TextEditor));
            }

            if (FoldingManagers.TryGetValue(TextEditor, out var FoldingManager))
            {
                GLSLFoldingStrategy.UpdateFoldings(FoldingManager, TextEditor.Document);
            }
        }

        public void CollapseCommentFoldings(TextEditor TextEditor)
        {
            if (TextEditor == null)
            {
                throw new ArgumentNullException(nameof(TextEditor));
            }

            if (FoldingManagers.TryGetValue(TextEditor, out var FoldingManager))
            {
                GLSLFoldingStrategy.CollapseCommentFoldings(FoldingManager);
            }
        }

        public void CollapseCommentFoldings(params TextEditor[] TextEditors)
        {
            foreach (var TextEditor in TextEditors)
            {
                CollapseCommentFoldings(TextEditor);
            }
        }

        private void FoldSelected(TextEditor TextEditor, bool CollapseOnlyRootLevel)
        {
            FoldingManager Manager = FoldingManagers.ContainsKey(TextEditor) ? FoldingManagers[TextEditor] : null;
            if (Manager == null)
            {
                return;
            }

            int StartOffset, EndOffset;
            if (TextEditor.SelectionLength > 0)
            {
                StartOffset = TextEditor.Document.GetLineByOffset(TextEditor.SelectionStart).Offset;
                DocumentLine EndOffsetLine = TextEditor.Document.GetLineByOffset(TextEditor.SelectionStart + TextEditor.SelectionLength);
                EndOffset = EndOffsetLine.EndOffset + EndOffsetLine.DelimiterLength;
            }
            else // All Document
            {
                StartOffset = 0;
                EndOffset = TextEditor.Text.Length;
            }

            bool FirstFolding = true;
            bool NeededFoldingState = true;

            foreach (FoldingSection Folding in Manager.AllFoldings)
            {
                if (Folding.StartOffset > EndOffset)
                {
                    break;
                }

                if (Folding.StartOffset < StartOffset || Folding.EndOffset > EndOffset)
                {
                    continue;
                }

                if (FirstFolding)
                {
                    FirstFolding = false;
                    NeededFoldingState = Folding.IsFolded = !Folding.IsFolded;
                }
                else
                {
                    Folding.IsFolded = NeededFoldingState;
                }

                if (CollapseOnlyRootLevel)
                {
                    StartOffset = Folding.EndOffset + 1;
                }
            }
        }
    }
}
