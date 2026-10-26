// Noninteractive regressions: single-letter cycles, caret ownership, and original-letter undo.
// Run: .\test.ps1 -Suite SingleLetterTests. Fixtures live under out/; no resident hooks are started.
using System;

namespace KeyJaya
{
    public static class SingleLetterTests
    {
        static int passed, failed;
        static void Check(bool ok, string label)
        {
            if (ok)
                passed++;
            else
            {
                failed++;
                Console.WriteLine("FAIL " + label);
            }
        }

        static void Equal(string actual, string expected, string label)
        {
            Check(actual == expected, label + " expected " + expected + " got " + actual);
        }

        public static int Main(string[] args)
        {
            uint[] conversionKeys =
            {
                0xBE,
                0xBC,
                0xBF,
                0xDC,
                0xDB,
                0xDD
            };
            string[] conversionModes =
            {
                "unicode",
                "balaram",
                "plain",
                "itrans",
                "devanagari",
                "bengali"
            };
            for (int i = 0; i < conversionKeys.Length; i++)
            {
                uint key = conversionKeys[i];
                Equal(ConversionShortcuts.Mode(key, true, false, false, false), conversionModes[i], "Ctrl-only output mapping");
                for (int bits = 0; bits < 16; bits++)
                {
                    bool ctrl = (bits & 1) != 0, shift = (bits & 2) != 0, alt = (bits & 4) != 0, win = (bits & 8) != 0;
                    Check((ConversionShortcuts.Mode(key, ctrl, shift, alt, win) != null) == (bits == 1), "only exact Ctrl chord claimed");
                }

                var gate = new ConversionShortcuts();
                string dispatch;
                Check(gate.Handle(key, true, false, conversionModes[i], false, out dispatch) && dispatch == conversionModes[i], "key-down consumed instead of reaching Word");
                Check(gate.Handle(key, true, false, conversionModes[i], false, out dispatch) && dispatch == null, "autorepeat consumed without duplicate conversion");
                Check(gate.Handle(key, false, true, null, false, out dispatch) && dispatch == null, "key-up consumed after Ctrl released");
                Check(!gate.Handle(key, false, true, null, false, out dispatch), "unmatched key-up passes");
                Check(!gate.Handle(key, true, false, conversionModes[i], true, out dispatch), "paused app leaves Word shortcut alone");
                Check(!gate.Handle(key, true, false, null, false, out dispatch), "unmodified punctuation passes");
            }

            foreach (uint key in new uint[]
            {
                0x43,
                0x56,
                0x58,
                0x5A,
                0x41,
                0x4E
            }

            )
                Check(ConversionShortcuts.Mode(key, true, false, false, false) == null, "normal Ctrl editing shortcuts preserved");
            var shortcutTap = new LeftControlTap();
            shortcutTap.Down(new IntPtr(1), 10, DateTime.UtcNow);
            shortcutTap.Cancel();
            Check(!shortcutTap.Up(new IntPtr(1), 10, DateTime.UtcNow), "conversion chord cannot also trigger bare Left Ctrl word action");
            string[] unicode =
            {
                "aā",
                "iī",
                "uū",
                "rṛṝ",
                "lḷḹ",
                "nṇṅñ",
                "sṣś",
                "tṭ",
                "dḍ",
                "hḥ",
                "mṃṁ"
            };
            string[] balaram =
            {
                "aä",
                "ié",
                "uü",
                "råè",
                "lÿý",
                "nëìï",
                "sñç",
                "tö",
                "dò",
                "hù",
                "mà"
            };
            string[] balaramUpper =
            {
                "AÄ",
                "IÉ",
                "UÜ",
                "RÅÈ",
                "LßÝ",
                "NËÌÏ",
                "SÑÇ",
                "TÖ",
                "DÒ",
                "HÙ",
                "MÀ"
            };
            for (int format = 0; format < 2; format++)
                for (int upper = 0; upper < 2; upper++)
                    for (int i = 0; i < unicode.Length; i++)
                    {
                        string cycleMode = format == 0 ? "unicode" : "balaram", row = format == 0 ? (upper == 1 ? unicode[i].ToUpperInvariant() : unicode[i]) : (upper == 1 ? balaramUpper[i] : balaram[i]);
                        string value = row[0].ToString();
                        for (int round = 0; round < 5; round++)
                            for (int n = 0; n < row.Length; n++)
                            {
                                Check(SingleLetters.Supported(value), "supported " + value);
                                value = SingleLetters.Next(value, cycleMode);
                                Equal(value, row[(n + 1) % row.Length].ToString(), "cycle " + cycleMode);
                            }
                    }

            foreach (string value in new[]
            {
                null,
                "",
                "ab",
                "a\u0304",
                "😀",
                "e",
                "o",
                "z",
                "1",
                " ",
                "`"
            }

            )
            {
                Check(!SingleLetters.Supported(value), "reject unsupported input");
                Equal(SingleLetters.Next(value, "unicode"), null, "unsupported has no cycle");
            }

            Equal(SingleLetters.Next("ā", "balaram", "unicode"), "ä", "switch Unicode to Balaram");
            Equal(SingleLetters.Next("ä", "unicode", "balaram"), "ā", "switch Balaram to Unicode");
            Equal(SingleLetters.Next("ñ", "balaram", "unicode"), "ï", "disambiguate Unicode palatal nasal");
            Equal(SingleLetters.Next("ñ", "unicode", "balaram"), "ṣ", "disambiguate Balaram s");
            Equal(SingleLetters.Next("ß", "unicode", "balaram"), "Ḷ", "uppercase legacy l");
            Equal(SingleLetters.Next("Ḷ", "balaram", "unicode"), "ß", "uppercase Unicode l");
            Equal(SingleLetters.Next("ā", "balaram"), "ä", "typed Unicode maps before cycling");
            Equal(SingleLetters.Next("ä", "unicode"), "ā", "typed legacy maps before cycling");
            var owner = new IntPtr(1);
            var other = new IntPtr(2);
            var now = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);
            var tap = new LeftControlTap();
            Check(LeftControlTap.IsLeftControl(0xA2, 0), "left-specific Control recognized");
            Check(LeftControlTap.IsLeftControl(0x11, 0), "generic nonextended Control recognized");
            Check(!LeftControlTap.IsLeftControl(0xA3, 1), "right Control excluded");
            Check(!LeftControlTap.IsLeftControl(0x11, 1), "generic extended right Control excluded");
            Check(!LeftControlTap.IsLeftControl(0x41, 0), "non-Control excluded");
            for (int duration = 0; duration <= 650; duration += 10)
            {
                tap.Down(owner, 9, now);
                Check(tap.Up(owner, 9, now.AddMilliseconds(duration)) == (duration <= 600), "tap duration");
                Check(!tap.Up(owner, 9, now), "tap cannot repeat after release");
            }

