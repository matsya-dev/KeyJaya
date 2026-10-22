using System;using System.Linq;using System.Reflection;
namespace KeyJaya {public static class FontAutoSelectionTests {
 public static int Main(string[] args){int pass=0;using(var core=new Core(args[0])){var fonts=new FontEncoding(args[0]);foreach(string input in new[]{"varna and Krsna","Tamala Krsna Goswami"}){int split=input.IndexOf(' ')+1;var snapshot=new InDesignSelection.Snapshot{Owner=new IntPtr(100),Text=input,Font="Noto Serif",Fonts=Enumerable.Range(0,input.Length).Select(n=>n<split?"Noto Serif\tRegular":"ScaGoudy\tRegular").ToArray()};var edit=(SafeEdit)typeof(SafeEdit).GetMethod("InDesignEdit",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{snapshot,false,true});ItalicSpan[] spans;string result=edit.FontAutoOutput(core,fonts,out spans);string expected=input.StartsWith("Tamala")?"Tamāla "+EncodingMap.Balaram("Kṛṣṇa Goswami"):"varṇa and "+EncodingMap.Balaram("Kṛṣṇa");if(result!=expected)throw new Exception("per-font text "+result);pass++;if(input.StartsWith("Tamala")?spans.Length!=0:spans.Length!=1||spans[0].Start!=0||spans[0].Length!=5)throw new Exception("cross-font proper name/italic context");pass++;}
 if(Core.ReencodeCurrent("rājā","balaram")!="räjä"||Core.ReencodeCurrent("räjä","unicode")!="rājā")throw new Exception("encoding changed spelling");pass++;
 }Console.WriteLine("PASS: "+pass+" FAIL: 0");return 0;}
}}

