using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebProxy
{
    public partial class MainFrm : Form
    {
        ProxyServer PS = new ProxyServer(IPAddress.Parse("192.168.8.111"),8080);
        Boolean CururentServerState = false;
        public MainFrm()
        {
            InitializeComponent();
            
            PS.ServerStateChanged = ServerStateChanged;
            PS.Start();
        }


        public void ServerStateChanged(Boolean ServerState)
        {
            if (ServerState)
            {
                this.btnServer.Text = "关闭(&C)";
            }
            else
            {
                this.btnServer.Text = "启动(&O)";
            }
            CururentServerState = ServerState;
        }
        private void btnServer_Click(object sender, EventArgs e)
        {
            if (CururentServerState)
            {
                PS.Stop();
            }
            else
            {
                PS.Start();
            }
        }
    }
}
