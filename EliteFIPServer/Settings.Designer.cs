
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
            this.lblMatricPin = new System.Windows.Forms.Label();
            this.lblMatricClient = new System.Windows.Forms.Label();
            this.txtMatricPin = new System.Windows.Forms.TextBox();
            this.chkImmediateStart = new System.Windows.Forms.CheckBox();
            this.chkEnableLog = new System.Windows.Forms.CheckBox();
            this.txtMatricClient = new System.Windows.Forms.TextBox();
            this.lblActiveClients = new System.Windows.Forms.Label();
            this.dgvActiveClients = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvActiveClients)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(180, 426);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnRevert
            // 
            this.btnRevert.Location = new System.Drawing.Point(261, 426);
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
            this.btnSave.Location = new System.Drawing.Point(622, 426);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblMatricPin
            // 
            this.lblMatricPin.AutoSize = true;
            this.lblMatricPin.Location = new System.Drawing.Point(86, 42);
            this.lblMatricPin.Name = "lblMatricPin";
            this.lblMatricPin.Size = new System.Drawing.Size(61, 15);
            this.lblMatricPin.TabIndex = 8;
            this.lblMatricPin.Text = "Matric Pin";
            // 
            // lblMatricClient
            // 
            this.lblMatricClient.AutoSize = true;
            this.lblMatricClient.Location = new System.Drawing.Point(86, 77);
            this.lblMatricClient.Name = "lblMatricClient";
            this.lblMatricClient.Size = new System.Drawing.Size(88, 15);
            this.lblMatricClient.TabIndex = 9;
            this.lblMatricClient.Text = "Matric Client Id";
            // 
            // txtMatricPin
            // 
            this.txtMatricPin.Location = new System.Drawing.Point(180, 39);
            this.txtMatricPin.MaxLength = 4;
            this.txtMatricPin.Name = "txtMatricPin";
            this.txtMatricPin.Size = new System.Drawing.Size(57, 23);
            this.txtMatricPin.TabIndex = 2;
            this.txtMatricPin.TextChanged += new System.EventHandler(this.txtMatricPin_TextChanged);
            // 
            // chkImmediateStart
            // 
            this.chkImmediateStart.AutoSize = true;
            this.chkImmediateStart.Location = new System.Drawing.Point(180, 330);
            this.chkImmediateStart.Name = "chkImmediateStart";
            this.chkImmediateStart.Size = new System.Drawing.Size(110, 19);
            this.chkImmediateStart.TabIndex = 4;
            this.chkImmediateStart.Text = "Immediate Start";
            this.chkImmediateStart.UseVisualStyleBackColor = true;
            // 
            // chkEnableLog
            // 
            this.chkEnableLog.AutoSize = true;
            this.chkEnableLog.Location = new System.Drawing.Point(180, 355);
            this.chkEnableLog.Name = "chkEnableLog";
            this.chkEnableLog.Size = new System.Drawing.Size(84, 19);
            this.chkEnableLog.TabIndex = 5;
            this.chkEnableLog.Text = "Enable Log";
            this.chkEnableLog.UseVisualStyleBackColor = true;
            // 
            // txtMatricClient
            // 
            this.txtMatricClient.Location = new System.Drawing.Point(180, 74);
            this.txtMatricClient.Name = "txtMatricClient";
            this.txtMatricClient.Size = new System.Drawing.Size(350, 23);
            this.txtMatricClient.TabIndex = 3;
            // 
            // lblActiveClients
            // 
            this.lblActiveClients.AutoSize = true;
            this.lblActiveClients.Location = new System.Drawing.Point(86, 127);
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
            this.dgvActiveClients.Location = new System.Drawing.Point(180, 127);
            this.dgvActiveClients.Name = "dgvActiveClients";
            this.dgvActiveClients.ReadOnly = true;
            this.dgvActiveClients.RowTemplate.Height = 25;
            this.dgvActiveClients.ShowEditingIcon = false;
            this.dgvActiveClients.Size = new System.Drawing.Size(517, 180);
            this.dgvActiveClients.TabIndex = 13;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(805, 491);
            this.Controls.Add(this.dgvActiveClients);
            this.Controls.Add(this.lblActiveClients);
            this.Controls.Add(this.txtMatricClient);
            this.Controls.Add(this.chkEnableLog);
            this.Controls.Add(this.chkImmediateStart);
            this.Controls.Add(this.txtMatricPin);
            this.Controls.Add(this.lblMatricClient);
            this.Controls.Add(this.lblMatricPin);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnRevert);
            this.Controls.Add(this.btnCancel);
            this.Name = "Settings";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvActiveClients)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRevert;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblMatricPin;
        private System.Windows.Forms.Label lblMatricClient;
        private System.Windows.Forms.TextBox txtMatricPin;
        private System.Windows.Forms.CheckBox chkImmediateStart;
        private System.Windows.Forms.CheckBox chkEnableLog;
        private System.Windows.Forms.TextBox txtMatricClient;
        private System.Windows.Forms.Label lblActiveClients;
        private System.Windows.Forms.DataGridView dgvActiveClients;
    }
}