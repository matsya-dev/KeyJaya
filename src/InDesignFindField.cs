using System;using System.Linq;using System.Text;using System.Threading;using System.Windows.Forms;using System.Reflection;using System.Runtime.InteropServices;using Microsoft.Win32;
namespace KeyJaya {
// No document DOM is used here. The field must prove its entire text matches
// a native Find/Change preference before any conversion may replace a fragment.
public static class InDesignFindField {
 public static Action<string> Trace;static int epoch;static Snapshot cached;static readonly object gate=new object();
 public static void Invalidate(){Interlocked.Increment(ref epoch);cached=null;}
 public static bool Focused(IntPtr owner){if(owner==IntPtr.Zero||owner!=Native.GetForegroundWindow())return false;uint pid;Native.GetWindowThreadProcessId(owner,out pid);if(!Native.ProcessImageIs(pid,"InDesign.exe"))return false;var title=new StringBuilder(128);Native.GetWindowText(owner,title,title.Capacity);string value=title.ToString().Replace("&","").Replace(" ","");return value.Equals("Find/Change",StringComparison.OrdinalIgnoreCase);}
 static string[] Preferences(IntPtr owner){if(!Focused(owner))return null;object app=null;try{string prog="InDesign.Application";using(var key=Registry.ClassesRoot.OpenSubKey(prog+"\\CurVer")){if(key!=null)prog=key.GetValue(null) as string??prog;}var type=Type.GetTypeFromProgID(prog);if(type==null)return null;app=Activator.CreateInstance(type);string script="(function(){var names=['findTextPreferences','changeTextPreferences','findGrepPreferences','changeGrepPreferences'],props=['findWhat','changeTo','findWhat','changeTo'],a=[];for(var n=0;n<4;n++)a.push(encodeURIComponent(String(app[names[n]][props[n]])));return a.join('|');})()";string raw=app.GetType().InvokeMember("DoScript",BindingFlags.InvokeMethod,null,app,new object[]{script,1246973031}) as string;if(raw==null)return null;var rows=raw.Split('|');return rows.Length==4?rows.Select(Uri.UnescapeDataString).ToArray():null;}catch{return null;}finally{if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);}}
 static T Sta<T>(Func<T> action){if(Thread.CurrentThread.GetApartmentState()==ApartmentState.STA){try{return action();}catch(Exception ex){if(Trace!=null)Trace("Find field skipped: "+ex.GetType().Name);return default(T);}}T result=default(T);var worker=new Thread(()=>{try{result=action();}catch(Exception ex){if(Trace!=null)Trace("Find field skipped: "+ex.GetType().Name);}}){IsBackground=true};worker.SetApartmentState(ApartmentState.STA);worker.Start();while(!worker.Join(10)){}return result;}
 static bool Same(string[] a,string[] b){return a!=null&&b!=null&&a.SequenceEqual(b);}
 sealed class Probe:IDisposable {
  readonly Native.ClipboardReference backup;readonly Form host=new Form();readonly IntPtr owner;readonly int stamp;uint last;bool touched;
  public Probe(IntPtr window,int generation){owner=window;stamp=generation;var clock=System.Diagnostics.Stopwatch.StartNew();backup=Native.ClipboardReference.Capture();if(Trace!=null)Trace("Find clipboard backup ms="+clock.ElapsedMilliseconds);if(backup==null)throw new InvalidOperationException("Clipboard unavailable");last=backup.Sequence;}
  public bool Current{get{bool valid=epoch==stamp&&Focused(owner);if(!valid&&Trace!=null){var kind=new StringBuilder(80);Native.GetClassName(Native.GetForegroundWindow(),kind,kind.Capacity);Trace("Find probe context changed epoch="+(epoch==stamp)+" window="+(Native.GetForegroundWindow()==owner)+" foregroundClass="+kind.ToString());}return valid;}}
  public string Copy(){var clock=System.Diagnostics.Stopwatch.StartNew();if(!Current)return null;string marker="KeyJaya-"+Guid.NewGuid().ToString("N");uint written;if(!Native.ExpectedTemporaryClipboard(host.Handle,marker,last,out written))return null;last=written;touched=true;if(!Current||!Native.Chord(0x43,true))return null;bool changed=false;while(clock.ElapsedMilliseconds<250){Application.DoEvents();Thread.Sleep(5);if(!Current)return null;uint sequence=Native.GetClipboardSequenceNumber();if(sequence==written)continue;changed=true;uint ownerPid,clipPid;Native.GetWindowThreadProcessId(owner,out ownerPid);Native.GetWindowThreadProcessId(Native.ClipboardOwner(),out clipPid);if(ownerPid==0||ownerPid!=clipPid){if(Trace!=null)Trace("Find copy clipboard owner mismatch");return null;}last=sequence;string text;if(!Native.ClipboardUnicode(sequence,4096,out text))continue;if(Trace!=null)Trace("Find copy ms="+clock.ElapsedMilliseconds);return text;}if(changed){if(Trace!=null)Trace("Find copied payload unavailable: "+Native.ClipboardReadFailure);return null;}if(Trace!=null)Trace("Find empty copy ms="+clock.ElapsedMilliseconds);return "";}
  public void Dispose(){try{if(touched)backup.Restore(last);}finally{backup.Dispose();host.Dispose();}}
 }
 static bool Move(int count){while(count>0){int step=Math.Min(160,count);if(!Native.MoveCaretCharacters(step))return false;count-=step;}return true;}
 static bool Select(int start,int end){return Native.SelectFieldRange(start,end);}
 public static Snapshot Capture(IntPtr owner){if(!Focused(owner))return null;var prior=cached;if(prior!=null&&prior.Owner==owner&&prior.Matches(false,false))return prior.Clone();return Sta(()=>{lock(gate){int stamp=epoch;string[] prefs=Preferences(owner);if(prefs==null)return null;using(var probe=new Probe(owner,stamp)){
   string selected=probe.Copy();if(Trace!=null)Trace("Find capture selected length="+(selected==null?-1:selected.Length));if(selected==null||selected.Length>4096)return null;
   if(selected.Length>0&&!Native.Chord(0x27))return null;
   if(!Native.Chord(0x24,true,true))return null;Thread.Sleep(20);string prefix=probe.Copy();if(Trace!=null)Trace("Find capture prefix length="+(prefix==null?-1:prefix.Length));if(prefix==null||prefix.Length>4096)return null;
   if(!Native.Chord(0x41,true))return null;Thread.Sleep(20);string full=probe.Copy();if(Trace!=null)Trace("Find capture full length="+(full==null?-1:full.Length));if(string.IsNullOrEmpty(full)||full.Length>4096||full.Any(char.IsSurrogate)||full.IndexOfAny(new[]{'\r','\n','\t','\0'})>=0)return null;
   int end=prefix.Length,start=end-selected.Length;int[] slots=Enumerable.Range(0,4).Where(n=>prefs[n]==full).ToArray();
   bool valid=slots.Length>0&&start>=0&&end<=full.Length&&full.StartsWith(prefix,StringComparison.Ordinal)&&full.Substring(start,selected.Length)==selected&&Same(prefs,Preferences(owner))&&probe.Current;
   if(Trace!=null)Trace("Find capture valid="+valid+" slots="+slots.Length+" start="+start+" end="+end);if(!valid)return null;if(!Select(start,end)){if(Trace!=null)Trace("Find caret restore failed");return null;}
   var snap=new Snapshot{Owner=owner,Full=full,Preferences=prefs,Slots=slots,Start=start,End=end,SelectionStart=start,SelectionEnd=end,Stamp=stamp};cached=snap.Clone();return snap;
  }}});}
 public sealed class Snapshot {
  public IntPtr Owner;public string Full;internal string[] Preferences;internal int[] Slots;internal int Stamp;public int Start,End,SelectionStart,SelectionEnd;bool selected,selectionProved;int provedStart,provedEnd;
  public string Text{get{return Full.Substring(Start,End-Start);}}public string Left{get{return Full.Substring(0,Start);}}public string Right{get{return Full.Substring(End);}}
  public Snapshot Clone(){return (Snapshot)MemberwiseClone();}
  public bool Matches(bool caret,bool target=true){return Stamp==epoch&&Focused(Owner)&&Same(Preferences,InDesignFindField.Preferences(Owner))&&(!target||(caret?!selected&&SelectionStart==SelectionEnd:selected||SelectionStart==Start&&SelectionEnd==End));}
  public bool SameStart(Snapshot other){return other!=null&&Owner==other.Owner&&Start==other.Start&&Slots.Intersect(other.Slots).Any();}
  public string Key{get{return Owner+":find:"+string.Join(",",Slots)+":"+Start;}}
  public bool Select(){return Sta(()=>{lock(gate){selectionProved=false;if(!Matches(false,false))return false;using(var probe=new Probe(Owner,Stamp)){if(!InDesignFindField.Select(Start,End))return false;string actual=probe.Copy();if(actual!=Text||!Matches(false,false))return false;selected=true;selectionProved=true;provedStart=Start;provedEnd=End;cached=Clone();return true;}}});}
  public bool Restore(){return Sta(()=>{if(!Matches(false,false))return false;selectionProved=false;selected=false;return InDesignFindField.Select(SelectionStart,SelectionEnd);});}
  public bool Position(string output,int offset){if(offset<0||offset>output.Length||!Focused(Owner))return false;return Sta(()=>{if(!InDesignFindField.Select(Start+offset,Start+offset))return false;SelectionStart=SelectionEnd=Start+offset;selectionProved=false;selected=false;cached=Clone();return true;});}
  public bool Replace(string output,bool retain,Func<bool> context){if(output==null||output.Length>4096||output.Any(c=>char.IsSurrogate(c)||char.IsControl(c)))return false;return Sta(()=>{lock(gate){if(!context()||!Matches(false)||Full.Length-Text.Length+output.Length>4096)return false;
   // Select already copied and verified this exact range. Reuse that proof only
   // while its input epoch, focus, source preferences and bounds still match.
   bool proved=selected&&selectionProved&&provedStart==Start&&provedEnd==End;
   using(var probe=proved?null:new Probe(Owner,Stamp)){
   if(probe!=null){string actual=probe.Copy();if(actual!=Text||!context()||!Matches(false))return false;}else if(Trace!=null)Trace("Find verified selection reused");
   selectionProved=false;string wanted=Full.Substring(0,Start)+output+Full.Substring(End);if(!Native.TypeText(output))return false;
   string[] after=null;int changedSlot=-1;for(int poll=0;poll<35&&context()&&Stamp==epoch&&Focused(Owner);poll++){after=InDesignFindField.Preferences(Owner);if(after==null)break;var changes=Enumerable.Range(0,4).Where(n=>after[n]!=Preferences[n]).ToArray();if(changes.Length==1&&Slots.Contains(changes[0])&&after[changes[0]]==wanted){changedSlot=changes[0];break;}if(changes.Length==0&&wanted==Full){changedSlot=Slots[0];break;}Thread.Sleep(10);}
   if(changedSlot<0)return false;
   Full=wanted;Preferences=after;Slots=new[]{changedSlot};End=Start+output.Length;SelectionStart=SelectionEnd=End;selected=false;
   if(retain){if(!InDesignFindField.Select(Start,End))return false;selected=true;SelectionStart=Start;SelectionEnd=End;}cached=Clone();return true;
  }}});}
 }
}
}
