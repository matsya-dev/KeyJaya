// Noninteractive regressions: typing queues, focus changes, stale operations, and replay ordering.
// Run: .\test.ps1 -Suite CorrectionRaceTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Text;

namespace KeyJaya
{
    public static class CorrectionRaceTests
    {
        static int passed;
        static void Check(bool value, string label)
        {
            if (!value)
                throw new Exception(label);
            passed++;
        }

        static string Apply(string initial, int position, Native.Input[] batch, out int end)
        {
            var text = new StringBuilder(initial);
            int caret = position;
            int? anchor = null;
            foreach (var item in batch)
            {
                var key = item.union.keyboard;
                Check(item.type == 1 && key.extra.ToUInt64() == Native.OwnCorrectionInput, "owned input only");
                if ((key.flags & 2) != 0)
                    continue;
                if (key.vk == 0xA0 || key.vk == 0x10)
                {
                    anchor = caret;
                    continue;
                }

                if ((key.flags & 4) != 0)
                {
                    if (anchor.HasValue)
                    {
                        int a = Math.Min(anchor.Value, caret), b = Math.Max(anchor.Value, caret);
                        text.Remove(a, b - a);
                        caret = a;
                        anchor = null;
                    }

                    text.Insert(caret++, (char)key.scan);
                    continue;
                }

                if (key.vk == 0x25 || key.vk == 0x27)
                {
                    caret += key.vk == 0x25 ? -1 : 1;
                    Check(caret >= 0 && caret <= text.Length, "navigation stays within field");
                    continue;
                }

                if (key.vk == 8)
                {
                    if (anchor.HasValue)
                    {
                        int a = Math.Min(anchor.Value, caret), b = Math.Max(anchor.Value, caret);
                        text.Remove(a, b - a);
                        caret = a;
                        anchor = null;
                    }
                    else if (caret > 0)
                        text.Remove(--caret, 1);
                    continue;
                }

                throw new Exception("Unexpected key");
            }

            end = caret;
            return text.ToString();
        }

        static void DeliveryAcknowledgment()
        {
            IntPtr owner = new IntPtr(12), other = new IntPtr(13);
            var hold = new Native.CorrectionInputHold();
            for (int n = 0; n < 10000; n++)
            {
                Check(hold.Begin(owner), "Consecutive replay-triggered conversions acquire a fresh queue");
                hold.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
                Check(hold.DrainNext(owner).Length == 1 && !hold.FinishIfEmpty(owner), "An undelivered event still protects the queue");
                Check(!hold.AcknowledgeInFlight(other), "Another window cannot acknowledge delivery");
                Check(hold.AcknowledgeInFlight(owner) && !hold.AcknowledgeInFlight(owner), "Hook delivery is acknowledged exactly once");
                Check(hold.FinishIfEmpty(owner) && !hold.Active, "A delivered initiating key cannot deadlock the next manual conversion");
            }

            hold.Begin(owner);
            hold.Keyboard(new Native.KeyData { vk = 0x41 }, 0x100, owner);
            hold.Keyboard(new Native.KeyData { vk = 0x42 }, 0x100, owner);
            hold.DrainNext(owner);
            Check(hold.AcknowledgeInFlight(owner) && !hold.FinishIfEmpty(owner) && hold.Count == 1, "Acknowledgment preserves following input");
            Check(hold.DrainNext(owner)[0].union.keyboard.vk == 0x42, "The following key retains its position");
            Check(hold.DeferInFlight(owner) && !hold.AcknowledgeInFlight(owner) && hold.Count == 1, "An undelivered deferred key cannot be acknowledged away");
            Check(hold.DrainNext(owner)[0].union.keyboard.vk == 0x42 && hold.AcknowledgeInFlight(owner) && hold.FinishIfEmpty(owner), "Focus-raced delivery recovers without loss or duplication");
        }

