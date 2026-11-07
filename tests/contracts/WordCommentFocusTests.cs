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

            // The observed existing-comment tree has a postRoot wrapper inside
            // cardRoot and the same Word-owned NetUI host as a new draft.
            var postedHost = Make("", ControlType.Pane, null, "NetUIHWNDElement", 73);
            var postedCard = Make("cardRoot_4_1272421377", ControlType.TreeItem, postedHost);
            var postedRoot = Make("postRoot_4_1272421377", ControlType.TreeItem, postedCard);
            var posted = Make("postEditor_4_1272421377", ControlType.Edit, postedRoot);
            Func<Element, IntPtr> postedHostFor = element => WordCommentFocus.CommentHost(element, 41, node => node.Facts, node => node.Parent, true);
            check(postedHostFor(posted) == new IntPtr(73), "Existing posted editor rejected by the production editor-ID guard");
            posted.Facts.Type = ControlType.Document;
            check(postedHostFor(posted) == IntPtr.Zero, "Posted editor ID without Edit type accepted");
            posted.Facts.Type = ControlType.Edit;
            postedCard.Facts.Process = 99;
            check(postedHostFor(posted) == IntPtr.Zero, "Posted editor crossed process ancestry");
            postedCard.Facts.Process = 41;
            postedCard.Facts.Id = "UnrelatedCard";
            check(postedHostFor(posted) == IntPtr.Zero, "PostRoot alone authorized an unrelated editor");
            postedCard.Facts.Id = "cardRoot_4_1272421377";
            posted.Focused = false;
            bool postedModern;
            check(WordCommentFocus.Resolve(posted, node => node.Focused && postedHostFor(node) != IntPtr.Zero, () => (Element)null, node => node.Focused, out postedModern) == null, "Stale posted-comment focus authorized conversion");
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
            var bodyNode = Make("Body", ControlType.Document, Make("outer", ControlType.Window));
            reads = parents = 0;
            check(WordCommentFocus.CommentHost(bodyNode, 41, node =>
            {
                reads++;
                return node.Facts;
            }, node =>
            {
                parents++;
                return node.Parent;
            }, true) == IntPtr.Zero, "body is not a Modern Comments editor");
            check(reads == 1 && parents == 0, "ordinary Word body focus does not incur an ancestry scan");
            var fastHost = Make("", ControlType.Pane, null, "NetUIHWNDElement", 73);
            var fastCard = Make("cardRoot_4_fast", ControlType.TreeItem, fastHost);
            var fastEditor = Make("cardEditor_4_fast", ControlType.Edit, fastCard);
            check(WordCommentFocus.CommentHost(fastEditor, 41, node => node.Facts, node => node.Parent, true) == new IntPtr(73), "comment/reply ancestry still validates after the body fast rejection");
            int labels = 0, steps = 0;
            check(!WordPasteGuard.MenuPath(bodyNode, node => node.Facts.Type == ControlType.Document, node =>
            {
                labels++;
                return node.Facts.Id;
            }, node =>
            {
                steps++;
                return node.Parent;
            }), "body focus is not a paste-menu ancestry path");
            check(labels == 0 && steps == 0, "paste detection does not read body labels or walk its ancestors");
            var menu = Make("Paste Options", ControlType.Menu);
            var menuChild = Make("item", ControlType.MenuItem, menu);
            check(WordPasteGuard.MenuPath(menuChild, node => node.Facts.Type == ControlType.Document, node => node.Facts.Id, node => node.Parent), "real paste-menu ancestors remain recognized");
            Func<Element, bool> documentPath = start => WordPasteOptions.DocumentPath(start, 41, node =>
            {
                reads++;
                return new WordPasteOptions.DocumentNode
                {
                    Process = node.Facts.Process,
                    Id = node.Facts.Id,
                    Type = node.Facts.Type,
                    Handle = node.Facts.Handle
                };
            }, node =>
            {
                parents++;
                return node.Parent;
            }, (node, info) => node.Facts.Class == "_WwG" && node.Focused);
            reads = parents = 0;
            check(documentPath(bodyNode) && reads == 1 && parents == 0, "body focus needs one fresh property batch and no parent queries");
            var bodyChild = Make("", ControlType.Text, Make("", ControlType.Edit, bodyNode));
            reads = parents = 0;
            check(documentPath(bodyChild) && reads == 3 && parents == 2, "Text/Edit child retains document ancestry with one read per node");
            bodyChild.Parent.Facts.Process = 42;
            check(!documentPath(bodyChild), "changed ancestry process is rejected on the next check");
            bodyChild.Parent.Facts.Process = 41;
            bodyChild.Parent.Facts.Type = ControlType.Menu;
            check(!documentPath(bodyChild), "menu ancestry cannot inherit body permission");
            bodyChild.Parent.Facts.Type = ControlType.Edit;
            check(!documentPath(Make("Body", ControlType.MenuItem)), "a menu item's Body ID cannot qualify as document focus");
            check(!documentPath(Make("Body", ControlType.Document, null, "", 0, 42)), "foreign document process is rejected");
            var classic = Make("", ControlType.Edit, null, "_WwG", 73);
            check(documentPath(classic), "verified writable classic comment remains accepted");
            classic.Focused = false;
            check(!documentPath(classic), "classic provider that loses writable selection is rejected");
            classic.Focused = true;
            classic.Facts.Class = "Edit";
            check(!documentPath(classic), "unrelated native edit cannot qualify as a classic comment");
            reads = parents = 0;
            check(!documentPath(cycle) && reads == 12, "document ancestry cycles stop at twelve nodes");
            check(!documentPath(null), "missing focus does not qualify as a document");
            var observation = new FocusObservation<Element>();
            var ticket = new FocusSession();
            fastEditor.Focused = false;
            observation.Publish(ticket, fastEditor);
            bool becameModern;
            Func<Element, bool> focusedComment = node => node.Focused && WordCommentFocus.CommentHost(node, 41, value => value.Facts, value => value.Parent, true) != IntPtr.Zero;
            check(WordCommentFocus.Resolve(observation.Read(), focusedComment, () => null, node => node.Focused, out becameModern) == null, "an early comment event is not permission to edit before focus settles");
            fastEditor.Focused = true;
            check(WordCommentFocus.Resolve(observation.Read(), focusedComment, () => null, node => node.Focused, out becameModern) == fastEditor && becameModern, "the observed comment becomes usable after live focus settles without a second event");
            int caretReads = 0;
            Func<Element> caretEditor = () =>
            {
                caretReads++;
                return fastEditor;
            };
            check(WordCommentFocus.CatchUp(() => fastEditor, caretEditor, focusedComment, node => node.Parent) == fastEditor && caretReads == 0, "direct focused comment avoids the optional caret probe");
            var staleBody = Make("Body", ControlType.Document);
            staleBody.Focused = false;
            var caretText = Make("", ControlType.Text, fastEditor);
            check(WordCommentFocus.CatchUp(() => staleBody, () => caretText, focusedComment, node => node.Parent) == fastEditor, "catch-up finds the already-focused comment behind a stale body provider");
            check(WordCommentFocus.CatchUp<Element>(() =>
            {
                throw new InvalidOperationException();
            }, caretEditor, focusedComment, node => node.Parent) == fastEditor, "failed global focus query can still use a verified caret hint");
            fastEditor.Focused = false;
            check(WordCommentFocus.CatchUp(() => staleBody, caretEditor, focusedComment, node => node.Parent) == null, "caret position without live comment focus never becomes evidence");
            fastEditor.Focused = true;
            fastHost.Facts.Process = 99;
            check(WordCommentFocus.CatchUp(() => staleBody, caretEditor, focusedComment, node => node.Parent) == null, "caret hint cannot cross comment host process ownership");
            fastHost.Facts.Process = 41;
            staleBody.Focused = true;
            check(WordCommentFocus.CatchUp(() => staleBody, () => staleBody, focusedComment, node => node.Parent) == null, "focused ordinary body is never seeded as a modern comment");
            check(WordCommentFocus.CatchUp<Element>(() => null, () => null, focusedComment, node => node.Parent) == null, "missing focus and caret hint stay unavailable");
            int hostQueries = 0;
            Func<Element> hostedEditor = () =>
            {
                hostQueries++;
                return WordCommentFocus.UniqueFocused(new[] { staleBody, fastEditor }, focusedComment, () => true);
            };
            check(WordCommentFocus.CatchUp(() => staleBody, () => staleBody, focusedComment, node => node.Parent, hostedEditor) == fastEditor && hostQueries == 1, "owned host recovers a focused comment when both legacy focus and native caret still describe the body");
            check(WordCommentFocus.CatchUp<Element>(() => null, () =>
            {
                throw new InvalidOperationException();
            }, focusedComment, node => node.Parent, hostedEditor) == fastEditor, "failed caret metadata still permits a fully verified owned host");
            hostQueries = 0;
            check(WordCommentFocus.CatchUp(() => fastEditor, () => staleBody, focusedComment, node => node.Parent, hostedEditor) == fastEditor && hostQueries == 0, "direct comment focus avoids host enumeration");
            check(WordCommentFocus.CatchUp(() => staleBody, caretEditor, focusedComment, node => node.Parent, hostedEditor) == fastEditor && hostQueries == 0, "valid caret catch-up avoids host enumeration");
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor, fastEditor }, focusedComment, () => true) == null, "ambiguous host editors are refused");
            check(WordCommentFocus.UniqueFocused(new[] { staleBody }, focusedComment, () => true) == null, "ordinary body is never a hosted comment");
            fastEditor.Focused = false;
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor }, focusedComment, () => true) == null, "host location without live focus is refused");
            fastEditor.Focused = true;
            fastHost.Facts.Process = 99;
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor }, focusedComment, () => true) == null, "host lookup retains full comment process ancestry");
            fastHost.Facts.Process = 41;
            int hostReads = 0;
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor }, value =>
            {
                hostReads++;
                return true;
            }, () => false) == null && hostReads == 0, "stale native focus prevents any host provider inspection");
            bool hostCurrent = true;
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor }, value =>
            {
                hostCurrent = false;
                return true;
            }, () => hostCurrent) == null, "focus change during host inspection rejects its result");
            hostReads = 0;
            check(WordCommentFocus.UniqueFocused(new[] { fastEditor }, value => ++hostReads == 1, () => true) == null && hostReads == 2, "host candidate is revalidated after enumeration");
            var excessive = new Element[65];
            for (int n = 0; n < excessive.Length; n++)
                excessive[n] = staleBody;
            excessive[0] = fastEditor;
            hostReads = 0;
            check(WordCommentFocus.UniqueFocused(excessive, value =>
            {
                hostReads++;
                return focusedComment(value);
            }, () => true) == null && hostReads == 64, "oversized host enumeration is bounded and never returns an early candidate");
            int caretParents = 0;
            check(WordCommentFocus.CatchUp<Element>(() => null, () => cycle, focusedComment, node =>
            {
                caretParents++;
                return node.Parent;
            }) == null && caretParents == 10, "catch-up parent cycles retain the ten-node bound");
            var firstNotice = new WordFocusNotice
            {
                Owner = 1,
                Host = 2,
                Process = 41,
                Editor = "cardEditor_same",
                Runtime = new[]
                {
                    3,
                    4
                },
                Bounds = new[]
                {
                    1.0,
                    2.0,
                    30.0,
                    40.0
                }
            };
            var duplicateNotice = new WordFocusNotice
            {
                Owner = 1,
                Host = 2,
                Process = 41,
                Editor = "cardEditor_same",
                Runtime = new[]
                {
                    3,
                    4
                },
                Bounds = new[]
                {
                    1.0,
                    2.0,
                    30.0,
                    40.0
                }
            };
            check(WordCommentFocus.SameNotice(firstNotice, duplicateNotice), "duplicate focus metadata retains the same locator identity");
            duplicateNotice.Runtime[1] = 5;
            check(!WordCommentFocus.SameNotice(firstNotice, duplicateNotice), "a new runtime ID is a new editor");
            duplicateNotice.Runtime[1] = 4;
            duplicateNotice.Host = 9;
            check(!WordCommentFocus.SameNotice(firstNotice, duplicateNotice), "duplicate identity cannot cross native hosts");
            duplicateNotice.Host = 2;
            duplicateNotice.Bounds[0] = 2;
            check(!WordCommentFocus.SameNotice(firstNotice, duplicateNotice), "moved bounds refresh the locator hint");
            var located = new object ();
            check(ReferenceEquals(WordCommentFocus.Locate(() => located, () =>
            {
                throw new InvalidOperationException("unavailable legacy root");
            }), located), "a verified comment point is independent of an unavailable legacy Word accessibility root");
            int pointReads = 0, treeReads = 0;
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() =>
            {
                pointReads++;
                return null;
            }, () =>
            {
                treeReads++;
                return located;
            }), located) && pointReads == 1 && treeReads == 1, "missing point metadata retains native-root fallback");
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() =>
            {
                throw new InvalidOperationException("unavailable point");
            }, () => located), located), "a failed point lookup can still use independently verified tree metadata");
            var oldTrace = WordCommentFocus.Trace;
            try
            {
                WordCommentFocus.Trace = message =>
                {
                    throw new InvalidOperationException("unavailable diagnostic writer");
                };
                check(ReferenceEquals(WordCommentFocus.Locate<object>(() =>
                {
                    throw new InvalidOperationException("unavailable point");
                }, () => located), located), "failed diagnostic logging cannot suppress a valid fallback");
            }
            finally
            {
                WordCommentFocus.Trace = oldTrace;
            }

            int frameReads = 0;
            Func<object> frameReply = () =>
            {
                frameReads++;
                return located;
            };
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() => null, () => null, frameReply), located) && frameReads == 1, "a live reply present only in the owned frame can be discovered when point and native host omit it");
            frameReads = 0;
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() => null, () =>
            {
                throw new InvalidOperationException("legacy native provider unavailable");
            }, frameReply), located) && frameReads == 1, "an unavailable native provider cannot suppress the independently verified frame reply");
            frameReads = 0;
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() => null, () => located, frameReply), located) && frameReads == 0, "a verified native-host result avoids frame discovery");
            treeReads = frameReads = 0;
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() => located, () =>
            {
                treeReads++;
                return null;
            }, frameReply), located) && treeReads == 0 && frameReads == 0, "a verified point result avoids both provider trees");
            check(WordCommentFocus.Locate<object>(() => null, () => null, () => null) == null, "no provider result remains an unavailable reply");
            var frameCandidate = new object ();
            bool frameCurrent = true, frameFocused = false;
            Func<object> validatedFrame = () => WordCommentFocus.UniqueFocused(new[] { frameCandidate }, item => frameFocused, () => frameCurrent);
            check(WordCommentFocus.Locate<object>(() => null, () => null, validatedFrame) == null, "a frame candidate without live focus cannot replace missing host evidence");
            frameFocused = true;
            frameCurrent = false;
            check(WordCommentFocus.Locate<object>(() => null, () => null, validatedFrame) == null, "frame fallback cannot survive a changed foreground/native focus identity");
            frameCurrent = true;
            check(ReferenceEquals(WordCommentFocus.Locate<object>(() => null, () => null, validatedFrame), frameCandidate), "current live frame metadata can resolve without the legacy body provider");
            Console.WriteLine("Word comment focus: " + passed + " assertions; lazy fallback, fresh ownership, process boundaries and bounded provider traversal.");
            return passed;
        }
    }
}
