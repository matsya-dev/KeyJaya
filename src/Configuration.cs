// KeyJaya: Typed preferences, strict trigger parsing, atomic saves, and compatibility migration.
// Loading is read-only. Upgrade additions are one-time operations that preserve user edits and deletions.
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Linq;

namespace KeyJaya
{
    /// <summary>
    /// Small atomic updates that retain unrelated preference keys. Callers update
    /// their in-memory setting only after the replacement file has been committed.
    /// </summary>
    public static class SettingsFile
    {
        public static void Commit(string temporary, string path)
        {
            for (int attempt = 0;; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                        File.Replace(temporary, path, null);
                    else
                        File.Move(temporary, path);
                    return;
                }
                catch (IOException)
                {
                    if (attempt >= 4)
                        throw;
                    System.Threading.Thread.Sleep(20 * (attempt + 1));
                }
            }
        }

        public static void Update(string path, params string[] values)
        {
            var keys = new HashSet<string>(values.Select(value => value.Substring(0, value.IndexOf('='))), StringComparer.Ordinal);
            var lines = File.Exists(path) ? File.ReadAllLines(path).Where(line =>
            {
                int at = line.IndexOf('=');
                return !keys.Contains((at < 0 ? line : line.Substring(0, at)).Trim());
            }).ToList() : new List<string>();
            lines.AddRange(values);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllLines(temporary, lines, new UTF8Encoding(false));
                Commit(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }

    public sealed class EncodingSettings
    {
        readonly string path;
        public string Mode { get; private set; }

        public static bool Valid(string mode)
        {
            return mode == "auto" || mode == "unicode" || mode == "balaram";
        }

        public EncodingSettings(string file)
        {
            path = file;
            Mode = "auto";
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] cells = line.Split(new[] { '=' }, 2);
                    if (cells.Length != 2 || cells[0].Trim() != "encoding_override")
                        continue;
                    string mode = cells[1].Trim().ToLowerInvariant();
                    if (!Valid(mode))
                        throw new InvalidDataException("Invalid encoding_override: choose auto, unicode or balaram.");
                    Mode = mode;
                }
        }

        public void Set(string mode)
        {
            if (!Valid(mode))
                throw new ArgumentException("Unknown encoding override");
            SettingsFile.Update(path, "encoding_override=" + mode);
            Mode = mode;
        }

