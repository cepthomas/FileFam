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


// TODO1 Open txt file as ntr? custom aliases and/or pgm associations?
// subl -n -w %1 --command "set_file_type {\"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\"}"
// When I try this, I see that it sets the syntax of the tab that was active before I executed the command.
// This tells us, that the command supplied on the command line is executed before the file is loaded, likely because ST does this asynchronously.
// For me, it's possible to get it working by using a separate invocation:
// subl C:\test\README && subl --command "set_file_type { \"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\" }"
// Note that I'm not using -w, as this would wait until the file is closed before executing the command.
// Also, you can set the syntax of a new file directly using the new_file command:
// subl --command "new_file { \"syntax\": \"Packages/JavaScript/JSON.sublime-syntax\" }"
// Obviously, if you want it in a new window, you can keep the -n argument. And if you want Sublime Text not to return control to the shell until you close the file, then you can keep the -w too, but from what I can see, that only works if you are opening a file, not when creating a new one. And if you use -w, you won't be able to change the syntax from the command line. You may be better off using a plugin like ApplySyntax or writing a small Python script yourself to set the file type when a file is opened with the path C:\test\README etc.


namespace Ephemera.NotrApp//TODO1 probably rename this.
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
            //KeyPreview = true; // for routing kbd strokes through OnKeyDown first.

            // Other UI items.
            ToolStrip.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.ControlColor };
            ddbTags.DropDown.Closing += (object? sender, ToolStripDropDownClosingEventArgs e) =>
            {
                e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
            };

            // File handling.
            //OpenMenuItem.Click += (_, __) => Open_Click();
            //MenuStrip.MenuActivate += (_, __) => UpdateUi();
            //FileMenuItem.DropDownOpening += File_DropDownOpening;

            // Tools.
            AboutMenuItem.Click += (_, __) => MiscUtils.ShowReadme("NotrApp");
            SettingsMenuItem.Click += (_, __) => EditSettings();
            //FakeDbMenuItem.Click += (_, __) => { _db.FillFake(); _db.Save(); };

            // The db.
            _db = new();
            _db.Load(Path.Combine(appDir, "db.json"));//TODO1 user selectable db file. store mru?

            //UpdateUi();

            InitDgv();

            Text = $"NotrApp {MiscUtils.GetVersionString()}";
            statusInfo.Text = "TODO1 needed?";
        }

        /// <summary>
        /// Heavy lifting for the grid.
        /// </summary>
        void InitDgv()
        {
            _bs.AllowNew = true; // need this so add works
            //_bsQuery.SupportsSorting = true;
            _bs.DataSource = _db.Files;

            //dgvFiles.AutoGenerateColumns = false;
            //dgvFiles.AllowUserToAddRows = true;
            //dgvFiles.RowHeadersVisible = true;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFiles.DataSource = _bs;

            // Set widths from settings.
            dgvFiles.Columns[Db.FullNameOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvFiles.Columns[Db.FullNameOrdinal].Width = _settings.FullNameWidth;
            dgvFiles.Columns[Db.IdOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvFiles.Columns[Db.IdOrdinal].Width = _settings.IdWidth;
            dgvFiles.Columns[Db.InfoOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvFiles.Columns[Db.InfoOrdinal].Width = _settings.InfoWidth;
            dgvFiles.Columns[Db.TagsOrdinal].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Event handlers.
            dgvFiles.ColumnHeaderMouseClick += DgvFiles_ColumnHeaderMouseClick;
            //dgvFiles.ColumnHeaderMouseDoubleClick += DgvFiles_ColumnHeaderMouseDoubleClick;
            dgvFiles.CellBeginEdit += DgvFiles_CellBeginEdit;
            dgvFiles.CellEndEdit += DgvFiles_CellEndEdit;
            dgvFiles.UserDeletingRow += DgvFiles_UserDeletingRow;
            //dgvFiles.UserAddedRow += DgvFiles_UserAddedRow;
            dgvFiles.CellDoubleClick += DgvFiles_CellDoubleClick;

           // dgvFiles.MouseMove += DgvFiles_MouseMove;
            dgvFiles.CellMouseEnter += DgvFiles_CellMouseEnter;

            //dgvFiles.ContextMenuStrip = new();
            //dgvFiles.ContextMenuStrip.Opening += DgvContextMenuStrip_Opening;

            //dataGridViewQueryList.RowPostPaint += new DataGridViewRowPostPaintEventHandler(DataGridViewQueryList_RowPostPaint);
        }

        void DgvFiles_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
        {
            statusInfo.Text = GetFullName(e.RowIndex);
        }

        void DgvFiles_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            OpenFile(GetFullName(e.RowIndex));
        }

        void DgvFiles_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
        {
            // Ask user first.
            e.Cancel = MessageBox.Show("Are you sure you want to delete this row?", "Delete", MessageBoxButtons.OKCancel) == DialogResult.Cancel;
        }

        void DgvFiles_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            //var colsel = dgvFiles.Columns[e.ColumnIndex];
            switch (e.ColumnIndex)
            {
                case Db.FullNameOrdinal:
                    {
                        // Pop up file open dlg.
                        using OpenFileDialog openDlg = new()
                        {
                            Title = "Select a file"
                            //Filter = fileTypes, TODO1 from settings + AllFiles
                            // "FileFilters": [
                            //     "Notr,ntr,notr",
                            //     "Text,txt,csv,json",
                            //     "Doc,doc,docx,xsl,xslx,pdf",
                            //     "All,*"
                            //   ],
                            //     -->
                            // "Notr Files|*.ntr;*.notr|Text Files|*.txt; ..."

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
                        _db.AllTags.ForEach(t => values[t] = selcurrentTags.Contains(t));

                        using OptionsEditor oped = new()
                        {
                            AllowEdit = true,
                            Values = values,
                            StartPosition = FormStartPosition.Manual,
                            Location = Cursor.Position
                        };

                        if (oped.ShowDialog() == DialogResult.OK)
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

        void DgvFiles_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            switch (e.ColumnIndex)
            {
                case Db.FullNameOrdinal:
                    // Check for valid file.
                    if (!File.Exists(GetFullName(e.RowIndex)))
                    {
                        dgvFiles.CancelEdit();
                    }
                    break;

                case Db.IdOrdinal:
                    // Clean invalid chars.
                    var id = dgvFiles.CurrentCell.Value.ToString()!.Replace(' ', '_');
                    // Check for uniqueness. TODO1?
                    dgvFiles.CurrentCell.Value = id;
                    break;

                case Db.TagsOrdinal:
                case Db.InfoOrdinal:
                    break;
            }
        }




        /// <summary>
        /// Do sorting. TODO1 also by mru.
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
            _db.Sort(e.ColumnIndex, asc);
            _bs.ResetBindings(false);
            // Update selected col.
            colsel.HeaderText = $"{name} {(asc ? '+' : '-')}";
        }


        /// <summary>
        /// Get the full name at the row index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The name or an empty string if invalid.</returns>
        string GetFullName(int index)
        {
            var fn = "";

            if (index < _db.Files.Count && index >= 0)
            {
                fn = _db.Files[index].FullName;
            }

            return fn;
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

        /// <summary>
        /// Common file opener.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        void OpenFile(string fn) //TODO1 open txt as ntr?
        {
            bool ok = File.Exists(fn);

            if (ok)
            {
                var ext = Path.GetExtension(fn).ToLower();
                var baseFn = Path.GetFileName(fn);

                // Valid file name.
                _logger.Info($"Opening file: {fn}");

                //using Process fileopener = new Process();
                //fileopener.StartInfo.FileName = "explorer";
                //fileopener.StartInfo.Arguments = "\"" + path + "\"";
                //fileopener.Start();

                Process.Start("explorer", "\"" + fn + "\"");

                _settings.UpdateMru(fn);
            }
            else
            {
                _logger.Warn($"Inalid file: {fn}");
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

                    // case "SingleClickSelect":
                    //    filTree.SingleClickSelect = _settings.SingleClickSelect;
                    //     break;

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
