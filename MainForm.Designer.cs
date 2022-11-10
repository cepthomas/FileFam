namespace Ephemera.FileFam
{
    partial class FileFam
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileFam));
            this.optionsEdit = new Ephemera.NBagOfUis.OptionsEditor();
            this.lv = new System.Windows.Forms.ListView();
            this.txtEdit = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.tvLog = new Ephemera.NBagOfUis.TextViewer();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.FileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.NewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.RecentMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveAsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SettingsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.DebugMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // optionsEdit
            // 
            this.optionsEdit.AllowEdit = true;
            this.optionsEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.optionsEdit.BackColor = System.Drawing.Color.Azure;
            this.optionsEdit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.optionsEdit.Location = new System.Drawing.Point(1083, 35);
            this.optionsEdit.Name = "optionsEdit";
            this.optionsEdit.Size = new System.Drawing.Size(125, 438);
            this.optionsEdit.TabIndex = 95;
            // 
            // lv
            // 
            this.lv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lv.Location = new System.Drawing.Point(12, 35);
            this.lv.Name = "lv";
            this.lv.Size = new System.Drawing.Size(1065, 438);
            this.lv.TabIndex = 93;
            this.lv.UseCompatibleStateImageBehavior = false;
            // 
            // txtEdit
            // 
            this.txtEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEdit.Location = new System.Drawing.Point(1083, 536);
            this.txtEdit.Name = "txtEdit";
            this.txtEdit.Size = new System.Drawing.Size(125, 27);
            this.txtEdit.TabIndex = 94;
            this.txtEdit.Text = "Edit";
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblInfo});
            this.statusStrip.Location = new System.Drawing.Point(0, 613);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1220, 26);
            this.statusStrip.TabIndex = 91;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblInfo
            // 
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(45, 20);
            this.lblInfo.Text = "Hello";
            // 
            // tvLog
            // 
            this.tvLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tvLog.Location = new System.Drawing.Point(12, 479);
            this.tvLog.MaxText = 5000;
            this.tvLog.Name = "tvLog";
            this.tvLog.Prompt = "> ";
            this.tvLog.Size = new System.Drawing.Size(1065, 131);
            this.tvLog.TabIndex = 92;
            this.tvLog.TabStop = false;
            this.tvLog.WordWrap = true;
            // 
            // menuStrip
            // 
            this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenu,
            this.ToolsMenu});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1220, 28);
            this.menuStrip.TabIndex = 97;
            this.menuStrip.Text = "menuStrip1";
            // 
            // FileMenu
            // 
            this.FileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewMenu,
            this.OpenMenu,
            this.RecentMenu,
            this.SaveMenu,
            this.SaveAsMenu});
            this.FileMenu.Name = "FileMenu";
            this.FileMenu.Size = new System.Drawing.Size(46, 24);
            this.FileMenu.Text = "File";
            // 
            // NewMenu
            // 
            this.NewMenu.Name = "NewMenu";
            this.NewMenu.Size = new System.Drawing.Size(141, 26);
            this.NewMenu.Text = "New";
            // 
            // OpenMenu
            // 
            this.OpenMenu.Name = "OpenMenu";
            this.OpenMenu.Size = new System.Drawing.Size(141, 26);
            this.OpenMenu.Text = "Open";
            // 
            // RecentMenu
            // 
            this.RecentMenu.Name = "RecentMenu";
            this.RecentMenu.Size = new System.Drawing.Size(141, 26);
            this.RecentMenu.Text = "Recent";
            // 
            // SaveMenu
            // 
            this.SaveMenu.Name = "SaveMenu";
            this.SaveMenu.Size = new System.Drawing.Size(141, 26);
            this.SaveMenu.Text = "Save";
            // 
            // SaveAsMenu
            // 
            this.SaveAsMenu.Name = "SaveAsMenu";
            this.SaveAsMenu.Size = new System.Drawing.Size(141, 26);
            this.SaveAsMenu.Text = "Save as";
            // 
            // ToolsMenu
            // 
            this.ToolsMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SettingsMenu,
            this.AboutMenu,
            this.DebugMenu});
            this.ToolsMenu.Name = "ToolsMenu";
            this.ToolsMenu.Size = new System.Drawing.Size(58, 24);
            this.ToolsMenu.Text = "Tools";
            // 
            // SettingsMenu
            // 
            this.SettingsMenu.Name = "SettingsMenu";
            this.SettingsMenu.Size = new System.Drawing.Size(145, 26);
            this.SettingsMenu.Text = "Settings";
            // 
            // AboutMenu
            // 
            this.AboutMenu.Name = "AboutMenu";
            this.AboutMenu.Size = new System.Drawing.Size(145, 26);
            this.AboutMenu.Text = "About";
            // 
            // DebugMenu
            // 
            this.DebugMenu.Name = "DebugMenu";
            this.DebugMenu.Size = new System.Drawing.Size(145, 26);
            this.DebugMenu.Text = "Fake";
            // 
            // FileFam
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 639);
            this.Controls.Add(this.optionsEdit);
            this.Controls.Add(this.txtEdit);
            this.Controls.Add(this.lv);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.tvLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.Name = "FileFam";
            this.Text = "FileFam";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblInfo;
        private NBagOfUis.TextViewer tvLog;

        private System.Windows.Forms.ListView lv;
        private System.Windows.Forms.TextBox txtEdit;
        private Ephemera.NBagOfUis.OptionsEditor optionsEdit;
        private System.Windows.Forms.MenuStrip menuStrip;

        private System.Windows.Forms.ToolStripMenuItem FileMenu;
        private System.Windows.Forms.ToolStripMenuItem ToolsMenu;

        private System.Windows.Forms.ToolStripMenuItem OpenMenu;
        private System.Windows.Forms.ToolStripMenuItem SettingsMenu;
        private System.Windows.Forms.ToolStripMenuItem AboutMenu;
        private System.Windows.Forms.ToolStripMenuItem DebugMenu;
        private System.Windows.Forms.ToolStripMenuItem SaveMenu;
        private System.Windows.Forms.ToolStripMenuItem SaveAsMenu;
        private System.Windows.Forms.ToolStripMenuItem RecentMenu;
        private System.Windows.Forms.ToolStripMenuItem NewMenu;
    }
}