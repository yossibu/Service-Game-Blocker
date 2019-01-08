using System;
using System.ComponentModel;
using System.Configuration;
using System.Text;
using System.Windows;

namespace ConfigUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;
            DataContext = this;
            InitConfigFile();
        }
        public string Password
        {
            get { return ConvertFromBase64(ConfigSettings.Settings["Password"].Value); }
            set
            {
                ConfigSettings.Settings["Password"].Value = ConvertToBase64(value);
                NotifyPropertyChanged("Password");
                IsDirty = true;
            }
        }
        public string Username
        {
            get { return ConfigSettings.Settings["Username"].Value; }
            set
            {
                ConfigSettings.Settings["Username"].Value = value;
                NotifyPropertyChanged("Username");
                IsDirty = true;
            }
        }

        public string TotalTime
        {
            get { return ConfigSettings.Settings["TotalTime"].Value; }
            set
            {
                
                var isNumeric = int.TryParse(value, out int n);
                if (!isNumeric)
                {
                    MessageBox.Show("Total time is not a number", "Configuraion Editor", MessageBoxButton.OK,MessageBoxImage.Error);
                    return;
                }
                ConfigSettings.Settings["TotalTime"].Value = value;
                NotifyPropertyChanged("TotalTime");
                IsDirty = true;
            }
        }

        public string ProcessName
        {
            get { return ConfigSettings.Settings["ProcessName"].Value; }
            set
            {
                ConfigSettings.Settings["ProcessName"].Value = value;
                NotifyPropertyChanged("ProcessName");
                IsDirty = true;
            }
        }

        public string SendTo
        {
            get { return ConfigSettings.Settings["SendTo"].Value; }
            set
            {
                ConfigSettings.Settings["SendTo"].Value = value;
                NotifyPropertyChanged("SendTo");
                IsDirty = true;
            }
        }

        public bool SendMail
        {
            get { return ConfigSettings.Settings["SendMail"].Value.Equals(Boolean.TrueString,StringComparison.OrdinalIgnoreCase); }
            set
            {
                ConfigSettings.Settings["SendMail"].Value = value.ToString();
                NotifyPropertyChanged("SendMail");
                IsDirty = true;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IsDirty = false;
        }
        public bool IsDirty
        {
            get; set;
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (IsDirty)
            {
                var results = MessageBox.Show("Do you want to save changes?", "Save", MessageBoxButton.YesNoCancel,
                                              MessageBoxImage.Question);

                switch (results)
                {
                    case MessageBoxResult.Yes:
                        SaveChanges();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }
        public AppSettingsSection ConfigSettings { get; set; }

        private Configuration Config { get; set; }

        private void InitConfigFile()
        {
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Windows.Update.Service.exe.config");
            var configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = configPath
            };
            Config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            ConfigSettings = Config.AppSettings;
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void SaveChanges()
        {            
            try
            {
                var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Windows.Update.Service.exe.config");
                Config.Save(ConfigurationSaveMode.Modified);
                System.IO.File.SetLastWriteTime(configPath, DateTime.Now);
                InitConfigFile();
                IsDirty = false;
                MessageBox.Show("Data saved successfully", "Configuraion Editor", MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save configuration changes " + ex.Message, "Error", MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
        #endregion

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }
        private  string ConvertFromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;
            return Encoding.ASCII.GetString(Convert.FromBase64String(base64));
        }        
        private string ConvertToBase64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(value));
        }
    }
}
