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


namespace Ephemera.FileFam
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Data store.</summary>
        readonly Db _db_XXX = new();

        /// <summary>The BindingSource for the data.</summary>
        readonly BindingSource _bs = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Improve performance and eliminate flicker.
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            



            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("FileFam", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();

            // Init logging.
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogEvent += (object? sender, LogEventArgs e) => { this.InvokeIfRequired(_ => { tvLog.AppendLine($"{e.Message}"); }); };
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

            // Other UI items.
            // ToolStrip.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.ControlColor };
            ddbTags.DropDown.Closing += (object? sender, ToolStripDropDownClosingEventArgs e) =>
            {
                e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
            };

            // File handling.
            //OpenMenuItem.Click += (_, __) => Open_Click();
            //MenuStrip.MenuActivate += (_, __) => UpdateUi();
            //FileMenuItem.DropDownOpening += File_DropDownOpening;

            //btnOpenDb.Click += OpenDb_Click;

            // Tools.
            AboutMenuItem.Click += (_, __) => MiscUtils.ShowReadme("FileFam");
            SettingsMenuItem.Click += (_, __) => EditSettings();
            //FakeDbMenuItem.Click += (_, __) => { _db.FillFake(); _db.Save(); };

            // The db.
            _db_XXX = new();
            var dbfn = Path.Combine(appDir, "filefam_db.json");
            if (!_db_XXX.Load(dbfn))
            {
                _logger.Warn("Couldn't open db file so I made a new one for you");
            }

            InitDgv();

            //InitList();


            /////////////////////////////////////////////////////


            Text = $"FileFam {MiscUtils.GetVersionString()}";
            statusInfo.Text = "";
        }

        void InitList()
        {
            //this.listView.DoubleBuffer();


            FileList.FullRowSelect = true;
            FileList.GridLines = true;
            FileList.View = View.Details;

            FileList.Items.Clear();

            // Get widths from settings.
            var widths = _settings.ColumnWidths.SplitByToken(",");
            while (widths.Count < 4)
            {
                widths.Add("100");
            }

            FileList.Columns.Add("FullName", int.Parse(widths[0]));
            FileList.Columns.Add("Id", int.Parse(widths[1]));
            FileList.Columns.Add("Tags", int.Parse(widths[2]));
            FileList.Columns.Add("LastAccess", int.Parse(widths[3]));
            FileList.Columns.Add("Info"); //TODO1 fill?


            /****
            private void listView1_Resize(object sender, System.EventArgs e)
            {
                SizeLastColumn((ListView) sender);
            }

            private void SizeLastColumn(ListView lv)
            {
                lv.Columns[lv.Columns.Count - 1].Width = -2;
            }

            private void SizeLastColumn(ListView lv)
            {
             int x = lv.Width/15 == 0 ? 1 : lv.Width/15;
             lv.Columns[0].Width = x*2; 
             lv.Columns[1].Width = x;
             lv.Columns[2].Width = x*2;
             lv.Columns[3].Width = x*6;
             lv.Columns[4].Width = x*2;
             lv.Columns[5].Width = x*2;
            }


              // Sum up the width of all columns except the auto-resizing one.
              int otherColumnsWidth = 0;
              foreach (ColumnHeader header in listView.Columns)
                if (header.Index != autoSizeColumnIndex)
                  otherColumnsWidth += header.Width;

              // Calculate the (possibly) new width of the auto-resizable column.
              int autoSizeColumnWidth = listView.ClientRectangle.Width - otherColumnsWidth;

              // Finally set the new width of the auto-resizing column, if it has changed.
              if (listView.Columns[autoSizeColumnIndex].Width != autoSizeColumnWidth)
                listView.Columns[autoSizeColumnIndex].Width = autoSizeColumnWidth;
            */



            // Show the data.
            foreach (var rec in _db_XXX)
            {

                var item = new ListViewItem(new[] { rec.FullName, rec.Id, rec.Tags, rec.LastAccess.ToString(), rec.Info });
                //{
                //    Tag = file.FullName
                //};
                FileList.Items.Add(item);


            }







            FileList.SelectedIndexChanged += (_, __) => { };


            //lbFiles.MouseClick += (object? sender, MouseEventArgs e) => SetClickSelection(e);
            //lbFiles.MouseDoubleClick += (object? sender, MouseEventArgs e) => SetClickSelection(e);

        }

        //#region Types
        ///// <summary>Convenience container.</summary>
        //class ListFileInfo
        //{
        //    public string VisibleName { get; set; } = "";
        //    public string FullName { get; set; } = "";
        //    public override string ToString() { return VisibleName; }
        //}
        //#endregion


        /// <summary>
        /// Heavy lifting for the grid init.
        /// </summary>
        void InitDgv()
        {
            _bs.AllowNew = true; // need this so add works
            bool b = _bs.SupportsSorting;
            _bs.DataSource = _db_XXX;

            //dgvFiles.AutoGenerateColumns = false;
            dgvFiles.AllowUserToAddRows = true;
            //dgvFiles.RowHeadersVisible = true;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFiles.DataSource = _bs;
            //dgvFiles.DataSource = _db_XXX;

            // Set widths from settings.
            var ws = _settings.ColumnWidths.SplitByToken(",");



            for (int i = 0; i < dgvFiles.Columns.Count; i++)
            {
                var col = dgvFiles.Columns[i];
                if (i < ws.Count)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.Width = int.Parse(ws[i]);
                }
                else
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }



            //dgvFiles.Columns[Db.FullNameOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            //dgvFiles.Columns[Db.FullNameOrdinal].Width = _settings.FullNameWidth;
            //dgvFiles.Columns[Db.IdOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            //dgvFiles.Columns[Db.IdOrdinal].Width = _settings.IdWidth;
            //dgvFiles.Columns[Db.InfoOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            //dgvFiles.Columns[Db.InfoOrdinal].Width = _settings.InfoWidth;
            //dgvFiles.Columns[Db.TagsOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Event handlers.
            dgvFiles.ColumnHeaderMouseClick += DgvFiles_ColumnHeaderMouseClick;
            //dgvFiles.ColumnHeaderMouseDoubleClick += DgvFiles_ColumnHeaderMouseDoubleClick;
            dgvFiles.CellBeginEdit += DgvFiles_CellBeginEdit;
            dgvFiles.CellEndEdit += DgvFiles_CellEndEdit;
            dgvFiles.UserDeletingRow += DgvFiles_UserDeletingRow;
            dgvFiles.CellDoubleClick += DgvFiles_CellDoubleClick;

            // dgvFiles.MouseMove += DgvFiles_MouseMove;
            dgvFiles.CellMouseEnter += DgvFiles_CellMouseEnter;

            dgvFiles.UserAddedRow += DgvFiles_UserAddedRow;


            //dgvFiles.ContextMenuStrip = new();
            //dgvFiles.ContextMenuStrip.Opening += DgvContextMenuStrip_Opening;

            //dataGridViewQueryList.RowPostPaint += new DataGridViewRowPostPaintEventHandler(DataGridViewQueryList_RowPostPaint);
        }


        void DgvFiles_UserAddedRow(object? sender, DataGridViewRowEventArgs e)
        {


        }

        /// <summary>
        /// Form is legal now. Init things that want to log.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Initialize tree from user settings.
            //InitNavigator();

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

        #region Gridview event handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            statusInfo.Text = _db_XXX.GetFullName(e.RowIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            OpenFile(_db_XXX.GetFullName(e.RowIndex));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
        {
            // Ask user first.
            e.Cancel = MessageBox.Show("Are you sure you want to delete this row?", "Delete", MessageBoxButtons.OKCancel) == DialogResult.Cancel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            //var colsel = dgvFiles.Columns[e.ColumnIndex];
            switch (e.ColumnIndex)
            {
                case Db.FullNameOrdinal:
                    {
                        StringBuilder sb = new();//TODO1 this looks ugly - fix.
                        for (int f = 0; f < _settings.FileFilters.Count; f++)
                        {
                            var filter = _settings.FileFilters[f];

                            var parts = filter.SplitByToken(",");
                            if (parts.Count >= 2)
                            {
                                List<string> fs = new();
                                fs.Add($"{parts[0]} Files|");

                                for (int p = 1; p < parts.Count; p++)
                                {
                                    fs.Add($"*.{parts[p]}");
                                    if (p < parts.Count - 1)
                                    {
                                        fs.Add(";");
                                    }
                                }
                                sb.Append(string.Join("", fs));

                                if (f < _settings.FileFilters.Count - 1)
                                {
                                    sb.Append("|");
                                }
                            }
                            else
                            {
                                _logger.Warn($"Something wrong with your FileFilters setting: {filter}");
                            }
                        }
                        var sfilter = sb.ToString();

                        // Pop up file open dlg.
                        using OpenFileDialog openDlg = new()
                        {
                            Title = "Select a file",
                            Filter = sfilter
                        };

                        if (openDlg.ShowDialog() == DialogResult.OK)
                        {
                            dgvFiles.CurrentCell.Value = openDlg.FileName;
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                    break;

                case Db.TagsOrdinal:
                    {
                        // Convert to list and pop up tag selector. When done update db list.
                        var selcurrentTags = dgvFiles.CurrentCell.Value.ToString()!.SplitByToken(" ").Distinct();
                        Dictionary<string, bool> values = new();
                        _db_XXX.GetAllTags().ForEach(t => values[t] = selcurrentTags.Contains(t));

                        using OptionsEditor oped = new()
                        {
                            AllowEdit = true,
                            Values = values,
                            StartPosition = FormStartPosition.Manual,
                            Location = Cursor.Position
                        };

                        using Form f = new()
                        {
                            Text = title,
                            ClientSize = new(450, height),
                            AutoScaleMode = AutoScaleMode.None,
                            Location = Cursor.Position,
                            StartPosition = FormStartPosition.Manual,
                            FormBorderStyle = FormBorderStyle.SizableToolWindow,
                            ShowIcon = false,
                            ShowInTaskbar = false
                        };

                        f.Controls.Add(oped);

                        if (f.ShowDialog() == DialogResult.OK)
                        {
                            var selVals = oped.Values.Where(v => v.Value).Select(v => v.Key);
                            dgvFiles.CurrentCell.Value = string.Join(" ", selVals);
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                    break;

                case Db.IdOrdinal:
                case Db.InfoOrdinal:
                    // Don't care.
                    break;
            }
        }

        /// <summary>
        /// Validate cell edits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            switch (e.ColumnIndex)
            {
                case Db.FullNameOrdinal:
                    // Check for valid file.
                    var fn = dgvFiles.CurrentCell.Value.ToString();
                    //var fn = _db_XXX.GetFullName(e.RowIndex);
                    if (!File.Exists(fn))
                    {
                        _logger.Warn($"Invalid file: {fn}");
                        dgvFiles.CancelEdit();
                    }
                    // TODO1 Check for duplicate entry.
                    break;

                case Db.IdOrdinal:
                    // Clean invalid chars.
                    var id = dgvFiles.CurrentCell.Value.ToString()!.Replace(' ', '_');
                    // TODO1 Check for duplicate entry.
                    dgvFiles.CurrentCell.Value = id;
                    break;

                case Db.TagsOrdinal: // Validated above.
                case Db.InfoOrdinal: // Free form text.
                    break;
            }
        }

        /// <summary>
        /// Do sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var colsel = dgvFiles.Columns[e.ColumnIndex];
            var name = colsel.Name;
            char cop = colsel.HeaderText.Last();

            // Reset all.
            foreach (DataGridViewColumn col in dgvFiles.Columns)
            {
                col.HeaderText = col.Name;
            }

            bool asc = cop != '+';

            _db_XXX.Sort(e.ColumnIndex, asc);

           

            _bs.ResetBindings(false);

           // dgvFiles.Update();

            // Update selected col.
            colsel.HeaderText = $"{name} {(asc ? '+' : '-')}";
        }
        #endregion

        #region Helpers
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

        ///// <summary>
        ///// Get the full name at the row index.
        ///// </summary>
        ///// <param name="index"></param>
        ///// <returns>The name or an empty string if invalid.</returns>
        //string GetFullName(int index)
        //{
        //    var fn = "";
        //    if (index < _db.Files.Count && index >= 0)
        //    {
        //        fn = _db.Files[index].FullName;
        //    }
        //    return fn;
        //}
        #endregion

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
            List<string> ws = new();
            for (int i = 0; i < dgvFiles.Columns.Count - 1; i++)
            {
                var col = dgvFiles.Columns[i];
                ws.Add(col.Width.ToString());
            }
            _settings.ColumnWidths = string.Join(",", ws);

            _settings.Save();
        }
        #endregion
    }
}
