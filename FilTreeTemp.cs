using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing.Design;
using Ephemera.NBagOfTricks;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;


namespace Ephemera.NotrApp
{
    /// <summary>
    /// Tree control with file type filters.
    /// </summary>
    public class FilTreeTemp : UserControl
    {
        #region Properties
        /// <summary>Client may supply these.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<string>? RecentFiles { get; set; } = null;

        /// <summary>Your favorite places.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<string> RootDirs { get; set; } = new();

        /// <summary>Show only these file types. Empty is valid for files without extensions.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<string> FilterExts { get; set; } = new();

        /// <summary>Ignore these noisy directories.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public List<string> IgnoreDirs { get; set; } = new();

        /// <summary>Splitter Position as Percent of width.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int SplitterPosition
        {
            get { return splitContainer.SplitterDistance * 100 / Width; }
            set { splitContainer.SplitterDistance = value * Width / 100; }
        }

        /// <summary>Generate event with single or double click.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool SingleClickSelect { get; set; } = false;
        #endregion

        #region Fields
        /// <summary>Tracking UI.</summary>
        int _lastListIndex = -1;

        /// <summary>Tracking UI.</summary>
        TreeNode? _lastNode = null;

        /// <summary>Special node.</summary>
        TreeNode? _recentNode = null;
        #endregion

        #region Events
        /// <summary>User has selected a file.</summary>
        public event EventHandler<string>? FileSelectedEvent = null;
        #endregion

        #region Types
        /// <summary>Convenience container.</summary>
        class TreeDirInfo
        {
            public string FullPath { get; set; } = "";
            public override string ToString()
            {
                return FullPath;
            }
        }

        /// <summary>Convenience container.</summary>
        class ListFileInfo
        {
            public string FullPath { get; set; } = "";
            public long Length { get; set; } = 0;
            public bool ShowFull { get; set; } = false;
            public override string ToString()
            {
                int kb = (int)(Length / 1024); // size not size on disk
                int mb = kb / 1024;
                string slen = kb > 9999 ? $"{mb}M" : $"{kb}K";
                var s = $"{(ShowFull ? FullPath : Path.GetFileName(FullPath))} ({slen})";
                return s;
            }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor. Initializes the filtree.
        /// </summary>
        public FilTreeTemp()
        {
            InitializeComponent();

            treeView.HideSelection = false;
            treeView.MouseMove += TreeView_MouseMove;
            treeView.NodeMouseClick += TreeView_NodeMouseClick;

            treeView.ContextMenuStrip = new();
            treeView.ContextMenuStrip.Items.Add("Copy Path", null, (_, __) => { GetInfo(treeView); });

            lbFiles.MouseClick += (object? sender, MouseEventArgs e) => FileSelected(e);
            lbFiles.MouseDoubleClick += (object? sender, MouseEventArgs e) => FileSelected(e);
            lbFiles.MouseMove += ListFiles_MouseMove;

            lbFiles.ContextMenuStrip = new();
            lbFiles.ContextMenuStrip.Items.Add("Copy Path", null, (_, __) => { GetInfo(lbFiles); });
        }
        #endregion

        #region Tree View
        /// <summary>
        /// Populate from properties. Client must call this after editing the properties.
        /// </summary>
        public void InitTree()
        {
            // Clean.
            treeView.Nodes.Clear();
            _recentNode = null;
            _lastNode = null;
            _lastListIndex = -1;

            RootDirs.RemoveAll(d => !Directory.Exists(d));
            if (RootDirs.Count == 0)
            {
                throw new Exception("No root directories. Edit your settings.");
            }

            // Recent files first.
            if (RecentFiles is not null)
            {
                _recentNode = new TreeNode("Recent Files") { Tag = new TreeDirInfo() { FullPath = "Recent Files" } };
                treeView.Nodes.Add(_recentNode);
            }

            // The rest.
            RootDirs.Distinct().ForEach(d =>
            {
                DirectoryInfo info = new(d);
                TreeNode node = new(info.Name) { Tag = new TreeDirInfo() { FullPath = info.FullName } };
                GetSubdirs(info.GetDirectories(), node);
                treeView.Nodes.Add(node);
            });

            if (treeView.Nodes.Count == 0)
            {
                throw new Exception("No tree entries.");
            }

            treeView.SelectedNode = treeView.Nodes[0];
            PopulateFileList(treeView.Nodes[0]);

            //// Open them up a bit.
            //foreach (TreeNode n in treeView.Nodes)
            //{
            //    n.Expand();
            //}
        }

        /// <summary>
        /// Recursively drill down through the directory structure and populate the tree.
        /// </summary>
        /// <param name="dis"></param>
        /// <param name="parentNode"></param>
        void GetSubdirs(DirectoryInfo[] dis, TreeNode parentNode)
        {
            foreach (DirectoryInfo di in dis)
            {
                // User list.
                bool ignore = IgnoreDirs.Contains(di.Name);//TODO wildcards
                // Ignore system and hidden etc.
                ignore |= (di.Attributes & FileAttributes.System) > 0;
                ignore |= (di.Attributes & FileAttributes.Hidden) > 0;

                if (!ignore)
                {
                    TreeNode subDirNode = new(di.Name, 0, 0) { Tag = new TreeDirInfo() { FullPath = di.FullName } };
                    // Recurse.
                    DirectoryInfo[] subDirs = di.GetDirectories();
                    GetSubdirs(subDirs, subDirNode);
                    parentNode.Nodes.Add(subDirNode);
                }
            }
        }

