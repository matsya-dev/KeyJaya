using System;using System.Text;using System.Threading;using System.Windows.Automation;using System.Windows.Automation.Text;
namespace KeyJaya {
using System.Threading.Tasks;
// Only one read-only provider request may remain outstanding. A late result
// cannot authorize a subsequent edit or keep the resident UI thread blocked.
public sealed class BoundedCellRead<T> where T:class {
 readonly object gate=new object();Task<T> pending;
 public bool TryRead(Func<T> read,int milliseconds,out T result){
  result=null;if(read==null||milliseconds<1||milliseconds>1000)return false;Task<T> request;
  lock(gate){if(pending!=null&&!pending.IsCompleted)return false;request=pending=Task.Factory.StartNew(()=>{try{return read();}catch{return null;}},CancellationToken.None,TaskCreationOptions.None,TaskScheduler.Default);}
  try{if(!request.Wait(milliseconds))return false;result=request.Result;return result!=null;}finally{lock(gate)if(pending==request&&request.IsCompleted)pending=null;}
 }
}
// Excel's EXCEL6 editor exposes no TextPattern. Its EXCEL< Formula Bar mirrors
// the same active edit buffer and selection. Both use the same formula guard.
// Read that owned mirror; use native input only after proving the exact range.
public static class ExcelCellField {
 static int epoch;static readonly object gate=new object();static readonly BoundedCellRead<CellState> reads=new BoundedCellRead<CellState>();static IntPtr mirrorOwner,modeOwner;static AutomationElement mirrorElement,modeElement;static TextPattern mirror;
 sealed class CellState {public string Text;public int Start,End;public string Mode;}
 public static Action<string> Trace;
 static void Note(string detail){if(Trace!=null)Trace("Excel "+detail);}
 public static void Invalidate(){Interlocked.Increment(ref epoch);}
 internal static bool EditorClass(string name){return name=="EXCEL6"||name=="EXCEL<";}
 static IntPtr Editor(IntPtr owner){if(owner==IntPtr.Zero||owner!=Native.GetForegroundWindow())return IntPtr.Zero;var root=new StringBuilder(64);Native.GetClassName(owner,root,64);if(root.ToString()!="XLMAIN")return IntPtr.Zero;var id=Native.FocusIdentity();if(id==null)return IntPtr.Zero;long hwnd=(uint)id[4]|((long)id[5]<<32);var handle=new IntPtr(hwnd);var name=new StringBuilder(64);Native.GetClassName(handle,name,64);return EditorClass(name.ToString())?handle:IntPtr.Zero;}
 public static bool Focused(IntPtr owner){return Editor(owner)!=IntPtr.Zero;}
 static TextPattern Mirror(IntPtr owner){uint pid;Native.GetWindowThreadProcessId(owner,out pid);try{if(mirrorOwner==owner&&mirrorElement!=null&&mirrorElement.Current.ProcessId==pid&&mirrorElement.Current.AutomationId=="FormulaBar")return mirror;}catch{}mirror=null;mirrorElement=null;mirrorOwner=IntPtr.Zero;
  // The Formula Bar is an immediate XLMAIN child. Descendants can enumerate
  // the worksheet's enormous virtual cell tree when the bar is hidden.
  Note("mirror discovery started");var root=AutomationElement.FromHandle(owner);var field=root.FindFirst(TreeScope.Children,new PropertyCondition(AutomationElement.AutomationIdProperty,"FormulaBar"));object provider;if(field==null){Note("Formula Bar missing");return null;}if(field.Current.ProcessId!=pid||field.Current.ControlType!=ControlType.Edit){Note("Formula Bar identity rejected");return null;}if(!field.TryGetCurrentPattern(TextPattern.Pattern,out provider)){Note("Formula Bar has no TextPattern");return null;}mirrorOwner=owner;mirrorElement=field;Note("mirror discovered");return mirror=(TextPattern)provider;
 }
 internal static bool TextBuffer(string value){return value!=null&&value.Length<=4096&&value.IndexOfAny(new[]{'\0','\r','\n','\t'})<0&&!value.StartsWith("=",StringComparison.Ordinal)&&!value.StartsWith("+",StringComparison.Ordinal)&&!value.StartsWith("-",StringComparison.Ordinal)&&!value.StartsWith("@",StringComparison.Ordinal);}
 internal static int CaretPosition(string full,int start,int end,string replacement,string tail,int offset){if(full==null||replacement==null||tail==null||start<0||end<start||end>full.Length||end-start!=replacement.Length||offset<0||offset>replacement.Length+tail.Length||tail.Length>full.Length-end||System.Linq.Enumerable.Any(tail,c=>!char.IsPunctuation(c)||c=='_'||c=='/'||c=='\\'||c=='@'||c=='#')||string.CompareOrdinal(full,start,replacement,0,replacement.Length)!=0||string.CompareOrdinal(full,end,tail,0,tail.Length)!=0)return -1;return start+offset;}
 internal static string ModeLabel(string name,string value){if(name=="Cell Mode"&&(value=="Enter"||value=="Edit"||value=="Ready"))return value;foreach(string mode in new[]{"Enter","Edit","Ready"})if(name=="Cell Mode "+mode)return mode;return null;}
 static string ModeValue(AutomationElement element){string name=element.Current.Name;if(name==null||!name.StartsWith("Cell Mode",StringComparison.Ordinal))return null;object provider;string value=null;if(element.TryGetCurrentPattern(ValuePattern.Pattern,out provider))value=((ValuePattern)provider).Current.Value;else if(element.TryGetCurrentPattern(TextPattern.Pattern,out provider))value=((TextPattern)provider).DocumentRange.GetText(32);string mode=ModeLabel(name,value);if(mode==null)Note("cell-mode label found without a recognized value");return mode;}
 static string ReadMode(IntPtr owner){uint pid;Native.GetWindowThreadProcessId(owner,out pid);try{if(modeOwner==owner&&modeElement!=null&&modeElement.Current.ProcessId==pid)return ModeValue(modeElement);}catch{}modeElement=null;modeOwner=IntPtr.Zero;
  // Search only shallow toolbar/status containers, never the virtual cell grid.
  var nodes=new System.Collections.Generic.Stack<Tuple<AutomationElement,int>>();nodes.Push(Tuple.Create(AutomationElement.FromHandle(owner),0));int remaining=192;
  while(nodes.Count>0&&remaining-->0){var item=nodes.Pop();var node=item.Item1;var type=node.Current.ControlType;if(type==ControlType.Text){string mode=ModeValue(node);if(mode!=null){modeOwner=owner;modeElement=node;Note("cell mode discovered "+mode);return mode;}continue;}if(item.Item2>=8||type!=ControlType.Window&&type!=ControlType.Pane&&type!=ControlType.ToolBar&&type!=ControlType.StatusBar)continue;var children=node.FindAll(TreeScope.Children,Condition.TrueCondition);for(int n=Math.Min(children.Count,64)-1;n>=0;n--)nodes.Push(Tuple.Create(children[n],item.Item2+1));}Note("cell mode unavailable");return null;
 }
 static bool Read(TextPattern pattern,out string text,out int start,out int end){text=null;start=end=-1;var document=pattern.DocumentRange;string value=document.GetText(4097);if(!TextBuffer(value)){Note("buffer rejected");return false;}var selected=pattern.GetSelection();if(selected.Length!=1||object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute),true)){Note("selection unavailable or read-only count="+selected.Length);return false;}
  var prefix=document.Clone();prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End,selected[0],TextPatternRangeEndpoint.Start);int begin=prefix.GetText(4097).Length;prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End,selected[0],TextPatternRangeEndpoint.End);int stop=prefix.GetText(4097).Length;if(begin<0||stop<begin||stop>value.Length||selected[0].GetText(4097)!=value.Substring(begin,stop-begin)){Note("range rejected");return false;}text=value;start=begin;end=stop;return true;
 }
 static CellState ReadCell(IntPtr owner,IntPtr editor,int stamp,bool includeMode){
  try{if(stamp!=epoch||Editor(owner)!=editor){Note("read invalidated before discovery");return null;}lock(gate){if(stamp!=epoch||Editor(owner)!=editor)return null;var pattern=Mirror(owner);string text;int start,end;if(pattern==null||!Read(pattern,out text,out start,out end))return null;string mode=includeMode?ReadMode(owner):null;if(stamp!=epoch||Editor(owner)!=editor){Note("read invalidated after discovery");return null;}return new CellState{Text=text,Start=start,End=end,Mode=mode};}}catch(Exception ex){Note("provider read failed "+ex.GetType().Name);return null;}
 }
 static bool TryReadCell(IntPtr owner,IntPtr editor,int stamp,out CellState state,bool includeMode=false){state=null;if(stamp!=epoch||Editor(owner)!=editor){Note("read invalidated before scheduling");return false;}CellState value;bool read=reads.TryRead(()=>ReadCell(owner,editor,stamp,includeMode),mirror==null||includeMode&&modeElement==null?1000:250,out value);if(!read||stamp!=epoch||Editor(owner)!=editor){if(!read&&Trace!=null)Trace("Excel cell read unavailable or exceeded time limit");return false;}state=value;return true;}
 public static Snapshot Capture(IntPtr owner){IntPtr editor=Editor(owner);if(editor==IntPtr.Zero)return null;try{int stamp=epoch;CellState state;if(!TryReadCell(owner,editor,stamp,out state))return null;
   return new Snapshot{Owner=owner,EditorHandle=editor,Stamp=stamp,Full=state.Text,Start=state.Start,End=state.End,SelectionStart=state.Start,SelectionEnd=state.End,OriginalStart=state.Start,OriginalEnd=state.End};
  }catch(Exception ex){if(Trace!=null)Trace("Excel mirror capture skipped: "+ex.GetType().Name);return null;}}
 public sealed class Snapshot {
  public IntPtr Owner;internal IntPtr EditorHandle;internal int Stamp;public string Full;public int Start,End,SelectionStart,SelectionEnd;internal int OriginalStart,OriginalEnd;internal string Tail="";
  public string Text{get{return Full.Substring(Start,End-Start);}}
  public bool Current{get{return Stamp==epoch&&Editor(Owner)==EditorHandle;}}
  public Snapshot Clone(){return (Snapshot)MemberwiseClone();}
  bool At(string text,int start,int end){try{if(!Current)return false;CellState actual;return TryReadCell(Owner,EditorHandle,Stamp,out actual)&&actual.Text==text&&actual.Start==start&&actual.End==end&&Current;}catch{return false;}}
  bool WaitAt(string text,int start,int end,Func<bool> context=null){for(int n=0;n<25;n++){if(!Current||context!=null&&!context())return false;if(At(text,start,end))return true;Thread.Sleep(5);}return false;}
  public bool Matches(bool caret){return caret?SelectionStart==SelectionEnd&&SelectionStart>=Start&&SelectionEnd<=End+Tail.Length&&At(Full,SelectionStart,SelectionEnd):SelectionStart==Start&&SelectionEnd==End&&At(Full,Start,End);}
  public bool SameStart(Snapshot other){return other!=null&&Owner==other.Owner&&EditorHandle==other.EditorHandle&&Start==other.Start&&Full.Substring(0,Start)==other.Full.Substring(0,other.Start)&&Full.Substring(End)==other.Full.Substring(other.End)&&Current;}
  public string Key{get{return Owner+":excel:"+EditorHandle+":"+Stamp+":"+Start;}}
  // F2 toggles Enter/Edit. Use it only when the owned status bar proves Enter;
  // otherwise Home/End can commit a cell instead of navigating its text.
  bool EnsureEdit(int begin,int end){CellState actual;if(!Current||!TryReadCell(Owner,EditorHandle,Stamp,out actual,true)||actual.Text!=Full||actual.Start!=begin||actual.End!=end)return false;if(actual.Mode=="Edit")return true;if(actual.Mode!="Enter"||!Native.Chord(0x71))return false;for(int n=0;n<25&&Current;n++){if(TryReadCell(Owner,EditorHandle,Stamp,out actual,true)&&actual.Text==Full&&actual.Start==begin&&actual.End==end&&actual.Mode=="Edit")return true;Thread.Sleep(5);}return false;}
  public bool Select(){if(!EnsureEdit(SelectionStart,SelectionEnd)||!Native.SelectFieldRange(Start,End,Full.Length,false)||!WaitAt(Full,Start,End))return false;SelectionStart=Start;SelectionEnd=End;return true;}
  public bool Restore(){if(!Current)return false;if(At(Full,OriginalStart,OriginalEnd)){SelectionStart=OriginalStart;SelectionEnd=OriginalEnd;return true;}CellState actual;if(!TryReadCell(Owner,EditorHandle,Stamp,out actual)||actual.Text!=Full||!EnsureEdit(actual.Start,actual.End)||!Native.SelectFieldRange(OriginalStart,OriginalEnd,Full.Length,false)||!WaitAt(Full,OriginalStart,OriginalEnd))return false;SelectionStart=OriginalStart;SelectionEnd=OriginalEnd;return true;}
  public bool Position(string replacement,int offset){int at=CaretPosition(Full,Start,End,replacement,Tail,offset);if(!Current||at<0||!EnsureEdit(SelectionStart,SelectionEnd))return false;if(!Native.SelectFieldRange(at,at,Full.Length,false)||!WaitAt(Full,at,at))return false;SelectionStart=SelectionEnd=at;return true;}
  public bool Replace(string output,bool retain,Func<bool> context){if(string.IsNullOrEmpty(output)||output.Length>4096||System.Linq.Enumerable.Any(output,c=>char.IsControl(c)||char.IsSurrogate(c)))return false;if(!context()||!Matches(false))return false;
   string wanted=Full.Substring(0,Start)+output+Full.Substring(End);if(!TextBuffer(wanted)||!EnsureEdit(Start,End))return false;var keys=new System.Collections.Generic.List<Native.Input>();foreach(char c in output)keys.AddRange(Native.UnicodeCharacter(c.ToString()));if(!context()||!Matches(false)||!Native.ReplayCorrectionInput(keys.ToArray())||!WaitAt(wanted,Start+output.Length,Start+output.Length,context))return false;
   Full=wanted;End=Start+output.Length;SelectionStart=SelectionEnd=End;if(retain){if(!Native.SelectFieldRange(Start,End,Full.Length,false)||!WaitAt(Full,Start,End,context))return false;SelectionStart=Start;}return true;
  }
  public bool Resume(string output,bool selected,bool typing){if(!Current)return false;if(output==null)return selected&&typing?Position(Text,Text.Length):Restore();if(Text!=output)return false;if(selected&&typing)return Position(output,output.Length);return At(Full,SelectionStart,SelectionEnd);}
 }
}
}
