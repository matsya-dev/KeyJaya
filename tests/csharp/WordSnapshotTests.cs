// Noninteractive regressions: Word edit/format snapshots, revision safety, and restoration models.
// Run: .\test.ps1 -Suite WordSnapshotTests. Fixtures live under out/; no resident hooks are started.
using System;

namespace KeyJaya
{
    public static class WordSnapshotTests
    {
        public static int Main()
        {
            int passed = 0;
            foreach (string host in new[]
            {
                "_WwG",
                "NetUIHWND",
                "Edit",
                "RichEdit60W",
                "Chrome_RenderWidgetHostHWND",
                "OpusApp",
                "",
                null
            }

            )
            {
                if (WordCommentFocus.HostClass(host) != (host == "_WwG" || host == "NetUIHWND"))
                    throw new Exception("Modern comment host boundary changed");
                passed++;
            }

            foreach (string id in new[]
            {
                "cardEditor_4_025af5b1-b25b-41c0-84dc-c3332098b9e3",
                "cardEditor_4_1379485682"
            }

            )
            {
                if (!WordCommentFocus.EditorId(id))
                    throw new Exception("Modern comment/reply editor rejected");
                passed++;
            }

            foreach (string id in new[]
            {
                null,
                "",
                "cardEditor_",
                "CardEditor_4_1",
                "Find",
                "Body",
                "draftCancelButton_4_1"
            }

            )
            {
                if (WordCommentFocus.EditorId(id))
                    throw new Exception("Unrelated modern-comment control accepted as editor");
                passed++;
            }

            foreach (string id in new[]
            {
                "draftCardRoot_4_025af5b1-b25b-41c0-84dc-c3332098b9e3",
                "cardRoot_4_1379485682"
            }

            )
            {
                if (!WordCommentFocus.CardId(id))
                    throw new Exception("Modern comment/reply card ancestor rejected");
                passed++;
            }

            foreach (string id in new[]
            {
                null,
                "",
                "draftCardRoot_",
                "cardRoot_",
                "CardRoot_4_1",
                "cardEditor_4_1",
                "Body"
            }

            )
            {
                if (WordCommentFocus.CardId(id))
                    throw new Exception("Unrelated control accepted as comment-card ancestry");
                passed++;
            }

            var originalMap = WordSelection.VisibleMap.Create("raja", 0, 4, new int[0], new int[0]);
            var insertedA = WordSelection.VisibleMap.Create("ā", 1, 2, new int[0], new int[0]);
            var editedMap = originalMap.Edited(1, 1, insertedA, 1);
            if (editedMap == null || editedMap.Value != "rāja" || editedMap.End != 5 || editedMap.NativeOffset(2) != 3 || !editedMap.MatchesRaw("rāaja") || !editedMap.MatchesRaw("rāja") || editedMap.MatchesRaw("other"))
                throw new Exception("Owned tracked edit projection lost");
            passed++;
            var lastA = WordSelection.VisibleMap.Create("ā", 4, 5, new int[0], new int[0]);
            var twiceEdited = editedMap.Edited(3, 1, lastA, 1);
            if (twiceEdited == null || twiceEdited.Value != "rājā" || twiceEdited.End != 6 || !twiceEdited.MatchesRaw("rāajāa"))
                throw new Exception("Multiple owned revisions lost");
            passed++;
            if (originalMap.Edited(1, 1, insertedA, -10) != null)
                throw new Exception("Invalid native growth accepted");
            passed++;
            var verseSpans = WordSelection.MergeVerseSpans("hari nāma and śāstra", new[] { new ItalicSpan(0, 4, true), new ItalicSpan(5, 4, true), new ItalicSpan(14, 6) });
            if (verseSpans.Length != 2 || verseSpans[0].Start != 0 || verseSpans[0].Length != 9 || !verseSpans[0].Force || verseSpans[1].Start != 14)
                throw new Exception("Verse grouping crossed regular prose");
            passed++;
            var plainSpans = WordSelection.MergeVerseSpans("śāstra śikhā", new[] { new ItalicSpan(0, 6), new ItalicSpan(7, 5) });
            if (plainSpans.Length != 2)
                throw new Exception("Prose formatting lost per-word protections");
            passed++;
            foreach (string raw in new[]
            {
                "rāja",
                "rāaja"
            }

            )
            {
                var map = WordSelection.VisibleMap.Create(raw, 0, 5, new[] { 2 }, new[] { 3 });
                if (map == null || map.Value != "rāja" || map.NativeOffset(2) != 3 || map.NativeOffset(4) != 5)
                    throw new Exception("Tracked Word text positions lost");
                passed++;
                var spans = map.Spans(1, 3);
                if (spans.Length != 2 || spans[0][0] != 1 || spans[0][1] != 2 || spans[1][0] != 3 || spans[1][1] != 5)
                    throw new Exception("Formatting crossed tracked deletion");
                passed++;
            }

            var plainMap = WordSelection.VisibleMap.Create("rāja", 18, 22, new int[0], new int[0]);
            if (plainMap == null || plainMap.NativeOffset(2) != 20)
                throw new Exception("Untracked positions changed");
            passed++;
            if (WordSelection.VisibleMap.Create("unrelated", 0, 5, new[] { 2 }, new[] { 3 }) != null)
                throw new Exception("Unverified tracked projection accepted");
            passed++;
            // No first-document special case: later paragraph characters have nonzero offsets.
            foreach (int begin in new[]
            {
                0,
                1,
                19,
                250,
                4096
            }

            )
            {
                if (!WordSelection.OwnedTextAnchor.Positions(begin + 1, begin + 1, begin, true))
                    throw new Exception("Letter caret rejected");
                passed++;
                if (!WordSelection.OwnedTextAnchor.Positions(begin, begin + 1, begin, false))
                    throw new Exception("Exact letter rejected");
                passed++;
                foreach (var wrong in new[]
                {
                    new[]
                    {
                        begin,
                        begin
                    },
                    new[]
                    {
                        begin - 1,
                        begin + 1
                    },
                    new[]
                    {
                        begin,
                        begin + 2
                    },
                    new[]
                    {
                        begin + 1,
                        begin + 2
                    }
                }

                )
                {
                    if (WordSelection.OwnedTextAnchor.Positions(wrong[0], wrong[1], begin, false))
                        throw new Exception("Neighbor or paragraph boundary accepted");
                    passed++;
                }
            }

            if (WordSelection.OwnedTextAnchor.Positions(0, 0, -1, true))
                throw new Exception("Negative owned offset accepted");
            passed++;
            foreach (string token in new[]
            {
                "doesnt ",
                "Doesnt ",
                "cant!",
                "Lets ",
                "shouldnt\n",
                "didnt\t"
            }

            )
                foreach (int begin in new[]
                {
                    0,
                    12,
                    200,
                    4096
                }

                )
                {
                    int end = begin + token.Length;
                    if (!WordSelection.OwnedTextAnchor.Positions(end, end, begin, true, token.Length) || !WordSelection.OwnedTextAnchor.Positions(begin, end, begin, false, token.Length))
                        throw new Exception("Completed contraction range rejected");
                    passed++;
                    if (WordSelection.OwnedTextAnchor.Positions(begin - 1, end, begin, false, token.Length) || WordSelection.OwnedTextAnchor.Positions(begin, end + 1, begin, false, token.Length) || WordSelection.OwnedTextAnchor.Positions(end + 1, end + 1, begin, true, token.Length))
                        throw new Exception("Contraction range crossed a boundary");
                    passed++;
                }

            foreach (string abbreviation in new[]
            {
                "SP",
                "HT",
                "SBB",
                "SPG",
                "BG1.1V",
                "BG1.1T",
                "BG1.1VT"
            }

            )
                foreach (char suffix in new[]
                {
                    '<',
                    '>',
                    '?'
                }

                )
                    foreach (int begin in new[]
                    {
                        0,
                        12,
                        200,
                        4096
                    }

                    )
                    {
                        string trigger = abbreviation + suffix;
                        int end = begin + trigger.Length;
                        if (!WordSelection.OwnedTextAnchor.Positions(end, end, begin, true, trigger.Length) || !WordSelection.OwnedTextAnchor.Positions(begin, end, begin, false, trigger.Length))
                            throw new Exception("Paragraph-start short trigger rejected");
                        passed++;
                        if (WordSelection.OwnedTextAnchor.Positions(begin - 1, end, begin, false, trigger.Length) || WordSelection.OwnedTextAnchor.Positions(begin, end + 1, begin, false, trigger.Length))
                            throw new Exception("Short trigger selected preceding or following text");
                        passed++;
                    }

            for (int story = 1; story <= 17; story++)
            {
                bool wanted = story >= 1 && story <= 4 || story >= 6 && story <= 11;
                if (WordSelection.SupportedStory(story) != wanted)
                    throw new Exception("Unsupported Word story admitted");
                passed++;
            }

            object document = new object ();
            var snapshot = new WordSelection.FontSnapshot(document, 5, 1, "raja");
            snapshot.Fonts.Add(new object ());
            if (!snapshot.Matches(document, 5, 1, "raja"))
                throw new Exception("Original ownership lost");
            passed++;
            if (snapshot.Matches(new object (), 5, 1, "raja") || snapshot.Matches(document, 6, 1, "raja") || snapshot.Matches(document, 5, 2, "raja") || snapshot.Matches(document, 5, 1, "rāja"))
                throw new Exception("Wrong document, range, story or text accepted");
            passed++;
            object[] leased = snapshot.Acquire();
            snapshot.Dispose();
            if (leased.Length != 1 || snapshot.Fonts.Count != 1)
                throw new Exception("Active font lease released prematurely");
            passed++;
            snapshot.Release();
            if (snapshot.Fonts.Count != 0 || snapshot.Matches(document, 5, 1, "raja"))
                throw new Exception("Expired snapshot retained");
            passed++;
            try
            {
                snapshot.Acquire();
                throw new Exception("Expired font lease accepted");
            }
            catch (InvalidOperationException)
            {
                passed++;
            }

            snapshot.Dispose();
            snapshot.Dispose();
            if (snapshot.Fonts.Count != 0)
                throw new Exception("Repeated snapshot disposal retained fonts");
            passed++;
            var twice = new WordSelection.FontSnapshot(document, 5, 1, "raja");
            twice.Fonts.Add(new object ());
            twice.Dispose();
            twice.Dispose();
            if (twice.Fonts.Count != 0 || twice.Matches(document, 5, 1, "raja"))
                throw new Exception("Repeated immediate disposal retained ownership");
            passed++;
            passed += WordApplicationTests.Run();
            passed += WordCommentFocusTests.Run();
            passed += WordNavigationTests.Run();
            passed += WordTransactionReuseTests.Run();
            passed += WordFormattingCycleTests.Run();
            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
