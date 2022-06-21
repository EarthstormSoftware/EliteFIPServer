
namespace EliteFIPServer {
    partial class Settings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnRevert = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.chkImmediateStart = new System.Windows.Forms.CheckBox();
            this.chkEnableLog = new System.Windows.Forms.CheckBox();
            this.lblActiveClients = new System.Windows.Forms.Label();
            this.dgvActiveClients = new System.Windows.Forms.DataGridView();
            this.chkEnablePanelServer = new System.Windows.Forms.CheckBox();
            this.txtPanelServerPort = new System.Windows.Forms.TextBox();
            this.lblPanelServerPort = new System.Windows.Forms.Label();
            this.lblMatricAPIPort = new System.Windows.Forms.Label();
            this.numMatricApiPort = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActiveClients)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMatricApiPort)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(86, 182);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnRevert
            // 
            this.btnRevert.Location = new System.Drawing.Point(167, 182);
            this.btnRevert.Name = "btnRevert";
            this.btnRevert.Size = new System.Drawing.Size(75, 23);
            this.btnRevert.TabIndex = 7;
            this.btnRevert.Text = "Revert";
            this.btnRevert.UseVisualStyleBackColor = true;
            this.btnRevert.Click += new System.EventHandler(this.btnRevert_Click);
            // 
            // btnSave
            // 
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSave.Location = new System.Drawing.Point(602, 182);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // chkImmediateStart
            // 
            this.chkImmediateStart.AutoSize = true;
            this.chkImmediateStart.Location = new System.Drawing.Point(86, 41);
            this.chkImmediateStart.Name = "chkImmediateStart";
            this.chkImmediateStart.Size = new System.Drawing.Size(110, 19);
            this.chkImmediateStart.TabIndex = 4;
            this.chkImmediateStart.Text = "Immediate Start";
            this.chkImmediateStart.UseVisualStyleBackColor = true;
            // 
            // chkEnableLog
            // 
            this.chkEnableLog.AutoSize = true;
            this.chkEnableLog.Location = new System.Drawing.Point(86, 66);
            this.chkEnableLog.Name = "chkEnableLog";
            this.chkEnableLog.Size = new System.Drawing.Size(84, 19);
            this.chkEnableLog.TabIndex = 5;
            this.chkEnableLog.Text = "Enable Log";
            this.chkEnableLog.UseVisualStyleBackColor = true;
            // 
            // lblActiveClients
            // 
            this.lblActiveClients.AutoSize = true;
            this.lblActiveClients.Location = new System.Drawing.Point(86, 251);
            this.lblActiveClients.Name = "lblActiveClients";
            this.lblActiveClients.Size = new System.Drawing.Size(79, 15);
            this.lblActiveClients.TabIndex = 12;
            this.lblActiveClients.Text = "Active Clients";
            // 
            // dgvActiveClients
            // 
            this.dgvActiveClients.AllowUserToAddRows = false;
            this.dgvActiveClients.AllowUserToDeleteRows = false;
            this.dgvActiveClients.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgvActiveClients.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvActiveClients.Location = new System.Drawing.Point(180, 251);
            this.dgvActiveClients.Name = "dgvActiveClients";
            this.dgvActiveClients.ReadOnly = true;
            this.dgvActiveClients.RowTemplate.Height = 25;
            this.dgvActiveClients.ShowEditingIcon = false;
            this.dgvActiveClients.Size = new System.Drawing.Size(509, 136);
            this.dgvActiveClients.TabIndex = 13;
            // 
            // chkEnablePanelServer
            // 
            this.chkEnablePanelServer.AutoSize = true;
            this.chkEnablePanelServer.Location = new System.Drawing.Point(86, 91);
            this.chkEnablePanelServer.Name = "chkEnablePanelServer";
            this.chkEnablePanelServer.Size = new System.Drawing.Size(128, 19);
            this.chkEnablePanelServer.TabIndex = 15;
            this.chkEnablePanelServer.Text = "Enable Panel Server";
            this.chkEnablePanelServer.UseVisualStyleBackColor = true;
            // 
            // txtPanelServerPort
            // 
            this.txtPanelServerPort.Location = new System.Drawing.Point(270, 89);
            this.txtPanelServerPort.MaxLength = 5;
            this.txtPanelServerPort.Name = "txtPanelServerPort";
            this.txtPanelServerPort.Size = new System.Drawing.Size(55, 23);
            this.txtPanelServerPort.TabIndex = 16;
            // 
            // lblPanelServerPort
            // 
            this.lblPanelServerPort.AutoSize = true;
            this.lblPanelServerPort.Location = new System.Drawing.Point(232, 91);
            this.lblPanelServerPort.Name = "lblPanelServerPort";
            this.lblPanelServerPort.Size = new System.Drawing.Size(32, 15);
            this.lblPanelServerPort.TabIndex = 17;
            this.lblPanelServerPort.Text = "Port:";
            // 
            // lblMatricAPIPort
            // 
            this.lblMatricAPIPort.AutoSize = true;
            this.lblMatricAPIPort.Location = new System.Drawing.Point(86, 127);
            this.lblMatricAPIPort.Name = "lblMatricAPIPort";
            this.lblMatricAPIPort.Size = new System.Drawing.Size(151, 15);
            this.lblMatricAPIPort.TabIndex = 18;
            this.lblMatricAPIPort.Text = "Matric Integration API Port:";
            // 
            // numMatricApiPort
            // 
            this.numMatricApiPort.Location = new System.Drawing.Point(243, 125);
            this.numMatricApiPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numMatricApiPort.Name = "numMatricApiPort";
            this.numMatricApiPort.Size = new System.Drawing.Size(85, 23);
            this.numMatricApiPort.TabIndex = 19;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 491);
            this.Controls.Add(this.numMatricApiPort);
            this.Controls.Add(this.lblMatricAPIPort);
            this.Controls.Add(this.lblPanelServerPort);
            this.Controls.Add(this.txtPanelServerPort);
            this.Controls.Add(this.chkEnablePanelServer);
            this.Controls.Add(this.dgvActiveClients);
            this.Controls.Add(this.lblActiveClients);
            this.Controls.Add(this.chkEnableLog);
            this.Controls.Add(this.chkImmediateStart);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnRevert);
            this.Controls.Add(this.btnCancel);
            this.Name = "Settings";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvActiveClients)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMatricApiPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRevert;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox chkImmediateStart;
        private System.Windows.Forms.CheckBox chkEnableLog;
        private System.Windows.Forms.Label lblActiveClients;
        private System.Windows.Forms.DataGridView dgvActiveClients;
        private CheckBox chkEnablePanelServer;
        private TextBox txtPanelServerPort;
        private Label lblPanelServerPort;
        private Label lblMatricAPIPort;
        private NumericUpDown numMatricApiPort;
    }
}