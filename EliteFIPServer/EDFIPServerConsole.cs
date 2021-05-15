using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EliteFIPServer.Logging;

namespace EliteFIPServer {

    public partial class EDFIPServerConsole : Form {    

        private delegate void SafeCallDelegate(string text);
        private ServerCore ServerCore;

        public EDFIPServerConsole() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            
            if (Properties.Settings.Default.EnableLog == true) {
                Log.LogEnabled(true);
            } else {
                Log.LogEnabled(false);
            }
            lblServerState.Text = CoreState.Stopped.ToString();            
            ServerCore = new ServerCore(this);
        }

        private void btnStartStop_Click(object sender, EventArgs e) {

            if (ServerCore.GetState() == CoreState.Stopped) {
                ServerCore.Start();
                btnStartStop.Text = "Stop";

            } else if (ServerCore.GetState() == CoreState.Started) {
                ServerCore.Stop();
                btnStartStop.Text = "Start";
            }
        }


        public void UpdateServerStatus(CoreState serverState) {
            if (serverState == CoreState.Stopped) {
                UpdateStatusText(CoreState.Stopped.ToString());
                UpdateButtonText("Start");
            } else if (serverState == CoreState.Started) {
                UpdateStatusText(CoreState.Started.ToString());
                UpdateButtonText("Stop");
            }
        }

        public void UpdateStatusText(string text) {
            if (lblServerState.InvokeRequired) {                
                var d = new SafeCallDelegate(UpdateStatusText);
                lblServerState.Invoke(d, new object[] { text });
            } else {
                lblServerState.Text = text;
            }
        }

        public void UpdateButtonText(string text) {
            if (btnStartStop.InvokeRequired) {
                var d = new SafeCallDelegate(UpdateButtonText);
                btnStartStop.Invoke(d, new object[] { text });
            } else {                
                btnStartStop.Text = text;
            }
        }

        private void mnuMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            Settings settings = new Settings(this);
            DialogResult dialogresult = settings.ShowDialog();
            settings.Dispose();
        }

        public CoreState GetServerState() {
            return ServerCore.GetState();
        }

        public MatricIntegration GetMatricApi() {
            return ServerCore.GetMatricApi();
        }
    }
}
