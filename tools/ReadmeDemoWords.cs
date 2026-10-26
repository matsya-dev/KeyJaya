// Developer probe: verify README demo spellings against the engine without starting the resident.
using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using KeyJaya;

// Only call the conversion engine. This never starts the resident or types in an app.
public static class ReadmeDemoWords
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
            throw new ArgumentException("Provide a clean app root and output JSON path.");
        using (var core = new Core(args[0]))
        {
            string krishna = core.CaretChoices("Krishna", "unicode")[0];
            string[] ramaChoices = core.CaretChoices("Rama", "unicode");
            string sp = core.CtrlShort("SP", "unicode");
            string ki = "k" + SingleLetters.Next("i", "unicode");
            if (krishna != "Kṛṣṇa" || sp != "Śrīla Prabhupāda" || ki != "kī")
                throw new InvalidOperationException("Unexpected README demo output.");
            if (string.Join(" / ", ramaChoices) != "Rāma / Ramā / Rāmā / Rama")
                throw new InvalidOperationException("Unexpected Rama cycle order.");
            File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(new { krishna, ramaChoices, sp, ki }), new UTF8Encoding(false));
            Console.WriteLine("Verified README demo output from KeyJaya " + KeyJaya.Version.Current + ": Krishna → " + krishna + "; SP → " + sp + "; ki → " + ki);
            Console.WriteLine("Rama cycle: " + string.Join(" / ", ramaChoices));
        }

        return 0;
    }
}
