// Exercise production Excel transactions with an independent field model.
// No Excel process, desktop input, clipboard or working workbook is used.
using System;
using System.Collections.Generic;

namespace KeyJaya
{
    internal static class ExcelTransactionTests
    {
        static int assertions;
        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception("Excel transaction: " + message);
            assertions++;
        }

        sealed class Field : ExcelCellField.ICellBackend
        {
            public string Text, Mode = "Edit";
            public int Anchor, Caret, Reads, Sends, Toggles;
            public bool Valid = true, IgnoreNavigation, Readable = true;
            public Action Reading;
            bool shift, alt;
            string lineBreak;
            public bool Current
            {
                get
                {
                    return Valid;
                }
            }

            public int Start
            {
                get
                {
                    return Math.Min(Anchor, Caret);
                }
            }

            public int End
            {
                get
                {
                    return Math.Max(Anchor, Caret);
                }
            }

            public bool Read(bool mode, out ExcelCellField.CellState state)
            {
                Reads++;
                state = new ExcelCellField.CellState
                {
                    Text = Text,
                    Start = Start,
                    End = End,
                    Mode = mode ? Mode : null
                };
                if (Reading != null)
                    Reading();
                return Readable;
            }

            public bool EnterEdit()
            {
                Toggles++;
                Mode = "Edit";
                return true;
            }

            int Move(int position, bool right)
            {
                if (right)
                    return position >= Text.Length ? Text.Length : position + (Text[position] == '\r' && position + 1 < Text.Length && Text[position + 1] == '\n' ? 2 : 1);
                return position <= 0 ? 0 : position - (position > 1 && Text[position - 1] == '\n' && Text[position - 2] == '\r' ? 2 : 1);
            }

            public bool Send(Native.Input[] keys)
            {
                Sends++;
                if (lineBreak == null)
                    lineBreak = Text.Contains("\r\n") ? "\r\n" : "\n";
                Check(Valid && Mode == "Edit", "input requires the same field in Edit mode");
                foreach (var item in keys)
                {
                    var key = item.union.keyboard;
                    Check(item.type == 1 && key.extra.ToUInt64() == Native.OwnCorrectionInput, "all cell input has correction ownership");
                    bool up = (key.flags & 2) != 0;
                    if (key.vk == 0xA0)
                    {
                        shift = !up;
                        if (shift && Anchor == Caret)
                            Anchor = Caret;
                        continue;
                    }

                    if (key.vk == 0xA4)
                    {
                        alt = !up;
                        continue;
                    }

                    Check(key.vk != 0xA2 && key.vk != 0xA3, "selection cannot issue a spreadsheet Ctrl command");
                    if (up)
                        continue;
                    if (key.vk == 13)
                    {
                        Check(alt, "line insertion uses Alt+Enter, never a cell-committing Enter");
                        int begin = Start;
                        string newline = lineBreak;
                        Text = Text.Substring(0, begin) + newline + Text.Substring(End);
                        Anchor = Caret = begin + newline.Length;
                        continue;
                    }

                    if ((key.flags & 4) != 0)
                    {
                        int begin = Start;
                        Text = Text.Substring(0, begin) + (char)key.scan + Text.Substring(End);
                        Anchor = Caret = begin + 1;
                        continue;
                    }

                    if (IgnoreNavigation)
                        continue;
                    int next;
                    if (key.vk == 0x25 || key.vk == 0x27)
                    {
                        Check((key.flags & 1) != 0, "arrows retain their extended-key identity");
                        next = !shift && Start != End ? (key.vk == 0x25 ? Start : End) : Move(Caret, key.vk == 0x27);
                    }
                    else
                    {
                        Check(Text.IndexOfAny(new[] { '\r', '\n' }) < 0 && (key.vk == 0x24 || key.vk == 0x23), "multiline selection never uses line-local Home/End");
                        next = key.vk == 0x24 ? 0 : Text.Length;
                    }

                    Caret = next;
                    if (!shift)
                        Anchor = Caret;
                }

                Check(!shift && !alt, "selection/insertion releases Shift and Alt");
                return true;
            }
        }

        static ExcelCellField.Snapshot Snapshot(Field field, int start, int end)
        {
            return new ExcelCellField.Snapshot
            {
                Owner = new IntPtr(-7821),
                EditorHandle = new IntPtr(-7822),
                Backend = field,
                Full = field.Text,
                Start = start,
                End = end,
                SelectionStart = field.Start,
                SelectionEnd = field.End,
                OriginalStart = field.Start,
                OriginalEnd = field.End
            };
        }

