using System;
using System.Reflection;
using System.Runtime.InteropServices;
namespace KeyJaya { public static class NativeKeyTests {
 static int passed,failed;
 static void Check(bool ok,string message){if(ok)passed++;else{failed++;Console.WriteLine("FAIL "+message);}}
 static Native.KeyboardInput Make(ushort key,bool up){return ((Native.Input)typeof(Native).GetMethod("Key",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{key,up})).union.keyboard;}
 static void FieldNavigation(int length,int start,int end,bool useControl=true){
  var keys=Native.FieldRangeKeys(start,end,length,useControl);int caret=length/3,anchor=caret;bool control=false,shift=false,sawControl=false;
  foreach(var input in keys){var key=input.union.keyboard;bool up=(key.flags&2)!=0;if(key.vk==0xA2){control=!up;sawControl=true;continue;}if(key.vk==0xA0){shift=!up;if(shift)anchor=caret;continue;}if(up)continue;caret=key.vk==0x24?0:key.vk==0x23?length:key.vk==0x25?Math.Max(0,caret-1):key.vk==0x27?Math.Min(length,caret+1):caret;if(!shift)anchor=caret;}
  Check(Math.Min(anchor,caret)==start&&Math.Max(anchor,caret)==end,"Excel field navigation selects exactly the requested characters");Check(!control&&!shift,"Excel field navigation releases both modifiers");Check(keys.Length<=4+2*end+(end>start?2:0),"Excel field navigation remains bounded");
  Check(sawControl==useControl,"Single-line Excel navigation omits spreadsheet Ctrl+Home/End commands");
 }
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
  for(int length=0;length<=80;length++)for(int start=0;start<=length;start++)for(int end=start;end<=length;end++)foreach(bool control in new[]{false,true})FieldNavigation(length,start,end,control);
  var random=new Random(192);for(int n=0;n<10000;n++){int length=random.Next(4097),start=random.Next(length+1),end=random.Next(start,length+1);FieldNavigation(length,start,end);FieldNavigation(length,start,end,false);}
  Check(Native.FieldRangeKeys(-1,2,3)==null&&Native.FieldRangeKeys(0,4,3)==null&&Native.FieldRangeKeys(0,0,4097)==null,"invalid Excel field ranges generate no input");
  Console.WriteLine("PASS: "+passed+" FAIL: "+failed);return failed==0?0:1;
 }
} }
