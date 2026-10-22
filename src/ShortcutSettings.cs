using System;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Collections.Generic;
namespace KeyJaya {
public sealed class ShortcutBinding {
 public readonly string Text,Tap;public readonly uint Key,Modifiers;
 public ShortcutBinding(string text,bool allowTap){
  text=(text??"").Trim();if(text.Equals("Left Ctrl",StringComparison.OrdinalIgnoreCase)||text.Equals("Right Ctrl",StringComparison.OrdinalIgnoreCase)){if(!allowTap)throw new InvalidDataException("Ctrl-tap keys are for letter or word actions only.");Tap=text.StartsWith("Left",StringComparison.OrdinalIgnoreCase)?"left":"right";Text=Tap=="left"?"Left Ctrl":"Right Ctrl";return;}
  string[] pieces=text.Split('+');uint modifiers=0;for(int n=0;n<pieces.Length-1;n++){uint flag=pieces[n].Trim().Equals("Ctrl",StringComparison.OrdinalIgnoreCase)?2u:pieces[n].Trim().Equals("Alt",StringComparison.OrdinalIgnoreCase)?1u:pieces[n].Trim().Equals("Shift",StringComparison.OrdinalIgnoreCase)?4u:0u;if(flag==0||(modifiers&flag)!=0)throw new InvalidDataException("Use Ctrl, Alt, Shift and one key.");modifiers|=flag;}
  string key=pieces[pieces.Length-1].Trim().ToUpperInvariant();uint vk;string label;
  if(key.Length==1&&((key[0]>='A'&&key[0]<='Z')||(key[0]>='0'&&key[0]<='9'))){vk=key[0];label=key;}
  else if(Regex.IsMatch(key,@"^F(?:[1-9]|1[0-9]|2[0-4])$")){vk=(uint)(0x70+int.Parse(key.Substring(1))-1);label=key;}
  else{string[] names={".",",","/","\\","[","]","`"};uint[] values={0xBE,0xBC,0xBF,0xDC,0xDB,0xDD,0xC0};int at=Array.IndexOf(names,key=="BACKTICK"?"`":key);if(at<0)throw new InvalidDataException("Use a letter, number, F1–F24, or . , / \\ [ ] `.");vk=values[at];label=names[at];}
  bool function=vk>=0x70&&vk<=0x87;if((modifiers&3)==0&&(!function||modifiers!=0)&&!(allowTap&&vk==0xC0&&modifiers==0))throw new InvalidDataException("Use Ctrl or Alt with ordinary keys; bare function keys are also allowed.");
  if(modifiers==2&&"ACVXZ".Contains(label))throw new InvalidDataException("Ctrl+A/C/V/X/Z are reserved for normal editing.");if((vk==0x73&&(modifiers&1)!=0)||vk==0x7B)throw new InvalidDataException("This shortcut is reserved by Windows or Word.");
  Key=vk;Modifiers=modifiers;Text=((modifiers&2)!=0?"Ctrl+":"")+((modifiers&1)!=0?"Alt+":"")+((modifiers&4)!=0?"Shift+":"")+label;
 }
 public string Identity {get{return Tap==null?Modifiers+":"+Key:"tap:"+Tap;}}
 public bool Matches(uint key,bool ctrl,bool shift,bool alt,bool win){return Tap==null&&!win&&Key==key&&Modifiers==((ctrl?2u:0u)|(shift?4u:0u)|(alt?1u:0u));}
}
public sealed class ShortcutSettings {
 public static readonly string[] Roles={"unicode","balaram","plain","itrans","devanagari","bengali","letter","word"};
 public static readonly string[] Labels={"Unicode / IAST","Balarama / Legacy","Plain Roman text","Hindi / ITRANS","Sanskrit Devanagari","Bengali script","Single letter","Word / compound / short trigger"};
 public static readonly string[] Defaults={"Ctrl+.","Ctrl+,","Ctrl+/","Ctrl+\\","Ctrl+[","Ctrl+]","`","Left Ctrl"};
 readonly string path;readonly Dictionary<string,ShortcutBinding> bindings=new Dictionary<string,ShortcutBinding>();
 public ShortcutBinding this[string role]{get{return bindings[role];}}
 public ShortcutSettings(string file,IDictionary<string,string> custom=null){path=file;var texts=Roles.Select((role,n)=>new{role,value=Defaults[n]}).ToDictionary(x=>x.role,x=>x.value);if(file!=null&&File.Exists(file))foreach(string line in File.ReadAllLines(file)){if(string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;string[] cells=line.Split(new[]{'='},2);if(cells.Length!=2||!texts.ContainsKey(cells[0].Trim()))throw new InvalidDataException("Invalid shortcuts.ini entry.");texts[cells[0].Trim()]=cells[1].Trim();}if(custom!=null)foreach(var row in custom){if(!texts.ContainsKey(row.Key))throw new InvalidDataException("Unknown shortcut action.");texts[row.Key]=row.Value;}
  var used=new HashSet<string>();foreach(string role in Roles){var binding=new ShortcutBinding(texts[role],role=="letter"||role=="word");if(!used.Add(binding.Identity))throw new InvalidDataException("Two actions cannot use "+binding.Text+".");bindings.Add(role,binding);}
 }
 public string Mode(uint key,bool ctrl,bool shift,bool alt,bool win){foreach(string role in Roles)if(bindings[role].Matches(key,ctrl,shift,alt,win))return role;return null;}
 public string TapRole(uint key,uint flags){string side=LeftControlTap.IsLeftControl(key,flags)?"left":LeftControlTap.IsRightControl(key,flags)?"right":null;if(side==null)return null;foreach(string role in Roles)if(bindings[role].Tap==side)return role;return null;}
 public void Save(){if(path==null)throw new InvalidOperationException("No shortcut settings path.");Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp-"+Guid.NewGuid().ToString("N");try{File.WriteAllLines(temporary,new[]{"# KeyJaya shortcuts"}.Concat(Roles.Select(role=>role+"="+bindings[role].Text)),new UTF8Encoding(false));SettingsFile.Commit(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
}
}
