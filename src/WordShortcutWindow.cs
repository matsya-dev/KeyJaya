// KeyJaya: User-requested Word shortcut inspection, with no desktop automation.
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace KeyJaya
{
    public sealed class WordShortcutWindow : ThemeWindow
    {
        readonly Font bodyFont = new Font("Segoe UI", 9f);
        readonly Label status = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(860, 0)
        };
        readonly ListView list = new ListView
        {
            Name = "WordShortcutFindings",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            ShowItemToolTips = true
        };
        readonly Button refresh = new ThemeButton
        {
            Name = "CheckWordShortcuts",
            Text = "Check again",
            AutoSize = true
        };
        readonly ShortcutSettings settings;
        public WordShortcutWindow(ShortcutSettings bindings)
        {
            settings = bindings;
            Text = "KeyJaya — Word shortcut conflicts";
            Font = bodyFont;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(920, 490);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(status, 0, 0);
            list.Columns.Add("Shortcut", 135);
            list.Columns.Add("Word assignment", 205);
            list.Columns.Add("Result", 335);
            list.Columns.Add("Word instance", 175);
            layout.Controls.Add(list, 0, 1);
            layout.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(860, 0), Text = "Overlaps are not always faults: KeyJaya uses these keys while enabled. Navigation changes may be intentional.\nTo review a Word assignment: File → Options → Customize Ribbon → Keyboard shortcuts: Customize.\nOnly the active window in each open Word instance is checked. No assignments, templates or documents are changed." }, 0, 2);
            var footer = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var close = new ThemeButton
            {
                Text = "Close",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            footer.Controls.AddRange(new Control[] { close, refresh });
            layout.Controls.Add(footer, 0, 3);
            Controls.Add(layout);
            CancelButton = close;
            close.Click += (s, e) => Close();
            refresh.Click += (s, e) => Check();
            Shown += (s, e) => Check();
            PrepareWindow(new Size(920, 490));
        }

        void Check()
        {
            if (!refresh.Enabled)
                return;
            refresh.Enabled = false;
            status.Text = "Reading Word's current shortcut assignments…";
            list.Items.Clear();
            ThreadPool.QueueUserWorkItem(unused =>
            {
                var report = WordShortcutInspector.Check(settings);
                try
                {
                    if (IsDisposed || !IsHandleCreated)
                        return;
                    BeginInvoke((Action)(() =>
                    {
                        if (IsDisposed)
                            return;
                        status.Text = report.Status;
                        foreach (var row in report.Findings)
                        {
                            var item = new ListViewItem(row.Shortcut)
                            {
                                ToolTipText = row.Expected == null ? row.Action : "Expected navigation: " + row.Expected
                            };
                            item.SubItems.Add(row.Command);
                            item.SubItems.Add(row.Finding);
                            item.SubItems.Add(row.Window);
                            list.Items.Add(item);
                        }

                        refresh.Enabled = true;
                    }));
                }
                catch (InvalidOperationException)
                {
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                bodyFont.Dispose();
        }
    }
}
