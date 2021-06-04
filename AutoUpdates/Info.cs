using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoUpdates
{
    public partial class Info : Form
    {
        public string Name { get; set; }
        public Info()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
           InstallUpdate.bInstall = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            InstallUpdate.bInstall = false;
            this.Close();
        }

        private void Info_Load(object sender, EventArgs e)
        {
            this.Text = Name;
        }
    }
}
