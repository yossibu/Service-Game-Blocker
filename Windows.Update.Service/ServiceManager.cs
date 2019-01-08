using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Timers;

namespace Windows.Update.Service
{
    internal class ServiceManager
    {
        private Timer _eventTimer;
        private Timer _resetTimer;
        private DateTime _eventTimerTime = DateTime.Now;
        private Timer _eventKillTimer;
        private ManagementEventWatcher _processStartEvent;
        private ManagementEventWatcher _processStopEvent;
        private readonly List<int> _processToKill = new List<int>();
        private int _totalMinutes = -1;

        protected void ShowInfoMessage()
        {           
            try
            {
                ProcessExtensions.StartProcessAsCurrentUser(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotificationMessage.exe"));
            }
            catch (Exception ex)
            {
                Common.WriteErrorToLog(ex);
            }
        }

        internal ServiceManager()
        {
            ProcessesToCheck = new ConcurrentDictionary<string, ProcessDetails>();
            _resetTimer = new Timer();
            _resetTimer.Elapsed += _resetTimer_Elapsed;
            _resetTimer.Interval = 60000;
            _resetTimer.Enabled = true;
            _resetTimer.Start();
            SetConfigFile();           
        }

        private void _resetTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                DateTime resetTime = Convert.ToDateTime("01:00");
                TimeSpan t1 = resetTime.Subtract(resetTime.Date);
                TimeSpan t2 = DateTime.Now.TimeOfDay;
                if ((t1.Hours == t2.Hours) && (t1.Minutes == t2.Minutes))
                {
                    _processStartEvent.Stop();
                    _processStopEvent.Stop();
                    _eventTimer.Stop();
                    _eventKillTimer.Stop();
                    Common.DeleteDataFile();
                    ProcessesToCheck.Clear();
                    _processStartEvent.Start();
                    _processStopEvent.Start();
                    _eventTimer.Start();
                    _eventKillTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Common.WriteErrorToLog(ex);
            }
        }

        internal void OnStart(object o)
        {
            OnStart();
        }
        internal void OnStart()
        {
            SendMailNotification.SendMail("Fortnite service checker started", "");
            StartProcessListener();
        }
        internal void OnStop()
        {
            SendMailNotification.SendMail("Fortnite service checker stopped", "");
            UpdateLocalStorage();
            _processStartEvent.Stop();
            _processStopEvent.Stop();
            _processStartEvent.EventArrived -= processStartEvent_EventArrived;
            _processStopEvent.EventArrived -= processStopEvent_EventArrived;
            StopTimers();
        }
        internal void OnPause()
        {
            UpdateLocalStorage();
            _processStartEvent.Stop();
            _processStopEvent.Stop();
            _eventTimer.Stop();
            _eventKillTimer.Stop();
        }
        internal void OnContinue()
        {
            _processStartEvent.Start();
            _processStopEvent.Start();
            _eventTimer.Start();
        }
        private void StartTimers()
        {
            SetConfigFile();
            _eventTimer = new Timer();
            _eventTimer.Elapsed += OnTimerEvent;
            _eventTimer.Interval = 1000 * 60 * 5; // every minute check if process alive
            _eventTimer.Enabled = true;

            _eventKillTimer = new Timer();
            _eventKillTimer.Elapsed += _eventKillTimer_Elapsed;
            _eventKillTimer.Interval = 1000 * 60 * 5; // 5 minutes timer killer
            _eventKillTimer.Enabled = true;
        }