            tap.Down(owner, 9, now);
            tap.Down(owner, 9, now.AddMilliseconds(590));
            Check(!tap.Up(owner, 9, now.AddMilliseconds(610)), "auto-repeat cannot rearm tap");
            tap.Down(owner, 9, now);
            tap.Cancel();
            Check(!tap.Up(owner, 9, now), "another key cancels Ctrl tap");
            tap.Down(owner, 9, now);
            Check(!tap.Up(other, 9, now), "focus change cancels Ctrl tap");
            tap.Down(owner, 9, now);
            Check(!tap.Up(owner, 10, now), "new typing cancels Ctrl tap");
            tap.Down(owner, 9, now);
            Check(!tap.Up(owner, 9, now.AddTicks(-1)), "clock reversal rejects tap");
            tap.Down(IntPtr.Zero, 9, now);
            Check(!tap.Up(IntPtr.Zero, 9, now), "no window rejects tap");
            var cycle = new LetterCycle();
            string mode, next;
            cycle.Observe('s', owner);
            Check(!cycle.Queue("unicode", other), "foreign window cannot queue");
            for (int i = 0; i < 3; i++)
                Check(cycle.Queue("unicode", owner), "queue three Unicode taps");
            foreach (string expected in new[]
            {
                "ṣ",
                "ś",
                "s"
            }

            )
            {
                Check(cycle.Take(out mode, out next), "take queued tap");
                Equal(next, expected, "queued taps use latest committed letter");
                cycle.Commit(next, mode);
            }

