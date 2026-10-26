// KeyJaya: InDesign document-text bridge for an owned selection or insertion point.
// Formatting and undo are preserved through the application's text interfaces, not document-file rewriting.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace KeyJaya
{
    using System.Windows.Automation;

    // InDesign's custom canvas has no usable Windows text range. Use its native DOM,
    // on demand only, and revalidate document, story, selection and text before edits.
    public static class InDesignSelection
    {
        public static Action<string> Trace;
        static readonly object gate = new object ();
        static readonly JavaScriptSerializer json = new JavaScriptSerializer();
        public static bool IsInDesign(IntPtr owner)
        {
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(owner, out pid);
                return Native.ProcessImageIs(pid, "InDesign.exe");
            }
            catch
            {
                return false;
            }
        }

        public static bool FieldFocused(IntPtr owner)
        {
            if (InDesignFindField.Focused(owner))
                return true;
            try
            {
                if (!IsInDesign(owner) || owner != Native.GetForegroundWindow())
                    return false;
                var focus = AutomationElement.FocusedElement;
                if (focus == null)
                    return false;
                uint pid;
                Native.GetWindowThreadProcessId(owner, out pid);
                if (focus.Current.ProcessId != (int)pid)
                    return false;
                object value;
                if (focus.Current.ControlType == ControlType.Edit)
                    return true;
                if (focus.TryGetCurrentPattern(ValuePattern.Pattern, out value) && !((ValuePattern)value).Current.IsReadOnly)
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool UseDocumentBridge(IntPtr owner)
        {
            if (!IsInDesign(owner) || FieldFocused(owner))
                return false;
            try
            {
                var node = AutomationElement.FocusedElement;
                for (int n = 0; n < 16 && node != null; n++)
                {
                    // A search dialog must not fall through to the document's stale selection.
                    if (node.Current.ControlType == ControlType.Window)
                    {
                        string label = (node.Current.Name ?? "").Replace("&", "").Replace(" ", "");
                        if (label.Equals("Find/Change", StringComparison.OrdinalIgnoreCase) || label.Equals("FindandReplace", StringComparison.OrdinalIgnoreCase))
                            return false;
                    }

                    node = TreeWalker.ControlViewWalker.GetParent(node);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string Quote(string text)
        {
            return json.Serialize(text).Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029");
        }

        public const string Prelude = @"function info(){if(!app.documents.length||app.selection.length!=1)return null;var s=app.selection[0],k=s.constructor.name;if(!/^(InsertionPoint|Text|Character|Word|Line|Paragraph|TextStyleRange)$/.test(k))return null;try{var st=s.parentStory,d=app.activeDocument,node=s;for(var n=0;n<12&&node;n++){var name=node.constructor.name;if(name=='Footnote'||name=='Endnote'){st=node;break;}if(name=='Cell')return null;if(name=='Story'||name=='Document')break;node=node.parent;}var base=st.insertionPoints.firstItem().index,a=s.insertionPoints.firstItem().index-base,b=s.insertionPoints.lastItem().index-base;var frames=s.parentTextFrames;for(var z=0;z<frames.length;z++)if(frames[z].locked||frames[z].itemLayer.locked)return null;return {s:s,st:st,d:d,a:a,b:b};}catch(e){return null;}}function txt(st,a,b){if(b<=a)return '';var raw=st.characters.itemByRange(a,b-1).contents,value='';if(typeof raw=='string')value=raw;else if(raw instanceof Array){for(var n=0;n<raw.length;n++)value+=typeof raw[n]=='string'?raw[n]:'\ufffc';}else value='\ufffc';return value.replace(/\r/g,'\n');}function same(q,d,st,a,b){return q&&q.d.id==d&&q.st.id==st&&q.a==a&&q.b==b;}function selectRange(st,a,b){if(a==b)app.select(st.insertionPoints.item(a));else app.select(st.characters.itemByRange(a,b-1));}function protectedStyle(c,d){try{if(c.appliedCharacterStyle.id!=d.characterStyles.firstItem().id)return true;var p=c.appliedParagraphStyle;for(var n=0;n<8&&p&&p.isValid;n++){if(/^(Heading(?:[ _-]?[1-9])?|Title|Subtitle)(?:$|[ _-])/i.test(p.name))return true;var next=p.basedOn;if(!next||!next.isValid||next.id==p.id)break;p=next;}}catch(e){return true;}return false;}function enc(x){return encodeURIComponent(String(x));}";
        static string Run(IntPtr owner, string script, bool edit = false)
        {
            if (owner != Native.GetForegroundWindow() || !UseDocumentBridge(owner))
                return null;
            lock (gate)
            {
                object app = null;
                try
                {
                    // Activating the registered COM server connects to the existing singleton;
                    // never do this unless its verified foreground process is already running.
                    string prog = "InDesign.Application";
                    using (var root = Registry.ClassesRoot.OpenSubKey(prog + "\\CurVer"))
                    {
                        if (root != null)
                        {
                            string current = root.GetValue(null) as string;
                            if (!string.IsNullOrEmpty(current))
                                prog = current;
                        }
                    }

                    Type type = Type.GetTypeFromProgID(prog);
                    if (type == null)
                        return null;
                    app = Activator.CreateInstance(type);
                    if (owner != Native.GetForegroundWindow())
                        return null;
                    object result = app.GetType().InvokeMember("DoScript", BindingFlags.InvokeMethod, null, app, new object[] { "(function(){" + Prelude + script + "})()", 1246973031, Type.Missing, edit ? 1699963733 : 1699967573, "KeyJaya conversion" });
                    return result as string;
                }
                catch (Exception ex)
                {
                    if (Trace != null)
                        Trace("InDesign bridge error=" + ex.GetType().Name + " code=" + ex.HResult);
                    return null;
                }
                finally
                {
                    if (app != null && Marshal.IsComObject(app))
                        Marshal.ReleaseComObject(app);
                }
            }
        }

        public sealed class Snapshot
        {
            public IntPtr Owner;
            public int Document, Story, SelectionStart, SelectionEnd, Start, End, Offset;
            public string Text, Left, Right, Font, Tail = "";
            public bool Mixed;
            public string[] Fonts, Styles;
            string Guard()
            {
                return "var q=info();if(!same(q," + Document + "," + Story + ",A,B)||txt(q.st," + Start + "," + End + ")!=" + Quote(Text) + ")return '';";
            }

            public bool Matches(bool caret)
            {
                int a = caret ? SelectionStart : Start, b = caret ? SelectionEnd : End;
                return Run(Owner, "var A=" + a + ",B=" + b + ";" + Guard() + "return 'OK';") == "OK";
            }

            public bool Select()
            {
                return Run(Owner, "var A=" + SelectionStart + ",B=" + SelectionEnd + ";" + Guard() + "selectRange(q.st," + Start + "," + End + ");return 'OK';") == "OK";
            }

            public bool CollapseSelected()
            {
                return Run(Owner, "var A=" + Start + ",B=" + End + ";" + Guard() + "selectRange(q.st,B,B);return 'OK';") == "OK";
            }

            public bool RestoreCaret()
            {
                return Run(Owner, "var q=info();if(!q||q.d.id!=" + Document + "||q.st.id!=" + Story + "||txt(q.st," + Start + "," + End + ")!=" + Quote(Text) + "||q.a<" + Start + "||q.b>" + End + ")return '';selectRange(q.st," + SelectionStart + "," + SelectionEnd + ");return 'OK';") == "OK";
            }

            public bool SameStart(Snapshot other)
            {
                return other != null && Owner == other.Owner && Document == other.Document && Story == other.Story && Start == other.Start;
            }

            public string Key
            {
                get
                {
                    return Owner + ":" + Document + ":" + Story + ":" + Start;
                }
            }

            public bool Position(string output, int offset)
            {
                if (offset < 0 || offset > output.Length + Tail.Length)
                    return false;
                return Run(Owner, "var q=info();if(!same(q," + Document + "," + Story + "," + (Start + output.Length) + "," + (Start + output.Length) + ")||txt(q.st," + Start + "," + (Start + output.Length) + ")!=" + Quote(output) + "||txt(q.st," + (Start + output.Length) + "," + (Start + output.Length + Tail.Length) + ")!=" + Quote(Tail) + ")return '';selectRange(q.st," + (Start + offset) + "," + (Start + offset) + ");return 'OK';") == "OK";
            }

            public bool Replace(string output, bool retain, ItalicSpan[] italics = null, Snapshot restore = null)
            {
                string script = ReplaceScript(output, retain, italics, restore);
                return script != null && Run(Owner, script, true) == "OK";
            }

            public string ReplaceScript(string output, bool retain, ItalicSpan[] italics = null, Snapshot restore = null)
            {
                if (output == null || output.Length > 200000 || italics != null && italics.Any(x => x.Start < 0 || x.Length < 1 || x.Start > output.Length - x.Length))
                    return null;
                var patches = SelectionFormatting.Plan(Text, output);
                if (patches == null)
                    return null;
                string script = "var A=" + Start + ",B=" + End + ";" + Guard() + "var typingStyle=String(q.st.insertionPoints.item(B).fontStyle);var patches=" + json.Serialize(patches.Select(p => new { offset = p.Offset, before = p.Before, after = p.After }).ToArray()) + ";";
                // Preflight every patch before the transaction changes any character.
                script += "for(var n=0;n<patches.length;n++){var p=patches[n];if(txt(q.st,A+p.offset,A+p.offset+p.before.length)!=p.before)return '';}var done=[];try{for(var n=0;n<patches.length;n++){var p=patches[n];if(p.before.length)q.st.characters.itemByRange(A+p.offset,A+p.offset+p.before.length-1).contents=p.after.replace(/\\n/g,'\\r');else q.st.insertionPoints.item(A+p.offset).contents=p.after.replace(/\\n/g,'\\r');done.push(p);}if(txt(q.st,A,A+" + output.Length + ")!=" + Quote(output) + ")throw Error('result');";
                if (restore != null && restore.Text == output && restore.Fonts != null)
                {
                    script += "var ff=" + json.Serialize(restore.Fonts) + ",ss=" + json.Serialize(restore.Styles) + ";for(var n=0;n<ff.length;n++){var c=q.st.characters.item(A+n);c.appliedFont=app.fonts.itemByName(ff[n]);c.fontStyle=ss[n];}";
                }

                if (italics != null)
                {
                    script += "var spans=" + json.Serialize(italics.Select(s => new { start = s.Start, length = s.Length, force = s.Force, italic = s.Italic }).ToArray()) + ";for(var n=0;n<spans.length;n++){var x=spans[n];for(var j=0;j<x.length;j++){var c=q.st.characters.item(A+x.start+j),style=String(c.fontStyle),hasItalic=/italic|oblique/i.test(style);if(protectedStyle(c,q.d)&&!x.force)continue;if(x.italic&&hasItalic||!x.italic&&!hasItalic)continue;if(x.italic&&/bold/i.test(style)&&!x.force)continue;var family=c.appliedFont.fontFamily,target=x.italic?(/bold/i.test(style)?'Bold Italic':'Italic'):style.replace(/\\s*(Italic|Oblique)\\b/ig,'').replace(/^\\s+|\\s+$/g,'');if(!target)target='Regular';var f=app.fonts.itemByName(family+'\\t'+target);if(!f.isValid&&!x.italic&&!/bold|demi|heavy/i.test(style)){var normal=['Regular','Roman','Book','Normal'];for(var z=0;z<normal.length;z++){f=app.fonts.itemByName(family+'\\t'+normal[z]);if(f.isValid)break;}}if(f.isValid)c.appliedFont=f;}}";
                }

                if (retain)
                    script += "selectRange(q.st,A,A+" + output.Length + ");";
                if (!retain)
                    script += "selectRange(q.st,A+" + output.Length + ",A+" + output.Length + ");if(/^(Regular|Roman|Book|Normal|Italic|Oblique|Bold|Bold Italic)$/i.test(typingStyle))q.st.insertionPoints.item(A+" + output.Length + ").fontStyle=typingStyle;";
                script += "return 'OK';}catch(e){for(var r=done.length-1;r>=0;r--){var p=done[r];if(p.after.length)q.st.characters.itemByRange(A+p.offset,A+p.offset+p.after.length-1).contents=p.before.replace(/\\n/g,'\\r');else q.st.insertionPoints.item(A+p.offset).contents=p.before.replace(/\\n/g,'\\r');}selectRange(q.st,A,B);return ''; }";
                return script;
            }
        }

        public static Snapshot Read(IntPtr owner)
        {
            string raw = Run(owner, @"var q=info();if(!q||q.b-q.a>200000)return '';var a=Math.max(0,q.a-321),b=Math.min(q.st.characters.length,q.b+321),f='',mixed=false;try{f=String(q.s.appliedFont.name);if(q.b>q.a){for(var n=q.a;n<Math.min(q.b,q.a+1024);n++)if(String(q.st.characters.item(n).appliedFont.name)!=f)mixed=true;if(q.b-q.a>1024)mixed=true;}}catch(e){mixed=true;}return [q.d.id,q.st.id,q.a,q.b,enc(txt(q.st,a,q.a)),enc(txt(q.st,q.a,q.b)),enc(txt(q.st,q.b,b)),enc(f),mixed?'1':'0'].join('|');");
            if (string.IsNullOrEmpty(raw))
                return null;
            try
            {
                string[] f = raw.Split('|');
                if (f.Length != 9)
                    return null;
                return new Snapshot
                {
                    Owner = owner,
                    Document = int.Parse(f[0]),
                    Story = int.Parse(f[1]),
                    SelectionStart = int.Parse(f[2]),
                    SelectionEnd = int.Parse(f[3]),
                    Start = int.Parse(f[2]),
                    End = int.Parse(f[3]),
                    Left = Uri.UnescapeDataString(f[4]),
                    Text = Uri.UnescapeDataString(f[5]),
                    Right = Uri.UnescapeDataString(f[6]),
                    Font = Uri.UnescapeDataString(f[7]),
                    Mixed = f[8] == "1"
                };
            }
            catch
            {
                return null;
            }
        }

        public static bool Describe(Snapshot s)
        {
            string raw = Run(s.Owner, "var q=info();if(!same(q," + s.Document + "," + s.Story + "," + s.SelectionStart + "," + s.SelectionEnd + ")||txt(q.st," + s.Start + "," + s.End + ")!=" + Quote(s.Text) + ")return '';var f=[],styles=[];for(var n=" + s.Start + ";n<" + s.End + ";n++){var c=q.st.characters.item(n);f.push(enc(c.appliedFont.name));styles.push(enc(c.fontStyle));}return f.join(',')+'|'+styles.join(',');");
            if (raw == null)
                return false;
            string[] fields = raw.Split('|');
            if (fields.Length != 2)
                return false;
            s.Fonts = fields[0].Length == 0 ? new string[0] : fields[0].Split(',').Select(Uri.UnescapeDataString).ToArray();
            s.Styles = fields[1].Length == 0 ? new string[0] : fields[1].Split(',').Select(Uri.UnescapeDataString).ToArray();
            s.Font = s.Fonts.FirstOrDefault() ?? s.Font;
            s.Mixed = s.Fonts.Any(f => f != s.Font);
            return true;
        }
    }
}
