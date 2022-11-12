using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Text.Json;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;

// TODO Make iterators for lvFiles.Columns/Items etc.

namespace Ephemera.FileFam
{
    public partial class FileFam : Form
    {
        #region Fields
        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>The ff data list.</summary>
        readonly List<TrackedFile> _trackedFiles = new();

        /// <summary>The ff filename.</summary>
        string _fffn = "";

        /// <summary>Prevent resize recursion.</summary>
        bool _resizing = false;

        /// <summary>Current file is dirty.</summary>
        bool _dirty = false;

        /// <summary>Current column selected for editing or sorting.</summary>
        int _selColumn = -1;

        /// <summary>Current sort spec.</summary>
        SortOrder _sortOrder = SortOrder.None;

        /// <summary>Visuals.</summary>
        const char ASC = '▲';

        /// <summary>Visuals.</summary>
        const char DESC = '▼';
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public FileFam()
        {
            // Improve performance and eliminate flicker.
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            
            // Sortable datetime format.
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentCulture = culture;

            // Must do this first before initializing further.
            string appDir = MiscUtils.GetAppDataDir("FileFam", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();

            // Log display.
            tvLog.MatchColors.Add("ERR", Color.LightPink);
            tvLog.MatchColors.Add("WRN", Color.Plum);

            // Init main form from settings.
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Location = new(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            // Listview.
            lvFiles.FullRowSelect = true;
            lvFiles.GridLines = true;
            lvFiles.MultiSelect = false;
            lvFiles.View = View.Details;
            while (_settings.ColumnWidths.Count < TrackedFile.ColumnNames.Length)
            {
                _settings.ColumnWidths.Add(20);
            }
            for (int i = 0; i < TrackedFile.ColumnNames.Length; i++)
            {
                string name = TrackedFile.ColumnNames[i];
                lvFiles.Columns.Add(name, name, _settings.ColumnWidths[i]);
            }
            lvFiles.ContextMenuStrip = new();
            lvFiles.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Add", null, LvFiles_Add));
            lvFiles.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Delete", null, LvFiles_Delete));
            lvFiles.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Pick File", null, LvFiles_PickFile));
            lvFiles.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Edit Tags", null, LvFiles_EditTags));
            // All interesting events.
            lvFiles.Click += LvFiles_Click;
            lvFiles.DoubleClick += LvFiles_DoubleClick;
            lvFiles.ColumnClick += LvFiles_ColumnClick;
            lvFiles.Resize += (_, __) => ResizeLv();
            lvFiles.ColumnWidthChanged += (_, __) => ResizeLv();

            // Misc UI.
            txtEdit.Visible = false;
            txtEdit.Leave += TxtEdit_Leave;
            txtEdit.KeyDown += TxtEdit_KeyDown;

            opedFilterTags.OptionsChanged += OpedFilterTags_OptionsChanged;

            // File menu.
            NewMenu.Click += NewMenu_Click;
            OpenMenu.Click += OpenMenu_Click;
            SaveMenu.Click += SaveMenu_Click;
            SaveAsMenu.Click += SaveAsMenu_Click;
            FileMenu.DropDownOpening += FileMenu_DropDownOpening;

            // Tools.
            SettingsMenu.Click += (_, __) => EditSettings();
            AboutMenu.Click += (_, __) => MiscUtils.ShowReadme("FileFam");
            DebugMenu.Click += (_, __) => MakeFake();
            DebugMenu.Visible = false;

            lblInfo.Text = "";
        }

        /// <summary>
        /// Form is legal now. Init controls.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // The ff file.
            if (_settings.RecentFiles.Count > 0)
            {
                OpenFile(_settings.RecentFiles[0]);
            }

            ResizeLv();
            UpdateUi();

            base.OnLoad(e);
        }

        /// <summary>
        /// Bye-bye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveFile("");
            SaveSettings();
            base.OnFormClosing(e);
        }
        #endregion

        #region File menu handlers
        /// <summary>
        /// Show the recent ff config files in the menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FileMenu_DropDownOpening(object? sender, EventArgs e)
        {
            RecentMenu.DropDownItems.Clear();

            _settings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f);
                menuItem.Click += (object? sender, EventArgs e) =>
                {
                    string fn = sender!.ToString()!;
                    OpenFile(fn);
                };

