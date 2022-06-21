using EliteFIPServer.Logging;
using Matric.Integration;
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
            Properties.Settings.Default.ImmediateStart = chkImmediateStart.Checked;
            Properties.Settings.Default.EnableLog = chkEnableLog.Checked;
            Properties.Settings.Default.EnablePanelServer = chkEnablePanelServer.Checked;   
            Properties.Settings.Default.PanelServerPort = txtPanelServerPort.Text;
            Properties.Settings.Default.MatricApiPort = (int)numMatricApiPort.Value;
            Properties.Settings.Default.Save();

            // Update log setting
            if (Properties.Settings.Default.EnableLog == true) {
                Log.LogEnabled(true);
            } else {
                Log.LogEnabled(false);
            }

            Caller.GetMatricApi().Connect(Properties.Settings.Default.MatricApiPort);

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
            chkEnablePanelServer.Checked = Properties.Settings.Default.EnablePanelServer;
            txtPanelServerPort.Text = Properties.Settings.Default.PanelServerPort;
            numMatricApiPort.Value = Properties.Settings.Default.MatricApiPort;

            if (Caller.GetServerState() == State.Started) {
                BindingList<ClientInfo> activeClients = new BindingList<ClientInfo>(Caller.GetMatricApi().GetConnectedClients());
                BindingSource dgvSource = new BindingSource(activeClients, null);
                dgvActiveClients.DataSource = dgvSource;
            }

        }
    }
}
