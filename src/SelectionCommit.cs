using System;
using System.Threading;
namespace KeyJaya {
// Some keyboard utilities replace a physical Space with a Unicode Space packet.
// Recognize one matching down/up pair only for a physically completed correction.
// This never feeds injected text into the trigger buffer or authorizes an edit.
public sealed class DelimiterReplay {
    long generation;IntPtr owner;DateTime deadline;bool active,down;
    public void Begin(long stamp,IntPtr window,char delimiter,DateTime now){generation=stamp;owner=window;deadline=now.AddMilliseconds(500);active=delimiter==' '&&window!=IntPtr.Zero;down=false;}
    public void Clear(){active=false;down=false;}
    public void Clear(long stamp){if(stamp==generation)Clear();}
    public bool Consume(uint vk,uint scan,uint flags,int message,long stamp,IntPtr window,DateTime now){
        if(!active||stamp!=generation||window!=owner||now>=deadline||vk!=231||scan!=32||(flags&0x10)==0)return false;
        bool up=(flags&0x80)!=0;
        if(!up&&message==0x100&&!down){down=true;return true;}
        if(up&&message==0x101&&down){Clear();return true;}
        return false;
    }
}
// Tracks only consecutive physical typing after one completed correction.
// Navigation, mouse/focus changes and reset cancel ownership instead of guessing.
public sealed class CorrectionTyping {
    long origin,last;IntPtr owner;string tail;bool active;
    public void Begin(long stamp,IntPtr window){origin=last=stamp;owner=window;tail="";active=window!=IntPtr.Zero;}
    public void Clear(){active=false;tail=null;}
    public void Clear(long stamp){if(origin==stamp)Clear();}
    public void Append(char c,long stamp,IntPtr window){if(!active)return;if(window!=owner||stamp!=last+1||tail.Length>=96||char.IsSurrogate(c)||char.IsControl(c)&&c!='\n'&&c!='\t'){Clear();return;}tail+=c;last=stamp;}
    public bool Read(long stamp,IntPtr window,long current,out string suffix){suffix=null;if(!active||origin!=stamp||owner!=window||last!=current)return false;suffix=tail;return true;}
}
public static class SelectionConversion {
    // Browser providers expose paragraph breaks differently from clipboard
    // text. This proves pasted content without changing the document or
    // forgiving missing/reordered words, spaces, punctuation, or diacritics.
    public static bool PastedTextMatches(string expected,string observed){
        if(string.IsNullOrEmpty(expected)||observed==null||expected.Length>200000||observed.Length>400000)return false;
        Func<string,string> paragraphs=value=>System.Text.RegularExpressions.Regex.Replace(value.Replace("\r\n","\n").Replace('\r','\n').Replace('\u2028','\n').Replace('\u2029','\n'),"\n+","\n");
        return paragraphs(expected)==paragraphs(observed);
    }
    public static bool PastedCaretMatches(string expected,string before,string after){
        if(before==null||after==null||after.Length>32||after.Trim('\r','\n','\u2028','\u2029').Length!=0)return false;
        return expected!=null&&PastedTextMatches(expected.TrimEnd('\r','\n','\u2028','\u2029'),before.TrimEnd('\r','\n','\u2028','\u2029'));
    }
    public static bool UntouchedManualInput(bool capturedTarget,bool committedOutput){return !capturedTarget&&!committedOutput;}
    public static bool ValidText(string text){return !string.IsNullOrEmpty(text)&&text.Length<=200000&&text.IndexOf('\0')<0;}
    public static bool CanCommit(Func<bool> context,Func<bool> selected){return context()&&selected()&&context();}
    public static bool MayRestoreClipboard(uint written,uint current,bool owned){return owned&&written==current;}
    public static object CloneClipboardValue(object value){if(value==null)throw new InvalidOperationException("Unavailable clipboard format");if(value is string)return value;if(value is byte[])return ((byte[])value).Clone();if(value is string[])return ((string[])value).Clone();var stream=value as System.IO.MemoryStream;if(stream!=null)return new System.IO.MemoryStream(stream.ToArray(),false);var clone=value as ICloneable;if(clone!=null)return clone.Clone();throw new InvalidOperationException("Unsupported clipboard format");}
}
public interface ISelectionSurface {
    bool AtOwnedCaret();
    bool ExactSelection();
    void RequestSelection();
    bool RestoreCaret();
    bool RequestKeyboardSelection();
}
public static class FocusEventGuard {
    // EVENT_OBJECT_FOCUS is global. A background window's notification must not
    // cancel the foreground editor's pending tap, token, or conversion.
    // Unknown roots and real foreground transitions still invalidate ownership.
    public static bool Ignore(uint eventId,IntPtr eventRoot,IntPtr foreground){
        return eventId==0x8005&&eventRoot!=IntPtr.Zero&&foreground!=IntPtr.Zero&&eventRoot!=foreground;
    }
}
// Keep only the most recent completed word; stale work must never edit earlier text.
public sealed class CorrectionQueue {
    string word;IntPtr owner;long generation;
    public void Set(string value,IntPtr window,long stamp){word=value;owner=window;generation=stamp;}
    public bool Take(long stamp,IntPtr window,bool paused,out string value){value=word;word=null;if(value==null||paused||stamp!=generation||window!=owner){value=null;return false;}return true;}
}
// Selection requests can be ignored or asynchronous. Never equate a request with success.
public static class SelectionCommit {
    public static bool OwnedRange(bool expanded,bool startSame,bool endSame,bool textSame,bool rangeSame,bool boundary,bool word,string endGap){
        return expanded&&startSame&&textSame&&rangeSame&&boundary&&(endSame||(word&&endGap==""));
    }
    public static int KeyboardCount(string text){if(string.IsNullOrEmpty(text)||text.Length>160)return 0;foreach(char c in text)if(c<32||c>126)return 0;return text.Length;}
    public static bool Prepare(ISelectionSurface surface,Func<bool> context,Action wait=null,bool keyboardFirst=false){
        bool success=false,requested=false;try{
        if(wait==null)wait=()=>Thread.Sleep(10);
        if(!context()||!surface.AtOwnedCaret())return false;
        if(keyboardFirst){
            requested=true;if(!surface.RequestKeyboardSelection())return false;
            for(int i=0;i<=10;i++){if(!context())return false;if(surface.ExactSelection()){success=context();return success;}if(i<10)wait();}
            return false;
        }
        requested=true;surface.RequestSelection();
        for(int i=0;i<=3;i++){
            if(!context())return false;
            if(surface.ExactSelection()){success=context();return success;}
            // Web editors may briefly report a collapsed/intermediate range
            // while fulfilling Select. Observe only; never type into that range.
            // Recheck after the final wait before deciding selection failed.
            if(i<3)wait();
        }
        if(!context()||!surface.AtOwnedCaret()||!surface.RequestKeyboardSelection())return false;
        for(int i=0;i<=10;i++){
            if(!context())return false;
            if(surface.ExactSelection()){success=context();return success;}
            if(i<10)wait();
        }
        return false;
        }finally{if(requested&&!success&&context())surface.RestoreCaret();}
    }
    public static bool CanPaste(ISelectionSurface surface,Func<bool> context){return context()&&surface.ExactSelection()&&context();}
}
}
