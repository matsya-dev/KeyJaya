using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyJaya {
// Re-evaluate on opening, not on a background timer. High contrast always wins.
public sealed class TrayPalette {
    public readonly bool Dark,HighContrast;
    public readonly Color Background,Text,Disabled,Selected,SelectedText,Border,Separator;
    public TrayPalette(bool dark,bool highContrast){
        HighContrast=highContrast;Dark=dark&&!highContrast;
        Background=highContrast?SystemColors.Menu:Dark?Color.FromArgb(32,32,32):Color.FromArgb(250,250,250);
        Text=highContrast?SystemColors.MenuText:Dark?Color.FromArgb(242,242,242):Color.FromArgb(24,24,24);
        Disabled=highContrast?SystemColors.GrayText:Dark?Color.FromArgb(155,155,155):Color.FromArgb(110,110,110);
        Selected=highContrast?SystemColors.Highlight:Dark?Color.FromArgb(58,58,58):Color.FromArgb(229,229,229);
        SelectedText=highContrast?SystemColors.HighlightText:Text;
        Border=highContrast?SystemColors.WindowText:Dark?Color.FromArgb(85,85,85):Color.FromArgb(190,190,190);
        Separator=highContrast?SystemColors.GrayText:Dark?Color.FromArgb(70,70,70):Color.FromArgb(215,215,215);
    }
    public static TrayPalette Read(){
        bool dark=false;
        try{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")){
            object value=key==null?null:key.GetValue("AppsUseLightTheme",key.GetValue("SystemUsesLightTheme",1));
            dark=value is int&&(int)value==0;
        }}catch(SecurityException){}catch(UnauthorizedAccessException){}catch(System.IO.IOException){}
        return new TrayPalette(dark,SystemInformation.HighContrast);
    }
}

public static class TrayGeometry {
    public static int MonitorDpi(Point point){try{uint x,y;if(Native.GetDpiForMonitor(Native.MonitorFromPoint(new Native.Point{X=point.X,Y=point.Y},2),0,out x,out y)==0)return Dpi((int)x);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}return 96;}
    public static int Dpi(int dpi){return dpi>=48&&dpi<=768?dpi:96;}
    public static int Pixels(int value,int dpi){return Math.Max(1,(int)Math.Round(value*Dpi(dpi)/96.0));}
    public static Rectangle Fit(Point anchor,Size requested,Rectangle work){
        int width=Math.Min(Math.Max(1,requested.Width),Math.Max(1,work.Width));
        int height=Math.Min(Math.Max(1,requested.Height),Math.Max(1,work.Height));
        return new Rectangle(Math.Max(work.Left,Math.Min(anchor.X,work.Right-width)),Math.Max(work.Top,Math.Min(anchor.Y,work.Bottom-height)),width,height);
    }
}

sealed class TrayRenderer : ToolStripProfessionalRenderer {
    readonly TrayPalette colors;readonly int dpi;
    int P(int value){return TrayGeometry.Pixels(value,dpi);}
    public TrayRenderer(TrayPalette palette,int scale){colors=palette;dpi=scale;RoundedEdges=false;}
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){using(var brush=new SolidBrush(colors.Background))e.Graphics.FillRectangle(brush,e.AffectedBounds);}
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var pen=new Pen(colors.Border))e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
        bool selected=e.Item.Selected&&e.Item.Enabled;
        using(var brush=new SolidBrush(selected?colors.Selected:colors.Background))e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));
        var item=e.Item as ToolStripMenuItem;
        if(item!=null&&item.Checked){
            var previous=e.Graphics.SmoothingMode;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(!item.Enabled?colors.Disabled:selected?colors.SelectedText:colors.Text,Math.Max(1.5f,P(2)))){
                float left=P(8),middle=item.Height/2f;
                e.Graphics.DrawLines(pen,new[]{new PointF(left,middle),new PointF(left+P(3),middle+P(3)),new PointF(left+P(9),middle-P(4))});
            }e.Graphics.SmoothingMode=previous;
        }
    }
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){}
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){
        e.TextColor=!e.Item.Enabled?colors.Disabled:e.Item.Selected?colors.SelectedText:colors.Text;
        e.TextRectangle=new Rectangle(P(29),0,Math.Max(1,e.Item.Width-P(41)),e.Item.Height);
        e.TextFormat=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;
        base.OnRenderItemText(e);
    }
    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(colors.Separator))e.Graphics.DrawLine(pen,P(8),e.Item.Height/2,e.Item.Width-P(8),e.Item.Height/2);}
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){e.ArrowColor=e.Item!=null&&!e.Item.Enabled?colors.Disabled:colors.Text;base.OnRenderArrow(e);}
}

