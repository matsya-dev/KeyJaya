// KeyJaya: Shortcut customization UI with explicit save/cancel behavior.
// The resident applies registration changes transactionally; the window only captures the requested bindings.
using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace KeyJaya
{
    public sealed class ShortcutWindow : ThemeWindow
    {
        readonly Font windowFont = new Font("Segoe UI", 9);
        public ShortcutWindow(ShortcutSettings current, Action<IDictionary<string, string>> save, Action<ShortcutSettings> checkWord = null)
        {
            Text = "KeyJaya shortcuts";
            Font = windowFont;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(480, 400);
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 10
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            var boxes = new Dictionary<string, TextBox>();
            for (int n = 0; n < ShortcutSettings.Roles.Length; n++)
            {
                string role = ShortcutSettings.Roles[n];
                layout.Controls.Add(new Label { Text = ShortcutSettings.Labels[n], AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, 0, n);
                var box = new TextBox
                {
                    Text = current[role].Text,
                    Dock = DockStyle.Fill,
                    AccessibleName = ShortcutSettings.Labels[n] + " shortcut"
                };
                boxes.Add(role, box);
                layout.Controls.Add(box, 1, n);
            }

            var hint = new Label
            {
                Text = "Examples: Ctrl+Alt+K, F8, `, Left Ctrl, Right Ctrl.\nCtrl+C/V/X/A/Z stay available for normal editing.",
                AutoSize = true
            };
            layout.Controls.Add(hint, 0, 8);
            layout.SetColumnSpan(hint, 2);
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var cancel = new ThemeButton
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true
            };
            var apply = new ThemeButton
            {
                Text = "Save",
                AutoSize = true
            };
            var defaults = new ThemeButton
            {
                Text = "Restore defaults",
                AutoSize = true
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(apply);
            buttons.Controls.Add(defaults);
            if (checkWord != null)
            {
                var check = new ThemeButton
                {
                    Text = "Check Word conflicts…",
                    AutoSize = true
                };
                buttons.Controls.Add(check);
                check.Click += (sender, args) =>
                {
                    try
                    {
                        checkWord(new ShortcutSettings(null, boxes.ToDictionary(row => row.Key, row => row.Value.Text)));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Shortcuts could not be checked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
            }

            layout.Controls.Add(buttons, 0, 9);
            layout.SetColumnSpan(buttons, 2);
            Controls.Add(layout);
            AcceptButton = apply;
            CancelButton = cancel;
            PrepareWindow(new Size(480, 400));
            defaults.Click += (sender, args) =>
            {
                for (int n = 0; n < ShortcutSettings.Roles.Length; n++)
                    boxes[ShortcutSettings.Roles[n]].Text = ShortcutSettings.Defaults[n];
            };
            apply.Click += (sender, args) =>
            {
                try
                {
                    save(boxes.ToDictionary(row => row.Key, row => row.Value.Text));
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Shortcut could not be saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                windowFont.Dispose();
        }
    }
}
