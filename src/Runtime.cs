// KeyJaya: Resident event loop and coordination of hooks, asynchronous conversion, UI, and input recovery.
// Every edit belongs to a foreground window and generation; completion must also settle queued input.
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Globalization;

namespace KeyJaya
{
    /// <summary>
    /// Coordinates the resident's STA message loop, editor workers, and input state.
    /// Expensive target discovery runs outside the hook callback. Before applying
    /// a result, its window, focus identity, and generation must still agree.
    /// </summary>
    public sealed partial class Resident : Form
    {
        Core core;
        ShortcutSettings shortcutSettings;
        readonly string root;
        // This gate owns generation/busy/queue transitions. It is not permission
        // to edit an external app: the SafeEdit/context checks must also pass.
        readonly object sync = new object ();
        readonly InputState state = new InputState();
        readonly ClipboardCycle clipboardCycle = new ClipboardCycle();
        bool selectionClipboardCycle;
        readonly CorrectionQueue pendingCorrections = new CorrectionQueue();
        readonly CorrectionQueue pendingPunctuation = new CorrectionQueue();
        readonly bool probe;
        readonly LetterCycle letters = new LetterCycle();
        readonly LeftControlTap controlTap = new LeftControlTap();
        readonly ConversionShortcuts conversionShortcuts = new ConversionShortcuts();
        readonly CaretWordCycle caretWords = new CaretWordCycle();
        readonly CaretTapQueue caretTaps = new CaretTapQueue();
        FontEncoding fontEncoding;
        string caretCharacter;
        IntPtr caretCharacterOwner;
        bool letterUndo, wordUndo;
        string letterUndoMode;
        long caretPhysicalGeneration = -1, caretConvertedGeneration = -1;
        SafeEdit caretConvertedAnchor;
        string caretConvertedExpected;
        int wordUndoOffset;
        SafeEdit wordUndoAnchor;
        WordSelection.FontSnapshot wordUndoFonts;
        SafeEdit selectedCycleEdit, selectionUndoEdit;
        string selectionUndoOriginal, selectedCtrlMode;
        bool selectionUndoKeyDown;
        bool manualInputActive, manualInputReady, manualInputSelected;
        SafeEdit manualInputEdit;
        WordSelection.ReplayAnchor manualWordAnchor;
        string manualInputOutput;
        readonly Native.CorrectionInputHold correctionInputHold = new Native.CorrectionInputHold();
        string correctionHoldOriginal, correctionHoldOutput;
        IntPtr correctionHoldOwner;
        int[] correctionHoldFocus;
        bool correctionHoldReady, correctionHoldTouched, correctionInputDraining, correctionDrainWaiting;
        SafeEdit correctionHoldEdit;
        readonly CorrectionTyping correctionTyping = new CorrectionTyping();
        readonly DelimiterReplay delimiterReplay = new DelimiterReplay();
        readonly WordPasteContext wordPasteContext = new WordPasteContext();
        EncodingSettings encodingSettings;
        FormattingSettings formattingSettings;
        bool encodingChanged;
        DateTime caretDiscoveryAfter, pendingWordTapUntil;
        IntPtr dismissingWordPaste, pendingWordTapOwner;
        long physicalRevision;
        uint mouseClipboardSequence;
        string mouseClipboardWord;
        readonly System.Windows.Forms.Timer manualRetry = new System.Windows.Forms.Timer
        {
            Interval = 40
        };
        int manualRetryAttempts;
        readonly System.Windows.Forms.Timer fontExpiry = new System.Windows.Forms.Timer
        {
            Interval = 11000
        };
        readonly NotifyIcon tray = new NotifyIcon();
        readonly TrayPopupMenu trayMenu = new TrayPopupMenu();
        readonly bool trayPreview;
        readonly Native.Hook keyboard, mouse;
        readonly Native.WinEvent foreground;
        IntPtr keyHook, mouseHook, focusHook, controlFocusHook;
        bool busy;
        Process cheatProcess;
        TriggerEditorWindow triggerEditor;
        bool triggerEditorQueued;
        IntPtr referenceOwner;
        bool referenceWindow;
        bool capsOn = (Native.GetKeyState(0x14) & 1) != 0;
        readonly uint taskbar = Native.RegisterWindowMessage("TaskbarCreated");
        DateTime expiry, diagnosticUntil;
        int diagnosticCount;
        readonly string logPath;
        void Diagnostic(string message)
        {
            if (DateTime.UtcNow > diagnosticUntil || Interlocked.Increment(ref diagnosticCount) > 2000)
                return;
            Log("diagnostic " + message);
        }

