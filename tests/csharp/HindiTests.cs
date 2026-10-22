using System;using System.IO;using System.Linq;
namespace KeyJaya {public static class HindiTests {
 static int passed,failed;static void Equal(string expected,string actual,string name){if(expected==actual)passed++;else{failed++;Console.WriteLine("FAIL "+name+" expected="+expected+" actual="+actual);}}
 public static int Main(string[] args){using(Core c=new Core(args[0])){
 Equal("sanskrit",c.ScriptOptions.Language,"Sanskrit default");
 foreach(string mode in new[]{"devanagari","bengali"})foreach(string roman in new[]{"phool","phul"})Equal(mode=="devanagari"?"फूल":"ফূল",c.ConvertCopied(roman,mode),"requested flower "+roman+mode);
 Equal("हरे कृष्ण",c.ConvertCopied("hare kṛṣṇa","devanagari"),"mantra Hare");Equal("হরে কৃষ্ণ",c.ConvertCopied("hare kṛṣṇa","bengali"),"Bengali mantra Hare");
 string baselineSanskrit=c.ConvertCopied("bhārat","devanagari"),baselineBengali=c.ConvertCopied("kala rang; kala","bengali");Equal("itrans",c.ScriptOptions.ShortcutMode("itrans"),"ITRANS shortcut default");c.ScriptOptions.SetLanguage("hindi");Equal("hindi",c.ScriptOptions.ShortcutMode("itrans"),"Hindi uses same shortcut");Equal("devanagari",c.ScriptOptions.ShortcutMode("devanagari"),"Sanskrit shortcut remains available");Equal("bengali",c.ScriptOptions.ShortcutMode("bengali"),"Bengali shortcut remains available");Equal(baselineSanskrit,c.ConvertCopied("bhārat","devanagari"),"Sanskrit unaffected by tertiary choice");Equal("hindi",new ScriptSettings(Path.Combine(Configuration.Folder(args[0]),"settings.ini")).Language,"Hindi persists");
 foreach(var word in Hindi.Common)Equal(word.Value,c.ConvertCopied(word.Key,"hindi"),"common Hindi "+word.Key);
 string[,] forms={{"भारत","bhārat"},{"आपका","āpkā"},{"कमरा","kamrā"},{"नमकीन","namkīn"},{"आगमन","āgaman"},{"हिन्दी","hindī"},{"राजा","rājā"},{"फूल","phūl"},{"घर","ghar"}};
 for(int i=0;i<forms.GetLength(0);i++){Equal(forms[i,1],Hindi.ToIast(forms[i,0]).Replace('ṃ','ṁ'),"Hindi pronunciation utility "+forms[i,0]);c.ScriptOptions.Set(true,false);Equal(IndicScripts.ToIast(forms[i,0]).Replace('ṃ','ṁ'),c.ConvertCopied(forms[i,0],"unicode"),"precise script conversion preserves written vowels "+forms[i,0]);c.ScriptOptions.Set(false,false);Equal(forms[i,0],c.ConvertCopied(forms[i,1],"hindi"),"Hindi IAST "+forms[i,1]);Equal(forms[i,0],c.ConvertCopied(forms[i,0],"devanagari"),"already-written Hindi stays written "+forms[i,0]);}
 foreach(string language in new[]{"hindi","sanskrit"}){c.ScriptOptions.SetLanguage(language);foreach(string mode in new[]{"unicode","balaram","plain","itrans"}){Equal(Core.Target("śrīkṛṣṇāya",mode),c.ConvertCopied("श्रीकृष्णाय",mode),"Sanskrit final vowel retained "+language+" "+mode);Equal(Core.Target("kṛṣṇāy",mode),c.ConvertCopied("कृष्णाय्",mode),"explicit final virama retained "+language+" "+mode);}}
 Equal("काला रंग; काल",c.ConvertCopied("kala rang; kala","hindi"),"color context and isolated time");
 Equal(baselineBengali,c.ConvertCopied("kala rang; kala","bengali"),"Bengali unaffected by tertiary choice");
 Equal("ड़ ढ़",c.ConvertItrans(".Da .Dha","devanagari"),"ITRANS dotted flap distinct from vocalic r");
 Equal("न हि स",c.ConvertCopied("na hi sa","hindi"),"basic syllables in Hindi");
 Equal("अपरिहार्य",c.ConvertCopied("aparihārya","hindi"),"broad dictionary exact written spelling");
 Equal("https://example.com/hare name@example.com",c.ConvertCopied("https://example.com/hare name@example.com","devanagari"),"literal URLs and mail");
 Equal("an age since",c.ConvertCopied("an age since","unicode"),"English stays protected");
 c.ScriptOptions.SetLanguage("sanskrit");Equal("bhārata",c.ConvertCopied("भारत","unicode"),"restored Sanskrit pronunciation");
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}}