        static void FailedControlTaps()
        {
            var owner = new IntPtr(12);
            var other = new IntPtr(13);
            for (int count = 1; count <= 64; count++)
            {
                var hold = new Native.CorrectionInputHold();
                hold.Begin(owner);
                for (int n = 0; n < count; n++)
                {
                    hold.Keyboard(new Native.KeyData { vk = 0xA2 }, 0x100, owner);
                    hold.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
                }

                Check(!hold.FinishIfOnlyControlTaps(other) && hold.Count == count * 2, "Wrong owner cannot retire taps");
                Check(hold.FinishIfOnlyControlTaps(owner) && !hold.Active && hold.Count == 0, "Unavailable target cannot endlessly replay complete Ctrl taps");
                Check(hold.Begin(other) && hold.FinishIfEmpty(other), "Another editor can immediately acquire the released queue");
            }

            foreach (uint vk in new uint[]
            {
                0x41,
                0x10,
                0xA0,
                0x12,
                0x5B,
                0x25,
                0xBE
            }

            )
            {
                var hold = new Native.CorrectionInputHold();
                hold.Begin(owner);
                hold.Keyboard(new Native.KeyData { vk = 0xA2 }, 0x100, owner);
                hold.Keyboard(new Native.KeyData { vk = vk }, 0x100, owner);
                hold.Keyboard(new Native.KeyData { vk = vk, flags = 0x80 }, 0x101, owner);
                hold.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
                Check(!hold.FinishIfOnlyControlTaps(owner) && hold.Count == 4, "Typing, shortcuts and other modifiers remain queued");
            }

            foreach (bool up in new[]
            {
                false,
                true
            }

            )
            {
                var hold = new Native.CorrectionInputHold();
                hold.Begin(owner);
                hold.Keyboard(new Native.KeyData { vk = 0xA2, flags = up ? 0x80u : 0u }, up ? 0x101 : 0x100, owner);
                Check(!hold.FinishIfOnlyControlTaps(owner) && hold.Count == 1, "Unmatched modifier events must not be discarded");
            }

            var flight = new Native.CorrectionInputHold();
            flight.Begin(owner);
            flight.Keyboard(new Native.KeyData { vk = 0xA2 }, 0x100, owner);
            flight.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
            flight.DrainNext(owner);
            Check(!flight.FinishIfOnlyControlTaps(owner), "In-flight Ctrl down still requires delivery and release");
            flight.DeferInFlight(owner);
            Check(flight.FinishIfOnlyControlTaps(owner), "A fully deferred undelivered pair can retire safely");
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(Resident);
            foreach (bool committed in new[]
            {
                false,
                true
            }

            )
                using (var timer = new System.Windows.Forms.Timer())
                {
                    var resident = (Resident)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
                    GC.SuppressFinalize(resident);
                    var hold = new Native.CorrectionInputHold();
                    hold.Begin(owner);
                    hold.Keyboard(new Native.KeyData { vk = 0xA2 }, 0x100, owner);
                    hold.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
                    Action<string, object> set = (name, value) => type.GetField(name, flags).SetValue(resident, value);
                    set("correctionInputHold", hold);
                    set("correctionHoldOwner", owner);
                    set("manualRetry", timer);
                    set("manualInputActive", true);
                    set("manualInputReady", true);
                    if (committed)
                        set("manualInputOutput", "committed output");
                    bool released = (bool)type.GetMethod("ReleaseUnchangedControlTaps", flags).Invoke(resident, null);
                    Check(released == !committed, "Actual resident releases only an unchanged failed discovery");
                    Check(hold.Active == committed, "Failed lookup cannot globally reserve the queue; committed input retains its proof requirements");
                }
        }

