using System;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;
namespace KeyJaya {
public static class VerseNormalization {
    public static string Key(string text){return Regex.Replace(text.Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('ṃ','ṁ'),@"[^\p{L}\p{M}\p{N}]","");}
    public static string Apply(string input,Func<string,string> decode,Func<string,string> lookup,Func<string,string> fallback,string mode){
        string[] pieces=Regex.Split(input,@"(\r\n|\r|\n|\|\|?|।|॥)");var output=new StringBuilder();
        foreach(string piece in pieces){if(Regex.IsMatch(piece,@"^(?:\r\n|\r|\n|\|\|?|।|॥)$")){output.Append(piece);continue;}
            string decoded=decode(piece),key=Key(decoded);string canonical=key.Length>=16?lookup(key):null;
            if(canonical==null||!Regex.IsMatch(canonical,@"\S+\s+\S+")){output.Append(fallback(piece));continue;}
            Match boundaries=Regex.Match(piece,@"^(\s*)[\s\S]*?(\s*)$");output.Append(boundaries.Groups[1].Value);output.Append(Core.Target(canonical,mode));output.Append(boundaries.Groups[2].Value);
        }
        return output.ToString();
    }
}
public sealed partial class Core {
    readonly Dictionary<string,string[]> scriptPhrases=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);Regex scriptPhrasePattern;
    static string PhraseKey(string text){return Regex.Replace(text.Normalize(NormalizationForm.FormC).Trim(),@"[ \t\u00a0]+"," ");}
    void ReadScriptPhrases(string path){if(!File.Exists(path))return;int row=0;foreach(string line in File.ReadLines(path)){row++;if(string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;string[] cells=line.Split('\t');if(cells.Length!=3)throw new InvalidDataException("Script phrases need three Tab-separated columns at line "+row);string key=PhraseKey(cells[0]);string[] words=key.Split(' ');if(words.Length<2||key.Length>1000||words.Any(w=>!Regex.IsMatch(w,@"^[\p{L}\p{M}'’~-]+$")))throw new InvalidDataException("Invalid script phrase at line "+row);for(int i=1;i<=2;i++){cells[i]=PhraseKey(cells[i]);if(cells[i]!=""&&(cells[i].Split(' ').Length!=words.Length||cells[i].Any(c=>char.IsControl(c)||(char.IsLetter(c)||char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.NonSpacingMark||char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.SpacingCombiningMark)&&!(i==1?c>='\u0900'&&c<='\u097f':c>='\u0980'&&c<='\u09ff'))||!cells[i].Any(char.IsLetter)))throw new InvalidDataException("Phrase output must use its target script and the same word count at line "+row);}if(cells[1]==""&&cells[2]==""||scriptPhrases.ContainsKey(key)||scriptPhrases.Count>=1000)throw new InvalidDataException("Empty, duplicate, or excessive script phrases at line "+row);scriptPhrases.Add(key,new[]{cells[1],cells[2]});}
        if(scriptPhrases.Count>0)scriptPhrasePattern=new Regex(@"(?<![\p{L}\p{M}\p{N}'’_-])(?:"+string.Join("|",scriptPhrases.Keys.OrderByDescending(x=>x.Split(' ').Length).ThenByDescending(x=>x.Length).ThenBy(x=>x,StringComparer.Ordinal).Select(x=>string.Join(@"[ \t\u00a0]+",x.Split(' ').Select(Regex.Escape).ToArray())).ToArray())+@")(?![\p{L}\p{M}\p{N}'’_-])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
    }
    string ConvertPhraseParts(string input,string mode){if(!ScriptOptions.Precise&&mode=="hindi")input=Hindi.Context(input,mode=="hindi"?"devanagari":mode,key=>scriptPhrases.ContainsKey(PhraseKey(key)));if(ScriptOptions.Precise||scriptPhrasePattern==null||(mode!="devanagari"&&mode!="bengali"&&mode!="hindi"))return ConvertCopiedUnphrased(input,mode);var result=new StringBuilder();int at=0;foreach(Match match in scriptPhrasePattern.Matches(input)){string replacement=scriptPhrases[PhraseKey(match.Value)][mode=="bengali"?1:0];if(replacement=="")continue;result.Append(ConvertCopiedUnphrased(input.Substring(at,match.Index-at),mode));string[] words=replacement.Split(' ');int word=0;result.Append(Regex.Replace(match.Value,@"[^ \t\u00a0]+",m=>words[word++]));at=match.Index+match.Length;}result.Append(ConvertCopiedUnphrased(input.Substring(at),mode));return result.ToString();}
    public ScriptSettings ScriptOptions {get;private set;}
    readonly Dictionary<string,string[]> scriptSpellings=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase);
    void ReadScriptSpellings(string path){if(!File.Exists(path))return;int lineNumber=0;foreach(string line in File.ReadLines(path)){lineNumber++;if(string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;string[] cells=line.Split('\t');if(cells.Length!=3||!Regex.IsMatch(cells[0],@"^[\p{L}\p{M}~^.'’{}-]+$"))throw new InvalidDataException("Invalid script spelling at line "+lineNumber);string key=cells[0].Normalize(NormalizationForm.FormC);for(int i=1;i<=2;i++){if(cells[i].Any(c=>char.IsControl(c)||(char.IsLetter(c)||char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.NonSpacingMark||char.GetUnicodeCategory(c)==System.Globalization.UnicodeCategory.SpacingCombiningMark)&&!(i==1?c>='\u0900'&&c<='\u097f':c>='\u0980'&&c<='\u09ff'))||cells[i].Length>160)throw new InvalidDataException("Wrong target script at line "+lineNumber);}if((cells[1]!=""&&!cells[1].Any(char.IsLetter))||(cells[2]!=""&&!cells[2].Any(char.IsLetter)))throw new InvalidDataException("Script output needs letters at line "+lineNumber);if(cells[1]==""&&cells[2]=="")throw new InvalidDataException("Empty script spelling at line "+lineNumber);if(scriptSpellings.ContainsKey(key))throw new InvalidDataException("Duplicate script spelling: "+key);scriptSpellings.Add(key,new[]{cells[1],cells[2]});}}
    static string CleanCopiedInput(string input){return Regex.Replace(input.Normalize(NormalizationForm.FormC),@"(?<=[\p{L}\p{M}])[\u200c\u200d\u2060]+(?=[\p{L}\p{M}])","").Replace("\ufeff","").Replace("\u200b"," ");}
    static bool LiteralSyllable(string input){return Regex.IsMatch(input,@"^(?:(?:kṣ|jñ|tr|śr|kh|gh|ch|jh|ṭh|ḍh|th|dh|ph|bh|[kgṅcjñṭḍṇtdnpbmyrlvśṣsh])(?:a|ā|i|ī|u|ū|ṛ|ṝ|ḷ|ḹ|e|ai|o|au)?|a|ā|i|ī|u|ū|ṛ|ṝ|ḷ|ḹ|e|ai|o|au)$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);}
    string PreciseScriptWord(string input,string mode){if(LiteralSyllable(input))return Target(input.ToLowerInvariant(),mode);string decoded;if(DecodeItrans(input,out decoded))return Target(decoded,mode);if(EncodingMap.IsLegacy(input)&&!EncodingMap.UnambiguousIndic(input)&&!English(input))return Target(EncodingMap.FromBalaram(input),mode);return Target(Regex.IsMatch(input,@"^[A-Za-z]+(?:-[A-Za-z]+)*(?:['’])?$")?RomanSyllables(input):input,mode);}
    // These defaults are limited to explicit script conversion. They never relax
    // the English guard used by corrections, diacritic conversion or caret cycling.
    static readonly Dictionary<string,string> CommonHindiScript=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
        {"phool","फूल"},{"phul","फूल"},{"kal","कल"},{"aaj","आज"},{"abhi","अभी"},{"bahut","बहुत"},{"acchi","अच्छी"},{"kaise","कैसे"},{"ho","हो"},{"hum","हम"},{"aao","आओ"},{"jao","जाओ"},{"padhai","पढ़ाई"},{"likhna","लिखना"},{"khana","खाना"},{"peena","पीना"},{"dost","दोस्त"},{"doston","दोस्तों"},{"bharat","भारत"},{"hindustan","हिंदुस्तान"},{"hindi","हिंदी"},{"samay","समय"},{"sundar","सुंदर"},{"kala","काल"},{"aapka","आपका"},{"apka","आपका"},{"āpkā","आपका"},
        {"aapki","आपकी"},{"aapkee","आपकी"},{"āpkī","आपकी"},{"aapke","आपके"},{"āpke","आपके"},
        {"aap","आप"},{"mera","मेरा"},{"meri","मेरी"},{"mere","मेरे"},
        {"tumhara","तुम्हारा"},{"hamara","हमारा"},{"namaste","नमस्ते"},
        {"hai","है"},{"hain","हैं"},{"nahi","नहीं"},{"nahin","नहीं"},{"haan","हाँ"},
        {"achha","अच्छा"},{"accha","अच्छा"},{"achchha","अच्छा"},
        {"chhota","छोटा"},{"bada","बड़ा"},{"baṛā","बड़ा"},{"ladka","लड़का"},{"laṛkā","लड़का"},{"ladki","लड़की"},{"laṛkī","लड़की"},
        {"aadmi","आदमी"},{"ādmī","आदमी"},{"kitab","किताब"},{"kitāb","किताब"},
        {"school","स्कूल"},{"computer","कंप्यूटर"},{"phone","फ़ोन"},
        {"kya","क्या"},{"kyon","क्यों"},{"paani","पानी"},{"pani","पानी"},
        {"kahan","कहाँ"},{"kahaan","कहाँ"},{"yahan","यहाँ"},{"yahaan","यहाँ"},
        {"wahan","वहाँ"},{"wahaan","वहाँ"},{"ghar","घर"},{"shukriya","शुक्रिया"},
        {"shahar","शहर"},{"zindagi","ज़िंदगी"},{"zaroor","ज़रूर"},{"zarur","ज़रूर"}
    };
    static readonly Dictionary<string,string> CommonBengaliScript=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
        {"aj","আজ"},{"ekhon","এখন"},{"onek","অনেক"},{"kibhabe","কীভাবে"},{"keno","কেন"},{"hya","হ্যাঁ"},{"achhen","আছেন"},{"achho","আছো"},{"bari","বাড়ি"},{"boi","বই"},{"pori","পড়ি"},{"kori","করি"},{"khai","খাই"},{"jabo","যাব"},{"esho","এসো"},{"bhalobasha","ভালোবাসা"},{"bondhu","বন্ধু"},{"bondhura","বন্ধুরা"},{"somoy","সময়"},{"sundor","সুন্দর"},{"sokal","সকাল"},{"rat","রাত"},{"ami","আমি"},{"tumi","তুমি"},{"apni","আপনি"},{"amar","আমার"},
        {"tomar","তোমার"},{"apnar","আপনার"},{"amra","আমরা"},
        {"bhalo","ভালো"},{"valo","ভালো"},{"achhi","আছি"},{"achi","আছি"},
        {"ache","আছে"},{"kothay","কোথায়"},{"kemon","কেমন"},
        {"boro","বড়"},{"baṛa","বড়"},{"baṛo","বড়ো"},{"chhoto","ছোট"},{"choto","ছোট"},
        {"school","স্কুল"},{"computer","কম্পিউটার"},{"phone","ফোন"},
        {"dhonnobad","ধন্যবাদ"},{"dhanyabad","ধন্যবাদ"},{"bangla","বাংলা"},{"bangali","বাঙালি"}
    };
    static string RomanSyllables(string input){return Regex.Replace(input.ToLowerInvariant(),@"aa|ii|ee|uu|oo|sh|w",m=>m.Value=="aa"?"ā":m.Value=="ii"||m.Value=="ee"?"ī":m.Value=="uu"||m.Value=="oo"?"ū":m.Value=="sh"?"ś":"v");}
    static bool CommonScript(string input,string mode,out string output){
        if(mode=="bengali"&&CommonBengaliScript.TryGetValue(input,out output))return true;
        string hindi;if(CommonHindiScript.TryGetValue(input,out hindi)){output=mode=="bengali"?IndicScripts.FromIast(IndicScripts.ToIast(hindi),mode):hindi;return true;}
        output=null;return false;
    }
    const string LatinCopiedChars=@"[\p{L}\p{M}~^{}_\u200c\u200d\u2011\u2013\--[\u0900-\u09ff]]";
    static readonly Regex CopiedTokens=new Regex(@"[\u0900-\u097f\u200c\u200d]+(?::(?!:))?|[\u0980-\u09ff\u200c\u200d]+(?::(?!:))?|"+LatinCopiedChars+@"+(?:\.(?:N\.h|Dh|ds|au|[mnNahDkgjycsp])"+LatinCopiedChars+@"*)*(?:['’]"+LatinCopiedChars+@"*)?|\.(?:N\.h|Dh|ds|au|[mnNahDkgjycsp])",RegexOptions.Compiled);
    static readonly Regex ScriptVerseMarkers=new Regex(@"[ \t]*[|।॥]+(?:[ \t]*(?:[0-9०-९০-৯]+|\([0-9०-९০-৯]+\)|\[[0-9०-९০-৯]+\])(?=[ \t]*(?:[|।॥]|$)))?[ \t]*",RegexOptions.Compiled);
    static readonly Regex ScriptFinalVerseNumber=new Regex(@"[ \t]+(?:[0-9०-९০-৯]+|\([0-9०-९০-৯]+\)|\[[0-9०-९০-৯]+\])[ \t]*[|।॥]+[ \t]*$",RegexOptions.Compiled);
    static string CleanScriptVerse(string input){return Regex.Replace(input,@"[^\r\n]+",line=>{
        if(!line.Value.Any(c=>c>='\u0900'&&c<='\u09ff'&&char.IsLetter(c))||!ScriptVerseMarkers.IsMatch(line.Value))return line.Value;
        return ScriptVerseMarkers.Replace(ScriptFinalVerseNumber.Replace(line.Value," ")," ").TrimEnd(' ','\t');
    });}
    readonly Lazy<IndexedStore> verseNormalization;
    string NormalizeCompoundCase(string input,string mode){if(mode!="unicode"&&mode!="balaram"&&mode!="plain")return input;return Compounds.Replace(input,m=>{string canonical;if(!CompoundForms.Pattern.IsMatch(m.Value)||!CompoundForms.TryKnown(m.Value,out canonical))return m.Value;string given=mode=="balaram"?EncodingMap.FromBalaram(m.Value):m.Value;string wanted=mode=="plain"?EncodingMap.Ascii(canonical):canonical;wanted=CompoundForms.PreserveSeparators(m.Value,wanted);return string.Equals(given.Replace('ṃ','ṁ'),wanted.Replace('ṃ','ṁ'),StringComparison.OrdinalIgnoreCase)?Target(wanted,mode):m.Value;});}
    string NormalizeItrans(string input,string mode){return NormalizeCompoundCase(VerseNormalization.Apply(input,text=>Itrans.Convert(text,"unicode"),LookupVerse,text=>Itrans.Convert(text,mode),mode),mode);}
    string LookupVerse(string key){return verseNormalization.Value==null?null:verseNormalization.Value.Get(key);}
    string DecodeBengali(string input,string context=null,int offset=0){if(!ScriptOptions.Precise&&input.Any(c=>c>='\u0900'&&c<='\u097f')&&Hindi.UseHindiReading(input,context,offset,KnownSanskritScript,KnownHindiScript))return Hindi.ToIast(input).Replace('ṃ','ṁ');string decoded=IndicScripts.ToIast(input,ScriptOptions.PreserveVerseMarkers);if(ScriptOptions.Precise)return decoded;if(!input.Any(c=>c>='\u0980'&&c<='\u09ff')||decoded.IndexOf('b')<0)return decoded;
        var variants=new List<string>{decoded};int count=0;for(int i=0;i<decoded.Length&&count<6;i++)if(decoded[i]=='b'){int size=variants.Count;for(int n=0;n<size;n++)variants.Add(variants[n].Substring(0,i)+"v"+variants[n].Substring(i+1));count++;}
        foreach(string variant in variants){string known=Find(EncodingMap.Ascii(variant));if(known!=null&&known.Replace('ṃ','ṁ')==variant.Replace('ṃ','ṁ'))return variant;}return decoded;
    }
    bool DecodeItrans(string input,out string decoded,bool trustedPassage=false){decoded=input;if(input.StartsWith("_",StringComparison.Ordinal)||input.EndsWith("_",StringComparison.Ordinal)||IndicLoanwords.Contains(input)||EncodingMap.IsIndic(input)||EncodingMap.IsLegacy(input)||!Itrans.TryWord(input,out decoded)||decoded==input)return false;
        bool marker=input.Contains("~")||input.Contains("^")||input.Contains(".m")||input.Contains(".n")||input.Contains(".N")||input.Contains(".h")||input.Contains(".a")||input.Contains("{}");
        bool internalUpper=input.Skip(1).Any(c=>"AIURNLTDHMS".IndexOf(c)>=0)&&!input.Where(char.IsLetter).All(char.IsUpper);
        string known=Find(EncodingMap.Ascii(decoded));
        // An unknown title/camel-case identifier is insufficient evidence of ITRANS.
        // Explicit markers and an established encoded passage remain authoritative.
        if(!marker&&!trustedPassage&&char.IsUpper(input[0])&&known==null)return false;
        if(!marker&&!internalUpper&&Find(input)!=null)return false;
        if(!marker&&!internalUpper&&known!=decoded)return false;
        if(English(input)&&!IndicLoanwords.Contains(input)&&!personal.ContainsKey(input)&&!trustedPassage)return false;
        decoded=decoded.Replace('ṃ','ṁ');return true;
    }
    string DecodeCopied(string input){return CopiedTokens.Replace(input,m=>{if(IndicScripts.Contains(m.Value))return DecodeBengali(m.Value,input,m.Index);string decoded;if(DecodeItrans(m.Value,out decoded))return decoded;if(EncodingMap.IsLegacy(m.Value)&&!EncodingMap.UnambiguousIndic(m.Value)&&!KnownUnicode(m.Value)&&!English(m.Value)){decoded=EncodingMap.FromBalaram(m.Value);string rendered=IndicScripts.FromIast(decoded,"devanagari");if(rendered!=decoded&&!rendered.Any(c=>char.IsLetter(c)&&!(c>='\u0900'&&c<='\u09ff')))return decoded;}return m.Value;});}
    string CopiedScriptWord(string input,string mode){bool hindiMode=mode=="hindi";if(hindiMode)mode="devanagari";
        if(KeepWord(input))return input;
        if(ScriptOptions.Precise)return PreciseScriptWord(input,mode);string[] preferredScript;if(scriptSpellings.TryGetValue(input.Normalize(NormalizationForm.FormC),out preferredScript)&&preferredScript[mode=="bengali"?1:0]!="")return preferredScript[mode=="bengali"?1:0];
        string custom,common;if(personal.TryGetValue(input,out custom))return Target(EncodingMap.Case(input,custom),mode);
        if(hindiMode){string hindi;if(LiteralSyllable(input)&&input.Length<=2)return Target(input.ToLowerInvariant(),mode);if(!Hindi.Common.TryGetValue(input,out hindi))hindi=HindiLookup(input)??Hindi.PhoneticScript(input);return mode=="devanagari"?hindi:IndicScripts.FromIast(IndicScripts.ToIast(hindi),mode);}
        if(LiteralSyllable(input))return Target(input.ToLowerInvariant(),mode);
        if(CommonScript(input.Normalize(NormalizationForm.FormC),mode,out common))return common;
        string itransScript;if(DecodeItrans(input,out itransScript))return Target(itransScript,mode);
        if(input.EndsWith("'s",StringComparison.OrdinalIgnoreCase)||input.EndsWith("’s",StringComparison.OrdinalIgnoreCase))return Word(input,mode);
        bool elided=input.EndsWith("'",StringComparison.Ordinal)||input.EndsWith("’",StringComparison.Ordinal);
        string stem=elided?input.Substring(0,input.Length-1):input;
        if(elided&&stem.Equals("kari",StringComparison.OrdinalIgnoreCase))return Target("kari’",mode);
        if(EncodingMap.UnambiguousIndic(stem)||KnownUnicode(stem))return Target(input,mode);
        bool explicitLegacy=stem.Any(c=>EncodingMap.IsLegacy(c.ToString())&&!EncodingMap.IsIndic(c.ToString()));
        if(explicitLegacy&&!English(stem)&&(!English(EncodingMap.Ascii(stem))||KnownUnicode(EncodingMap.FromBalaram(stem)))){
            string decoded=EncodingMap.FromBalaram(input),script=Target(decoded,mode);
            // Render supplied diacritics directly instead of replacing them with a dictionary preference.
            if(script!=decoded&&!script.Any(c=>char.IsLetter(c)&&!(c>='\u0900'&&c<='\u09ff')))return script;
        }
        if(Regex.IsMatch(stem,@"^[A-Za-z]+(?:-[A-Za-z]+)*$")){
            // Short syllables are literal: hi -> हि, not a guessed dictionary vowel.
            string book=stem.Length>3?Find(stem.ToLowerInvariant()):null;
            string spelling=book==null?RomanSyllables(stem):EncodingMap.IsIndic(book)?book:RomanSyllables(book);
            string rendered=Target(spelling+(elided?"’":""),mode);
            return rendered.Any(c=>char.IsLetter(c)&&!(c>='\u0900'&&c<='\u09ff'))?input:rendered;
        }
        string known=Word(stem,"unicode");
        if(known!=stem)return Target(known+(elided?"’":""),mode);
        if(elided&&(stem.Equals("kari",StringComparison.OrdinalIgnoreCase)||(!English(stem)&&Find(stem.ToLowerInvariant())!=null)))return Target(known+"’",mode);
        return Word(input,mode);
    }
    bool ItransPassage(string input){int evidence=0;foreach(Match word in CopiedTokens.Matches(input)){string decoded;bool signals=word.Value.Skip(1).Any(c=>"AIURNLTDHMS".IndexOf(c)>=0)&&!word.Value.Where(char.IsLetter).All(char.IsUpper)||word.Value.IndexOfAny(new[]{'~','^'})>=0;if(signals&&Itrans.TryWord(word.Value,out decoded)&&decoded!=word.Value&&!English(word.Value)&&++evidence>=3)return true;}return false;}
    string CopiedFallback(string input,string mode){bool trusted=ItransPassage(input),singleWord=SingleProseWord(input);return CopiedTokens.Replace(input,m=>{if(KeepWord(m.Value))return m.Value;if(IndicScripts.Contains(m.Value))return Target(mode=="devanagari"||mode=="bengali"||mode=="hindi"?IndicScripts.ToIast(m.Value,ScriptOptions.PreserveVerseMarkers):DecodeBengali(m.Value,input,m.Index),mode);if(mode=="devanagari"||mode=="bengali"||mode=="hindi")return CopiedScriptWord(m.Value,mode);string decoded;if(DecodeItrans(m.Value,out decoded,trusted))return Target(decoded,mode);return ProseWord(m.Value,mode,singleWord);});}
    public string ConvertCopied(string input,string mode){var result=new StringBuilder();int position=0;foreach(Match literal in Protected.Matches(input)){result.Append(ConvertCopiedPart(input.Substring(position,literal.Index-position),mode));result.Append(literal.Value);position=literal.Index+literal.Length;}result.Append(ConvertCopiedPart(input.Substring(position),mode));return ProseNameCase(result.ToString(),mode);}
    string ConvertCopiedPart(string input,string mode){
        string output=ConvertCopiedPartUnstyled(input,mode);if(mode!="unicode"&&mode!="balaram"&&mode!="plain")return IndicScripts.ConvertDigits(output,mode);
        var titles=Regex.Matches(input,@"[\p{L}\p{M}]+").Cast<Match>().Where(m=>IndicStyle.Key(m.Value,"balaram")=="goswami"||IndicStyle.Key(m.Value,"balaram")=="gosvami").ToArray();int index=0;
        return Regex.Replace(output,@"[\p{L}\p{M}]+",m=>{string key=IndicStyle.Key(m.Value,mode);if((key!="goswami"&&key!="gosvami")||index>=titles.Length)return m.Value;string chosen=IndicStyle.GoswamiTitle(input,titles[index++]);return Target(EncodingMap.FromBalaram(chosen),mode);});
    }
    string ConvertCopiedPartUnstyled(string input,string mode){if(neverConvert.Count==0)return ConvertPhraseParts(CleanCopiedInput(input),mode);var output=new StringBuilder();int at=0;foreach(Match token in CopiedTokens.Matches(input)){if(!KeepWord(token.Value))continue;output.Append(ConvertPhraseParts(CleanCopiedInput(input.Substring(at,token.Index-at)),mode));output.Append(token.Value);at=token.Index+token.Length;}output.Append(ConvertPhraseParts(CleanCopiedInput(input.Substring(at)),mode));return output.ToString();}
    string ConvertCopiedUnphrased(string input,string mode){if(!ScriptOptions.PreserveVerseMarkers&&mode!="devanagari"&&mode!="bengali"&&mode!="hindi")input=CleanScriptVerse(input);string value=ScriptOptions.Precise&&(mode=="devanagari"||mode=="bengali"||mode=="hindi"||IndicScripts.Contains(input))?CopiedFallback(input,mode):VerseNormalization.Apply(input,DecodeCopied,LookupVerse,text=>CopiedFallback(text,mode),mode);foreach(var rule in rules)value=value.Replace(rule.Key,rule.Value);return value;}
}
}
