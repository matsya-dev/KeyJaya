using System;using System.Text;using System.Threading;using System.Windows.Automation;using System.Windows.Automation.Text;
namespace KeyJaya {
// Excel's EXCEL6 editor exposes no TextPattern. Its Formula Bar mirrors the
// same active edit buffer and selection, including while the cell has focus.
// Read that owned mirror; use native input only after proving the exact range.
public static class ExcelCellField {
 static int epoch;static readonly object gate=new object();static IntPtr mirrorOwner;static AutomationElement mirrorElement;static TextPattern mirror;
 public static Action<string> Trace;
 public static void Invalidate(){Interlocked.Increment(ref epoch);}
 static IntPtr Editor(IntPtr owner){if(owner==IntPtr.Zero||owner!=Native.GetForegroundWindow())return IntPtr.Zero;var root=new StringBuilder(64);Native.GetClassName(owner,root,64);if(root.ToString()!="XLMAIN")return IntPtr.Zero;var id=Native.FocusIdentity();if(id==null)return IntPtr.Zero;long hwnd=(uint)id[4]|((long)id[5]<<32);var handle=new IntPtr(hwnd);var name=new StringBuilder(64);Native.GetClassName(handle,name,64);return name.ToString()=="EXCEL6"?handle:IntPtr.Zero;}
 public static bool Focused(IntPtr owner){return Editor(owner)!=IntPtr.Zero;}
 static TextPattern Mirror(IntPtr owner){uint pid;Native.GetWindowThreadProcessId(owner,out pid);try{if(mirrorOwner==owner&&mirrorElement!=null&&mirrorElement.Current.ProcessId==pid&&mirrorElement.Current.ClassName=="XLFormulaBarEditor")return mirror;}catch{}mirror=null;mirrorElement=null;mirrorOwner=IntPtr.Zero;
  var root=AutomationElement.FromHandle(owner);var field=root.FindFirst(TreeScope.Descendants,new AndCondition(new PropertyCondition(AutomationElement.AutomationIdProperty,"FormulaBar"),new PropertyCondition(AutomationElement.ClassNameProperty,"XLFormulaBarEditor")));object provider;if(field==null||field.Current.ProcessId!=pid||!field.TryGetCurrentPattern(TextPattern.Pattern,out provider))return null;mirrorOwner=owner;mirrorElement=field;return mirror=(TextPattern)provider;
 }
 static bool Read(TextPattern pattern,out string text,out int start,out int end){text=null;start=end=-1;var document=pattern.DocumentRange;string value=document.GetText(4097);if(value==null||value.Length>4096||value.IndexOfAny(new[]{'\0','\r','\n','\t'})>=0)return false;var selected=pattern.GetSelection();if(selected.Length!=1||object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute),true))return false;
  var prefix=document.Clone();prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End,selected[0],TextPatternRangeEndpoint.Start);int begin=prefix.GetText(4097).Length;prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End,selected[0],TextPatternRangeEndpoint.End);int stop=prefix.GetText(4097).Length;if(begin<0||stop<begin||stop>value.Length||selected[0].GetText(4097)!=value.Substring(begin,stop-begin))return false;text=value;start=begin;end=stop;return true;
 }
 public static Snapshot Capture(IntPtr owner){IntPtr editor=Editor(owner);if(editor==IntPtr.Zero)return null;try{lock(gate){int stamp=epoch;var pattern=Mirror(owner);string text;int start,end;if(pattern==null||!Read(pattern,out text,out start,out end)||text.StartsWith("=",StringComparison.Ordinal)||text.StartsWith("+",StringComparison.Ordinal)||text.StartsWith("@",StringComparison.Ordinal)||stamp!=epoch||Editor(owner)!=editor)return null;
   return new Snapshot{Owner=owner,EditorHandle=editor,Stamp=stamp,Full=text,Start=start,End=end,SelectionStart=start,SelectionEnd=end,OriginalStart=start,OriginalEnd=end,pattern=pattern};
  }}catch(Exception ex){if(Trace!=null)Trace("Excel mirror capture skipped: "+ex.GetType().Name);return null;}}
 public sealed class Snapshot {
  public IntPtr Owner;internal IntPtr EditorHandle;internal int Stamp;public string Full;public int Start,End,SelectionStart,SelectionEnd;internal int OriginalStart,OriginalEnd;internal TextPattern pattern;
  public string Text{get{return Full.Substring(Start,End-Start);}}
  public bool Current{get{return Stamp==epoch&&Editor(Owner)==EditorHandle;}}
  public Snapshot Clone(){return (Snapshot)MemberwiseClone();}
  bool At(string text,int start,int end){try{if(!Current)return false;string actual;int begin,stop;return Read(pattern,out actual,out begin,out stop)&&actual==text&&begin==start&&stop==end&&Current;}catch{return false;}}
  bool WaitAt(string text,int start,int end,Func<bool> context=null){for(int n=0;n<25;n++){if(!Current||context!=null&&!context())return false;if(At(text,start,end))return true;Thread.Sleep(5);}return false;}
  public bool Matches(bool caret){return caret?SelectionStart==SelectionEnd&&SelectionStart>=Start&&SelectionEnd<=End&&At(Full,SelectionStart,SelectionEnd):SelectionStart==Start&&SelectionEnd==End&&At(Full,Start,End);}
  public bool SameStart(Snapshot other){return other!=null&&Owner==other.Owner&&EditorHandle==other.EditorHandle&&Start==other.Start&&Full.Substring(0,Start)==other.Full.Substring(0,other.Start)&&Full.Substring(End)==other.Full.Substring(other.End)&&Current;}
  public string Key{get{return Owner+":excel:"+EditorHandle+":"+Stamp+":"+Start;}}
  public bool Select(){if(!Current||!At(Full,SelectionStart,SelectionEnd)||!Native.SelectFieldRange(Start,End,Full.Length)||!WaitAt(Full,Start,End))return false;SelectionStart=Start;SelectionEnd=End;return true;}
  public bool Restore(){if(!Current||!Native.SelectFieldRange(OriginalStart,OriginalEnd,Full.Length)||!WaitAt(Full,OriginalStart,OriginalEnd))return false;SelectionStart=OriginalStart;SelectionEnd=OriginalEnd;return true;}
  public bool Position(string replacement,int offset){if(!Current||offset<0||offset>replacement.Length||replacement!=Text)return false;int at=Start+offset;if(!Native.SelectFieldRange(at,at,Full.Length)||!WaitAt(Full,at,at))return false;SelectionStart=SelectionEnd=at;return true;}
  public bool Replace(string output,bool retain,Func<bool> context){if(string.IsNullOrEmpty(output)||output.Length>4096||System.Linq.Enumerable.Any(output,c=>char.IsControl(c)||char.IsSurrogate(c)))return false;if(!context()||!Matches(false))return false;
   string wanted=Full.Substring(0,Start)+output+Full.Substring(End);if(wanted.Length>4096)return false;var keys=new System.Collections.Generic.List<Native.Input>();foreach(char c in output)keys.AddRange(Native.UnicodeCharacter(c.ToString()));if(!context()||!Matches(false)||!Native.ReplayCorrectionInput(keys.ToArray())||!WaitAt(wanted,Start+output.Length,Start+output.Length,context))return false;
   Full=wanted;End=Start+output.Length;SelectionStart=SelectionEnd=End;if(retain){if(!Native.SelectFieldRange(Start,End,Full.Length)||!WaitAt(Full,Start,End,context))return false;SelectionStart=Start;}return true;
  }
  public bool Resume(string output,bool selected,bool typing){if(!Current)return false;if(output==null)return selected&&typing?Position(Text,Text.Length):Restore();if(Text!=output)return false;if(selected&&typing)return Position(output,output.Length);return At(Full,SelectionStart,SelectionEnd);}
 }
}
}
