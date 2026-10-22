using System;
namespace KeyJaya {public static class ScriptLanguageDetectionTests {
 static int passed,failed;static void Equal(string expected,string actual,string label){if(expected==actual)passed++;else{failed++;Console.WriteLine("FAIL "+label+" expected="+expected+" actual="+actual);}}
 public static int Main(string[] args){
 Equal("False",Hindi.UseHindiReading("श्रीकृष्णाय","यह श्रीकृष्णाय है",3,x=>false,x=>true).ToString(),"Sanskrit dative survives Hindi context");
 Equal("False",Hindi.UseHindiReading("राम",null,0,x=>true,x=>true).ToString(),"shared isolated word stays Sanskrit");
 Equal("True",Hindi.UseHindiReading("राम","राम यहाँ है",0,x=>true,x=>true).ToString(),"Hindi sentence evidence");
 Equal("False",Hindi.UseHindiReading("राम","राम उवाच",0,x=>true,x=>true).ToString(),"Sanskrit sentence evidence");
 Equal("False",Hindi.UseHindiReading("राम","राम। यहाँ है",0,x=>true,x=>true).ToString(),"context does not cross sentence boundary");
 Equal("False",Hindi.UseHindiReading("राम","राम\nयहाँ है",0,x=>true,x=>true).ToString(),"context does not cross paragraph boundary");
 Equal("True",Hindi.UseHindiReading("कमरा",null,0,x=>false,x=>true).ToString(),"Hindi-only dictionary evidence");
 Equal("False",Hindi.UseHindiReading("अपरिचित",null,0,x=>false,x=>false).ToString(),"unknown word not guessed Hindi");
 using(Core c=new Core(args[0]))foreach(string language in new[]{"hindi","sanskrit"}){
 c.ScriptOptions.SetLanguage(language);c.ScriptOptions.Set(false,false);
 foreach(string mode in new[]{"unicode","balaram","plain","itrans"}){
 Equal(Core.Target("śrīkṛṣṇāya",mode),c.ConvertCopied("श्रीकृष्णाय",mode),"reported dative "+language+" "+mode);
 Equal(Core.Target("rāma",mode),c.ConvertCopied("राम",mode),"ambiguous isolated word "+language+" "+mode);
 Equal(Core.Target("rām yahām̐ hai",mode),c.ConvertCopied("राम यहाँ है",mode),"clear Hindi clause "+language+" "+mode);
 Equal(Core.Target("rāma uvāca",mode),c.ConvertCopied("राम उवाच",mode),"clear Sanskrit clause "+language+" "+mode);
 Equal(Core.Target("kṛṣṇāy",mode),c.ConvertCopied("कृष्णाय्",mode),"explicit virama "+language+" "+mode);
 }
 c.ScriptOptions.Set(true,false);Equal("rāma yahām̐ hai",c.ConvertCopied("राम यहाँ है","unicode"),"precise mode keeps written vowels");
 }
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}}