        static void ClosedTargetRecovery(string root)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(Resident);
            var oldOwner = new IntPtr(-8675309);
            var nextOwner = new IntPtr(-8675310);
            Check(!Native.IsWindow(oldOwner) && !Native.IsWindow(nextOwner), "Recovery fixture uses no application window");
            foreach (bool automatic in new[]
            {
                false,
                true
            }

            )
                foreach (bool deferred in new[]
                {
                    false,
                    true
                }

                )
                {
                    // Exercise the resident's real transition without starting a Form, hooks,
                    // hotkeys, a tray icon, or any input injection into the user's applications.
                    var resident = (Resident)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
                    GC.SuppressFinalize(resident);
                    var hold = new Native.CorrectionInputHold();
                    var typing = new CorrectionTyping();
                    using (var timer = new System.Windows.Forms.Timer())
                    {
                        Action<string, object> set = (name, value) => type.GetField(name, flags).SetValue(resident, value);
                        Func<string, object> get = name => type.GetField(name, flags).GetValue(resident);
                        hold.Begin(oldOwner);
                        hold.Keyboard(new Native.KeyData { vk = 0x41 }, 0x100, oldOwner);
                        if (deferred)
                        {
                            hold.DrainNext(oldOwner);
                            hold.DeferInFlight(oldOwner);
                        }

                        set("correctionInputHold", hold);
                        set("correctionTyping", typing);
                        set("manualRetry", timer);
                        set("correctionHoldOwner", oldOwner);
                        set("logPath", System.IO.Path.Combine(root, "logs", "closed-target-test.log"));
                        foreach (string name in new[]
                        {
                            "manualInputActive",
                            "manualInputReady",
                            "manualInputSelected",
                            "correctionInputDraining",
                            "correctionDrainWaiting",
                            "correctionHoldReady",
                            "correctionHoldTouched"
                        }

                        )
                            set(name, true);
                        set("manualInputOutput", "old output");
                        set("manualRetryAttempts", 25);
                        string method = automatic ? "BeginCorrectionTyping" : "BeginManualInput";
                        object[] input = automatic ? new object[]
                        {
                            71L,
                            nextOwner,
                            "cant "
                        }

                        : new object[]
                        {
                            nextOwner,
                            0u,
                            false
                        };
                        Check((bool)type.GetMethod(method, flags).Invoke(resident, input), "A closed editor permits a fresh automatic or manual transaction");
                        Check(hold.Owns(nextOwner) && hold.Count == 0, "Closed-target input cannot leak into the new editor");
                        Check(!(bool)get("correctionInputDraining") && !(bool)get("correctionDrainWaiting") && !(bool)get("correctionHoldReady") && !(bool)get("correctionHoldTouched"), "Old draining and correction state cannot poison a new transaction");
                        Check((bool)get("manualInputActive") == !automatic && !(bool)get("manualInputReady") && !(bool)get("manualInputSelected"), "Automatic correction cannot inherit abandoned manual replay state");
                        Check(get("manualInputOutput") == null && (int)get("manualRetryAttempts") == 0, "Old conversion output and retry budget are cleared");
                        hold.Keyboard(new Native.KeyData { vk = 0x42 }, 0x100, nextOwner);
                        Check(hold.DrainNext(nextOwner)[0].union.keyboard.vk == 0x42 && hold.AcknowledgeInFlight(nextOwner) && hold.FinishIfEmpty(nextOwner), "Normal typing and queue release recover in the new editor");
                    }
                }
        }

