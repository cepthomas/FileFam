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
// using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


//Lambda Expression: Var Val = value. Where (Val<=5);
//Query Syntax: Var Val = from val in Value Where val> 10;
//
// var query =
//    from c in db.Customers
//    where c.Name.StartsWith ("A") || c.Name.StartsWith ("B")
//    orderby c.Name
//    select c.Name.ToUpper();
// var thirdPage = query.Skip(20).Take(10);
// You might have noticed another more subtle (but important) benefit of the LINQ approach. We chose to compose
// the query in two steps—and this allows us to generalize the second step into a reusable method as follows:
// IQueryable<T> Paginate<T> (this IQueryable<T> query, int skip, int take)
// {
//    return query.Skip(skip).Take(take);
// }


namespace Ephemera.FileFam
{
    public partial class FileFam : Form
    {
        #region Fields
        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>The ff config records.</summary>
        readonly List<TrackedFile> _trackedFiles = new();

        /// <summary>The ff config filename.</summary>
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

            // The ff config.
            if (_settings.RecentFiles.Count > 0)
            {
                OpenFile(_settings.RecentFiles[0]);

            }
            optionsEdit.OptionsChanged += OptionsEdit_OptionsChanged;

            // Listview.
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.MultiSelect = false;
            lv.View = View.Details;
            while (_settings.ColumnWidths.Count < TrackedFile.ColumnNames.Length)
            {
                _settings.ColumnWidths.Add(20);
            }
            for (int i = 0; i < TrackedFile.ColumnNames.Length; i++)
            {
                string name = TrackedFile.ColumnNames[i];
                lv.Columns.Add(name, name, _settings.ColumnWidths[i]);
            }
            lv.ContextMenuStrip = new();
            lv.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Add", null, Lv_Add));
            lv.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Delete", null, Lv_Delete));
            // All interesting events.
            lv.Click += Lv_Click;
            lv.DoubleClick += Lv_DoubleClick;
            lv.ColumnClick += Lv_ColumnClick;
            lv.Resize += (_, __) => ResizeLv();
            lv.ColumnWidthChanged += (_, __) => ResizeLv();

            // Misc UI.
            txtEdit.Visible = false;
            txtEdit.Leave += TxtEdit_Leave;
            txtEdit.KeyDown += TxtEdit_KeyDown;

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

            lblInfo.Text = "";
        }

        /// <summary>
        /// Form is legal now. Init controls.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            ShowRecords(0);
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
                var records = (List<TrackedFile>)JsonSerializer.Deserialize(json, typeof(List<TrackedFile>), opts)!;
                records.ForEach(r => _trackedFiles.Add(r));

                _fffn = fn;
                _dirty = false;

                // Get all tags in ff ordered by frequency.
                Dictionary<string, bool> allTags = new();
                Dictionary<string, int> ffTags = new();
                _trackedFiles
                    .ForEach(f => f.Tags.SplitByToken(" ")
                    .ForEach(t =>
                    {
                        if (!ffTags.ContainsKey(t))
                        {
                            ffTags.Add(t, 0);
                        }
                        ffTags[t]++;
                    }
                ));

                ffTags
                    .OrderByDescending(t => t.Value)
                    .ForEach(t => allTags.Add(t.Key, false));

                optionsEdit.Options = allTags;

                _fffn = fn;
                _settings.UpdateMru(fn);
                _dirty = false;
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
        void OptionsEdit_OptionsChanged(object? sender, EventArgs e)
        {
            ShowRecords(-1);
        }

        /// <summary>
        /// Add a new record.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Add(object? sender, EventArgs e)
        {
            var rec = new TrackedFile();
            _trackedFiles.Add(rec);
            _dirty = true;
            ShowRecords(rec.UID);
        }

        /// <summary>
        /// Remove current record.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Delete(object? sender, EventArgs e)
        {
            if (lv.SelectedIndices.Count > 0)
            {
                // Get the next item.
                int nextuid = -1;
                int row = lv.SelectedIndices[0];
                if (lv.Items.Count > row + 1)
                {
                    var nextrec = lv.Items[row + 1].Tag as TrackedFile;
                    nextuid = nextrec!.UID;
                }

                // Process selection.
                var rec = lv.SelectedItems[0].Tag as TrackedFile;
                _trackedFiles.Remove(rec!);
                _dirty = true;

                ShowRecords(nextuid);
            }
        }

        /// <summary>
        /// Does sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            _selColumn = e.Column;

            var colsel = lv.Columns[_selColumn];
            var name = colsel.Name;

            // Current sort.
            char cop = colsel.Text.Last();

            // New sort.
            _sortOrder = cop == ASC ? SortOrder.Descending : SortOrder.Ascending;

