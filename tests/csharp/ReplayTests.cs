// Noninteractive regressions: deterministic event replay and state-transition models.
// Run: .\test.ps1 -Suite ReplayTests. Fixtures live under out/; no resident hooks are started.
using System;

namespace KeyJaya
{
    public static class ReplayTests
    {
        static int passed, failed;
        static void Check(bool ok, string name)
        {
            if (ok)
                passed++;
            else
            {
                failed++;
                if (failed < 20)
                    Console.WriteLine("FAIL " + name);
            }
        }

        public static int Main(string[] args)
        {
            DateTime now = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);
            IntPtr window = new IntPtr(10);
            var replay = new DelimiterReplay();
            Check(!replay.Consume(231, 32, 16, 256, 1, window, now), "injected space without physical completion rejected");
            replay.Begin(1, IntPtr.Zero, ' ', now);
            Check(!replay.Consume(231, 32, 16, 256, 1, IntPtr.Zero, now), "no foreground window rejected");
            for (int stamp = 1; stamp <= 1000; stamp++)
            {
                replay.Begin(stamp, window, ' ', now);
                Check(replay.Consume(231, 32, 16, 256, stamp, window, now.AddMilliseconds(499)), "matching down accepted");
                Check(!replay.Consume(231, 32, 16, 256, stamp, window, now.AddMilliseconds(499)), "second down rejected");
                Check(replay.Consume(231, 32, 144, 257, stamp, window, now.AddMilliseconds(499)), "matching up accepted");
                Check(!replay.Consume(231, 32, 144, 257, stamp, window, now.AddMilliseconds(499)), "second up rejected");
                Check(!replay.Consume(231, 32, 16, 256, stamp, window, now.AddMilliseconds(499)), "second pair rejected");
                foreach (int delay in new[]
                {
                    500,
                    501,
                    1000,
                    10000
                }

                )
                {
                    replay.Begin(stamp, window, ' ', now);
                    Check(!replay.Consume(231, 32, 16, 256, stamp, window, now.AddMilliseconds(delay)), "expired attempt rejected");
                }

                foreach (uint scan in new uint[]
                {
                    0,
                    9,
                    13,
                    31,
                    33,
                    65,
                    97,
                    0x2019,
                    0xFFFF
                }

                )
                {
                    replay.Begin(stamp, window, ' ', now);
                    Check(!replay.Consume(231, scan, 16, 256, stamp, window, now), "only exact Unicode space accepted");
                }

                foreach (uint key in new uint[]
                {
                    0,
                    8,
                    9,
                    13,
                    16,
                    32,
                    65,
                    86,
                    255
                }

                )
                {
                    replay.Begin(stamp, window, ' ', now);
                    Check(!replay.Consume(key, 32, 16, 256, stamp, window, now), "nonpacket keys rejected");
                }

                foreach (int message in new[]
                {
                    0,
                    257,
                    260,
                    261
                }

                )
                {
                    replay.Begin(stamp, window, ' ', now);
                    Check(!replay.Consume(231, 32, 16, message, stamp, window, now), "wrong message rejected");
                }

                replay.Begin(stamp, window, ' ', now);
                Check(!replay.Consume(231, 32, 0, 256, stamp, window, now), "noninjected rejected");
                Check(!replay.Consume(231, 32, 144, 257, stamp, window, now), "up without matching down rejected");
                replay.Begin(stamp, window, ' ', now);
                Check(!replay.Consume(231, 32, 16, 256, stamp + 1, window, now), "further typing invalidates replay");
                Check(!replay.Consume(231, 32, 16, 256, stamp, new IntPtr(11), now), "different window rejected");
                foreach (char delimiter in new[]
                {
                    '\t',
                    '\n',
                    '>',
                    '<',
                    ':',
                    'a'
                }

                )
                {
                    replay.Begin(stamp, window, delimiter, now);
                    Check(!replay.Consume(231, 32, 16, 256, stamp, window, now), "non-space physical completion rejected");
                }

                replay.Begin(stamp, window, ' ', now);
                replay.Clear(stamp);
                Check(!replay.Consume(231, 32, 16, 256, stamp, window, now), "completed worker invalidates replay");
                replay.Begin(stamp + 1, window, ' ', now);
                replay.Clear(stamp);
                Check(replay.Consume(231, 32, 16, 256, stamp + 1, window, now), "older worker preserves newest completion");
            }

            foreach (string word in new[]
            {
                "didnt",
                "doesnt",
                "cant",
                "wouldnt",
                "isnt",
                "Im"
            }

            )
            {
                var state = new InputState();
                foreach (char c in word)
                    state.Type(c);
                string candidate = state.CompletedEnglish(' ');
                state.Type(' ');
                long stamp = state.Generation;
                replay.Begin(stamp, window, ' ', now);
                Check(candidate == word + " ", "physical completed word captured");
                Check(replay.Consume(231, 32, 16, 256, stamp, window, now), "physical completed word replay accepted");
                Check(state.Generation == stamp && state.Token == "", "replay does not type or change buffer");
                Check(replay.Consume(231, 32, 144, 257, stamp, window, now), "physical completion up accepted");
            }

            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