            Check(!cycle.HasPending && !cycle.Take(out mode, out next), "queue exhausted");
            cycle.Observe('a', owner);
            Check(cycle.Queue("unicode", owner), "queue first format");
            cycle.Take(out mode, out next);
            cycle.Commit(next, mode);
            Check(cycle.Queue("balaram", owner), "queue switch format");
            cycle.Take(out mode, out next);
            Equal(next, "ä", "switch retains accent");
            cycle.Commit(next, mode);
            Check(cycle.Queue("balaram", owner), "queue same format");
            cycle.Take(out mode, out next);
            Equal(next, "a", "next tap cycles to plain");
            cycle.Commit(next, mode);
            cycle.Observe('a', owner);
            for (int i = 0; i < 10000; i++)
                Check(cycle.Queue("unicode", owner), "saturation consumes conversion key");
            int taken = 0;
            while (cycle.Take(out mode, out next))
            {
                cycle.Commit(next, mode);
                taken++;
            }

            Check(taken == 64, "queue memory bounded");
            long revision = cycle.Revision;
            cycle.Observe('s', owner);
            Check(cycle.Revision != revision, "new letter invalidates earlier worker");
            cycle.Queue("unicode", owner);
            Check(!cycle.ClearIfOwned(revision) && cycle.HasPending && cycle.Current == "s", "stale failed worker cannot discard newer letter cycle");
            Check(cycle.ClearIfOwned(cycle.Revision) && !cycle.HasPending, "owned failed worker clears its cycle");
            cycle.Queue("unicode", owner);
            cycle.Clear();
            Check(!cycle.HasPending && cycle.Current == null, "reset drops old pending taps");
            foreach (char c in new[]
            {
                ' ',
                '`',
                'x',
                '1'
            }

            )
            {
                cycle.Observe('a', owner);
                cycle.Observe(c, owner);
                Check(!cycle.Queue("unicode", owner), "unsupported new typing clears previous letter");
            }

            // The same supported letter may occur many times. Only the current owned offset is selected.
            foreach (string prefix in new[]
            {
                "",
                "Earlier an age since ",
                "r",
                "😀 previous\r\n",
                "a a a "
            }

            )
                foreach (string suffix in new[]
                {
                    "",
                    "ja",
                    " untouched a",
                    "\r\n"
                }

                )
                    foreach (string row in unicode)
                        foreach (char c in row)
                        {
                            string text = prefix + c + suffix, letter = c.ToString();
                            int caret = prefix.Length + 1, begin;
                            Check(LetterEditGuard.Locate(text, caret, caret, letter, out begin) && begin == prefix.Length, "locate exactly one letter");
                            Check(LetterEditGuard.Selected(text, begin, caret, letter, begin, caret), "verify exact owned selection");
                            string output = SingleLetters.Next(letter, "unicode"), changed = text.Substring(0, begin) + output + text.Substring(caret);
                            Check(changed == prefix + output + suffix, "preserve prefix and suffix");
                            Check(!LetterEditGuard.Locate(text, begin, caret, letter, out begin), "pre-existing selection rejected");
                            Check(!LetterEditGuard.Selected(text, 0, caret, letter, prefix.Length, caret) || prefix.Length == 0, "cannot select preceding text");
                            Check(!LetterEditGuard.Selected(text, prefix.Length, caret + 1, letter, prefix.Length, caret), "cannot select following text");
                        }

