// Opt-in body comparison. Both implementations run against the same exclusively
// owned scratch document, alternating order. No resident, hooks or clipboard.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    public static partial class NativeWordBindingTests
    {
        const string PerformancePrefix = "Before. ", PerformanceSuffix = " After.";
        const string PerformanceStyleName = "KeyJaya performance fixture character";
        static string PerformanceFocus()
        {
            IntPtr window = Native.GetForegroundWindow();
            uint process;
            Native.GetWindowThreadProcessId(window, out process);
            var name = new StringBuilder(64);
            Native.GetClassName(window, name, name.Capacity);
            string executable = "unavailable";
            try
            {
                using (var running = Process.GetProcessById((int)process))
                    executable = running.ProcessName;
            }
            catch (Exception)
            {
            }

            return "window=" + window.ToInt64() + " process=" + process + " executable=" + executable + " class=" + name + " root_owner=" + Native.GetAncestor(window, 3).ToInt64();
        }

        static WordSelection.VisibleMap Projection(object document)
        {
            object content = Get(document, "Content");
            try
            {
                return (WordSelection.VisibleMap)typeof(WordSelection).GetMethod("Visible", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { content });
            }
            finally
            {
                Release(content);
            }
        }

        static string VisibleScratch(object document)
        {
            object content = Get(document, "Content");
            try
            {
                return WordPerformanceSupport.VisibleBody(Convert.ToString(Get(content, "WordOpenXML")));
            }
            finally
            {
                Release(content);
            }
        }

        static void PerformanceFont(object document, int begin, int end, string name, float size, int bold, int color, int underline)
        {
            object range = Call(document, "Range", begin, end), font = null;
            try
            {
                font = Get(range, "Font");
                Set(font, "Name", name);
                Set(font, "Size", size);
                Set(font, "Bold", bold);
                Set(font, "Italic", 0);
                Set(font, "Color", color);
                Set(font, "Underline", underline);
            }
            finally
            {
                Release(font);
                Release(range);
            }
        }

        static List<string[]> PerformanceFonts(object document, int length)
        {
            var map = Projection(document);
            WordPerformanceSupport.Require(map != null && map.Value.Length >= length && map.Value.TrimEnd('\n') == VisibleScratch(document), "Native font positions disagree with independent XML text");
            var result = new List<string[]>();
            for (int n = 0; n < length; n++)
            {
                int start = map.NativeOffset(n);
                object range = Call(document, "Range", start, start + 1), font = null, style = null;
                try
                {
                    font = Get(range, "Font");
                    var values = new List<string>(new[] { "Name", "Size", "Bold", "Italic", "Color", "Underline" }.Select(property => Convert.ToString(Get(font, property), CultureInfo.InvariantCulture)));
                    style = Get(range, "Style");
                    values.Add(style is string ? (string)style : Convert.ToString(Get(style, "NameLocal"), CultureInfo.InvariantCulture));
                    values.Add(Convert.ToString(Get(range, "LanguageID"), CultureInfo.InvariantCulture));
                    result.Add(values.ToArray());
                }
                finally
                {
                    Release(style);
                    Release(font);
                    Release(range);
                }
            }

            return result;
        }

        static void PerformancePrepare(object app, object document, WordPerformanceCase item, bool tracking)
        {
            Set(document, "TrackRevisions", false);
            object revisions = Get(document, "Revisions"), content = null;
            try
            {
                // This is the one scratch document we created, never a user document.
                Call(revisions, "AcceptAll");
                content = Get(document, "Content");
                Set(content, "Text", PerformancePrefix + (item.seed ?? item.input) + PerformanceSuffix);
                // Built-in IDs are language-independent. Do not inherit a
                // character style from the user's new-document template.
                Set(content, "Style", -1); // wdStyleNormal
                Set(content, "Style", -66); // wdStyleDefaultParagraphFont
            }
            finally
            {
                Release(content);
                Release(revisions);
            }

            int seedLength = (item.seed ?? item.input).Length;
            PerformanceFont(document, 0, PerformancePrefix.Length + seedLength + PerformanceSuffix.Length, "Calibri", 12, 0, 0, 0);
            PerformanceFont(document, 0, PerformancePrefix.Length, "Calibri", 14, -1, 255, 0);
            PerformanceFont(document, PerformancePrefix.Length + seedLength, PerformancePrefix.Length + seedLength + PerformanceSuffix.Length, "Calibri", 10, 0, 32768, 0);
            if (item.mixed)
                PerformanceFont(document, PerformancePrefix.Length + 1, PerformancePrefix.Length + 2, "Consolas", 15, -1, 16711680, 1);
            if (item.character_style)
            {
                object part = null;
                try
                {
                    part = Call(document, "Range", PerformancePrefix.Length, PerformancePrefix.Length + seedLength);
                    Set(part, "Style", PerformanceStyleName);
                }
                finally
                {
                    Release(part);
                }
            }

            if (item.deletion >= 0)
            {
                Set(document, "TrackRevisions", true);
                object deleted = Call(document, "Range", PerformancePrefix.Length + item.deletion, PerformancePrefix.Length + item.deletion + 1);
                try
                {
                    Call(deleted, "Delete");
                }
                finally
                {
                    Release(deleted);
                }
            }

            // Keep one known earlier edit in the native undo stack. A range
            // export must not damage either the conversion's record or history.
            Set(document, "TrackRevisions", false);
            object tail = null, record = null;
            try
            {
                content = Get(document, "Content");
                int last = Convert.ToInt32(Get(content, "End")) - 2;
                tail = Call(document, "Range", last, last + 1);
                Set(tail, "Text", "?");
                Call(document, "UndoClear");
                record = Get(app, "UndoRecord");
                Call(record, "StartCustomRecord", "Earlier scratch edit");
                try
                {
                    Set(tail, "Text", ".");
                }
                finally
                {
                    Call(record, "EndCustomRecord");
                }
            }
            finally
            {
                Release(record);
                Release(tail);
                Release(content);
            }

            Set(document, "TrackRevisions", tracking);
        }

        // Primitive/COM arguments and serialized output permit the baseline and
        // candidate assemblies to run side by side without sharing adapter types.
        public static string PerformanceExecute(object app, object document, long window, string fixtureJson, bool tracking)
        {
            var json = new JavaScriptSerializer();
            var item = json.Deserialize<WordPerformanceCase>(fixtureJson);
            IntPtr owner = new IntPtr(window);
            int checks = 0;
            Action<bool, string> require = (condition, message) =>
            {
                WordPerformanceSupport.Require(condition, message);
                checks++;
            };
            int[] focus = null;
            // Match the resident's native focus proof. Repeated UIA focus
            // queries during editing can report transitional provider state.
            Func<bool> context = () => Native.GetForegroundWindow() == owner && (focus == null || EditGuard.SameFocus(focus, Native.FocusIdentity()));
            require(context(), "Owned scratch body must retain foreground focus");
            PerformancePrepare(app, document, item, tracking);
            string original = PerformancePrefix + item.input + PerformanceSuffix;
            string observedSeed = VisibleScratch(document);
            require(observedSeed == original, "Fixture seed has exact visible body text; expected=" + json.Serialize(original) + " observed=" + json.Serialize(observedSeed));
            var fonts = PerformanceFonts(document, original.Length);
            var initial = Projection(document);
            int begin = initial.NativeOffset(PerformancePrefix.Length), end = initial.NativeOffset(PerformancePrefix.Length + item.input.Length);
            object range = Call(document, "Range", item.kind == "caret" ? end : begin, end);
            try
            {
                Call(range, "Select");
            }
            finally
            {
                Release(range);
            }

            int stable = 0;
            for (int poll = 0; poll < 50 && stable < 2; poll++)
            {
                require(context(), "Scratch setup stops on a native foreground/focus change; expected=" + owner.ToInt64() + " " + PerformanceFocus());
                stable = WordPasteOptions.DocumentFocused(owner) ? stable + 1 : 0;
                if (stable < 2)
                    System.Threading.Thread.Sleep(20);
            }

            require(stable == 2, "Scripted body setup publishes stable focus before timing");
            focus = Native.FocusIdentity();
            require(focus != null && context(), "Capture the same native focus identity used by the resident");
            object revisions = Get(document, "Revisions");
            int beforeRevisions;
            try
            {
                beforeRevisions = Convert.ToInt32(Get(revisions, "Count"));
            }
            finally
            {
                Release(revisions);
            }

            require(beforeRevisions == (item.deletion >= 0 ? 1 : 0), "Each repetition starts with the prescribed revision state");
            var spans = item.italics.Select(span => new ItalicSpan(span.start, span.length, true, span.italic)).ToArray();
            WordSelection.FontSnapshot snapshot = null;
            WordSelection.ReplayAnchor replay = null;
            var nativeTrace = new List<string>();
            var previousTrace = WordSelection.Trace;
            var watch = Stopwatch.StartNew();
            try
            {
                WordSelection.Trace = nativeTrace.Add;
                if (item.kind == "caret")
                {
                    var caret = SafeEdit.ReadCaret(owner, false);
                    require(caret != null && caret.Text == item.input && caret.Edit != null && context() && caret.Edit.AtOwnedCaret(), "Discover the exact owned caret word");
                    // Exercise the production UIA request without its keyboard
                    // fallback: this harness never injects Windows input.
                    caret.Edit.RequestSelection();
                    for (int poll = 0; poll < 25 && context() && (!caret.Edit.ExactSelection() || !WordPasteOptions.DocumentFocused(owner)); poll++)
                        System.Threading.Thread.Sleep(10);
                    require(context() && caret.Edit.ExactSelection(), "UIA selects the exact caret word without keyboard fallback");
                }
                else
                {
                    bool present;
                    var selected = SafeEdit.ReadSelected(owner, out present);
                    require(present && selected != null && selected.Expected == item.input && selected.ExactSelection(), "Read the exact selected word/passage; present=" + present + " expected=" + json.Serialize(item.input) + " observed=" + json.Serialize(selected == null ? null : selected.Expected));
                }

                bool handled;
                Action<WordSelection.FontSnapshot> capture = item.kind == "caret" || item.id == "selected-word" ? (Action<WordSelection.FontSnapshot>)(value => snapshot = value) : null;
                bool converted = WordSelection.Replace(owner, item.input, item.output, context, out handled, spans, item.kind != "caret", captureFonts: capture, captureReplay: value => replay = value);
                bool resumed = converted && replay != null && replay.Resume(item.output, item.kind != "caret", false);
                watch.Stop();
                require(converted && handled, "Native conversion commits through the Word adapter; handled=" + handled + " native_context=" + context());
                require(resumed, "Finalize the owned continuation and its grouped undo record; anchor=" + (replay != null));
                require(capture == null || snapshot != null, "Word conversions retain original-font ownership");
                // Revision/font inspection can itself reset pending typing
                // format. Observe it immediately after resident finalization.
                object selection = Get(app, "Selection"), selectedRange = null, typingFont = null;
                try
                {
                    int a = Convert.ToInt32(Get(selection, "Start")), b = Convert.ToInt32(Get(selection, "End"));
                    if (item.kind == "caret")
                    {
                        typingFont = Get(selection, "Font");
                        int typingItalic = Convert.ToInt32(Get(typingFont, "Italic"));
                        require(typingItalic == 0, "Subsequent typing retains original regular formatting; observed=" + typingItalic);
                        require(a == b && a == Projection(document).NativeOffset(PerformancePrefix.Length + item.output.Length), "Caret finishes at the converted word end");
                    }
                    else
                    {
                        selectedRange = Get(selection, "Range");
                        var selectedMap = (WordSelection.VisibleMap)typeof(WordSelection).GetMethod("Visible", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { selectedRange });
                        require(a != b && selectedMap != null && selectedMap.Value == item.output, "Converted word/passage stays selected");
                    }
                }
                finally
                {
                    Release(typingFont);
                    Release(selectedRange);
                    Release(selection);
                }

                string expectedText = PerformancePrefix + item.output + PerformanceSuffix;
                require(VisibleScratch(document) == expectedText, "Independent XML oracle verifies output and untouched neighboring text");
                var afterFonts = PerformanceFonts(document, expectedText.Length);
                for (int n = 0; n < expectedText.Length; n++)
                {
                    int offset = n - PerformancePrefix.Length;
                    int originalIndex = offset < 0 ? n : offset >= item.output.Length ? n + item.input.Length - item.output.Length : PerformancePrefix.Length + (item.input.Length == item.output.Length ? offset : 0);
                    var expectedFont = (string[])fonts[originalIndex].Clone();
                    if (offset >= 0 && offset < item.output.Length)
                        foreach (var span in item.italics)
                            if (offset >= span.start && offset < span.start + span.length)
                                expectedFont[3] = span.italic ? "-1" : "0";
                    require(expectedFont.SequenceEqual(afterFonts[n]), "Font name/size/bold/italic/color/underline, style and language remain correct at visible offset " + n + "; expected=" + json.Serialize(expectedFont) + " observed=" + json.Serialize(afterFonts[n]));
                }

                require(WordPerformanceSupport.WaitForOwnedForeground(owner.ToInt64(), () => Native.GetForegroundWindow().ToInt64(), context, () => System.Threading.Thread.Sleep(20)), "Scratch body still owns focus before undo");
                require(Convert.ToBoolean(Call(document, "Undo", 1)), "One native undo succeeds");
                string undoneText = VisibleScratch(document);
                require(undoneText == original, "One native undo restores the exact original visible body; observed=" + json.Serialize(undoneText));
                var undone = PerformanceFonts(document, original.Length);
                require(fonts.Count == undone.Count && fonts.Zip(undone, (a, b) => a.SequenceEqual(b)).All(value => value), "One undo restores original mixed fonts and neighboring formatting");
                revisions = Get(document, "Revisions");
                try
                {
                    require(Convert.ToInt32(Get(revisions, "Count")) == beforeRevisions, "Undo restores the original tracked-revision population");
                }
                finally
                {
                    Release(revisions);
                }

                require(Convert.ToBoolean(Get(document, "TrackRevisions")) == tracking, "Conversion/undo preserve the requested Track Changes state");
                require(Convert.ToBoolean(Call(document, "Undo", 1)), "The earlier native edit remains undoable");
                require(VisibleScratch(document) == original.Substring(0, original.Length - 1) + "?", "Second undo restores the earlier scratch edit without changing the original query");
                return json.Serialize(new { elapsed_ms = watch.Elapsed.TotalMilliseconds, checks = checks, native_trace = nativeTrace.ToArray() });
            }
            catch (Exception failure)
            {
                failure.Data["native_trace"] = nativeTrace.ToArray();
                throw;
            }
            finally
            {
                if (replay != null)
                    replay.FinishRecord();
                if (snapshot != null)
                    snapshot.Dispose();
                WordSelection.Trace = previousTrace;
            }
        }

        static string PerformanceHash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public static int PerformanceMain(string[] args)
        {
            if (args.Length == 1 && args[0] == WordFocusMonitor.Argument)
                return WordFocusMonitor.HelperMain();
            if (args.Length == 4 && args[0] == "--self-test")
            {
                string selfReport = Path.GetFullPath(args[3]);
                WordPerformanceSupport.Require(selfReport.StartsWith(Path.Combine(Path.GetFullPath(args[1]), "out", "word-performance-tests") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !File.Exists(selfReport), "Fresh isolated self-test report required");
                int count = WordPerformanceContracts.Run(File.ReadAllText(args[2]));
                File.WriteAllText(selfReport, new JavaScriptSerializer().Serialize(new { status = "passed", assertions = count, word_started = false, desktop_inspected = false }));
                return 0;
            }

            if (args.Length != 9 || args[0] != "--desktop" || args[7] != "same" && args[7] != "different")
            {
                Console.WriteLine("No Word test run. Explicit --desktop arguments are required.");
                return 2;
            }

            string root = Path.GetFullPath(args[1]), reportPath = Path.GetFullPath(args[4]);
            WordPerformanceSupport.Require(reportPath.StartsWith(Path.Combine(root, "out", "word-performance-tests") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !File.Exists(reportPath), "Fresh comparison report under out/word-performance-tests required");
            var plan = WordPerformanceSupport.Parse(File.ReadAllText(args[2]));
            plan = WordPerformanceSupport.Filter(plan, args[8]);
            var trials = WordPerformanceSupport.Schedule(plan, int.Parse(args[5], CultureInfo.InvariantCulture), int.Parse(args[6], CultureInfo.InvariantCulture));
            var json = new JavaScriptSerializer();
            var samples = new List<WordPerformanceSample>();
            var report = new Dictionary<string, object>
            {
                {
                    "schema",
                    1
                },
                {
                    "status",
                    "running"
                },
                {
                    "scope",
                    "Known-output body discovery/selection, native transaction and continuation finalization; excludes engine/mode selection, resident dispatch, physical Ctrl, OS input replay, paste UI and Modern Comments"
                },
                {
                    "fixture_sha256",
                    PerformanceHash(args[2])
                },
                {
                    "candidate_binary_sha256",
                    PerformanceHash(Assembly.GetExecutingAssembly().Location)
                },
                {
                    "baseline_binary_sha256",
                    PerformanceHash(args[3])
                },
                {
                    "samples",
                    samples
                }
            };
            report["selected_cases"] = plan.cases.Select(item => item.id).ToArray();
            report["full_matrix"] = args[8] == "all";
            int[] existing = Process.GetProcessesByName("WINWORD").Select(process =>
            {
                using (process)
                    return process.Id;
            }).ToArray();
            object app = null, documents = null, document = null, revisionView = null, revisionFilter = null, originalView = null, originalMarkup = null;
            bool owned = false;
            int result = 1;
            try
            {
                Native.DpiAware();
                foreach (int key in new[]
                {
                    0x10,
                    0x11,
                    0x12,
                    0xA0,
                    0xA1,
                    0xA2,
                    0xA3
                }

                )
                    WordPerformanceSupport.Require((Native.GetAsyncKeyState(key) & 0x8000) == 0, "Release physical modifiers before the native test");
                var baseline = Assembly.LoadFrom(Path.GetFullPath(args[3])).GetType("KeyJaya.NativeWordBindingTests", true).GetMethod("PerformanceExecute");
                app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application", true));
                documents = Get(app, "Documents");
                WordPerformanceSupport.Require(Convert.ToInt32(Get(documents, "Count")) == 0, "Refuse a Word instance containing existing documents");
                document = Call(documents, "Add");
                object window = Get(document, "ActiveWindow");
                IntPtr owner;
                try
                {
                    owner = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                }
                finally
                {
                    Release(window);
                }

                uint process;
                Native.GetWindowThreadProcessId(owner, out process);
                WordPerformanceSupport.Require(process != 0 && !existing.Contains((int)process), "Refuse a pre-existing/shared Word process");
                owned = true;
                object styles = Get(document, "Styles"), characterStyle = null, styleFont = null;
                try
                {
                    characterStyle = Call(styles, "Add", PerformanceStyleName, 2);
                    styleFont = Get(characterStyle, "Font");
                    Set(styleFont, "Name", "Georgia");
                    Set(styleFont, "Italic", 0);
                    Set(styleFont, "Bold", 0);
                    Set(styleFont, "Color", 8388736);
                }
                finally
                {
                    Release(styleFont);
                    Release(characterStyle);
                    Release(styles);
                }

                // Match the final-text XML oracle, then restore the inherited
                // display settings before closing this exclusively owned app.
                object revisionWindow = Get(document, "ActiveWindow");
                try
                {
                    revisionView = Get(revisionWindow, "View");
                }
                finally
                {
                    Release(revisionWindow);
                }

                revisionFilter = Get(revisionView, "RevisionsFilter");
                originalView = Get(revisionFilter, "View");
                originalMarkup = Get(revisionFilter, "Markup");
                report["initial_revision_view"] = originalView;
                report["initial_revision_markup"] = originalMarkup;
                Set(revisionFilter, "View", 0); // wdRevisionsViewFinal
                Set(revisionFilter, "Markup", 0); // wdRevisionsMarkupNone
                report["fixture_revision_view"] = "final/no-markup";
                report["owned_word_process"] = process;
                report["word_version"] = Convert.ToString(Get(app, "Version"));
                Set(app, "DisplayAlerts", 0);
                Set(app, "Visible", true);
                Call(app, "Activate");
                Native.SetForegroundWindow(owner);
                WordPerformanceSupport.Require(Native.GetForegroundWindow() == owner, "Could not focus the exclusively owned scratch Word window");
                foreach (var trial in trials)
                {
                    WordPerformanceSupport.Require(Native.GetForegroundWindow() == owner && Convert.ToInt32(Get(documents, "Count")) == 1, "Focus changed or another document joined the scratch instance; stop the comparison");
                    var sample = new WordPerformanceSample
                    {
                        id = trial.fixture.id,
                        variant = trial.variant,
                        tracking = trial.tracking,
                        iteration = trial.iteration,
                        warmup = trial.warmup,
                        status = "failed"
                    };
                    samples.Add(sample);
                    object[] parameters =
                    {
                        app,
                        document,
                        owner.ToInt64(),
                        json.Serialize(trial.fixture),
                        trial.tracking
                    };
                    string response = trial.variant == "baseline" ? (string)baseline.Invoke(null, parameters) : PerformanceExecute(app, document, owner.ToInt64(), (string)parameters[3], trial.tracking);
                    var observed = json.Deserialize<Dictionary<string, object>>(response);
                    sample.elapsed_ms = Convert.ToDouble(observed["elapsed_ms"], CultureInfo.InvariantCulture);
                    sample.checks = Convert.ToInt32(observed["checks"], CultureInfo.InvariantCulture);
                    sample.native_trace = json.ConvertToType<string[]>(observed["native_trace"]);
                    sample.status = "passed";
                    File.WriteAllText(reportPath, json.Serialize(report), new UTF8Encoding(false));
                }

                WordPerformanceSupport.ValidateSamples(trials, samples);
                report["same_adapter_source"] = args[7] == "same";
                report["scenarios"] = WordPerformanceSupport.Summarize(trials, samples, args[7] == "same");
                report["status"] = "passed";
                result = 0;
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["failure_foreground"] = PerformanceFocus();
                var failure = error.InnerException ?? error;
                if (failure.Data.Contains("native_trace"))
                    report["failure_trace"] = failure.Data["native_trace"];
                report["error"] = failure.ToString();
            }
            finally
            {
                try
                {
                    if (owned && revisionFilter != null && originalView != null && originalMarkup != null)
                    {
                        Set(revisionFilter, "View", originalView);
                        Set(revisionFilter, "Markup", originalMarkup);
                        WordPerformanceSupport.Require(Convert.ToInt32(Get(revisionFilter, "View")) == Convert.ToInt32(originalView) && Convert.ToInt32(Get(revisionFilter, "Markup")) == Convert.ToInt32(originalMarkup), "Restore inherited revision display before scratch shutdown");
                        report["revision_display_restored"] = true;
                    }

                    if (owned && Convert.ToInt32(Get(documents, "Count")) == 1)
                        Call(app, "Quit", 0);
                    else if (document != null)
                        Call(document, "Close", 0);
                }
                catch (Exception error)
                {
                    report["cleanup_error"] = error.ToString();
                    report["status"] = "failed";
                    result = 1;
                }
                finally
                {
                    Release(revisionFilter);
                    Release(revisionView);
                    Release(document);
                    Release(documents);
                    Release(app);
                }

                File.WriteAllText(reportPath, json.Serialize(report), new UTF8Encoding(false));
            }

            Console.WriteLine("Word comparison " + report["status"] + ": " + samples.Count + "/" + trials.Count + " validated samples; " + reportPath);
            return result;
        }
    }

    public static class WordPerformanceTests
    {
        [STAThread]
        public static int Main(string[] args)
        {
            return NativeWordBindingTests.PerformanceMain(args);
        }
    }
}
