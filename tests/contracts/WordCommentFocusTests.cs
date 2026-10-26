// Noninteractive provider model: no Word process, desktop focus or installed state.
using System;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class WordCommentFocusTests
    {
        sealed class Element
        {
            public WordCommentFocus.Node Facts;
            public Element Parent;
            public bool Focused = true;
        }

        static Element Make(string id, ControlType type, Element parent = null, string name = "", int handle = 0, int process = 41)
        {
            return new Element
            {
                Parent = parent,
                Facts = new WordCommentFocus.Node
                {
                    Id = id,
                    Type = type,
                    Class = name,
                    Handle = new IntPtr(handle),
                    Process = process
                }
            };
        }

        public static int Run()
        {
            int passed = 0, reads = 0, parents = 0;
            Func<Element, IntPtr> host = element => WordCommentFocus.CommentHost(element, 41, node =>
            {
                reads++;
                return node.Facts;
            }, node =>
            {
                parents++;
                return node.Parent;
            });
            Action<bool, string> check = (valid, message) =>
            {
                if (!valid)
                    throw new Exception(message);
                passed++;
            };
            foreach (string cardId in new[]
            {
                "draftCardRoot_4_new",
                "cardRoot_4_reply"
            }

            )
            {
                var native = Make("", ControlType.Pane, null, "NetUIHWNDElement", 73);
                var card = Make(cardId, ControlType.TreeItem, native);
                var wrapper = Make("", ControlType.Pane, card);
                var editor = Make("cardEditor_4_new", ControlType.Edit, wrapper);
                reads = parents = 0;
                check(host(editor) == new IntPtr(73), "Owned comment/reply host lost");
                check(reads == 4 && parents == 3, "Comment ancestry was read more than once");
                foreach (var node in new[]
                {
                    editor,
                    wrapper,
                    card,
                    native
                }

                )
                {
                    node.Facts.Process = 99;
                    check(host(editor) == IntPtr.Zero, "Mixed-process comment ancestry accepted");
                    node.Facts.Process = 41;
                }

                card.Facts.Type = ControlType.Edit;
                check(host(editor) == IntPtr.Zero, "Card ID without TreeItem accepted");
                card.Facts.Type = ControlType.TreeItem;
                card.Facts.Id = "Body";
                check(host(editor) == IntPtr.Zero, "Unrelated editor accepted as a comment");
                card.Facts.Id = cardId;
                native.Facts.Class = "OtherHost";
                check(host(editor) == IntPtr.Zero, "Unrelated native host accepted");
                native.Facts.Class = "NetUIHWNDElement";
                native.Facts.Handle = IntPtr.Zero;
                check(host(editor) == IntPtr.Zero, "Missing native comment handle accepted");
                native.Facts.Handle = new IntPtr(73);
                // Simulate repeated guards in one shortcut. They must keep
                // checking live scope, while never querying the stale body.
                int normalCalls = 0, focusReads = 0;
                Func<Element, bool> comment = element => element.Focused && host(element) != IntPtr.Zero;
                var body = Make("Body", ControlType.Document);
                body.Focused = false;
                Func<Element> normal = () =>
                {
                    normalCalls++;
                    return body;
                };
                Func<Element, bool> focus = element =>
                {
                    focusReads++;
                    return element.Focused;
                };
                bool modern;
                reads = parents = 0;
                for (int i = 0; i < 30; i++)
                    check(WordCommentFocus.Resolve(editor, comment, normal, focus, out modern) == editor && modern, "Observed comment did not resolve");
                check(normalCalls == 0 && focusReads == 0, "Valid comment queried global body focus");
                check(reads == 120 && parents == 90, "Live ancestry was cached across independent guards");
                editor.Focused = false;
                check(WordCommentFocus.Resolve(editor, comment, normal, focus, out modern) == null && !modern, "Stale comment/body focus accepted");
                check(normalCalls == 1, "Focus loss did not consult the fallback exactly once");
                body.Focused = true;
                check(WordCommentFocus.Resolve(editor, comment, normal, focus, out modern) == body && !modern, "Live body fallback lost");
                editor.Focused = true;
                native.Facts.Process = 99;
                check(WordCommentFocus.Resolve(editor, comment, normal, focus, out modern) == body && !modern, "Changed host process reused a previous ownership proof");
                native.Facts.Process = 41;
                check(WordCommentFocus.Resolve<Element>(null, comment, () => editor, focus, out modern) == editor && modern, "Direct modern-editor fallback lost");
                check(WordCommentFocus.Resolve<Element>(null, comment, () => null, focus, out modern) == null && !modern, "Missing fallback accepted");
            }

            foreach (int depth in new[]
            {
                18,
                19
            }

            )
            {
                var native = Make("", ControlType.Pane, null, "NetUIHWNDElement", 73);
                Element start = native;
                for (int n = 1; n < depth - 1; n++)
                    start = Make("", ControlType.Pane, start);
                start = Make("draftCardRoot_4_depth", ControlType.TreeItem, start);
                reads = parents = 0;
                check((host(start) != IntPtr.Zero) == (depth == 18), "Comment ancestry bound changed");
                check(reads == 18, "Comment ancestry exceeded its provider-read bound");
            }

            var cycle = Make("cardRoot_4_cycle", ControlType.TreeItem);
            cycle.Parent = cycle;
            reads = parents = 0;
            check(host(cycle) == IntPtr.Zero && reads == 18 && parents == 18, "Provider cycle was not bounded");
            check(host(null) == IntPtr.Zero, "Missing editor accepted");
            var wrongOrder = Make("", ControlType.Pane, Make("cardRoot_4_wrong", ControlType.TreeItem), "NetUIHWNDElement", 73);
            check(host(wrongOrder) == IntPtr.Zero, "Host below the comment card accepted");
            Console.WriteLine("Word comment focus: " + passed + " assertions; lazy fallback, fresh ownership, process boundaries and bounded provider traversal.");
            return passed;
        }
    }
}
