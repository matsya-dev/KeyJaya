using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
namespace KeyJaya {
public sealed class ConversionShortcuts {
    readonly HashSet<uint> held=new HashSet<uint>();
    public static string Mode(uint key,bool ctrl,bool shift,bool alt,bool win){if(!ctrl||shift||alt||win)return null;switch(key){case 0xBE:return "unicode";case 0xBC:return "balaram";case 0xBF:return "plain";case 0xDC:return "itrans";case 0xDB:return "devanagari";case 0xDD:return "bengali";default:return null;}}
    public bool Handle(uint key,bool down,bool up,string mode,bool paused,out string dispatch){dispatch=null;if(up)return held.Remove(key);if(!down)return false;if(held.Contains(key))return true;if(paused||mode==null)return false;held.Add(key);dispatch=mode;return true;}
    public void Clear(){held.Clear();}
}
public static class SingleLetters {
    static readonly string[] Unicode={"aā","iī","uū","rṛṝ","lḷḹ","nṇṅñ","sṣś","tṭ","dḍ","hḥ","mṃṁ"};
    static readonly string[] UnicodeUpper=Upper(Unicode),Balaram=Legacy(Unicode),BalaramUpper=Legacy(UnicodeUpper);
    static string[] Upper(string[] rows){var result=new string[rows.Length];for(int i=0;i<rows.Length;i++)result[i]=rows[i].ToUpperInvariant();return result;}
    static string[] Legacy(string[] rows){var result=new string[rows.Length];for(int i=0;i<rows.Length;i++){string unique="";foreach(char c in EncodingMap.Balaram(rows[i]))if(unique.IndexOf(c)<0)unique+=c;result[i]=unique;}return result;}
    static string[] Forms(string mode,bool upper){if(mode=="unicode")return upper?UnicodeUpper:Unicode;if(mode=="balaram")return upper?BalaramUpper:Balaram;throw new ArgumentException("Unknown letter format");}
    static bool Contains(string[] rows,char c){foreach(string row in rows)if(row.IndexOf(c)>=0)return true;return false;}
    public static string[] Rows(string mode){return (string[])Forms(mode,false).Clone();}
    public static bool Supported(string value){return value!=null&&value.Length==1&&(Contains(Unicode,value[0])||Contains(UnicodeUpper,value[0])||Contains(Balaram,value[0])||Contains(BalaramUpper,value[0]));}
    public static string Next(string value,string mode,string previousMode=null){
        if(!Supported(value))return null;bool upper=Contains(UnicodeUpper,value[0])||Contains(BalaramUpper,value[0]);string current=value;
        // Switching formats preserves the current diacritic before cycling it again.
        if(previousMode!=null&&previousMode!=mode){string unicode=previousMode=="balaram"?EncodingMap.FromBalaram(current):current;string mapped=mode=="balaram"?EncodingMap.Balaram(unicode):unicode;if(mapped!=current)return mapped;}
        string[] forms=Forms(mode,upper);foreach(string row in forms){int index=row.IndexOf(current[0]);if(index>=0)return row[(index+1)%row.Length].ToString();}
        string translated=mode=="unicode"?EncodingMap.FromBalaram(current):EncodingMap.Balaram(current);return translated==current?null:translated;
    }
}
public sealed class LetterCycle {
    readonly Queue<string> pending=new Queue<string>();
    public string Original {get;private set;}public string Current {get;private set;}public string Mode {get;private set;}public IntPtr Owner {get;private set;}
    public long Revision {get;private set;}
    public bool HasPending {get{return Current!=null&&pending.Count>0;}}
    public void Clear(){Original=Current=Mode=null;Owner=IntPtr.Zero;pending.Clear();Revision++;}
    public bool ClearIfOwned(long revision){if(Revision!=revision)return false;Clear();return true;}
    public void Observe(char c,IntPtr owner){Clear();if(owner!=IntPtr.Zero&&SingleLetters.Supported(c.ToString())){Original=Current=c.ToString();Owner=owner;}}
    public bool Queue(string mode,IntPtr owner){if(Current==null||Owner!=owner||SingleLetters.Next(Current,mode,Mode)==null)return false;if(pending.Count<64)pending.Enqueue(mode);return true;}
    public bool Take(out string mode,out string next){mode=next=null;if(Current==null||pending.Count==0)return false;mode=pending.Dequeue();next=SingleLetters.Next(Current,mode,Mode);return next!=null;}
    public void Commit(string output,string mode){if(!SingleLetters.Supported(output))throw new ArgumentException("A letter cycle must contain exactly one supported character");Current=output;Mode=mode;}
}
public static class LetterEditGuard {
    public static bool Locate(string text,int start,int end,string expected,out int begin){begin=-1;if(!SingleLetters.Supported(expected)||text==null||start!=end||end<1||end>text.Length||text[end-1]!=expected[0])return false;begin=end-1;return true;}
    public static bool Selected(string text,int start,int end,string expected,int ownedStart,int ownedEnd){return SingleLetters.Supported(expected)&&text!=null&&start==ownedStart&&end==ownedEnd&&end-start==1&&start>=0&&end<=text.Length&&text[start]==expected[0];}
}
public sealed class LeftControlTap {
    bool armed;IntPtr owner;long generation;DateTime pressed;
    public void Cancel(){armed=false;}
    public bool Pending(IntPtr window,DateTime now){return armed&&window==owner&&now>=pressed&&now-pressed<=TimeSpan.FromMilliseconds(600);}
    public void Rebase(long stamp){if(armed)generation=stamp;}
    public static bool IsLeftControl(uint key,uint flags){return key==0xA2||(key==0x11&&(flags&1)==0);}
    public static bool IsRightControl(uint key,uint flags){return key==0xA3||(key==0x11&&(flags&1)!=0);}
    public void Down(IntPtr window,long stamp,DateTime now){if(armed)return;armed=window!=IntPtr.Zero;owner=window;generation=stamp;pressed=now;}
    public bool Up(IntPtr window,long stamp,DateTime now){bool convert=armed&&window==owner&&generation==stamp&&now>=pressed&&now-pressed<=TimeSpan.FromMilliseconds(600);Cancel();return convert;}
}
public static class WordPasteGuard {
    // Exact labels only: never send Escape to an arbitrary Word menu or dialog.
    public static bool Label(string value){if(value==null)return false;value=value.Trim();return string.Equals(value,"Paste Options",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"Paste Options:",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"Paste Options (Ctrl)",StringComparison.OrdinalIgnoreCase)||string.Equals(value,"Paste Options: (Ctrl)",StringComparison.OrdinalIgnoreCase);}
    public static bool Owned(string windowClass,uint ownerProcess,int focusedProcess){return windowClass=="OpusApp"&&ownerProcess!=0&&ownerProcess==(uint)focusedProcess;}
    public static bool DocumentNode(string automationId,bool document,bool menu){return !menu&&(automationId=="Body"||document);}
    public static bool KeepFocusTransition(bool sameOwner,bool pending,bool document,bool pasteMenu){return sameOwner&&pending&&(document||pasteMenu);}
    public static bool Settled(bool document,bool pasteMenu){return document&&!pasteMenu;}
}
public static class WordPasteRecovery {
    // Office may retain a native paste popup even when UIA reports Body focus.
    // Never select text until that popup is gone; never escape an unrecognized dialog.
    public static bool Dismiss(Func<bool> current,Func<bool> pasteMenu,Func<bool> document,Func<bool> escape,Action<int> pause,bool verifiedPaste){
        for(int attempt=0;attempt<2;attempt++){
            if(!current())return false;bool menu=pasteMenu();if(!current()||(!menu&&(!verifiedPaste||attempt>0)))return false;
            if(!escape())return false;
            for(int poll=0;poll<15;poll++){
                if(!current())return false;bool doc=document(),active=pasteMenu();
                if(!current())return false;if(WordPasteGuard.Settled(doc,active))return true;
                pause(20);
            }
        }return false;
    }
}
public sealed class WordPasteContext {
    IntPtr owner;uint sequence;bool document;
    public void Clear(){owner=IntPtr.Zero;}
    public void Mark(IntPtr window,uint clipboardSequence){owner=window;sequence=clipboardSequence;document=false;}
    public bool Owns(IntPtr window,uint clipboardSequence){return owner!=IntPtr.Zero&&window==owner&&clipboardSequence==sequence;}
    public void Verify(IntPtr window,uint clipboardSequence){if(Owns(window,clipboardSequence))document=true;}
    public bool Verified(IntPtr window,uint clipboardSequence){return document&&Owns(window,clipboardSequence);}
    public static string ClipboardWord(string text){if(string.IsNullOrEmpty(text))return null;return CaretWords.AtEnd(text.Length>512?text.Substring(text.Length-512):text);}
    public static bool Matches(string clipboardWord,string actual){return !string.IsNullOrEmpty(clipboardWord)&&!string.IsNullOrEmpty(actual)&&EditGuard.CaseEquivalent(clipboardWord,actual);}
}
public sealed class FontEncoding {
    readonly Dictionary<string,string> formats=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    public FontEncoding(string root){
        foreach(string family in new[]{"Balaram","BellCentennial","ScaBenguit","ScaCheltenham","ScaFrizQuadrata","ScaGoudy","ScaHelvetica","ScaKorinna","ScaOptima","ScaPalatino","ScaSabon","ScaTimes"}){
            foreach(string style in new[]{""," Bold"," Italic"," BoldItalic"," Normal"})formats[Key(family+style)]="balaram";
        }
        foreach(string family in new[]{"Inbenb","Inbeni","Inbeno","Inbenr","Indevr"})formats[Key(family)]="ignore";
        if(root==null)return;string path=Path.Combine(root,"config","font-encoding.tsv");if(!File.Exists(path))return;
        foreach(string row in File.ReadLines(path)){if(row.Trim().Length==0||row.TrimStart().StartsWith("#"))continue;string[] cells=row.Split('\t');if(cells.Length!=2||Key(cells[0]).Length==0||(cells[1]!="unicode"&&cells[1]!="balaram"&&cells[1]!="ignore"))throw new FormatException("Invalid font-encoding.tsv row; use font name, tab, and unicode/balaram/ignore.");formats[Key(cells[0])]=cells[1];}
    }
    static string Key(string value){return Regex.Replace((value??"").Trim().TrimStart('@'),@"\s+"," ");}
    public string Resolve(string font,bool mixed=false){if(mixed)return null;string mode;return formats.TryGetValue(Key(font),out mode)?(mode=="ignore"?null:mode):"unicode";}
}
public sealed class CaretWordSpan {
    public readonly int Start,End,Caret;public readonly string Word,Tail;
    public CaretWordSpan(int start,int end,int caret,string word,string tail){Start=start;End=end;Caret=caret;Word=word;Tail=tail;}
    public int Offset {get{return Caret-Start;}}
}
public static class MixedWordFonts {
    // A Unicode fallback run is evidence only when it contains diacritics,
    // not ordinary letters deliberately set in another font.
    public static string BalaramFamily(string expected,string[] texts,string[] families,FontEncoding fonts,bool[] protectedCharacters=null){
        if(expected==null||expected.Length>160||expected.Any(char.IsWhiteSpace)||texts==null||families==null||texts.Length!=families.Length||texts.Length>160)return null;
        string joined="",family=null;bool fallback=false,protectedDifference=false;if(protectedCharacters!=null&&protectedCharacters.Length!=expected.Length)return null;
        for(int n=0;n<texts.Length;n++){
            string text=texts[n],name=families[n];if(string.IsNullOrEmpty(text)||string.IsNullOrEmpty(name))return null;int offset=joined.Length;joined+=text;
            string mode=fonts.Resolve(name);bool protectedRun=protectedCharacters!=null&&protectedCharacters.Skip(offset).Take(text.Length).All(x=>x);if(protectedRun){protectedDifference|=mode!="balaram";continue;}if(mode==null)return null;
            if(mode=="balaram"){if(family!=null&&!family.Equals(name,StringComparison.OrdinalIgnoreCase))return null;family=name;}
            else{if(!text.All(c=>EncodingMap.IsIndic(c.ToString())||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark))return null;fallback=true;}
        }
        return joined==expected&&(fallback||protectedDifference)?family:null;
    }
}
public static class CaretWords {
    static readonly Regex Word=new Regex(@"[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)*(?:['’]s)?$",RegexOptions.Compiled);
    public static string AtEnd(string prefix){if(string.IsNullOrEmpty(prefix))return null;Match match=Word.Match(prefix);if(match.Success&&match.Length<=160&&EditGuard.LeftBoundary(prefix,match.Index,match.Value))return match.Value;var span=Locate(prefix,prefix.Length,prefix.Length);return span!=null&&span.End==prefix.Length&&(match.Success&&span.Start==match.Index||Numbers.Single(span.Word)!=null)?span.Word:null;}
    static readonly Regex AnywhereWord=new Regex(@"[0-9]+[A-Z][A-Z0-9]*(?![\p{L}\p{M}])|[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)*(?:['’]s)?",RegexOptions.Compiled);
    // A slash can separate prose words, but schemes, drive paths, domains,
    // repeated/leading slashes and filenames must not become editable words.
    static readonly Regex SlashWords=new Regex(@"^[('""“‘\[{]*[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)*(?:['’]s)?(?:/[\p{L}\p{M}]+(?:[-\u2011\u2013][\p{L}\p{M}]+)*(?:['’]s)?)+[)""'”’\]},.!?:;]*$",RegexOptions.Compiled);
    static bool LetterMark(char c){var kind=CharUnicodeInfo.GetUnicodeCategory(c);return char.IsLetter(c)||kind==UnicodeCategory.NonSpacingMark||kind==UnicodeCategory.SpacingCombiningMark;}
    static bool BoundaryCharacter(char c){return LetterMark(c)||char.IsDigit(c)||c=='-'||c=='\u2011'||c=='\u2013'||c=='_'||c=='/'||c=='\\'||c=='@';}
    static bool TailCharacter(char c){return char.IsPunctuation(c)&&c!='_'&&c!='/'&&c!='\\'&&c!='@'&&c!='#';}
    static CaretWordSpan Target(int start,int stop,int caret,string word,string tail,bool componentOnly){
        if(!CompoundForms.Pattern.IsMatch(word)||!componentOnly&&caret==stop&&tail.Length==0)return new CaretWordSpan(start,stop,caret,word,tail);
        var pieces=Regex.Matches(word,@"[\p{L}\p{M}]+(?:['’]s)?");foreach(Match piece in pieces){int begin=start+piece.Index,end=begin+piece.Length;if(begin<=caret&&caret<=end)return new CaretWordSpan(begin,end,caret,piece.Value,"");}
        if(tail.Length>0&&pieces.Count>0){Match last=pieces[pieces.Count-1];return new CaretWordSpan(start+last.Index,start+last.Index+last.Length,caret,last.Value,tail);}return null;
    }
    public static CaretWordSpan Locate(string text,int caret,int selectionEnd,bool componentOnly=false){
        if(text==null||caret!=selectionEnd||caret<0||caret>text.Length)return null;
        int begin=caret,end=caret;while(begin>0&&!char.IsWhiteSpace(text[begin-1])&&caret-begin<=1024)begin--;while(end<text.Length&&!char.IsWhiteSpace(text[end])&&end-caret<=1024)end++;if(end-begin>1024)return null;
        string token=text.Substring(begin,end-begin);bool slash=token.IndexOf('/')>=0;if(token.IndexOfAny(new[]{'\\','@','_'})>=0||token.StartsWith("#",StringComparison.Ordinal)||slash&&!SlashWords.IsMatch(token))return null;
        Match numeric=Numbers.CaretMatch(token);if(numeric.Success){Group number=numeric.Groups["number"];int first=begin+number.Index,last=first+number.Length;if(Numbers.Single(number.Value)==null||caret<first)return null;return new CaretWordSpan(first,last,caret,number.Value,caret>last?text.Substring(last,caret-last):"");}
        CaretWordSpan preceding=null;foreach(Match match in AnywhereWord.Matches(token)){int start=begin+match.Index,stop=start+match.Length;if(match.Length>160||(start>0&&BoundaryCharacter(text[start-1])&&!(slash&&text[start-1]=='/'))||(stop<text.Length&&BoundaryCharacter(text[stop])&&!(slash&&text[stop]=='/')))continue;
            if(start<=caret&&caret<=stop)return Target(start,stop,caret,match.Value,"",componentOnly);
            if(stop<caret){string tail=text.Substring(stop,caret-stop);if(tail.Length<=32&&tail.All(TailCharacter))preceding=Target(start,stop,caret,match.Value,tail,componentOnly);}
        }return preceding;
    }
    public static int NavigationCount(string word){if(string.IsNullOrEmpty(word)||word.Length>160)return 0;foreach(char c in word)if(char.IsSurrogate(c)||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark||CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.SpacingCombiningMark||char.IsControl(c))return 0;return word.Length;}
    public static int ReplacementOffset(int originalOffset,int originalLength,int replacementLength){return originalOffset>=originalLength?replacementLength+originalOffset-originalLength:Math.Min(originalOffset,replacementLength);}
    public static bool MatchesPhysical(string word,int offset,string tail,string last){if(string.IsNullOrEmpty(last))return true;if(offset<=0)return false;string observed=offset<=word.Length?word.Substring(offset-1,1):string.IsNullOrEmpty(tail)?null:tail.Substring(tail.Length-1,1);return observed!=null&&EditGuard.CaseEquivalent(last,observed);}
    public static bool MatchesTyped(string actual,string token,string last){if(last!=null&&!char.IsLetter(last,0)&&CharUnicodeInfo.GetUnicodeCategory(last,0)!=UnicodeCategory.NonSpacingMark)return false;return string.IsNullOrEmpty(token)||AtEnd(token)!=token||EditGuard.CaseEquivalent(token,actual);}
}
public sealed class CaretWordCycle {
    public string Original {get;private set;}
    string current,mode;string[] choices;int index;IntPtr owner;int[] focus;
    public void Clear(){current=null;choices=null;focus=null;Original=null;}
    public string Expected(IntPtr window,int[] field){return choices!=null&&owner==window&&EditGuard.SameFocus(focus,field)?current:null;}
    public bool Next(string text,string format,IntPtr window,int[] field,out string next){next=null;if(current!=text||mode!=format||owner!=window||!EditGuard.SameFocus(focus,field)||choices==null){Clear();return false;}next=choices[(index+1)%choices.Length];return true;}
    public void Start(string output,string[] variants,string format,IntPtr window,int[] field,string original=null){Clear();if(variants==null||variants.Length<1||field==null||field.Length==0)return;int found=Array.IndexOf(variants,output);if(found<0)return;Original=original??output;current=output;choices=(string[])variants.Clone();index=found;mode=format;owner=window;focus=(int[])field.Clone();}
    public void Commit(string output){if(choices==null)return;index=(index+1)%choices.Length;current=output;}
}
// Bounded on-demand discovery: an unavailable first UIA sample is not evidence of a literal key.
public static class CaretLetterDiscovery {
    public static string Expected(bool sameOwner,bool freshPhysical,string cached){return sameOwner&&freshPhysical?cached:null;}
    public static bool Accept(string observed,string physicalExpected,string convertedExpected,bool sameConvertedLocation){if(physicalExpected!=null)return EditGuard.CaseEquivalent(physicalExpected,observed);return !sameConvertedLocation||convertedExpected==null||EditGuard.CaseEquivalent(convertedExpected,observed);}
    public static bool MayTypeLiteral(bool observed,bool supported,bool sameCaret){return observed&&!supported&&sameCaret;}
    public const int Attempts=25,DelayMilliseconds=20;
    public static T Read<T>(Func<T> probe,Func<T,bool> acceptable,Func<bool> current,Action<int> wait) where T:class {
        for(int i=0;i<Attempts;i++){if(!current())return null;T target=probe();if(!current())return null;if(target!=null&&acceptable(target))return target;if(i+1<Attempts)wait(DelayMilliseconds);}return null;
    }
}
public sealed class CaretTapQueue {
    sealed class Request {public IntPtr Owner;public long Generation;public bool Word;}
    readonly Queue<Request> pending=new Queue<Request>();
    public void Clear(){pending.Clear();}
    public void Add(IntPtr owner,long generation,bool word){if(pending.Count<64)pending.Enqueue(new Request{Owner=owner,Generation=generation,Word=word});}
    public bool Take(IntPtr owner,long generation,bool paused,out bool word){word=false;while(pending.Count>0){var next=pending.Dequeue();if(!paused&&owner==next.Owner&&generation==next.Generation){word=next.Word;return true;}}return false;}
}
}

