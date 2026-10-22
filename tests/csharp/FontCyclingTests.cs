using System;using System.IO;using System.Linq;using System.Text.RegularExpressions;
namespace KeyJaya {public static class FontCyclingTests {
static long passed,failed;static void Check(bool ok,string label){if(ok)passed++;else if(failed++<40)Console.WriteLine("FAIL "+label);}
static void Equal(string expected,string actual,string label){Check(expected==actual,label+" expected="+expected+" actual="+actual);}
public static int Main(string[] args){
var font=new FontEncoding(null);
foreach(string family in new[]{"Balaram","BellCentennial","ScaBenguit","ScaCheltenham","ScaFrizQuadrata","ScaGoudy","ScaHelvetica","ScaKorinna","ScaOptima","ScaPalatino","ScaSabon","ScaTimes"})foreach(string style in new[]{""," Bold"," Italic"," BoldItalic"," Normal"}){
Equal("balaram",font.Resolve(" @"+family.ToUpperInvariant()+style.Replace(" ","  ")+" "),"known font variant");
Equal("ā",SingleLetters.Next("a","unicode"),"Unicode letter unchanged");Equal("ä",SingleLetters.Next("a",font.Resolve(family)),"font selects Balaram");
Equal(null,font.Resolve(family,true),"mixed font skipped");
}
foreach(string name in new[]{null,"","Arial","Times New Roman","ScaGoudyUnicode","Fake Balaram","Noto Serif"})Equal("unicode",font.Resolve(name),"unknown font fallback");
foreach(string name in new[]{"Inbenb","Inbeni","Inbeno","Inbenr","Indevr"})Equal(null,font.Resolve(name),"incompatible legacy font skipped");
string config=Path.Combine(Path.GetTempPath(),"KeyJaya-font-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(config,"config"));string file=Path.Combine(config,"config/font-encoding.tsv");
File.WriteAllText(file,"# example\nMy Legacy Font\tbalaram\nBalaram\tunicode\nArial\tignore\n");var custom=new FontEncoding(config);Equal("balaram",custom.Resolve("my legacy font"),"custom family");Equal("unicode",custom.Resolve("Balaram"),"override built-in");Equal(null,custom.Resolve("Arial"),"explicit ignore");
foreach(string invalid in new[]{"Font\tunknown","\tunicode","Font\tbalaram\textra","Font"}){File.WriteAllText(file,invalid);bool rejected=false;try{new FontEncoding(config);}catch(FormatException){rejected=true;}Check(rejected,"invalid configuration rejected");Equal("unicode",custom.Resolve("Balaram"),"old map remains valid");}
Directory.Delete(config,true);
foreach(string prefix in new[]{"123","raja","Earlier rāja","Earlier\r\nrājā","(räja","x:brāhmaṇas","saṁyatendriyaḥ","tat-paraḥ","rāja's"}){string actual=CaretWords.AtEnd(prefix);Check(actual!=null,"word at caret "+prefix);int begin=prefix.Length-actual.Length;Check(EditGuard.LeftBoundary(prefix,begin,actual),"current word left boundary");}
foreach(string prefix in new[]{null,"","Earlier ","raja ","raja.","raja>",new string('a',161),"hello_raja","https://example.com/raja"})Check(CaretWords.AtEnd(prefix)==null,"unqualified caret no word "+prefix);
foreach(string stale in new[]{"rāj","previous","a","raj"})Check(!CaretWords.MatchesTyped(stale,"raja","a"),"late editor word rejected");
Check(CaretWords.MatchesTyped("Raja","raja","a"),"Word capitalization accepted");
Check(CaretWords.MatchesTyped("rāja",null,null),"existing word accepted");
foreach(string last in new[]{" ",".",">","<","1"})Check(!CaretWords.MatchesTyped("rāja","",last),"late delimiter cannot convert previous word");
Check(LeftControlTap.IsRightControl(0xA3,0)&&LeftControlTap.IsRightControl(0x11,1),"right Ctrl identities");Check(!LeftControlTap.IsRightControl(0xA2,0)&&!LeftControlTap.IsRightControl(0x11,0),"left Ctrl excluded");
Check(LeftControlTap.IsLeftControl(0xA2,0)&&LeftControlTap.IsLeftControl(0x11,0),"word action recognizes physical Left Ctrl");
Check(!LeftControlTap.IsLeftControl(0xA3,0)&&!LeftControlTap.IsLeftControl(0x11,1),"Right Ctrl has no word action");
var owner=new IntPtr(1);var other=new IntPtr(2);int[] focus={10},different={11};var now=DateTime.UtcNow;var tap=new LeftControlTap();
for(int i=0;i<1000;i++){tap.Down(owner,i,now);tap.Cancel();Check(!tap.Up(owner,i,now.AddMilliseconds(100)),"Ctrl chord cancels tap");tap.Down(owner,i,now);Check(!tap.Up(other,i,now.AddMilliseconds(100)),"changed owner cancels tap");tap.Down(owner,i,now);Check(!tap.Up(owner,i+1,now.AddMilliseconds(100)),"typing generation cancels tap");}
var queue=new CaretTapQueue();bool word;
for(int i=0;i<100;i++)queue.Add(owner,1,i%2==0);for(int i=0;i<64;i++)Check(queue.Take(owner,1,false,out word)&&word==(i%2==0),"bounded FIFO tap order");Check(!queue.Take(owner,1,false,out word),"tap overflow bounded");
foreach(int n in new[]{0,1,2}){queue.Add(owner,1,true);Check(!queue.Take(n==0?other:owner,n==1?2:1,n==2,out word),"stale/paused tap discarded");}

// Interior and punctuation positions own only the complete adjacent word.
foreach(string prefix in new[]{"", "Earlier untouched ", "First paragraph.\r\nEarlier an age since "})foreach(string token in new[]{"raja","rāja","räja","rāja’s"})foreach(string tail in new[]{"", ",", ".", "!", "?!", "’", "\"", ")", "].", "॥"}){
 string text=prefix+token+tail+" next word";for(int offset=0;offset<=token.Length+tail.Length;offset++){var span=CaretWords.Locate(text,prefix.Length+offset,prefix.Length+offset);Check(span!=null&&span.Start==prefix.Length&&span.End==prefix.Length+token.Length&&span.Word==token,"complete word at every interior/punctuation caret");if(span==null)continue;string replacement="rājā",changed=text.Substring(0,span.Start)+replacement+text.Substring(span.End);Equal(prefix+replacement+tail+" next word",changed,"word replacement preserves all punctuation and neighboring text");int newOffset=CaretWords.ReplacementOffset(span.Offset,token.Length,replacement.Length);Check(newOffset>=0&&newOffset<=replacement.Length+tail.Length,"restored caret remains within word/owned punctuation");int restored=CaretWords.ReplacementOffset(newOffset,replacement.Length,token.Length);if(offset>=token.Length)Check(restored==offset,"punctuation caret round trips after undo");}
}
foreach(string text in new[]{"raja ","raja. ","https://example.com/raja","name@raja","raja_foo","C:\\raja","123raja","raja123","#raja",new string('a',161)})Check(CaretWords.Locate(text,text.Length,text.Length)==null,"invalid current token never jumps to a preceding word");
Check(CaretWords.Locate("Earlier raja",3,5)==null,"selected text is not a bare Ctrl caret word");
foreach(string navigationWord in new[]{"rāja","räja","rāja’s"})Check(CaretWords.NavigationCount(navigationWord)==navigationWord.Length,"composed word navigation is bounded and exact");foreach(string navigationWord in new[]{"ra\u0304ja", "😀",new string('a',161)})Check(CaretWords.NavigationCount(navigationWord)==0,"ambiguous grapheme navigation refused");
Check(CaretWords.MatchesPhysical("raja",4,"","a"),"fresh typing sampled at word end");Check(CaretWords.MatchesPhysical("raja",2,"","a"),"fresh typing sampled inside full word");Check(CaretWords.MatchesPhysical("raja",5,",",","),"fresh punctuation sample accepted");Check(!CaretWords.MatchesPhysical("raja",4,"","s"),"stale letter sampled before physical insertion rejected");
// Pasted input has no physical token: the collapsed caret remains the only source of truth.
foreach(string prefix in new[]{"", "Earlier untouched ", "Earlier an age since\r\n", "First paragraph.\r\nSecond paragraph ends with "}){
 Equal("raja",CaretWords.AtEnd(prefix+"raja"),"one-word/paragraph paste discovers only final word");
 Check(CaretWords.MatchesTyped("raja",null,null),"pasted word does not require physical typing");
 Check(CaretWords.AtEnd(prefix+"raja ")==null,"paste with trailing space rejected");
 int begin;string fixture=prefix+"raja";
 Check(EditGuard.Locate(fixture,fixture.Length,fixture.Length,"raja",out begin)&&begin==prefix.Length,"pasted final word exact boundary");
 Check(!EditGuard.Locate(fixture,prefix.Length,fixture.Length,"raja",out begin),"pasted selection rejected");
}
using(var core=new Core(args[0])){
foreach(string protectedWord in new[]{"an","age","since","prana","EnglishUnknown","prana's","Prana’s","prana’s","age's","since’s"})foreach(string protectedMode in new[]{"unicode","balaram"})Check(core.CaretChoices(protectedWord,protectedMode).Length==0,"explicit English/collision caret protection");
Check(core.CaretChoices("raja","unicode").Contains("rāja")&&core.CaretChoices("raja","unicode").Contains("rājā"),"raja has both requested Unicode variants");
Check(core.CaretChoices("raja","balaram").Contains("räja")&&core.CaretChoices("raja","balaram").Contains("räjä"),"raja has both requested Balaram variants");
foreach(string mode in new[]{"unicode","balaram"})Check(core.CaretChoices("Krishna",mode).Contains("Krishna"),"original plain alias retained rather than canonical krsna");
foreach(string mode in new[]{"unicode","balaram"}){
 foreach(string input in new[]{"raja","rāja","rājā","räja","räjä"}){
  string[] choices=core.CaretChoices(input,mode);
  Check(choices.Contains("raja"),"plain option in caret cycle");
  Equal("raja",choices[choices.Length-1],"plain after accented spellings");
  Check(core.ClipboardChoices(input,mode,"auto").Contains("raja"),"plain option in copied cycle");
  Check(core.Cycle(input,mode=="balaram"?'<':'>').Contains("raja"),"plain option in suffix cycle");
 }
 foreach(string input in new[]{"Rāja","RĀJĀ","Räja","RÄJÄ"})Check(core.CaretChoices(input,mode).Contains(EncodingMap.Ascii(input)),"plain option preserves case");
 Check(core.CaretChoices("rāja’s",mode).Contains("raja’s"),"plain option preserves possessive");
 Check(core.ClipboardChoices("rāja","plain","auto").Distinct().Count()==core.ClipboardChoices("rāja","plain","auto").Length,"plain choices deduplicated");
}
foreach(string mode in new[]{"unicode","balaram"})foreach(string input in new[]{"raja","rāja","rājā","räja","räjä","kṛṣṇa","pratistha","brāhmaṇas","ayurveda","jñāna"}){
string[] options=core.CaretChoices(input,mode);if(options.Length<2)continue;var cycle=new CaretWordCycle();cycle.Start(options[0],options,mode,owner,focus);string current=options[0],next;
for(int i=1;i<=options.Length*100;i++){Check(cycle.Next(current,mode,owner,focus,out next),"repeat available through 100 complete loops");Equal(options[i%options.Length],next,"stable full cycle order including plain option");cycle.Commit(next);current=next;}
foreach(int failure in new[]{0,1,2,3}){cycle.Start(options[0],options,mode,owner,focus);Check(!cycle.Next(failure==0?"other":options[0],failure==1?(mode=="unicode"?"balaram":"unicode"):mode,failure==2?other:owner,failure==3?different:focus,out next),"changed context cancels word cycle");}
cycle.Start(options[0],options,mode,owner,null);Check(!cycle.Next(options[0],mode,owner,null,out next),"unidentified field cannot own cycle");
}
foreach(string wordValue in File.ReadLines(args[1]).Skip(1).Select(x=>x.Split('/')[0]).Where(x=>Regex.IsMatch(x,@"^[A-Za-z]+$")&&!Core.IndicLoanwords.Contains(x)).Distinct())foreach(string form in new[]{wordValue,char.ToUpperInvariant(wordValue[0])+wordValue.Substring(1),wordValue.ToUpperInvariant()})foreach(string mode in new[]{"unicode","balaram"})Check(core.CaretChoices(form,mode).Length==0,"English caret protection "+form+" "+mode);
}
foreach(string mode in new[]{"unicode","balaram"}){
 string stateFile=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])),"preference-test-artifacts","caret-"+Guid.NewGuid().ToString("N")+".tsv");
 string canonical=mode=="unicode"?"rāja":"räja",alternative=mode=="unicode"?"rājā":"räjä";
 for(int session=1;session<=3;session++)using(var core=new Core(args[0],stateFile)){
  Check(!core.RememberCaretSpelling("raja",canonical,mode,false),"initial plain conversion never votes");
  Check(core.RememberCaretSpelling(canonical,alternative,mode,false),"existing word first tap selects spelling");
  Equal("rāja",core.Convert("rāja","unicode"),"choice not promoted before completed session");core.FinishSpellingChoice();
  Equal(session==3?"rājā":"rāja",core.Convert("rāja","unicode"),"caret preference requires three sessions");
 }
 string cancelFile=stateFile+"-cancel";using(var core=new Core(args[0],cancelFile)){
  for(int n=0;n<5;n++){Check(core.RememberCaretSpelling(canonical,alternative,mode,false),"caret choice before undo");core.CancelSpellingChoice();core.FinishSpellingChoice();}
  Equal("rāja",core.Convert("rāja","unicode"),"undone caret choice does not learn");
  Check(!File.Exists(cancelFile),"undo leaves no preference state");
  Check(core.RememberCaretSpelling(canonical,alternative,mode,true),"alternative staged before plain choice");
  Check(core.RememberCaretSpelling(alternative,"raja",mode,true),"verified plain choice can train spelling");
  core.FinishSpellingChoice();
  Equal("rāja",core.Convert("rāja","unicode"),"plain selection cancels pending accented preference");
  Check(File.Exists(cancelFile),"plain selection records one pending vote");string plainState=File.ReadAllText(cancelFile);
  Check(core.RememberSpelling(canonical,alternative,mode,"auto"),"choice staged before original alias plain");
  Check(!core.RememberSpelling(alternative,"Rajah",mode,"auto"),"plain source alias cancels even outside recovered options");core.FinishSpellingChoice();
  Equal(plainState,File.ReadAllText(cancelFile),"invalid source alias cannot commit prior preference");
  Check(core.RememberCaretSpelling(canonical,alternative,mode,true),"caret choice staged before source alias plain");
  Check(!core.RememberCaretSpelling(alternative,"Rajah",mode,true),"caret plain alias cancels pending choice");core.FinishSpellingChoice();
  Equal(plainState,File.ReadAllText(cancelFile),"invalid caret alias cannot commit prior preference");
  Check(!core.RememberCaretSpelling("prana","prāṇa",mode,true),"English collision never votes");
 }
}
Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
}}}
