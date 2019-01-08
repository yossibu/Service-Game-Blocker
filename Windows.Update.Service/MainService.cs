using System;
using System.ServiceProcess;

namespace Windows.Update.Service
{
    public partial class MainService : ServiceBase
    {
        private ServiceManager _manager;
        public MainService()
        {
            InitializeComponent();
            CanPauseAndContinue = true;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        protected override void OnStart(string[] args)
        {
            Common.WriteToLog("Service started");
            if (_manager == null)
            {
                _manager = new ServiceManager();
            }
            System.Threading.ThreadPool.QueueUserWorkItem(_manager.OnStart, null);
        }

        protected override void OnStop()
        {
            Common.WriteToLog("Service stopped");
            _manager.OnStop();
        }
        protected override void OnContinue()
        {
            Common.WriteToLog("Service continued");
            _manager.OnContinue();
        }
        private void OnReset()
        {
            Common.WriteToLog("Service reset");
            OnStop();
            OnStart(null);
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Common.WriteErrorToLog(ex);
        }
#if DEBUG
        //static void Main()
        //{
        //    var servicesToRun = new ServiceBase[]
        //  {
        //      new MainService()
        //  };
        //    Run(servicesToRun);
        //}
        [STAThread]
        static void Main()
        {
            try
            {
                var service = new MainService();
                service.OnStart(null);
                var exitLoop = false;
                // wait for a key to be pressed
                while (true)
                {
                    var line = Console.ReadLine();
                    line = line ?? "";

                    switch (line.ToLower())
                    {
                        case "r":
                            service.OnReset();
                            break;
                        case "q":
                            service.OnStop();
                            exitLoop = true;
                            break;
                    }
                    if (exitLoop)
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

#else
		static void Main()
		{
		    var servicesToRun = new ServiceBase[]
		        {
		            new MainService()
		        };
		    Run(servicesToRun);
		}
#endif
    }
}
