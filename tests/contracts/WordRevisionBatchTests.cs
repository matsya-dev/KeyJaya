// Full production projection against counted native-style batches and the
// public Framework adapter bridge. No Word, UIA, clipboard or desktop input.
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace KeyJaya
{
    internal static class WordRevisionBatchTests
    {
        sealed class Revision
        {
            public int Type { get; set; }
            public Scope Range { get; set; }
        }

        sealed class Scope
        {
            public int Start { get; set; }
            public int End { get; set; }
            public string Text { get; set; }
            public Collection Revisions { get; set; }
        }

        sealed class Iterator : IEnumVARIANT
        {
            internal object[] Items;
            internal int At, Calls, Largest, FaultCall = 1;
            internal string Fault;
            public int Next(int count, object[] values, IntPtr fetched)
            {
                Calls++;
                Largest = Math.Max(Largest, count);
                int taken = Math.Min(count, Items.Length - At);
                for (int n = 0; n < taken; n++)
                    values[n] = Items[At++];
                if (fetched == IntPtr.Zero && count != 1)
                    throw new Exception("A batched call requires its fetched count");
                if (fetched != IntPtr.Zero)
                    Marshal.WriteInt32(fetched, taken);
                int status = taken == count ? 0 : 1;
                if (Calls != FaultCall)
                    return status;
                if (Fault == "throw")
                    throw new InvalidOperationException("Synthetic iterator failure");
                if (Fault == "hresult")
                    return unchecked((int)0x80004005);
                if (Fault == "status")
                    return 2;
                if (Fault == "negative")
                    Marshal.WriteInt32(fetched, -1);
                if (Fault == "oversized")
                    Marshal.WriteInt32(fetched, count + 1);
                if (Fault == "short-ok")
                    return 0;
                if (Fault == "full-false")
                    return 1;
                if (Fault == "missing")
                    values[0] = null;
                if (Fault == "trailing")
                    values[taken] = new Revision
                    {
                        Type = 16
                    };
                return status;
            }

            public int Reset()
            {
                throw new Exception("The current native position must not reset");
            }

            public int Skip(int count)
            {
                throw new Exception("No revisions may be skipped");
            }

            public IEnumVARIANT Clone()
            {
                throw new Exception("The iterator must not be cloned");
            }
        }

        sealed class Adapter : IEnumerator, ICustomAdapter
        {
            internal object Underlying;
            internal IEnumerator Managed;
            object current;
            internal int Unwraps, Moves;
            internal bool Fail;
            public object GetUnderlyingObject()
            {
                Unwraps++;
                if (Fail)
                    throw new InvalidOperationException("Adapter unavailable");
                return Underlying;
            }

            public bool MoveNext()
            {
                Moves++;
                if (Managed != null)
                    return Managed.MoveNext();
                var values = new object[1];
                int status = ((IEnumVARIANT)Underlying).Next(1, values, IntPtr.Zero);
                if (status < 0)
                    Marshal.ThrowExceptionForHR(status);
                current = values[0];
                return status == 0;
            }

            public object Current
            {
                get
                {
                    return Managed == null ? current : Managed.Current;
                }
            }

            public void Reset()
            {
                throw new Exception("Do not reset the managed fallback");
            }
        }

        sealed class Collection
        {
            internal int Counts, Enumerations, Reported;
            internal object Iterator;
            public int Count
            {
                get
                {
                    Counts++;
                    return Reported;
                }
            }

            public object _NewEnum
            {
                get
                {
                    Enumerations++;
                    return Iterator;
                }
            }
        }

        static WordSelection.VisibleMap Project(Collection collection)
        {
            return (WordSelection.VisibleMap)typeof(WordSelection).GetMethod("Visible", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { new Scope { Start = 10, End = 16, Text = "abcdef", Revisions = collection } });
        }

        static object[] Items(int count)
        {
            return Enumerable.Range(0, count).Select(n => (object)new Revision { Type = 1 }).ToArray();
        }

        internal static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (valid, message) =>
            {
                if (!valid)
                    throw new Exception("Revision batching: " + message);
                passed++;
            };
            foreach (bool wrapped in new[]
            {
                false,
                true
            }

            )
                foreach (int count in new[]
                {
                    1,
                    2,
                    31,
                    32,
                    33,
                    64,
                    65
                }

                )
                {
                    var native = new Iterator
                    {
                        Items = Items(count)
                    };
                    native.Items[count - 1] = new Revision
                    {
                        Type = 2,
                        Range = new Scope
                        {
                            Start = 11,
                            End = 12
                        }
                    };
                    var adapter = new Adapter
                    {
                        Underlying = native
                    };
                    var collection = new Collection
                    {
                        Reported = count,
                        Iterator = wrapped ? (object)adapter : native
                    };
                    var map = Project(collection);
                    check(map != null && map.Value == "acdef" && map.NativeOffset(1) == 12, "partial/full batches preserve the last deletion and native positions: " + count + "/" + wrapped);
                    int size = Math.Min(32, count + 1);
                    check(native.Calls == count / size + 1 && native.Largest == size && collection.Counts == 1 && collection.Enumerations == 1, "bounded batches reduce native calls without another Count: " + count + "/" + wrapped);
                    if (wrapped)
                        check(adapter.Unwraps == 1 && adapter.Moves == 0, "the public bridge lends the existing iterator once");
                }

            foreach (int type in new[]
            {
                2,
                14,
                7,
                9,
                16
            }

            )
            {
                var items = Items(65);
                items[64] = new Revision
                {
                    Type = type,
                    Range = new Scope
                    {
                        Start = 11,
                        End = 12
                    }
                };
                var native = new Iterator
                {
                    Items = items
                };
                var map = Project(new Collection { Reported = 65, Iterator = native });
                check(type == 2 || type == 14 ? map != null && map.Value == "acdef" : map == null, "all batches retain deletion/move filtering and unsupported type rejection: " + type);
            }

            var growing = new Iterator
            {
                Items = Items(3)
            };
            growing.Items[2] = new Revision
            {
                Type = 16
            };
            check(Project(new Collection { Reported = 1, Iterator = growing }) == null && growing.Calls == 2, "enumeration continues beyond the preliminary count and sees a later unsupported revision");
            var empty = new Iterator
            {
                Items = new object[0]
            };
            check(Project(new Collection { Reported = 1, Iterator = empty }).Value == "abcdef" && empty.Calls == 1, "a now-empty iterator requires an explicit valid end result");
            foreach (string fault in new[]
            {
                "throw",
                "hresult",
                "status",
                "negative",
                "oversized",
                "short-ok",
                "full-false",
                "missing",
                "trailing"
            }

            )
            {
                int count = fault == "full-false" ? 2 : 1;
                var native = new Iterator
                {
                    Items = Items(count),
                    Fault = fault
                };
                bool refused = false;
                try
                {
                    Project(new Collection { Reported = 1, Iterator = native });
                }
                catch (TargetInvocationException error)
                {
                    refused = fault != "hresult" || error.InnerException.HResult == unchecked((int)0x80004005);
                }

                check(refused, "malformed/failed batches never become empty revisions: " + fault);
            }

            foreach (string fault in new[]
            {
                "throw",
                "hresult"
            }

            )
            {
                var native = new Iterator
                {
                    Items = Items(65),
                    Fault = fault,
                    FaultCall = 2
                };
                bool refused = false;
                try
                {
                    Project(new Collection { Reported = 65, Iterator = native });
                }
                catch (TargetInvocationException)
                {
                    refused = true;
                }

                check(refused && native.Calls == 2, "a later failure cannot authorize the already collected prefix: " + fault);
            }

            var excessive = new Iterator
            {
                Items = Items(10001)
            };
            bool bounded = false;
            try
            {
                Project(new Collection { Reported = 1, Iterator = excessive });
            }
            catch (TargetInvocationException error)
            {
                bounded = error.InnerException is InvalidOperationException;
            }

            check(bounded && excessive.Largest == 2, "a growing native collection retains the 10,000-item ceiling");
            var noRead = new Iterator
            {
                Items = Items(1)
            };
            var zero = new Collection
            {
                Reported = 0,
                Iterator = noRead
            };
            check(Project(zero).Value == "abcdef" && zero.Counts == 1 && zero.Enumerations == 0 && noRead.Calls == 0, "revision-free ranges retain their existing count-only path");
            foreach (object opaque in new object[]
            {
                null,
                new object ()
            }

            )
            {
                var adapter = new Adapter
                {
                    Underlying = opaque,
                    Managed = new object[]
                    {
                        new Revision
                        {
                            Type = 2,
                            Range = new Scope
                            {
                                Start = 11,
                                End = 12
                            }
                        }
                    }.GetEnumerator()
                };
                check(Project(new Collection { Reported = 1, Iterator = adapter }).Value == "acdef" && adapter.Unwraps == 1 && adapter.Moves == 2, "an unavailable native bridge keeps the complete managed fallback");
            }

            var failed = new Adapter
            {
                Fail = true
            };
            bool unavailable = false;
            try
            {
                Project(new Collection { Reported = 1, Iterator = failed });
            }
            catch (TargetInvocationException)
            {
                unavailable = true;
            }

            check(unavailable && failed.Unwraps == 1 && failed.Moves == 0, "a failed adapter is unavailable evidence, not no revisions");
            Console.WriteLine("Word revision batches: " + passed + " assertions; native bridge, call reduction, partial/error batches and full filtering; no Word access.");
            return passed;
        }
    }
}