        static void RelativeNavigation()
        {
            foreach (string text in new[]
            {
                "",
                "a\nb",
                "ab\r\ncd",
                "ā\nśā",
                "\n\n",
                "x\r\ny\nz"
            }

            )
            {
                var boundaries = new List<int>
                {
                    0
                };
                for (int n = 0; n < text.Length; n++)
                {
                    if (text[n] == '\r')
                        n++;
                    boundaries.Add(n + 1);
                }

                foreach (int fromStart in boundaries)
                    foreach (int fromEnd in boundaries)
                    {
                        if (fromStart > fromEnd)
                            continue;
                        foreach (int start in boundaries)
                            foreach (int end in boundaries)
                            {
                                if (start > end)
                                    continue;
                                foreach (bool reversed in new[]
                                {
                                    false,
                                    true
                                }

                                )
                                {
                                    var field = new Field
                                    {
                                        Text = text,
                                        Anchor = reversed ? fromEnd : fromStart,
                                        Caret = reversed ? fromStart : fromEnd
                                    };
                                    var keys = Native.RelativeFieldRangeKeys(text, fromStart, fromEnd, start, end);
                                    Check(keys != null && keys.Length <= 4 + 4 * text.Length, "valid relative selection has a finite plan");
                                    field.Send(keys);
                                    Check(field.Text == text && field.Start == start && field.End == end, "relative navigation selects the exact offsets without editing text");
                                }
                            }
                    }
            }

            foreach (string text in new[]
            {
                "a\rb",
                "a\tb",
                "e\u0301\nb",
                "😀\nb",
                "a\u200d\nb"
            }

            )
                Check(Native.RelativeFieldRangeKeys(text, text.Length, text.Length, 0, text.Length) == null, "navigation through ambiguous line breaks or character clusters produces no input");
            Check(Native.RelativeFieldRangeKeys("a\r\nb", 2, 2, 0, 1) == null, "caret inside a CRLF pair is refused");
            Check(Native.RelativeFieldRangeKeys("a\nb", 0, 0, 3, 4) == null && Native.RelativeFieldRangeKeys(new string ('a', 4097), 0, 0, 0, 1) == null, "out-of-range and oversized navigation produce no input");
        }

