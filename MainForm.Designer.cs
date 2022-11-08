namespace Ephemera.FileFam
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));

            this.optionsEdit = new Ephemera.NBagOfUis.OptionsEditor();
            this.lv = new System.Windows.Forms.ListView();
            this.txtEdit = new System.Windows.Forms.TextBox();

            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.tvLog = new Ephemera.NBagOfUis.TextViewer();

            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblInfo});
            this.statusStrip.Location = new System.Drawing.Point(0, 616);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1238, 26);
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
            this.tvLog.Location = new System.Drawing.Point(541, 456);
            this.tvLog.MaxText = 5000;
            this.tvLog.Name = "tvLog";
            this.tvLog.Prompt = "> ";
            this.tvLog.Size = new System.Drawing.Size(686, 144);
            this.tvLog.TabIndex = 92;
            this.tvLog.TabStop = false;
            this.tvLog.WordWrap = true;
            // 
            // lv
            // 
            this.lv.Location = new System.Drawing.Point(15, 8);
            this.lv.Name = "lv";
            this.lv.Size = new System.Drawing.Size(825, 427);
            this.lv.TabIndex = 93;
            this.lv.UseCompatibleStateImageBehavior = false;
            // 
            // txtEdit
            // 
            this.txtEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEdit.Location = new System.Drawing.Point(920, 394);
            this.txtEdit.Name = "txtEdit";
            this.txtEdit.Size = new System.Drawing.Size(125, 27);
            this.txtEdit.TabIndex = 94;
            this.txtEdit.Text = "Edit";
            // 
            // optionsEdit
            // 
            this.optionsEdit.AllowEdit = true;
            this.optionsEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.optionsEdit.BackColor = System.Drawing.Color.Azure;
            this.optionsEdit.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.optionsEdit.Location = new System.Drawing.Point(933, 12);
            this.optionsEdit.Name = "optionsEdit";
            this.optionsEdit.Size = new System.Drawing.Size(125, 426);
            this.optionsEdit.TabIndex = 95;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1238, 642);
            this.Controls.Add(this.optionsEdit);
            this.Controls.Add(this.txtEdit);
            this.Controls.Add(this.lv);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.tvLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
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
    }
}