        public static int Main(string[] runArgs)
        {
            DeliveryAcknowledgment();
            FailedControlTaps();
            ClosedTargetRecovery(runArgs.Length > 0 ? runArgs[0] : System.IO.Path.Combine(Environment.CurrentDirectory, "out", "tests", "fixture"));
            var tracker = new CorrectionTyping();
            var owner = new IntPtr(12);
            string suffix;
            tracker.Begin(10, owner);
            Check(tracker.Read(10, owner, 10, out suffix) && suffix == "", "initial completion");
            tracker.Append('b', 11, owner);
            tracker.Append('e', 12, owner);
            Check(tracker.Read(10, owner, 12, out suffix) && suffix == "be", "fast next word retained");
            Check(!tracker.Read(10, owner, 11, out suffix), "stale proof refused");
            Check(!tracker.Read(10, new IntPtr(13), 12, out suffix), "other app refused");
            tracker.Clear();
            Check(!tracker.Read(10, owner, 100, out suffix), "navigation/reset cancels");
            tracker.Begin(20, owner);
            tracker.Append('b', 22, owner);
            Check(!tracker.Read(20, owner, 22, out suffix), "nonconsecutive typing refused");
            tracker.Begin(30, owner);
            for (int n = 0; n < 97; n++)
                tracker.Append('b', 31 + n, owner);
            Check(!tracker.Read(30, owner, 127, out suffix), "bounded following text");
            tracker.Begin(100, owner);
            tracker.Clear(99);
            Check(tracker.Read(100, owner, 100, out suffix), "old worker cannot clear new task");
            tracker.Clear(100);
            Check(!tracker.Read(100, owner, 100, out suffix), "matching task ends");
            foreach (string typo in new[]
            {
                "canx",
                "doesnx",
                "didnx",
                "wouldnx",
                "Ix",
                "letx"
            }

            )
            {
                var state = new InputState();
                foreach (char c in typo)
                    state.Type(c);
                Check(state.BackspaceTyped(), "partial typo backspace retained");
                char replacement = typo == "Ix" ? 'm' : typo == "letx" ? 's' : 't';
                state.Type(replacement);
                Check(state.CompletedEnglish(' ') == typo.Substring(0, typo.Length - 1) + replacement + " ", "corrected complete contraction retained");
                state.Type(' ');
                Check(!state.BackspaceTyped(), "cannot infer deletion across unknown word boundary");
            }

            var hold = new Native.CorrectionInputHold();
            Check(hold.Begin(owner), "new correction begins");
            Check(!hold.Begin(new IntPtr(13)), "pending input never overwritten by another target");
            var press = new Native.KeyData
            {
                vk = 0x42,
                scan = 0x30
            };
            Check(hold.Keyboard(press, 0x100, owner), "following key held");
            press.flags = 0x80;
            Check(hold.Keyboard(press, 0x101, owner), "following key release held");
            Check(hold.Take(new IntPtr(13)) == null && hold.Active, "wrong window cannot release held input");
            var events = hold.Take(owner);
            Check(events.Length == 2 && !hold.Active, "held input released once");
            Check(events[0].union.keyboard.vk == 0x42 && events[0].union.keyboard.flags == 0 && events[1].union.keyboard.flags == 2, "physical key ordering retained");
            Check(events[0].union.keyboard.extra.ToUInt64() == Native.ReplayedInput, "replay feeds normal word tracking");
            Check(!hold.Keyboard(press, 0x101, owner), "normal typing passes after release");
            var draining = new Native.CorrectionInputHold();
            Check(draining.Begin(owner), "drain begins");
            for (int n = 0; n < 1000; n++)
                Check(draining.Keyboard(new Native.KeyData { vk = (uint)(0x41 + n % 26) }, 0x100, owner), "fresh input captured while draining");
            Check(draining.DrainNext(new IntPtr(13)) == null && draining.Count == 1000, "drain cannot reach another owner");
            for (int n = 0; n < 1000; n++)
            {
                var step = draining.DrainNext(owner);
                Check(step.Length == 1 && step[0].union.keyboard.vk == 0x41 + n % 26 && step[0].union.keyboard.extra.ToUInt64() == Native.DrainedInput, "oldest event drained in order");
                if (n == 0)
                    Check(draining.Keyboard(new Native.KeyData { vk = 0x5A }, 0x100, owner), "new arrival remains behind older events");
            }

            Check(draining.Active && draining.Count == 1, "capture stays armed until final acknowledgment");
            Check(draining.DrainNext(owner)[0].union.keyboard.vk == 0x5A, "late arrival follows entire earlier queue");
            Check(draining.DrainNext(owner).Length == 0 && !draining.Active, "empty drain disarms atomically");
            var raced = new Native.CorrectionInputHold();
            Check(raced.Begin(owner), "raced drain begins");
            raced.Keyboard(new Native.KeyData { vk = 0x41 }, 0x100, owner);
            raced.Keyboard(new Native.KeyData { vk = 0x42 }, 0x100, owner);
            Check(raced.DrainNext(owner)[0].union.keyboard.vk == 0x41, "first raced event submitted");
            Check(!raced.DeferInFlight(new IntPtr(13)), "another owner cannot reclaim event");
            Check(raced.DeferInFlight(owner), "undelivered event reclaimed after focus race");
            Check(!raced.DeferInFlight(owner) && raced.Count == 2, "deferred event retained exactly once");
            raced.Keyboard(new Native.KeyData { vk = 0x43 }, 0x100, owner);
            foreach (ushort vk in new ushort[]
            {
                0x41,
                0x42,
                0x43
            }

            )
                Check(raced.DrainNext(owner)[0].union.keyboard.vk == vk, "reclaimed event precedes all following arrivals");
            Check(raced.DrainNext(owner).Length == 0 && !raced.DeferInFlight(owner), "completed drain has no event to reclaim");
            var manual = new Native.CorrectionInputHold();
            Check(manual.Begin(owner), "manual queue arms before selection");
            manual.InitialRelease(0xA2);
            manual.InitialRelease(0xBE);
            Check(!manual.Keyboard(new Native.KeyData { vk = 0xBE }, 0x100, owner), "initiating autorepeat reaches shortcut dispatcher");
            Check(!manual.Keyboard(new Native.KeyData { vk = 0xBE, flags = 0x80 }, 0x101, owner), "initiating shortcut release is not delayed");
            Check(!manual.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner), "initiating modifier release is not delayed");
            Check(manual.Count == 0 && !manual.HasTyping, "initial releases do not count as following text");
            manual.Keyboard(new Native.KeyData { vk = 0xA2 }, 0x100, owner);
            manual.Keyboard(new Native.KeyData { vk = 0x43 }, 0x100, owner);
            manual.Keyboard(new Native.KeyData { vk = 0x43, flags = 0x80 }, 0x101, owner);
            manual.Keyboard(new Native.KeyData { vk = 0xA2, flags = 0x80 }, 0x101, owner);
            Check(manual.Count == 4 && !manual.HasTyping, "later Ctrl+C is fully queued and retains converted selection");
            manual.Keyboard(new Native.KeyData { vk = 0x42 }, 0x100, owner);
            Check(manual.HasTyping, "following text requests caret at converted selection end");
            Check(!manual.Keyboard(new Native.KeyData { vk = 0x45 }, 0x100, new IntPtr(13)), "typing in other window remains untouched");
            var ordered = manual.Take(owner);
            Check(ordered.Length == 5 && ordered[0].union.keyboard.vk == 0xA2 && ordered[3].union.keyboard.vk == 0xA2 && ordered[4].union.keyboard.vk == 0x42, "later chord then typing retain exact event order");
            Check(manual.Begin(owner), "next manual occurrence starts cleanly");
            Check(manual.Keyboard(new Native.KeyData { vk = 0xBE, flags = 0x80 }, 0x101, owner), "old initiating key exemption never leaks to next action");
            manual.Take(owner);
            var abandoned = new Native.CorrectionInputHold();
            abandoned.Begin(owner);
            abandoned.Keyboard(new Native.KeyData { vk = 0x41 }, 0x100, owner);
            abandoned.Keyboard(new Native.KeyData { vk = 0x42 }, 0x100, owner);
            abandoned.DrainNext(owner);
            abandoned.DeferInFlight(owner);
            var recovered = abandoned.Take(owner);
            Check(recovered.Length == 2 && recovered[0].union.keyboard.vk == 0x41 && recovered[1].union.keyboard.vk == 0x42, "target cleanup retains deferred ordering");
            foreach (string pair in new[]
            {
                "cant|can’t",
                "doesnt|doesn’t",
                "didnt|didn’t",
                "Im|I’m",
                "Lets|Let’s",
                "wont|won’t",
                "id|I’d",
                "teh|the",
                "(cant),|(can’t),"
            }

            )
                foreach (char delimiter in new[]
                {
                    ' ',
                    '\u00a0',
                    ',',
                    '!',
                    '\n',
                    '\t'
                }

                )
                    foreach (string tail in new[]
                    {
                        "",
                        "b",
                        "be",
                        "be quick",
                        "be quick ",
                        "be\nquick",
                        "be\tquick",
                        new string ('x', 96)
                    }

                    )
                    {
                        var cells = pair.Split('|');
                        string input = cells[0] + delimiter, output = cells[1] + delimiter;
                        var batch = Native.AutomaticCorrectionInput(input, output, tail);
                        Check(batch != null, "valid correction batch");
                        string prefix = "Earlier untouched ", after = " [untouched]";
                        int end;
                        string actual = Apply(prefix + input + tail + after, prefix.Length + input.Length + tail.Length, batch, out end);
                        Check(actual == prefix + output + tail + after, "entire contraction and surrounding words preserved");
                        Check(end == prefix.Length + output.Length + tail.Length, "caret after following word");
                    }

            foreach (var args in new[]
            {
                new[]
                {
                    "cant ",
                    "can’t ",
                    new string ('x', 97)
                },
                new[]
                {
                    "cant ",
                    "can’t!",
                    "be"
                },
                new[]
                {
                    "cant ",
                    "can’t ",
                    "😀"
                },
                new[]
                {
                    "",
                    "x",
                    ""
                }
            }

            )
                Check(Native.AutomaticCorrectionInput(args[0], args[1], args[2]) == null, "unsafe batch rejected before selection");
            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