        static void Transactions()
        {
            foreach (string separator in new[]
            {
                "\n",
                "\r\n"
            }

            )
                foreach (bool retain in new[]
                {
                    false,
                    true
                }

                )
                    foreach (string mode in new[]
                    {
                        "Enter",
                        "Edit"
                    }

                    )
                    {
                        string original = "Earlier untouched" + separator + "prasada." + separator + "Later untouched";
                        int start = original.IndexOf("prasada", StringComparison.Ordinal);
                        var field = new Field
                        {
                            Text = original,
                            Anchor = start + 8,
                            Caret = start + 8,
                            Mode = mode
                        };
                        var snap = Snapshot(field, start, start + 7);
                        snap.Tail = ".";
                        Check(snap.Select(), "word in a later line can be selected");
                        Check(snap.Replace("prasāda", retain, () => field.Valid), "word replacement verifies the whole multiline buffer");
                        string wanted = original.Replace("prasada", "prasāda");
                        Check(field.Text == wanted && (retain ? field.Start == start && field.End == start + 7 : field.Start == start + 7 && field.End == start + 7), "only the intended word changes and retention is exact");
                        Check(field.Toggles == (mode == "Enter" ? 1 : 0), "F2 is used only for a verified Enter-to-Edit transition");
                        Check(field.Reads <= (mode == "Enter" ? 7 : 6), "selection/replacement does not repeat a redundant full-buffer proof");
                        Check(snap.Resume("prasāda", retain, true), "following typing resumes at the converted word end");
                        Check(field.Start == field.End && field.End == start + 7, "resume does not jump into another line");
                        Check(snap.Position("prasāda", 8) && field.Caret == start + 8, "caret can be positioned after the verified punctuation tail");
                    }

            foreach (string text in new[]
            {
                "earlier\nraja",
                "earlier\r\nraja"
            }

            )
            {
                var field = new Field
                {
                    Text = text,
                    Anchor = 1,
                    Caret = 1
                };
                var snap = Snapshot(field, text.Length - 4, text.Length);
                Check(snap.Select() && snap.Restore() && field.Start == 1 && field.End == 1 && field.Text == text, "cancel restores a caret in an earlier line without changing text");
            }

            foreach (string mode in new[]
            {
                "Ready",
                "Point",
                "Calculating",
                "Unknown"
            }

            )
            {
                var field = new Field
                {
                    Text = "a\nraja",
                    Anchor = 6,
                    Caret = 6,
                    Mode = mode
                };
                var snap = Snapshot(field, 2, 6);
                Check(!snap.Select() && field.Sends == 0 && field.Toggles == 0, "unknown/nonediting modes never navigate or commit a cell");
            }

            var stale = new Field
            {
                Text = "a\nraja",
                Anchor = 6,
                Caret = 6
            };
            var staleSnap = Snapshot(stale, 2, 6);
            stale.Reading = () => stale.Valid = false;
            Check(!staleSnap.Select() && stale.Sends == 0, "ownership lost during a read cannot authorize input");
            var ignored = new Field
            {
                Text = "a\nraja",
                Anchor = 6,
                Caret = 6,
                IgnoreNavigation = true
            };
            var ignoredSnap = Snapshot(ignored, 2, 6);
            Check(!ignoredSnap.Select() && !ignoredSnap.Replace("rāja", false, () => true) && ignored.Text == "a\nraja", "ignored selection cannot append converted text");
            Check(ignoredSnap.Restore() && ignored.Caret == 6, "failed unchanged selection preserves its original caret");
            var cancelled = new Field
            {
                Text = "a\nraja",
                Anchor = 6,
                Caret = 6
            };
            var cancelledSnap = Snapshot(cancelled, 2, 6);
            Check(cancelledSnap.Select(), "cancellation fixture selects its word");
            int contexts = 0, sends = cancelled.Sends;
            Check(!cancelledSnap.Replace("rāja", false, () => ++contexts == 1) && cancelled.Sends == sends && cancelled.Text == "a\nraja", "context lost after mode verification prevents replacement");
            var formula = new Field
            {
                Text = "raja",
                Anchor = 4,
                Caret = 4
            };
            var formulaSnap = Snapshot(formula, 0, 4);
            Check(formulaSnap.Select(), "formula-output fixture selects plain text");
            sends = formula.Sends;
            Check(!formulaSnap.Replace("=1+2", false, () => true) && formula.Sends == sends && formula.Text == "raja", "replacement cannot create a formula");
            Check(formulaSnap.Replace("two\nlines", false, () => true) && formula.Text == "two\nlines", "multiline output uses Excel line insertion inside the same cell");
            var complex = new Field
            {
                Text = "a\ne\u0301",
                Anchor = 4,
                Caret = 4
            };
            Check(!Snapshot(complex, 2, 4).Select() && complex.Sends == 0, "unproven cluster navigation skips before changing selection");
        }

        static void Discovery()
        {
            foreach (bool selected in new[]
            {
                false,
                true
            }

            )
            {
                var snap = new ExcelCellField.Snapshot
                {
                    Owner = new IntPtr(-7821),
                    EditorHandle = new IntPtr(-7822),
                    Full = "Earlier prasada",
                    Start = selected ? 8 : 15,
                    End = 15,
                    SelectionStart = selected ? 8 : 15,
                    SelectionEnd = 15
                };
                int captures = 0;
                bool present;
                SafeEdit.CaretTarget caret;
                var edit = SafeEdit.ReadExcelDiscovery(() =>
                {
                    captures++;
                    return snap.Clone();
                }, out present, out caret);
                Check(captures == 1 && present == selected, "word discovery captures the cell exactly once");
                Check(selected ? edit != null && edit.UserSelection && caret == null : edit == null && caret != null && caret.Text == "prasada", "one capture resolves either selection or the existing word");
                captures = 0;
                var insertion = SafeEdit.ReadExcelInsertionOrSelection(() =>
                {
                    captures++;
                    return snap.Clone();
                });
                Check(captures == 1 && insertion != null && insertion.UserSelection == selected, "clipboard discovery resolves selection/insertion from one capture");
            }

            bool missing;
            SafeEdit.CaretTarget absent;
            Check(SafeEdit.ReadExcelDiscovery(() => null, out missing, out absent) == null && missing && absent == null, "failed capture is ambiguous rather than a usable empty caret");
            var multiline = new ExcelCellField.Snapshot
            {
                Owner = new IntPtr(-7821),
                EditorHandle = new IntPtr(-7822),
                Full = "prasada\nKrsna",
                Start = 0,
                End = 13,
                SelectionStart = 0,
                SelectionEnd = 13
            };
            var selectedLines = SafeEdit.ReadExcelDiscovery(() => multiline.Clone(), out missing, out absent);
            Check(selectedLines != null && selectedLines.UserSelection && missing && absent == null, "selected multiline text is recognized as a selection");
            Check(SafeEdit.ReadExcelInsertionOrSelection(() => multiline.Clone()) != null, "clipboard discovery preserves a supported multiline selection");
        }

