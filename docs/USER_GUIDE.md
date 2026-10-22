# KeyJaya

KeyJaya helps you write Sanskrit and Bengali, add diacritics, and convert text between writing systems. It runs in the Windows system tray.

## Main features

- **Add diacritics:** convert a letter, word, or selected passage using your keyboard.
- **Use your preferred fonts:** work with Unicode or Balarama / legacy fonts.
- **Convert text:** switch between IAST, Balarama, plain Roman text, ITRANS, Devanagari, and Bengali script.
- **Expand abbreviations and verses:** insert frequently used names, phrases, and scripture passages.
- **Manage triggers:** add, edit, disable, or delete abbreviations in a built-in editor.
- **Correct typing:** automatically fix common spelling mistakes and add apostrophes to contractions.
- **Apply italics:** format eligible Sanskrit/Bengali terms while keeping English, proper names, and editorial exceptions regular.
- **Spell out numbers:** turn a targeted number such as `24` into `twenty-four`.

## Get started

1. Run the **KeyJaya Setup** installer. You can choose whether to create a desktop shortcut.
2. Find the white **की** icon in the Windows system tray. It may be under the hidden-icons arrow.
3. Click **Open Cheat sheet** when setup finishes, or right-click the tray icon and choose **Cheat sheet**.

KeyJaya starts after installation and when you sign in to Windows. You can turn off **Start with Windows** in the tray menu.

To open it later, search for **KeyJaya** in the Start menu or Flow Launcher with Start Menu indexing enabled. Updates preserve your settings, custom triggers, and desktop-shortcut choice.

## Try it

These examples show Unicode output. With **Encoding: Auto (detect font)**, a Balarama font produces the corresponding legacy characters.

| What to do | Result |
| --- | --- |
| Type `a`, then press **backtick** | `ā` |
| Type `Krsna`, then tap Left Ctrl by itself | `Kṛṣṇa` |
| Type `God/Krsna`, then tap Left Ctrl | `God/Kṛṣṇa` |
| Type `HT`, then tap Left Ctrl | `Haridāsa Ṭhākura` |
| Type `doesnt` followed by a space | `doesn’t` |
| Type `24`, then tap Left Ctrl | `twenty-four` |
| Type `24th`, then tap Left Ctrl | `twenty-fourth` |
| Type `BG1.1V>` | Inserts the Bhagavad-gītā 1.1 verse |

**Backtick changes only the letter immediately before the caret.** Press it repeatedly to cycle through that letter’s options.

**Left Ctrl converts the word at the caret.** It also works inside an existing word. Repeated taps cycle through available spellings and plain text. At the end of a hyphenated compound it converts the whole compound; inside it, it targets the current part. Normal Ctrl+C and Ctrl+V still work.

You can also select a passage and tap Left Ctrl to convert recognized Sanskrit/Bengali words. Ordinary English words and numbers in the passage stay unchanged. To spell out a number, target only that number with the caret or select only the number.

Press **Backspace immediately after a manual conversion** to restore its original text. After cycling, this restores the text from before the cycle. Use **Ctrl+Z** to undo a selected-text conversion. Automatic spelling and contraction corrections use normal Backspace behavior.

## Convert selected or copied text

Select text and press a shortcut. Copying first is optional. With no selection, KeyJaya uses your copied text.

| Default shortcut | Output |
| --- | --- |
| `Ctrl+.` | Unicode / IAST, such as `kṛṣṇa` |
| `Ctrl+,` | Balarama / legacy text, such as `kåñëa` |
| `Ctrl+/` | Plain Roman text, such as `krsna` |
| `Ctrl+\` | ITRANS, such as `kRShNa`; choose Hindi in the tray for Hindi Devanagari instead |
| `Ctrl+[` | Sanskrit Devanagari, such as `कृष्ण` |
| `Ctrl+]` | Bengali script, such as `কৃষ্ণ` |

Ctrl+. also recognizes copied Devanagari and Bengali text and converts it to IAST. Your clipboard is preserved after conversion.

## Other ways to convert

Add an ending while typing: `krsna>` gives Unicode, `krsna<` gives Balarama, and `krsna?` gives plain Roman text. The cheat sheet lists the available short triggers and scripture formats.

## Useful settings

Choose **Edit triggers…** in the tray menu or at the bottom of the cheat sheet. Search for an abbreviation such as SP, K, or ISKCON, then add, edit, or delete it. Unicode and pasted Balarama replacements are supported; other output forms are generated automatically, with optional overrides. Use **Keep replacement exactly as entered** for fixed wording. Uncheck an entry to disable it temporarily, or **Restore built-in** to restore only that entry’s original replacement.

Click **Save and apply** to activate edits immediately. **Cancel** discards unsaved changes. Deleting an abbreviation removes all its encoding forms together. A backup of the previous trigger file is kept automatically.

Right-click the tray icon to change these options:

The tray menu follows Windows light, dark, and high-contrast settings and scales to the monitor where you open it. The cheat sheet, trigger editor, and shortcut editor follow the same theme.

- **Encoding:** keep Auto to follow the font, or choose Unicode / IAST or Balarama / Legacy yourself.
- **Auto-italicize conversions:** enabled by default; turn it off whenever you prefer. Devanagari, Bengali script, and ITRANS outputs do not gain automatic italics. Plain-text fields remain plain; supported Markdown editing uses italic markers.
- **Autocorrect spelling / Automatic contractions:** turn either feature on or off.
- **Customize shortcuts:** change any of the eight conversion actions, or restore their defaults.
- **Pause KeyJaya:** temporarily return all keys to the app’s normal behavior.

If a conversion does nothing, check that KeyJaya is running and unpaused, and that the caret is in editable text. For word conversion, place it inside the word or directly after the word or its punctuation, with no trailing space. If the characters look wrong, check the Encoding choice. Open **Cheat sheet** whenever you need a reminder.
