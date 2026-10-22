using System;
using System.Reflection;
using System.Runtime.InteropServices;
namespace KeyJaya { public static class NativeKeyTests {
 static int passed,failed;
 static void Check(bool ok,string message){if(ok)passed++;else{failed++;Console.WriteLine("FAIL "+message);}}
 static Native.KeyboardInput Make(ushort key,bool up){return ((Native.Input)typeof(Native).GetMethod("Key",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{key,up})).union.keyboard;}
 public static int Main(string[] args){
  Check(Marshal.SizeOf(typeof(Native.Input))==40,"Windows x64 INPUT ABI");
  foreach(ushort key in new ushort[]{0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x2D,0x2E})foreach(bool up in new[]{false,true}){
   var input=Make(key,up);Check((input.flags&1)!=0,"navigation uses extended-key identity");Check(input.scan!=0,"navigation scan code");Check(input.vk==key,"navigation virtual key");Check(((input.flags&2)!=0)==up,"navigation direction");Check(input.extra.ToUInt64()==Native.OwnInput,"self-input tag");
  }
  foreach(ushort key in new ushort[]{0x10,0x11,0x12,8,0x56})foreach(bool up in new[]{false,true}){
   var input=Make(key,up);Check((input.flags&1)==0,"ordinary left modifiers/keys are not extended");Check(input.scan!=0,"ordinary scan code");Check(((input.flags&2)!=0)==up,"ordinary direction");Check(input.extra.ToUInt64()==Native.OwnInput,"ordinary self-input tag");
  }
  foreach(string letter in new[]{"ā","ä","ṣ","ś","a","ṝ","ḷ"}){var inputs=Native.UnicodeCharacter(letter);Check(inputs.Length==2,"single character event count");for(int i=0;i<2;i++){var key=inputs[i].union.keyboard;Check(inputs[i].type==1&&key.vk==0&&key.scan==letter[0]&&key.flags==(i==0?4:6)&&key.extra.ToUInt64()==Native.OwnInput,"direct Unicode character without paste or spaces");}}
  foreach(string invalid in new[]{null,"","ab","😀"}){bool rejected=false;try{Native.UnicodeCharacter(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"reject invalid single-character insertion");}
  foreach(string text in new[]{"rāja","räjä","brāhmaṇas","Aśaucam","kṛṣṇa"}){var data=Native.UnicodeText(text);Check(data.Length==text.Length*2,"word input length");for(int i=0;i<data.Length;i++){var key=data[i].union.keyboard;Check(key.vk==0&&key.scan==text[i/2]&&key.flags==(i%2==0?4:6)&&key.extra.ToUInt64()==Native.OwnInput,"word Unicode event identity");}}
  foreach(string invalid in new[]{null,"","😀","hello\r\nworld",new string('a',161)})Check(Native.UnicodeText(invalid)==null,"invalid word payload rejected");
  foreach(int delta in new[]{-192,-32,-1,1,32,160,192}){var move=Native.CaretNavigation(delta);Check(move.Length==Math.Abs(delta)*2,"bounded caret movement event count");foreach(var entry in move){var key=entry.union.keyboard;Check(key.vk==(delta<0?0x25:0x27)&&(key.flags&1)!=0&&key.extra.ToUInt64()==Native.OwnInput,"owned extended left/right navigation preserves selection modifiers");}}
  foreach(int invalid in new[]{int.MinValue,-193,0,193,int.MaxValue})Check(Native.CaretNavigation(invalid)==null,"invalid navigation rejected before any input");
  Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
 }
} }
