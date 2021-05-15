namespace EliteFIPServer
{
    partial class EDFIPServerConsole
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStartStop = new System.Windows.Forms.Button();
            this.lblServerStatusLabel = new System.Windows.Forms.Label();
            this.lblServerState = new System.Windows.Forms.Label();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.mnuitemSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(122, 108);
            this.btnStartStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(169, 59);
            this.btnStartStop.TabIndex = 0;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // lblServerStatusLabel
            // 
            this.lblServerStatusLabel.AutoSize = true;
            this.lblServerStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblServerStatusLabel.Location = new System.Drawing.Point(82, 65);
            this.lblServerStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServerStatusLabel.Name = "lblServerStatusLabel";
            this.lblServerStatusLabel.Size = new System.Drawing.Size(129, 20);
            this.lblServerStatusLabel.TabIndex = 1;
            this.lblServerStatusLabel.Text = "Server Status: ";
            // 
            // lblServerState
            // 
            this.lblServerState.AutoSize = true;
            this.lblServerState.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblServerState.Location = new System.Drawing.Point(219, 65);
            this.lblServerState.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblServerState.Name = "lblServerState";
            this.lblServerState.Size = new System.Drawing.Size(102, 20);
            this.lblServerState.TabIndex = 2;
            this.lblServerState.Text = "DummyState";
            // 
            // mnuMain
            // 
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuitemSettings});
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(414, 24);
            this.mnuMain.TabIndex = 3;
            this.mnuMain.Text = "Main Menu";
            this.mnuMain.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.mnuMain_ItemClicked);
            // 
            // mnuitemSettings
            // 
            this.mnuitemSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.mnuitemSettings.Name = "mnuitemSettings";
            this.mnuitemSettings.Size = new System.Drawing.Size(61, 20);
            this.mnuitemSettings.Text = "Settings";
            // 
            // EDFIPServerConsole
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(414, 196);
            this.Controls.Add(this.lblServerState);
            this.Controls.Add(this.lblServerStatusLabel);
            this.Controls.Add(this.btnStartStop);
            this.Controls.Add(this.mnuMain);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(430, 235);
            this.MinimumSize = new System.Drawing.Size(430, 235);
            this.Name = "EDFIPServerConsole";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Elite FIP Server Console";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Label lblServerStatusLabel;
        private System.Windows.Forms.Label lblServerState;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem mnuitemSettings;
    }
}