            int unused;
            Check(!LetterEditGuard.Locate("a", 0, 0, "a", out unused), "caret at start rejected");
            Check(!LetterEditGuard.Locate("a", 2, 2, "a", out unused), "caret past end rejected");
            Check(!LetterEditGuard.Locate("s", 1, 1, "a", out unused), "wrong letter rejected");
            Check(!LetterEditGuard.Selected("a a", 0, 1, "a", 2, 3), "identical preceding letter cannot substitute for owned letter");
            var state = new InputState();
            foreach (char c in "Earlier raja")
                state.Type(c);
            long generation = state.Generation;
            Check(state.ReplaceLastLetter('a', 'ā'), "update current token");
            Equal(state.Token, "rajā", "only last token character changes");
            Equal(state.CurrentLiteral, "rajā", "literal buffer matches actual text");
            Check(state.Generation == generation, "internal edit preserves clipboard verification generation");
            state.RecordReplacement("a", "ā", owner);
            state.CancelManualConversion();
            Check(state.UndoOriginal == null, "undo ticket consumed");
            Check(state.ReplaceLastLetter('ā', 'a'), "undo updates token");
            Equal(state.Token, "raja", "undo restores token");
            Check(!state.ReplaceLastLetter('s', 'ṣ'), "mismatching token rejected");
            Equal(state.Token, "raja", "mismatch leaves token alone");
            state.Clear();
            for (int i = 0; i < 161; i++)
                state.Type('a');
            Check(!state.ReplaceLastLetter('a', 'ā'), "overflow buffer never rewritten");
            Check(state.Type('>') == null, "overflow cannot expand preceding stale text");
            foreach (string oldLetter in new[]
            {
                "a",
                "ā",
                "ä",
                "s",
                "ṣ",
                "ś",
                "n",
                "ṇ",
                "m",
                "ṁ"
            }

            )
                foreach (string newLetter in new[]
                {
                    "a",
                    "i",
                    "u",
                    "r",
                    "l",
                    "n",
                    "s",
                    "t",
                    "d",
                    "h",
                    "m"
                }

                )
                {
                    Equal(null, CaretLetterDiscovery.Expected(true, false, oldLetter), "previous converted letter never constrains a new caret");
                    Equal(newLetter, CaretLetterDiscovery.Expected(true, true, newLetter), "fresh physical letter still guards delayed input");
                    Equal(null, CaretLetterDiscovery.Expected(false, true, newLetter), "another owner has no typing expectation");
                    int count = 0;
                    string expected = CaretLetterDiscovery.Expected(true, false, oldLetter);
                    Equal(newLetter, CaretLetterDiscovery.Read(() =>
                    {
                        count++;
                        return newLetter;
                    }, x => expected == null || EditGuard.CaseEquivalent(expected, x), () => true, ms =>
                    {
                    }), "first tap on a different supported letter is accepted");
                    Check(count == 1, "new letter does not wait for cached previous letter");
                }

            Check(!CaretLetterDiscovery.Accept("a", null, "ā", true), "same-position sample waits for the just-inserted letter");
            Check(CaretLetterDiscovery.Accept("ā", null, "ā", true), "settled same-position conversion can cycle");
            Check(CaretLetterDiscovery.Accept("s", null, "ā", false), "different caret letter bypasses previous letter expectation");
            Check(!CaretLetterDiscovery.Accept("ā", "s", "ā", true), "fresh typing expectation wins over converted-letter cache");
            Check(CaretLetterDiscovery.Accept("s", "s", "ā", true), "new physical letter accepted without prior-letter warmup");
            for (int mask = 0; mask < 8; mask++)
            {
                bool observed = (mask & 1) != 0, supported = (mask & 2) != 0, sameCaret = (mask & 4) != 0;
                Check(CaretLetterDiscovery.MayTypeLiteral(observed, supported, sameCaret) == (observed && !supported && sameCaret), "literal backtick requires confirmed nonconvertible caret");
            }

            Check(!CaretLetterDiscovery.MayTypeLiteral(false, false, true), "unavailable first sample cannot generate a backtick");
            Check(!CaretLetterDiscovery.MayTypeLiteral(true, true, true), "unresolved supported-letter font cannot generate a backtick");
            // Real discovery policy, including the unavailable first Word sample after caret movement.
            for (int ready = 0; ready < CaretLetterDiscovery.Attempts; ready++)
            {
                int probes = 0, waits = 0;
                string discovered = CaretLetterDiscovery.Read(() => ++probes > ready ? "a" : null, x => x == "a", () => true, ms =>
                {
                    Check(ms == 20, "bounded retry delay");
                    waits++;
                });
                Equal("a", discovered, "recover delayed existing letter " + ready);
                Check(probes == ready + 1 && waits == ready, "stop at first valid sample");
            }

