using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Diagnostics;using System.Text.RegularExpressions;
namespace KeyJaya {
public static class StringentTests {
 static long passed,failed;static List<string> failures=new List<string>();
 static void Check(bool ok,string label){if(ok)passed++;else{failed++;if(failures.Count<1000)failures.Add(label);}}
 public static int Main(string[] args){var timer=Stopwatch.StartNew();using(Core c=new Core(args[0])){
  string[] words=File.ReadLines(args[1]).Skip(1).Select(x=>x.Split('/')[0]).Where(x=>Regex.IsMatch(x,@"^[\p{L}]+$")&&!Core.IndicLoanwords.Contains(x)).Distinct().ToArray();
  Console.WriteLine("Independent lowercase English headwords: "+words.Length);
  foreach(string word in words){foreach(string form in new[]{word,char.ToUpperInvariant(word[0])+word.Substring(1),word.ToUpperInvariant(),word+"'s",word+"’s"}){
   foreach(string mode in new[]{"unicode","balaram","plain","itrans"}){
    string actual=c.Word(form,mode);Check(actual==form,"English Word "+form+" "+mode+" -> "+actual);
    string sentence="Before: ("+form+"), after.";actual=c.Convert(sentence,mode);Check(actual==sentence,"English prose "+form+" "+mode+" -> "+actual);
   }
   foreach(char suffix in new[]{'>','<','?'}){string input=form+suffix;string stem=form.EndsWith("'s")||form.EndsWith("’s")?form.Substring(0,form.Length-2):form;if(!c.Short.ContainsKey(input)&&!Core.ExplicitIndicTriggers.Contains(stem)){string actual=c.Trigger(input);Check(actual==null,"English trigger "+input+" -> "+actual);}}
  }}
  string[] prefixes={"", "an age since ","older Sanskrit kṛṣṇa ","keep this exactly\r\n", "previous\t", "emoji 😀 ","combining e\u0301 ","punctuation,", "before.","[", "\"",new string('x',500)+" "};
  foreach(string prefix in prefixes)foreach(string trigger in new[]{"Govind>","gana>","gana<","HT>","HT<","HT?","BG9.1>","CC1.1.1VT>"}){
   InputState state=new InputState();string candidate=null;foreach(char ch in prefix+trigger){string captured=state.Type(ch);if(captured!=null)candidate=captured;}
   Check(candidate==trigger,"current-token isolation prefix="+prefix+" candidate="+candidate);
  }
  // Mutation of a whole input cannot silently bypass the anchored word/scripture grammar.
  foreach(string trigger in new[]{"Govind>","gana<","HT>","BG9.1>"})foreach(string junk in new[]{"old ","old\n","old\t","old,","old-","1","😀","abc"})if(junk=="old-"&&(trigger=="Govind>"||trigger=="gana<"))Check(c.Trigger(junk+trigger)=="old-"+c.Word(trigger.Substring(0,trigger.Length-1),trigger.EndsWith("<")?"balaram":"unicode"),"compound preserves English prefix "+junk+trigger);else Check(c.Trigger(junk+trigger)==null,"whole-prefix trigger rejected "+junk+trigger);
  Random rng=new Random(18521);string alphabet="abcdefghijklmnopqrstuvwxyz0123456789";
  for(int i=0;i<20000;i++){
   string prefix=new string(Enumerable.Range(0,rng.Next(1,240)).Select(j=>alphabet[rng.Next(alphabet.Length)]).ToArray());InputState state=new InputState();
   foreach(char ch in prefix)state.Type(ch);long oldGeneration=state.Generation;state.Clear();Check(state.Generation!=oldGeneration,"context reset invalidates pending edit "+i);string candidate=null;foreach(char ch in "Govind>"){string captured=state.Type(ch);if(captured!=null)candidate=captured;}
   Check(candidate=="Govind>","cursor/focus reset removes previous token "+i);
   state=new InputState();foreach(char ch in prefix+" Govind")state.Type(ch);Check(state.Type('>')=="Govind>","random whitespace prefix isolation "+i);
   state=new InputState();foreach(char ch in new string('a',161)+"Govind")state.Type(ch);Check(state.Type('>')==null,"overflow cannot convert tail "+i);
  }
  foreach(string input in new[]{"e\u0301gana>","unknown\u0301Govind>","a\u093eGovind>"}){InputState state=new InputState();string candidate=null;foreach(char ch in input){string x=state.Type(ch);if(x!=null)candidate=x;}Check(candidate==input,"combining mark cannot match a trailing fragment "+input);}
  // Exercise the exact production native range guard, including duplicate words and stale selections.
  Random rangeRng=new Random(91463);
  for(int i=0;i<100000;i++){
   string prefix="untouched "+i+" "+new string('x',rangeRng.Next(0,180))+" ";string trigger=i%3==0?"BG9.1>":i%3==1?"Govind>":"HT<";
   string tail=" preceding-looking Govind> stays after caret";string document=prefix+trigger+tail;int caret=prefix.Length+trigger.Length,begin;
   Check(EditGuard.Locate(document,caret,caret,trigger,out begin)&&begin==prefix.Length,"exact trigger range "+i);
   string output=c.Trigger(trigger);string result=document.Substring(0,begin)+output+document.Substring(caret);
   Check(result.StartsWith(prefix,StringComparison.Ordinal)&&result.EndsWith(tail,StringComparison.Ordinal),"both sides preserved "+i);
   Check(!EditGuard.Locate(document,caret-1,caret-1,trigger,out begin),"moved caret rejects stale replacement "+i);
   Check(!EditGuard.Locate(document,caret-1,caret,trigger,out begin),"selected text rejects replacement "+i);
   Check(!EditGuard.Locate(document,caret,caret,trigger+"x",out begin),"changed expected text rejected "+i);
   Check(!EditGuard.Locate(document,caret,caret,"X"+trigger.Substring(1),out begin),"same-length changed expected word rejected "+i);
   Check(!EditGuard.Locate(document,caret,caret,"old "+trigger,out begin),"preceding text cannot extend owned range "+i);
  }
  foreach(int position in new[]{-1,0,1,999,int.MaxValue}){int begin;Check(!EditGuard.Locate("text",position,position,"Govind>",out begin),"invalid caret fails closed "+position);}
  {int begin;Check(!EditGuard.Locate("old\r\nHT>",8,8,"old\nHT>",out begin),"CRLF units never normalized for native selection");}
  foreach(string prefix in new[]{"old","abc-","word_","word'","word’","domain.","path/","path\\","mail@","e\u0301"})foreach(string word in new[]{"Govind>","HT>","doesnt ","teh ","rāja"}){int begin;string document=prefix+word;Check(!EditGuard.Locate(document,document.Length,document.Length,word,out begin),"midword/reset fragment rejected "+document);}
  foreach(string suffix in new[]{"abc","-tail","_tail","’s",".com","/tail","@host"}){int begin;Check(!EditGuard.Locate("rāja"+suffix,4,4,"rāja",out begin),"following fragment cannot be edited "+suffix);}
  {int begin;Check(EditGuard.Locate("old word rāja",13,13,"rāja",out begin)&&begin==9,"whole word boundary remains valid");}
  for(int i=1;i<=5000;i++){
   int[] captured={42,i,193};Check(EditGuard.SameFocus(captured,new[]{42,i,193}),"same focused field "+i);
   Check(!EditGuard.SameFocus(captured,new[]{42,i+1,193}),"other field in same window "+i);
   Check(!EditGuard.SameFocus(captured,new[]{43,i,193}),"other window "+i);
   Check(!EditGuard.SameFocus(captured,new[]{42,i}),"provider identity prefix is insufficient "+i);
  }
  Check(!EditGuard.SameFocus(null,new[]{1}),"missing capture fails closed");Check(!EditGuard.SameFocus(new[]{1},null),"missing focus fails closed");Check(!EditGuard.SameFocus(new int[0],new int[0]),"empty provider IDs fail closed");
  // Case collisions with short triggers are intentional only for exact registered shortcuts.
  int collisions=words.SelectMany(x=>new[]{x,char.ToUpperInvariant(x[0])+x.Substring(1),x.ToUpperInvariant()}).SelectMany(x=>new[]{x+">",x+"<",x+":"}).Count(c.Short.ContainsKey);
  var collisionRows=words.SelectMany(x=>new[]{x,char.ToUpperInvariant(x[0])+x.Substring(1),x.ToUpperInvariant()}).SelectMany(x=>new[]{x+">",x+"<",x+":"}).Where(c.Short.ContainsKey).Distinct().Select(x=>x+"\t"+c.Short[x]);File.WriteAllLines(Path.Combine(Path.GetDirectoryName(args[1]),"explicit-short-collisions.tsv"),collisionRows,Encoding.UTF8);
  Console.WriteLine("Explicit short-trigger/English collisions (excluded from no-trigger oracle): "+collisions);
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed+" TOTAL_MS: "+timer.ElapsedMilliseconds);foreach(string f in failures)Console.WriteLine("FAIL "+f);return failed==0?0:1;
 }
}
}

