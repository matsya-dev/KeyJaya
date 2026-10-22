using System;
namespace KeyJaya {public static class ResidentReplacementTests {
 public static int Main(string[] args){int passed=0,failed=0;Action<bool,string> check=(ok,name)=>{if(ok)passed++;else{failed++;Console.WriteLine("FAIL "+name);}};
 string root=@"C:\Users\reader\AppData\Roaming\KeyJaya",legacy=@"C:\Users\reader\AppData\Roaming\espanso\keyjaya";
 check(ResidentReplacement.Owns(root+@"\engine\keyjaya-app.exe","KeyJaya",1,1,root,legacy),"installed app stopped");
 check(ResidentReplacement.Owns(@"C:\Documents\Codex\work\live175\engine\keyjaya-app.exe","KeyJaya",1,1,root,legacy),"stale preview stopped outside installation folder");
 check(ResidentReplacement.Owns(legacy+@"\engine\keyjaya-app.exe","",1,1,root,legacy),"legacy app stopped without version metadata");
 check(!ResidentReplacement.Owns(root+@"\engine\keyjaya-app.exe","KeyJaya",2,1,root,legacy),"another Windows session untouched");
 check(!ResidentReplacement.Owns(@"C:\other\keyjaya-app.exe","Other product",1,1,root,legacy),"same filename with different product untouched");
 check(!ResidentReplacement.Owns(@"C:\other\other-app.exe","KeyJaya",1,1,root,legacy),"another executable name untouched");
 check(!ResidentReplacement.Owns(root+@"-backup\engine\keyjaya-app.exe","",1,1,root,legacy),"folder-prefix collision untouched");
 check(!ResidentReplacement.Owns(null,"KeyJaya",1,1,root,legacy),"unidentified process untouched");
 check(ResidentReplacement.Owns(root.ToUpperInvariant()+@"\engine\keyjaya-app.exe","",1,1,root,legacy),"Windows case-insensitive installed path");
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
 }
}}
