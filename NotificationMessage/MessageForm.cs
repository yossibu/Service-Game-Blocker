using System;
using System.Configuration;
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
            var totalTime = GetTotalTime();
            var startInfo = new ProcessStartInfo("iexplore");
            startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            startInfo.Arguments = string.Format("-extoff {0}", Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"msg.html?total="+ totalTime));
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Maximized;
            Process.Start(startInfo);
            Application.Exit();
        }
        private int GetTotalTime()
        {
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Windows.Update.Service.exe.config");
            var configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };
            var config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            var totalTime = int.Parse (config.AppSettings.Settings["TotalTime"].Value)/60;
            return totalTime;
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
