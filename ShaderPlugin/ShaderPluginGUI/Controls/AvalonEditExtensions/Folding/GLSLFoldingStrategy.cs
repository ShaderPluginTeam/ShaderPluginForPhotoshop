using System;
using System.Collections.Generic;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace AvalonEditExtensions
{
    public static class GLSLFoldingStrategy
    {
        public static void UpdateFoldings(FoldingManager Manager, TextDocument Document)
        {
            try
            {
                List<NewFolding> Foldings = CreateFoldings(Document, out int FirstErrorOffset);
                Manager?.UpdateFoldings(Foldings, FirstErrorOffset);
            }
            catch { }
        }

        public static void CollapseCommentFoldings(FoldingManager FManager)
        {
            foreach (FoldingSection Folding in FManager.AllFoldings)
            {
                if (Folding != null && !Folding.IsFolded)
                {
                    if (Folding.Title.Contains(@"//") || Folding.Title.Contains(@"/*"))
                    {
                        Folding.IsFolded = true;
                    }
                }
            }
        }

        static List<NewFolding> CreateFoldings(TextDocument Document, out int FirstErrorOffset)
        {
            FirstErrorOffset = -1;
            List<NewFolding> Foldings = new List<NewFolding>();

            Stack<int> FoldingStack_Region = new Stack<int>();
            Stack<int> FoldingStack_PragmaRegion = new Stack<int>();
            Stack<int> FoldingStack_PreProcCondition = new Stack<int>();
            Stack<int> FoldingStack_CurlyBrace = new Stack<int>();
            Stack<int> FoldingStack_Parentheses = new Stack<int>();
            List<int> SingleLineComments = new List<int>();

            int NewLineIndex = -1;
            string Text = Document.Text + '\0';

            for (int CharOffset = 0; CharOffset < Text.Length - 1; CharOffset++)
            {
                char CharCurrent = Text[CharOffset];
                char CharNext = Text[CharOffset + 1];

                if (CharCurrent == '/' && CharNext == '*')
                {
                    int MultiLineCommentStart = CharOffset;
                    for (int InnerOffset = CharOffset + 2; InnerOffset < Text.Length - 1; InnerOffset++)
                    {
                        CharCurrent = Text[InnerOffset];
                        CharNext = Text[InnerOffset + 1];

                        if (CharCurrent == '*' && CharNext == '/')
                        {
                            int MultiLineCommentEnd = InnerOffset + 2;
                            if (IsOffsetsInDifferentLines(Document, MultiLineCommentStart, MultiLineCommentEnd))
                            {
                                int FirstLineEndOffset = Document.GetLineByOffset(MultiLineCommentStart).EndOffset;
                                Foldings.Add(new NewFolding(MultiLineCommentStart, MultiLineCommentEnd)
                                {
                                    Name = GetFoldingName(Text, MultiLineCommentStart, FirstLineEndOffset, true)
                                });
                            }
                            CharOffset = InnerOffset + 1;
                            break;
                        }
                    }
                    continue;
                }

                if (CharCurrent == '\n')
                {
                    NewLineIndex = CharOffset;
                    continue;
                }

                if (CharCurrent == '/' && CharNext == '/' && OnlySpaceSymbolsInRange(Text, NewLineIndex + 1, CharOffset - 1))
                {
                    int LineEndOffset = Document.GetLineByOffset(CharOffset).EndOffset;
                    int EndRegionOffset = Text.IndexOf("endregion", CharOffset, LineEndOffset - CharOffset, StringComparison.InvariantCultureIgnoreCase);
                    if (EndRegionOffset > 0 && FoldingStack_Region.Count > 0 && OnlySpaceSymbolsInRange(Text, CharOffset + 2, EndRegionOffset))
                    {
                        int LastRegionOffset = FoldingStack_Region.Pop();
                        CharOffset = Document.GetLineByOffset(EndRegionOffset).EndOffset - 1;
                        Foldings.Add(new NewFolding(LastRegionOffset, LineEndOffset)
                        {
                            Name = GetFoldingName(Text, LastRegionOffset, LineEndOffset, false)
                        });
                        continue;
                    }

                    int RegionOffset = Text.IndexOf("region", CharOffset, LineEndOffset - CharOffset, StringComparison.InvariantCultureIgnoreCase);
                    if (RegionOffset > 0 && OnlySpaceSymbolsInRange(Text, CharOffset + 2, RegionOffset))
                    {
                        FoldingStack_Region.Push(CharOffset);
                        CharOffset = Document.GetLineByOffset(RegionOffset).EndOffset - 1;
                        continue;
                    }

                    SingleLineComments.Add(NewLineIndex + 1);
                    CharOffset++;
                    continue;
                }

                if (CharCurrent == '{')
                {
                    if (OnlySpaceSymbolsInRange(Text, NewLineIndex + 1, CharOffset - 1) && CanCollapseNearLastWord(Document, Text, NewLineIndex))
                    {
                        FoldingStack_CurlyBrace.Push(Document.GetLineByOffset(NewLineIndex).EndOffset);
                    }
                    else
                    {
                        FoldingStack_CurlyBrace.Push(CharOffset);
                    }
                    continue;
                }

                if (CharCurrent == '}')
                {
                    if (FoldingStack_CurlyBrace.Count > 0)
                    {
                        int LeftBraceOffset = FoldingStack_CurlyBrace.Pop();
                        if (IsOffsetsInDifferentLines(Document, LeftBraceOffset, CharOffset))
                        {
                            Foldings.Add(new NewFolding(LeftBraceOffset, CharOffset + 1) { Name = "..." });
                        }
                    }
                    else if (FirstErrorOffset < 0)
                    {
                        FirstErrorOffset = CharCurrent;
                    }
                    continue;
                }

                /*if (CharCurrent == '(')
                {
                    if (OnlySpaceSymbolsInRange(Text, NewLineIndex + 1, CharOffset - 1))
                    {
                        FoldingStack_Parentheses.Push(Document.GetLineByOffset(NewLineIndex).EndOffset);
                    }
                    else
                    {
                        FoldingStack_Parentheses.Push(CharOffset);
                    }
                    continue;
                }

                if (CharCurrent == ')')
                {
                    if (FoldingStack_Parentheses.Count > 0)
                    {
                        int LeftBraceOffset = FoldingStack_Parentheses.Pop();
                        if (IsOffsetsInDifferentLines(Document, LeftBraceOffset, CharOffset))
                        {
                            Foldings.Add(new NewFolding(LeftBraceOffset, CharOffset + 1) { Name = "( ... )" });
                        }
                    }
                    else if (FirstErrorOffset < 0)
                    {
                        FirstErrorOffset = CharCurrent;
                    }
                    continue;
                }*/

                if (CharCurrent == '#')
                {
                    if (OnlySpaceSymbolsInRange(Text, NewLineIndex + 1, CharOffset - 1))
                    {
                        int PreProcStartOffset = CharOffset + 1;
                        int LineEndOffset = Document.GetLineByOffset(CharOffset).EndOffset;

                        // #pragma
                        int PragmaOffset = Text.IndexOf("pragma", PreProcStartOffset, Math.Min(6, LineEndOffset - PreProcStartOffset));
                        if (PragmaOffset > 0)
                        {
                            int PragmaEndOffset = PragmaOffset + 6; // Offset after "pragma" word
                            int EndRegionOffset = Text.IndexOf("endregion", PragmaEndOffset, LineEndOffset - PragmaEndOffset);
                            if (EndRegionOffset > 0 && FoldingStack_PragmaRegion.Count > 0 && OnlySpaceSymbolsInRange(Text, PragmaEndOffset, EndRegionOffset))
                            {
                                int LastRegionOffset = FoldingStack_PragmaRegion.Pop();
                                Foldings.Add(new NewFolding(LastRegionOffset, LineEndOffset)
                                {
                                    Name = GetFoldingName(Text, LastRegionOffset, LineEndOffset, false)
                                });
                                CharOffset = Document.GetLineByOffset(EndRegionOffset).EndOffset - 1;
                                continue;
                            }

                            int RegionOffset = Text.IndexOf("region", PragmaEndOffset, LineEndOffset - PragmaEndOffset);
                            if (RegionOffset > 0 && OnlySpaceSymbolsInRange(Text, PragmaEndOffset, RegionOffset))
                            {
                                FoldingStack_PragmaRegion.Push(CharOffset);
                                CharOffset = Document.GetLineByOffset(RegionOffset).EndOffset - 1;
                                continue;
                            }

                            CharOffset = PragmaEndOffset;
                            continue;
                        }

                        // #if, #ifdef
                        int IfOffset = Text.IndexOf("if", PreProcStartOffset, Math.Min(2, LineEndOffset - PreProcStartOffset));
                        if (IfOffset > 0)
                        {
                            FoldingStack_PreProcCondition.Push(LineEndOffset);
                            CharOffset = Document.GetLineByOffset(CharOffset).EndOffset - 1;
                            continue;
                        }

                        // #else, #elif
                        int ElseOffset = Text.IndexOf("el", PreProcStartOffset, Math.Min(2, LineEndOffset - PreProcStartOffset));
                        if (ElseOffset > 0)
                        {
                            if (FoldingStack_PreProcCondition.Count > 0)
                            {
                                int PreProcConditionOffset = FoldingStack_PreProcCondition.Pop();
                                int PrewLineEndOffset = Document.GetLineByOffset(NewLineIndex).EndOffset;
                                Foldings.Add(new NewFolding(PreProcConditionOffset, PrewLineEndOffset) { Name = "..." });
                            }
                            else if (FirstErrorOffset < 0)
                            {
                                FirstErrorOffset = CharCurrent;
                            }

                            FoldingStack_PreProcCondition.Push(LineEndOffset);
                            CharOffset = Document.GetLineByOffset(ElseOffset).EndOffset - 1;
                            continue;
                        }

                        // #endif
                        int EndIfOffset = Text.IndexOf("endif", PreProcStartOffset, Math.Min(5, LineEndOffset - PreProcStartOffset));
                        if (EndIfOffset > 0)
                        {
                            if (FoldingStack_PreProcCondition.Count > 0)
                            {
                                int PreProcConditionOffset = FoldingStack_PreProcCondition.Pop();
                                Foldings.Add(new NewFolding(PreProcConditionOffset, LineEndOffset) { Name = "..." });
                            }
                            else if (FirstErrorOffset < 0)
                            {
                                FirstErrorOffset = CharCurrent;
                            }

                            CharOffset = Document.GetLineByOffset(EndIfOffset).EndOffset - 1;
                            continue;
                        }
                    }

                    CharOffset = Document.GetLineByOffset(CharOffset).EndOffset - 1;
                    continue;
                }
            }

            // Single Line Comments
            CreateSingleLineCommentFoldings(Document, Text, Foldings, SingleLineComments);

            if (Foldings.Count > 0)
            {
                Foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            }

            return Foldings;
        }

        static bool OnlySpaceSymbolsInRange(string Text, int StartOffset, int EndOffset)
        {
            for (int CharIndex = StartOffset; CharIndex < EndOffset; CharIndex++)
            {
                if (!Char.IsWhiteSpace(Text[CharIndex]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool CanCollapseNearLastWord(TextDocument Document, string Text, int EndOffset)
        {
            DocumentLine Line = Document.GetLineByOffset(EndOffset);
            for (int CharIndex = Line.EndOffset; CharIndex >= Line.Offset; --CharIndex)
            {
                char CurrentChar = Text[CharIndex];
                if (!Char.IsWhiteSpace(CurrentChar))
                {
                    return Text.LastIndexOf(';', Line.EndOffset, Line.Length) < 0;
                }
            }

            return false;
        }

        static bool IsOffsetsInDifferentLines(TextDocument Document, int TextOffsetA, int TextOffsetB)
        {
            if (TextOffsetA == TextOffsetB)
            {
                return true;
            }

            return Document.GetLineByOffset(TextOffsetA).LineNumber != Document.GetLineByOffset(TextOffsetB).LineNumber;
        }

        static void CreateSingleLineCommentFoldings(TextDocument Document, string Text, List<NewFolding> Foldings, List<int> SingleLineComments)
        {
            if (SingleLineComments.Count < 2)
            {
                return;
            }

            int GroupStartIndex = 0;
            DocumentLine PrevLine = Document.GetLineByOffset(SingleLineComments[0]);

            for (int i = 1; i <= SingleLineComments.Count; i++)
            {
                DocumentLine CurrentLine = i < SingleLineComments.Count ? Document.GetLineByOffset(SingleLineComments[i]) : null;
                bool isGroupEnd = CurrentLine == null || (CurrentLine.LineNumber != PrevLine.LineNumber + 1);
                if (isGroupEnd)
                {
                    int GroupLength = i - GroupStartIndex;
                    if (GroupLength > 1)
                    {
                        int StartOffset = SingleLineComments[GroupStartIndex];
                        DocumentLine GroupStartLine = Document.GetLineByOffset(StartOffset);
                        Foldings.Add(new NewFolding(GroupStartLine.Offset, PrevLine.EndOffset)
                        {
                            Name = GetFoldingName(Text, GroupStartLine.Offset, GroupStartLine.EndOffset, true)
                        });
                    }

                    GroupStartIndex = i;
                }

                PrevLine = CurrentLine;
            }
        }

        static string GetFoldingName(string Text, int StartOffset, int EndOffset, bool ForceAddThreeDots)
        {
            const int MaxFoldingNameLen = 76;

            int Length = EndOffset - StartOffset;
            if (Length > 0)
            {
                string FoldingName = Text.Substring(StartOffset, Math.Min(Length, MaxFoldingNameLen));
                if (Length > MaxFoldingNameLen || ForceAddThreeDots)
                {
                    return FoldingName + " ...";
                }
                else
                {
                    return FoldingName;
                }
            }

            return "...";
        }
    }
}