// KeyJaya: Devanagari/Bengali character parsing and IAST rendering, including marks and digits.
// These script mechanics do not decide prose styling or which English-looking words should convert.
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace KeyJaya
{
    public static class IndicScripts
    {
        static readonly string[] Vowels =
        {
            "a",
            "ā",
            "i",
            "ī",
            "u",
            "ū",
            "ṛ",
            "ṝ",
            "ḷ",
            "ḹ",
            "e",
            "ai",
            "o",
            "au"
        };
        static readonly string[] Consonants =
        {
            "k",
            "kh",
            "g",
            "gh",
            "ṅ",
            "c",
            "ch",
            "j",
            "jh",
            "ñ",
            "ṭ",
            "ṭh",
            "ḍ",
            "ḍh",
            "ṇ",
            "t",
            "th",
            "d",
            "dh",
            "n",
            "p",
            "ph",
            "b",
            "bh",
            "m",
            "y",
            "r",
            "l",
            "v",
            "ś",
            "ṣ",
            "s",
            "h"
        };
        static readonly string[] DevaV =
        {
            "अ",
            "आ",
            "इ",
            "ई",
            "उ",
            "ऊ",
            "ऋ",
            "ॠ",
            "ऌ",
            "ॡ",
            "ए",
            "ऐ",
            "ओ",
            "औ"
        };
        static readonly string[] DevaM =
        {
            "",
            "ा",
            "ि",
            "ी",
            "ु",
            "ू",
            "ृ",
            "ॄ",
            "ॢ",
            "ॣ",
            "े",
            "ै",
            "ो",
            "ौ"
        };
        static readonly string[] DevaC =
        {
            "क",
            "ख",
            "ग",
            "घ",
            "ङ",
            "च",
            "छ",
            "ज",
            "झ",
            "ञ",
            "ट",
            "ठ",
            "ड",
            "ढ",
            "ण",
            "त",
            "थ",
            "द",
            "ध",
            "न",
            "प",
            "फ",
            "ब",
            "भ",
            "म",
            "य",
            "र",
            "ल",
            "व",
            "श",
            "ष",
            "स",
            "ह"
        };
        static readonly string[] BengaliV =
        {
            "অ",
            "আ",
            "ই",
            "ঈ",
            "উ",
            "ঊ",
            "ঋ",
            "ৠ",
            "ঌ",
            "ৡ",
            "এ",
            "ঐ",
            "ও",
            "ঔ"
        };
        static readonly string[] BengaliM =
        {
            "",
            "া",
            "ি",
            "ী",
            "ু",
            "ূ",
            "ৃ",
            "ৄ",
            "ৢ",
            "ৣ",
            "ে",
            "ৈ",
            "ো",
            "ৌ"
        };
        // Bengali uses ব for both Sanskrit ba and va; dictionary recognition can restore known forms.
        static readonly string[] BengaliC =
        {
            "ক",
            "খ",
            "গ",
            "ঘ",
            "ঙ",
            "চ",
            "ছ",
            "জ",
            "ঝ",
            "ঞ",
            "ট",
            "ঠ",
            "ড",
            "ঢ",
            "ণ",
            "ত",
            "থ",
            "দ",
            "ধ",
            "ন",
            "প",
            "ফ",
            "ব",
            "ভ",
            "ম",
            "য",
            "র",
            "ল",
            "ব",
            "শ",
            "ষ",
            "স",
            "হ"
        };
        // Nukta characters are verified against the Unicode Devanagari/Bengali charts.
        // https://www.unicode.org/charts/PDF/U0900.pdf / U0980.pdf
        static readonly string[] Extended =
        {
            "q",
            "k͟h",
            "ġ",
            "z",
            "r̤",
            "r̤h",
            "f",
            "ẏ",
            "ṟ",
            "l̤",
            "ṉ"
        };
        static readonly string[] ExtendedDeva =
        {
            "क़",
            "ख़",
            "ग़",
            "ज़",
            "ड़",
            "ढ़",
            "फ़",
            "य़",
            "ऱ",
            "ऴ",
            "ऩ"
        };
        static readonly string[] ExtendedBengali =
        {
            "ক়",
            "খ়",
            "গ়",
            "জ়",
            "ড়",
            "ঢ়",
            "ফ",
            "য়",
            "র",
            "ল",
            "ন"
        };
        static readonly string[] Tokens = Vowels.Concat(Consonants).Concat(Extended).Concat(new[] { "ṁ", "ṃ", "ḥ", "m̐", "’", "'" }).OrderByDescending(x => x.Length).ToArray();
        // Unicode NFC leaves several nukta letters decomposed. Consume the complete letter
        // before applying an inherent vowel, vowel sign, or virama.
        static readonly Dictionary<string, string> DottedConsonants = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {
                "য়".Normalize(NormalizationForm.FormC),
                "y"
            },
            {
                "ড়".Normalize(NormalizationForm.FormC),
                "ṛ"
            },
            {
                "ঢ়".Normalize(NormalizationForm.FormC),
                "ṛh"
            },
            {
                "ক়",
                "q"
            },
            {
                "খ়",
                "k͟h"
            },
            {
                "গ়",
                "ġ"
            },
            {
                "জ়",
                "z"
            },
            {
                "ফ়",
                "f"
            },
            {
                "क़".Normalize(NormalizationForm.FormC),
                "q"
            },
            {
                "ख़".Normalize(NormalizationForm.FormC),
                "k͟h"
            },
            {
                "ग़".Normalize(NormalizationForm.FormC),
                "ġ"
            },
            {
                "ज़".Normalize(NormalizationForm.FormC),
                "z"
            },
            {
                "ड़".Normalize(NormalizationForm.FormC),
                "r̤"
            },
            {
                "ढ़".Normalize(NormalizationForm.FormC),
                "r̤h"
            },
            {
                "फ़".Normalize(NormalizationForm.FormC),
                "f"
            },
            {
                "य़".Normalize(NormalizationForm.FormC),
                "ẏ"
            },
            {
                "ऱ".Normalize(NormalizationForm.FormC),
                "ṟ"
            },
            {
                "ऴ".Normalize(NormalizationForm.FormC),
                "l̤"
            },
            {
                "ऩ".Normalize(NormalizationForm.FormC),
                "ṉ"
            }
        };
        public static bool Contains(string value)
        {
            return value.Any(c => c >= '\u0900' && c <= '\u09ff');
        }

        static bool ColonVisarga(string input, int at)
        {
            if (at == 0 || input[at] != ':' || (at + 1 < input.Length && input[at + 1] == ':'))
                return false;
            char previous = input[at - 1];
            if (previous < '\u0900' || previous > '\u09ff')
                return false;
            var category = char.GetUnicodeCategory(previous);
            return char.IsLetter(previous) || category == System.Globalization.UnicodeCategory.NonSpacingMark || category == System.Globalization.UnicodeCategory.SpacingCombiningMark;
        }

        public static string ToIast(string input, bool preserveVerseMarkers = false)
        {
            input = input.Normalize(NormalizationForm.FormC);
            var output = new StringBuilder();
            bool pending = false;
            for (int at = 0; at < input.Length; at++)
            {
                char c = input[at];
                bool bengali = c >= '\u0980' && c <= '\u09ff';
                var vowels = bengali ? BengaliV : DevaV;
                var marks = bengali ? BengaliM : DevaM;
                var consonants = bengali ? BengaliC : DevaC;
                string symbol = c.ToString();
                if (c == '\u200d' || c == '\u200c')
                    continue;
                string dotted = null;
                int consumed = 1;
                bool hasDotted = at + 1 < input.Length && (input[at + 1] == '़' || input[at + 1] == '়') && DottedConsonants.TryGetValue(input.Substring(at, 2), out dotted);
                if (hasDotted)
                    consumed = 2;
                else
                    hasDotted = DottedConsonants.TryGetValue(symbol, out dotted);
                if (hasDotted)
                {
                    if (pending)
                        output.Append('a');
                    output.Append(dotted);
                    pending = true;
                    at += consumed - 1;
                    continue;
                }

                if (c == '्' || c == '্')
                {
                    if (!pending)
                        output.Append(c);
                    pending = false;
                    continue;
                }

                int mark = Array.IndexOf(marks, symbol);
                if (mark > 0)
                {
                    if (pending)
                        output.Append(Vowels[mark]);
                    else
                        output.Append(c);
                    pending = false;
                    continue;
                }

                if (pending)
                {
                    output.Append('a');
                    pending = false;
                }

                if (ColonVisarga(input, at))
                {
                    output.Append('ḥ');
                    continue;
                }

                int consonant = Array.IndexOf(consonants, symbol);
                if (consonant >= 0)
                {
                    output.Append(Consonants[consonant]);
                    pending = true;
                    continue;
                }

                if (c == 'ৱ')
                {
                    output.Append('v');
                    pending = true;
                    continue;
                }

                if (c == 'ৰ')
                {
                    output.Append('r');
                    pending = true;
                    continue;
                }

                if (c == 'ळ')
                {
                    output.Append('ḻ');
                    pending = true;
                    continue;
                }

                int vowel = Array.IndexOf(vowels, symbol);
                if (vowel >= 0)
                {
                    output.Append(Vowels[vowel]);
                    continue;
                }

                if (c == 'ं' || c == 'ং')
                    output.Append('ṁ');
                else if (c == 'ः' || c == 'ঃ')
                    output.Append('ḥ');
                else if (c == 'ँ' || c == 'ঁ')
                    output.Append("m̐");
                else if (c == 'ऽ' || c == 'ঽ')
                    output.Append('’');
                else if (c == '।')
                    output.Append(preserveVerseMarkers ? "।" : "|");
                else if (c == '॥')
                    output.Append(preserveVerseMarkers ? "॥" : "||");
                else if (c == 'ॐ')
                    output.Append("oṁ");
                else if (c >= '०' && c <= '९')
                    output.Append((char)('0' + c - '०'));
                else if (c >= '০' && c <= '৯')
                    output.Append((char)('0' + c - '০'));
                else if (c == 'ৎ')
                    output.Append('t');
                else
                    output.Append(c);
            }

            if (pending)
                output.Append('a');
            return output.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string ConvertDigits(string input, string mode)
        {
            if (mode != "devanagari" && mode != "hindi" && mode != "bengali")
                return input;
            char zero = mode == "bengali" ? '০' : '०';
            var result = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                int digit = c >= '0' && c <= '9' ? c - '0' : c >= '०' && c <= '९' ? c - '०' : c >= '০' && c <= '৯' ? c - '০' : -1;
                result.Append(digit < 0 ? c : (char)(zero + digit));
            }

            return result.ToString();
        }

        public static string FromIast(string input, string mode)
        {
            bool bengali = mode == "bengali";
            string[] vowels = bengali ? BengaliV : DevaV, marks = bengali ? BengaliM : DevaM, consonants = bengali ? BengaliC : DevaC;
            string virama = bengali ? "্" : "्";
            return ConvertDigits(Regex.Replace(input.Normalize(NormalizationForm.FormC), @"[\p{L}\p{M}’']+", match =>
            {
                string text = match.Value.ToLowerInvariant();
                var output = new StringBuilder();
                bool pending = false, vowelBefore = false;
                for (int at = 0; at < text.Length;)
                {
                    string token = Tokens.FirstOrDefault(x => at + x.Length <= text.Length && string.CompareOrdinal(text, at, x, 0, x.Length) == 0);
                    if (token == null)
                        return match.Value;
                    at += token.Length;
                    int vowel = Array.IndexOf(Vowels, token);
                    if (vowel >= 0)
                    {
                        output.Append(pending ? marks[vowel] : vowels[vowel]);
                        pending = false;
                        vowelBefore = true;
                        continue;
                    }

                    if (pending)
                    {
                        output.Append(virama);
                        pending = false;
                    }

                    bool medialY = bengali && token == "y" && vowelBefore;
                    vowelBefore = false;
                    int consonant = Array.IndexOf(Consonants, token);
                    if (consonant >= 0)
                    {
                        output.Append(medialY ? "য়" : consonants[consonant]);
                        pending = true;
                    }
                    else if (Array.IndexOf(Extended, token) >= 0)
                    {
                        output.Append((bengali ? ExtendedBengali : ExtendedDeva)[Array.IndexOf(Extended, token)]);
                        pending = true;
                    }
                    else if (token == "ṁ" || token == "ṃ")
                        output.Append(bengali ? "ং" : "ं");
                    else if (token == "ḥ")
                        output.Append(bengali ? "ঃ" : "ः");
                    else if (token == "m̐")
                        output.Append(bengali ? "ঁ" : "ँ");
                    else
                        output.Append(bengali ? "’" : "ऽ");
                }

                if (pending)
                    output.Append(virama);
                return output.ToString();
            }), mode);
        }
    }
}
