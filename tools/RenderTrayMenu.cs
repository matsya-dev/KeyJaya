// Developer renderer: draw the actual tray controls into an image without capturing the desktop.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using KeyJaya;

// Render real app controls with factory settings. No resident, screen capture,
// keyboard hooks, clipboard access, or installed configuration is involved.
public static class RenderTrayMenu
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length != 1)
            throw new ArgumentException("Provide the output PNG path.");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (var menu = new TrayPopupMenu())
        {
            foreach (var row in TrayMenu.Items(false, true, true))
            {
                if ((row.Flags & 0x800) != 0)
                    menu.Items.Add(new ToolStripSeparator());
                else
                    menu.Items.Add(new ToolStripMenuItem(row.Label) { Checked = (row.Flags & 8) != 0, Enabled = (row.Flags & 2) == 0 });
            }

            menu.ApplyAppearance(new TrayPalette(false, false), 96, new Rectangle(0, 0, 1200, 1600));
            IntPtr handle = menu.Handle;
            using (var bitmap = new Bitmap(menu.Width, menu.Height))
            {
                menu.DrawToBitmap(bitmap, new Rectangle(Point.Empty, menu.Size));
                string path = Path.GetFullPath(args[0]);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                bitmap.Save(path, ImageFormat.Png);
                Console.WriteLine("Rendered default tray menu: " + bitmap.Width + "x" + bitmap.Height);
            }
        }

        return 0;
    }
}
