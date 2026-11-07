// The observed Navigation native path and routing policy, without Office/UIA.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class WordSearchFieldTests
    {
        public static int Run(string root)
        {
            int passed = 0;
            Action<bool, string> check = (value, message) =>
            {
                if (!value)
                    throw new Exception("Word search: " + message);
                passed++;
            };
            string[] classes =
            {
                "RICHEDIT60W",
                "NetUICtrlNotifySink",
                "NetUIHWND",
                "NUIPane",
                "MsoWorkPane",
                "MsoCommandBar",
                "MsoCommandBarDock",
                "OpusApp"
            };
            var nodes = new Dictionary<IntPtr, WordSearchField.Node>();
            var parents = new Dictionary<IntPtr, IntPtr>();
            for (int n = 0; n < classes.Length; n++)
            {
                var handle = new IntPtr(n + 1);
                nodes.Add(handle, new WordSearchField.Node { Class = classes[n], Process = 41, Id = n == 0 ? 1 : 0 });
                parents.Add(handle, n == classes.Length - 1 ? IntPtr.Zero : new IntPtr(n + 2));
            }

            int reads = 0, steps = 0;
            Func<IntPtr, WordSearchField.Node> read = handle =>
            {
                reads++;
                return nodes[handle];
            };
            Func<IntPtr, IntPtr> parent = handle =>
            {
                steps++;
                return parents[handle];
            };
            Func<bool> match = () => WordSearchField.Path(new IntPtr(1), new IntPtr(8), 41, read, parent);
            check(match() && reads == 8 && steps == 7, "observed docked query validates once per native ancestor");
            for (int n = 0; n < classes.Length; n++)
            {
                var node = nodes[new IntPtr(n + 1)];
                node.Process = 42;
                check(!match(), "foreign process at depth " + n + " is rejected");
                node.Process = 41;
                node.Class = "Unrelated";
                check(!match(), "changed native class at depth " + n + " is rejected");
                node.Class = classes[n];
            }

            var first = nodes[new IntPtr(1)];
            first.Id = 2;
            check(!match(), "another Rich Edit control ID is not the query");
            first.Id = 1;
            foreach (string name in new[]
            {
                "_WwG",
                "Edit",
                "RICHEDIT50W",
                "NetUITextbox",
                null
            }

            )
            {
                first.Class = name;
                reads = steps = 0;
                check(!match() && reads == 1 && steps == 0, "body/ribbon/unknown field is rejected before ancestry: " + name);
            }

            first.Class = classes[0];
            check(!WordSearchField.Path(new IntPtr(1), new IntPtr(9), 41, read, parent), "matching classes cannot cross to another frame");
            check(!WordSearchField.Path(IntPtr.Zero, new IntPtr(8), 41, read, parent), "missing native focus is unavailable");
            check(!WordSearchField.Path(new IntPtr(1), IntPtr.Zero, 41, read, parent), "missing owner is unavailable");
            check(!WordSearchField.Path(new IntPtr(1), new IntPtr(8), 0, read, parent), "missing process cannot authorize query access");
            parents[new IntPtr(4)] = IntPtr.Zero;
            check(!match(), "incomplete native path is unavailable");
            parents[new IntPtr(4)] = new IntPtr(1);
            reads = 0;
            check(!match() && reads <= 8, "provider parent cycles stay bounded");
            parents[new IntPtr(4)] = new IntPtr(5);
            check(match(), "fresh path is checked again after a transient failure");
            check(WordSearchField.Writable(0x50000088), "observed single-line query style is writable");
            foreach (int style in new[]
            {
                0x08000000,
                0x800,
                0x20,
                0x4,
                0x824
            }

            )
                check(!WordSearchField.Writable(0x50000088 | style), "disabled/read-only/password/multiline query is refused: " + style);
            check(WordSearchField.Editor("NetUITextbox", ControlType.Edit, true, true, false, true, true), "owned live query editor metadata is supported");
            check(!WordSearchField.Editor("Body", ControlType.Edit, true, true, false, true, true), "body identity cannot inherit query permission");
            check(!WordSearchField.Editor("NetUITextbox", ControlType.Document, true, true, false, true, true), "document provider cannot inherit query permission");
            for (int bit = 0; bit < 5; bit++)
                check(!WordSearchField.Editor("NetUITextbox", ControlType.Edit, bit != 0, bit != 1, bit == 2, bit != 3, bit != 4), "live focus/enabled/password/native/process checks remain required: " + bit);
            int fields = 0, documents = 0;
            Func<object> missingField = () =>
            {
                fields++;
                return null;
            };
            object body = new object ();
            Func<object> document = () =>
            {
                documents++;
                return body;
            };
            check(WordSearchField.Resolve(true, missingField, document) == null && fields == 1 && documents == 0, "missing query provider never falls back to a stale document provider");
            check(WordSearchField.Resolve(false, missingField, document) == body && fields == 1 && documents == 1, "ordinary document/comment focus keeps its established resolver");
            int accessibleReads = 0;
            check(SafeEdit.TextProvider(match(), () =>
            {
                accessibleReads++;
                return body;
            }) == null && accessibleReads == 0, "verified native query coordinates bypass an unrelated accessible text range");
            var queryNodes = new WordPasteOptions.DocumentNode[classes.Length];
            for (int n = 0; n < queryNodes.Length; n++)
                queryNodes[n] = new WordPasteOptions.DocumentNode
                {
                    Process = 41,
                    Type = n == 0 ? ControlType.Edit : ControlType.Pane,
                    Id = "",
                    Handle = new IntPtr(n + 1)
                };
            check(!WordPasteOptions.DocumentPath(queryNodes[0], 41, node => node, node =>
            {
                int at = Array.IndexOf(queryNodes, node);
                return at >= 0 && at + 1 < queryNodes.Length ? queryNodes[at + 1] : null;
            }, (node, info) => false), "query ancestry cannot qualify as Word document focus");
            var documentNode = new WordPasteOptions.DocumentNode
            {
                Process = 41,
                Type = ControlType.Document,
                Id = "Body"
            };
            check(WordPasteOptions.DocumentPath(documentNode, 41, node => node, node => null, (node, info) => false), "ordinary body focus remains available through the unchanged document policy");
            var edit = (SafeEdit)FormatterServices.GetUninitializedObject(typeof(SafeEdit));
            GC.SuppressFinalize(edit);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(SafeEdit).GetField("wordSearch", flags).SetValue(edit, true);
            typeof(SafeEdit).GetField("expected", flags).SetValue(edit, "krsna");
            foreach (string value in new[]
            {
                "kṛṣṇa",
                "kåñëa",
                "कृष्ण",
                "earlier kṛṣṇa",
                ""
            }

            )
                check(edit.AllowsOutput(value), "single-line query output remains supported: " + value);
            foreach (string value in new[]
            {
                "a\nb",
                "a\rb",
                "a\tb",
                "a\0b",
                "a\u001bb",
                "a\u007fb",
                null
            }

            )
                check(!edit.AllowsOutput(value), "query output cannot send a control character or search/navigation action");
            for (int code = 0x80; code <= 0x9f; code++)
                check(!edit.AllowsOutput("a" + (char)code + "b"), "query refuses Unicode C1 control U+" + code.ToString("X4"));
            foreach (char separator in new[]
            {
                '\u2028',
                '\u2029'
            }

            )
                check(!edit.AllowsOutput("a" + separator + "b"), "query refuses Unicode line/paragraph separator U+" + ((int)separator).ToString("X4"));
            foreach (char space in new[]
            {
                '\u00a0',
                '\u2003',
                '\u202f'
            }

            )
                check(edit.AllowsOutput("a" + space + "b"), "ordinary Unicode query spacing remains supported U+" + ((int)space).ToString("X4"));
            typeof(SafeEdit).GetField("wordSearch", flags).SetValue(edit, false);
            check(edit.AllowsOutput("a\nb"), "other surfaces retain their existing multiline-output contract");
            foreach (char separator in new[]
            {
                '\u0085',
                '\u2028',
                '\u2029'
            }

            )
                check(edit.AllowsOutput("a" + separator + "b"), "other surfaces retain their existing Unicode separator output contract");
            typeof(SafeEdit).GetField("wordSearch", flags).SetValue(edit, true);
            string settingsFile = Path.Combine(root, "word-search-contract-settings.ini");
            try
            {
                foreach (string mode in new[]
                {
                    "auto",
                    "unicode",
                    "balaram"
                }

                )
                {
                    string original = "encoding_override=" + mode + "\r\n";
                    File.WriteAllText(settingsFile, original);
                    var settings = new EncodingSettings(settingsFile);
                    check(edit.SelectionEncoding(null, settings) == (mode == "auto" ? "unicode" : mode), "query encoding uses Unicode auto or the explicit override: " + mode);
                    check(File.ReadAllText(settingsFile) == original, "query encoding does not save settings: " + mode);
                }
            }
            finally
            {
                File.Delete(settingsFile);
            }

            Console.WriteLine("Word Navigation query: " + passed + " assertions; owned native path, stale-provider refusal and text-only encoding; no Word or input.");
            return passed;
        }
    }
}
