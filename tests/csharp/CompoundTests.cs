using System;using System.Linq;using System.Collections.Generic;using System.IO;
namespace KeyJaya {public static class CompoundTests {
 static int passed,failed;static void Equal(string expected,string actual,string label){if(expected==actual)passed++;else{failed++;if(failed<35)Console.WriteLine("FAIL "+label+" expected="+expected+" actual="+actual);}}
 static string Styled(Core core,string text,string mode){return string.Join("|",core.ItalicWords(text,mode).Select(span=>text.Substring(span.Start,span.Length)));}
 public static int Main(string[] args){using(Core core=new Core(args[0])){
  foreach(string canonical in CompoundForms.Spellings)foreach(string separator in new[]{"-","\u2011","\u2013"})foreach(string mode in new[]{"unicode","balaram","plain"}){
   string wanted=Core.Target(canonical.Replace("-",separator),mode);
   foreach(string source in new[]{EncodingMap.Ascii(canonical).ToLowerInvariant(),canonical,EncodingMap.Balaram(canonical)}){
    string input=source.Replace("-",separator);Equal(wanted,core.Word(input,mode),"word "+input+" "+mode);Equal(wanted,core.ConvertCopied(input,mode),"copy "+input+" "+mode);
    string original="Earlier untouched ("+input+"), next";Equal("Earlier untouched ("+wanted+"), next",core.ConvertCopied(original,mode),"prose boundaries "+mode);
    Equal(wanted,core.Trigger(input+(mode=="balaram"?"<":mode=="plain"?"?":">")),"suffix "+input);
    string[] choices=core.CaretChoices(input,mode);Equal("True",choices.Contains(wanted).ToString(),"whole selection choices "+input);
    using(var recorded=new IndexedStore(Path.Combine(args[0],"data"),"cycle")){
     var keys=new[]{source,EncodingMap.FromBalaram(source)}.SelectMany(x=>new[]{EncodingMap.Ascii(x).ToLowerInvariant(),CompoundForms.Key(x)}).Distinct();
     var attested=keys.Select(recorded.Get).Where(x=>x!=null).SelectMany(x=>x.Split('\0')[0].Split('\x1f')).Where(x=>CompoundForms.Pattern.IsMatch(x)).Select(x=>Core.Target(EncodingMap.Case(input,CompoundForms.PreserveSeparators(input,x)),mode)).ToArray();
     string plain=EncodingMap.Ascii(canonical).Replace("-",separator);
     Equal("True",choices.All(x=>x==wanted||x==plain||x==plain.ToLowerInvariant()||attested.Contains(x)).ToString(),"extra compound choices must be recorded whole spellings");
    }
   }
  }
  foreach(string mode in new[]{"unicode","balaram"}){
   Equal(Core.Target("founder-ācārya",mode),core.Word("founder-acarya",mode),"mixed English/Indic");
   Equal(Core.Target("Founder-ācārya",mode),core.Word("Founder-acarya",mode),"English capitalization preserved");
   Equal(Core.Target("Vyāsa-Unknownword",mode),core.Word("Vyasa-Unknownword",mode),"unknown component preserved");
   Equal(Core.Target("śiva-bhakti",mode),core.Word("siva-bhakti",mode),"Siva component");
   Equal("English-Unknownword",core.Word("English-Unknownword",mode),"unknown compound unchanged");
   Equal("café-"+Core.Target("ācārya",mode),core.Word("café-acarya",mode),"unknown accented component untouched");
   Equal("True",core.CaretChoices("café-acarya",mode).Contains("café-acarya").ToString(),"plain cycle preserves unknown accented text");
   string eventText=Core.Target("Vyāsa-pūjā Gaura-Nitāi Navadvīpa-dhāma",mode);Equal("",Styled(core,eventText,mode),"event/name/place roman");
   string ordinary=Core.Target("guru-bhakti kṛṣṇa-līlā",mode);Equal(Core.Target("guru-bhakti|kṛṣṇa-līlā",mode),Styled(core,ordinary,mode),"ordinary compounds italic");
   Equal(Core.Target("ācārya",mode),Styled(core,Core.Target("founder-ācārya",mode),mode),"English part regular");
   Equal(Core.Target("śāstra",mode),Styled(core,Core.Target("Gauḍiyā Gauḍīya and śāstra",mode),mode),"Gaudiya exception");
  }
  Equal("māthura",core.ConvertCopied("māthura","unicode"),"explicit adjective spelling preserved");
  string[,] words={{"niti","nīti"},{"Niti","Nīti"},{"niti-sastra","nīti-śāstra"},{"saiva","śaiva"},{"Saiva","Śaiva"},{"varna","varṇa"},{"Siva","Śiva"},{"siva","śiva"},{"brahmin","brāhmaṇa"},{"brahmins","brāhmaṇas"},{"Brahmins","Brāhmaṇas"},{"Gosvami","Gosvāmī"},{"gosvami","Gosvāmī"},{"Gosvamipada","Gosvāmīpāda"},{"gosvamipada","Gosvāmīpāda"},{"mathura","Mathurā"},{"Mathura","Mathurā"}};
  for(int n=0;n<words.GetLength(0);n++)foreach(string mode in new[]{"unicode","balaram"}){string expected=Core.Target(words[n,1],mode);Equal(expected,core.Word(words[n,0],mode),"requested word "+words[n,0]);Equal("True",core.CaretChoices(words[n,0],mode).Contains(expected).ToString(),"requested caret "+words[n,0]);Equal(expected,core.ConvertCopied(words[n,0],mode),"requested copied "+words[n,0]);Equal(expected,core.ConvertHighlighted(words[n,0],mode),"requested selected "+words[n,0]);Equal(expected,core.Trigger(words[n,0]+(mode=="balaram"?"<":">")),"requested suffix "+words[n,0]);}
  Equal("nama-kīrtana",core.ConvertItrans("nama-kIrtana","unicode"),"explicit ITRANS preserves short vowel");
  Equal("nama-kīrtana",core.ConvertCopied("nama-kIrtana","unicode"),"automatic ITRANS compound preserves supplied vowels");
  Equal("mūtra-pūriṣaṁ",core.ConvertCopied("mUtra-pUriSaM","unicode"),"ITRANS compound modifiers");
  Equal("jñāna-yoga",core.ConvertCopied("j~nAna-yoga","unicode"),"ITRANS marker not split inside token");
  Equal("Unknownword-nāma",core.ConvertCopied("Unknownword-nAma","unicode"),"encoded component does not reinterpret unknown neighbor");
  Equal("Tamāla Kṛṣṇa Goswami",core.ConvertCopied("Tamala Krsna Gosvami","unicode"),"modern name context still wins");
  bool[] mask=core.CompoundProtectedMask("founder-ācārya");Equal("True",mask.Take(8).All(x=>x).ToString(),"English and separator font protected");Equal("True",mask.Skip(8).All(x=>!x).ToString(),"Indic font repair scoped");
  var fonts=new FontEncoding(null);Equal("ScaGoudy",MixedWordFonts.BalaramFamily("founder-ācārya",new[]{"founder-","ā","c","ā","rya"},new[]{"Cambria","Cambria","ScaGoudy","Cambria","ScaGoudy"},fonts,mask),"mixed compound font evidence");
  var spans=core.CompoundFontSpans("founder-acarya","founder-äcärya","balaram");Equal("8:6",string.Join("|",spans.Select(span=>span.Start+":"+span.Length)),"font repair only Indic component");
  Equal("Vyāsa-pūjā’s",core.Word("Vyasa-puja’s","unicode"),"possessive preserved");
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}}
