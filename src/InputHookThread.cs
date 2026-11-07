// Keep low-level hooks off the WinForms/UIA/Word apartment. Windows can silently
// remove a low-level hook when its installing thread cannot dispatch in time.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyJaya
{
    internal interface IInputHookBackend
    {
        IntPtr Keyboard { get; }

        IntPtr Mouse { get; }

        void Install(Native.Hook keyboard, Native.Hook mouse);
        void Pump();
        void Quit();
        void Release();
    }

    internal sealed class InputHookThread : IDisposable
    {
        [ThreadStatic]
        static bool dispatching;
        readonly IInputHookBackend backend;
        readonly Native.Hook keyboard, mouse;
        readonly ManualResetEvent ready = new ManualResetEvent(false);
        readonly ManualResetEvent stop = new ManualResetEvent(false);
        readonly Thread thread;
        readonly Action<string> log;
        IntPtr initialKeyboard, initialMouse;
        Exception failure;
        int disposed;
        public IntPtr Keyboard
        {
            get
            {
                return initialKeyboard;
            }
        }

        public IntPtr Mouse
        {
            get
            {
                return initialMouse;
            }
        }

        internal InputHookThread(Native.Hook keyboard, Native.Hook mouse, Action<string> log = null) : this(new NativeBackend(), keyboard, mouse, log)
        {
        }

        internal InputHookThread(IInputHookBackend backend, Native.Hook keyboard, Native.Hook mouse, Action<string> log = null)
        {
            this.backend = backend;
            this.keyboard = keyboard;
            this.mouse = mouse;
            this.log = log;
            thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "KeyJaya input hooks"
            };
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            if (!ready.WaitOne(3000))
            {
                Dispose();
                throw new InvalidOperationException("Input thread did not start.");
            }

            if (failure != null)
            {
                Dispose();
                throw new InvalidOperationException("Input hooks could not be registered.", failure);
            }
        }

        void Run()
        {
            dispatching = true;
            bool started = false;
            int failures = 0;
            try
            {
                while (!stop.WaitOne(0))
                {
                    var uptime = Stopwatch.StartNew();
                    try
                    {
                        backend.Install(keyboard, mouse);
                        if (!started)
                        {
                            // These prove initial registration only. The backend
                            // owns each live handle through recovery; Windows
                            // ignores the handle passed to CallNextHookEx.
                            initialKeyboard = backend.Keyboard;
                            initialMouse = backend.Mouse;
                            started = true;
                            ready.Set();
                        }

                        if (!stop.WaitOne(0))
                            backend.Pump();
                    }
                    catch (Exception error)
                    {
                        if (!started)
                        {
                            failure = error;
                            return;
                        }

                        Report("Input hook dispatch failed: " + error.GetType().Name);
                    }
                    finally
                    {
                        backend.Release();
                    }

                    if (stop.WaitOne(0))
                        break;
                    failures = uptime.ElapsedMilliseconds >= 60000 ? 1 : Math.Min(failures + 1, 4);
                    int delay = failures == 1 ? 250 : failures == 2 ? 1000 : failures == 3 ? 2500 : 5000;
                    Report("Input hooks stopped unexpectedly; retry in " + delay + " ms.");
                    if (stop.WaitOne(delay))
                        break;
                }
            }
            finally
            {
                dispatching = false;
                ready.Set();
            }
        }

        // Diagnostics invoked by a keyboard/mouse callback must not wait for
        // profile I/O. Other callers keep their synchronous logging contract.
        internal static void WriteLog(Action<string> write, string message)
        {
            if (!dispatching)
            {
                write(message);
                return;
            }

            ThreadPool.QueueUserWorkItem(unused =>
            {
                try
                {
                    write(message);
                }
                catch (Exception)
                {
                }
            });
        }

        void Report(string message)
        {
            // Operational logging must never delay the thread that dispatches
            // hooks, even if the profile is on a slow or unavailable drive.
            if (log != null)
                ThreadPool.QueueUserWorkItem(unused =>
                {
                    try
                    {
                        log(message);
                    }
                    catch (Exception)
                    {
                    }
                });
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            stop.Set();
            backend.Quit();
            if (thread.Join(1000))
            {
                ready.Dispose();
                stop.Dispose();
            }
        }

        sealed class NativeBackend : IInputHookBackend
        {
            [StructLayout(LayoutKind.Sequential)]
            struct Message
            {
                public IntPtr Window;
                public uint Id;
                public UIntPtr W;
                public IntPtr L;
                public uint Time;
                public Native.Point Point;
                public uint Private;
            }

            [DllImport("kernel32.dll")]
            static extern uint GetCurrentThreadId();
            [DllImport("user32.dll")]
            static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint flags);
            [DllImport("user32.dll")]
            static extern int GetMessage(out Message message, IntPtr window, uint min, uint max);
            [DllImport("user32.dll")]
            static extern bool PostThreadMessage(uint thread, uint message, UIntPtr w, IntPtr l);
            uint owner;
            public IntPtr Keyboard { get; private set; }
            public IntPtr Mouse { get; private set; }

            public void Install(Native.Hook keyboard, Native.Hook mouse)
            {
                owner = GetCurrentThreadId();
                Message ignored;
                PeekMessage(out ignored, IntPtr.Zero, 0, 0, 0);
                Keyboard = Native.SetWindowsHookEx(13, keyboard, Native.GetModuleHandle(null), 0);
                Mouse = Native.SetWindowsHookEx(14, mouse, Native.GetModuleHandle(null), 0);
                if (Keyboard == IntPtr.Zero || Mouse == IntPtr.Zero)
                    throw new InvalidOperationException("Hook registration failed.");
            }

            public void Pump()
            {
                Message message;
                int status;
                while ((status = GetMessage(out message, IntPtr.Zero, 0, 0)) > 0)
                {
                }

                if (status < 0)
                    throw new InvalidOperationException("Input message loop failed.");
            }

            public void Quit()
            {
                if (owner != 0)
                    PostThreadMessage(owner, 0x12, UIntPtr.Zero, IntPtr.Zero);
            }

            public void Release()
            {
                if (Keyboard != IntPtr.Zero)
                    Native.UnhookWindowsHookEx(Keyboard);
                if (Mouse != IntPtr.Zero)
                    Native.UnhookWindowsHookEx(Mouse);
                Keyboard = Mouse = IntPtr.Zero;
            }
        }
    }
}
