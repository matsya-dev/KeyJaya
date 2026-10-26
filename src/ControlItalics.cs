// KeyJaya: Explicit Ctrl-word styling is separate from passage/editorial policy.
// Formatting-only choices keep a distinct original-formatting cycle position.
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace KeyJaya
{
    public sealed partial class Core
    {
        public static bool SingleControlWord(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length > 321)
                return false;
            var words = Compounds.Matches(input);
            if (words.Count != 1)
                return false;
            var word = words[0];
            string outside = input.Substring(0, word.Index) + input.Substring(word.Index + word.Length);
            return outside.All(c => char.IsWhiteSpace(c) || char.IsPunctuation(c) && "`[]<>{}\\".IndexOf(c) < 0) && !outside.Any(char.IsDigit);
        }

        bool ControlName(string word, string mode)
        {
            string key = IndicStyle.Key(word, mode);
            if (key.EndsWith("'s", StringComparison.Ordinal) || key.EndsWith("’s", StringComparison.Ordinal))
                key = key.Substring(0, key.Length - 2);
            if (IndicStyle.ProperName(word, mode, bbtRomanNames))
                return true;
            if (key.EndsWith("s", StringComparison.Ordinal) && key.Length > 1 && IndicStyle.ProperName(key.Substring(0, key.Length - 1), "unicode", bbtRomanNames))
                return true;
            return key == "gaura-purnima" || key == "janmastami" || key == "ratha-yatra" || key == "kali-yuga";
        }

        // Known editorial loanwords are explicit Sanskrit/Bengali exceptions to
        // the ordinary English guard; arbitrary English homographs stay protected.
        public ItalicSpan[] ControlItalics(string token, string output, string mode, bool individual = false, string context = null)
        {
            if (output == null || output.Length > 200000 || !AutomaticItalicMode(mode))
                return new ItalicSpan[0];
            if (token != null && CtrlExpansion(token, mode) != null)
                return CtrlItalics(token, output, mode);
            var baseSpans = ItalicWords(output, mode);
            int[] styles = new int[output.Length];
            bool[] forced = new bool[output.Length];
            foreach (var span in baseSpans)
                for (int n = span.Start; n < span.Start + span.Length; n++)
                {
                    styles[n] = 1;
                    forced[n] = span.Force || individual;
                }

            var names = IndicStyle.NameContext(output, mode, value => RecognizedIndic(value, mode));
            bool contextualName = false;
            if (individual && !string.IsNullOrEmpty(context) && !string.IsNullOrEmpty(token) && context.EndsWith(token, StringComparison.Ordinal))
            {
                string updated = context.Substring(0, context.Length - token.Length) + output;
                contextualName = IndicStyle.NameContext(updated, mode, value => RecognizedIndic(value, mode)).Contains(updated.Length - output.Length);
            }

            var compounds = Compounds.Matches(output);
            var literals = Protected.Matches(output);
            int literalIndex = 0;
            for (int index = 0; index < compounds.Count; index++)
            {
                Match word = compounds[index];
                while (literalIndex < literals.Count && literals[literalIndex].Index + literals[literalIndex].Length <= word.Index)
                    literalIndex++;
                if (literalIndex < literals.Count && literals[literalIndex].Index < word.Index + word.Length)
                    continue;
                string key = IndicStyle.Key(word.Value, mode), personalRule;
                bool personalRoman = italicOverrides.TryGetValue(key, out personalRule) && (personalRule == "roman" || personalRule == "name" && char.IsUpper(word.Value[0]));
                bool title = IndicStyle.Titles.Contains(key) || (key == "sri" || key == "srimad") && index + 1 < compounds.Count && IndicStyle.Titles.Contains(IndicStyle.Key(compounds[index + 1].Value, mode)) && Regex.IsMatch(output.Substring(word.Index + word.Length, compounds[index + 1].Index - word.Index - word.Length), @"^ +$");
                bool name = ControlName(word.Value, mode) || names.Contains(word.Index) || contextualName;
                bool loanword = IndicStyle.Loanword(word.Value, mode);
                if (KeepWord(word.Value) || ForceStyle(word.Value, mode) || title && !personalRoman)
                    continue;
                int value = personalRoman || name ? -1 : loanword ? individual ? 1 : -1 : 0;
                if (value == 0)
                    continue;
                for (int n = word.Index; n < word.Index + word.Length; n++)
                {
                    styles[n] = value;
                    forced[n] = true;
                }
            }

            var result = new List<ItalicSpan>();
            for (int at = 0; at < styles.Length;)
            {
                if (styles[at] == 0)
                {
                    at++;
                    continue;
                }

                int end = at + 1;
                while (end < styles.Length && styles[end] == styles[at] && forced[end] == forced[at])
                    end++;
                result.Add(new ItalicSpan(at, end - at, forced[at], styles[at] > 0));
                at = end;
            }

            return result.ToArray();
        }

        public WordConversionPlan PrepareControlWord(string input, string mode, string context = null)
        {
            var original = PrepareWordConversion(input, mode, context);
            if (original.Expansion || !AutomaticItalicMode(mode) || !SingleControlWord(input))
                return original;
            Match word = Compounds.Match(input);
            string current = word.Value;
            if (KeepWord(current))
                return original;
            bool known = RecognizedIndic(current, mode) || IndicStyle.Loanword(current, mode);
            bool eligible = known && ControlItalics(null, input, mode, true, context).Any(x => x.Italic);
            if (!eligible)
                return original;
            string canonical = Word(current, mode);
            bool validCurrent = canonical.Normalize() == current.Normalize();
            if (!validCurrent && IndicStyle.Loanword(current, mode) && current.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                string stem = current.Substring(0, current.Length - 1);
                validCurrent = Word(stem, mode).Normalize() == stem.Normalize();
            }

            if (!validCurrent)
            {
                if (original.Choices.Length == 0 && !string.IsNullOrEmpty(canonical))
                    return new WordConversionPlan(input.Substring(0, word.Index) + canonical + input.Substring(word.Index + word.Length), new[] { input.Substring(0, word.Index) + canonical + input.Substring(word.Index + word.Length), input }, false, false, 1);
                int origin = Array.LastIndexOf(original.Choices, input);
                return new WordConversionPlan(original.Text, original.Choices, false, original.Contextual, origin);
            }

            string prefix = input.Substring(0, word.Index), suffix = input.Substring(word.Index + word.Length);
            string[] alternatives = CaretChoices(current, mode).Where(x => x != current).Select(x => prefix + x + suffix).Distinct().ToArray();
            string[] choices = new[]
            {
                input
            }.Concat(alternatives).Concat(new[] { input }).ToArray();
            return new WordConversionPlan(input, choices, false, original.Contextual, choices.Length - 1);
        }
    }
}
