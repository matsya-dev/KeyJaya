using System;using System.Linq;
namespace KeyJaya {public static class CompoundCaretTests {
 static int passed;static void Check(bool ok,string label){if(!ok)throw new Exception(label);passed++;}
 public static int Main(string[] args){using(Core core=new Core(args[0])){
  foreach(string separator in new[]{"-","\u2011","\u2013"})foreach(string prefix in new[]{"","Earlier untouched ","Earlier paragraph.\r\n("})foreach(string tail in new[]{"",",",",)","?!"}){
   string compound="Vyasa"+separator+"puja",text=prefix+compound+tail+" next";
   for(int offset=0;offset<=compound.Length+tail.Length;offset++){
    int caret=prefix.Length+offset;var span=CaretWords.Locate(text,caret,caret);string expected;int start,end;
    if(offset<=5){expected="Vyasa";start=prefix.Length;end=start+5;}else if(offset<10||offset>10){expected="puja";start=prefix.Length+6;end=start+4;}else{expected=compound;start=prefix.Length;end=start+10;}
    Check(span!=null&&span.Word==expected&&span.Start==start&&span.End==end,"exact component/whole target offset="+offset+" separator="+separator);
    string converted=core.Word(span.Word,"unicode"),changed=text.Substring(0,span.Start)+converted+text.Substring(span.End);
    string wanted=text.Substring(0,start)+converted+text.Substring(end);Check(changed==wanted,"only intended range changed");
    int restoredCaret=start+span.Offset;Check(restoredCaret==caret,"original caret saved independently of target length");
    if(offset>10)Check(span.Tail==tail.Substring(0,offset-10),"punctuation belongs to caret only, not replaced text");
   }
   var component=CaretWords.Locate(text,prefix.Length+10,prefix.Length+10,true);Check(component!=null&&component.Word=="puja","internal replacement positioning can locate final component at collapsed caret");
  }
  foreach(string invalid in new[]{"https://example.com/Vyasa-puja","person@Vyasa-puja","C:\\Vyasa-puja","#Vyasa-puja","Vyasa-puja ","Vyasa--puja"})Check(CaretWords.Locate(invalid,invalid.Length,invalid.Length)==null,"unsafe boundary rejected "+invalid);
  var state=new InputState();string trigger=null;foreach(char c in "Earlier untouched Vyasa\u2011puja>"){string next=state.Type(c);if(next!=null)trigger=next;}Check(trigger=="Vyasa\u2011puja>","nonbreaking-hyphen physical trigger stays whole");
  var cycle=new CaretWordCycle();var owner=new IntPtr(2);int[] focus={5};cycle.Start("pūjā",new[]{"pūjā","puja"},"unicode",owner,focus,"puja");string nextChoice;for(int n=0;n<20;n++){Check(cycle.Next(cycle.Expected(owner,focus),"unicode",owner,focus,out nextChoice),"component cycle remains continuous");cycle.Commit(nextChoice);Check(cycle.Original=="puja","component undo origin never changes");}
  Check(core.CaretChoices("puja","unicode").Contains("pūjā"),"isolated puja component has diacritics");
  Check(core.Word("Vyasa","unicode")=="Vyāsa","isolated Vyasa component has diacritics");
  string[,] reference={{"vyasa-puja","Vyāsa-pūjā"},{"vaidhi-bhakti","vaidhī-bhakti"},{"sac-cid-ananda-vigraha","sac-cid-ānanda-vigraha"},{"nama-sankirtana","nāma-saṅkīrtana"},{"varnaśrama-dharma","varṇāśrama-dharma"},{"navadvipa-dhama","Navadvīpa-dhāma"},{"founder-acarya","founder-ācārya"}};
  for(int n=0;n<reference.GetLength(0);n++)Check(core.ConvertCopied(reference[n,0],"unicode")==reference[n,1],"independent book spelling "+reference[n,0]);
 }
 Console.WriteLine("PASS: "+passed+" FAIL: 0");return 0;
 }
}}
