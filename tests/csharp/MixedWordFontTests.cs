using System;using System.Linq;
namespace KeyJaya {public static class MixedWordFontTests {
 static int passed,failed;static void Equal(string expected,string actual,string label){if(expected==actual)passed++;else{failed++;Console.WriteLine("FAIL "+label+" expected="+expected+" actual="+actual);}}
 public static int Main(string[] args){var fonts=new FontEncoding(null);
 foreach(string family in new[]{"ScaGoudy","ScaTimes","Balaram"}){
 Equal(family,MixedWordFonts.BalaramFamily("kṛṣṇa",new[]{"k","ṛṣṇ","a"},new[]{family,"Cambria",family},fonts),"fallback cluster "+family);
 Equal(family,MixedWordFonts.BalaramFamily("rāja",new[]{"r","ā","ja"},new[]{family,"Cambria",family},fonts),"single fallback "+family);
 Equal(family,MixedWordFonts.BalaramFamily("jñāna",new[]{"j","ñā","na"},new[]{family,"Cambria",family},fonts),"ambiguous Unicode nasal retains font evidence "+family);
 Equal(null,MixedWordFonts.BalaramFamily("raja",new[]{"r","a","ja"},new[]{family,"Cambria",family},fonts),"ordinary mixed fonts skipped "+family);
 Equal(null,MixedWordFonts.BalaramFamily("rāja",new[]{"r","ā","ja"},new[]{family,family,family},fonts),"uniform font unchanged "+family);
 }
 Equal(null,MixedWordFonts.BalaramFamily("rāja",new[]{"r","ā","ja"},new[]{"ScaGoudy","Cambria","ScaTimes"},fonts),"conflicting legacy families");
 Equal(null,MixedWordFonts.BalaramFamily("rāja",new[]{"r","ā","ja"},new[]{"Arial","Cambria","Arial"},fonts),"Unicode font still Unicode");
 Equal(null,MixedWordFonts.BalaramFamily("rāja guru",new[]{"r","ā","ja guru"},new[]{"ScaGoudy","Cambria","ScaGoudy"},fonts),"sentence formatting not flattened");
 Equal(null,MixedWordFonts.BalaramFamily("rājā",new[]{"r","ā","ja"},new[]{"ScaGoudy","Cambria","ScaGoudy"},fonts),"text proof exact");
 using(Core c=new Core(args[0]))foreach(string word in new[]{"kṛṣṇa","rāja","jñāna","śrīla","āyurveda"})Equal(Core.Target(word,"balaram"),c.Word(word,"balaram"),"preserve Unicode spelling while encoding "+word);
 Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;}
}}
