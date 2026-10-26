// KeyJaya: Resident input-hold lifecycle and exactly-once replay coordination.
// This partial shares the existing resident instance, synchronization gate and
// field initializers. It adds no worker, timer, queue, or input hook.
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyJaya
{
    public sealed partial class Resident
    {
        bool RecoverClosedInputTarget()
        {
            if (!correctionInputHold.Active || Native.IsWindow(correctionHoldOwner))
                return false;
            if (manualWordAnchor != null)
                manualWordAnchor.FinishRecord();
            correctionInputHold.Take(correctionHoldOwner);
            manualRetry.Stop();
            manualRetryAttempts = 0;
            manualInputActive = manualInputReady = manualInputSelected = false;
            manualInputEdit = null;
            manualWordAnchor = null;
            manualInputOutput = null;
            correctionInputDraining = correctionDrainWaiting = correctionHoldReady = correctionHoldTouched = false;
            correctionHoldOwner = IntPtr.Zero;
            correctionHoldFocus = null;
            correctionHoldEdit = null;
            correctionHoldOriginal = correctionHoldOutput = null;
            correctionTyping.Clear();
            return true;
        }

        // Start capturing later physical input before asynchronous discovery.
        // Typed events must remain ordered, even if discovery fails or focus moves.
        bool BeginManualInput(IntPtr owner, uint trigger = 0, bool released = false)
        {
            RecoverClosedInputTarget();
            if (manualInputActive)
                return correctionInputHold.Owns(owner);
            if (correctionInputDraining && correctionInputHold.Owns(owner))
                correctionInputDraining = false;
            else if (!correctionInputHold.Begin(owner))
                return false;
            correctionHoldOwner = owner;
            correctionHoldFocus = FocusId();
            correctionHoldReady = false;
            manualInputActive = true;
            manualRetryAttempts = 0;
            manualInputReady = false;
            manualInputEdit = null;
            manualWordAnchor = null;
            manualInputOutput = null;
            manualInputSelected = false;
            if (trigger != 0 && !released)
                correctionInputHold.InitialRelease(trigger);
            foreach (uint vk in new uint[]
            {
                0xA0,
                0xA1,
                0xA2,
                0xA3,
                0xA4,
                0xA5,
                0x5B,
                0x5C
            }

            )
                if (vk != trigger && (Native.GetAsyncKeyState((int)vk) & 0x8000) != 0)
                    correctionInputHold.InitialRelease(vk);
            if (DateTime.UtcNow <= diagnosticUntil)
                Diagnostic("manual input hold armed editor=" + NativeClass(owner));
            return true;
        }

        void ManualTarget(SafeEdit edit)
        {
            if (!manualInputActive || edit == null)
                return;
            if (manualWordAnchor != null)
                manualWordAnchor.FinishRecord();
            correctionHoldFocus = FocusId();
            edit.CaptureStableStart();
            manualInputEdit = edit;
            manualWordAnchor = null;
            manualInputOutput = null;
            manualInputSelected = edit.UserSelection;
        }

        void ManualSelectionVerified(SafeEdit edit, WordSelection.FontSnapshot continuation = null, bool captureInReplacement = false)
        {
            if (manualInputActive && edit != null && WordPasteOptions.IsWord(edit.Window))
            {
                // Native Word replacement already proves the same document,
                // visible range and continuation style. Capture inside that
                // transaction rather than binding and scanning revisions twice.
                if (!captureInReplacement || WordCommentFocus.IsCommentFocus(edit.Window))
                    manualWordAnchor = WordSelection.ReplayAnchor.Capture(edit.Window, edit.Expected, continuation);
            }
        }

        // Record a dispatched mutation before attempting post-edit caret proofs.
        // Recovery must know that text may already have changed if verification fails.
        void ManualCommitted(SafeEdit edit, string output, bool selected = false)
        {
            if (!manualInputActive)
                return;
            if (selected && edit != null)
                edit.CaptureStableStart();
            manualInputEdit = edit;
            manualInputOutput = output;
            manualInputSelected = selected;
        }

        // A completed conversion is not finished until the hold is settled.
        // Never release queued text against a different foreground target.
        void FinishManualInput()
        {
            if (!manualInputActive)
                return;
            manualInputReady = true;
            BeginInvoke((Action)ReleaseCorrectionInput);
        }

        bool ReleaseUnchangedControlTaps()
        {
            if (!manualInputReady || manualInputEdit != null || manualInputOutput != null || manualWordAnchor != null || !correctionInputHold.FinishIfOnlyControlTaps(correctionHoldOwner))
                return false;
            manualInputActive = manualInputReady = manualInputSelected = false;
            correctionInputDraining = correctionDrainWaiting = false;
            manualRetry.Stop();
            manualRetryAttempts = 0;
            Diagnostic("unchanged conversion retired repeated bare Ctrl taps");
            return true;
        }

        void ReleaseFailedManualInput()
        {
            if (manualWordAnchor != null)
                manualWordAnchor.FinishRecord();
            if (Native.GetForegroundWindow() != correctionHoldOwner || !EditGuard.SameFocus(correctionHoldFocus, FocusId()))
                return;
            if (correctionInputHold.FinishIfEmpty(correctionHoldOwner))
            {
                manualInputActive = manualInputReady = false;
                manualInputEdit = null;
                manualWordAnchor = null;
                manualInputOutput = null;
                Diagnostic("unverified manual conversion released empty input hold");
                return;
            }

            if (!SafeEdit.ReleaseUnverifiedCaret(correctionHoldOwner, correctionHoldFocus))
                return;
            Diagnostic("unverified manual conversion released at fresh editable caret");
            manualInputActive = manualInputReady = false;
            manualInputEdit = null;
            manualWordAnchor = null;
            manualInputOutput = null;
            correctionInputDraining = true;
            DrainCorrectionInput();
        }

        bool ReleaseManualInput()
        {
            if (!manualInputReady || !correctionInputHold.Owns(correctionHoldOwner))
                return false;
            if (ReleaseUnchangedControlTaps())
                return true;
            if (correctionInputHold.FinishIfEmpty(correctionHoldOwner))
            {
                if (manualWordAnchor != null)
                    manualWordAnchor.FinishRecord();
                manualInputActive = manualInputReady = false;
                manualInputEdit = null;
                manualWordAnchor = null;
                manualInputOutput = null;
                Diagnostic("manual conversion released empty input hold");
                return true;
            }

            if (Native.GetForegroundWindow() != correctionHoldOwner || !EditGuard.SameFocus(correctionHoldFocus, FocusId()))
                return false;
            // Discovery did not change selection or text. Do not require the same
            // unavailable provider to authorize typing at that untouched position.
            if (manualWordAnchor == null && SelectionConversion.UntouchedManualInput(manualInputEdit != null, manualInputOutput != null))
            {
                manualInputActive = manualInputReady = false;
                manualInputSelected = false;
                Diagnostic("unchanged manual target released queued typing");
                correctionInputDraining = true;
                DrainCorrectionInput();
                return true;
            }

            if (manualInputEdit == null)
            {
                bool present;
                var untouched = SafeEdit.ReadSelected(correctionHoldOwner, out present);
                if (present)
                {
                    if (untouched == null)
                        return false;
                    manualInputEdit = untouched;
                    manualInputSelected = true;
                    ManualSelectionVerified(untouched);
                }
            }

            bool typing = correctionInputHold.HasTyping;
            if (manualInputEdit != null && !(manualWordAnchor != null ? manualWordAnchor.Resume(manualInputOutput, manualInputSelected, typing) : manualInputEdit.ResumeManual(manualInputOutput, manualInputSelected, typing)))
            {
                Diagnostic("manual replay awaiting owned caret");
                return false;
            }

            if (!typing && manualInputEdit != null && manualInputSelected && correctionInputHold.HasTyping && !(manualWordAnchor != null ? manualWordAnchor.Resume(manualInputOutput, true, true) : manualInputEdit.ResumeManual(manualInputOutput, true, true)))
                return false;
            Diagnostic("manual input hold releasing count=" + correctionInputHold.Count);
            manualInputActive = manualInputReady = false;
            manualInputEdit = null;
            manualWordAnchor = null;
            manualInputOutput = null;
            correctionInputDraining = true;
            DrainCorrectionInput();
            return true;
        }

        IntPtr Mouse(int code, IntPtr w, IntPtr l)
        {
            bool drained = code >= 0 && ((Native.MouseHookData)Marshal.PtrToStructure(l, typeof(Native.MouseHookData))).extra.ToUInt64() == Native.DrainedInput;
            try
            {
                return MouseCore(code, w, l, drained);
            }
            finally
            {
                if (drained)
                {
                    correctionInputHold.AcknowledgeInFlight(correctionHoldOwner);
                    BeginInvoke((Action)(() =>
                    {
                        correctionDrainWaiting = false;
                        DrainCorrectionInput();
                    }));
                }
            }
        }

        // Low-level hooks must return promptly. Recognition schedules conversion;
        // provider queries and formatting are handled by the later worker/STA stages.
        IntPtr Keyboard(int code, IntPtr w, IntPtr l)
        {
            bool drained = code >= 0 && ((Native.KeyData)Marshal.PtrToStructure(l, typeof(Native.KeyData))).extra.ToUInt64() == Native.DrainedInput;
            try
            {
                return KeyboardCore(code, w, l, drained);
            }
            finally
            {
                if (drained)
                {
                    correctionInputHold.AcknowledgeInFlight(correctionHoldOwner);
                    BeginInvoke((Action)(() =>
                    {
                        correctionDrainWaiting = false;
                        DrainCorrectionInput();
                    }));
                }
            }
        }

        // Word must use the same ordered input transaction as browser fields.
        // Selecting/pasting a completed word can consume its delimiter and allows
        // the next physical word to overwrite the still-selected contraction.
        bool BeginCorrectionTyping(long generation, IntPtr owner, string input)
        {
            if (!InDesignSelection.IsInDesign(owner))
            {
                if (RecoverClosedInputTarget())
                    Log("pending correction target closed");
                if (correctionInputDraining && correctionInputHold.Owns(owner))
                    correctionInputDraining = false;
                else if (!correctionInputHold.Begin(owner))
                {
                    Diagnostic("automatic queue unavailable same-owner=" + correctionInputHold.Owns(owner) + " pending=" + correctionInputHold.Count);
                    return false;
                }

                correctionInputDraining = false;
                correctionHoldOwner = owner;
                correctionHoldReady = correctionHoldTouched = false;
                correctionHoldEdit = null;
                correctionHoldOriginal = input;
                correctionHoldOutput = null;
                correctionHoldFocus = FocusId();
            }

            correctionTyping.Begin(generation, owner);
            return true;
        }

        void ReleaseCorrectionInput()
        {
            if (manualInputActive)
            {
                bool released = ReleaseManualInput();
                if (released)
                {
                    manualRetry.Stop();
                    manualRetryAttempts = 0;
                }
                else if (manualInputReady && Native.GetForegroundWindow() == correctionHoldOwner && EditGuard.SameFocus(correctionHoldFocus, FocusId()) && manualRetryAttempts < 25)
                    manualRetry.Start();
                return;
            }

            IntPtr owner = correctionHoldOwner;
            if (!correctionHoldReady || !correctionInputHold.Owns(owner))
                return;
            // With no pending or in-flight input there is nothing to reposition or
            // replay. An editor rebuilding its caret must not reserve this global
            // queue and silently disable future automatic corrections in other apps.
            if (correctionInputHold.FinishIfEmpty(owner))
            {
                correctionInputDraining = correctionDrainWaiting = correctionHoldReady = correctionHoldTouched = false;
                correctionHoldOwner = IntPtr.Zero;
                correctionHoldEdit = null;
                correctionHoldOriginal = correctionHoldOutput = null;
                correctionHoldFocus = null;
                Diagnostic("automatic correction released empty input hold");
                return;
            }

            if (owner != Native.GetForegroundWindow() || !EditGuard.SameFocus(correctionHoldFocus, FocusId()))
                return;
            // Never deliver held typing into an unverified selection or another field.
            SafeEdit proof = correctionHoldOutput == null ? null : SafeEdit.BehindCaret(owner, correctionHoldOutput, false, true);
            if (proof == null && correctionHoldOriginal != null)
                proof = SafeEdit.BehindCaret(owner, correctionHoldOriginal, false, true);
            if (correctionHoldTouched && (proof == null || !proof.AtOwnedCaret()))
            {
                if (correctionHoldEdit == null)
                    return;
                bool recovered = correctionHoldOutput != null && correctionHoldEdit.RecoverAutomaticCaret(correctionHoldOutput);
                if (!recovered && correctionHoldOriginal != null)
                    recovered = correctionHoldEdit.RecoverAutomaticCaret(correctionHoldOriginal);
                if (!recovered)
                    return;
                proof = correctionHoldOutput == null ? null : SafeEdit.BehindCaret(owner, correctionHoldOutput, false, true);
                if (proof == null && correctionHoldOriginal != null)
                    proof = SafeEdit.BehindCaret(owner, correctionHoldOriginal, false, true);
                if (proof == null || !proof.AtOwnedCaret())
                    return;
            }

            correctionInputDraining = true;
            correctionHoldReady = false;
            DrainCorrectionInput();
        }

        void DrainCorrectionInput()
        {
            if (!correctionInputDraining || correctionDrainWaiting || !correctionInputHold.Owns(correctionHoldOwner) || Native.GetForegroundWindow() != correctionHoldOwner)
                return;
            Native.Input[] next = correctionInputHold.DrainNext(correctionHoldOwner);
            if (next == null)
                return;
            if (next.Length == 0)
            {
                correctionInputDraining = false;
                correctionHoldOwner = IntPtr.Zero;
                correctionHoldOriginal = correctionHoldOutput = null;
                correctionHoldFocus = null;
                return;
            }

            correctionDrainWaiting = true;
            if (!Native.ReplayCorrectionInput(next))
            {
                correctionDrainWaiting = false;
                Log("held correction input could not be replayed");
            }
        }
    }
}
