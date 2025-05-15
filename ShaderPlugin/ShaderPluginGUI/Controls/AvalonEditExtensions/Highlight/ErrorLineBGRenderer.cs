using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace AvalonEditExtensions
{
    public class ErrorLineBackgroundRenderer : IBackgroundRenderer
    {
        static SolidColorBrush ErrorBrush = new SolidColorBrush(Color.FromArgb(0xA0, 0xA0, 0x30, 0x30));
        static Pen ErrorPen = new Pen(new SolidColorBrush(Color.FromArgb(0xA0, 0xC0, 0x30, 0x30)), 1.0f);

        private readonly TextEditor Editor;
        private readonly Dictionary<TextAnchor, string> ErrorAnchors = new Dictionary<TextAnchor, string>();

        public ErrorLineBackgroundRenderer(TextEditor Editor)
        {
            this.Editor = Editor;
            this.Editor.TextArea.TextView.BackgroundRenderers.Add(this);
            InitializeErrorTooltip();
        }

        private void InitializeErrorTooltip()
        {
            ToolTip tooltip = new ToolTip();
            tooltip.Style = Editor.FindResource("ToolTipErrorLine") as Style;

            Editor.ToolTip = tooltip;
            Editor.TextArea.Unloaded += (s, e) => tooltip.IsOpen = false;
            Editor.TextArea.LostFocus += (s, e) => tooltip.IsOpen = false;
            Editor.TextArea.MouseLeave += (s, e) => tooltip.IsOpen = false;
            Editor.TextArea.MouseMove += (s, e) =>
            {
                TextViewPosition? TextViewPos = Editor.GetPositionFromPoint(e.GetPosition(Editor));
                if (TextViewPos == null)
                {
                    return;
                }

                int Offset = Editor.Document.GetOffset(TextViewPos.Value.Line, 1);
                foreach (var kvp in ErrorAnchors)
                {
                    TextAnchor Anchor = kvp.Key;
                    if (Anchor == null || Anchor.IsDeleted)
                    {
                        continue;
                    }

                    DocumentLine Line = Editor.Document.GetLineByOffset(Anchor.Offset);
                    if (Offset >= Line.Offset && Offset <= Line.EndOffset)
                    {
                        tooltip.Content = kvp.Value;
                        tooltip.IsOpen = true;
                        return;
                    }
                }

                tooltip.IsOpen = false;
            };
        }

        void IBackgroundRenderer.Draw(TextView textView, DrawingContext drawingContext)
        {
            if (ErrorAnchors.Count == 0)
            {
                return;
            }

            textView.EnsureVisualLines();
            foreach (TextAnchor ErrorAnchor in ErrorAnchors.Keys)
            {
                if (ErrorAnchor == null || ErrorAnchor.IsDeleted)
                {
                    continue;
                }

                DocumentLine Line = Editor.Document.GetLineByOffset(ErrorAnchor.Offset);
                Rect LineRect = BackgroundGeometryBuilder.GetRectsForSegment(textView, Line).FirstOrDefault();
                LineRect.Width = textView.HorizontalOffset + textView.ActualWidth;

                if (LineRect != Rect.Empty)
                {
                    drawingContext.DrawRectangle(ErrorBrush, ErrorPen, LineRect);
                }
            }
        }

        public void SetErrorLines(Dictionary<int, string> ErrorLines)
        {
            ErrorAnchors.Clear();

            TextDocument Document = Editor.Document;
            foreach (var kvp in ErrorLines)
            {
                DocumentLine Line = Document.GetLineByNumber(kvp.Key);
                TextAnchor Anchor = Document.CreateAnchor(Line.Offset);
                Anchor.MovementType = AnchorMovementType.AfterInsertion;
                Anchor.SurviveDeletion = true;
                ErrorAnchors.Add(Anchor, kvp.Value);
            }

            Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        public void ClearErrors()
        {
            ErrorAnchors.Clear();
            Editor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        public KnownLayer Layer => KnownLayer.Background;
    }
}
