using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using System.Globalization;
using System.Threading;

namespace Ephemera.FileFam
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;
        #endregion

        DataStore _ds = new();


        /// <summary>Prevent resize recursion.</summary>
        bool _resizing = false;

        /// <summary>Current column selected for editing or sorting.</summary>
        int _selColumn = -1;

        /// <summary>Current sort spec.</summary>
        SortOrder _sortOrder = SortOrder.None;

        /// <summary>Visuals.</summary>
        const char ASC = '▲';

        /// <summary>Visuals.</summary>
        const char DESC = '▼';



        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Improve performance and eliminate flicker.
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            
            // Sortable datetime format.
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentCulture = culture;



            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("FileFam", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();

            // Init logging.
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => { this.InvokeIfRequired(_ => { tvLog.AppendLine($"{e.Message}"); }); };
            LogManager.Run(Path.Join(appDir, "log.txt"), 100000);

            // Log display.
            tvLog.Font = Font;
            tvLog.MatchColors.Add("ERR", Color.LightPink);
            tvLog.MatchColors.Add("WRN", Color.Plum);

            // Init main form from settings.
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            //KeyPreview = true; // for routing kbd strokes through OnKeyDown first.



            // Tools. TODO1
            //btnOpenDb.Click += OpenDb_Click;
            // AboutMenuItem.Click += (_, __) => MiscUtils.ShowReadme("FileFam");
            // SettingsMenuItem.Click += (_, __) => EditSettings();
            //FakeDbMenuItem.Click += (_, __) => { MakeFake(); _db.Save(); };

            // The DataStore.
            _ds = new();
            var dsfn = Path.Combine(appDir, "filefam_db.json");//TODO1
            if (!_ds.Load(dsfn))
            {
                _logger.Warn("Couldn't open db file so I made a new one for you");
            }

            /////////////////////////////////////////////////////


            Text = $"FileFam {MiscUtils.GetVersionString()}";
            lblInfo.Text = "";
        }


        /// <summary>
        /// Form is legal now. Init controls.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {

            ///// These are only for dev. get/put from settings + all possibles.
            lv.Location = new(5, 5);
            //lv.Size = new(700, ClientSize.Height - 10);
            Dictionary<string, bool> filters = new();
            for (int i = 1; i < 10; i++)
            {
                filters.Add($"T{i}", i % 3 == 0);
            }

            optionsEdit.Options = filters;
            optionsEdit.OptionsChanged += OptionsEdit_OptionsChanged;
            //btnAll.Click += (_, __) => { _filters.Keys.ForEach(k => _filters[k] = true); optionsEdit.Options = _filters; };
            //btnNone.Click += (_, __) => { _filters.Keys.ForEach(k => _filters[k] = false); optionsEdit.Options = _filters; };

            ///// These could be in designer.
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.MultiSelect = false;
            lv.View = View.Details;

            txtEdit.Visible = false;

            ///// Real/typical.
            // Get settings. TODO1 >>>>>>>>>>>>>>>
            List<int> colWidths = new() { 70, 100, 160, 100, 0 };
            // Create listview.
            for (int i = 0; i < Record.ColumnNames.Length; i++)
            {
                lv.Columns.Add(Record.ColumnNames[i], Record.ColumnNames[i], colWidths[i]);
            }

            ShowRecords();

            lv.ContextMenuStrip = new();
            lv.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Add", null, Lv_Add));
            lv.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Delete", null, Lv_Delete));

            // All interesting events.
            lv.Click += Lv_Click;
            //lv.DoubleClick += Lv_DoubleClick; TODO1 probably opens the file. Update the timestamp too.
            lv.ColumnClick += Lv_ColumnClick;
            lv.Resize += (_, __) => ResizeLv();
            lv.ColumnWidthChanged += (_, __) => ResizeLv();

            // All interesting events.
            txtEdit.Leave += TxtEdit_Leave;
            txtEdit.KeyDown += TxtEdit_KeyDown;

            ResizeLv();

            base.OnLoad(e);
        }


        /// <summary>
        /// Bye-bye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
 //           _db.Save();
            SaveSettings();
            _logger.Info("Shutting down");
            LogManager.Stop();
            base.OnFormClosing(e);
        }
        #endregion



        /// <summary>
        /// Common file opener.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        void OpenFile(string fn)
        {
            bool ok = File.Exists(fn);

            if (ok)
            {
                // var ext = Path.GetExtension(fn).ToLower();
                // var baseFn = Path.GetFileName(fn);

                // Valid file name.
                _logger.Info($"Opening file: {fn}");

                Process.Start("explorer", "\"" + fn + "\"");

                _settings.UpdateMru(fn);
            }
            else
            {
                _logger.Warn($"Invalid file: {fn}");
            }
        }




        /// <summary>
        /// Filters changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OptionsEdit_OptionsChanged(object? sender, EventArgs e)
        {
            ShowRecords();
        }

        /// <summary>
        /// Add a new record.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Add(object? sender, EventArgs e)
        {
            var rec = new Record();
            _ds.Add(rec);

            ShowRecords();

            // Select the added record - at end.
            int row = lv.Items.Count - 1;
            lv.Items[row].Selected = true;
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
                int row = lv.SelectedIndices[0];
                var rec = lv.SelectedItems[0].Tag as Record;
                _ds.Remove(rec!);

                ShowRecords();

                // Select the next record.
                if (lv.Items.Count > 0)
                {
                    row = Math.Min(row, lv.Items.Count - 1);
                    lv.Items[row].Selected = true;
                }
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

            var selRecordId = (lv.SelectedItems[0].Tag as Record)!.Id;

            ShowRecords();

            // Show the current selected row.
            for (int i = 0; i < lv.Items.Count; i++)
            {
                if ((lv.Items[i].Tag as Record)!.Id == selRecordId)
                {
                    lv.Items[i].Selected = true;
                    break;
                }
            }

            // Update selected col text.
            colsel.Text = $"{name} {(_sortOrder == SortOrder.Ascending ? ASC : DESC)}";
        }

        /// <summary>
        /// Starts cell editing maybe.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lv_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var pt = PointToClient(MousePosition);
                var hitTestInfo = lv.HitTest(pt);
                var record = hitTestInfo.Item.Tag as Record;
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
                var record = lv.SelectedItems[0].Tag as Record;
                bool ok = record!.Parse(txtEdit.Text, _selColumn);

                if (ok)
                {
                    int row = lv.SelectedIndices[0];

                    ShowRecords();

                    lv.Items[row].Selected = true;
                }
            }
            // else dump

            txtEdit.Visible = false;
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
        /// Sort and filter then update the listview. This is a client responsibility.
        /// </summary>
        void ShowRecords()
        {
            // IEnumerable<Record> records
            IEnumerable<Record> sorted;

            // Filter by tags.
            HashSet<string> filterTags = new();
            optionsEdit.Options.Where(o => o.Value == true).ForEach(o => filterTags.Add(o.Key));
            var qry = filterTags.Count > 0 ?
                _ds.Where(rec => rec.Tags.SplitByToken(" ").Intersect(filterTags).Any()) :
                _ds;

            // Sort by column and direction.
            sorted = (_selColumn, _sortOrder) switch // TODO2 improve? IComparer?
            {
                (0, SortOrder.Ascending) => qry.OrderBy(r => r.FullName),
                (0, SortOrder.Descending) => qry.OrderByDescending(r => r.FullName),
                (1, SortOrder.Ascending) => qry.OrderBy(r => r.Id),
                (1, SortOrder.Descending) => qry.OrderByDescending(r => r.Id),
                (2, SortOrder.Ascending) => qry.OrderBy(r => r.LastAccess),
                (2, SortOrder.Descending) => qry.OrderByDescending(r => r.LastAccess),
                (3, SortOrder.Ascending) => qry.OrderBy(r => r.Tags),
                (3, SortOrder.Descending) => qry.OrderByDescending(r => r.Tags),
                (4, SortOrder.Ascending) => qry.OrderBy(r => r.Info),
                (4, SortOrder.Descending) => qry.OrderByDescending(r => r.Info),
                (_, _) => qry // no sort - use raw
            };

            // Show the data.
            lv.SuspendLayout();
            lv.Items.Clear();
            foreach (var d in sorted)
            {
                var item = new ListViewItem(d.ValueStrings) { Tag = d };
                lv.Items.Add(item);
            }
            lv.ResumeLayout();

            lblInfo.Text = $"{lv.Items.Count} records";
        }

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
            _ds.Clear();
            Random rnd = new();
            for (int i = 0; i < 50; i++)
            {
                _ds.Add(new()
                {
                    FullName = $"FullName_{rnd.Next(1, 50)}",
                    Id = $"Id_{rnd.Next(1, 50)}",
                    LastAccess = DateTime.Now + new TimeSpan(rnd.Next(1, 1300), rnd.Next(1, 20), rnd.Next(1, 50), rnd.Next(1, 50)),
                    Tags = $"T{rnd.Next(1, 9)} T{rnd.Next(1, 9)} T{rnd.Next(1, 9)}",
                    Info = $"{strings[rnd.Next(1, 50) % strings.Count]} {strings[rnd.Next(1, 50) % strings.Count]}",
                });
            }
        }






        #region Settings
        /// <summary>
        /// Edit user settings.
        /// </summary>
        void EditSettings()
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            //// Detect changes of interest. Lists are ref bound so already updated.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "ControlColor":
                    case "FileLogLevel":
                    case "NotifLogLevel":
                        restart = true;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }

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
            for (int i = 0; i < lv.Columns.Count - 1; i++)
            {
                var col = lv.Columns[i];
                _settings.ColumnWidths.Add(col.Width);
            }

            _settings.Save();
        }
        #endregion
    }
}
