// KeyJaya: Cardinal/ordinal English spelling and strict single-number targeting.
// Passage conversion leaves numbers alone; the caller must explicitly target an individual number.
using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KeyJaya
{
    public static class Numbers
    {
        const string Digits = @"(?:[0-9]{1,3}(?:,[0-9]{3})+|[0-9]+)";
        static readonly Regex SinglePattern = new Regex(@"\A(" + Digits + @")(st|nd|rd|th)?\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static readonly Regex CaretPattern = new Regex(@"\A[('""“‘\[{]*(?<number>" + Digits + @"(?:st|nd|rd|th)?)[)""'”’\]},.!?:;]*\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static string Single(string input)
        {
            if (input == null)
                return null;
            Match match = SinglePattern.Match(input);
            ulong value;
            if (!match.Success || !ulong.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out value))
                return null;
            return Words(match.Groups[1].Value, match.Groups[2].Success);
        }

        internal static Match CaretMatch(string token)
        {
            return CaretPattern.Match(token);
        }

        static readonly string[] ones =
        {
            "zero",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "ten",
            "eleven",
            "twelve",
            "thirteen",
            "fourteen",
            "fifteen",
            "sixteen",
            "seventeen",
            "eighteen",
            "nineteen"
        };
        static readonly string[] tens =
        {
            "",
            "",
            "twenty",
            "thirty",
            "forty",
            "fifty",
            "sixty",
            "seventy",
            "eighty",
            "ninety"
        };
        static readonly string[] units =
        {
            "",
            "thousand",
            "million",
            "billion",
            "trillion",
            "quadrillion",
            "quintillion"
        };
        static readonly string[] cardinal =
        {
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "twelve"
        };
        static readonly string[] ord =
        {
            "first",
            "second",
            "third",
            "fourth",
            "fifth",
            "sixth",
            "seventh",
            "eighth",
            "ninth",
            "twelfth"
        };
        static string Small(int n)
        {
            if (n < 20)
                return ones[n];
            if (n < 100)
                return tens[n / 10] + (n % 10 == 0 ? "" : "-" + ones[n % 10]);
            return ones[n / 100] + " hundred" + (n % 100 == 0 ? "" : " " + Small(n % 100));
        }

        public static string Words(string value, bool ordinal)
        {
            ulong n;
            if (!ulong.TryParse(value.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out n))
                return value;
            string output = "";
            int unit = 0;
            ulong remaining = n;
            do
            {
                int part = (int)(remaining % 1000);
                if (part > 0)
                    output = Small(part) + (unit == 0 ? "" : " " + units[unit]) + (output == "" ? "" : " " + output);
                remaining /= 1000;
                unit++;
            }
            while (remaining > 0);
            if (n == 0)
                output = "zero";
            if (!ordinal)
                return output;
            int split = Math.Max(output.LastIndexOf(' '), output.LastIndexOf('-'));
            string last = output.Substring(split + 1), prefix = output.Substring(0, split + 1);
            int i = Array.IndexOf(cardinal, last);
            last = i >= 0 ? ord[i] : last.EndsWith("y") ? last.Substring(0, last.Length - 1) + "ieth" : last + "th";
            return prefix + last;
        }

        public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            if (args.Length > 0)
                Console.Write(Words(args[0], args.Length > 1 && args[1] == "ordinal"));
            return 0;
        }
    }
}
