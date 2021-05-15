using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EliteFIPServer {
    public partial class MatricPinEntry : Form {

        public string MatricPin = null;

        public MatricPinEntry() {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e) {
            if (txtMatricPin.Text != null) {
                MatricPin = txtMatricPin.Text;
            }

        }
    }
}
