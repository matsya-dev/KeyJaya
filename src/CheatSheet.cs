using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace KeyJaya {
public sealed class CheatEntry {
    public readonly string Input,Action;
    public CheatEntry(string input,string action){Input=input;Action=action;}
}
public static class CheatSheet {
    public static CheatEntry[] Shortcuts(ShortcutSettings settings=null){var rows=new[]{
        new CheatEntry("Ctrl+Z","Undo the last selected-text conversion"),
        new CheatEntry("Ctrl+.","Convert selected or copied text to Unicode / IAST"),
        new CheatEntry("Ctrl+,","Convert selected or copied text to Balaram"),
        new CheatEntry("Ctrl+/","Convert selected or copied text to plain text"),
        new CheatEntry("Ctrl+\\","Convert selected or copied text to ITRANS or Hindi Devanagari; choose in tray"),
        new CheatEntry("Ctrl+[","Convert selected or copied text to Sanskrit Devanagari"),
        new CheatEntry("Ctrl+]","Convert selected or copied text to Bengali"),
        new CheatEntry("Backtick (`)","Cycle the previous letter; tray encoding overrides font detection"),
        new CheatEntry("Left Ctrl tap","Convert a word, short trigger, or single number. Passage numbers stay unchanged."),
        new CheatEntry("24 + "+(settings==null?"Left Ctrl":settings["word"].Text),"twenty-four; select only the number or place the caret inside/after it"),
        new CheatEntry("24th + "+(settings==null?"Left Ctrl":settings["word"].Text),"twenty-fourth; numbers in longer selections stay unchanged"),
        new CheatEntry("Inside a compound","Convert only that component. After punctuation: the final component."),
        new CheatEntry("Repeat >, <, or ?","Loop through spellings and plain text"),
        new CheatEntry("Repeat shortcut","Loop through spellings and plain text"),
        new CheatEntry("Backspace","Undo the last manual conversion"),
        new CheatEntry("Precise script conversion","Tray setting: follow supplied sounds without spelling guesses"),
        new CheatEntry("Ctrl+\\ output","Tray choice: ITRANS or Hindi; Sanskrit and Bengali always available"),
        new CheatEntry("Italicize conversions","Tray toggle: BBT/BVT style; names, places, festivals and loanwords stay roman"),
        new CheatEntry("Keep verse markers and numbers","Tray setting: retain verse structure"),
        };if(settings!=null){for(int n=0;n<8;n++){string label=n==6?"Backtick (`)":n==7?"Left Ctrl tap":ShortcutSettings.Defaults[n];for(int j=0;j<rows.Length;j++)if(rows[j].Input==label)rows[j]=new CheatEntry(settings[ShortcutSettings.Roles[n]].Text,rows[j].Action);}for(int j=0;j<rows.Length;j++)if(rows[j].Input=="Ctrl+\\ output")rows[j]=new CheatEntry(settings["itrans"].Text+" output",rows[j].Action);}return rows;}
    public static CheatEntry[] Triggers(IDictionary<string,string> personal){
        var rows=new List<CheatEntry>{
            new CheatEntry("SP + Left Ctrl","Expand a short trigger using the selected encoding; case-sensitive"),new CheatEntry("SPG + Left Ctrl","Supreme Personality of Godhead in either encoding"),new CheatEntry("word>","Unicode / IAST: krsna> → kṛṣṇa"),
            new CheatEntry("word<","Balaram: krsna< → kåñëa"),
            new CheatEntry("word?","Plain spelling: krsna? → krsna"),
            new CheatEntry("doesnt + space","Add an apostrophe: doesn’t"),
            new CheatEntry("teh + space","Correct spelling: the"),
            
            
            new CheatEntry("BG9.1>","Insert a scripture verse"),
            new CheatEntry("BG9.1T>","Insert its translation"),
            new CheatEntry("BG9.1VT>","Insert verse and translation"),
            new CheatEntry("BG9.1VTR>","Include verse, translation, and reference")};
        foreach(var row in personal.OrderBy(x=>x.Key,StringComparer.Ordinal))rows.Add(new CheatEntry(row.Key,row.Value));
        return rows.ToArray();
    }
    public static CheatEntry[] Filter(IEnumerable<CheatEntry> rows,string query){query=(query??"").Trim();return rows.Where(row=>query.Length==0||row.Input.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||row.Action.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();}
}
public sealed class CheatSheetWindow : ThemeWindow {
    readonly TabControl tabs=new ThemeTabs();
    readonly Font referenceFont=new Font("Segoe UI",9f);
    public CheatSheetWindow(IDictionary<string,string> entries,ShortcutSettings settings=null,Action customizeShortcuts=null,Action editTriggers=null){
        Text="KeyJaya cheat sheet";AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.None;
        Font=referenceFont;ClientSize=new Size(560,320);Padding=new Padding(10);
        StartPosition=FormStartPosition.CenterScreen;ShowInTaskbar=true;MinimizeBox=MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedDialog;
        tabs.Dock=DockStyle.Fill;
        var shortcuts=new TabPage("Shortcuts");var shortcutList=List();Fill(shortcutList,CheatSheet.Shortcuts(settings));
        var hint=new Label{Text="Select text, then press a conversion shortcut. Copying first is optional.",Dock=DockStyle.Top,Height=34};
        shortcuts.Controls.Add(shortcutList);shortcuts.Controls.Add(hint);tabs.TabPages.Add(shortcuts);
        var triggers=new TabPage("Triggers");var triggerList=List();var rows=CheatSheet.Triggers(entries);Fill(triggerList,rows);
        var search=new TextBox{Dock=DockStyle.Top};search.AccessibleName="Find a trigger";
        search.TextChanged+=(sender,args)=>Fill(triggerList,CheatSheet.Filter(rows,search.Text));
        var searchLabel=new Label{Text="Find a trigger:",Dock=DockStyle.Top,Height=22};
        triggers.Controls.Add(triggerList);triggers.Controls.Add(search);triggers.Controls.Add(searchLabel);tabs.TabPages.Add(triggers);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=38,FlowDirection=FlowDirection.RightToLeft};
        var close=new ThemeButton{Text="Close",AutoSize=true,DialogResult=DialogResult.Cancel};close.Click+=(sender,args)=>Close();footer.Controls.Add(close);CancelButton=close;
        if(customizeShortcuts!=null){var customize=new ThemeButton{Text="Customize shortcuts…",AutoSize=true};customize.Click+=(sender,args)=>customizeShortcuts();footer.Controls.Add(customize);}
        if(editTriggers!=null){var edit=new ThemeButton{Name="EditTriggers",Text="Edit triggers…",AutoSize=true};edit.Click+=(sender,args)=>editTriggers();footer.Controls.Add(edit);}
        Controls.Add(tabs);Controls.Add(footer);PrepareWindow(new Size(560,320));
    }

    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)referenceFont.Dispose();}
    static ListView List(){var view=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,HideSelection=false,ShowItemToolTips=true};view.Columns.Add("Shortcut / trigger",175);view.Columns.Add("What it does",340);return view;}
    static void Fill(ListView view,IEnumerable<CheatEntry> rows){view.BeginUpdate();try{view.Items.Clear();foreach(var row in rows){var item=new ListViewItem(row.Input);item.SubItems.Add(row.Action.Replace("\r","").Replace("\n"," ↵ "));item.ToolTipText=row.Action;view.Items.Add(item);}}finally{view.EndUpdate();}}
}
}