                RecentMenu.DropDownItems.Add(menuItem);
            });
        }

        /// <summary>
        /// Allows the user to select ff config file from system.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        void OpenMenu_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openDlg = new()
            {
                Filter = $"FileFam Files|*.ff",
                Title = "Open a FileFam file"
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                var fn = openDlg.FileName;
                OpenFile(fn);
            }
            UpdateUi();
        }

        /// <summary>
        /// Save current ff config as a new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SaveAsMenu_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog saveDlg = new()
            {
                Filter = $"FileFam Files|*.ff",
                Title = "Save a FileFam file",
            };

            if (saveDlg.ShowDialog() == DialogResult.OK)
            {
                _fffn = saveDlg.FileName;
                SaveFile(saveDlg.FileName);
            }
            UpdateUi();
        }

        /// <summary>
        /// Save current ff config file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SaveMenu_Click(object? sender, EventArgs e)
        {
            SaveFile("");
            UpdateUi();
        }

        /// <summary>
        /// Create a new ff config.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void NewMenu_Click(object? sender, EventArgs e)
        {
            // Check for current file dirty.
            CheckCurrentFile();

            _trackedFiles.Clear();
            _fffn = "";
            _dirty = true;

            ShowMessage($"Created new ff for you");
            UpdateUi();
        }
        #endregion

        #region ff config file I/O
        /// <summary>
        /// Common file opener.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        /// <returns>Success.</returns>
        bool OpenFile(string fn)
        {
            bool ok = true;

            try
            {
                // Check for current file dirty.
                CheckCurrentFile();

                // Do validity checks.
                var ext = Path.GetExtension(fn).ToLower();
                var baseFn = Path.GetFileName(fn);

                // Valid file name.
                ShowMessage($"Opening file: {fn}");

                JsonSerializerOptions opts = new() { AllowTrailingCommas = true };
                string json = File.ReadAllText(fn);
                var tfs = (List<TrackedFile>)JsonSerializer.Deserialize(json, typeof(List<TrackedFile>), opts)!;
                _trackedFiles.AddRange(tfs);

                _fffn = fn;
                _dirty = false;
                _settings.UpdateMru(fn);

                // Get all tags in ff ordered by frequency.
                Dictionary<string, int> ffTags = new();
                _trackedFiles
                    .ForEach(f => f.TagsList
                    .ForEach(t => ffTags[t] = ffTags.ContainsKey(t) ? ffTags[t] + 1 : 1));

                Dictionary<string, bool> filters = new();
                ffTags
                    .OrderByDescending(t => t.Value)
                    .ForEach(t => filters.Add(t.Key, _settings.CurrentTags.Contains(t.Key)));
                opedFilterTags.Options = filters;

                ShowRecords(0);// first
            }
            catch (Exception ex)
            {
                ShowMessage($"ERR Couldn't open the file: {fn} because: {ex.Message}");
                ok = false;
            }

            UpdateUi();

            return ok;
        }

        /// <summary>
        /// Save the file.
        /// </summary>
        /// <param name="fn">The file to save to. If empty assume the current file.</param>
        /// <returns>Status.</returns>
        bool SaveFile(string fn)
        {
            bool ok = false;
            var newfn = fn == "" ? _fffn : fn;

            try
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(_trackedFiles, typeof(List<TrackedFile>), opts);
                File.WriteAllText(newfn, json);
                // Ok, update status.
                _fffn = newfn;
                _dirty = false;

            }
            catch (Exception ex)
            {
                ShowMessage($"ERR Couldn't save the file: {newfn} because: {ex.Message}");
                ok = false;
            }

            UpdateUi();

            return ok;
        }

        /// <summary>
        /// Set UI according to system states.
        /// </summary>
        void UpdateUi()
        {
            OpenMenu.Enabled = true;
            SaveMenu.Enabled = _dirty;
            SaveAsMenu.Enabled = true;

            AboutMenu.Enabled = true;
            SettingsMenu.Enabled = true;

            Text = $"FileFam {MiscUtils.GetVersionString()} {_fffn}";
        }

        /// <summary>
        /// If current ff is dirty ask the user what to do.
        /// </summary>
        /// <returns>True means handled.</returns>
        bool CheckCurrentFile()
        {
            bool ok = true;

            if (_dirty)
            {
                var newfn = _fffn == "" ? "New file" : _fffn;

                var res = MessageBox.Show($"{newfn} has unsaved changes. Do you want to save it?", "Close File", MessageBoxButtons.YesNoCancel);

                switch (res)
                {
                    case DialogResult.Yes:
                        SaveAsMenu_Click(null, EventArgs.Empty);
                        break;

                    case DialogResult.No:
                        // Don't save file.
                        break;

                    case DialogResult.Cancel:
                        ok = false;
                        break;
                }
            }

            return ok;
        }
        #endregion

        #region UI controls handlers
        /// <summary>
        /// Filters changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OpedFilterTags_OptionsChanged(object? sender, EventArgs e)
        {
            ShowRecords(0); // first
        }

        /// <summary>
        /// Add a new entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_Add(object? sender, EventArgs e)
        {
            var tf = new TrackedFile();
            _trackedFiles.Add(tf);
            _dirty = true;

            ShowRecords(tf.UID);// current
        }

        /// <summary>
        /// Remove current entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_Delete(object? sender, EventArgs e)
        {
            if (lvFiles.SelectedIndices.Count > 0)
            {
                // Get an adjacent item first.
                int adjuid = 0; // default
                int row = lvFiles.SelectedIndices[0];

                if (row > 0)
                {
                    var nexttf = lvFiles.Items[row - 1].Tag as TrackedFile;
                    adjuid = nexttf!.UID;
                }

                // Process the selection.
                var tf = Selected();
                _trackedFiles.Remove(tf);
                _dirty = true;

                ShowRecords(adjuid);// adjacent
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_PickFile(object? sender, EventArgs e)
        {
            // Collect filters. Like "Midi Files|*.sty;*.pcs;*.sst;*.prs|Style Files|*.sty;*.prs"
            List<string> filters = new();

            try
            {
                _settings.TrackedFileTypes.ForEach(t =>
                {
                    filters.Add($"{t.CategoryName} Files");
                    string exts = string.Join(";*.", t.Extensions).Remove(0, 1);
                    filters.Add(exts);
                });

                filters.Add("All Files");
                filters.Add("*.*");
                var fs = string.Join("|", filters);

                using OpenFileDialog openDlg = new()
                {
                    Filter = fs,
                    Title = "Pick File"
                };

                if (openDlg.ShowDialog() == DialogResult.OK)
                {
                    // Save selection.
                    var tf = Selected();
                    tf.FullName = openDlg.FileName;

                    ShowRecords(tf.UID);// current
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"ERR Bad filter: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_EditTags(object? sender, EventArgs e)
        {
            var tf = Selected();
            var fileTags = tf.TagsList;
            Dictionary<string, bool> filters = new();

            opedFilterTags.Options.ForEach(o => filters.Add(o.Key, fileTags.Contains(o.Key)));

            OptionsEditor opedTags = new()
            {
                AllowEdit = true,
                Dock = DockStyle.Fill,
                Options = filters
            };
            opedTags.OptionsChanged += (_, __) => ShowMessage($"Options changed:{opedTags.Options.Where(o => o.Value == true).Count()}");

            using Form f = new()
            {
                Text = "Edit Tags",
                Location = Cursor.Position,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            f.ClientSize = new(150, 300); // do this after constructor.
            f.Controls.Add(opedTags);

            var dr = f.ShowDialog();

            // Update results.
            List<string> ls = new();
            opedTags.Options
                .Where(o => o.Value)
                .ForEach(o => ls.Add(o.Key));

            tf.TagsList = ls;
            _dirty = true;

            ShowRecords(tf.UID);// current
        }

        /// <summary>
        /// Does sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            var tf = Selected();
            _selColumn = e.Column;

            var colsel = lvFiles.Columns[_selColumn];
            var name = colsel.Name;

            // Current sort.
            char cop = colsel.Text.Last();

            // New sort.
            _sortOrder = cop == ASC ? SortOrder.Descending : SortOrder.Ascending;

            // Reset all column headers.
            for (int i = 0; i < lvFiles.Columns.Count; i++)
            {
                var colhdr = lvFiles.Columns[i];
                colhdr.Text = colhdr.Name;
            }

            ShowRecords(tf.UID);// current

            // Update selected col text.
            colsel.Text = $"{name} {(_sortOrder == SortOrder.Ascending ? ASC : DESC)}";
        }

        /// <summary>
        /// Opens the target file clicked by the user. Update the timestamp too.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_DoubleClick(object? sender, EventArgs e)
        {
            var tf = Selected();
            var fn = tf.FullName;

            try
            {
                if (!File.Exists(fn))
                {
                    throw new Exception($"Invalid file {fn}");
                }

                // Dig out the file type particulars.
                var ext = Path.GetExtension(tf.FullName).Replace(".", "");
                FileCategory? fc = _settings.TrackedFileTypes.FirstOrDefault(fc => fc.Extensions.Contains(ext));

                if (fc is null)
                {
                    throw new Exception($"Extension {ext} not found");
                }

                var cmd = fc.TargetCommand.Replace("%F", fn);
                ShowMessage($"Executing target command: {cmd}");
                var info = new ProcessStartInfo(cmd) { UseShellExecute = true };
                var proc = new Process() { StartInfo = info };
                proc.Start();

                // Update record.
                tf.LastAccess = DateTime.Now;
                _dirty = true;

                ShowRecords(tf.UID);// current
            }
            catch (Exception ex)
            {
                ShowMessage($"ERR Something went wrong with your Tracked File Types: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts cell editing, maybe.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LvFiles_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var pt = PointToClient(MousePosition);
                var hitTestInfo = lvFiles.HitTest(pt);
                var tf = hitTestInfo.Item.Tag as TrackedFile;
                _selColumn = hitTestInfo.Item.SubItems.IndexOf(hitTestInfo.SubItem);

                // Show the text to edit.
                string lastContent = tf!.StringAt(_selColumn);

                if (lastContent.Length > 35)
                {
                    txtEdit.Multiline = true;
                    txtEdit.Size = new(200, 100);
                    txtEdit.ScrollBars = ScrollBars.None;
                }
                else
                {
                    txtEdit.Multiline = false;
                    txtEdit.Size = new(200, 27);
                    txtEdit.ScrollBars = ScrollBars.Both;
                }

                txtEdit.Text = lastContent;
                txtEdit.Location = pt;
                txtEdit.Visible = true;
                txtEdit.Focus();
                txtEdit.Select(0, 0);
            }
        }

        /// <summary>
        /// Process return and escape keys.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TxtEdit_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Return:
                    // Indicates validate and save.
                    lvFiles.Focus(); // force Leave
                    break;

                case Keys.Escape:
                    // Indicates cancel.
                    txtEdit.Text = "";
                    lvFiles.Focus(); // force Leave
                    break;

                default:
                    // Normal op.
                    break;
            }
        }

        /// <summary>
        /// Validate and save edit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TxtEdit_Leave(object? sender, EventArgs e)
        {
            if (txtEdit.Text != "")
            {
                // Save the contents.
                var tf = Selected();
                bool ok = tf.Parse(txtEdit.Text, _selColumn);

                if (ok)
                {
                    _dirty = true;
                    ShowRecords(tf.UID);// current
                }
            }
            // else dump

            txtEdit.Visible = false;
        }
        #endregion

        #region Internal functions
        /// <summary>
        /// Sort and filter then update the listview.
        /// </summary>
        /// <param name="seluid">Put the focus on this entry after showing. -1 sort of means ignore.</param>
        void ShowRecords(int seluid)
        {
            // Filter by tags. Get current filter selections.
            HashSet<string> filterTags = new();
            opedFilterTags.Options
                .Where(o => o.Value == true)
                .ForEach(o => filterTags.Add(o.Key));

            // Get file tags.
            var qry = filterTags.Count > 0 ?
                _trackedFiles
                .Where(tf => tf.TagsList
                .Intersect(filterTags)
                .Any())
                :
                _trackedFiles;

            // TODO also filter by other columns?

            // Sort by column and direction.
            var sorted = _selColumn switch
            {
                TrackedFile.FullNameOrdinal => qry.OrderBy(r => r.FullName),
                TrackedFile.IdOrdinal => qry.OrderBy(r => r.Id),
                TrackedFile.LastAccessOrdinal => qry.OrderBy(r => r.LastAccess),
                TrackedFile.TagsOrdinal => qry.OrderBy(r => r.Tags),
                TrackedFile.InfoOrdinal => qry.OrderBy(r => r.Info),
                _ => qry // no sort - use raw
            };
            var sortedOrdered = _sortOrder == SortOrder.Descending ? sorted.Reverse() : sorted;

            // Show the data.
            lvFiles.SuspendLayout();
            lvFiles.Items.Clear();
            sortedOrdered.ForEach(f => { lvFiles.Items.Add(new ListViewItem(f.ValueStrings) { Tag = f }); });
            lvFiles.ResumeLayout();

            // Select the requested entry. TODO optimize?
            for (int i = 0; i < lvFiles.Items.Count; i++)
            {
                var tf = lvFiles.Items[i].Tag as TrackedFile;
                if (tf!.UID == seluid)
                {
                    lvFiles.Items[i].Selected = true;
                    break;
                }
            }

            lblInfo.Text = $"{lvFiles.Items.Count} entries";
        }

        /// <summary>
        /// Does last column fill.
        /// </summary>
        void ResizeLv()
        {
            if (!_resizing) // TODO optimize?
            {
                _resizing = true;

                int tw = 0;
                for (int i = 0; i < lvFiles.Columns.Count - 1; i++)
                {
                    tw += lvFiles.Columns[i].Width;
                }
                lvFiles.Columns[^1].Width = lvFiles.ClientRectangle.Width - tw;
                _resizing = false;
            }
        }

        /// <summary>
        /// Tell the user something.
        /// </summary>
        /// <param name="msg"></param>
        void ShowMessage(string msg)
        {
            tvLog.AppendLine($"{msg}");
        }

        /// <summary>
        /// Get the current selectd record.
        /// </summary>
        /// <returns></returns>
        TrackedFile Selected()
        {
            return (lvFiles.SelectedItems[0].Tag as TrackedFile)!;
        }
        #endregion

        #region Settings
        /// <summary>
        /// Edit user settings.
        /// </summary>
        void EditSettings()
        {
            SettingsEditor.Edit(_settings, "User Settings", 400);
            // Detect changes of interest. Lists are ref bound so already updated.
            SaveSettings();
        }

        /// <summary>
        /// Collect and save user settings.
        /// </summary>
        void SaveSettings()
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Width, Height);

            // Column widths.
            _settings.ColumnWidths.Clear();
            for (int i = 0; i < lvFiles.Columns.Count; i++)
            {
                var col = lvFiles.Columns[i];
                _settings.ColumnWidths.Add(col.Width);
            }

            // Selected tags.
            _settings.CurrentTags.Clear();
            opedFilterTags.Options
                .Where(kv => kv.Value)
                .ForEach(o => _settings.CurrentTags.Add(o.Key));

            _settings.Save();
        }
        #endregion

        #region Debug stuff
        /// <summary>
        /// 
        /// </summary>
        void MakeFake()
        {
            List<string> strings = new()
            {
                "Lorem ipsum dolor sit amet", "consectetur adipiscing elit", "sed do eiusmod tempor incididunt",
                "ut labore et dolore magna aliqua", "Ut enim ad minim veniam", "quis nostrud exercitation",
                "ullamco laboris nisi ut aliquip ex ea commodo consequat", "Duis aute irure dolor in reprehenderit",
                "in voluptate velit esse cillum dolore", "eu fugiat nulla pariatur", "mollit anim id est laborum",
                "Excepteur sint occaecat cupidatat non proident", "sunt in culpa qui officia deserunt"
            };

            // Data.
            var trackedFiles = new List<TrackedFile>();
            Random rnd = new();
            for (int i = 0; i < 50; i++)
            {
                trackedFiles.Add(new()
                {
                    FullName = $"FullName_{rnd.Next(1, 50)}.txt",
                    Id = $"Id_{rnd.Next(1, 50)}",
                    LastAccess = DateTime.Now + new TimeSpan(rnd.Next(1, 1300), rnd.Next(1, 20), rnd.Next(1, 50), rnd.Next(1, 50)),
                    Tags = $"T{rnd.Next(1, 9)} T{rnd.Next(1, 9)} T{rnd.Next(1, 9)}",
                    Info = $"{strings[rnd.Next(1, 50) % strings.Count]} {strings[rnd.Next(1, 50) % strings.Count]}",
                });
            }

            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(trackedFiles, typeof(List<TrackedFile>), opts);
            Clipboard.SetText(json);
        }
        #endregion
    }
}
