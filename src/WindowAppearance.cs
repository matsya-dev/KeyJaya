using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyJaya {
// Window-local theme handling: no polling, global hooks, or persistent subscriptions.
public class ThemeWindow : Form {
    readonly HashSet<Control> styled=new HashSet<Control>();
    readonly Dictionary<ListView,int[]> logicalColumns=new Dictionary<ListView,int[]>();
    Point? openAnchor;
    int placementVersion,renderDpi=96;
    Size logicalClientSize;
    Font dpiFont;
    bool applyingDpi,layoutQueued;
    public int RenderDpi{get{return renderDpi;}}
    public TrayPalette Appearance{get;private set;}
    [StructLayout(LayoutKind.Sequential)]struct WindowRect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]struct StartupInfo{public uint Size;public IntPtr Reserved,Desktop,Title;public uint X,Y,Width,Height,CharsX,CharsY,Fill,Flags;public ushort Show,ReservedSize;public IntPtr ReservedBytes,Input,Output,Error;}
    [DllImport("kernel32.dll",EntryPoint="GetStartupInfoW")]static extern void GetStartupInfo(out StartupInfo info);
    [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out WindowRect rect);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out WindowRect rect);
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr window,string app,string classes);
    static bool hiddenStartupHandled;
    void ConsumeHiddenStartup(){
        if(hiddenStartupHandled)return;hiddenStartupHandled=true;StartupInfo info;GetStartupInfo(out info);
        // Consume a hidden launch on a still-hidden HWND. Windows must not apply
        // the resident's startup state to the first settings window the user opens.
        if((info.Flags&1)!=0&&info.Show==0)Native.ShowWindow(Handle,0);
    }
    void RestoreClientSize(){
        if(logicalClientSize.IsEmpty||!IsHandleCreated||IsDisposed)return;
        var desired=new Size((int)Math.Round(logicalClientSize.Width*renderDpi/96.0),(int)Math.Round(logicalClientSize.Height*renderDpi/96.0));
        WindowRect client,outer;if(!GetClientRect(Handle,out client)||!GetWindowRect(Handle,out outer))return;
        int dx=desired.Width-(client.Right-client.Left),dy=desired.Height-(client.Bottom-client.Top);
        if(dx!=0||dy!=0)Native.SetWindowPos(Handle,IntPtr.Zero,0,0,outer.Right-outer.Left+dx,outer.Bottom-outer.Top+dy,0x16);
    }
    void ApplyDpi(int dpi){
        if(applyingDpi||logicalClientSize.IsEmpty||dpi<=0||dpi==renderDpi)return;
        applyingDpi=true;SuspendLayout();try{
            float factor=dpi/(float)renderDpi;renderDpi=dpi;
            // These fixed reference dialogs have one inherited body font. Scale
            // layout once; set an absolute font and client size to avoid drift.
            Scale(new SizeF(factor,factor));
            Font previous=dpiFont;dpiFont=new Font(Font.FontFamily,9f*dpi/96f,FontStyle.Regular,GraphicsUnit.Point);Font=dpiFont;if(previous!=null)previous.Dispose();
            RestoreClientSize();
        }finally{ResumeLayout(true);applyingDpi=false;}
        ApplyAppearance(TrayPalette.Read());
    }
    void SyncMonitorDpi(){if(IsHandleCreated&&!IsDisposed)ApplyDpi((int)Native.GetDpiForWindow(Handle));}
    void QueueDpiLayout(){
        if(layoutQueued||logicalClientSize.IsEmpty||!IsHandleCreated||IsDisposed)return;layoutQueued=true;
        BeginInvoke((Action)(()=>{layoutQueued=false;if(!IsDisposed&&Visible){SyncMonitorDpi();RestoreClientSize();}}));
    }
    protected void PrepareWindow(Size designClientSize){logicalClientSize=designClientSize;OpenAt(Cursor.Position);}
    void PlaceAt(Point anchor){
        Rectangle work=Screen.FromPoint(anchor).WorkingArea;Point point=TrayGeometry.Fit(new Point(work.Left+(work.Width-Width)/2,work.Top+(work.Height-Height)/2),Size,work).Location;
        Native.SetWindowPos(Handle,IntPtr.Zero,point.X,point.Y,0,0,0x15);SyncMonitorDpi();RestoreClientSize();
    }
    public void OpenAt(Point anchor){
        int request=++placementVersion;openAnchor=anchor;StartPosition=FormStartPosition.Manual;
        if(IsHandleCreated){PlaceAt(anchor);BeginInvoke((Action)(()=>{if(!IsDisposed&&request==placementVersion)PlaceAt(anchor);}));}
        else{Rectangle work=Screen.FromPoint(anchor).WorkingArea;Location=new Point(work.Left+(work.Width-Width)/2,work.Top+(work.Height-Height)/2);}
    }
    public void Reveal(){if(!Visible)Show();if(!Native.IsWindowVisible(Handle))Native.ShowWindow(Handle,5);SyncMonitorDpi();RestoreClientSize();BringToFront();Activate();Native.SetForegroundWindow(Handle);}
    void NativeTheme(Control control){if(!control.IsHandleCreated||Appearance==null)return;string name=Appearance.HighContrast?null:Appearance.Dark?(control is ComboBox?"DarkMode_CFD":"DarkMode_Explorer"):null;try{SetWindowTheme(control.Handle,name,null);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
    public void ApplyAppearance(TrayPalette palette){
        if(palette==null)throw new ArgumentNullException("palette");Appearance=palette;
        SuspendLayout();try{Style(this);}finally{ResumeLayout(true);}
        if(IsHandleCreated){int dark=palette.Dark?1:0;try{DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
        Invalidate(true);
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);ConsumeHiddenStartup();ApplyAppearance(TrayPalette.Read());}
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Visible)QueueDpiLayout();}
    protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);if(Visible)QueueDpiLayout();}
    protected override void OnShown(EventArgs e){base.OnShown(e);if(!Native.IsWindowVisible(Handle))Native.ShowWindow(Handle,5);SyncMonitorDpi();RestoreClientSize();ApplyAppearance(TrayPalette.Read());if(openAnchor.HasValue)OpenAt(openAnchor.Value);}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x2E0&&!logicalClientSize.IsEmpty){
            // WinForms auto-resizing is disabled for these dialogs. Own the
            // documented per-monitor DPI message and its suggested placement.
            ApplyDpi((int)((long)m.WParam&0xffff));
            if(m.LParam!=IntPtr.Zero){var rect=(WindowRect)Marshal.PtrToStructure(m.LParam,typeof(WindowRect));Native.SetWindowPos(Handle,IntPtr.Zero,rect.Left,rect.Top,0,0,0x15);}
            RestoreClientSize();QueueDpiLayout();m.Result=IntPtr.Zero;return;
        }
        base.WndProc(ref m);if(!IsHandleCreated||IsDisposed)return;
        if(m.Msg==0x1A||m.Msg==0x31A)BeginInvoke((Action)(()=>{if(!IsDisposed)ApplyAppearance(TrayPalette.Read());}));
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing&&dpiFont!=null){dpiFont.Dispose();dpiFont=null;}}
    void Style(Control control){
        var colors=Appearance;bool input=control is TextBoxBase||control is ListView||control is ComboBox;
        control.BackColor=colors.HighContrast?(input?SystemColors.Window:SystemColors.Control):input?(colors.Dark?Color.FromArgb(40,40,40):Color.White):colors.Background;
        control.ForeColor=control.Enabled?colors.Text:colors.Disabled;
        var page=control as TabPage;if(page!=null)page.UseVisualStyleBackColor=false;
        var tabs=control as ThemeTabs;if(tabs!=null){tabs.Palette=colors;tabs.UpdateMetrics(renderDpi);}
        var button=control as Button;
        var paintedButton=control as ThemeButton;if(paintedButton!=null)paintedButton.Palette=colors;
        if(button!=null){button.UseVisualStyleBackColor=colors.HighContrast;button.FlatStyle=colors.HighContrast?FlatStyle.System:FlatStyle.Flat;button.FlatAppearance.BorderColor=colors.Border;button.FlatAppearance.MouseOverBackColor=colors.Selected;button.FlatAppearance.MouseDownBackColor=colors.Selected;}
        var check=control as CheckBox;
        var paintedCheck=control as ThemeCheckBox;if(paintedCheck!=null)paintedCheck.Palette=colors;
        if(check!=null){check.UseVisualStyleBackColor=colors.HighContrast;check.FlatStyle=colors.HighContrast?FlatStyle.Standard:FlatStyle.Flat;check.FlatAppearance.BorderColor=colors.Border;check.FlatAppearance.CheckedBackColor=colors.Selected;check.FlatAppearance.MouseOverBackColor=colors.Selected;}
        var combo=control as ComboBox;if(combo!=null){combo.FlatStyle=colors.HighContrast?FlatStyle.Standard:FlatStyle.Flat;combo.DrawMode=colors.HighContrast?DrawMode.Normal:DrawMode.OwnerDrawFixed;combo.ItemHeight=combo.Font.Height+(int)Math.Round(6*renderDpi/96.0);}
        var list=control as ListView;if(list!=null){
            list.OwnerDraw=!colors.HighContrast;int[] widths;
            if(!logicalColumns.TryGetValue(list,out widths)){widths=new int[list.Columns.Count];for(int n=0;n<widths.Length;n++)widths[n]=list.Columns[n].Width;logicalColumns.Add(list,widths);}
            for(int n=0;n<widths.Length-1;n++)list.Columns[n].Width=(int)Math.Round(widths[n]*renderDpi/96.0);
        }
        if(styled.Add(control)){
            control.ControlAdded+=(s,e)=>{if(Appearance!=null)Style(e.Control);};
            control.EnabledChanged+=(s,e)=>{if(Appearance!=null)control.ForeColor=control.Enabled?Appearance.Text:Appearance.Disabled;};
            if(input){control.HandleCreated+=(s,e)=>NativeTheme(control);}
            if(combo!=null)combo.DrawItem+=DrawCombo;
            if(list!=null){bool sizing=false;Action fit=()=>{if(sizing||list.Columns.Count==0)return;try{sizing=true;int used=0;for(int n=0;n<list.Columns.Count-1;n++)used+=list.Columns[n].Width;list.Columns[list.Columns.Count-1].Width=Math.Max(50,list.ClientSize.Width-used-4);}finally{sizing=false;}};list.Resize+=(s,e)=>fit();list.ColumnWidthChanged+=(s,e)=>fit();list.DrawColumnHeader+=DrawHeader;list.DrawItem+=(s,e)=>{if(e.Item.ListView.View!=View.Details)e.DrawDefault=true;};list.DrawSubItem+=DrawCell;fit();}
        }
        if(input)NativeTheme(control);
        foreach(Control child in control.Controls)Style(child);
    }
    void DrawCombo(object sender,DrawItemEventArgs e){
        var combo=(ComboBox)sender;var colors=Appearance;bool selected=(e.State&DrawItemState.Selected)!=0;
        using(var brush=new SolidBrush(selected?colors.Selected:combo.BackColor))e.Graphics.FillRectangle(brush,e.Bounds);
        if(e.Index>=0&&e.Index<combo.Items.Count)TextRenderer.DrawText(e.Graphics,combo.GetItemText(combo.Items[e.Index]),combo.Font,e.Bounds,!combo.Enabled?colors.Disabled:selected?colors.SelectedText:colors.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if((e.State&DrawItemState.Focus)!=0)e.DrawFocusRectangle();
    }
    void DrawHeader(object sender,DrawListViewColumnHeaderEventArgs e){
        var colors=Appearance;using(var brush=new SolidBrush(colors.Selected))e.Graphics.FillRectangle(brush,e.Bounds);
        using(var pen=new Pen(colors.Border)){e.Graphics.DrawLine(pen,e.Bounds.Left,e.Bounds.Bottom-1,e.Bounds.Right,e.Bounds.Bottom-1);e.Graphics.DrawLine(pen,e.Bounds.Right-1,e.Bounds.Top,e.Bounds.Right-1,e.Bounds.Bottom);}
        var bounds=e.Bounds;bounds.Inflate(-5,0);TextRenderer.DrawText(e.Graphics,e.Header.Text,e.Font,bounds,colors.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
    void DrawCell(object sender,DrawListViewSubItemEventArgs e){
        var list=(ListView)sender;var colors=Appearance;bool selected=e.Item.Selected;Color background=selected?colors.Selected:list.BackColor,foreground=selected?colors.SelectedText:colors.Text;
        using(var brush=new SolidBrush(background))e.Graphics.FillRectangle(brush,e.Bounds);
        var text=e.Bounds;text.Inflate(-5,0);
        if(list.CheckBoxes&&e.ColumnIndex==0){
            int side=Math.Max(9,Math.Min(e.Bounds.Height-4,(int)Math.Round(13*list.Font.Size/9f)));var box=new Rectangle(e.Bounds.Left+4,e.Bounds.Top+(e.Bounds.Height-side)/2,side,side);
            using(var fill=new SolidBrush(list.BackColor))e.Graphics.FillRectangle(fill,box);using(var pen=new Pen(colors.Border))e.Graphics.DrawRectangle(pen,box);
            if(e.Item.Checked)using(var pen=new Pen(foreground,Math.Max(1.5f,side/7f))){e.Graphics.DrawLines(pen,new[]{new Point(box.Left+side/5,box.Top+side/2),new Point(box.Left+side*2/5,box.Top+side*3/4),new Point(box.Left+side*4/5,box.Top+side/4)});}
            text.X=box.Right+5;text.Width=Math.Max(1,e.Bounds.Right-text.X-4);
        }
        TextRenderer.DrawText(e.Graphics,e.SubItem.Text,list.Font,text,foreground,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if(e.Item.Focused&&list.Focused&&e.ColumnIndex==0)ControlPaint.DrawFocusRectangle(e.Graphics,e.Item.Bounds,foreground,background);
    }
}

// Native disabled button/checkbox painting can ignore ForeColor. Paint those two
// surfaces explicitly while keeping their normal focus, input and accessibility.
public sealed class ThemeButton : Button {
    public TrayPalette Palette{get;set;}
    public override Size GetPreferredSize(Size proposedSize){float scale=Font.Size/9f;Size text=TextRenderer.MeasureText(Text,Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);return new Size(Math.Max((int)Math.Round(75*scale),text.Width+(int)Math.Round(20*scale)),Math.Max((int)Math.Round(23*scale),Font.Height+(int)Math.Round(8*scale)));}
    protected override void OnPaint(PaintEventArgs e){var p=Palette;if(p==null||p.HighContrast){base.OnPaint(e);return;}bool hover=Enabled&&ClientRectangle.Contains(PointToClient(Cursor.Position));e.Graphics.Clear(hover?p.Selected:BackColor);using(var pen=new Pen(p.Border))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?p.Text:p.Disabled,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(ClientRectangle,-3,-3),p.Text,BackColor);}
}
public sealed class ThemeCheckBox : CheckBox {
    public TrayPalette Palette{get;set;}
    protected override void OnPaint(PaintEventArgs e){var p=Palette;if(p==null||p.HighContrast){base.OnPaint(e);return;}e.Graphics.Clear(BackColor);Color text=Enabled?p.Text:p.Disabled;int side=Math.Max(9,Math.Min(Height-2,(int)Math.Round(13*Font.Size/9f)));var box=new Rectangle(1,(Height-side)/2,side,side);using(var fill=new SolidBrush(p.Background))e.Graphics.FillRectangle(fill,box);using(var pen=new Pen(text))e.Graphics.DrawRectangle(pen,box);if(Checked)using(var pen=new Pen(text,Math.Max(1.5f,side/7f)))e.Graphics.DrawLines(pen,new[]{new Point(box.Left+side/5,box.Top+side/2),new Point(box.Left+side*2/5,box.Top+side*3/4),new Point(box.Left+side*4/5,box.Top+side/4)});var bounds=new Rectangle(box.Right+5,0,Math.Max(1,Width-box.Right-5),Height);TextRenderer.DrawText(e.Graphics,Text,Font,bounds,text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,bounds,text,BackColor);}
}

public sealed class ThemeTabs : TabControl {
    public TrayPalette Palette{get;set;}
    public ThemeTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    public void UpdateMetrics(int dpi){int width=0;foreach(TabPage page in TabPages)width=Math.Max(width,TextRenderer.MeasureText(page.Text,Font).Width);SizeMode=TabSizeMode.Fixed;ItemSize=new Size(width+(int)Math.Round(20*dpi/96.0),Font.Height+(int)Math.Round(10*dpi/96.0));}
    protected override void OnPaint(PaintEventArgs e){
        var colors=Palette??TrayPalette.Read();e.Graphics.Clear(colors.Background);
        for(int n=0;n<TabCount;n++){
            Rectangle bounds=GetTabRect(n);bool selected=n==SelectedIndex;
            using(var brush=new SolidBrush(selected?colors.Selected:colors.Background))e.Graphics.FillRectangle(brush,bounds);
            using(var pen=new Pen(colors.Border))e.Graphics.DrawRectangle(pen,bounds.X,bounds.Y,Math.Max(1,bounds.Width-1),Math.Max(1,bounds.Height-1));
            TextRenderer.DrawText(e.Graphics,TabPages[n].Text,Font,bounds,Enabled?colors.Text:colors.Disabled,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            if(selected&&Focused)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(bounds,-3,-3),colors.Text,colors.Selected);
        }
        base.OnPaint(e);
    }
}
}






