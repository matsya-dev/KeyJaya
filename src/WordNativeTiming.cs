// Diagnostic-only native transaction timings. Values/arguments/document names
// are never recorded; a failed observer cannot change conversion or rollback.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace KeyJaya
{
    internal sealed class WordNativeTiming : IDisposable
    {
        sealed class Phase
        {
            internal string Name, Slowest;
            internal long Started, Wall, CallsTime, Maximum;
            internal int Calls, Gets, Sets;
            internal long ReleaseTime, ReleaseMaximum;
            internal int Releases;
        }

        [ThreadStatic]
        static WordNativeTiming current;
        static int sequence;
        readonly WordNativeTiming previous;
        readonly Action<string> trace;
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly List<Phase> phases = new List<Phase>();
        readonly int id;
        Phase phase;
        bool committed, disposed;
        internal static bool Active
        {
            get
            {
                return current != null;
            }
        }

        WordNativeTiming(Action<string> trace)
        {
            this.trace = trace;
            previous = current;
            current = this;
            id = Interlocked.Increment(ref sequence);
            Next("focus");
        }

        internal static WordNativeTiming Begin(Action<string> trace)
        {
            return trace == null ? null : new WordNativeTiming(trace);
        }

        internal static void Next(string name)
        {
            if (current == null)
                return;
            long now = current.clock.ElapsedTicks;
            if (current.phase != null)
                current.phase.Wall = now - current.phase.Started;
            current.phase = new Phase
            {
                Name = name,
                Started = now
            };
            current.phases.Add(current.phase);
        }

        internal static void Record(string member, BindingFlags kind, long ticks)
        {
            if (current == null)
                return;
            var phase = current.phase;
            phase.Calls++;
            phase.CallsTime += ticks;
            if ((kind & BindingFlags.GetProperty) != 0)
                phase.Gets++;
            if ((kind & BindingFlags.SetProperty) != 0)
                phase.Sets++;
            if (ticks >= phase.Maximum)
            {
                phase.Maximum = ticks;
                phase.Slowest = member;
            }
        }

        internal static void Committed()
        {
            if (current != null)
                current.committed = true;
        }

        internal static void Release(Action release)
        {
            var owner = current;
            if (owner == null)
            {
                release();
                return;
            }

            var phase = owner.phase;
            var watch = Stopwatch.StartNew();
            try
            {
                release();
            }
            finally
            {
                phase.Releases++;
                long elapsed = watch.ElapsedTicks;
                phase.ReleaseTime += elapsed;
                phase.ReleaseMaximum = Math.Max(phase.ReleaseMaximum, elapsed);
            }
        }

        internal static T Measure<T>(string member, Func<T> call)
        {
            if (current == null)
                return call();
            var watch = Stopwatch.StartNew();
            try
            {
                return call();
            }
            finally
            {
                Record(member, BindingFlags.InvokeMethod, watch.ElapsedTicks);
            }
        }

        static string Ms(long ticks)
        {
            return (ticks * 1000.0 / Stopwatch.Frequency).ToString("F1", CultureInfo.InvariantCulture);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            clock.Stop();
            phase.Wall = clock.ElapsedTicks - phase.Started;
            current = previous;
            var message = new StringBuilder("Word native timing id=" + id + " committed=" + committed + " total_ms=" + Ms(clock.ElapsedTicks));
            foreach (var item in phases)
                message.Append(" [" + item.Name + " ms=" + Ms(item.Wall) + " calls=" + item.Calls + " gets=" + item.Gets + " sets=" + item.Sets + " calls_ms=" + Ms(item.CallsTime) + " max_member=" + (item.Slowest ?? "none") + " max_ms=" + Ms(item.Maximum) + " releases=" + item.Releases + " release_ms=" + Ms(item.ReleaseTime) + " release_max_ms=" + Ms(item.ReleaseMaximum) + "]");
            try
            {
                trace(message.ToString());
            }
            catch (Exception)
            {
            }
        }
    }
}
