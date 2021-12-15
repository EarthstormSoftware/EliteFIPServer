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
            lblServerState.Text = State.Stopped.ToString();
            ServerCore = new ServerCore(this);
        }

        private void btnStartStop_Click(object sender, EventArgs e) {

            if (ServerCore.GetState() == State.Stopped) {
                ServerCore.Start();
                btnStartStop.Text = "Stop";

            } else if (ServerCore.GetState() == State.Started) {
                ServerCore.Stop();
                btnStartStop.Text = "Start";
            }
        }


        public void UpdateServerStatus(State serverState) {
            if (serverState == State.Stopped) {
                UpdateStatusText(State.Stopped.ToString());
                UpdateButtonText("Start");
            } else if (serverState == State.Started) {
                UpdateStatusText(State.Started.ToString());
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

        public State GetServerState() {
            return ServerCore.GetState();
        }

        public MatricIntegration GetMatricApi() {
            return ServerCore.GetMatricApi();
        }
    }
}