        public Resident(string path, bool dryRun = false, bool preview = false, bool diagnostic = false)
        {
            if (diagnostic)
            {
                diagnosticUntil = DateTime.UtcNow.AddMinutes(15);
                SafeEdit.Trace = Diagnostic;
                EmbeddedWebViewFocus.Trace = Diagnostic;
                WordSelection.Trace = Diagnostic;
                WordApplication.Trace = Diagnostic;
                ClipboardBackup.Trace = Diagnostic;
                InDesignSelection.Trace = Diagnostic;
                InDesignFindField.Trace = Diagnostic;
                ExcelCellField.Trace = Diagnostic;
            }

            manualRetry.Tick += (sender, args) =>
            {
                manualRetry.Stop();
                if (manualInputReady && ++manualRetryAttempts < 25)
                    ReleaseCorrectionInput();
                else if (manualInputReady)
                    ReleaseFailedManualInput();
                else if (manualWordAnchor != null)
                    manualWordAnchor.FinishRecord();
            };
            fontExpiry.Tick += (sender, args) =>
            {
                lock (sync)
                {
                    if (wordUndoFonts == null || DateTime.UtcNow >= expiry)
                    {
                        ClearWordUndoFonts();
                        fontExpiry.Stop();
                    }
                }
            };
            root = path;
            shortcutSettings = new ShortcutSettings(Path.Combine(Configuration.Folder(path), "shortcuts.ini"));
            probe = dryRun;
            trayPreview = preview;
            core = new Core(path);
            fontEncoding = new FontEncoding(path);
            logPath = Path.Combine(path, "logs", "keyjaya.log");
            encodingSettings = new EncodingSettings(Path.Combine(Configuration.Folder(path), "settings.ini"));
            formattingSettings = new FormattingSettings(Path.Combine(Configuration.Folder(path), "settings.ini"));
            Text = LaunchRequests.Caption(root);
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Opacity = 0;
            Width = 1;
            Height = 1;
            keyboard = Keyboard;
            mouse = Mouse;
            foreground = Foreground;
            tray.Text = "KeyJaya " + Version.Current;
            tray.MouseUp += TrayClick;
            trayMenu.Opening += (sender, args) =>
            {
                UpdateIcon();
                RefreshMenu();
            };
            trayMenu.Closing += (sender, args) =>
            {
                var selected = trayMenu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(item => item.Selected);
                if (args.CloseReason == ToolStripDropDownCloseReason.ItemClicked && selected != null && ((uint)selected.Tag == 19 || (uint)selected.Tag == 20 || (uint)selected.Tag == 30 || (uint)selected.Tag == 31 || (uint)selected.Tag == 32 || ((uint)selected.Tag >= 22 && (uint)selected.Tag <= 26)))
                    args.Cancel = true;
            };
            BuildMenu();
            tray.ContextMenuStrip = trayMenu;
            UpdateIcon();
            tray.Visible = true;
            if (trayPreview)
            {
                FormBorderStyle = FormBorderStyle.FixedDialog;
                Opacity = 1;
                ShowInTaskbar = true;
                Text = "KeyJaya tray test";
                ClientSize = new Size(320, 100);
                StartPosition = FormStartPosition.CenterScreen;
                var open = new Button
                {
                    Text = "Open tray menu",
                    Dock = DockStyle.Fill
                };
                open.Click += (sender, args) => ShowMenu();
                Controls.Add(open);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(trayPreview && value);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (trayPreview)
                return;
            Register();
            keyHook = Native.SetWindowsHookEx(13, keyboard, Native.GetModuleHandle(null), 0);
            mouseHook = Native.SetWindowsHookEx(14, mouse, Native.GetModuleHandle(null), 0);
            focusHook = Native.SetWinEventHook(3, 3, IntPtr.Zero, foreground, 0, 0, 0);
            controlFocusHook = Native.SetWinEventHook(0x8005, 0x8005, IntPtr.Zero, foreground, 0, 0, 0);
            if (keyHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
                throw new InvalidOperationException("Keyboard/mouse hook could not be registered");
            Log("ready " + Version.Current);
        }

        void UnregisterShortcuts()
        {
            for (int i = 1; i <= 8; i++)
                Native.UnregisterHotKey(Handle, i);
        }

        bool RegisterShortcuts(ShortcutSettings settings)
        {
            bool ok = true;
            for (int n = 0; n < ShortcutSettings.Roles.Length; n++)
            {
                var binding = settings[ShortcutSettings.Roles[n]];
                if (binding.Tap != null)
                    continue;
                if (!Native.RegisterHotKey(Handle, n + 1, 0x4000 | binding.Modifiers, binding.Key))
                {
                    ok = false;
                    Log("shortcut unavailable: " + binding.Text + " error=" + Marshal.GetLastWin32Error());
                }
            }

            return ok;
        }

        void Register()
        {
            RegisterShortcuts(shortcutSettings);
        }

        void DispatchShortcut(string mode, bool literal = false)
        {
            IntPtr inputOwner = WordPasteOptions.Owner(Native.GetForegroundWindow());
            if (!manualInputActive && !BeginManualInput(inputOwner))
                return;
            try
            {
                if (mode == "word")
                    PrepareClipboardShortcut(null, true);
                else if (mode == "letter")
                {
                    IntPtr owner = WordPasteOptions.Owner(Native.GetForegroundWindow());
                    lock (sync)
                    {
                        if (busy)
                            caretTaps.Add(owner, state.Generation, false);
                        else
                            DiscoverLetter(owner, "auto", literal);
                    }
                }
                else
                    PrepareClipboardShortcut(mode);
            }
            finally
            {
                if (!busy)
                    FinishManualInput();
            }
        }

        void CustomizeShortcuts()
        {
            using (var window = new ShortcutWindow(shortcutSettings, values =>
            {
                lock (sync)
                {
                    if (busy)
                        throw new InvalidOperationException("Finish the current conversion first.");
                    var next = new ShortcutSettings(Path.Combine(Configuration.Folder(root), "shortcuts.ini"), values);
                    UnregisterShortcuts();
                    try
                    {
                        if (!state.Paused && !RegisterShortcuts(next))
                            throw new InvalidOperationException("A shortcut is already in use. Choose another combination.");
                        next.Save();
                        shortcutSettings = next;
                        conversionShortcuts.Clear();
                        ResetInput();
                    }
                    catch
                    {
                        UnregisterShortcuts();
                        if (!state.Paused)
                            Register();
                        throw;
                    }
                }

                CloseCheatSheet();
                RefreshMenu();
            }))
                window.ShowDialog();
        }

        internal const int ShortcutRequestMessage = 0x8000 + 34, CorrectionProbeMessage = 0x8000 + 35, TriggerEditorRequestMessage = 0x8000 + 36;
        protected override void WndProc(ref Message m)
        {
            if (LaunchRequests.Message != 0 && (uint)m.Msg == LaunchRequests.Message)
            {
                m.Result = new IntPtr(1);
                BeginInvoke((Action)OpenCheatSheet);
                return;
            }

            if (m.Msg == TriggerEditorRequestMessage)
            {
                if (cheatProcess != null && !cheatProcess.HasExited && m.WParam.ToInt64() == cheatProcess.Id)
                    BeginInvoke((Action)(() =>
                    {
                        CloseCheatSheet();
                        QueueTriggerEditor();
                    }));
                return;
            }

            if (m.Msg == CorrectionProbeMessage)
            {
                m.Result = DateTime.UtcNow <= diagnosticUntil && correctionInputHold.Active ? new IntPtr(correctionInputHold.Count + 1) : IntPtr.Zero;
                return;
            }

            if (m.Msg == ShortcutRequestMessage)
            {
                if (cheatProcess != null && !cheatProcess.HasExited && m.WParam.ToInt64() == cheatProcess.Id)
                    BeginInvoke((Action)(() =>
                    {
                        CloseCheatSheet();
                        CustomizeShortcuts();
                    }));
                return;
            }

            if (m.Msg == 0x312)
            {
                int id = m.WParam.ToInt32();
                if (id >= 1 && id <= 8)
                    DispatchShortcut(ShortcutSettings.Roles[id - 1]);
                return;
            }

            if ((uint)m.Msg == taskbar)
            {
                tray.Visible = false;
                UpdateIcon();
                tray.Visible = true;
            }

            base.WndProc(ref m);
        }

        void RecordCaretCharacter(string text, IntPtr owner, bool physical = false)
        {
            caretConvertedAnchor = null;
            caretConvertedExpected = null;
            caretConvertedGeneration = -1;
            caretPhysicalGeneration = physical ? state.Generation + 1 : -1;
            caretCharacter = string.IsNullOrEmpty(text) ? null : text.Substring(text.Length - 1);
            caretCharacterOwner = owner;
        }

        void ClearWordUndoFonts()
        {
            if (wordUndoFonts != null)
            {
                wordUndoFonts.Dispose();
                wordUndoFonts = null;
            }
        }

        void ResetInput(bool preserveControl = false)
        {
            correctionTyping.Clear();
            ClearWordUndoFonts();
            selectedCtrlMode = null;
            selectedCycleEdit = null;
            selectionUndoEdit = null;
            selectionUndoOriginal = null;
            wordPasteContext.Clear();
            pendingWordTapOwner = IntPtr.Zero;
            caretDiscoveryAfter = DateTime.UtcNow.AddMilliseconds(40);
            caretCharacter = null;
            caretCharacterOwner = IntPtr.Zero;
            caretPhysicalGeneration = -1;
            caretConvertedAnchor = null;
            caretConvertedExpected = null;
            caretConvertedGeneration = -1;
            state.Clear();
            clipboardCycle.Clear();
            letters.Clear();
            if (preserveControl)
                controlTap.Rebase(state.Generation);
            else
                controlTap.Cancel();
            caretWords.Clear();
            caretTaps.Clear();
            letterUndo = false;
            wordUndo = false;
            wordUndoAnchor = null;
            delimiterReplay.Clear();
        }

        void Foreground(IntPtr hook, uint evt, IntPtr hwnd, int obj, int child, uint thread, uint time)
        {
            if (FocusEventGuard.Ignore(evt, Native.GetAncestor(hwnd, 2), Native.GetForegroundWindow()))
                return;
            if (correctionInputHold.Active)
                BeginInvoke((Action)(() =>
                {
                    if (correctionInputDraining)
                        DrainCorrectionInput();
                    else
                        ReleaseCorrectionInput();
                }));
            InDesignFindField.Invalidate();
            referenceOwner = IntPtr.Zero;
            lock (sync)
            {
                IntPtr owner = WordPasteOptions.Owner(Native.GetForegroundWindow());
                bool paste = wordPasteContext.Owns(owner, Native.GetClipboardSequenceNumber());
                if (owner == dismissingWordPaste && WordPasteOptions.DocumentFocused(owner))
                    return;
                if (paste)
                    return;
                if (owner == pendingWordTapOwner && DateTime.UtcNow <= pendingWordTapUntil && WordPasteGuard.KeepFocusTransition(true, true, WordPasteOptions.DocumentFocused(owner), WordPasteOptions.FocusedMenu(owner)))
                    return;
                bool preserve = WordPasteOptions.IsWord(owner) && controlTap.Pending(owner, DateTime.UtcNow);
                if (manualInputActive && owner == correctionHoldOwner && EditGuard.SameFocus(correctionHoldFocus, FocusId()))
                    return;
                ExcelCellField.Invalidate();
                if (manualWordAnchor != null)
                    manualWordAnchor.FinishRecord();
                if (busy)
                    Diagnostic("focus reset event=" + evt + " preserve=" + preserve + " source=" + NativeClass(hwnd) + " focused=" + NativeFocusClass());
                ResetInput(preserve);
            }
        }

        static string NativeClass(IntPtr hwnd)
        {
            var text = new StringBuilder(64);
            Native.GetClassName(hwnd, text, 64);
            return text.ToString();
        }

        static string NativeFocusClass()
        {
            var id = Native.FocusIdentity();
            return id == null ? "none" : NativeClass(new IntPtr((uint)id[4] | ((long)id[5] << 32)));
        }

        void ObserveWordPaste(IntPtr owner)
        {
            uint sequence = Native.GetClipboardSequenceNumber();
            wordPasteContext.Mark(owner, sequence);
            BeginInvoke((Action)(() =>
            {
                bool document = WordPasteOptions.DocumentFocused(owner);
                lock (sync)
                {
                    if (document)
                        wordPasteContext.Verify(owner, sequence);
                }
            }));
        }

        void ObserveWordMousePaste(IntPtr owner, long revision)
        {
            BeginInvoke((Action)(() =>
            {
                try
                {
                    uint sequence = Native.GetClipboardSequenceNumber();
                    string copied;
                    lock (sync)
                    {
                        if (state.Paused || physicalRevision != revision || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()))
                            return;
                        if (mouseClipboardSequence != sequence)
                        {
                            mouseClipboardSequence = sequence;
                            mouseClipboardWord = null;
                            string text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
                            string word = WordPasteContext.ClipboardWord(text);
                            if (word != null && (core.CtrlExpansion(word, "unicode") != null || core.CaretChoices(word, "unicode").Length > 0))
                                mouseClipboardWord = word;
                        }

                        copied = mouseClipboardWord;
                    }

                    if (copied == null)
                        return;
                    Task.Run(() =>
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            lock (sync)
                                if (state.Paused || physicalRevision != revision || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()) || sequence != Native.GetClipboardSequenceNumber())
                                    return;
                            if (WordPasteOptions.DocumentFocused(owner))
                            {
                                var target = SafeEdit.ReadCaret(owner, false);
                                if (target != null && WordPasteContext.Matches(copied, target.Text))
                                {
                                    lock (sync)
                                    {
                                        if (!state.Paused && physicalRevision == revision && owner == Native.GetForegroundWindow() && sequence == Native.GetClipboardSequenceNumber())
                                        {
                                            wordPasteContext.Mark(owner, sequence);
                                            wordPasteContext.Verify(owner, sequence);
                                        }
                                    }

                                    return;
                                }
                            }

                            if (i < 7)
                                Thread.Sleep(20);
                        }
                    });
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                }
            }));
        }

        IntPtr MouseCore(int code, IntPtr w, IntPtr l, bool drained)
        {
            if (code >= 0 && drained && !correctionInputHold.Owns(Native.GetForegroundWindow()))
            {
                correctionInputHold.DeferInFlight(correctionHoldOwner);
                Diagnostic("queued mouse event deferred after focus change");
                return new IntPtr(1);
            }

            if (code >= 0 && !drained && correctionInputHold.Active && correctionInputHold.Mouse((Native.MouseHookData)Marshal.PtrToStructure(l, typeof(Native.MouseHookData)), w.ToInt32(), Native.GetForegroundWindow()))
                return new IntPtr(1);
            if (code >= 0 && (w.ToInt32() == 0x201 || w.ToInt32() == 0x204 || w.ToInt32() == 0x207 || w.ToInt32() == 0x20A || w.ToInt32() == 0x20E))
            {
                lock (sync)
                {
                    physicalRevision++;
                    InDesignFindField.Invalidate();
                    ExcelCellField.Invalidate();
                    ResetInput();
                }
            }

            if (code >= 0 && w.ToInt32() == 0x202)
            {
                lock (sync)
                {
                    IntPtr owner = WordPasteOptions.Owner(Native.GetForegroundWindow());
                    if (!state.Paused && WordPasteOptions.IsWord(owner))
                        ObserveWordMousePaste(owner, physicalRevision);
                }
            }

            return Native.CallNextHookEx(mouseHook, code, w, l);
        }

        IntPtr KeyboardCore(int code, IntPtr w, IntPtr l, bool drained)
        {
            if (code < 0)
                return Native.CallNextHookEx(keyHook, code, w, l);
            if (drained && !correctionInputHold.Owns(Native.GetForegroundWindow()))
            {
                correctionInputHold.DeferInFlight(correctionHoldOwner);
                Diagnostic("queued key deferred after focus change");
                return new IntPtr(1);
            }

            Native.KeyData k = (Native.KeyData)Marshal.PtrToStructure(l, typeof(Native.KeyData));
            if (k.extra.ToUInt64() == Native.OwnCorrectionInput)
            {
                if (correctionInputHold.Active && !correctionInputHold.Owns(Native.GetForegroundWindow()) && (k.flags & 0x80) == 0)
                    return new IntPtr(1);
                return Native.CallNextHookEx(keyHook, code, w, l);
            }

            if (k.extra.ToUInt64() == Native.OwnInput)
                return Native.CallNextHookEx(keyHook, code, w, l);
            bool replayed = drained || k.extra.ToUInt64() == Native.ReplayedInput;
            if (!drained && (replayed || (k.flags & 0x10) == 0) && correctionInputHold.Active && correctionInputHold.Keyboard(k, w.ToInt32(), Native.GetForegroundWindow()))
                return new IntPtr(1);
            if (w.ToInt32() == 0x100 || w.ToInt32() == 0x104)
            {
                bool control = LeftControlTap.IsLeftControl(k.vk, k.flags) || LeftControlTap.IsRightControl(k.vk, k.flags);
                bool consumed = shortcutSettings.Mode(k.vk, (Native.GetAsyncKeyState(0x11) & 0x8000) != 0, (Native.GetAsyncKeyState(0x10) & 0x8000) != 0, (Native.GetAsyncKeyState(0x12) & 0x8000) != 0, (Native.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5C) & 0x8000) != 0) != null;
                if ((k.flags & 0x10) != 0 || state.Paused || !control && !consumed)
                {
                    InDesignFindField.Invalidate();
                    ExcelCellField.Invalidate();
                }
            }

            if ((w.ToInt32() == 0x100 || w.ToInt32() == 0x104) && !LeftControlTap.IsLeftControl(k.vk, k.flags))
                lock (sync)
                    physicalRevision++;
            // Lookup text in our own reference window must remain literal.
            if (IsReferenceWindow(Native.GetForegroundWindow()))
            {
                lock (sync)
                    ResetInput();
                return Native.CallNextHookEx(keyHook, code, w, l);
            }

            if ((k.flags & 0x10) != 0 && !replayed)
            {
                lock (sync)
                {
                    if (!delimiterReplay.Consume(k.vk, k.scan, k.flags, w.ToInt32(), state.Generation, Native.GetForegroundWindow(), DateTime.UtcNow))
                    {
                        ResetInput();
                    }
                }

                return Native.CallNextHookEx(keyHook, code, w, l);
            }

            bool down = w.ToInt32() == 0x100 || w.ToInt32() == 0x104;
            if (k.vk == 0x5A)
            {
                bool consumed = false;
                SafeEdit undoEdit = null;
                string undoText = null;
                lock (sync)
                {
                    if (!down && selectionUndoKeyDown)
                    {
                        selectionUndoKeyDown = false;
                        consumed = true;
                    }
                    else if (down && selectionUndoKeyDown)
                        consumed = true;
                    else if (down && !state.Paused && !busy && selectionUndoEdit != null && (Native.GetAsyncKeyState(0x11) & 0x8000) != 0 && (Native.GetAsyncKeyState(0x10) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5B) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5C) & 0x8000) == 0)
                    {
                        undoEdit = selectionUndoEdit;
                        undoText = selectionUndoOriginal;
                        selectionUndoEdit = null;
                        selectionUndoOriginal = null;
                        selectionUndoKeyDown = true;
                        controlTap.Cancel();
                        consumed = true;
                    }
                }

                if (undoEdit != null)
                {
                    BeginManualInput(undoEdit.Window, k.vk);
                    SafeEdit restoreEdit = undoEdit;
                    string restoreText = undoText;
                    BeginInvoke((Action)(() =>
                    {
                        if (restoreEdit.ExactSelection())
                            ConvertSelected(restoreEdit, "unicode", false, false, false, restoreText, true);
                    }));
                }

                if (consumed)
                    return new IntPtr(1);
            }

            string shortcutMode = shortcutSettings.Mode(k.vk, (Native.GetAsyncKeyState(0x11) & 0x8000) != 0, (Native.GetAsyncKeyState(0x10) & 0x8000) != 0, (Native.GetAsyncKeyState(0x12) & 0x8000) != 0, (Native.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5C) & 0x8000) != 0), dispatch;
            lock (sync)
            {
                if (conversionShortcuts.Handle(k.vk, down, w.ToInt32() == 0x101 || w.ToInt32() == 0x105, shortcutMode, state.Paused, out dispatch))
                {
                    controlTap.Cancel();
                    if (dispatch != null)
                    {
                        Diagnostic("shortcut hook queued mode=" + dispatch);
                        string mode = dispatch;
                        long stamp = state.Generation;
                        IntPtr shortcutOwner = WordPasteOptions.Owner(Native.GetForegroundWindow());
                        if (!BeginManualInput(shortcutOwner, k.vk))
                            return new IntPtr(1);
                        if (WordPasteOptions.IsWord(shortcutOwner))
                        {
                            pendingWordTapOwner = shortcutOwner;
                            pendingWordTapUntil = DateTime.UtcNow.AddSeconds(3);
                        }

                        BeginInvoke((Action)(() =>
                        {
                            lock (sync)
                            {
                                if (state.Paused || state.Generation != stamp || WordPasteOptions.Owner(Native.GetForegroundWindow()) != shortcutOwner)
                                {
                                    Diagnostic("shortcut callback cancelled");
                                    FinishManualInput();
                                    return;
                                }
                            }

                            Diagnostic("shortcut callback entered");
                            DispatchShortcut(mode, mode == "letter" && shortcutSettings["letter"].Text == "`");
                        }));
                    }

                    return new IntPtr(1);
                }
            }

            string tapRole = shortcutSettings.TapRole(k.vk, k.flags);
            if (tapRole == null && (LeftControlTap.IsLeftControl(k.vk, k.flags) || LeftControlTap.IsRightControl(k.vk, k.flags)))
            {
                lock (sync)
                    controlTap.Cancel();
                return Native.CallNextHookEx(keyHook, code, w, l);
            }

            if (tapRole != null)
            {
                lock (sync)
                {
                    IntPtr window = WordPasteOptions.Owner(Native.GetForegroundWindow());
                    if (state.Paused)
                        controlTap.Cancel();
                    else if (down)
                    {
                        if ((Native.GetAsyncKeyState(0x10) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(LeftControlTap.IsLeftControl(k.vk, k.flags) ? 0xA3 : 0xA2) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5B) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5C) & 0x8000) == 0)
                            controlTap.Down(window, state.Generation, DateTime.UtcNow);
                        else
                            controlTap.Cancel();
                    }
                    else if ((w.ToInt32() == 0x101 || w.ToInt32() == 0x105) && controlTap.Up(window, state.Generation, DateTime.UtcNow))
                    {
                        Diagnostic("Ctrl tap accepted role=" + tapRole);
                        if (!BeginManualInput(window, k.vk, true))
                        {
                            Diagnostic("Ctrl tap input hold unavailable");
                            return Native.CallNextHookEx(keyHook, code, w, l);
                        }

                        long stamp = state.Generation;
                        if (WordPasteOptions.IsWord(window))
                        {
                            pendingWordTapOwner = window;
                            pendingWordTapUntil = DateTime.UtcNow.AddMilliseconds(1500);
                        }

                        BeginInvoke((Action)(() =>
                        {
                            lock (sync)
                            {
                                if (state.Generation != stamp || state.Paused || WordPasteOptions.Owner(Native.GetForegroundWindow()) != window)
                                {
                                    Diagnostic("Ctrl tap callback cancelled");
                                    FinishManualInput();
                                    return;
                                }

                                Diagnostic("Ctrl tap callback entered busy=" + busy);
                                if (busy)
                                    caretTaps.Add(window, stamp, tapRole == "word");
                                else
                                    DispatchShortcut(tapRole);
                            }
                        }));
                    }
                }

                return Native.CallNextHookEx(keyHook, code, w, l);
            }

            if (!down)
                return Native.CallNextHookEx(keyHook, code, w, l);
            lock (sync)
            {
                controlTap.Cancel();
                wordPasteContext.Clear();
            }

            IntPtr owner = Native.GetForegroundWindow();
            string candidate = null;
            long generation = 0;
            lock (sync)
            {
                if (state.Paused)
                    return Native.CallNextHookEx(keyHook, code, w, l);
                bool modifier = k.vk == 0x10 || k.vk == 0xA0 || k.vk == 0xA1 || k.vk == 0x11 || k.vk == 0xA2 || k.vk == 0xA3;
                bool conversionKey = shortcutMode != null;
                if (!modifier && !conversionKey)
                    clipboardCycle.Clear();
                if (k.vk == 8)
                {
                    caretWords.Clear();
                    caretTaps.Clear();
                }

                // Modified Backspace must still reach the editor.
                if (k.vk == 8 && state.UndoOriginal != null && state.Owner == owner && !busy && DateTime.UtcNow <= expiry && (Native.GetAsyncKeyState(0x11) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x10) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5B) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5C) & 0x8000) == 0)
                {
                    string original = state.UndoOriginal, output = state.UndoOutput, originalMode = letterUndoMode;
                    bool undoLetter = letterUndo, undoWord = wordUndo;
                    int undoOffset = wordUndoOffset;
                    SafeEdit undoAnchor = wordUndoAnchor;
                    wordUndoAnchor = null;
                    letterUndo = false;
                    wordUndo = false;
                    letters.Clear();
                    core.CancelSpellingChoice();
                    if (undoLetter)
                        state.CancelManualConversion();
                    else
                        state.Clear();
                    busy = true;
                    long undoGeneration = state.Generation;
                    BeginManualInput(owner, k.vk);
                    if (probe)
                    {
                        busy = false;
                        return Native.CallNextHookEx(keyHook, code, w, l);
                    }

                    BeginInvoke((Action)(() => Undo(owner, original, output, undoGeneration, undoLetter, originalMode, undoWord, undoOffset, undoAnchor)));
                    return new IntPtr(1);
                }

                if (k.vk == 8)
                {
                    bool plain = (Native.GetAsyncKeyState(0x11) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x10) & 0x8000) == 0;
                    if (plain && state.BackspaceTyped())
                    {
                        ClearWordUndoFonts();
                        selectedCtrlMode = null;
                        selectedCycleEdit = null;
                        selectionUndoEdit = null;
                        selectionUndoOriginal = null;
                        correctionTyping.Clear();
                        letters.Clear();
                        caretWords.Clear();
                        caretTaps.Clear();
                        controlTap.Cancel();
                        delimiterReplay.Clear();
                        caretCharacter = null;
                        caretPhysicalGeneration = -1;
                        letterUndo = wordUndo = false;
                    }
                    else
                        ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if (k.vk == 0x14)
                {
                    capsOn = !capsOn;
                    ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if (k.vk == 0x10 || k.vk == 0xA0 || k.vk == 0xA1)
                    return Native.CallNextHookEx(keyHook, code, w, l);
                if (k.vk == 0x11 || k.vk == 0xA2 || k.vk == 0xA3 || k.vk == 0x12 || k.vk == 0xA4 || k.vk == 0xA5 || k.vk == 0x5B || k.vk == 0x5C)
                {
                    ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if ((Native.GetAsyncKeyState(0x11) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x12) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x5C) & 0x8000) != 0)
                {
                    bool paste = k.vk == 0x56 && (Native.GetAsyncKeyState(0x11) & 0x8000) != 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && WordPasteOptions.IsWord(owner);
                    ResetInput();
                    if (paste)
                        ObserveWordPaste(owner);
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if (k.vk == 0x2D && (Native.GetAsyncKeyState(0x10) & 0x8000) != 0 && WordPasteOptions.IsWord(owner))
                {
                    ResetInput();
                    ObserveWordPaste(owner);
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if (k.vk == 13 || k.vk == 9)
                {
                    char delimiter = k.vk == 13 ? '\n' : '\t';
                    string completed = state.CompletedEnglish(delimiter);
                    state.Type(delimiter);
                    correctionTyping.Append(delimiter, state.Generation, owner);
                    if (completed != null && core.MayCorrectCompletedWord(completed))
                    {
                        long stamp = state.Generation;
                        delimiterReplay.Begin(stamp, owner, delimiter, DateTime.UtcNow);
                        if (busy)
                            pendingCorrections.Set(completed, owner, stamp);
                        else if (BeginCorrectionTyping(stamp, owner, completed))
                        {
                            busy = true;
                            ExpandEnglish(owner, completed, stamp);
                        }
                    }
                    else
                        ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                if (k.vk >= 0x21 && k.vk <= 0x2E || k.vk == 27)
                {
                    ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                byte[] keys = new byte[256];
                Native.GetKeyboardState(keys);
                keys[k.vk] = 0x80;
                keys[0x14] = capsOn ? (byte)1 : (byte)0;
                keys[0x10] = keys[0xA0] = keys[0xA1] = 0;
                if ((Native.GetAsyncKeyState(0x10) & 0x8000) != 0)
                    keys[0x10] = 0x80;
                StringBuilder text = new StringBuilder(8);
                uint pid;
                uint tid = Native.GetWindowThreadProcessId(owner, out pid);
                int count = Native.ToUnicodeEx(k.vk, k.scan, keys, text, 8, 4, Native.GetKeyboardLayout(tid));
                if (count != 1)
                {
                    ResetInput();
                    return Native.CallNextHookEx(keyHook, code, w, l);
                }

                char c = text[0];
                caretWords.Clear();
                caretTaps.Clear();
                RecordCaretCharacter(c.ToString(), owner, true);
                letters.Observe(c, owner);
                letterUndo = false;
                wordUndo = false;
                if (state.QueueCycle(c, owner, DateTime.UtcNow))
                {
                    if (!busy)
                        BeginCycle();
                    return new IntPtr(1);
                }

                string punctuationCandidate = state.CompletedEllipsis(c);
                string englishCandidate = state.CompletedEnglish(c);
                candidate = state.Type(c, core);
                generation = state.Generation;
                correctionTyping.Append(c, generation, owner);
                if (englishCandidate != null && core.MayCorrectCompletedWord(englishCandidate) && !core.IsDirectTrigger(candidate))
                    candidate = null;
                if (punctuationCandidate != null)
                {
                    delimiterReplay.Begin(generation, owner, c, DateTime.UtcNow);
                    if (busy)
                        pendingPunctuation.Set(punctuationCandidate, owner, generation);
                    else
                    {
                        busy = true;
                        ExpandPunctuation(owner, punctuationCandidate, generation);
                    }
                }
                else if (candidate != null && candidate.Length > 1 && !busy)
                {
                    Diagnostic("trigger recognized length=" + candidate.Length);
                    busy = true;
                    string captured = candidate;
                    BeginInvoke((Action)(() => Expand(owner, captured, generation)));
                }
                else if (candidate == null && englishCandidate != null && core.MayCorrectCompletedWord(englishCandidate))
                {
                    delimiterReplay.Begin(generation, owner, c, DateTime.UtcNow);
                    if (busy)
                        pendingCorrections.Set(englishCandidate, owner, generation);
                    else if (BeginCorrectionTyping(generation, owner, englishCandidate))
                    {
                        busy = true;
                        string captured = englishCandidate;
                        ExpandEnglish(owner, captured, generation);
                    }
                }
            }

            return Native.CallNextHookEx(keyHook, code, w, l);
        }

        void DiscoverLetter(IntPtr owner, string mode, bool backtickFallback)
        {
            if (busy)
                return;
            caretWords.Clear();
            long generation = state.Generation;
            int[] field = FocusId();
            string expected = CaretLetterDiscovery.Expected(caretCharacterOwner == owner, caretPhysicalGeneration == state.Generation, caretCharacter);
            busy = true;
            DateTime settleAfter = caretDiscoveryAfter;
            Task.Run(() =>
            {
                while (DateTime.UtcNow < settleAfter)
                {
                    lock (sync)
                    {
                        if (state.Generation != generation || state.Paused)
                        {
                            Complete();
                            return;
                        }
                    }

                    Thread.Sleep(10);
                }

                SafeEdit.CaretTarget target = CaretLetterDiscovery.Read(() => SafeEdit.ReadCaret(owner, true), observed =>
                {
                    SafeEdit previousAnchor;
                    string previousExpected;
                    bool sameGeneration;
                    lock (sync)
                    {
                        previousAnchor = caretConvertedAnchor;
                        previousExpected = caretConvertedExpected;
                        sameGeneration = caretConvertedGeneration == generation;
                    }

                    bool sameLocation = sameGeneration && observed.Edit != null && observed.Edit.SameLetterLocation(previousAnchor);
                    return CaretLetterDiscovery.Accept(observed.Text, expected, previousExpected, sameLocation) && (!SingleLetters.Supported(observed.Text) || mode != "auto" || encodingSettings.Resolve(fontEncoding, observed.Font, observed.Mixed) != null);
                }, () =>
                {
                    lock (sync)
                        return state.Generation == generation && !state.Paused;
                }, milliseconds => Thread.Sleep(milliseconds));
                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        lock (sync)
                        {
                            busy = false;
                            if (state.Generation != generation || state.Paused || Native.GetForegroundWindow() != owner || (field != null && !EditGuard.SameFocus(field, FocusId())))
                                return;
                            string format = mode == "auto" ? (target == null ? null : encodingSettings.Resolve(fontEncoding, target.Font, target.Mixed)) : mode;
                            if (target != null && SingleLetters.Supported(target.Text) && format != null)
                            {
                                if (letters.Current != target.Text || letters.Owner != owner)
                                    letters.Observe(target.Text[0], owner);
                                if (letters.Queue(format, owner))
                                    BeginLetter();
                            }
                            else if (backtickFallback && CaretLetterDiscovery.MayTypeLiteral(target != null, target != null && SingleLetters.Supported(target.Text), target != null && target.Edit != null && target.Edit.AtOwnedCaret()))
                            {
                                ResetInput();
                                InDesignFindField.Invalidate();
                                ExcelCellField.Invalidate();
                                Native.TypeCharacter("`");
                            }
                            else
                                Diagnostic("letter lookup unavailable or encoding unresolved; no literal backtick inserted");
                        }
                    }
                    finally
                    {
                        lock (sync)
                            if (!busy)
                                Complete();
                    }
                }));
            });
        }

        void DiscoverWord(IntPtr owner)
        {
            Diagnostic("word discovery entered busy=" + busy + " probe=" + probe + " focus=" + NativeFocusClass());
            if (busy)
                return;
            long generation = state.Generation;
            int[] field = FocusId();
            string last = caretCharacterOwner == owner ? caretCharacter : null;
            busy = true;
            Task.Run(() =>
            {
                SafeEdit edit = null;
                bool inserted = false, delegated = false;
                try
                {
                    if (probe || !WaitModifiers(generation))
                        return;
                    bool selectionPresent;
                    SafeEdit selected = SafeEdit.ReadSelected(owner, out selectionPresent);
                    Diagnostic("word initial selection present=" + selectionPresent + " readable=" + (selected != null) + " excel=" + ExcelCellField.Focused(owner));
                    if (selectionPresent)
                    {
                        if (selected == null)
                            return;
                        string selectedMode = selected.SelectionEncoding(fontEncoding, encodingSettings, core);
                        if (selectedMode == null)
                            return;
                        BeginInvoke((Action)(() =>
                        {
                            bool current;
                            lock (sync)
                            {
                                busy = false;
                                current = state.Generation == generation && !state.Paused && owner == Native.GetForegroundWindow();
                            }

                            if (!current)
                            {
                                Complete();
                                return;
                            }

                            ConvertSelected(selected, selectedMode, false, false, true);
                            if (!busy)
                                FinishManualInput();
                        }));
                        delegated = true;
                        return;
                    }

                    if (WordPasteOptions.IsWord(owner))
                    {
                        Thread.Sleep(40);
                        lock (sync)
                            if (state.Generation != generation || state.Paused || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()))
                                return;
                        bool pasted;
                        lock (sync)
                            pasted = wordPasteContext.Verified(owner, Native.GetClipboardSequenceNumber());
                        bool pasteMenu = WordPasteOptions.FocusedMenu(owner);
                        Diagnostic("word paste verified=" + pasted + " menu=" + pasteMenu);
                        if (pasted || pasteMenu)
                        {
                            lock (sync)
                            {
                                if (state.Generation != generation || state.Paused || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()))
                                    return;
                                dismissingWordPaste = owner;
                            }

                            try
                            {
                                bool settled = WordPasteRecovery.Dismiss(() =>
                                {
                                    lock (sync)
                                        return state.Generation == generation && !state.Paused && owner == WordPasteOptions.Owner(Native.GetForegroundWindow());
                                }, () => WordPasteOptions.FocusedMenu(owner), () => WordPasteOptions.DocumentFocused(owner), () => Native.Escape(), milliseconds => Thread.Sleep(milliseconds), pasted);
                                Diagnostic("word paste dismissed=" + settled);
                                if (!settled)
                                    return;
                            }
                            finally
                            {
                                lock (sync)
                                    dismissingWordPaste = IntPtr.Zero;
                            }

                            last = null;
                            field = FocusId();
                        }

                        for (int i = 0; i < 15 && !WordPasteOptions.DocumentFocused(owner); i++)
                        {
                            lock (sync)
                                if (state.Generation != generation || state.Paused || owner != Native.GetForegroundWindow())
                                    return;
                            Thread.Sleep(20);
                        }

                        if (!WordPasteGuard.Settled(WordPasteOptions.DocumentFocused(owner), WordPasteOptions.FocusedMenu(owner)))
                        {
                            Diagnostic("word document not settled");
                            return;
                        }

                        field = FocusId();
                    }

                    if (field == null)
                        return;
                    SafeEdit.CaretTarget target = null;
                    string known;
                    SafeEdit priorAnchor;
                    lock (sync)
                    {
                        known = caretWords.Expected(owner, field);
                        priorAnchor = wordUndoAnchor;
                    }

                    if (known != null && priorAnchor != null && RichClipboardTarget(owner))
                    {
                        target = priorAnchor.ReadReplacementCaret(known);
                        Diagnostic("rich cycle anchored=" + (target != null));
                    }

                    for (int i = 0; target == null && i < 25; i++)
                    {
                        bool physical;
                        lock (sync)
                        {
                            if (state.Generation != generation || state.Paused)
                                return;
                            physical = caretPhysicalGeneration == generation;
                        }

                        target = SafeEdit.ReadCaret(owner, false);
                        if (target != null && target.Edit != null && (!physical || CaretWords.MatchesPhysical(target.Text, target.Edit.CaretOffset, target.Edit.CaretTail, last)))
                            break;
                        target = null;
                        Thread.Sleep(20);
                    }

                    if (target == null)
                    {
                        Diagnostic("word caret discovery unavailable");
                        return;
                    }

                    string mode = target.Edit.SelectionEncoding(fontEncoding, encodingSettings, core);
                    if (mode == null)
                    {
                        Diagnostic("word encoding unavailable");
                        return;
                    }

                    if (target.Edit.IsMarkdownSource)
                        Diagnostic("markdown word discovered characters=" + target.Text.Length + " wrapped=" + (SafeEdit.MarkdownWord(target.Text) != target.Text) + " offset=" + target.Edit.CaretOffset);
                    string next;
                    bool repeat;
                    string cycleInput = target.Edit.IsMarkdownSource ? SafeEdit.MarkdownWord(target.Text) : target.Text;
                    string shortOutput = core.CtrlExpansion(cycleInput, mode);
                    bool shortExpansion = shortOutput != null;
                    bool scriptureExpansion = shortExpansion && Core.IsScriptureReference(cycleInput);
                    string[] choices = null;
                    Diagnostic("word resolved characters=" + target.Text.Length + " offset=" + target.Edit.CaretOffset + " reference=" + Core.IsScriptureReference(cycleInput) + " expansion=" + shortExpansion + " mode=" + mode);
                    lock (sync)
                    {
                        if (state.Generation != generation || state.Paused || Native.GetForegroundWindow() != owner || !EditGuard.SameFocus(field, FocusId()))
                        {
                            Diagnostic("word discovery context changed generation=" + (state.Generation != generation) + " owner=" + (Native.GetForegroundWindow() != owner) + " field=" + !EditGuard.SameFocus(field, FocusId()));
                            return;
                        }

                        repeat = caretWords.Next(target.Text, mode, owner, field, out next);
                    }

                    if (shortExpansion)
                    {
                        repeat = false;
                        next = shortOutput;
                        if (target.Edit.IsMarkdownSource && formattingSettings.Italicize)
                            next = target.Edit.MarkdownOutput(next, core, mode);
                        choices = new string[0];
                    }
                    else if (target.Edit.MixedWordUnicode != null)
                    {
                        repeat = false;
                        next = core.Word(target.Edit.MixedWordUnicode, mode);
                        choices = new[]
                        {
                            next
                        }.Concat(core.ClipboardChoices(target.Edit.MixedWordUnicode, mode, "unicode")).Distinct().ToArray();
                    }
                    else if (!repeat)
                    {
                        core.FinishSpellingChoice();
                        var plan = core.PrepareWordConversion(cycleInput, mode, target.Edit.ContextThroughWord());
                        choices = plan.Choices;
                        if (target.Edit.IsMarkdownSource && formattingSettings.Italicize)
                            choices = choices.Select(v => v == EncodingMap.Ascii(v) ? v : target.Edit.MarkdownOutput(v, core, mode)).Distinct().ToArray();
                        if (choices.Length == 0)
                        {
                            Diagnostic("word has no choices characters=" + target.Text.Length + " offset=" + target.Edit.CaretOffset + " mode=" + mode);
                            return;
                        }

                        string preserved = encodingChanged ? Core.ReencodeCurrent(cycleInput, mode) : null;
                        next = preserved != null && choices.Contains(preserved) ? preserved : Core.NextSpelling(target.Text, choices, plan.Contextual);
                        if (next == target.Text && (!formattingSettings.Italicize || !WordPasteOptions.IsWord(owner) && !target.Edit.UsesNativeReplacement && !target.Edit.IsMarkdownSource && !RichClipboardTarget(owner) || core.ItalicWords(next, mode).Length == 0))
                            return;
                    }

                    if (WordPasteOptions.IsWord(owner) && !WordPasteGuard.Settled(WordPasteOptions.DocumentFocused(owner), WordPasteOptions.FocusedMenu(owner)))
                    {
                        Diagnostic("word popup reacquired focus");
                        return;
                    }

                    edit = target.Edit;
                    ManualTarget(edit);
                    Func<bool> wordContext = () =>
                    {
                        lock (sync)
                            return state.Generation == generation && !state.Paused && owner == Native.GetForegroundWindow() && EditGuard.SameFocus(field, FocusId());
                    };
                    if (edit == null || !edit.Select(wordContext) || !WaitModifiers(generation) || !edit.ReadyForPaste())
                    {
                        Diagnostic("word selection not ready");
                        return;
                    }

                    ManualSelectionVerified(edit, repeat ? wordUndoFonts : null);
                    if (!repeat)
                        lock (sync)
                            ClearWordUndoFonts();
                    WordSelection.FontSnapshot originalFonts;
                    lock (sync)
                        originalFonts = wordUndoFonts;
                    bool restoringOrigin = repeat && originalFonts != null && next == originalFonts.Original;
                    bool wordHandled;
                    inserted = scriptureExpansion ? WordSelection.ReplaceExpansion(owner, target.Text, next, wordContext, out wordHandled, formattingSettings.Italicize ? core.CtrlItalics(cycleInput, next, mode) : null, false, manualInputActive ? manualWordAnchor : null) : WordSelection.Replace(owner, target.Text, next, wordContext, out wordHandled, restoringOrigin ? null : formattingSettings.Italicize ? core.CtrlItalics(cycleInput, next, mode) : null, false, restoringOrigin ? null : edit.BalaramWordFont, restoringOrigin ? originalFonts : null, !repeat && !shortExpansion ? (Action<WordSelection.FontSnapshot>)(snapshot =>
                    {
                        lock (sync)
                        {
                            if (state.Generation == generation && !state.Paused)
                                wordUndoFonts = snapshot;
                            else
                                snapshot.Dispose();
                        }

                        BeginInvoke((Action)(() =>
                        {
                            fontExpiry.Stop();
                            fontExpiry.Start();
                        }));
                    }) : null, restoringOrigin ? null : core.CompoundFontSpans(target.Text, next, mode), manualInputActive ? manualWordAnchor : null);
                    if (!wordHandled)
                        inserted = ReplaceRichCaret(edit, next, mode, wordContext);
                    if (!inserted)
                        return;
                    encodingChanged = false;
                    BeginInvoke((Action)(() =>
                    {
                        lock (sync)
                        {
                            if (wordUndoFonts != null)
                            {
                                fontExpiry.Stop();
                                fontExpiry.Start();
                            }
                        }
                    }));
                    int desired = scriptureExpansion ? next.Length + edit.CaretTail.Length : shortExpansion ? next.Length + edit.CaretTail.Length : CaretWords.ReplacementOffset(edit.CaretOffset, target.Text.Length, next.Length);
                    ManualCommitted(edit, next);
                    if (!edit.PositionAfterReplacement(next, desired, wordContext))
                        Diagnostic("converted word caret position could not be restored");
                    lock (sync)
                    {
                        if (state.Generation != generation || state.Paused)
                            return;
                        string original = repeat && caretWords.Original != null ? caretWords.Original : target.Text;
                        int originalOffset = repeat ? wordUndoOffset : edit.CaretOffset;
                        letters.Clear();
                        clipboardCycle.Clear();
                        letterUndo = false;
                        wordUndo = !shortExpansion && (next != original || repeat || WordPasteOptions.IsWord(owner) && wordUndoFonts != null);
                        wordUndoOffset = originalOffset;
                        if (!repeat)
                            wordUndoAnchor = edit;
                        state.Token = next;
                        state.RecordReplacement(original, next, owner, !wordUndo && !shortExpansion);
                        RecordCaretCharacter(next, owner);
                        expiry = DateTime.UtcNow.AddSeconds(10);
                        state.Candidates = null;
                        if (shortExpansion)
                        {
                            core.CancelSpellingChoice();
                            caretWords.Clear();
                        }
                        else
                        {
                            core.RememberCaretSpelling(cycleInput, edit.IsMarkdownSource ? SafeEdit.MarkdownWord(next) : next, mode, repeat, edit.OccurrenceKey());
                            if (repeat)
                                caretWords.Commit(next);
                            else
                                caretWords.Start(next, choices, mode, owner, field, target.Text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                }
                finally
                {
                    if (!inserted && edit != null)
                        edit.Cancel();
                    lock (sync)
                    {
                        if (!inserted)
                            caretWords.Clear();
                        if (state.Generation == generation)
                        {
                            wordPasteContext.Clear();
                            pendingWordTapOwner = IntPtr.Zero;
                        }
                    }

                    if (!delegated)
                        Complete();
                }
            });
        }

        // Called by caret discovery's worker. Clipboard API capture/restore stays on the UI STA.
        // Direct input handles only short unformatted text. Multiline/long/rich
        // output uses a temporary clipboard payload, content proof, then restoration.
        bool ReplaceRichCaret(SafeEdit edit, string output, string mode, Func<bool> context)
        {
            if (edit.UsesNativeReplacement)
            {
                SafeEdit origin = null;
                lock (sync)
                {
                    if (caretWords.Original == output)
                        origin = wordUndoAnchor;
                }

                return context() && edit.ReplaceInDesign(output, false, origin != null ? null : formattingSettings.Italicize ? core.CtrlItalics(SafeEdit.MarkdownWord(edit.Expected), output, mode) : null, origin);
            }

            if (!edit.MarkdownAllowed)
                return false;
            byte[] html = ConversionHtml(edit.Window, output, mode, Core.CtrlScriptureTrigger(SafeEdit.MarkdownWord(edit.Expected), mode));
            if (!Native.TextNeedsClipboard(output, html != null))
                return context() && edit.ExactSelection() && Native.TypeText(output);
            Diagnostic("caret replacement uses clipboard characters=" + output.Length + " formatted=" + (html != null));
            ClipboardBackup saved = null;
            uint temporary = 0;
            bool pasted = false;
            try
            {
                Invoke((Action)(() =>
                {
                    if (!context() || !edit.ExactSelection())
                        return;
                    saved = ClipboardBackup.Capture();
                    if (saved == null || saved.Sequence != Native.GetClipboardSequenceNumber())
                        return;
                    if (!Native.TemporaryClipboard(Handle, output, out temporary, html))
                    {
                        saved.Restore(Handle, Native.GetClipboardSequenceNumber());
                        return;
                    }

                    if (context() && edit.ExactSelection())
                        pasted = Native.Paste();
                    if (!pasted)
                        saved.Restore(Handle, temporary);
                }));
                if (!pasted)
                    return false;
                ManualCommitted(edit, output);
                bool verified = false;
                for (int i = 0; i < 50; i++)
                {
                    if (!context())
                        break;
                    if (SafeEdit.BehindCaret(edit.Window, output, false, false, true) != null || edit.VerifyReplacementCaret(output))
                    {
                        verified = true;
                        break;
                    }

                    Thread.Sleep(20);
                }

                Diagnostic("caret clipboard replacement verified=" + verified);
                return verified && context();
            }
            finally
            {
                if (pasted && saved != null)
                    Invoke((Action)(() => saved.Restore(Handle, temporary)));
            }
        }

        void BeginLetter()
        {
            string mode, next;
            if (!letters.Take(out mode, out next))
                return;
            string expected = letters.Current, originalMode = letters.Mode;
            IntPtr owner = letters.Owner;
            long generation = state.Generation, revision = letters.Revision;
            busy = true;
            Task.Run(() =>
            {
                SafeEdit edit = null;
                bool inserted = false;
                try
                {
                    if (probe || !WaitModifiers(generation))
                        return;
                    edit = PrepareOwnedEdit(owner, expected, generation, true);
                    if (edit == null || !WaitModifiers(generation) || !Insert(next, edit))
                        return;
                    inserted = true;
                    lock (sync)
                    {
                        if (state.Generation != generation || state.Paused || letters.Revision != revision)
                            return;
                        state.ReplaceLastLetter(expected[0], next[0]);
                        state.RecordReplacement(letters.Original ?? expected, next, owner);
                        RecordCaretCharacter(next, owner);
                        caretConvertedAnchor = edit;
                        caretConvertedExpected = next;
                        caretConvertedGeneration = generation;
                        letterUndo = true;
                        letterUndoMode = originalMode;
                        expiry = DateTime.UtcNow.AddSeconds(10);
                        letters.Commit(next, mode);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                }
                finally
                {
                    if (!inserted && edit != null)
                        edit.Cancel();
                    lock (sync)
                    {
                        if (!inserted && state.Generation == generation)
                            letters.ClearIfOwned(revision);
                    }

                    Complete();
                }
            });
        }

        void ExpandPunctuation(IntPtr owner, string input, long generation)
        {
            Task.Run(() =>
            {
                SafeEdit edit = null;
                bool inserted = false;
                try
                {
                    string output = EllipsisTyping.Replacement(input);
                    if (output == null || probe || !WaitModifiers(generation))
                        return;
                    int[] field = FocusId();
                    Func<bool> context = () =>
                    {
                        lock (sync)
                            return state.Generation == generation && !state.Paused && owner == Native.GetForegroundWindow() && EditGuard.SameFocus(field, FocusId());
                    };
                    string compact = WordPasteOptions.IsWord(owner) && input.TrimStart(' ', '\u00a0').StartsWith("...", StringComparison.Ordinal) ? (input[0] == ' ' || input[0] == '\u00a0' ? input[0].ToString() : "") + "…" + input[input.Length - 1] : null;
                    for (int poll = 0; poll < 25 && context(); poll++)
                    {
                        edit = SafeEdit.BehindCaret(owner, input);
                        if (edit == null && compact != null)
                            edit = SafeEdit.BehindCaret(owner, compact);
                        if (edit != null)
                            break;
                        Thread.Sleep(20);
                    }

                    if (edit == null || !edit.MarkdownAllowed || !edit.Select(context, SelectionCommit.KeyboardCount(edit.Expected) > 0) || !WaitModifiers(generation) || !edit.ReadyForPaste())
                        return;
                    if (edit.UsesNativeReplacement)
                        inserted = edit.ReplaceInDesign(output, false);
                    else
                    {
                        bool handled;
                        inserted = WordSelection.Replace(owner, edit.Expected, output, context, out handled, null, false);
                        if (!handled)
                            inserted = SelectionConversion.CanCommit(context, edit.ExactSelection) && Native.TypeText(output);
                    }

                    if (inserted)
                        lock (sync)
                        {
                            if (context())
                            {
                                state.RecordReplacement(input, output, owner, true);
                                RecordCaretCharacter(output, owner);
                                letterUndo = wordUndo = false;
                            }
                        }
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                }
                finally
                {
                    if (!inserted && edit != null)
                        edit.Cancel();
                    lock (sync)
                        delimiterReplay.Clear(generation);
                    Complete();
                }
            });
        }

        bool CorrectTypingAtCaret(IntPtr owner, string input, long generation)
        {
            // Run proof and the single native batch on the hook's UI thread, so a
            // pending physical key is observed before commit or after the full batch.
            bool inserted = false;
            int[] field = FocusId();
            var clock = Stopwatch.StartNew();
            for (int attempt = 0; attempt < 8 && clock.ElapsedMilliseconds < 200; attempt++)
            {
                string suffix;
                long stamp;
                lock (sync)
                {
                    stamp = state.Generation;
                    if (state.Paused || !correctionTyping.Read(generation, owner, stamp, out suffix))
                        return false;
                }

                SafeEdit edit = SafeEdit.BehindCaret(owner, input + suffix, false, true);
                if (edit == null && input.EndsWith(" ", StringComparison.Ordinal))
                    edit = SafeEdit.BehindCaret(owner, input.Substring(0, input.Length - 1) + "\u00a0" + suffix, false, true);
                if (edit == null)
                {
                    Thread.Sleep(5);
                    continue;
                }

                string observed = edit.Expected.Substring(0, input.Length), logical = observed.EndsWith("\u00a0", StringComparison.Ordinal) ? observed.Substring(0, observed.Length - 1) + " " : observed, output = core.CorrectCompletedWord(logical);
                if (output != null && observed.EndsWith("\u00a0", StringComparison.Ordinal))
                    output = output.Substring(0, output.Length - 1) + "\u00a0";
                if (output == null || Native.AutomaticCorrectionInput(observed, output, suffix) == null || !edit.MarkdownAllowed)
                    return false;
                Func<bool> current = () =>
                {
                    lock (sync)
                    {
                        string now;
                        return !state.Paused && state.Generation == stamp && Native.GetForegroundWindow() == owner && correctionTyping.Read(generation, owner, stamp, out now) && now == suffix && EditGuard.SameFocus(field, FocusId());
                    }
                };
                Invoke((Action)(() =>
                {
                    if (current() && edit.AtOwnedCaret() && current())
                    {
                        edit.CaptureStableStart();
                        correctionHoldEdit = edit;
                        correctionHoldTouched = true;
                        inserted = Native.CorrectAutomatic(observed, output, suffix);
                    }
                }));
                if (inserted)
                {
                    correctionHoldOutput = output + suffix;
                    correctionHoldOriginal = observed + suffix;
                    var settle = Stopwatch.StartNew();
                    long stable = -1, lastRecovery = 0;
                    bool verified = false;
                    while (settle.ElapsedMilliseconds < 900 && current())
                    {
                        var result = SafeEdit.BehindCaret(owner, output + suffix, false, true);
                        if (result != null && result.AtOwnedCaret())
                        {
                            if (stable < 0)
                                stable = settle.ElapsedMilliseconds;
                            if (settle.ElapsedMilliseconds - stable >= 40)
                            {
                                verified = true;
                                break;
                            }
                        }
                        else
                        {
                            stable = -1;
                            if (settle.ElapsedMilliseconds - lastRecovery >= 100)
                            {
                                Invoke((Action)(() =>
                                {
                                    if (current())
                                        edit.RecoverAutomaticCaret(output + suffix);
                                }));
                                lastRecovery = settle.ElapsedMilliseconds;
                            }
                        }

                        Thread.Sleep(5);
                    }

                    Diagnostic("automatic typing settled=" + verified + " ms=" + clock.ElapsedMilliseconds);
                    lock (sync)
                    {
                        state.RecordReplacement(observed, output, owner, true);
                        correctionTyping.Clear(generation);
                    }

                    return verified;
                }
            }

            return false;
        }

        void ExpandEnglish(IntPtr owner, string input, long generation)
        {
            Task.Run(() =>
            {
                SafeEdit edit = null;
                bool inserted = false;
                try
                {
                    string output = core.CorrectCompletedWord(input);
                    if (output == null || probe)
                        return;
                    if (!InDesignSelection.IsInDesign(owner))
                    {
                        lock (sync)
                        {
                            string suffix;
                            if (!correctionTyping.Read(generation, owner, state.Generation, out suffix) && state.Generation == generation && !state.Paused)
                                correctionTyping.Begin(generation, owner);
                        }

                        inserted = CorrectTypingAtCaret(owner, input, generation);
                        return;
                    }

                    if (!WaitModifiers(generation))
                        return;
                    edit = FindOwnedEdit(owner, input, generation, false, true);
                    if (edit == null)
                        return;
                    if (edit.Expected != input)
                    {
                        input = edit.Expected;
                        output = core.CorrectCompletedWord(input);
                        if (output == null)
                            return;
                    }

                    if (!edit.Select(() =>
                    {
                        lock (sync)
                            return state.Generation == generation && !state.Paused;
                    }, true))
                    {
                        Log("automatic correction selection not verified");
                        return;
                    }

                    if (!Insert(output, edit))
                        return;
                    inserted = true;
                    lock (sync)
                    {
                        if (state.Generation == generation && !state.Paused)
                        {
                            state.RecordReplacement(input, output, owner, true);
                            RecordCaretCharacter(output, owner);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                }
                finally
                {
                    if (!inserted && edit != null)
                        edit.Cancel();
                    lock (sync)
                    {
                        delimiterReplay.Clear(generation);
                        correctionTyping.Clear(generation);
                    }

                    Complete();
                    if (correctionInputHold.Active)
                    {
                        correctionHoldReady = true;
                        BeginInvoke((Action)ReleaseCorrectionInput);
                    }
                }
            });
        }

        void Expand(IntPtr owner, string trigger, long generation)
        {
            Task.Run(() =>
            {
                try
                {
                    core.FinishSpellingChoice();
                    string output = core.Trigger(trigger);
                    if (output == null || probe || !WaitModifiers(generation))
                    {
                        Complete();
                        return;
                    }

                    string query = trigger.Substring(0, trigger.Length - 1);
                    string[] choices = core.IsDirectTrigger(trigger) ? null : core.Cycle(query, trigger[trigger.Length - 1]);
                    Diagnostic("trigger worker entered");
                    SafeEdit edit = PrepareOwnedEdit(owner, trigger, generation, false, true);
                    Diagnostic("trigger selection=" + (edit != null));
                    if (edit != null && edit.Expected != trigger)
                    {
                        bool direct = core.IsDirectTrigger(trigger);
                        output = core.TriggerWithObservedCase(trigger, edit.Expected);
                        trigger = edit.Expected;
                        query = trigger.Substring(0, trigger.Length - 1);
                        choices = direct ? null : core.Cycle(query, trigger[trigger.Length - 1]);
                        if (output == null)
                        {
                            edit.Cancel();
                            Complete();
                            return;
                        }
                    }

                    BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            lock (sync)
                            {
                                if (state.Generation != generation || state.Paused)
                                {
                                    Diagnostic("trigger cancelled before insert");
                                    return;
                                }
                            }

                            if (edit == null || !Insert(output, edit, trigger.EndsWith("<") ? "balaram" : trigger.EndsWith("?") ? "plain" : "unicode", trigger))
                            {
                                Diagnostic("trigger insert rejected");
                                return;
                            }

                            Diagnostic("trigger inserted");
                            lock (sync)
                            {
                                state.RecordReplacement(trigger, output, owner);
                                RecordCaretCharacter(output, owner);
                                expiry = DateTime.UtcNow.AddSeconds(10);
                                state.Candidates = choices;
                                state.CycleMode = trigger.EndsWith("<") ? "balaram" : trigger.EndsWith("?") ? "plain" : "unicode";
                                if (state.Candidates != null && state.Candidates.Length < 2)
                                    state.Candidates = null;
                                state.CycleDeadline = DateTime.UtcNow.AddSeconds(10);
                                state.CycleIndex = state.Candidates == null ? 0 : Math.Max(0, Array.IndexOf(state.Candidates, output));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log(ex.GetType().Name);
                        }
                        finally
                        {
                            Complete();
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                    Complete();
                }
            });
        }

        SafeEdit FindOwnedEdit(IntPtr owner, string expected, long generation, bool singleLetter = false, bool allowCase = false)
        {
            for (int i = 0; i < 15; i++)
            {
                lock (sync)
                {
                    if (state.Generation != generation || state.Paused)
                    {
                        Diagnostic("caret search context cancelled");
                        return null;
                    }
                }

                if (Native.GetForegroundWindow() != owner)
                {
                    Diagnostic("caret search window changed");
                    return null;
                }

                SafeEdit edit = SafeEdit.BehindCaret(owner, expected, singleLetter, allowCase);
                if (edit != null)
                    return edit;
                Thread.Sleep(20);
            }

            return null;
        }

        SafeEdit PrepareOwnedEdit(IntPtr owner, string expected, long generation, bool singleLetter = false, bool allowCase = false)
        {
            SafeEdit edit = FindOwnedEdit(owner, expected, generation, singleLetter, allowCase);
            if (edit == null)
                return null;
            ManualTarget(edit);
            if (!edit.Select(() =>
            {
                lock (sync)
                    return state.Generation == generation && !state.Paused;
            }, singleLetter || SelectionCommit.KeyboardCount(edit.Expected) > 0))
            {
                Log("selection not verified");
                return null;
            }

            ManualSelectionVerified(edit);
            return edit;
        }

        bool WaitModifiers(long generation)
        {
            for (int i = 0; i < 25; i++)
            {
                lock (sync)
                {
                    if (state.Generation != generation || state.Paused)
                        return false;
                }

                if ((Native.GetAsyncKeyState(0x11) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x10) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x12) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5B) & 0x8000) == 0 && (Native.GetAsyncKeyState(0x5C) & 0x8000) == 0)
                    return true;
                Thread.Sleep(20);
            }

            return false;
        }

        void BeginCycle()
        {
            if (state.PendingCycles == 0 || state.Candidates == null)
                return;
            state.PendingCycles--;
            busy = true;
            int index = (state.CycleIndex + 1) % state.Candidates.Length;
            IntPtr owner = state.Owner;
            string previous = state.UndoOutput, next = state.Candidates[index];
            long generation = state.Generation;
            BeginInvoke((Action)(() => Cycle(owner, previous, next, index, generation)));
        }

        void Cycle(IntPtr owner, string previous, string next, int index, long generation)
        {
            Task.Run(() =>
            {
                try
                {
                    // SendInput paste is asynchronous. Wait briefly for that exact owned word to appear.
                    if (!WaitModifiers(generation))
                    {
                        Complete();
                        return;
                    }

                    SafeEdit edit = PrepareOwnedEdit(owner, previous, generation);
                    SafeEdit verified = edit;
                    BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            lock (sync)
                            {
                                if (state.Generation != generation || state.Paused)
                                    return;
                            }

                            if (verified != null && Insert(next, verified, state.CycleMode))
                            {
                                lock (sync)
                                {
                                    core.RememberSpelling(previous, next, state.CycleMode, "auto", verified.OccurrenceKey());
                                    state.UndoOutput = next;
                                    RecordCaretCharacter(next, owner);
                                    state.CycleIndex = index;
                                    expiry = DateTime.UtcNow.AddSeconds(10);
                                    state.CycleDeadline = expiry;
                                }
                            }
                            else
                            {
                                lock (sync)
                                    state.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (sync)
                                state.Clear();
                            Log(ex.GetType().Name);
                        }
                        finally
                        {
                            Complete();
                        }
                    }));
                }
                catch (Exception ex)
                {
                    lock (sync)
                        state.Clear();
                    Log(ex.GetType().Name);
                    Complete();
                }
            });
        }

        void Undo(IntPtr owner, string original, string output, long generation, bool singleLetter = false, string originalMode = null, bool directWord = false, int originalOffset = -1, SafeEdit wordAnchor = null)
        {
            Task.Run(() =>
            {
                SafeEdit edit = null;
                Func<bool> context = () =>
                {
                    lock (sync)
                        return state.Generation == generation && !state.Paused && owner == Native.GetForegroundWindow();
                };
                try
                {
                    if (!WaitModifiers(generation))
                        return;
                    if (directWord)
                    {
                        var target = SafeEdit.ReadCaret(owner, false);
                        edit = target != null && target.Text == output ? target.Edit : SafeEdit.BehindCaret(owner, output, false, false, true);
                        bool location = edit != null && edit.SameStart(wordAnchor);
                        Diagnostic("word undo target=" + (edit != null) + " location=" + location);
                        if (!location)
                            return;
                        ManualTarget(edit);
                        if (!edit.Select(context) || !edit.ReadyForPaste())
                        {
                            Diagnostic("word undo selection unavailable");
                            return;
                        }

                        ManualSelectionVerified(edit, wordUndoFonts);
                        bool replaced;
                        if (WordPasteOptions.IsWord(owner))
                        {
                            WordSelection.FontSnapshot fonts;
                            lock (sync)
                                fonts = wordUndoFonts;
                            if (fonts == null)
                                return;
                            bool handled;
                            replaced = WordSelection.Replace(owner, output, original, context, out handled, null, false, null, fonts, null, null, manualInputActive ? manualWordAnchor : null);
                        }
                        else
                            replaced = edit.UsesNativeReplacement ? edit.ReplaceInDesign(original, false, null, wordAnchor) : Native.TypeText(original);
                        if (!replaced)
                            return;
                        ManualCommitted(edit, original);
                        edit.PositionAfterReplacement(original, originalOffset < 0 ? original.Length : originalOffset, context);
                        lock (sync)
                        {
                            if (context())
                            {
                                RecordCaretCharacter(original, owner);
                                state.Token = original;
                            }
                        }

                        return;
                    }

                    edit = PrepareOwnedEdit(owner, output, generation, singleLetter);
                    if (edit == null || !edit.ReadyForPaste() || !Insert(original, edit))
                        return;
                    lock (sync)
                    {
                        if (context())
                        {
                            RecordCaretCharacter(original, owner);
                            if (singleLetter)
                            {
                                state.ReplaceLastLetter(output[0], original[0]);
                                letters.Observe(original[0], owner);
                                if (originalMode != null)
                                    letters.Commit(original, originalMode);
                                caretConvertedAnchor = edit;
                                caretConvertedExpected = original;
                                caretConvertedGeneration = generation;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.GetType().Name);
                }
                finally
                {
                    if (directWord)
                        lock (sync)
                            ClearWordUndoFonts();
                    Complete();
                }
            });
        }

        static int[] FocusId()
        {
            return Native.FocusIdentity();
        }

        sealed class ClipboardBackup
        {
            public static Action<string> Trace;
            static void Detail(string message)
            {
                if (Trace != null)
                    Trace("clipboard " + message);
            }

            readonly DataObject data;
            public readonly string Text;
            public readonly uint Sequence;
            ClipboardBackup(DataObject value, string text, uint sequence)
            {
                data = value;
                Text = text;
                Sequence = sequence;
            }

            public static ClipboardBackup Capture()
            {
                try
                {
                    uint sequence = Native.GetClipboardSequenceNumber();
                    IDataObject original = Clipboard.GetDataObject();
                    var stored = new DataObject();
                    if (original != null)
                        foreach (string format in original.GetFormats(false))
                            stored.SetData(format, false, SelectionConversion.CloneClipboardValue(original.GetData(format, false)));
                    string text = Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : "";
                    if (sequence != Native.GetClipboardSequenceNumber())
                        return null;
                    Detail("captured characters=" + text.Length);
                    return new ClipboardBackup(stored, text, sequence);
                }
                catch (Exception ex)
                {
                    Detail("capture failure=" + ex.GetType().Name);
                    return null;
                }
            }

            public bool Restore(IntPtr window, uint temporary)
            {
                uint current = Native.GetClipboardSequenceNumber();
                bool owned = Native.ClipboardOwner() == window;
                if (!Native.TemporaryClipboardOwned(window, temporary) && !Native.FailedTemporaryClipboardOwned(window, temporary))
                {
                    Detail("restore refused sequenceChanged=" + (temporary != current) + " ownerChanged=" + !owned);
                    return false;
                }

                try
                {
                    Clipboard.SetDataObject(data, true);
                    Native.ReleaseTemporaryClipboard(window, temporary);
                    Detail("restored characters=" + Text.Length);
                    return true;
                }
                catch (ExternalException ex)
                {
                    Detail("restore exception code=" + ex.HResult);
                    return false;
                }
            }
        }

        static bool RichClipboardTarget(IntPtr owner)
        {
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(owner, out pid);
                return new[]
                {
                    "chrome.exe",
                    "msedge.exe",
                    "firefox.exe",
                    "zen.exe",
                    "safari.exe",
                    "outlook.exe",
                    "thunderbird.exe"
                }.Any(name => Native.ProcessImageIs(pid, name));
            }
            catch (Exception)
            {
                return false;
            }
        }

        byte[] ConversionHtml(IntPtr owner, string text, string mode, string trigger = null)
        {
            if (!formattingSettings.Italicize || mode == null || !RichClipboardTarget(owner))
                return null;
            ItalicSpan[] spans = trigger == null ? core.ItalicWords(text, mode) : core.TriggerItalics(trigger, text, mode);
            return spans.Length == 0 ? null : RichClipboard.Html(text, spans);
        }

        void ConvertSelected(SafeEdit edit, string mode, bool fromItrans, bool fromBalaram, bool cycleWord = false, string forcedOutput = null, bool restoring = false)
        {
            IntPtr owner = edit.Window;
            int[] field = FocusId();
            long generation;
            string input = edit.Expected, repeatOutput = null;
            lock (sync)
            {
                if (state.Paused || busy)
                    return;
                if (cycleWord && selectedCtrlMode == mode && selectedCycleEdit != null && edit.Expected == selectedCycleEdit.Expected && edit.SameStart(selectedCycleEdit) && System.Text.RegularExpressions.Regex.Matches(input, @"[\p{L}\p{M}]+(?:[-\u2011\u2013\x27\u2019][\p{L}\p{M}]+)*").Count > 1)
                    return;
                if (!cycleWord && !restoring && selectionClipboardCycle && selectedCycleEdit != null && edit.SameStart(selectedCycleEdit))
                    clipboardCycle.NextSelected(mode, fromItrans ? "itrans" : fromBalaram ? "balaram" : "auto", input, Native.GetClipboardSequenceNumber(), owner, field, DateTime.UtcNow, out repeatOutput);
                state.Clear();
                letters.Clear();
                controlTap.Cancel();
                letterUndo = wordUndo = false;
                clipboardCycle.Clear();
                selectionClipboardCycle = false;
                selectionUndoEdit = null;
                selectionUndoOriginal = null;
                generation = state.Generation;
                busy = true;
            }

            ManualTarget(edit);
            Func<bool> context = () =>
            {
                lock (sync)
                    return state.Generation == generation && !state.Paused && owner == Native.GetForegroundWindow() && EditGuard.SameFocus(field, FocusId());
            };
            Task.Run(() =>
            {
                try
                {
                    core.FinishOtherOccurrence(edit.OccurrenceKey());
                    string semanticInput = edit.IsMarkdownSource ? SafeEdit.MarkdownWord(input) : input;
                    string source = fromItrans ? "itrans" : fromBalaram ? "balaram" : "auto";
                    ItalicSpan[] fontItalic = null;
                    string output = mode == "font-auto" ? edit.FontAutoOutput(core, fontEncoding, out fontItalic, cycleWord) : edit.MarkdownConvert(input, v => core.ConvertSource(v, mode, source));
                    string[] choices = mode == "font-auto" ? new string[0] : cycleWord ? core.PrepareWordConversion(semanticInput, mode).Choices : core.ClipboardChoices(input, mode, source);
                    if (cycleWord && mode != "font-auto")
                    {
                        output = semanticInput != input ? SafeEdit.CycleMarkdownWord(core, input, mode) : edit.MarkdownConvert(semanticInput, v => core.ConvertHighlighted(v, mode, Numbers.Single(semanticInput) != null));
                        if (core.CtrlExpansion(semanticInput, mode) != null)
                            choices = new string[0];
                        else if (edit.MixedWordUnicode != null)
                        {
                            output = core.Word(edit.MixedWordUnicode, mode);
                            choices = new string[0];
                        }
                    }

                    if (repeatOutput != null)
                        output = repeatOutput;
                    if (forcedOutput != null)
                    {
                        output = forcedOutput;
                        choices = new string[0];
                    }
                    else if (edit.IsMarkdownSource && formattingSettings.Italicize && mode != "font-auto")
                        output = edit.MarkdownOutput(output, core, mode);
                    if (!WaitModifiers(generation))
                    {
                        Diagnostic("selection modifiers/context cancelled");
                        Complete();
                        return;
                    }

                    Diagnostic("selection output prepared inputCharacters=" + input.Length + " outputCharacters=" + output.Length + " markdown=" + edit.IsMarkdownSource);
                    Action applySelection = () =>
                    {
                        bool pending = false;
                        try
                        {
                            if (!edit.OwnExistingSelection(context) || !SelectionConversion.ValidText(output))
                            {
                                Diagnostic("selection ownership rejected context=" + context());
                                return;
                            }

                            Diagnostic("selection ownership verified");
                            ManualSelectionVerified(edit);
                            ItalicSpan[] italicSpans = !restoring && formattingSettings.Italicize && (WordPasteOptions.IsWord(owner) || edit.UsesNativeReplacement) ? core.CtrlItalics(cycleWord ? semanticInput : null, output, mode) : null;
                            if (mode == "font-auto" && !restoring && formattingSettings.Italicize)
                                italicSpans = fontItalic;
                            byte[] html = restoring ? null : ConversionHtml(owner, output, mode, cycleWord ? Core.CtrlScriptureTrigger(semanticInput, mode) : null);
                            if (output == input && (italicSpans == null || italicSpans.Length == 0) && html == null)
                                return;
                            string baseline = WordPasteOptions.IsWord(owner) || edit.IsExcelCell ? "" : Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : "";
                            bool wordCommitted = false;
                            Action committed = () =>
                            {
                                if (!wordCommitted)
                                    ManualCommitted(edit, output, false);
                                SafeEdit retained;
                                if (wordCommitted)
                                {
                                    bool present;
                                    retained = SafeEdit.ReadSelected(owner, out present);
                                    if (!present || retained == null || retained.Expected != output || !context())
                                        retained = null;
                                }
                                else
                                    retained = edit.RetainReplacement(output, context);
                                if (retained == null)
                                {
                                    Diagnostic("converted selection could not be retained");
                                    return;
                                }

                                ManualCommitted(retained, output, true);
                                if (cycleWord && choices.Length > 0)
                                    core.RememberCaretSpelling(input, output, mode, false, edit.OccurrenceKey());
                                else if (!restoring && repeatOutput != null)
                                    core.RememberSpelling(input, output, mode, source, edit.OccurrenceKey());
                                lock (sync)
                                {
                                    if (state.Generation != generation || state.Paused)
                                        return;
                                    selectedCycleEdit = retained;
                                    selectedCtrlMode = cycleWord ? mode : null;
                                    if (!restoring && !WordPasteOptions.IsWord(owner) && !edit.UsesNativeReplacement)
                                    {
                                        selectionUndoEdit = retained;
                                        selectionUndoOriginal = input;
                                    }

                                    state.RecordReplacement(input, output, owner, true);
                                    RecordCaretCharacter(output, owner);
                                    expiry = DateTime.UtcNow.AddSeconds(10);
                                    clipboardCycle.Start(output, choices, mode, source, Native.GetClipboardSequenceNumber(), owner, field, DateTime.UtcNow);
                                    clipboardCycle.Preserved(baseline, Native.GetClipboardSequenceNumber());
                                    selectionClipboardCycle = true;
                                }
                            };
                            if (edit.UsesNativeReplacement)
                            {
                                if (edit.ReplaceInDesign(output, true, italicSpans))
                                    committed();
                                return;
                            }

                            bool wordHandled;
                            bool scriptureExpansion = cycleWord && Core.IsScriptureReference(semanticInput) && output != input;
                            bool wordReplaced = scriptureExpansion ? WordSelection.ReplaceExpansion(owner, input, output, context, out wordHandled, italicSpans, true, manualInputActive ? manualWordAnchor : null) : WordSelection.Replace(owner, input, output, context, out wordHandled, italicSpans, true, mode == "balaram" ? edit.BalaramWordFont : null, null, null, core.CompoundFontSpans(input, output, mode), manualInputActive ? manualWordAnchor : null);
                            if (wordReplaced)
                            {
                                Diagnostic("Word selection committed");
                                ManualCommitted(edit, output, true);
                                wordCommitted = true;
                                committed();
                                return;
                            }

                            if (wordHandled)
                            {
                                Diagnostic("Word conversion safely skipped");
                                return;
                            }

                            edit.CaptureStableStart();
                            if (Native.UnicodeText(output) != null && html == null)
                            {
                                if (SelectionConversion.CanCommit(context, edit.ExactSelection) && Native.TypeText(output))
                                    committed();
                                return;
                            }

                            ClipboardBackup saved = ClipboardBackup.Capture();
                            if (saved == null)
                            {
                                Diagnostic("clipboard snapshot unavailable; selection unchanged");
                                return;
                            }

                            baseline = saved.Text;
                            uint temporary;
                            if (!SelectionConversion.CanCommit(context, edit.ExactSelection) || saved.Sequence != Native.GetClipboardSequenceNumber())
                                return;
                            if (!Native.TemporaryClipboard(Handle, output, out temporary, html))
                            {
                                saved.Restore(Handle, Native.GetClipboardSequenceNumber());
                                return;
                            }

                            if (!SelectionConversion.CanCommit(context, edit.ExactSelection) || !Native.Paste())
                            {
                                saved.Restore(Handle, temporary);
                                return;
                            }

                            pending = true;
                            Task.Run(() =>
                            {
                                bool verified = false;
                                for (int i = 0; i < 50; i++)
                                {
                                    if (!context())
                                        break;
                                    bool present;
                                    SafeEdit selected = SafeEdit.ReadSelected(owner, out present);
                                    if (present && edit.RepresentsOutput(selected, output) || !present && (SafeEdit.BehindCaret(owner, output, false, false, true) != null || SafeEdit.BehindCaret(owner, edit.DisplayedOutput(output), false, false, true) != null))
                                    {
                                        verified = true;
                                        break;
                                    }

                                    Thread.Sleep(20);
                                }

                                BeginInvoke((Action)(() =>
                                {
                                    try
                                    {
                                        if (verified)
                                        {
                                            bool restored = saved.Restore(Handle, temporary);
                                            if (context())
                                            {
                                                committed();
                                                if (!restored)
                                                    clipboardCycle.Clear();
                                            }
                                        }
                                        else
                                        {
                                            saved.Restore(Handle, temporary);
                                            clipboardCycle.Clear();
                                            Diagnostic("selected passage could not be verified");
                                        }
                                    }
                                    finally
                                    {
                                        Complete();
                                    }
                                }));
                            });
                        }
                        catch (Exception ex)
                        {
                            clipboardCycle.Clear();
                            Log(ex.GetType().Name);
                        }
                        finally
                        {
                            if (!pending)
                                Complete();
                        }
                    };
                    if (edit.IsExcelCell)
                        applySelection();
                    else
                        BeginInvoke(applySelection);
                }
                catch (Exception ex)
                {
                    clipboardCycle.Clear();
                    Log(ex.GetType().Name);
                    Complete();
                }
            });
        }

        void PrepareClipboardShortcut(string mode, bool caret = false)
        {
            mode = core.ScriptOptions.ShortcutMode(mode);
            IntPtr owner = WordPasteOptions.Owner(Native.GetForegroundWindow());
            if (!caret && ExcelCellField.Focused(owner))
            {
                PrepareExcelClipboardShortcut(owner, mode);
                return;
            }

            if (!WordPasteOptions.IsWord(owner))
            {
                if (caret)
                    DiscoverWord(owner);
                else
                    ConvertClipboard(mode);
                return;
            }

            bool selectionSeen = false;
            SafeEdit previousSelection = null;
            long stamp;
            lock (sync)
            {
                if (state.Paused || busy)
                    return;
                stamp = state.Generation;
                busy = true;
                pendingWordTapOwner = owner;
                pendingWordTapUntil = DateTime.UtcNow.AddSeconds(3);
            }

            Func<bool> current = () =>
            {
                lock (sync)
                    return !state.Paused && state.Generation == stamp && WordPasteOptions.Owner(Native.GetForegroundWindow()) == owner;
            };
            Task.Run(() =>
            {
                bool delegated = false;
                try
                {
                    if (!current())
                        return;
                    previousSelection = SafeEdit.ReadSelected(owner, out selectionSeen);
                    if (!current())
                        return;
                    if (!WaitModifiers(stamp) || !current())
                        return;
                    Thread.Sleep(40);
                    if (!current())
                        return;
                    bool pasted;
                    lock (sync)
                        pasted = wordPasteContext.Verified(owner, Native.GetClipboardSequenceNumber());
                    bool menu = WordPasteOptions.FocusedMenu(owner);
                    if (menu || pasted)
                    {
                        lock (sync)
                            dismissingWordPaste = owner;
                        try
                        {
                            if (!WordPasteRecovery.Dismiss(current, () => WordPasteOptions.FocusedMenu(owner), () => WordPasteOptions.DocumentFocused(owner), () => Native.Escape(), ms => Thread.Sleep(ms), pasted))
                            {
                                Diagnostic("shortcut paste recovery failed");
                                return;
                            }
                        }
                        finally
                        {
                            lock (sync)
                                dismissingWordPaste = IntPtr.Zero;
                        }
                    }

                    if (!current() || !WordPasteGuard.Settled(WordPasteOptions.DocumentFocused(owner), WordPasteOptions.FocusedMenu(owner)))
                        return;
                    Diagnostic("shortcut document settled");
                    BeginInvoke((Action)(() =>
                    {
                        if (!current() || selectionSeen && (previousSelection == null || !previousSelection.ExactSelection()))
                        {
                            Diagnostic("shortcut recovered selection changed");
                            Complete();
                            return;
                        }

                        lock (sync)
                        {
                            busy = false;
                            wordPasteContext.Clear();
                            pendingWordTapOwner = IntPtr.Zero;
                        }

                        if (caret)
                            DiscoverWord(owner);
                        else
                            ConvertClipboard(mode);
                        if (!busy)
                            FinishManualInput();
                    }));
                    delegated = true;
                }
                catch (Exception ex)
                {
                    Log("shortcut preparation " + ex.GetType().Name);
                }
                finally
                {
                    if (!delegated)
                        Complete();
                }
            });
        }

        // Excel providers may stall. Discover the cell off the resident/hook thread;
        // only clipboard access and dispatch return to the UI STA.
        void PrepareExcelClipboardShortcut(IntPtr owner, string mode)
        {
            long stamp;
            int[] focus = FocusId();
            lock (sync)
            {
                if (state.Paused || busy)
                    return;
                stamp = state.Generation;
                busy = true;
            }

            Task.Run(() =>
            {
                SafeEdit target = null;
                try
                {
                    bool present;
                    target = SafeEdit.ReadSelected(owner, out present);
                    if (!present)
                        target = SafeEdit.InsertionAnchor(owner);
                }
                catch (Exception ex)
                {
                    Diagnostic("Excel shortcut discovery " + ex.GetType().Name);
                }

                BeginInvoke((Action)(() =>
                {
                    lock (sync)
                    {
                        busy = false;
                        if (state.Paused || state.Generation != stamp || owner != Native.GetForegroundWindow() || !EditGuard.SameFocus(focus, FocusId()))
                        {
                            FinishManualInput();
                            return;
                        }
                    }

                    if (target != null)
                        ConvertClipboard(mode, false, false, target, true);
                    if (!busy)
                        FinishManualInput();
                }));
            });
        }

        void ConvertClipboard(string mode, bool fromItrans = false, bool fromBalaram = false, SafeEdit prepared = null, bool supplied = false)
        {
            Diagnostic("convert entry mode=" + mode);
            if (probe)
                return;
            lock (sync)
            {
                if (state.Paused || busy)
                {
                    Diagnostic("convert blocked paused=" + state.Paused + " busy=" + busy);
                    return;
                }
            }

            IntPtr owner = Native.GetForegroundWindow();
            bool selectionPresent;
            SafeEdit selected;
            if (supplied)
            {
                selectionPresent = prepared != null && prepared.UserSelection;
                selected = selectionPresent ? prepared : null;
            }
            else
                selected = SafeEdit.ReadSelected(owner, out selectionPresent);
            Diagnostic("selection present=" + selectionPresent + " readable=" + (selected != null));
            if (selectionPresent)
            {
                if (selected != null)
                    ConvertSelected(selected, mode, fromItrans, fromBalaram);
                return;
            }

            SafeEdit insertion = supplied ? prepared : SafeEdit.InsertionAnchor(owner);
            ManualTarget(insertion);
            int[] field = FocusId();
            string input, next = null;
            long generation;
            uint clipboardSequence;
            lock (sync)
            {
                if (state.Paused || busy)
                    return;
            }

            try
            {
                clipboardSequence = Native.GetClipboardSequenceNumber();
                input = Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : "";
                if (clipboardSequence != Native.GetClipboardSequenceNumber())
                    return;
            }
            catch (ExternalException)
            {
                return;
            }

            string source = fromItrans ? "itrans" : fromBalaram ? "balaram" : "auto";
            bool repeat;
            lock (sync)
            {
                string previous = clipboardCycle.Current;
                repeat = clipboardCycle.Next(mode, source, input, clipboardSequence, owner, field, DateTime.UtcNow, out next);
                if (!repeat)
                    selectionClipboardCycle = false;
                if (!repeat && input.Length == 0)
                    return;
                if (repeat)
                    input = previous;
                state.Clear();
                letters.Clear();
                controlTap.Cancel();
                letterUndo = false;
                wordUndo = false;
                generation = state.Generation;
                busy = true;
            }

            Task.Run(() =>
            {
                try
                {
                    if (!repeat)
                        core.FinishSpellingChoice();
                    string output = repeat ? next : core.ConvertSource(input, mode, source);
                    string[] choices = repeat ? null : core.ClipboardChoices(input, mode, source);
                    if (!WaitModifiers(generation))
                    {
                        Complete();
                        return;
                    }

                    SafeEdit edit = repeat ? PrepareOwnedEdit(owner, input, generation) : null;
                    BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            lock (sync)
                            {
                                if (state.Generation != generation || state.Paused)
                                {
                                    clipboardCycle.Clear();
                                    return;
                                }
                            }

                            if (Native.GetForegroundWindow() != owner || (field != null && !EditGuard.SameFocus(field, FocusId())) || Native.GetClipboardSequenceNumber() != clipboardSequence)
                            {
                                clipboardCycle.Clear();
                                return;
                            }

                            if (repeat && edit == null)
                            {
                                clipboardCycle.Clear();
                                return;
                            }

                            if (!(selectionClipboardCycle && repeat ? edit != null && edit.ReadyForPaste() && (edit.UsesNativeReplacement ? edit.ReplaceInDesign(output, false, formattingSettings.Italicize ? core.ItalicWords(output, mode) : null) : Native.TypeText(output)) : InsertCopied(output, edit, mode)))
                            {
                                clipboardCycle.Clear();
                                return;
                            }

                            ManualCommitted(edit ?? insertion, output);
                            lock (sync)
                            {
                                state.RecordReplacement(input, output, owner);
                                RecordCaretCharacter(output, owner);
                                expiry = DateTime.UtcNow.AddSeconds(10);
                                if (repeat)
                                {
                                    core.RememberSpelling(input, output, mode, source, edit == null ? null : edit.OccurrenceKey());
                                    clipboardCycle.Commit(output, Native.GetClipboardSequenceNumber(), DateTime.UtcNow);
                                    clipboardCycle.Preserved(Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : "", Native.GetClipboardSequenceNumber());
                                }
                                else
                                {
                                    clipboardCycle.Start(output, choices, mode, source, Native.GetClipboardSequenceNumber(), owner, field, DateTime.UtcNow);
                                    clipboardCycle.Preserved(Clipboard.ContainsText() ? Clipboard.GetText(TextDataFormat.UnicodeText) : "", Native.GetClipboardSequenceNumber());
                                    if (mode != "plain" && mode != "itrans" && choices != null && choices.Length > 1 && Array.IndexOf(choices, output) >= 0)
                                    {
                                        state.Candidates = choices;
                                        state.CycleMode = mode;
                                        state.CycleIndex = Array.IndexOf(choices, output);
                                        state.CycleDeadline = expiry;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            clipboardCycle.Clear();
                            Log(ex.GetType().Name);
                        }
                        finally
                        {
                            Complete();
                        }
                    }));
                }
                catch (Exception ex)
                {
                    clipboardCycle.Clear();
                    Log(ex.GetType().Name);
                    Complete();
                }
            });
        }

        bool InsertCopied(string value, SafeEdit edit, string mode)
        {
            if (edit == null && InDesignSelection.UseDocumentBridge(Native.GetForegroundWindow()))
            {
                edit = SafeEdit.InDesignInsertion(Native.GetForegroundWindow());
                if (edit == null)
                    return false;
                long stamp;
                lock (sync)
                    stamp = state.Generation;
                if (!edit.Select(() =>
                {
                    lock (sync)
                        return !state.Paused && state.Generation == stamp;
                }))
                    return false;
            }

            if (edit != null && (WordPasteOptions.IsWord(edit.Window) || edit.UsesNativeReplacement))
                return Insert(value, edit, mode);
            if (edit != null && !edit.ReadyForPaste())
                return false;
            IntPtr owner = Native.GetForegroundWindow();
            byte[] html = ConversionHtml(owner, value, mode);
            if (html == null && Native.UnicodeText(value) != null)
                return Native.TypeText(value);
            ClipboardBackup saved = ClipboardBackup.Capture();
            if (saved == null || saved.Sequence != Native.GetClipboardSequenceNumber())
                return false;
            uint temporary;
            if (!Native.TemporaryClipboard(Handle, value, out temporary, html))
            {
                saved.Restore(Handle, Native.GetClipboardSequenceNumber());
                return false;
            }

            if (edit != null && !edit.ReadyForPaste() || Native.GetForegroundWindow() != owner || !Native.Paste())
            {
                saved.Restore(Handle, temporary);
                return false;
            }

            Task.Run(() =>
            {
                for (int n = 0; n < 50; n++)
                {
                    if (Native.GetForegroundWindow() != owner || SafeEdit.BehindCaret(owner, value, false, false, true) != null)
                        break;
                    Thread.Sleep(20);
                }

                BeginInvoke((Action)(() =>
                {
                    if (saved.Restore(Handle, temporary))
                        lock (sync)
                            clipboardCycle.Preserved(saved.Text, Native.GetClipboardSequenceNumber());
                    else
                        lock (sync)
                            clipboardCycle.Clear();
                }));
            });
            return true;
        }

        public static DataObject PastePayload(string value)
        {
            DataObject data = new DataObject();
            data.SetData(DataFormats.UnicodeText, false, value);
            foreach (string format in new[]
            {
                "ExcludeClipboardContentFromMonitorProcessing",
                "CanIncludeInClipboardHistory",
                "CanUploadToCloudClipboard"
            }

            )
                data.SetData(format, false, new MemoryStream(new byte[4]));
            return data;
        }

        bool ReplaceWord(bool expansion, IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics, bool retain)
        {
            return expansion ? WordSelection.ReplaceExpansion(owner, input, output, context, out handled, italics, retain, manualInputActive ? manualWordAnchor : null) : WordSelection.Replace(owner, input, output, context, out handled, italics, retain, null, null, null, null, manualInputActive ? manualWordAnchor : null);
        }

        bool Insert(string value, SafeEdit edit = null, string conversionMode = null, string trigger = null)
        {
            bool singleLetter = edit != null && edit.SingleLetter;
            bool committed = false;
            if (edit != null && !edit.MarkdownAllowed)
                return false;
            if (edit != null && edit.IsMarkdownSource && conversionMode != null && formattingSettings.Italicize)
                value = edit.MarkdownOutput(value, core, conversionMode, trigger);
            try
            {
                if (edit != null && !edit.ReadyForPaste())
                    return false;
                if (edit != null && edit.UsesNativeReplacement)
                {
                    committed = edit.ReplaceInDesign(value, false, conversionMode != null && formattingSettings.Italicize ? (trigger == null ? core.ItalicWords(value, conversionMode) : core.TriggerItalics(trigger, value, conversionMode)) : null);
                    return committed;
                }

                if (singleLetter)
                {
                    committed = Native.TypeCharacter(value);
                    return committed;
                }

                if (edit != null && conversionMode != null && WordPasteOptions.IsWord(edit.Window))
                {
                    long stamp;
                    lock (sync)
                        stamp = state.Generation;
                    int[] field = FocusId();
                    bool handled;
                    committed = ReplaceWord(trigger != null, edit.Window, edit.Expected, value, () =>
                    {
                        lock (sync)
                            return state.Generation == stamp && !state.Paused && edit.Window == Native.GetForegroundWindow() && EditGuard.SameFocus(field, FocusId());
                    }, out handled, formattingSettings.Italicize ? (trigger == null ? core.ItalicWords(value, conversionMode) : core.TriggerItalics(trigger, value, conversionMode)) : null, false);
                    return committed;
                }

                uint sequence;
                if (!Native.TemporaryClipboard(Handle, value, out sequence, ConversionHtml(Native.GetForegroundWindow(), value, conversionMode, trigger)))
                    return false;
                if (edit != null && !edit.ReadyForPaste())
                    return false;
                bool pasted = Native.Paste();
                committed = pasted;
                if (pasted)
                {
                    IntPtr owner = Native.GetForegroundWindow();
                    long generation;
                    lock (sync)
                        generation = state.Generation;
                    Task.Run(() =>
                    {
                        SafeEdit verified = FindOwnedEdit(owner, value, generation, singleLetter);
                        BeginInvoke((Action)(() =>
                        {
                            try
                            {
                                lock (sync)
                                {
                                    if (!(verified != null && !probe && state.Generation == generation && !state.Paused && Native.TemporaryClipboardOwned(Handle, sequence)) || Native.GetForegroundWindow() != owner)
                                        return;
                                }

                                if (SafeEdit.BehindCaret(owner, value, singleLetter) == null)
                                    return;
                                uint clearedSequence;
                                if (Native.ClearTemporaryClipboard(Handle, sequence, out clearedSequence))
                                    lock (sync)
                                        clipboardCycle.Released(clearedSequence);
                            }
                            catch (ExternalException)
                            {
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }));
                    });
                }

                return pasted;
            }
            catch (ExternalException)
            {
                return false;
            }
            finally
            {
                if (committed)
                    ManualCommitted(edit, value);
                else if (edit != null)
                    edit.Cancel();
            }
        }

        // Central completion boundary for success, safe skips, and exceptions.
        // Pending work is dispatched in order; otherwise release/recover the input hold.
        void Complete()
        {
            lock (sync)
            {
                busy = false;
                if (!state.Paused && letters.HasPending)
                {
                    BeginLetter();
                    return;
                }

                string latest;
                IntPtr owner = Native.GetForegroundWindow();
                if (pendingPunctuation.Take(state.Generation, owner, state.Paused, out latest))
                {
                    busy = true;
                    ExpandPunctuation(owner, latest, state.Generation);
                    return;
                }

                if (pendingCorrections.Take(state.Generation, owner, state.Paused, out latest) && BeginCorrectionTyping(state.Generation, owner, latest))
                {
                    busy = true;
                    ExpandEnglish(owner, latest, state.Generation);
                    return;
                }

                bool word;
                if (caretTaps.Take(owner, state.Generation, state.Paused, out word))
                {
                    if (word)
                        PrepareClipboardShortcut(null, true);
                    else
                        DiscoverLetter(owner, "auto", shortcutSettings["letter"].Text == "`");
                    return;
                }

                if (!state.Paused && state.PendingCycles > 0 && state.Candidates != null)
                {
                    BeginCycle();
                    return;
                }

                FinishManualInput();
            }
        }

        void UpdateIcon()
        {
            string path = Path.Combine(root, "assets", "keyjaya.ico");
            IntPtr image = Native.LoadImage(IntPtr.Zero, path, 1, InputState.IconPixels(Native.CursorDpi()), InputState.IconPixels(Native.CursorDpi()), 0x10);
            if (image == IntPtr.Zero)
            {
                tray.Icon = SystemIcons.Application;
                return;
            }

            using (Icon icon = Icon.FromHandle(image))
            {
                Icon old = tray.Icon;
                tray.Icon = (Icon)icon.Clone();
                if (old != null && old != SystemIcons.Application)
                    old.Dispose();
            }

            Native.DestroyIcon(image);
        }

        void TrayClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ShowMenu();
        }

        public void ShowMenu()
        {
            trayMenu.ShowAt(Cursor.Position);
        }

        void BuildMenu()
        {
            foreach (TrayItem item in TrayMenu.Items(false, true, true))
            {
                if (item.Id == 0)
                {
                    trayMenu.Items.Add(new ToolStripSeparator());
                    continue;
                }

                uint id = item.Id;
                var action = new ToolStripMenuItem(item.Label)
                {
                    Tag = id
                };
                action.Click += (sender, args) =>
                {
                    DispatchMenu(id);
                };
                trayMenu.Items.Add(action);
            }

            RefreshMenu();
        }

        void RefreshMenu()
        {
            bool paused;
            lock (sync)
                paused = state.Paused;
            var rows = TrayMenu.Items(paused, core.AutomaticCorrections.Spelling, core.AutomaticCorrections.Contractions, encodingSettings.Mode, core.ScriptOptions.Precise, core.ScriptOptions.PreserveVerseMarkers, formattingSettings.Italicize, core.ScriptOptions.Language, StartupEnabled(), CanManageStartup());
            for (int i = 0; i < rows.Length; i++)
            {
                var action = trayMenu.Items[i] as ToolStripMenuItem;
                if (action == null)
                    continue;
                action.Text = rows[i].Id == 31 ? shortcutSettings["itrans"].Text + ": ITRANS" : rows[i].Id == 32 ? shortcutSettings["itrans"].Text + ": Hindi" : rows[i].Label;
                action.Checked = (rows[i].Flags & 8) != 0;
                action.Enabled = (rows[i].Flags & 2) == 0;
            }
        }

        const string StartupRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        bool CanManageStartup()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\KeyJaya"))
                {
                    string location = key == null ? null : key.GetValue("InstallLocation") as string;
                    return location != null && Path.GetFullPath(location).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        string StartupCommand()
        {
            return "\"" + Application.ExecutablePath + "\"";
        }

        bool StartupEnabled()
        {
            if (!CanManageStartup())
                return new StartupSettings(Path.Combine(Configuration.Folder(root), "settings.ini")).Enabled;
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRunKey))
                    return key != null && string.Equals(key.GetValue("KeyJaya") as string, StartupCommand(), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        void SetStartup(bool enabled)
        {
            if (!CanManageStartup())
                throw new InvalidOperationException("Startup can be changed only from the installed KeyJaya app.");
            var preference = new StartupSettings(Path.Combine(Configuration.Folder(root), "settings.ini"));
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(StartupRunKey))
            {
                object old = key.GetValue("KeyJaya");
                if (old != null && !string.Equals(old as string, StartupCommand(), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The startup entry belongs to another KeyJaya location.");
                try
                {
                    if (enabled)
                        key.SetValue("KeyJaya", StartupCommand());
                    else
                        key.DeleteValue("KeyJaya", false);
                    preference.Set(enabled);
                }
                catch
                {
                    if (old == null)
                        key.DeleteValue("KeyJaya", false);
                    else
                        key.SetValue("KeyJaya", old);
                    throw;
                }
            }
        }

        void DispatchMenu(uint choice)
        {
            if (choice == 35)
            {
                QueueTriggerEditor();
                return;
            }

            if (choice == 34)
            {
                CustomizeShortcuts();
                return;
            }

            if (choice == 33)
            {
                try
                {
                    SetStartup(!StartupEnabled());
                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Startup setting could not be saved.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            if (choice == 31 || choice == 32)
            {
                try
                {
                    lock (sync)
                    {
                        if (busy)
                            return;
                        core.ScriptOptions.SetLanguage(choice == 31 ? "sanskrit" : "hindi");
                        ResetInput();
                    }

                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Language setting could not be saved.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            if (choice == 30)
            {
                try
                {
                    lock (sync)
                    {
                        if (busy)
                            return;
                        formattingSettings.Set(!formattingSettings.Italicize);
                        ResetInput();
                    }

                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Italic setting could not be saved.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            if (choice == 25 || choice == 26)
            {
                try
                {
                    lock (sync)
                    {
                        if (busy)
                        {
                            tray.ShowBalloonTip(3000, "KeyJaya", "Finish the current conversion before changing script options.", ToolTipIcon.Info);
                            return;
                        }

                        core.ScriptOptions.Set(choice == 25 ? !core.ScriptOptions.Precise : core.ScriptOptions.Precise, choice == 26 ? !core.ScriptOptions.PreserveVerseMarkers : core.ScriptOptions.PreserveVerseMarkers);
                        ResetInput();
                    }

                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Script settings could not be saved.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            if (choice >= 22 && choice <= 24)
            {
                try
                {
                    lock (sync)
                    {
                        if (busy)
                        {
                            tray.ShowBalloonTip(3000, "KeyJaya", "Finish the current conversion before changing encoding.", ToolTipIcon.Info);
                            return;
                        }

                        encodingSettings.Set(choice == 22 ? "auto" : choice == 23 ? "unicode" : "balaram");
                        core.CancelSpellingChoice();
                        ResetInput();
                        encodingChanged = true;
                    }

                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The encoding setting could not be saved. Your previous choice remains active.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return;
            }

            if (choice == 1)
            {
                lock (sync)
                {
                    state.Paused = !state.Paused;
                    ResetInput();
                    tray.Text = state.Paused ? "KeyJaya " + Version.Current + " — Paused" : "KeyJaya " + Version.Current;
                }

                if (trayPreview)
                    return;
                if (state.Paused)
                {
                    conversionShortcuts.Clear();
                    UnregisterShortcuts();
                    Native.UnhookWindowsHookEx(keyHook);
                    Native.UnhookWindowsHookEx(mouseHook);
                    keyHook = mouseHook = IntPtr.Zero;
                }
                else
                {
                    Register();
                    keyHook = Native.SetWindowsHookEx(13, keyboard, Native.GetModuleHandle(null), 0);
                    mouseHook = Native.SetWindowsHookEx(14, mouse, Native.GetModuleHandle(null), 0);
                }
            }
            else if (choice == 19 || choice == 20)
            {
                try
                {
                    core.AutomaticCorrections.Set(choice == 19 ? !core.AutomaticCorrections.Spelling : core.AutomaticCorrections.Spelling, choice == 20 ? !core.AutomaticCorrections.Contractions : core.AutomaticCorrections.Contractions);
                    lock (sync)
                        ResetInput();
                    RefreshMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The setting could not be saved. Your previous options remain active.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (choice == 21)
                OpenCheatSheet();
            else if (choice == 5)
                Process.Start(Configuration.Folder(root));
            else if (choice == 17)
                ReloadConfiguration();
            else if (choice == 7)
            {
                string exe = Application.ExecutablePath;
                Thread restart = new Thread(() =>
                {
                    Thread.Sleep(300);
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                });
                Close();
                restart.Start();
            }
            else if (choice == 8)
                Close();
        }

        bool IsReferenceWindow(IntPtr owner)
        {
            if (owner == referenceOwner)
                return referenceWindow;
            referenceOwner = owner;
            var caption = new StringBuilder(80);
            var kind = new StringBuilder(100);
            Native.GetWindowText(owner, caption, caption.Capacity);
            Native.GetClassName(owner, kind, kind.Capacity);
            referenceWindow = (caption.ToString() == "KeyJaya cheat sheet" || caption.ToString() == "KeyJaya shortcuts" || caption.ToString() == "KeyJaya triggers" || caption.ToString() == "KeyJaya trigger") && kind.ToString().StartsWith("WindowsForms", StringComparison.Ordinal);
            return referenceWindow;
        }

        void CloseCheatSheet()
        {
            if (cheatProcess == null)
                return;
            try
            {
                if (!cheatProcess.HasExited)
                    cheatProcess.CloseMainWindow();
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                cheatProcess.Dispose();
                cheatProcess = null;
            }
        }

        internal void OpenCheatSheet()
        {
            try
            {
                if (cheatProcess != null && !cheatProcess.HasExited)
                {
                    cheatProcess.Refresh();
                    IntPtr existing = cheatProcess.MainWindowHandle;
                    if (existing != IntPtr.Zero)
                    {
                        Native.ShowWindow(existing, 9);
                        Native.SetForegroundWindow(existing);
                    }

                    return;
                }

                if (cheatProcess != null)
                    cheatProcess.Dispose();
                cheatProcess = Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--cheat-sheet " + Handle.ToInt64().ToString(CultureInfo.InvariantCulture)) { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Normal });
                Log("cheat sheet launched");
            }
            catch (Exception ex)
            {
                Log("cheat sheet launch: " + ex.GetType().Name);
                MessageBox.Show("The cheat sheet could not open.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void QueueTriggerEditor()
        {
            trayMenu.Close();
            if (triggerEditorQueued || IsDisposed)
                return;
            triggerEditorQueued = true;
            BeginInvoke((Action)(() =>
            {
                triggerEditorQueued = false;
                if (!IsDisposed)
                    EditTriggers();
            }));
        }

        static void ActivateEditor(Form window)
        {
            if (window.IsDisposed)
                return;
            if (!window.Visible)
                window.Show();
            if (window.WindowState == FormWindowState.Minimized)
                window.WindowState = FormWindowState.Normal;
            Native.ShowWindow(window.Handle, 5);
            window.BringToFront();
            window.Activate();
            Native.SetForegroundWindow(window.Handle);
        }

        void EditTriggers()
        {
            trayMenu.Close();
            if (triggerEditor != null && !triggerEditor.IsDisposed)
            {
                ActivateEditor(triggerEditor);
                return;
            }

            TriggerEditorWindow window = null;
            try
            {
                var store = new TriggerEditorStore(Configuration.Short(root), Configuration.Factory(root));
                window = new TriggerEditorWindow(store, () =>
                {
                    lock (sync)
                    {
                        if (busy)
                            throw new InvalidOperationException("Finish the current conversion before saving triggers.");
                        core = Core.Reload(core, root);
                        ResetInput();
                    }

                    CloseCheatSheet();
                    RefreshMenu();
                });
                triggerEditor = window;
                window.FormClosed += (sender, args) =>
                {
                    if (triggerEditor == window)
                        triggerEditor = null;
                };
                // Keep the resident's hidden form out of the ownership/modal chain.
                // Read-only editing may open during a conversion; Save still verifies idle.
                window.Show();
                ActivateEditor(window);
                window.BeginInvoke((Action)(() => ActivateEditor(window)));
                Log("trigger editor opened");
            }
            catch (Exception ex)
            {
                if (window != null)
                    window.Dispose();
                if (triggerEditor == window)
                    triggerEditor = null;
                Log("trigger editor open: " + ex.GetType().Name);
                MessageBox.Show("The trigger editor could not open.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ReloadConfiguration()
        {
            lock (sync)
            {
                if (busy)
                {
                    tray.ShowBalloonTip(3000, "KeyJaya", "Finish the current conversion, then reload again.", ToolTipIcon.Info);
                    return;
                }
            }

            try
            {
                var fonts = new FontEncoding(root);
                var encoding = new EncodingSettings(Path.Combine(Configuration.Folder(root), "settings.ini"));
                core = Core.Reload(core, root);
                fontEncoding = fonts;
                encodingSettings = encoding;
                lock (sync)
                    ResetInput();
                CloseCheatSheet();
                tray.ShowBalloonTip(3000, "KeyJaya", "Configuration reloaded.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Configuration was not reloaded. The previous configuration remains active.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                if (File.Exists(logPath) && new FileInfo(logPath).Length > 262144)
                {
                    string old = logPath + ".1";
                    if (File.Exists(old))
                        File.Delete(old);
                    File.Move(logPath, old);
                }

                File.AppendAllText(logPath, DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine);
            }
            catch (IOException)
            {
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (manualWordAnchor != null)
                manualWordAnchor.FinishRecord();
            manualRetry.Dispose();
            fontExpiry.Dispose();
            lock (sync)
                ClearWordUndoFonts();
            if (triggerEditor != null)
            {
                var editor = triggerEditor;
                triggerEditor = null;
                editor.Close();
                editor.Dispose();
            }

            CloseCheatSheet();
            tray.Visible = false;
            tray.Dispose();
            trayMenu.Dispose();
            if (keyHook != IntPtr.Zero)
                Native.UnhookWindowsHookEx(keyHook);
            if (mouseHook != IntPtr.Zero)
                Native.UnhookWindowsHookEx(mouseHook);
            if (focusHook != IntPtr.Zero)
                Native.UnhookWinEvent(focusHook);
            if (controlFocusHook != IntPtr.Zero)
                Native.UnhookWinEvent(controlFocusHook);
            UnregisterShortcuts();
            core.Dispose();
            base.OnFormClosed(e);
        }
    }
}
