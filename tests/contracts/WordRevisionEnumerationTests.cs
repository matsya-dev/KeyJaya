// Real projection/enumeration code against counted managed/native-style
// collections, including failed native enumeration and retained deletions.
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace KeyJaya
{
    internal static class WordRevisionEnumerationTests
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

        sealed class NativeIterator : IEnumVARIANT
        {
            readonly object[] items;
            readonly int failure;
            int index;
            internal NativeIterator(object[] items, int failure)
            {
                this.items = items;
                this.failure = failure;
            }

            public int Next(int count, object[] values, IntPtr fetched)
            {
                if (failure != 0)
                    return failure;
                int taken = Math.Min(count, items.Length - index);
                for (int n = 0; n < taken; n++)
                    values[n] = items[index++];
                if (fetched != IntPtr.Zero)
                    Marshal.WriteInt32(fetched, taken);
                return taken == count ? 0 : 1;
            }

            public int Skip(int count)
            {
                throw new NotSupportedException();
            }

            public int Reset()
            {
                index = 0;
                return 0;
            }

            public IEnumVARIANT Clone()
            {
                throw new NotSupportedException();
            }
        }

        sealed class Collection
        {
            internal object[] Items = new object[0];
            internal bool Native;
            internal int Failure, Counts, Enumerations;
            internal int? ReportedCount;
            internal bool FailCount;
            public int Count
            {
                get
                {
                    Counts++;
                    if (FailCount)
                        throw new Exception("Revision count unavailable");
                    return ReportedCount ?? Items.Length;
                }
            }

            public object _NewEnum
            {
                get
                {
                    Enumerations++;
                    return Native ? (object)new NativeIterator(Items, Failure) : Items.GetEnumerator();
                }
            }
        }

        static WordSelection.VisibleMap Project(Collection revisions)
        {
            var scope = new Scope
            {
                Start = 10,
                End = 13,
                Text = "abc",
                Revisions = revisions
            };
            return (WordSelection.VisibleMap)typeof(WordSelection).GetMethod("Visible", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { scope });
        }

        internal static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (valid, message) =>
            {
                if (!valid)
                    throw new Exception("Revision enumeration: " + message);
                passed++;
            };
            foreach (bool native in new[]
            {
                false,
                true
            }

            )
            {
                var collection = new Collection
                {
                    Native = native
                };
                var map = Project(collection);
                check(map != null && map.Value == "abc" && map.Start == 10 && map.End == 13, "empty collection preserves the exact range");
                check(collection.Counts == 1 && collection.Enumerations == 0, "zero revisions need no native enumerator");
                foreach (int kind in new[]
                {
                    2,
                    14
                }

                )
                {
                    collection.Items = new object[]
                    {
                        new Revision
                        {
                            Type = kind,
                            Range = new Scope
                            {
                                Start = 11,
                                End = 12
                            }
                        }
                    };
                    map = Project(collection);
                    check(map != null && map.Value == "ac" && map.NativeOffset(1) == 12, "tracked deletion/move-from stays excluded with native positions intact");
                }

                foreach (int kind in new[]
                {
                    7,
                    9,
                    16
                }

                )
                {
                    collection.Items = new object[]
                    {
                        new Revision
                        {
                            Type = kind
                        }
                    };
                    check(Project(collection) == null, "unsupported revision still blocks conversion");
                }
            }

            foreach (int status in new[]
            {
                unchecked((int)0x80004005),
                2
            }

            )
            {
                bool rejected = false;
                try
                {
                    Project(new Collection { Native = true, Failure = status, ReportedCount = 1 });
                }
                catch (TargetInvocationException)
                {
                    rejected = true;
                }

                check(rejected, "failed/unknown native enumeration status is never mistaken for no revisions");
            }

            var many = new object[10001];
            for (int n = 0; n < many.Length; n++)
                many[n] = new Revision
                {
                    Type = 1
                };
            bool bounded = false;
            try
            {
                Project(new Collection { Items = many, ReportedCount = 1 });
            }
            catch (TargetInvocationException error)
            {
                bounded = error.InnerException is InvalidOperationException;
            }

            check(bounded, "the existing 10,000-revision ceiling remains enforced");
            foreach (int count in new[]
            {
                -1,
                10001
            }

            )
            {
                var collection = new Collection
                {
                    ReportedCount = count
                };
                check(Project(collection) == null && collection.Enumerations == 0, "invalid or excessive count refuses projection before enumeration");
            }

            bool unavailable = false;
            try
            {
                Project(new Collection { FailCount = true });
            }
            catch (TargetInvocationException)
            {
                unavailable = true;
            }

            check(unavailable, "an unavailable count never becomes an empty revision collection");
            Console.WriteLine("Word revision enumeration: " + passed + " assertions; deletion coordinates, failures and limit; no Word access.");
            return passed;
        }
    }
}