        /// <summary>
        /// Drill down through dirs/files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                PopulateFileList(e.Node);
            }
        }

        /// <summary>
        /// Show folder info at mouse position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TreeView_MouseMove(object? sender, MouseEventArgs e)
        {
            var node = treeView.GetNodeAt(e.Location);

            if (node is not null)
            {
                if (node != _lastNode)
                {
                    var nodeDirInfo = node.Tag as TreeDirInfo;

                    lblInfo.Text = $"{nodeDirInfo!.FullPath}";
                    _lastNode = node;
                }
            }
            else
            {
                lblInfo.Text = $"";
            }
        }
        #endregion

        #region File List
        /// <summary>
        /// Populate the file selector.
        /// </summary>
        /// <param name="node">Selected node.</param>
        void PopulateFileList(TreeNode node)
        {
            lbFiles.BeginUpdate();
            lbFiles.Items.Clear();

            if (node == _recentNode)
            {
                RecentFiles?.ForEach(fn => DoOne(new FileInfo(fn), true));
            }
            else
            {
                var nodeDirInfo = new DirectoryInfo((node.Tag as TreeDirInfo)!.FullPath);
                nodeDirInfo!.EnumerateFiles("*").OrderBy(f => char.IsLetterOrDigit(f.Name[0])).ForEach(finfo => DoOne(finfo, false));
            }
            lbFiles.EndUpdate();

            ///// Local common function.
            void DoOne(FileInfo finfo, bool fullName)
            {
                var ext = Path.GetExtension(finfo.Name).ToLower();

                if (FilterExts.Contains(ext))//TODO wildcards
                {
                    var item = new ListFileInfo()
                    {
                        FullPath = finfo.FullName,
                        ShowFull = fullName,
                        Length = finfo.Length
                    };

                    lbFiles.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Handler for single or double mouse clicks.
        /// </summary>
        /// <param name="e"></param>
        void FileSelected(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if ((SingleClickSelect && e.Clicks == 1) || (!SingleClickSelect && e.Clicks >= 2))
                {
                    if (lbFiles.IndexFromPoint(e.Location) != -1)
                    {
                        var fi = lbFiles.SelectedItem as ListFileInfo;
                        if (fi is not null)
                        {
                            FileSelectedEvent?.Invoke(this, fi.FullPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show file info at mouse position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ListFiles_MouseMove(object? sender, MouseEventArgs e)
        {
            var ind = lbFiles.IndexFromPoint(e.Location);

            if (ind != -1)
            {
                if (ind != _lastListIndex)
                {
                    var info = lbFiles.Items[ind] as ListFileInfo;
                    lblInfo.Text = $"{info!.FullPath}";
                }
            }
            else
            {
                lblInfo.Text = $"";
            }
            _lastListIndex = ind;
        }
        #endregion

        #region Misc privates
        /// <summary>
        /// Context menu handler.
        /// </summary>
        /// <param name="ctl"></param>
        void GetInfo(Control ctl)
        {
            if (ctl == treeView)
            {
                var node = treeView.SelectedNode;
                if (node is not null)
                {
                    var nodeDirInfo = node.Tag as TreeDirInfo;
                    //string s = which == "Path" ? nodeDirInfo!.FullName : nodeDirInfo!.Name;
                    string s = nodeDirInfo!.FullPath;
                    Clipboard.SetText(s);
                }
            }
            else if (ctl == lbFiles)
            {
                ListFileInfo? fi = lbFiles.SelectedItem as ListFileInfo;
                if (fi is not null)
                {
                    //string s = which == "Path" ? fi.FullName : fi.Name;
                    string s = fi.FullPath;
                    Clipboard.SetText(s);
                }
            }
        }
        #endregion

        #region Component Designer generated code
        readonly TreeView treeView = new();
        readonly ListBox lbFiles = new();
        readonly SplitContainer splitContainer = new();
        readonly StatusStrip statusStrip = new();
        readonly ToolStripStatusLabel lblInfo = new();

        /// <summary>Required method for Designer support - do not modify the contents of this method with the code editor. (haha)</summary>
        void InitializeComponent()
        {
            treeView.BorderStyle = BorderStyle.None;
            treeView.Dock = DockStyle.Fill;
            treeView.Name = "treeView";
            treeView.TabIndex = 0;

            lbFiles.BorderStyle = BorderStyle.None;
            lbFiles.Dock = DockStyle.Fill;
            lbFiles.ItemHeight = 20;
            lbFiles.Name = "lbFiles";
            lbFiles.TabIndex = 1;

            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Name = "splitContainer";
            splitContainer.SplitterDistance = 262;
            splitContainer.Panel1.Controls.Add(treeView);
            splitContainer.Panel2.Controls.Add(lbFiles);
            Controls.Add(splitContainer);

            statusStrip.Items.AddRange(new ToolStripItem[] { lblInfo });
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new(600, 26);
            statusStrip.LayoutStyle = ToolStripLayoutStyle.Flow;
            Controls.Add(statusStrip);

            lblInfo.Name = "lblInfo";
        }
        #endregion
    }
}
