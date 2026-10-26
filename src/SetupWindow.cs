// KeyJaya: Setup preferences and progress UI for the installer worker.
// Installation work is reported to the UI; the progress window does not own the deployment transaction.
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace KeyJaya
{
    public static class SetupPreferences
    {
        public static string FilePath(string root)
        {
            return Path.Combine(root, "state", "setup-preferences.ini");
        }

        public static bool? ReadDesktop(string root)
        {
            string path = FilePath(root);
            if (!File.Exists(path))
                return null;
            bool? result = null;
            foreach (string line in File.ReadAllLines(path))
            {
                string[] pair = line.Split(new[] { '=' }, 2);
                if (pair.Length != 2 || pair[0].Trim() != "desktop_shortcut")
                    continue;
                bool value;
                if (!bool.TryParse(pair[1].Trim(), out value) || result.HasValue && result.Value != value)
                    throw new InvalidDataException("Invalid saved desktop shortcut preference.");
                result = value;
            }

            return result;
        }

        public static bool? ResolveDesktop(bool? saved, bool upgrade, bool ownedShortcut)
        {
            return saved ?? (upgrade ? (bool? )ownedShortcut : null);
        }

        public static void SaveDesktop(string root, bool enabled)
        {
            if (ReadDesktop(root) == enabled)
                return;
            SettingsFile.Update(FilePath(root), "desktop_shortcut=" + enabled.ToString().ToLowerInvariant());
        }
    }

    public sealed class SetupProgressWindow : Form
    {
        readonly Icon setupIcon;
        readonly Font bodyFont = new Font("Segoe UI", 9f), headingFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        readonly Label heading = new Label
        {
            Name = "Heading",
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        readonly Label status = new Label
        {
            Name = "Status",
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        readonly ProgressBar progress = new ProgressBar
        {
            Name = "Progress",
            Dock = DockStyle.Fill,
            Height = 22,
            Minimum = 0,
            Maximum = 100
        };
        readonly FlowLayoutPanel buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        readonly Button finish = new Button
        {
            Name = "Finish",
            Text = "Finish",
            AutoSize = true
        }, open = new Button
        {
            Name = "OpenCheatSheet",
            Text = "Open Cheat sheet",
            AutoSize = true
        };
        bool completed;
        Action openReference;
        public bool Succeeded { get; private set; }

        public SetupProgressWindow()
        {
            Text = "KeyJaya setup";
            try
            {
                setupIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                Icon = setupIcon;
            }
            catch
            {
            }

            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = bodyFont;
            ClientSize = new Size(480, 230);
            Padding = new Padding(20);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = ControlBox = false;
            ShowInTaskbar = true;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heading.Font = headingFont;
            heading.Text = "Installing KeyJaya";
            heading.Margin = new Padding(0, 0, 0, 12);
            status.Text = "Preparing installation…";
            status.Margin = new Padding(0, 0, 0, 12);
            progress.Margin = new Padding(0, 0, 0, 8);
            buttons.Margin = new Padding(0, 8, 0, 0);
            finish.Visible = open.Visible = false;
            finish.Click += (s, e) => Close();
            open.Click += (s, e) =>
            {
                open.Enabled = false;
                try
                {
                    if (openReference != null)
                        openReference();
                    Close();
                }
                catch (Exception ex)
                {
                    open.Enabled = true;
                    MessageBox.Show(this, "The cheat sheet could not open. You can also open it from the KeyJaya tray icon.\n\n" + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            buttons.Controls.Add(finish);
            buttons.Controls.Add(open);
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(status, 0, 1);
            layout.Controls.Add(progress, 0, 2);
            layout.Controls.Add(buttons, 0, 3);
            Controls.Add(layout);
            FormClosing += (s, e) =>
            {
                if (!completed && e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };
        }

        public void Report(string text, int percent)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Report(text, percent)));
                return;
            }

            status.Text = text;
            progress.Value = Math.Max(progress.Value, Math.Max(0, Math.Min(100, percent)));
        }

        public void Complete(Action showReference)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Complete(showReference)));
                return;
            }

            completed = Succeeded = true;
            openReference = showReference;
            heading.Text = "KeyJaya is installed";
            status.Text = "Open the cheat sheet for shortcuts and examples.\nYou can also find it in the KeyJaya tray menu.";
            progress.Value = 100;
            finish.Visible = open.Visible = true;
            ControlBox = true;
            AcceptButton = open;
            CancelButton = finish;
            open.Focus();
        }

        public void Fail(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => Fail(message)));
                return;
            }

            completed = true;
            MessageBox.Show(this, message, "KeyJaya setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                bodyFont.Dispose();
                headingFont.Dispose();
                if (setupIcon != null)
                    setupIcon.Dispose();
            }
        }
    }
}