            int absent = 0, delays = 0;
            Check(CaretLetterDiscovery.Read(() =>
            {
                absent++;
                return (string)null;
            }, x => true, () => true, ms => delays++) == null, "unavailable caret returns no sample");
            Check(absent == CaretLetterDiscovery.Attempts && delays == CaretLetterDiscovery.Attempts - 1, "no unbounded retries or idle polling");
            int cancelledProbes = 0;
            Check(CaretLetterDiscovery.Read(() =>
            {
                cancelledProbes++;
                return "a";
            }, x => true, () => false, ms =>
            {
            }) == null && cancelledProbes == 0, "focus cancellation before probe");
            bool active = true;
            Check(CaretLetterDiscovery.Read(() =>
            {
                active = false;
                return "a";
            }, x => true, () => active, ms =>
            {
            }) == null, "focus cancellation during probe rejects sample");
            int stale = 0;
            Equal("s", CaretLetterDiscovery.Read(() => ++stale < 3 ? "a" : "s", x => x == "s", () => true, ms =>
            {
            }), "reject stale letter before replacement settles");
            Check(WordPasteGuard.Label("Paste Options:") && WordPasteGuard.Label("Paste Options"), "identify observed Word paste menu labels");
            Check(WordPasteGuard.Label("Paste Options (Ctrl)") && WordPasteGuard.Label("Paste Options: (Ctrl)"), "recognize shortcut-qualified paste labels");
            Check(WordPasteGuard.Label("paste options (ctrl)") && WordPasteGuard.Label(" Paste Options (Ctrl) "), "label case and outer spacing do not hide owned paste menu");
            Check(!WordPasteGuard.Label("Paste Options (Ctrl) unrelated") && !WordPasteGuard.Label(null), "shortcut-qualified label must still match exactly");
            Check(WordPasteGuard.DocumentNode("Body", false, false) && WordPasteGuard.DocumentNode("", true, false), "document ancestor can own focused Edit/Text child");
            Check(!WordPasteGuard.DocumentNode("Find", false, false) && !WordPasteGuard.DocumentNode("Body", true, true), "ribbon/find and menu are not document focus");
            Check(WordPasteGuard.KeepFocusTransition(true, true, true, false), "late document focus after Escape retains pending Ctrl conversion");
            Check(WordPasteGuard.KeepFocusTransition(true, true, false, true), "paste menu opening retains pending Ctrl conversion");
            Check(!WordPasteGuard.KeepFocusTransition(false, true, true, true), "another Word window cancels pending conversion");
            Check(!WordPasteGuard.KeepFocusTransition(true, false, true, true), "expired conversion cannot retain focus state");
            Check(!WordPasteGuard.KeepFocusTransition(true, true, false, false), "unrelated Word dialog cancels conversion");
            Check(!WordPasteGuard.Settled(true, true), "UIA Body focus cannot authorize selection while native paste popup remains");
            Check(WordPasteGuard.Settled(true, false) && !WordPasteGuard.Settled(false, false), "document must recover after paste popup closes");
            int escapes = 0, pauses = 0;
            bool pasteMenu = true, document = true, current = true;
            Check(WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                pasteMenu = false;
                return true;
            }, ms => pauses++, false) && escapes == 1, "single Escape dismissal");
            escapes = pauses = 0;
            pasteMenu = true;
            Check(WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                if (escapes == 2)
                    pasteMenu = false;
                return true;
            }, ms => pauses++, false) && escapes == 2 && pauses == 15, "second Escape closes retained native paste popup");
            escapes = pauses = 0;
            pasteMenu = true;
            Check(!WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                return true;
            }, ms => pauses++, false) && escapes == 2 && pauses == 30, "unresponsive popup cannot cause unbounded retries or selection");
            escapes = 0;
            pasteMenu = false;
            Check(!WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                return true;
            }, ms =>
            {
            }, false) && escapes == 0, "no trusted paste means no Escape to unknown UI");
            current = false;
            pasteMenu = true;
            Check(!WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                return true;
            }, ms =>
            {
            }, true) && escapes == 0, "changed context cannot dismiss a popup");
            current = true;
            escapes = pauses = 0;
            Check(!WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                return true;
            }, ms =>
            {
                pauses++;
                current = false;
            }, false) && escapes == 1 && pauses == 1, "typing/focus change cancels recovery before another Escape");
            current = true;
            escapes = 0;
            Check(!WordPasteRecovery.Dismiss(() => current, () => pasteMenu, () => document, () =>
            {
                escapes++;
                return false;
            }, ms =>
            {
            }, true) && escapes == 1, "failed Escape stops recovery");
            foreach (string label in new[]
            {
                "",
                "Paste",
                "Options",
                "Font",
                "Paste Options: unrelated"
            }

            )
                Check(!WordPasteGuard.Label(label), "never dismiss unrelated menu");
            Check(WordPasteGuard.Owned("OpusApp", 42, 42), "Word menu ownership");
            Check(!WordPasteGuard.Owned("OpusApp", 42, 43) && !WordPasteGuard.Owned("Other", 42, 42) && !WordPasteGuard.Owned("OpusApp", 0, 0), "reject foreign menu ownership");
            var pasteTap = new LeftControlTap();
            var tapTime = new DateTime(2026, 10, 11);
            pasteTap.Down(owner, 1, tapTime);
            Check(pasteTap.Pending(owner, tapTime.AddMilliseconds(100)), "Ctrl tap survives bounded Word menu focus");
            pasteTap.Rebase(2);
            Check(pasteTap.Up(owner, 2, tapTime.AddMilliseconds(100)), "new generation after popup focus");
            pasteTap.Down(owner, 3, tapTime);
            pasteTap.Cancel();
            pasteTap.Rebase(4);
            Check(!pasteTap.Up(owner, 4, tapTime.AddMilliseconds(100)), "Ctrl chord cancellation cannot rearm");
            pasteTap.Down(owner, 5, tapTime);
            Check(!pasteTap.Pending(owner, tapTime.AddMilliseconds(601)) && !pasteTap.Up(owner, 5, tapTime.AddMilliseconds(601)), "held Ctrl never converts");
            var pasteContext = new WordPasteContext();
            pasteContext.Mark(owner, 25);
            Check(pasteContext.Owns(owner, 25) && !pasteContext.Verified(owner, 25), "paste marker alone cannot dismiss menu");
            pasteContext.Verify(new IntPtr(999), 25);
            Check(!pasteContext.Verified(owner, 25), "foreign focus cannot verify paste");
            pasteContext.Verify(owner, 26);
            Check(!pasteContext.Verified(owner, 25), "changed clipboard cannot verify paste");
            pasteContext.Verify(owner, 25);
            Check(pasteContext.Verified(owner, 25), "verified document paste scoped to owner and clipboard");
            Check(!pasteContext.Verified(owner, 26) && !pasteContext.Verified(new IntPtr(999), 25), "context not reused after clipboard/window change");
            pasteContext.Clear();
            Check(!pasteContext.Owns(owner, 25) && !pasteContext.Verified(owner, 25), "other input cancels paste context");
            pasteContext.Mark(owner, 27);
            Check(!pasteContext.Verified(owner, 27), "next paste needs new document verification");
            Equal("raja", WordPasteContext.ClipboardWord("Earlier untouched raja"), "mouse paste paragraph final word");
            Equal("raja", WordPasteContext.ClipboardWord(new string ('x', 900) + " raja"), "bounded clipboard tail");
            foreach (string text in new[]
            {
                null,
                "",
                "raja ",
                "raja\r\n",
                "https://example.com/raja",
                new string ('a', 161)
            }

            )
                Check(WordPasteContext.ClipboardWord(text) == null, "unqualified clipboard cannot mark mouse paste");
            Check(WordPasteContext.Matches("raja", "Raja"), "Word capitalization accepted for mouse paste");
            foreach (string actual in new[]
            {
                null,
                "",
                "previous",
                "raj",
                "rājā"
            }

            )
                Check(!WordPasteContext.Matches("raja", actual), "wrong caret word cannot mark mouse paste");
            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
