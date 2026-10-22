using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Text;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;
namespace KeyJaya {
public static class Tests {
 static int passed,failed;static readonly List<string> failures=new List<string>();
 static void Check(bool ok,string name){if(ok)passed++;else{failed++;if(failures.Count<50)failures.Add(name);}}
 static void Equal(string expected,string actual,string name){Check(expected==actual,name+" expected="+expected+" actual="+actual);}
 public static int Main(string[] args){string root=args[0];Stopwatch total=Stopwatch.StartNew();using(Core c=new Core(root)){
  foreach(string mode in new[]{"unicode","balaram","plain"}){string expected=Core.Target("ācāryas",mode);Equal(expected,c.Word("acaryas",mode),"known plural "+mode);Equal(expected,c.Word("ācāryas",mode),"marked plural "+mode);Equal(expected,c.Word("äcäryas",mode),"legacy plural "+mode);Check(c.CaretChoices("ācāryas",mode).Contains(expected),"plural choices "+mode);Equal("an age since cats cars",c.ConvertCopied("an age since cats cars",mode),"plural English protection "+mode);}
  Equal("Ācāryas",c.Word("Acaryas","unicode"),"plural capitalization");Equal("ācāryas’s",c.Word("acaryas’s","unicode"),"plural possessive");
  Equal("Earlier untouched ācāryas and Kṛṣṇa!",c.ConvertCopied("Earlier untouched acaryas and Krsna!","unicode"),"selected passage plurals and English");
  Equal("ācāryas",c.ConvertHighlighted("acaryas","unicode"),"selected plain plural");
  Check(c.ConvertHighlighted("ācāryas","unicode")!="ācāryas","selected marked plural cycles");
  Equal("Earlier untouched ācāryas and Kṛṣṇa!",c.ConvertHighlighted("Earlier untouched acaryas and Krsna!","unicode"),"selected passage conversion");
  Equal("(ācāryas),\nKṛṣṇa!",c.ConvertHighlighted("(acaryas),\nKrsna!","unicode"),"selected punctuation newline");
  Equal("an age since cats cars",c.ConvertHighlighted("an age since cats cars","unicode"),"selected English passage");
  Equal("äcäryas",c.ConvertHighlighted("acaryas","balaram"),"selected Balaram plural");
  Equal("Austenians or Janites",c.ConvertHighlighted("Austenians or Janites","unicode"),"English proper plural holdout");
  foreach(string plural in new[]{"ācāryas","brāhmaṇas","vaiṣṇavas","śāstras","kīrtanas","ṛṣis","munis"})foreach(string mode in new[]{"unicode","balaram"}){Check(c.CaretChoices(plural,mode).Length>0,"nominal plural recognized "+plural+" "+mode);Check(c.ConvertHighlighted(plural,mode)!=null,"selected nominal plural "+plural+" "+mode);}
  Equal("an age since",c.Convert("an age since","unicode"),"reported English regression");
  Equal("An Age Since AN AGE SINCE",c.Convert("An Age Since AN AGE SINCE","unicode"),"English capitalization");
  Equal("Since an early age, Govinda has visited Vṛndāvana.",c.Convert("Since an early age, Govind has visited Vrindavan.","unicode"),"mixed English and names");
  foreach(string mode in new[]{"unicode","balaram","plain"})foreach(string word in File.ReadLines(Path.Combine(root,"data","english-guard.txt"))){if(Core.IndicLoanwords.Contains(word)||Core.IndicLoanwords.Contains(System.Text.RegularExpressions.Regex.Replace(word,@"(?:['’]s)$","",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))continue;Equal(word,c.Word(word,mode),"English guard "+mode+" "+word);Equal(word,c.Convert(word,mode),"English sentence path "+mode+" "+word);}
  string[] plain={"Govind","Gopinath","Madhav","prakash","bhog","Jagannath","Vishvanath","Vrindavan","darshan","kirtan","Prabhupad","vaishnav","acharya","shastra","atman","ahimsa","chandra","chakra","charana","chaitanya","chitta","chatra"};
  string[] indic={"Govinda","Gopīnātha","Mādhava","prakāśa","bhoga","Jagannātha","Viśvanātha","Vṛndāvana","darśana","kīrtana","Prabhupāda","vaiṣṇava","ācārya","śāstra","ātman","ahiṁsā","candra","cakra","caraṇa","caitanya","citta","chatra"};
  for(int i=0;i<plain.Length;i++){Equal(indic[i],c.Convert(plain[i],"unicode"),"alias "+plain[i]);Equal(indic[i],c.Trigger(plain[i]+">"),"trigger "+plain[i]);Equal(EncodingMap.Balaram(indic[i]),c.Convert(plain[i],"balaram"),"Balaram "+plain[i]);}
  Equal("Śrī Kṛṣṇa Caitanya Mahāprabhu",c.Convert("Sri Krsna Caitanya Mahaprabhu","unicode"),"classic phrase");
  Equal("Śrī Kṛṣṇa Caitanya Mahāprabhu",c.Convert(c.Convert("Śrī Kṛṣṇa Caitanya Mahāprabhu","balaram"),"unicode"),"Balaram round trip");
  Equal("Haridäsa Öhäkura",c.Trigger("HT<"),"HT Balaram");Equal("Haridāsa Ṭhākura",c.Trigger("HT>"),"HT Unicode");Equal("Haridasa Thakura",c.Trigger("HT?"),"HT plain");
  Equal("Bhagavad-gītā",c.Trigger("BG>"),"BG title");Equal("Bhagavad-gétä",c.Trigger("BG<"),"BG legacy title");
  foreach(var t in c.Short)Equal(Configuration.Expand(t.Value,DateTime.Now),Core.CtrlOnlyShort(t.Key)?c.CtrlShort(t.Key,"unicode"):c.Trigger(t.Key),"exclusive configured short "+t.Key);
  foreach(string bad in new[]{"unknownenglishword>","the beautiful Govind>","an>","age<","since>","Govind> HT>","BG99999.9>","chapter>","this>"})Equal(null,c.Trigger(bad),"unknown or whole-sentence must not match "+bad);
  string english="The child changed chapters while chatting about China and charity. The chairman chose a chocolate cake.";Equal(english,c.Convert(english,"unicode"),"English ch words");
  string protectedText="https://example.com/Govind someone@example.com C:\\Users\\Govind\\chapter.txt \\\\server\\Govind\nGovind went to Vrindavan.";
  Equal("https://example.com/Govind someone@example.com C:\\Users\\Govind\\chapter.txt \\\\server\\Govind\nGovinda went to Vṛndāvana.",c.Convert(protectedText,"unicode"),"URL email path protection");
  Equal("\"Govinda,\"\r\n\r\n(Gopīnātha)\tMādhava!",c.Convert("\"Govind,\"\r\n\r\n(Gopinath)\tMadhav!","unicode"),"exact punctuation whitespace");
  Equal("kṛṣṇa-bhakti",c.Convert("krsna-bhakti","unicode"),"hyphenated Sanskrit");
  Equal("café",c.Convert("café","unicode"),"foreign English accent protection");
  Equal("Śrī",c.Convert("S\u0301ri\u0304","unicode"),"decomposed IAST");
  string verse=c.Trigger("BG9.1>");Check(verse!=null&&verse.StartsWith("idaṁ tu te")&&!verse.Contains("uvāca"),"BG9.1 speaker removal");
  string translation=c.Trigger("BG9.1T>");Check(translation!=null&&translation.Contains("The Supreme Personality of Godhead"),"translation");
  Equal(verse+"\n\n"+translation,c.Trigger("BG9.1VT>"),"verse plus translation");Check(c.Trigger("BG9.1VTR>").EndsWith("Bhagavad-gītā 9.1"),"reference title");
  foreach(string book in new[]{"CC1.1.1>","CC1.1.1VT>","CB2.3.4<","SB1.1.1>","BS5.1>","NOI1>","ISOINV>"})Check(!string.IsNullOrEmpty(c.Trigger(book)),"scripture "+book);
  Equal(c.Trigger("CC1.1.1>"),c.Trigger("CCAdi1.1>"),"named CC section");
  foreach(string prefix in new[]{"the beautiful ","word\t","old\r\n","old,","old.","HT> "}){InputState s=new InputState();string token=null;foreach(char ch in prefix+"Govind>"){string candidate=s.Type(ch);if(candidate!=null)token=candidate;}Equal("Govind>",token,"token boundary "+prefix);}
  InputState longToken=new InputState();string overflow=null;foreach(char ch in new string('a',161)+"Govind>"){string candidate=longToken.Type(ch);if(candidate!=null)overflow=candidate;}Equal(null,overflow,"oversized token cannot match tail");
  InputState reset=new InputState();foreach(char ch in "old")reset.Type(ch);reset.Clear();foreach(char ch in "Govind")reset.Type(ch);Equal("Govind>",reset.Type('>'),"context reset");reset.Paused=true;Equal(null,reset.Type('>'),"pause prevents matching");
  InputState undo=new InputState();undo.UndoOriginal="Govind>";undo.UndoOutput="Govinda";undo.Type('x');Equal(null,undo.UndoOriginal,"typing disarms undo");undo.UndoOriginal="source";undo.Clear();Equal(null,undo.UndoOriginal,"context change disarms undo");
  foreach(int dpi in new[]{96,120,144,192,288})Equal((16*dpi/96).ToString(),InputState.IconPixels(dpi).ToString(),"tray icon DPI "+dpi);
  int inputSize=System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.Input));Check(inputSize==40,"x64 SendInput ABI");
  Equal("one hundred twenty-three",Numbers.Words("123",false),"number cardinal");Equal("one hundred twenty-third",Numbers.Words("123",true),"number ordinal");Equal("one million",Numbers.Words("1,000,000",false),"number thousands");
  string[] gana=c.Cycle("gana",'>');Check(gana.Length>=2,"gana has alternatives");Equal("gāna",gana[0],"corpus most common gana first");Check(gana.Contains("gaṇa"),"gana contains gaṇa");
  Equal(c.Trigger("gana>"),gana[0],"initial conversion agrees with cycling");
  Equal(EncodingMap.Balaram(gana[0]),c.Cycle("gana",'<')[0],"Balaram first variant");Equal(EncodingMap.Ascii(gana[0]),c.Cycle("gana",'?')[0],"question-mark cycle requests plain text");
  Check(c.Cycle("an",'>').Length==0&&c.Cycle("since",'?').Length==0,"English cannot cycle");
  Equal("Gāna",c.Cycle("Gana",'>')[0],"cycle capitalization");Equal("gāna's",c.Cycle("gana's",'>')[0],"possessive cycling");
  foreach(string alias in new[]{"Krishna","Vrindavan","Gopinath"}){string[] variants=c.Cycle(alias,'>');if(variants.Length>0)Equal(c.Trigger(alias+">"),variants[0],"alias preferred "+alias);}
  InputState cycling=new InputState{Owner=new IntPtr(42),Candidates=gana,UndoOriginal="gana>",UndoOutput=gana[0],CycleDeadline=DateTime.UtcNow.AddSeconds(10)};
  foreach(char suffix in new[]{'>','<','?'})Check(cycling.QueueCycle(suffix,new IntPtr(42),DateTime.UtcNow),"cycle suffix "+suffix);
  Check(cycling.PendingCycles==3,"rapid cycle presses queued");Check(!cycling.QueueCycle('>',new IntPtr(43),DateTime.UtcNow),"different window cannot cycle");
  for(int i=0;i<100;i++)cycling.QueueCycle('?',new IntPtr(42),DateTime.UtcNow);Check(cycling.PendingCycles==64,"bounded cycle queue");
  cycling.Clear();Check(cycling.PendingCycles==0&&!cycling.QueueCycle('>',new IntPtr(42),DateTime.UtcNow),"context change cancels queued cycles");
  cycling.Candidates=gana;cycling.CycleDeadline=DateTime.UtcNow.AddSeconds(-1);Check(!cycling.QueueCycle('>',new IntPtr(42),DateTime.UtcNow),"expired word cannot cycle");
  cycling.CycleDeadline=DateTime.UtcNow.AddSeconds(10);cycling.Type('x');Check(cycling.Candidates==null&&cycling.PendingCycles==0,"new typing cancels cycle");
  Stopwatch stress=Stopwatch.StartNew();Parallel.For(0,1000,i=>{string value=c.Convert("Govind Gopinath Madhav Krsna Vrindavan an age since","unicode");if(value!="Govinda Gopīnātha Mādhava Kṛṣṇa Vṛndāvana an age since")throw new Exception("Concurrent conversion mismatch");});Check(true,"1000 parallel conversions");Console.WriteLine("Concurrent stress ms: "+stress.ElapsedMilliseconds);
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed+" TOTAL_MS: "+total.ElapsedMilliseconds);foreach(string failure in failures)Console.WriteLine("FAIL "+failure);return failed==0?0:1;
 }
}
}
