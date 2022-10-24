using System;
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


namespace Ephemera.NotrApp
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Data store.</summary>
        readonly Db _db = new();

        /// <summary>The BindingSource for the data.</summary>
        readonly BindingSource _bs = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("NotrApp", "Ephemera");
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
            // KeyPreview = true; // for routing kbd strokes through OnKeyDown first.

            // Other UI items.
            ToolStrip.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.ControlColor };
            ddbTags.DropDown.Closing += (object? sender, ToolStripDropDownClosingEventArgs e) =>
            {
                e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
            };


            //// FilTree settings. TODO1
            //filTree.FilterExts = _settings.FilterExts;
            //filTree.IgnoreDirs = _settings.IgnoreDirs;
            //filTree.SingleClickSelect = _settings.SingleClickSelect;

            // File handling.
            OpenMenuItem.Click += (_, __) => Open_Click();
            MenuStrip.MenuActivate += (_, __) => UpdateUi();
            FileMenuItem.DropDownOpening += File_DropDownOpening;

            // Tools.
            AboutMenuItem.Click += (_, __) => MiscUtils.ShowReadme("NotrApp");
            SettingsMenuItem.Click += (_, __) => EditSettings();

            // The db.
            _db = new();
            _db.Load(Path.Combine(appDir, "db.json"));

            UpdateUi();

            InitDgv();

            Text = $"NotrApp {MiscUtils.GetVersionString()}";

            //FakeDbMenuItem.Visible = false;
            FakeDbMenuItem.Click += (_, __) => { _db.FillFake(); _db.Save(); };
        }


        void InitDgv()
        {
            _bs.AllowNew = true; // need this so add works
            _bs.DataSource = _db.Files;
            // _bsQuery.SupportsSorting = true;

            //dgvFiles.AutoGenerateColumns = true;
            dgvFiles.DataSource = _bs;
            //dgvFiles.AllowUserToAddRows = true;
            //dgvFiles.RowHeadersVisible = true;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvFiles.ColumnHeaderMouseClick += DgvFiles_ColumnHeaderMouseClick;
            dgvFiles.ColumnHeaderMouseDoubleClick += DgvFiles_ColumnHeaderMouseDoubleClick;

            dgvFiles.CellBeginEdit += DgvFiles_CellBeginEdit;

            dgvFiles.UserDeletingRow += DgvFiles_UserDeletingRow;

           // dgvFiles.ContextMenuStrip.Opening += DgvContextMenuStrip_Opening;

            // TODO1 Open txt file as ntr?
            // const string NOTR_FILE_TYPES = "*.ntr";
            // const string EDITOR_FILE_TYPES = "*.txt;*.csv;*.json;*.c;*.cpp;*.cs;*.h";
            // const string APP_FILE_TYPES = "*.doc;*.docx;*.xsl;*.xslx;*.pdf";


            // TODO1 Filter: tags, file/dir name (wildcards).



            // dgv.CellFormatting += DataGridViewCellFormattingEventHandler(this.dgvReagents_CellFormatting);
            // dgv.CellMouseDown += DataGridViewCellMouseEventHandler(this.DataGridView_CellMouseDown);
            // dgv.CellValueChanged += DataGridViewCellEventHandler(this.dgvReagents_CellValueChanged);
            // dgv.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvSetDirty_CurrentCellDirtyStateChanged);
            // dgv.DataError += DataGridViewDataErrorEventHandler(this.DataGridView_DataError);
            // dgv.DefaultValuesNeeded += DataGridViewRowEventHandler(this.dgvReagents_DefaultValuesNeeded);
            // dgv.EditingControlShowing += DataGridViewEditingControlShowingEventHandler(this.dgvReagents_EditingControlShowing);
            // dgv.RowsAdded += DataGridViewRowsAddedEventHandler(this.dgvReagents_RowsAdded);
            // dgv.KeyDown += dgvReagents_KeyDown;
            // this.dgvResults.CellClick += DataGridViewCellEventHandler(this.dgvResults_CellClick);
            //dataGridViewQueryList.RowPostPaint += new DataGridViewRowPostPaintEventHandler(DataGridViewQueryList_RowPostPaint);


        }

        private void DgvContextMenuStrip_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {


        }

        void DgvFiles_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
        {
            // TODO1 ask user first.
        }

        void DgvFiles_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            // TODO1 if it's full name, pop up file open dlg.
            // TODO1 if it's tags, pop up tag selector. ??
        }



        /// <summary>
        /// Do sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DgvFiles_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var colsel = dgvFiles.Columns[e.ColumnIndex];
            var prop = colsel.Name;
            char cop = colsel.HeaderText.Last();

            // Reset all.
            foreach (DataGridViewColumn col in dgvFiles.Columns)
            {
                col.HeaderText = col.Name;
            }

            bool asc = cop != '+';
            _db.Sort(prop, asc);
            _bs.ResetBindings(false);
            // Update selected col.
            colsel.HeaderText = $"{colsel.Name} {(asc ? '+' : '-')}";
        }

        void DgvFiles_ColumnHeaderMouseDoubleClick(object? sender, DataGridViewCellMouseEventArgs e)
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

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Menu management
        /// <summary>
        /// Show the recent files in the menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void File_DropDownOpening(object? sender, EventArgs e)
        {
            var vv = FileMenuItem.DropDownItems;

            RecentMenuItem.DropDownItems.Clear();

            _settings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f);
                menuItem.Click += (object? sender, EventArgs e) =>
                {
                    string fn = sender!.ToString()!;
                    OpenFile(fn);
                };

                RecentMenuItem.DropDownItems.Add(menuItem);
            });
        }

        /// <summary>
        /// Set UI item enables according to system states.
        /// </summary>
        void UpdateUi()
        {
            bool anyOpen = false;
            //bool anyDirty = false;

            OpenMenuItem.Enabled = true;
            CloseMenuItem.Enabled = anyOpen;
            CloseAllMenuItem.Enabled = anyOpen;
            ExitMenuItem.Enabled = true;

            AboutMenuItem.Enabled = true;
            SettingsMenuItem.Enabled = true;
        }
        #endregion

        #region File I/O
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
                // Do validity checks.
                if (!File.Exists(fn))
                {
                    throw new InvalidOperationException($"Invalid file.");
                }

                var ext = Path.GetExtension(fn).ToLower();
                var baseFn = Path.GetFileName(fn);

                // Valid file name.
                _logger.Info($"Opening file: {fn}");


                if (ok)
                {
                    _settings.UpdateMru(fn);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Couldn't open the file: {fn} because: {ex.Message}");
                ok = false;
            }

            UpdateUi();

            return ok;
        }

        /// <summary>
        /// Allows the user to select from file system.
        /// </summary>
        void Open_Click()
        {
            //var fn = GetUserFilename();
            //if (fn != "")
            //{
            //    OpenFile(fn);
            //}
        }

        #endregion

        //#region Navigator
        ///// <summary>
        ///// Tree has selected a file.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="fn"></param>
        //void Navigator_FileSelectedEvent(object? sender, string fn)
        //{
        //    OpenFile(fn);
        //}
        //#endregion

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

                    case "SingleClickSelect":
//                        filTree.SingleClickSelect = _settings.SingleClickSelect;
                        break;

                    //case "SplitterPosition":
                    //    filTree.SplitterPosition = _settings.SplitterPosition;
                    //    break;
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
            _settings.Save();
        }
        #endregion
    }
}
