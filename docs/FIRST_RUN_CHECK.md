# Five-minute first-run check

Use a new disposable document or scratch text field. Keep working manuscripts
and important clipboard contents out of a test. These are physical typing checks;
pasting cannot replace a test that says **type**.

1. **Launch and tray:** install from Releases, find the white की tray icon, and
   open Cheat sheet. Open Customize shortcuts and Edit triggers, then cancel.
2. **Letters and words:** in a Unicode font, type `a` and press backtick twice:
   `ā` then `a`. Type `raja` and tap Left Ctrl several times: variants such as
   `rāja`, `rājā`, and plain `raja` should cycle. Backspace immediately afterward
   should restore the original word. Type an English word afterward; it should
   keep normal formatting.
3. **English protection and triggers:** select `Earlier an age since raja and Krsna.`
   and press Ctrl+.. Expect `Earlier an age since rāja and Kṛṣṇa.` with English
   unchanged. Type `SP`, then tap Left Ctrl: expect `Śrīla Prabhupāda` in Unicode.
4. **Rapid typing:** physically type `dont stop cant be doesnt work `, including
   the last space, at your usual speed. Apostrophes should appear without lost,
   duplicated, or reordered letters. Repeat after correcting `canx` with Backspace
   and continuing with `t be `.
5. **Scripts and clipboard:** copy a disposable sentinel such as `Clipboard check 42`.
   Select `kṛṣṇa` and press Ctrl+[ for `कृष्ण`; select it and press Ctrl+. to return
   to `kṛṣṇa`. Repeat with Ctrl+] for `কৃষ্ণ`. Paste elsewhere to check that the
   original sentinel remains on the clipboard. Check that font size is preserved.
6. **Word paste and Balarama:** if you use Word, paste `Earlier untouched raja`,
   leave Paste Options visible, and tap Left Ctrl once at the end. Only `raja`
   should convert. If you have a Balarama font, repeat letter/word conversion in
   Auto encoding and check legacy output. Keep earlier text unchanged.
7. **Pause and restart:** pause through the tray and check normal application key
   behavior, then resume. Exit and launch KeyJaya through the Start menu. Later,
   at a convenient time, check its Start with Windows setting at Windows sign-in.

For a miss, record the KeyJaya/app versions, font, encoding choice, exact scratch
input, keys, caret/selection position, and whether typing was rapid or text was
pasted. Use the repository's bug form. Do not include private documents or text.

This quick check complements the [full test plan](TESTING.md). Hosted automated
checks cover logic/build/isolated installation; they cannot validate physical
typing, app-specific controls, Windows sign-in, sleep/wake, or multiple monitors.