            // Reset all column headers.
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                var colhdr = lv.Columns[i];
                colhdr.Text = colhdr.Name;
            }

            var seluid = (lv.SelectedItems[0].Tag as TrackedFile)!.UID;

            ShowRecords(seluid);

            // Update selected col text.
            colsel.Text = $"{name} {(_sortOrder == SortOrder.Ascending ? ASC : DESC)}";
        }

        /// <summary>
        /// Opens the target file clicked by the user. Update the timestamp too.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_DoubleClick(object? sender, EventArgs e)
        {
            var tf = lv.SelectedItems[0].Tag as TrackedFile;
            var fn = tf!.FullName;

            bool ok = File.Exists(fn);

            if (ok)
            {
                // var ext = Path.GetExtension(fn).ToLower();
                // var baseFn = Path.GetFileName(fn);

                // Valid file name.
                var cmd = _settings.TargetCommand.Replace("%F", fn);
                ShowMessage($"Executing target command: {cmd}");

                var info = new ProcessStartInfo(cmd) { UseShellExecute = true };
                var proc = new Process() { StartInfo = info };
                proc.Start();

                tf.LastAccess = DateTime.Now;
                _dirty = true;

                ShowRecords(tf.UID);
            }
            else
            {
                ShowMessage($"WRN Invalid file: {fn}");
            }
        }

        /// <summary>
        /// Starts cell editing, maybe.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var pt = PointToClient(MousePosition);
                var hitTestInfo = lv.HitTest(pt);
                var record = hitTestInfo.Item.Tag as TrackedFile;
                var subItem = hitTestInfo.SubItem;
                _selColumn = hitTestInfo.Item.SubItems.IndexOf(subItem);

                // Show the text to edit.
                string lastContent = record!.StringAt(_selColumn);

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
                    lv.Focus(); // force Leave
                    break;

                case Keys.Escape:
                    // Indicates cancel.
                    txtEdit.Text = "";
                    lv.Focus(); // force Leave
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
                var record = lv.SelectedItems[0].Tag as TrackedFile;
                bool ok = record!.Parse(txtEdit.Text, _selColumn);

                if (ok)
                {
                    _dirty = true;
                    ShowRecords(record.UID);
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
        /// <param name="seluid">Put the focus on this record.</param>
        void ShowRecords(int seluid)
        {
            //IEnumerable<TrackedFile> sorted;

            // Filter by tags.
            HashSet<string> filterTags = new();
            optionsEdit.Options
                .Where(o => o.Value == true)
                .ForEach(o => filterTags.Add(o.Key));

            var qry = filterTags.Count > 0 ?
                _trackedFiles.Where(rec => rec.Tags.SplitByToken(" ").Intersect(filterTags).Any()) :
                _trackedFiles;

            // Sort by column and direction.
            var sorted = _selColumn switch
            {
                0 => qry.OrderBy(r => r.FullName),
                1 => qry.OrderBy(r => r.Id),
                2 => qry.OrderBy(r => r.LastAccess),
                3 => qry.OrderBy(r => r.Tags),
                4 => qry.OrderBy(r => r.Info),
                _ => qry // no sort - use raw
            };

            var sortedOrdered = _sortOrder == SortOrder.Descending ? sorted.Reverse() : sorted;

            // Show the data.
            lv.SuspendLayout();
            lv.Items.Clear();
            foreach (var d in sortedOrdered)
            {
                var item = new ListViewItem(d.ValueStrings) { Tag = d };
                lv.Items.Add(item);
            }
            lv.ResumeLayout();

            // Select the requested record. TODO2 optimize?
            for (int i = 0; i < lv.Items.Count; i++)
            {
                if ((lv.Items[i].Tag as TrackedFile)!.UID == seluid)
                {
                    lv.Items[i].Selected = true;
                    break;
                }
            }

            lblInfo.Text = $"{lv.Items.Count} records";
        }

        /// <summary>
        /// Does last column fill.
        /// </summary>
        void ResizeLv()
        {
            if (!_resizing) // TODO2 improve?
            {
                _resizing = true;

                int tw = 0;
                for (int i = 0; i < lv.Columns.Count - 1; i++)
                {
                    tw += lv.Columns[i].Width;
                }
                lv.Columns[^1].Width = lv.ClientRectangle.Width - tw;
                _resizing = false;
            }
        }

        /// <summary>
        /// Tell the user something.
        /// </summary>
        /// <param name="msg"></param>
        void ShowMessage(string msg)
        {
            tvLog.AppendLine($"> {msg}");
        }
        #endregion

        #region Settings
        /// <summary>
        /// Edit user settings.
        /// </summary>
        void EditSettings()
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 400);

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
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                var col = lv.Columns[i];
                _settings.ColumnWidths.Add(col.Width);
            }

            // Selected tags.
            _settings.CurrentTags.Clear();
            foreach (var kv in optionsEdit.Options)
            {
                if (kv.Value)
                {
                    _settings.CurrentTags.Add(kv.Key);
                }
            }

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
            _trackedFiles.Clear();
            Random rnd = new();
            for (int i = 0; i < 50; i++)
            {
                _trackedFiles.Add(new()
                {
                    FullName = $"FullName_{rnd.Next(1, 50)}",
                    Id = $"Id_{rnd.Next(1, 50)}",
                    LastAccess = DateTime.Now + new TimeSpan(rnd.Next(1, 1300), rnd.Next(1, 20), rnd.Next(1, 50), rnd.Next(1, 50)),
                    Tags = $"T{rnd.Next(1, 9)} T{rnd.Next(1, 9)} T{rnd.Next(1, 9)}",
                    Info = $"{strings[rnd.Next(1, 50) % strings.Count]} {strings[rnd.Next(1, 50) % strings.Count]}",
                });
            }

            _dirty = true;
        }
        #endregion
    }
}