        public string Resolve(FontEncoding fonts, string font, bool mixed)
        {
            return Mode == "auto" ? fonts.Resolve(font, mixed) : Mode;
        }
    }

    public sealed class StartupSettings
    {
        public static bool IsLegacyCommand(string command, string legacyRoot)
        {
            if (command == null || string.IsNullOrEmpty(legacyRoot))
                return false;
            string target = Path.Combine(legacyRoot, "engine", "keyjaya-app.exe");
            string value = Environment.ExpandEnvironmentVariables(command.Trim());
            return string.Equals(value, target, StringComparison.OrdinalIgnoreCase) || string.Equals(value, "\"" + target + "\"", StringComparison.OrdinalIgnoreCase);
        }

        readonly string path;
        public bool Enabled { get; private set; }

        public StartupSettings(string file)
        {
            path = file;
            Enabled = true;
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] cells = line.Split(new[] { '=' }, 2);
                    if (cells.Length == 2 && cells[0].Trim() == "start_with_windows")
                    {
                        bool value;
                        if (!bool.TryParse(cells[1].Trim(), out value))
                            throw new InvalidDataException("Invalid start_with_windows true/false setting");
                        Enabled = value;
                    }
                }
        }

        public void Set(bool enabled)
        {
            SettingsFile.Update(path, "start_with_windows=" + enabled.ToString().ToLowerInvariant());
            Enabled = enabled;
        }
    }

    public sealed class FormattingSettings
    {
        readonly string path;
        public bool Italicize { get; private set; }

        public FormattingSettings(string file)
        {
            path = file;
            Italicize = true;
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] cells = line.Split(new[] { '=' }, 2);
                    if (cells.Length == 2 && cells[0].Trim() == "italicize_conversions")
                    {
                        bool value;
                        if (!bool.TryParse(cells[1].Trim(), out value))
                            throw new InvalidDataException("Invalid italicize_conversions true/false setting");
                        Italicize = value;
                    }
                }
        }

        public void Set(bool enabled)
        {
            SettingsFile.Update(path, "italicize_conversions=" + enabled.ToString().ToLowerInvariant());
            Italicize = enabled;
        }
    }

    public sealed class CorrectionSettings
    {
        readonly string path;
        public bool Spelling { get; private set; }
        public bool Contractions { get; private set; }

        public CorrectionSettings(string file)
        {
            path = file;
            Spelling = Contractions = true;
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] cells = line.Split(new[] { '=' }, 2);
                    if (cells.Length != 2)
                        continue;
                    string key = cells[0].Trim();
                    bool value;
                    if (key != "autocorrect_spelling" && key != "auto_contractions")
                        continue;
                    if (!bool.TryParse(cells[1].Trim(), out value))
                        throw new InvalidDataException("Invalid true/false setting: " + key);
                    if (key == "autocorrect_spelling")
                        Spelling = value;
                    else
                        Contractions = value;
                }
        }

        public void Set(bool spelling, bool contractions)
        {
            SettingsFile.Update(path, "autocorrect_spelling=" + spelling.ToString().ToLowerInvariant(), "auto_contractions=" + contractions.ToString().ToLowerInvariant());
            Spelling = spelling;
            Contractions = contractions;
        }
    }

    public sealed class ScriptSettings
    {
        public string ShortcutMode(string mode)
        {
            return mode == "itrans" && Language == "hindi" ? "hindi" : mode;
        }

        readonly string path;
        public string Language { get; private set; }
        public bool Precise { get; private set; }
        public bool PreserveVerseMarkers { get; private set; }

        public ScriptSettings(string file)
        {
            path = file;
            Language = "sanskrit";
            if (File.Exists(path))
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] cells = line.Split(new[] { '=' }, 2);
                    if (cells.Length != 2)
                        continue;
                    string key = cells[0].Trim();
                    if (key == "script_language")
                    {
                        string language = cells[1].Trim().ToLowerInvariant();
                        if (language != "sanskrit" && language != "hindi")
                            throw new InvalidDataException("Invalid script_language");
                        Language = language;
                        continue;
                    }

                    if (key != "script_precise" && key != "preserve_verse_markers")
                        continue;
                    bool value;
                    if (!bool.TryParse(cells[1].Trim(), out value))
                        throw new InvalidDataException("Invalid true/false setting: " + key);
                    if (key == "script_precise")
                        Precise = value;
                    else
                        PreserveVerseMarkers = value;
                }
        }

        public void SetLanguage(string language)
        {
            if (language != "sanskrit" && language != "hindi")
                throw new ArgumentException("Unknown script language");
            SettingsFile.Update(path, "script_language=" + language);
            Language = language;
        }

        public void Set(bool precise, bool preserve)
        {
            SettingsFile.Update(path, "script_precise=" + precise.ToString().ToLowerInvariant(), "preserve_verse_markers=" + preserve.ToString().ToLowerInvariant());
            Precise = precise;
            PreserveVerseMarkers = preserve;
        }
    }

    // One optional customization file for new installations. Existing tables are
    // read without rewriting them; sections in the new file override the same kind
    // of legacy entry. Ordered replacement rules are deliberately kept in order.
    public sealed class CustomizationTable
    {
        public readonly string Section, Name;
        public readonly string[] Lines;
        public readonly bool OverridesLegacy;
        public CustomizationTable(string section, string name, IEnumerable<string> lines, bool overridesLegacy = false)
        {
            Section = section;
            Name = name;
            Lines = lines.ToArray();
            OverridesLegacy = overridesLegacy;
        }
    }

    /// <summary>
    /// Presents old TSV tables and the sectioned file through one read-only loader.
    /// Legacy ordering is explicit; newer entries override the same kind of rule.
    /// </summary>
    public static class Customizations
    {
        public const string FileName = "customizations.ini";
        static readonly string[] legacyOrder =
        {
            "diacritics-overrides.tsv",
            "overrides.tsv",
            "word-spellings.tsv",
            "script-spellings.tsv",
            "script-phrases.tsv",
            "italics-exceptions.tsv",
            "conversion-rules.tsv"
        };
        static readonly Dictionary<string, string> legacy = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {
                "diacritics-overrides.tsv",
                "aliases"
            },
            {
                "overrides.tsv",
                "aliases"
            },
            {
                "word-spellings.tsv",
                "words"
            },
            {
                "script-spellings.tsv",
                "script-words"
            },
            {
                "script-phrases.tsv",
                "script-phrases"
            },
            {
                "italics-exceptions.tsv",
                "italics"
            },
            {
                "conversion-rules.tsv",
                "replacements"
            }
        };
        public static IEnumerable<CustomizationTable> Read(string root)
        {
            string folder = Configuration.Folder(root);
            if (Directory.Exists(folder))
            {
                foreach (string name in legacyOrder)
                {
                    string path = Path.Combine(folder, name);
                    if (File.Exists(path))
                        yield return new CustomizationTable(legacy[name], path, File.ReadAllLines(path));
                }
            }
            else
            {
                // Preserve the precedence used by installations predating config/.
                foreach (string name in new[]
                {
                    "diacritics-overrides.tsv",
                    "overrides.tsv",
                    "conversion-rules.tsv"
                }

                )
                    foreach (string parent in new[]
                    {
                        root,
                        Path.Combine(root, "personal")
                    }

                    )
                    {
                        string path = Path.Combine(parent, name);
                        if (File.Exists(path))
                            yield return new CustomizationTable(legacy[name], path, File.ReadAllLines(path));
                    }
            }

            string unified = Path.Combine(folder, FileName);
            if (!File.Exists(unified))
                yield break;
            var sections = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            string current = null;
            int number = 0;
            foreach (string line in File.ReadLines(unified))
            {
                number++;
                string text = line.Trim();
                if (text.Length == 0 || text.StartsWith("#"))
                    continue;
                if (text.StartsWith("[") && text.EndsWith("]") && line.TrimStart().IndexOf('\t') < 0)
                {
                    current = text.Substring(1, text.Length - 2);
                    if (!legacy.Values.Contains(current) || sections.ContainsKey(current))
                        throw new InvalidDataException("Unknown or duplicate customization section at line " + number);
                    sections.Add(current, new List<string>());
                }
                else
                {
                    if (current == null)
                        throw new InvalidDataException("Customization row needs a section at line " + number);
                    if (current == "aliases" || current == "replacements")
                    {
                        string[] cells = line.Split('\t');
                        if (cells.Length != 2 || cells[0].Length == 0)
                            throw new InvalidDataException("Customization row needs two Tab-separated columns at line " + number);
                    }

                    sections[current].Add(line);
                }
            }

            foreach (var section in sections)
                yield return new CustomizationTable(section.Key, unified + " [" + section.Key + "]", section.Value, true);
        }
    }

    public static class Configuration
    {
        public static string Factory(string root)
        {
            return Path.Combine(root, "assets", "short-trigger-defaults.yml");
        }

        public static void CreateDefaultTriggers(string root)
        {
            string target = Short(root);
            if (File.Exists(Personal(root)))
                return;
            string factory = Factory(root);
            if (!File.Exists(factory))
                throw new InvalidDataException("Missing factory trigger definitions.");
            Read(factory);
            Directory.CreateDirectory(Folder(root));
            File.Copy(factory, target);
        }

        public static bool UpdateAcbspInitials(string root)
        {
            string path = Short(root);
            if (!File.Exists(path))
                return false;
            var exact = new HashSet<string>(StringComparer.Ordinal);
            var current = Read(path, false, true, exact);
            var previous = new HashSet<string>(new[] { "A. C. Bhaktivedanta Swami Prabhupāda", "A. C. Bhaktivedanta Swami Prabhupäda", "A. C. Bhaktivedanta Swami Prabhupada" }, StringComparer.Ordinal);
            byte[] original = File.ReadAllBytes(path);
            string text = File.ReadAllText(path, Encoding.UTF8);
            var blocks = Regex.Matches(text, @"(?m)^[ \t]*-\s+trigger:\s*([^\r\n]+)");
            var output = new StringBuilder();
            int at = 0;
            bool changed = false;
            for (int n = 0; n < blocks.Count; n++)
            {
                var block = blocks[n];
                int end = n + 1 < blocks.Count ? blocks[n + 1].Index : text.Length;
                output.Append(text.Substring(at, block.Index - at));
                string raw = text.Substring(block.Index, end - block.Index), key = PlainTriggerKey(Decode(block.Groups[1].Value)), value;
                if ((key == "ACBSP" || key == "ACBSP>" || key == "ACBSP<" || key == "ACBSP?") && !exact.Contains(key) && current.TryGetValue(key, out value) && previous.Contains(value))
                {
                    string revised = Regex.Replace(raw, @"(?m)^([ \t]+replace:[ \t]*)([^\r\n]*)", m =>
                    {
                        string scalar = m.Groups[2].Value;
                        int offset = scalar.IndexOf("A. C. Bhaktivedanta Swami Prabhup", StringComparison.Ordinal);
                        return m.Groups[1].Value + (offset < 0 ? scalar : scalar.Remove(offset + 2, 1));
                    });
                    changed |= revised != raw;
                    raw = revised;
                }

                output.Append(raw);
                at = end;
            }

            output.Append(text.Substring(at));
            if (!changed)
                return false;
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N"), backup = Path.Combine(root, "backups", "acbsp-initials-previous.yml");
            bool bom = original.Length >= 3 && original[0] == 239 && original[1] == 187 && original[2] == 191;
            try
            {
                File.WriteAllText(temporary, output.ToString(), new UTF8Encoding(bom));
                Read(temporary, false, true);
                if (!File.ReadAllBytes(path).SequenceEqual(original))
                    throw new IOException("Trigger configuration changed during upgrade.");
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if (!File.Exists(backup))
                    File.WriteAllBytes(backup, original);
                SettingsFile.Commit(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }

            return true;
        }

        // These are deliberately named one-time additions, not a default-list sync.
        // Existing custom/disabled groups and deletions after their marker are authoritative.
        // BB already existed in older factory lists. Its absence cannot establish
        // an upgrade defect rather than a deliberate deletion: restore it only
        // through an explicit user edit, never by creating a new migration marker.
        public static bool AddRequestedShortTriggers(string root)
        {
            bool updated = UpdateAcbspInitials(root);
            string additions = Factory(root);
            if (!File.Exists(additions))
                return updated;
            var exact = new HashSet<string>(StringComparer.Ordinal);
            var requested = Read(additions, false, false, exact);
            foreach (string name in new[]
            {
                "SBB",
                "SSR"
            }

            )
                updated = AddRequestedShortGroup(root, name, requested, exact) || updated;
            return updated;
        }

        static bool ShortGroupKey(string key, string name)
        {
            return key == name || key == name + ">" || key == name + "<" || key == name + "?";
        }

        static bool AddRequestedShortGroup(string root, string name, Dictionary<string, string> requested, HashSet<string> exact)
        {
            string marker = Path.Combine(root, "state", name.ToLowerInvariant() + "-trigger-added.flag");
            if (File.Exists(marker))
                return false;
            var rows = requested.Where(row => ShortGroupKey(row.Key, name)).ToArray();
            if (rows.Length == 0)
                return false;
            string path = Short(root);
            var current = Read(path, false, true);
            bool changed = !current.Keys.Any(key => ShortGroupKey(key, name));
            // An existing or disabled abbreviation is the user's configuration. Do not
            // introduce missing forms that could override its custom Ctrl replacement.
            if (changed)
            {
                byte[] original = File.ReadAllBytes(path);
                bool bom = original.Length >= 3 && original[0] == 239 && original[1] == 187 && original[2] == 191;
                string raw = File.ReadAllText(path, Encoding.UTF8), newline = raw.Contains("\r\n") ? "\r\n" : "\n";
                var text = new StringBuilder(Regex.Replace(raw, @"(?m)^([ \t]*matches:)[ \t]*\[\]([ \t]*\r?)$", "$1$2"));
                var json = new JavaScriptSerializer();
                text.Append(newline);
                foreach (var row in rows)
                {
                    text.Append("  - trigger: " + json.Serialize(row.Key) + newline + "    replace: " + json.Serialize(row.Value) + newline);
                    if (exact.Contains(row.Key))
                        text.Append("    keep_exact: true" + newline);
                }

                string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(bom));
                    Read(temporary, false, true);
                    if (!File.ReadAllBytes(path).SequenceEqual(original))
                        throw new IOException("Trigger configuration changed during upgrade.");
                    SettingsFile.Commit(temporary, path);
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(marker));
            File.WriteAllText(marker, "Explicit " + name + " addition applied; subsequent deletions stay respected.\n");
            return changed;
        }

        public const string PersonalFile = "personal-triggers.yml";
        public const string ShortFile = "short-triggers.yml";
        public static string Folder(string root)
        {
            return Path.Combine(root, "config");
        }

        public static string Short(string root)
        {
            return Path.Combine(Folder(root), ShortFile);
        }

        public static string Personal(string root)
        {
            string canonical = Short(root);
            return File.Exists(canonical) ? canonical : Path.Combine(Folder(root), PersonalFile);
        }

        public static string Decode(string value)
        {
            value = value.Trim();
            if (value.StartsWith("\""))
            {
                try
                {
                    return new JavaScriptSerializer().Deserialize<string>(value);
                }
                catch (Exception)
                {
                    throw new InvalidDataException("Invalid quoted text: " + value);
                }
            }

            if (value.StartsWith("'"))
            {
                if (value.Length < 2 || !value.EndsWith("'"))
                    throw new InvalidDataException("Unclosed quoted text");
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }

            if (value.Contains(" #"))
                value = value.Substring(0, value.IndexOf(" #", StringComparison.Ordinal));
            return value;
        }

        public static string PlainTriggerKey(string key)
        {
            return key != null && key.Length > 1 && key.EndsWith(":", StringComparison.Ordinal) && !key.EndsWith("::", StringComparison.Ordinal) ? key.Substring(0, key.Length - 1) + "?" : key;
        }

        public static bool RemapPlainFile(string path, string backup)
        {
            if (!File.Exists(path))
                return false;
            Read(path);
            string original = File.ReadAllText(path, Encoding.UTF8);
            bool changed = false;
            JavaScriptSerializer json = new JavaScriptSerializer();
            string updated = Regex.Replace(original, @"(?m)^(\s*-\s+trigger:\s*)([^\r\n]+)", m =>
            {
                string key = Decode(m.Groups[2].Value), mapped = PlainTriggerKey(key);
                if (mapped == key)
                    return m.Value;
                changed = true;
                return m.Groups[1].Value + json.Serialize(mapped);
            });
            if (!changed)
                return false;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup))
                File.Copy(path, backup);
            File.WriteAllText(path, updated, new UTF8Encoding(false));
            return true;
        }

        /// <summary>
        /// Parses the supported YAML subset without evaluating scripts or shells.
        /// Include disabled rows for editing/migration; runtime lookup excludes them.
        /// Normalizing legacy suffix keys also exposes duplicate-key collisions.
        /// </summary>
        public static Dictionary<string, string> Read(string path, bool import = false, bool includeDisabled = false, HashSet<string> exactKeys = null)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(path))
                return result;
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            bool header = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i], trim = line.Trim();
                if (trim == "" || trim.StartsWith("#"))
                    continue;
                if (trim == "matches:" || trim == "matches: []")
                {
                    if (header)
                        throw new InvalidDataException("Duplicate matches header: " + path);
                    header = true;
                    continue;
                }

                Match trigger = Regex.Match(line, @"^\s*-\s+trigger:\s*(.+)$");
                if (!header || !trigger.Success)
                    throw new InvalidDataException("Unsupported trigger configuration at " + path + ":" + (i + 1));
                string key = Decode(trigger.Groups[1].Value);
                if (key.Length < 1 || key.Length > 160 || key.Any(char.IsWhiteSpace))
                    throw new InvalidDataException("Triggers must be one token of 1–160 characters: " + key);
                List<string> block = new List<string>();
                while (i + 1 < lines.Length && !Regex.IsMatch(lines[i + 1], @"^\s*-\s+trigger:"))
                    block.Add(lines[++i]);
                string replacement = null;
                bool variables = false, enabled = true, keepExact = false;
                var options = new HashSet<string>(StringComparer.Ordinal);
                List<string> variableLines = new List<string>();
                for (int j = 0; j < block.Count; j++)
                {
                    string entry = block[j], content = entry.Trim();
                    if (content == "" || content.StartsWith("#"))
                        continue;
                    Match option = Regex.Match(content, @"^(enabled|keep_exact):\s*(.*?)\s*$");
                    if (option.Success)
                    {
                        bool value;
                        if (!options.Add(option.Groups[1].Value) || !bool.TryParse(option.Groups[2].Value, out value))
                            throw new InvalidDataException("Invalid trigger option for " + key);
                        if (option.Groups[1].Value == "enabled")
                            enabled = value;
                        else
                            keepExact = value;
                        continue;
                    }

                    if (content == "vars:")
                    {
                        variables = true;
                        variableLines.AddRange(block.Skip(j + 1));
                        break;
                    }

                    Match replace = Regex.Match(entry, @"^\s+replace:\s*(.*)$");
                    if (!replace.Success || replacement != null)
                        throw new InvalidDataException("Only trigger and replace are supported for " + key);
                    string scalar = replace.Groups[1].Value;
                    if (scalar == "|" || scalar == "|-" || scalar == "|+" || scalar == ">" || scalar == ">-")
                    {
                        List<string> body = new List<string>();
                        int indentation = -1;
                        while (j + 1 < block.Count)
                        {
                            string next = block[j + 1];
                            if (next.Trim().Length == 0)
                            {
                                body.Add("");
                                j++;
                                continue;
                            }

                            int spaces = next.Length - next.TrimStart(' ').Length;
                            if (indentation < 0)
                                indentation = spaces;
                            if (spaces < indentation)
                                break;
                            body.Add(next.Substring(indentation));
                            j++;
                        }

                        replacement = string.Join(scalar.StartsWith(">") ? " " : "\n", body);
                        if (!scalar.EndsWith("-"))
                            replacement += "\n";
                    }
                    else
                        replacement = Decode(scalar);
                }

                if (replacement == null)
                    throw new InvalidDataException("Missing replace for " + key);
                if (variables)
                {
                    if (!import)
                        throw new InvalidDataException("Use {{date:FORMAT}} directly in replace; vars are only accepted during import.");
                    replacement = ImportVariables(replacement, variableLines, key);
                }

                ValidateTemplate(replacement);
                key = PlainTriggerKey(key);
                if (!seen.Add(key))
                    throw new InvalidDataException("Duplicate trigger: " + key);
                if (enabled || includeDisabled)
                    result.Add(key, replacement);
                if ((enabled || includeDisabled) && keepExact && exactKeys != null)
                    exactKeys.Add(key);
            }

            if (!header)
                throw new InvalidDataException("Missing matches header: " + path);
            return result;
        }

        static string ImportVariables(string replacement, List<string> lines, string key)
        {
            string text = string.Join("\n", lines);
            Match name = Regex.Match(text, @"(?m)^\s*-\s*name:\s*([^\r\n]+)$"), type = Regex.Match(text, @"(?m)^\s*type:\s*([^\r\n]+)$");
            if (!name.Success || !type.Success || Regex.Matches(text, @"(?m)^\s*-\s*name:").Count != 1)
                throw new InvalidDataException("Unsupported variables for " + key);
            string n = Decode(name.Groups[1].Value), kind = Decode(type.Groups[1].Value);
            if (replacement != "{{" + n + "}}")
                throw new InvalidDataException("Unsupported dynamic replacement for " + key);
            if (kind == "date")
            {
                Match format = Regex.Match(text, @"(?m)^\s*format:\s*([^\r\n]+)$");
                if (!format.Success)
                    throw new InvalidDataException("Missing date format: " + key);
                foreach (string line in lines)
                {
                    string t = line.Trim();
                    if (t == "" || t.StartsWith("#") || Regex.IsMatch(t, @"^(-\s*name:|type:|params:|format:)"))
                        continue;
                    throw new InvalidDataException("Unsupported date parameter: " + key);
                }

                return "{{date:" + Decode(format.Groups[1].Value) + "}}";
            }

            if (kind == "shell")
            {
                Match command = Regex.Match(text, @"(?m)^\s*cmd:\s*([^\r\n]+)$");
                if (command.Success && Decode(command.Groups[1].Value) == "echo 'Hello from your shell'" && key == ":shell")
                    return "Hello from your shell";
            }

            throw new InvalidDataException("This dynamic trigger must be migrated explicitly before replacing Espanso: " + key);
        }

        public static string Date(string format, DateTime now)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] != '%')
                {
                    result.Append(format[i]);
                    continue;
                }

                if (++i >= format.Length)
                    throw new InvalidDataException("Incomplete date code");
                string spec;
                switch (format[i])
                {
                    case 'Y':
                        spec = "yyyy";
                        break;
                    case 'y':
                        spec = "yy";
                        break;
                    case 'm':
                        spec = "MM";
                        break;
                    case 'd':
                        spec = "dd";
                        break;
                    case 'e':
                        spec = "d ";
                        break;
                    case 'H':
                        spec = "HH";
                        break;
                    case 'I':
                        spec = "hh";
                        break;
                    case 'M':
                        spec = "mm";
                        break;
                    case 'S':
                        spec = "ss";
                        break;
                    case 'p':
                        spec = "tt";
                        break;
                    case 'b':
                        spec = "MMM";
                        break;
                    case 'B':
                        spec = "MMMM";
                        break;
                    case 'a':
                        spec = "ddd";
                        break;
                    case 'A':
                        spec = "dddd";
                        break;
                    case '%':
                        result.Append('%');
                        continue;
                    default:
                        throw new InvalidDataException("Unsupported date code: %" + format[i]);
                }

                result.Append(now.ToString(spec, CultureInfo.InvariantCulture).TrimEnd());
            }

            return result.ToString();
        }

        public static string Expand(string text, DateTime now)
        {
            return Regex.Replace(text, @"\{\{date:([^{}]+)\}\}", m => Date(m.Groups[1].Value, now));
        }

        static void ValidateTemplate(string text)
        {
            string expanded = Expand(text, new DateTime(2026, 10, 11, 13, 14, 15));
            if (expanded.Contains("{{") || expanded.Contains("}}"))
                throw new InvalidDataException("Unsupported placeholder; use literal text or {{date:FORMAT}}.");
        }

        public static void Write(string path, Dictionary<string, string> rows)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            StringBuilder text = new StringBuilder("# EDIT THIS FILE to add, edit, or delete KeyJaya short triggers.\n# Save, then choose Reload configuration in the tray menu.\n# All active short triggers are listed here; deleting an entry disables it.\n# Example:\n#   - trigger: ':hello'\n#     replace: 'Hello!'\n# Multiline replacement example:\n#   - trigger: ':sign'\n#     replace: |-\n#       Best wishes,\n#       Your name\n# Date example: replace: '{{date:%Y-%m-%d}}'\n\n");
            text.Append(rows.Count == 0 ? "matches: []\n" : "matches:\n");
            JavaScriptSerializer json = new JavaScriptSerializer();
            foreach (var row in rows.OrderBy(x => x.Key, StringComparer.Ordinal))
                text.Append("  - trigger: " + json.Serialize(row.Key) + "\n    replace: " + json.Serialize(row.Value) + "\n");
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        public static void Merge(Dictionary<string, string> target, Dictionary<string, string> source, bool overrideExisting)
        {
            foreach (var row in source)
            {
                string old;
                if (target.TryGetValue(row.Key, out old) && old != row.Value && !overrideExisting)
                    throw new InvalidDataException("Conflicting imported trigger: " + row.Key);
                target[row.Key] = row.Value;
            }
        }
    }
}
