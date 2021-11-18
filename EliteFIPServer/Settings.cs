using EliteFIPServer.Logging;
using Matric.Integration;
using System;
using System.ComponentModel;
using System.Windows.Forms;

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
            Properties.Settings.Default.ImmediateStart = chkImmediateStart.Checked;
            Properties.Settings.Default.EnableLog = chkEnableLog.Checked;
            Properties.Settings.Default.Save();

            // Update log setting
            if (Properties.Settings.Default.EnableLog == true) {
                Log.LogEnabled(true);
            } else {
                Log.LogEnabled(false);
            }

            Caller.GetMatricApi().Connect();

        }

        private void btnRevert_Click(object sender, EventArgs e) {
            loadSettings();
        }

        private void Settings_Load(object sender, EventArgs e) {
            loadSettings();
        }

        private void loadSettings() {
            chkImmediateStart.Checked = Properties.Settings.Default.ImmediateStart;
            chkEnableLog.Checked = Properties.Settings.Default.EnableLog;

            if (Caller.GetServerState() == CoreState.Started) {
                BindingList<ClientInfo> activeClients = new BindingList<ClientInfo>(Caller.GetMatricApi().GetConnectedClients());
                BindingSource dgvSource = new BindingSource(activeClients, null);
                dgvActiveClients.DataSource = dgvSource;
            }

        }
    }
}
