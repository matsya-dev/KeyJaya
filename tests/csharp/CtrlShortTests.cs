using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace KeyJaya {public static class CtrlShortTests {
 static int passed,failed;static void Equal(string expected,string actual,string name){if(expected==actual)passed++;else{failed++;Console.WriteLine("FAIL "+name+" expected="+expected+" actual="+actual);}}
 public static int Main(string[] args){
 using(Core c=new Core(args[0])){
 var active=c.ActiveShortTriggers();foreach(var row in active){if(row.Key.Length<2)continue;char suffix=row.Key[row.Key.Length-1];if(suffix!='>'&&suffix!='<'&&suffix!='?')continue;string stem=row.Key.Substring(0,row.Key.Length-1);if(!Core.CtrlOnlyShort(stem))continue;string mode=suffix=='<'?"balaram":suffix=='?'?"plain":"unicode";Equal(Configuration.Expand(row.Value,DateTime.Now),c.CtrlShort(stem,mode),"existing YAML Ctrl alias "+row.Key);Equal(c.Trigger(row.Key),c.CtrlShort(stem,mode),"explicit trigger preserved "+row.Key);}
 Equal(null,c.CtrlShort("sp","unicode"),"case sensitive");Equal(null,c.CtrlShort("SPG extra","unicode"),"whole selection exact");Equal(null,c.CtrlShort("SP!","unicode"),"no punctuation match");Equal(null,c.CtrlShort("age","unicode"),"English no expansion");Equal(c.Word("raja","unicode"),c.ConvertHighlighted("raja","unicode"),"ordinary word fallback respects current preference");
 }
 string root=Path.Combine(Path.GetDirectoryName(typeof(CtrlShortTests).Assembly.Location),"ctrl-short-fixture");Directory.CreateDirectory(Path.Combine(root,"data"));Directory.CreateDirectory(Configuration.Folder(root));foreach(string file in Directory.GetFiles(Path.Combine(args[0],"data")))if(!File.Exists(Path.Combine(root,"data",Path.GetFileName(file))))File.Copy(file,Path.Combine(root,"data",Path.GetFileName(file)));
 Configuration.Write(Configuration.Short(root),new Dictionary<string,string>{{"SP>","Śrīla Prabhupāda"},{"SP<","Çréla Prabhupäda"},{"SP?","Srila Prabhupada"},{"SPG","Supreme Personality of Godhead"},{"KJ","Kṛṣṇa"},{"ZZ>","Custom exact output"}});
 using(Core c=new Core(root)){
 Equal("Śrīla Prabhupāda",c.CtrlShort("SP","unicode"),"Unicode SP");Equal("Çréla Prabhupäda",c.CtrlShort("SP","balaram"),"Balaram SP");Equal("Srila Prabhupada",c.CtrlShort("SP","plain"),"plain SP");
 foreach(string mode in new[]{"unicode","balaram","plain"})Equal("Supreme Personality of Godhead",c.CtrlShort("SPG",mode),"single roman replacement "+mode);
 Equal("kåñëa",c.CtrlShort("KJ","balaram").ToLowerInvariant(),"single Unicode replacement encodes");Equal("Custom exact output",c.ConvertHighlighted("ZZ","unicode"),"selected alias precedence");Equal(null,c.CtrlShort("ZZ","balaram"),"deleted variant not inferred");Equal(null,c.Trigger("SPG"),"bare trigger requires Ctrl");Equal("False",c.IsDirectTrigger("SPG").ToString(),"bare not automatic");Equal("True",c.IsDirectTrigger("SP>").ToString(),"suffix automatic");
 }
 var span=CaretWords.Locate("Earlier untouched SPG",21,21);Equal("SPG",span==null?null:span.Word,"current token boundary");var number=CaretWords.Locate("4REGS",5,5);Equal("4REGS",number==null?null:number.Word,"numeric abbreviation");
 var ctrl=new LeftControlTap(); // Existing modifier guard is exercised by the font-cycling suite.
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
 }
}}
