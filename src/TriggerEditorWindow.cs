// KeyJaya: Search/add/edit/delete trigger UI over the document model.
// The store owns persistence; the resident owns reloading the active conversion engine.
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace KeyJaya
{
    public sealed class TriggerEditorWindow : ThemeWindow
    {
        readonly Font bodyFont = new Font("Segoe UI", 9f);
        readonly TriggerEditorStore store;
        readonly Action apply;
        readonly TextBox search = new TextBox
        {
            Name = "SearchTriggers",
            Dock = DockStyle.Fill
        };
        readonly ListView list = new ListView
        {
            Name = "Triggers",
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            CheckBoxes = true,
            HideSelection = false
        };
        readonly Button edit = new ThemeButton
        {
            Text = "Edit…",
            AutoSize = true
        }, delete = new ThemeButton
        {
            Text = "Delete",
            AutoSize = true
        }, restore = new ThemeButton
        {
            Text = "Restore built-in",
            AutoSize = true
        };
        bool loading;
        TriggerEntry Selected
        {
            get
            {
                return list.SelectedItems.Count == 1 ? list.SelectedItems[0].Tag as TriggerEntry : null;
            }
        }

        public TriggerEditorWindow(TriggerEditorStore document, Action saveAndApply)
        {
            store = document;
            apply = saveAndApply;
            Text = "KeyJaya triggers";
            Font = bodyFont;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(740, 440);
            MinimumSize = new Size(620, 360);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = "Find a trigger. Check or uncheck an entry to enable or disable it. Matching is case-sensitive.", AutoSize = true }, 0, 0);
            layout.Controls.Add(search, 0, 1);
            list.Columns.Add("Trigger", 110);
            list.Columns.Add("Replacement (Unicode)", 440);
            list.Columns.Add("Type", 100);
            layout.Controls.Add(list, 0, 2);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            var add = new ThemeButton
            {
                Name = "AddTrigger",
                Text = "Add…",
                AutoSize = true
            };
            actions.Controls.AddRange(new Control[] { add, edit, delete, restore });
            layout.Controls.Add(actions, 0, 3);
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };
            var cancel = new ThemeButton
            {
                Name = "CancelTriggers",
                Text = "Cancel",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            var save = new ThemeButton
            {
                Name = "SaveTriggers",
                Text = "Save and apply",
                AutoSize = true
            };
            footer.Controls.Add(cancel);
            footer.Controls.Add(save);
            layout.Controls.Add(footer, 0, 4);
            Controls.Add(layout);
            CancelButton = cancel;
            AcceptButton = save;
            search.TextChanged += (s, e) => Fill();
            list.SelectedIndexChanged += (s, e) => Selection();
            list.ItemChecked += (s, e) =>
            {
                if (!loading)
                    store.SetEnabled((TriggerEntry)e.Item.Tag, e.Item.Checked);
            };
            list.DoubleClick += (s, e) =>
            {
                if (Selected != null)
                    EditEntry(Selected);
            };
            add.Click += (s, e) => EditEntry(null);
            edit.Click += (s, e) => EditEntry(Selected);
            delete.Click += (s, e) =>
            {
                var entry = Selected;
                if (entry != null && MessageBox.Show(this, "Delete " + entry.Name + " and all its encoding forms?\n\nThe deletion takes effect when you save.", "Delete trigger", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    store.Delete(entry);
                    Fill();
                }
            };
            restore.Click += (s, e) =>
            {
                var entry = Selected;
                if (entry != null)
                {
                    store.Restore(entry);
                    Fill(entry.Name);
                }
            };
            cancel.Click += (s, e) => Close();
            save.Click += (s, e) =>
            {
                try
                {
                    store.Save(apply);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Triggers could not be saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            Fill();
            PrepareWindow(new Size(740, 440));
        }

        void Fill(string selected = null)
        {
            loading = true;
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                foreach (var entry in store.Entries.Where(x => !x.Deleted && (search.Text.Length == 0 || x.Name.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0 || x.Replacement.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    var item = new ListViewItem(entry.Name)
                    {
                        Tag = entry,
                        Checked = entry.Enabled
                    };
                    item.SubItems.Add(entry.Replacement.Replace("\r", "").Replace("\n", " ↵ "));
                    item.SubItems.Add(store.CanRestore(entry.Name) ? "Built-in" : "Custom");
                    list.Items.Add(item);
                    if (entry.Name == selected)
                        item.Selected = true;
                }
            }
            finally
            {
                list.EndUpdate();
                loading = false;
            }

            Selection();
        }

        void Selection()
        {
            edit.Enabled = delete.Enabled = Selected != null;
            restore.Enabled = Selected != null && store.CanRestore(Selected.Name);
        }

        void EditEntry(TriggerEntry entry)
        {
            using (var window = new TriggerEntryWindow(entry == null ? store.NewEntry() : entry.Copy(), value => store.Upsert(entry, value)))
            {
                if (window.ShowDialog(this) == DialogResult.OK)
                    Fill(window.SavedName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                bodyFont.Dispose();
        }
    }

    public sealed class TriggerEntryWindow : ThemeWindow
    {
        readonly Font bodyFont = new Font("Segoe UI", 9f);
        readonly TriggerEntry entry;
        readonly Action<TriggerEntry> save;
        readonly TextBox name = new TextBox
        {
            Name = "TriggerName",
            Dock = DockStyle.Fill,
            MaxLength = 159
        }, replacement = new TextBox
        {
            Name = "TriggerReplacement",
            Multiline = true,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            MaxLength = 200000
        }, legacy = new TextBox
        {
            Name = "LegacyReplacement",
            Multiline = true,
            Dock = DockStyle.Fill,
            MaxLength = 200000
        }, plain = new TextBox
        {
            Name = "PlainReplacement",
            Multiline = true,
            Dock = DockStyle.Fill,
            MaxLength = 200000
        };
        readonly ComboBox source = new ComboBox
        {
            Name = "ReplacementEncoding",
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        readonly CheckBox exact = new ThemeCheckBox
        {
            Name = "ExactReplacement",
            Text = "Keep replacement exactly as entered",
            AutoSize = true
        }, legacyOverride = new ThemeCheckBox
        {
            Name = "OverrideLegacy",
            Text = "Override Balarama",
            AutoSize = true
        }, plainOverride = new ThemeCheckBox
        {
            Name = "OverridePlain",
            Text = "Override plain",
            AutoSize = true
        };
        readonly Label preview = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        bool loading;
        public string SavedName { get; private set; }

        public TriggerEntryWindow(TriggerEntry value, Action<TriggerEntry> apply)
        {
            entry = value;
            save = apply;
            Text = "KeyJaya trigger";
            Font = bodyFont;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(580, 505);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 2,
                RowCount = 10
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Add(layout, "Trigger (case-sensitive)", name, 0);
            Add(layout, "Replacement", replacement, 1);
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            source.Items.AddRange(new object[] { "Auto detect pasted text", "Unicode / IAST", "Balarama / Legacy" });
            Add(layout, "Input encoding", source, 2);
            layout.Controls.Add(exact, 0, 3);
            layout.SetColumnSpan(exact, 2);
            layout.Controls.Add(preview, 0, 4);
            layout.SetColumnSpan(preview, 2);
            layout.Controls.Add(legacyOverride, 0, 5);
            layout.Controls.Add(legacy, 1, 5);
            layout.Controls.Add(plainOverride, 0, 6);
            layout.Controls.Add(plain, 1, 6);
            for (int row = 2; row < 7; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 4 ? 52 : 38));
            var hint = new Label
            {
                Text = "New abbreviations work with Left Ctrl and > / < / ?.\nReplacement text can contain line breaks. Save and apply in the main window to activate edits.",
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            layout.Controls.Add(hint, 0, 7);
            layout.SetColumnSpan(hint, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            var cancel = new ThemeButton
            {
                Text = "Cancel",
                AutoSize = true,
                DialogResult = DialogResult.Cancel
            };
            var ok = new ThemeButton
            {
                Name = "AcceptTrigger",
                Text = "OK",
                AutoSize = true
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons, 0, 8);
            layout.SetColumnSpan(buttons, 2);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);
            CancelButton = cancel;
            AcceptButton = ok;
            loading = true;
            name.Text = entry.Name;
            replacement.Text = entry.Replacement;
            source.SelectedIndex = entry.New ? 0 : 1;
            exact.Checked = entry.Exact;
            legacyOverride.Checked = entry.LegacyOverride;
            plainOverride.Checked = entry.PlainOverride;
            legacy.Text = entry.Legacy;
            plain.Text = entry.Plain;
            loading = false;
            PrepareWindow(new Size(580, 505));
            replacement.TextChanged += (s, e) => Preview();
            source.SelectedIndexChanged += (s, e) => Preview();
            exact.CheckedChanged += (s, e) => Preview();
            legacyOverride.CheckedChanged += (s, e) => Preview();
            plainOverride.CheckedChanged += (s, e) => Preview();
            name.TextChanged += (s, e) => Preview();
            Preview();
            ok.Click += (s, e) =>
            {
                try
                {
                    var updated = entry.Copy();
                    updated.Name = name.Text.Trim();
                    updated.Exact = exact.Checked;
                    updated.Replacement = exact.Checked ? replacement.Text : TriggerEditorStore.Normalize(replacement.Text, Source());
                    updated.LegacyOverride = legacyOverride.Checked;
                    updated.PlainOverride = plainOverride.Checked;
                    updated.Legacy = legacy.Text;
                    updated.Plain = plain.Text;
                    save(updated);
                    SavedName = updated.Name;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Trigger could not be added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        string Source()
        {
            return source.SelectedIndex == 1 ? "unicode" : source.SelectedIndex == 2 ? "balaram" : "auto";
        }

        void Preview()
        {
            if (loading)
                return;
            string text = exact.Checked ? replacement.Text : TriggerEditorStore.Normalize(replacement.Text, Source());
            string display = text.Replace("\r", "").Replace("\n", " ↵ ");
            preview.Text = "Unicode preview: " + (display.Length > 150 ? display.Substring(0, 150) + "…" : display);
            source.Enabled = !exact.Checked;
            bool forms = Core.CtrlOnlyShort(name.Text.Trim()) && !exact.Checked;
            legacyOverride.Enabled = plainOverride.Enabled = forms;
            legacy.Enabled = forms && legacyOverride.Checked;
            plain.Enabled = forms && plainOverride.Checked;
            if (!legacyOverride.Checked)
                legacy.Text = exact.Checked ? text : EncodingMap.Balaram(text);
            if (!plainOverride.Checked)
                plain.Text = exact.Checked ? text : EncodingMap.Ascii(text);
        }

        static void Add(TableLayoutPanel layout, string label, Control control, int row)
        {
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                bodyFont.Dispose();
        }
    }
}