        private void _eventKillTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _eventKillTimer.Stop();
            if (_processToKill.Count > 0)
            {
                foreach (var proc in _processToKill)
                {
                    var procName = "";
                    try
                    {
                        var process = Process.GetProcessById(proc);
                        if (process != null)
                        {
                            procName = process.ProcessName;
                            process.Kill();
                        }

                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Common.WriteErrorToLog(ex);
                    }
                    var msg = string.Format("Process name [{0}]  id:{1} killed",
                                            procName, proc);
                    SendMailNotification.SendMail(msg, "");
                    Common.WriteToLog(msg);
                    ProcessDetails processDetails;
                    ProcessesToCheck.TryRemove(procName, out processDetails);
                    Common.WriteToLog(string.Format("Process: {0} removed from checking list", proc));

                    // kill IE to lcose all messages
                    var ieProcs = Process.GetProcessesByName("IEXPLORE");
                    foreach (Process ieProc in ieProcs)
                    {
                        if (ieProc.MainWindowTitle.IndexOf("msg.html") > -1)
                            ieProc.Kill();
                    }
                }
                _processToKill.Clear();
            }
            _eventKillTimer.Start();
        }
        private void StopTimers()
        {
            _eventTimer.Stop();
            _eventTimer.Elapsed -= OnTimerEvent;
            _eventTimer = null;

            _eventKillTimer.Stop();
            _eventKillTimer.Elapsed -= OnTimerEvent;
            _eventKillTimer = null;

        }
        private void OnTimerEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                _eventTimer.Stop();
                Common.WriteToLog(string.Format("Checking games... next check: {0}", DateTime.Now.AddMinutes(1)));
                
