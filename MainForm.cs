using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using NBagOfTricks;
using NBagOfTricks.Slog;
using NBagOfUis;


namespace Notr
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");

        /// <summary>The settings.</summary>
        readonly UserSettings _settings;

        ///// <summary>UI indicator.</summary>
        //const string DIRTY_FILE_IND = "*";
        #endregion


        /// <summary>Supported types.</summary>
        public const string NOTR_FILE_TYPES = "*.ntr";
        public const string EDITOR_FILE_TYPES = "*.txt;*.csv;*.json;*.c;*.c++;*.cs;*.h";
        public const string APP_FILE_TYPES = "*.doc;*.xsl;*.xslx;*.pdf";

        //    @"C:\Users\cepth\AppData\Roaming\Sublime Text\Packages", // TODO1 hide some but not others

        #region Types

        #endregion

        #region Lifecycle
        public MainForm()
        {
            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("Notr", "Ephemera");
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

            // Bind some filtree stuff.
            ftree.Settings = _settings.FilTreeSettings;
            ftree.RecentFiles = _settings.RecentFiles;
            //ftree.Settings.RootDirs = paths;
            ftree.Init();
            ftree.FileSelectedEvent += Navigator_FileSelectedEvent;

            // File handling.
            OpenMenuItem.Click += (_, __) => Open_Click();
            MenuStrip.MenuActivate += (_, __) => UpdateUi();
            FileMenuItem.DropDownOpening += File_DropDownOpening;

            // Tools.
            AboutMenuItem.Click += (_, __) => MiscUtils.ShowReadme("Notr");
            SettingsMenuItem.Click += (_, __) => EditSettings();

            UpdateUi();

            Text = $"Notr {MiscUtils.GetVersionString()}";

            btnDB.Click += (_, __) => { Db dt = new(); dt.DoAll(); };
        }

        /// <summary>
        /// Form is legal now. Init things that want to log.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Initialize tree from user settings.
            InitNavigator();

            base.OnLoad(e);
        }

        /// <summary>
        /// Bye-bye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();
            SaveSettings();
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

            // >>> My stuff here.

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
                    _settings.RecentFiles.UpdateMru(fn);
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

        #region Navigator
        /// <summary>
        /// Initialize tree from user settings.
        /// </summary>
        void InitNavigator()
        {
            ftree.Settings.FilterExts.Clear();
            ftree.Settings.FilterExts.AddRange(NOTR_FILE_TYPES.SplitByTokens(";*"));
            ftree.Settings.FilterExts.AddRange(EDITOR_FILE_TYPES.SplitByTokens(";*"));
            ftree.Settings.FilterExts.AddRange(APP_FILE_TYPES.SplitByTokens(";*"));

            //like: var fileTypes = $"Audio Files|{AudioLibDefs.AUDIO_FILE_TYPES}|Midi Files|{MidiLibDefs.MIDI_FILE_TYPES}|Style Files|{MidiLibDefs.STYLE_FILE_TYPES}";

            try
            {
                ftree.Init();
            }
            catch (DirectoryNotFoundException)
            {
                _logger.Warn("No tree directories");
            }
        }

        /// <summary>
        /// Tree has selected a file to play.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fn"></param>
        void Navigator_FileSelectedEvent(object? sender, string fn)
        {
            OpenFile(fn);
        }
        #endregion

        #region Settings
        /// <summary>
        /// Edit user settings.
        /// </summary>
        void EditSettings()
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            //// Detect changes of interest.
            //bool navChange = false;
            //bool restart = false;

            //foreach (var (name, cat) in changes)
            //{
            //    switch(name)
            //    {
            //        case "WavOutDevice":
            //        case "Latency":
            //        case "ControlColor":
            //        case "WaveColor":
            //        case "FileLogLevel":
            //        case "NotifLogLevel":
            //            restart = true;
            //            break;

            //        case "RootDirs":
            //            navChange = true;
            //            break;
            //    }
            //}

            //if (restart)
            //{
            //    MessageBox.Show("Restart required for device changes to take effect");
            //}

            //if (navChange)
            //{
            //    InitNavigator();
            //}

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
