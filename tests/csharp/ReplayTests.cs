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

        static void NavigationChords()
        {
            var owner = new IntPtr(-7821);
            foreach (uint control in new uint[]
            {
                0xA2,
                0xA3
            }

            )
                foreach (uint shift in new uint[]
                {
                    0xA0,
                    0xA1
                }

                )
                    foreach (uint arrow in new uint[]
                    {
                        0x21,
                        0x22,
                        0x23,
                        0x24,
                        0x25,
                        0x26,
                        0x27,
                        0x28
                    }

                    )
                        foreach (bool shiftFirst in new[]
                        {
                            false,
                            true
                        }

                        )
                            foreach (bool controlUpFirst in new[]
                            {
                                false,
                                true
                            }

                            )
                                foreach (int repeats in new[]
                                {
                                    1,
                                    3
                                }

                                )
                                {
                                    var hold = new Native.CorrectionInputHold();
                                    var events = new System.Collections.Generic.List<Native.KeyData>();
                                    Action<uint, bool> add = (key, up) => events.Add(new Native.KeyData { vk = key, scan = key == arrow ? 0x4Bu : 0x1Du, flags = (key == arrow || key == 0xA3 ? 1u : 0u) | (up ? 0x80u : 0u) });
                                    add(shiftFirst ? shift : control, false);
                                    add(shiftFirst ? control : shift, false);
                                    for (int n = 0; n < repeats; n++)
                                        add(arrow, false);
                                    add(arrow, true);
                                    add(controlUpFirst ? control : shift, true);
                                    add(controlUpFirst ? shift : control, true);
                                    Check(hold.Begin(owner), "Selection chord owns an isolated replay transaction");
                                    foreach (var key in events)
                                        Check(hold.Keyboard(key, (key.flags & 0x80) == 0 ? 0x100 : 0x101, owner), "Whole Ctrl+Shift+navigation chord is captured in order");
                                    Check(!hold.HasTyping, "Selection navigation is not mistaken for text typing");
                                    Check(!hold.FinishIfOnlyControlTaps(owner), "Selection chord is never retired as repeated bare Ctrl taps");
                                    var replayed = hold.Take(owner);
                                    Check(replayed.Length == events.Count, "Selection chord retains modifier releases and autorepeat");
                                    for (int n = 0; n < events.Count; n++)
                                    {
                                        var actual = replayed[n].union.keyboard;
                                        var expected = events[n];
                                        Check(actual.vk == expected.vk && actual.scan == expected.scan && actual.flags == ((expected.flags & 1) | ((expected.flags & 0x80) == 0 ? 0u : 2u)) && actual.extra.ToUInt64() == Native.ReplayedInput, "Selection chord preserves physical key identity, extended arrows and FIFO order");
                                    }

                                    Check(!hold.Active, "Selection chord replay does not reserve the next application's queue");
                                }
        }

        public static int Main(string[] args)
        {
            NavigationChords();
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