                if (e.SignalTime >= _eventTimerTime && ProcessesToCheck.Count > 0)
                {
                    foreach (var processDetailse in ProcessesToCheck)
                    {
                        var process = GetProcess(processDetailse.Value.Pid);
                        if (process != null)
                        {
                            var totalTime = DateTime.Now - process.StartTime;
                            Console.WriteLine("Total Time : {0}", totalTime);
                            UpdateProcessTimeStamp(processDetailse.Value.Name, false, true);
                            var aggrigateTime = ProcessesToCheck.Sum(p => p.Value.TotalTime);
                            if (aggrigateTime >= AllowedTotalTime)
                            {                                
                                if (!_processToKill.Contains(processDetailse.Value.Pid))
                                {
                                    Common.WriteToLog(string.Format("Process exceeded allowed time : {0} will close at {1}", aggrigateTime, DateTime.Now.AddMinutes(5)));
                                    ShowInfoMessage();
                                    _processToKill.Add(processDetailse.Value.Pid);
                                    _eventKillTimer.Start();
                                }
                            }
                        }
                    }
                    _eventTimerTime = DateTime.Now.AddMinutes(1);
                }
               
            }
            catch (Exception ex)
            {
                Common.WriteToLog(ex.ToString());
            }
            finally
            {
                _eventTimer.Start();
            }
        }
        
        private void ProcessSearch()
        {
            var procList = Process.GetProcesses();
            var processes = new List<Process>(procList);
            var processesToFind = Common.AppSettings.Settings["ProcessName"].Value.Split(';');
            foreach (var procName in processesToFind)
            {                
                var processesExist = processes.FindAll(p => p.ProcessName.Equals(procName.Replace(".exe",""),StringComparison.InvariantCultureIgnoreCase));
                if (processesExist.Any())
                {
                    foreach (var proc in processesExist)
                    {
                        var processName = proc.ProcessName;
                        var processId = proc.Id;
                        ProcessesToCheck.TryGetValue(processName, out ProcessDetails processDetails);
                        if (processDetails == null)
                        {
                            processDetails = new ProcessDetails { Name = processName, Pid = processId, StartTime = DateTime.Now };
                        }
                        //   var processDetails = new ProcessDetails { Name = processName, Pid = processId, StartTime = DateTime.Now };
                        var totalTime = processDetails.TotalTime;
                        Console.WriteLine("Total Time: {0} Allowed Total Time:", totalTime, AllowedTotalTime);
                        if (totalTime >= AllowedTotalTime)
                        {
                            // not allowed to continue playing today..
                            if (!_processToKill.Contains(processDetails.Pid))
                            {
                                Common.WriteToLog(string.Format("Process exceeded allowed time : {0} will close at {1}", totalTime, DateTime.Now.AddMinutes(5)));
                                ShowInfoMessage();
                                _processToKill.Add(processDetails.Pid);
                               // proc.Kill();
                            }
                        }
                        else
                        {
                            Common.WriteToLog(string.Format("Process [{0}] is currently running | ID: {1}", processName, processId));
                            ProcessesToCheck.AddOrUpdate(processName, processDetails, (newKey, oldValue) => processDetails);
                        }                        
                    }
                }
                else
                {
                    Common.WriteToLog("process not found at startup");
                }                
            }
            
        }
                
        private int AllowedTotalTime
        {
            get
            {
                if (_totalMinutes == -1)
                {
                    var timeAsText = Common.AppSettings.Settings["TotalTime"].Value;
                    int totalMinutes;
                    _totalMinutes = int.TryParse(timeAsText, out totalMinutes) ? totalMinutes : 3*60; // set default 3 hours if invalid number in config
                }
                return _totalMinutes;
            }
        }
        
        private void SetConfigFile()
        {
            Common.AppSettings = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetEntryAssembly().Location).AppSettings;

        }
        
        private Process GetProcess(int pId)
        {
            Process proc = null;
            try
            {
                proc = Process.GetProcessById(pId);
            }
            catch (ArgumentException argEx)
            {
                Common.WriteToLog(argEx.Message);
            }
            catch (Exception ex)
            {
                Common.WriteToLog(ex.Message);
            }
            return proc;
        }
        
        private ConcurrentDictionary<string,ProcessDetails> ProcessesToCheck
        {
            get; set;
        }

        private void StartProcessListener()
        {
            var savedProcessDetails = Common.GetProcessDetails();            
            foreach (var savedProcessDetail in savedProcessDetails)
            {
                ProcessesToCheck.TryAdd(savedProcessDetail.Name, savedProcessDetail);
            }            
            ProcessSearch();
            var process = Common.AppSettings.Settings["ProcessName"].Value.Split(';');
            var processSql = process.Aggregate("1=1", (current, proc) => current + string.Format(" or ProcessName ='{0}' ", proc));
            processSql = processSql.Replace("1=1 or", "");
            _processStartEvent = new ManagementEventWatcher(string.Format("SELECT * FROM Win32_ProcessStartTrace where {0}",processSql));
            _processStopEvent = new ManagementEventWatcher(string.Format("SELECT * FROM Win32_ProcessStopTrace where {0}",processSql));

            _processStartEvent.EventArrived += processStartEvent_EventArrived;
            _processStopEvent.EventArrived += processStopEvent_EventArrived;

            _processStartEvent.Start();
            _processStopEvent.Start();
            StartTimers();
        }
        
        private void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            _eventTimer.Stop();
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString().Replace(".exe", "");
            var processId = int.Parse(e.NewEvent.Properties["ProcessID"].Value.ToString());
            var processDetails = new ProcessDetails { Name = processName, Pid = processId, StartTime = DateTime.Now };
            Common.WriteToLog(string.Format("Process [{0}] started | ID: {1}", processName, processId));
            ProcessesToCheck.AddOrUpdate(processName, processDetails, (newKey, oldValue) => processDetails);
            SendMailNotification.SendMail(string.Format("Game {0} started at: {1}", processName, DateTime.Now), "");
            UpdateProcessTimeStamp(processName, true, true, true);
            _eventTimer.Start();
        }

        private void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            _eventTimer.Stop();
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString().Replace(".exe", "");
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            Common.WriteToLog(string.Format("Process [{0}] stopped | ID: {1}", processName, processId));
            UpdateProcessTimeStamp(processName, true,true,true);
            _eventTimer.Start();
        }
        
        private void UpdateProcessTimeStamp(string processName,bool includeEndTime,bool updateLocalStorage, bool sendMail=false)
        {
            ProcessDetails processDetails;
            ProcessesToCheck.TryGetValue(processName, out processDetails);
            if (processDetails != null)
            {
                processDetails.EndTime = includeEndTime ? DateTime.Now : DateTime.MinValue;
                processDetails.TotalTime = (DateTime.Now - processDetails.StartTime).TotalMinutes;
                ProcessesToCheck.AddOrUpdate(processName, processDetails, (newKey, oldValue) => processDetails);
                if (sendMail)
                {
                    SendMailNotification.SendMail(
                        string.Format("Game {0} stopped at: {1} Total time played: {2:0.00}", processName, DateTime.Now,
                                      processDetails.TotalTime), "");
                }
            }
            if (updateLocalStorage)
            {
                UpdateLocalStorage();
            }
        }
        
        private void UpdateLocalStorage()
        {
            Common.SaveProcessDetails(ProcessesToCheck.Values.ToList());
        }
    }

    
}