// Both NotifyIcon's right-click path and the explicit left-click path use this menu.
// Pixel fonts and absolute metrics avoid accumulating scaling across monitor moves.
public sealed class TrayPopupMenu : ContextMenuStrip {
    Font ownedFont;Point? requestedAnchor;Rectangle workingArea;
    public int MenuDpi{get;private set;}
    public TrayPalette Palette{get;private set;}
    public TrayPopupMenu(){ShowImageMargin=false;ShowCheckMargin=false;AutoSize=false;}
    public void ShowAt(Point anchor){requestedAnchor=anchor;try{Show(anchor);}finally{requestedAnchor=null;}}
    protected override void OnOpening(CancelEventArgs e){
        base.OnOpening(e);if(e.Cancel)return;
        Point anchor=requestedAnchor??Cursor.Position;PrepareAt(anchor);
    }
    void PrepareAt(Point anchor){
        // A hidden popup can retain its original window DPI until it becomes visible.
        // Query the target monitor in our PMv2 process, including the very first open.
        int dpi=0;
        try{uint x,y;if(Native.GetDpiForMonitor(Native.MonitorFromPoint(new Native.Point{X=anchor.X,Y=anchor.Y},2),0,out x,out y)==0)dpi=(int)x;}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
        if(dpi==0){try{dpi=(int)Native.GetDpiForWindow(Handle);}catch(EntryPointNotFoundException){}}
        ApplyAppearance(TrayPalette.Read(),dpi,Screen.FromPoint(anchor).WorkingArea);
    }
    public void RefreshAppearance(){if(Visible){Point anchor=Location;PrepareAt(anchor);Location=TrayGeometry.Fit(anchor,Size,workingArea).Location;Invalidate();}}
    protected override void WndProc(ref Message m){
        base.WndProc(ref m);
        if(Visible&&!IsDisposed&&(m.Msg==0x1A||m.Msg==0x31A||m.Msg==0x7E))BeginInvoke((Action)RefreshAppearance);
    }
    public void ApplyAppearance(TrayPalette palette,int dpi,Rectangle work){
        if(palette==null)throw new ArgumentNullException("palette");
        MenuDpi=TrayGeometry.Dpi(dpi);Palette=palette;workingArea=work;
        Font source=SystemFonts.MenuFont;
        var next=new Font(source.FontFamily,source.SizeInPoints*MenuDpi/72f,source.Style,GraphicsUnit.Pixel);
        Font previous=ownedFont;ownedFont=next;
        SuspendLayout();
        try{
            Font=next;BackColor=palette.Background;ForeColor=palette.Text;
            Renderer=new TrayRenderer(palette,MenuDpi);
            int width=1,height=TrayGeometry.Pixels(4,MenuDpi),row=TrayGeometry.Pixels(24,MenuDpi),separator=TrayGeometry.Pixels(8,MenuDpi);
            foreach(ToolStripItem item in Items){if(!item.Available)continue;Size text=TextRenderer.MeasureText(item.Text??"",next,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);width=Math.Max(width,text.Width);row=Math.Max(row,text.Height+TrayGeometry.Pixels(6,MenuDpi));}
            width=Math.Min(Math.Max(1,work.Width),width+TrayGeometry.Pixels(49,MenuDpi));
            Padding=new Padding(1,TrayGeometry.Pixels(2,MenuDpi),1,TrayGeometry.Pixels(2,MenuDpi));
            foreach(ToolStripItem item in Items){item.AutoSize=false;item.Margin=Padding.Empty;item.Padding=Padding.Empty;item.Size=new Size(Math.Max(1,width-Padding.Horizontal),item is ToolStripSeparator?separator:row);if(item.Available)height+=item.Height;}
            MaximumSize=new Size(Math.Max(1,work.Width),Math.Max(1,work.Height));
            Size=new Size(width,Math.Min(height,Math.Max(1,work.Height)));
        }finally{ResumeLayout(true);if(previous!=null)previous.Dispose();}
    }
    protected override void OnOpened(EventArgs e){Location=TrayGeometry.Fit(Location,Size,workingArea).Location;base.OnOpened(e);}
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing&&ownedFont!=null){ownedFont.Dispose();ownedFont=null;}}
}
}
