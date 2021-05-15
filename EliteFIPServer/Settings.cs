using System;
using System.Windows.Forms;
using Matric.Integration;
using EliteFIPServer.Logging;
using System.Collections.Generic;
using System.ComponentModel;

namespace EliteFIPServer {
    public partial class Settings : Form {

        EDFIPServerConsole Caller;
        MatricIntegration matricconn = new MatricIntegration();


        public Settings() {
            InitializeComponent();
        }

        public Settings(Form caller) {
            InitializeComponent();
            this.Caller = caller as EDFIPServerConsole;
        }


        private void btnSave_Click(object sender, EventArgs e) {
            Log.Instance.Info("Saving settings");
            Properties.Settings.Default.MatricPin = txtMatricPin.Text;
            Properties.Settings.Default.MatricClient = txtMatricClient.Text;
            Properties.Settings.Default.ImmediateStart = chkImmediateStart.Checked;
            Properties.Settings.Default.EnableLog = chkEnableLog.Checked;            
            Properties.Settings.Default.Save();

            // Update log setting
            if (Properties.Settings.Default.EnableLog == true) {
                Log.LogEnabled(true);
            } else {
                Log.LogEnabled(false);
            }

            // Update running Client Id
            Caller.GetMatricApi().SetClientId(Properties.Settings.Default.MatricClient);


        }

        private void btnRevert_Click(object sender, EventArgs e) {
            loadSettings();
        }

        private void Settings_Load(object sender, EventArgs e) {
            loadSettings();
        }

        private void loadSettings() {
            txtMatricPin.Text = Properties.Settings.Default.MatricPin;
            txtMatricClient.Text = Properties.Settings.Default.MatricClient;
            chkImmediateStart.Checked = Properties.Settings.Default.ImmediateStart;
            chkEnableLog.Checked = Properties.Settings.Default.EnableLog;

            if (Caller.GetServerState() == CoreState.Started) {
                BindingList<ClientInfo> activeClients = new BindingList<ClientInfo>(Caller.GetMatricApi().GetConnectClients());
                BindingSource dgvSource = new BindingSource(activeClients, null);
                dgvActiveClients.DataSource = dgvSource;
            } 
            
        }

        private void txtMatricPin_TextChanged(object sender, EventArgs e) {

        }
    }
}
