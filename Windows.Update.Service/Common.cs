using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Update.Service
{
    internal class Common
    {
        public static AppSettingsSection AppSettings { get; set; }
        public static void WriteToLog(string message)
        {
            Console.Write(message);
            LogMessageToFile(message);
        }

        public static void WriteErrorToLog(Exception ex,
                                           [CallerMemberName] string memberName = "",
                                           [CallerFilePath] string fileName = "",
                                           [CallerLineNumber] int lineNumber = 0)
        {
            var line = string.Format("File:{0} {4}Line:{1} {4}Function:{2} {4}Message:{3}",
                                     new object[] {fileName, lineNumber, memberName, ex.Message, Environment.NewLine});
            Console.WriteLine(line);
            LogMessageToFile(line);
        }

        private static void LogMessageToFile(string msg)
        {            
            try
            {
                var f = new FileInfo(LogPath);
                if (f.Length / 1024 > 2000) // bigger then 2MB
                {
                    f.Delete();
                }
            }
            catch
            {
            }
            var sw = File.AppendText(LogPath);
            try
            {
                string logLine = string.Format(
                        "{0:G}: {1}.", DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
                sw.Dispose();
            }
        }
        public static void SaveProcessDetails(List<ProcessDetails> processDetails)
        {
            var dataText = JsonConvert.SerializeObject(processDetails);
            File.WriteAllText(DataPath, dataText);
        }
        public static string ConvertFromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                return string.Empty;
            return Encoding.ASCII.GetString(Convert.FromBase64String(base64));
        }
        public static List<ProcessDetails> GetProcessDetails()
        {
            var dataText = ReadDataFile();
            if (dataText != null)
            {
               var dataObj = JsonConvert.DeserializeObject<List<ProcessDetails>>(dataText);
                //if(DateTime.Now.Day>dataObj[0].EndTime.Day) // check if data is older than 24 hours
                //{
                //    DeleteDataFile(); ;
                //}
                return dataObj;
            }
            return new List<ProcessDetails>();
        }
        public static void DeleteDataFile()
        {
            try
            {
                File.Move(DataPath, DataPath.Replace(".json", DateTime.Now.Ticks.ToString() + ".json"));
            }
            catch(Exception ex)
            {
                WriteErrorToLog(ex);
            }
        }
        private static string ReadDataFile()
        {
            if (File.Exists(DataPath))
            {
                var dataText = File.ReadAllText(DataPath);
                return dataText;
            }
            return null;
        }
        private static string LogPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt");
            }
        }
        private static string DataPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data.json");
            }
        }        
    }
    internal class ProcessDetails
    {
        public string Name { get; set; }
        public int Pid { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalTime { get; set; }
    }
}