        static void Passages(string root)
        {
            using (var core = new Core(root))
                foreach (string newline in new[]
                {
                    "\n",
                    "\r\n"
                }

                )
                    foreach (string encoding in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        string input = "prasada" + newline + "Krsna";
                        string output = core.ConvertHighlighted(input, encoding);
                        string expected = "prasāda" + newline + "Kṛṣṇa";
                        if (encoding == "balaram")
                            expected = EncodingMap.Balaram(expected);
                        Check(output == expected, "conversion retains the source line boundaries");
                        var field = new Field
                        {
                            Text = "Prefix " + input + " suffix",
                            Anchor = 7,
                            Caret = 7 + input.Length
                        };
                        var snap = Snapshot(field, 7, 7 + input.Length);
                        Check(snap.Select() && snap.Replace(output, true, () => true), "selected lines convert through the production adapter: " + encoding + "/" + newline.Length);
                        Check(field.Text == "Prefix " + expected + " suffix" && field.Start == 7 && field.End == 7 + output.Length, "line breaks, neighbors and retained selection are exact");
                    }

            foreach (string prefix in new[]
            {
                "😀 ",
                "e\u0301 ",
                "👩\u200d💻\n"
            }

            )
            {
                var field = new Field
                {
                    Text = prefix + "raja tail",
                    Anchor = prefix.Length + 4,
                    Caret = prefix.Length + 4
                };
                var snap = Snapshot(field, prefix.Length, prefix.Length + 4);
                Check(snap.Select() && snap.Replace("rāja", true, () => true), "untraversed emoji/combining text does not block a nearby word");
                Check(field.Text == prefix + "rāja tail", "complex surrounding characters stay byte-for-byte unchanged");
            }

            foreach (string script in new[]
            {
                "devanagari",
                "bengali",
                "itrans",
                "plain"
            }

            )
                using (var core = new Core(root))
                {
                    var field = new Field
                    {
                        Text = "Krsna",
                        Anchor = 0,
                        Caret = 5
                    };
                    var snap = Snapshot(field, 0, 5);
                    string output = core.ConvertHighlighted("Krsna", script);
                    Check(snap.Select() && snap.Replace(output, true, () => true), "existing single-line script conversion is preserved: " + script);
                    Check(field.Text == output && field.Start == 0 && field.End == output.Length, "script conversion retains the full selected buffer: " + script);
                }

            string longText = new string ('x', 1800) + " raja " + new string ('y', 1800);
            var nearby = ExcelCellField.SelectionKeys(longText, 1805, 1805, 1801, 1805);
            Check(nearby != null && nearby.Length == 10, "a nearby word in a long cell needs only its own selection steps");
            foreach (string invalid in new[]
            {
                "bad\ttext",
                "bad\rtext",
                "😀",
                new string ('x', 4097)
            }

            )
                Check(Native.ExcelTextKeys(invalid) == null, "unsupported insertion generates no input");
            foreach (string text in new[]
            {
                "a\nb",
                "a\r\nb",
                "ā\nKṛṣṇa"
            }

            )
            {
                var field = new Field
                {
                    Text = "",
                    Anchor = 0,
                    Caret = 0
                };
                var keys = Native.ExcelTextKeys(text);
                Check(keys != null, "valid cell lines have a finite input plan");
                field.Send(keys);
                Check(field.Text == text.Replace("\r\n", "\n"), "line input produces cell text rather than a committed row");
            }
        }

        public static int Run(string root)
        {
            RelativeNavigation();
            Transactions();
            Discovery();
            Passages(root);
            Console.WriteLine("Excel transactions: " + assertions + " assertions; relative multiline selection, real transaction guards and one-pass discovery; no desktop input.");
            return assertions;
        }
    }
}
