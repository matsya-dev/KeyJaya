// Opt-in passive Ctrl/focus metadata monitor. Never suppresses or sends input.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace KeyJaya
{
    internal static class AcrobatControlTapProbe
    {
        static readonly List<object> events = new List<object>();
        static readonly Stopwatch clock = Stopwatch.StartNew();
        static readonly LeftControlTap tap = new LeftControlTap();
        static IntPtr owner, hook, focusHook;
        static long generation;
        static bool completed;
        static long until = 45000;
        static string Kind(IntPtr window)
        {
            var value = new StringBuilder(128);
            Native.GetClassName(window, value, value.Capacity);
            return value.ToString();
        }

        static bool Acrobat(IntPtr window)
        {
            uint pid;
            Native.GetWindowThreadProcessId(window, out pid);
            return Native.ProcessImageIs(pid, "Acrobat.exe") || Native.ProcessImageIs(pid, "AcroRd32.exe");
        }

        static IntPtr Keyboard(int code, IntPtr w, IntPtr l)
        {
            if (code >= 0 && Native.GetForegroundWindow() == owner)
            {
                var key = (Native.KeyData)Marshal.PtrToStructure(l, typeof(Native.KeyData));
                if ((key.flags & 0x10) == 0 && LeftControlTap.IsLeftControl(key.vk, key.flags))
                {
                    bool down = w.ToInt32() == 0x100 || w.ToInt32() == 0x104;
                    bool accepted = false;
                    if (down)
                        tap.Down(owner, generation, DateTime.UtcNow);
                    else
                    {
                        accepted = tap.Up(owner, generation, DateTime.UtcNow);
                        completed = true;
                        until = clock.ElapsedMilliseconds + 1500;
                    }

                    events.Add(new { ms = clock.ElapsedMilliseconds, kind = down ? "Ctrl down" : "Ctrl up", accepted = accepted, generation = generation });
                }
            }

            return Native.CallNextHookEx(hook, code, w, l);
        }

        static void Focus(IntPtr h, uint evt, IntPtr window, int obj, int child, uint thread, uint time)
        {
            if (Native.GetForegroundWindow() != owner || FocusEventGuard.Ignore(evt, Native.GetAncestor(window, 2), owner))
                return;
            generation++;
            tap.Cancel();
            events.Add(new { ms = clock.ElapsedMilliseconds, kind = "focus", source = Kind(window), object_id = obj, child_id = child, generation = generation });
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length != 1 || args[0] != "--allow-desktop")
            {
                Console.WriteLine("Passive inspection requires --allow-desktop and user preparation.");
                return 0;
            }

            bool prepared = false;
            while (clock.ElapsedMilliseconds < 60000)
            {
                owner = Native.GetForegroundWindow();
                if (Acrobat(owner))
                {
                    var id = Native.FocusIdentity();
                    if (id != null && Kind(new IntPtr((uint)id[4] | ((long)id[5] << 32))) == "Edit")
                    {
                        prepared = true;
                        break;
                    }
                }

                Thread.Sleep(250);
            }

            if (!prepared || !Acrobat(owner))
            {
                Console.WriteLine("No prepared Acrobat field; nothing monitored.");
                return 0;
            }

            clock.Restart();
            Native.Hook keyboard = Keyboard;
            Native.WinEvent focus = Focus;
            var timer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            timer.Tick += (sender, e) =>
            {
                if (clock.ElapsedMilliseconds >= until)
                    Application.ExitThread();
            };
            try
            {
                hook = Native.SetWindowsHookEx(13, keyboard, Native.GetModuleHandle(null), 0);
                focusHook = Native.SetWinEventHook(0x8005, 0x8005, IntPtr.Zero, focus, 0, 0, 0);
                if (hook == IntPtr.Zero || focusHook == IntPtr.Zero)
                    throw new InvalidOperationException("Passive monitor unavailable.");
                Console.WriteLine("Monitoring Ctrl and focus metadata only; tap Left Ctrl once in the prepared field.");
                timer.Start();
                Application.Run();
            }
            finally
            {
                timer.Dispose();
                if (hook != IntPtr.Zero)
                    Native.UnhookWindowsHookEx(hook);
                if (focusHook != IntPtr.Zero)
                    Native.UnhookWinEvent(focusHook);
                GC.KeepAlive(keyboard);
                GC.KeepAlive(focus);
            }

            Console.WriteLine(new JavaScriptSerializer().Serialize(new { completed = completed, events = events }));
            return 0;
        }
    }
}
