using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NotificationMessage
{
    public partial class MessageForm : Form
    {
        public MessageForm()
        {
            InitializeComponent();
            Visible = false;
            Load += MessageForm_Load;
        }

        void MessageForm_Load(object sender, EventArgs e)
        {
            var startInfo = new ProcessStartInfo("iexplore");
            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            startInfo.Arguments = string.Format("-extoff {0}", Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"msg.html"));
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Maximized;
            Process.Start(startInfo);
            Application.Exit();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
