using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace resumeGC
{
    class Program
    {
        private static bool isDebug = true;
        private static string womDirStr = @"F:\Logs\Click Portal";

        private static string teminatedKeyword = @"Warning: Garbage Collection on store 'rise' terminating with Exception";

        private static string restartKeyword = @"Information: GarbageCollection (rise) - Performing Garbage Collection.";
        private static string stopGCFile = @"C:\Schedulled Tasks\resumeGC.bat";
        private static string resumeGCFile = @"C:\Schedulled Tasks\stopGC.bat";
        
        private static int howlong = isDebug ? 1:300;
        private static int interval = 300;
        private static readonly int intervalForCheck = isDebug ? 1000 : interval * 1000;
        static void Main(string[] args)
        {
            try
            {  
                LogMessageToFile("Start Resume GC Program");           
                var dayofToday = DateTime.Now.DayOfWeek;
                if (dayofToday.ToString() == "Saturday" || dayofToday.ToString() == "Sunday" || isDebug == true)
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    while (s.Elapsed < TimeSpan.FromMinutes(howlong))
                    {
                        FileInfo result = null;
                        result = GetLatestWomLogFile(womDirStr);
                        Console.WriteLine(result.FullName);
                        List<string> teminatedLines = GetKewwordLines(result.FullName, teminatedKeyword);
                        List<string> restartedLines = GetKewwordLines(result.FullName, restartKeyword);
                        var lastTerminatedTimeStamp = GetTimeStamp(teminatedLines);
                        var lastRestartedTimeStamp = GetTimeStamp(restartedLines);
                        if (lastTerminatedTimeStamp != DateTime.MinValue)
                        {
                            ProcessResumeGc(lastTerminatedTimeStamp, lastRestartedTimeStamp);
                        }
                        Thread.Sleep(intervalForCheck);
                    }
                    s.Stop();
                }
                LogMessageToFile("End Resume GC Program");
            }
            catch (Exception e)
            {
                LogMessageToFile(e.Message);
                throw;
            }        

        }

        private static List<string> GetKewwordLines(string fullName, string searchKeyword)
        {
            try
            {
                string[] textLines = File.ReadAllLines(fullName);
                List<string> results = new List<string>();

                foreach (string line in textLines)
                {
                    if (line.Contains(searchKeyword))
                    {
                        results.Add(line);
                    }
                }
                return results;
            }
            catch (Exception e)
            {
                LogMessageToFile(e.Message);
                throw;
            }
        }

        private static void ProcessResumeGc(DateTime lastTerminatedTimeStamp, DateTime lastRestartedTimeStamp)
        {
            try
            {
                if (DateTime.Compare(lastRestartedTimeStamp, lastTerminatedTimeStamp) < 0)
                {
                    LogMessageToFile("start stop GC");
                    
                    using (Process stopGc = Process.Start(stopGCFile))
                    {
                        if (stopGc != null) stopGc.WaitForExit();
                    }

                    Thread.Sleep(10000);
                    LogMessageToFile("Start Resume GC");
                    Process.Start(resumeGCFile);                               
                }
            }
            catch (Exception e)
            {
                LogMessageToFile(e.Message);
                throw;
            }
        }

        private static DateTime GetTimeStamp(List<string> Lines)
        {
            var lastItem = Lines.LastOrDefault();
            var lastDateTime = DateTime.MinValue;
            
            if (lastItem != null)
            {
                LogMessageToFile(lastItem);
                var year = int.Parse(lastItem.Substring(0, 4));
                var month = int.Parse(lastItem.Substring(5, 2));
                var day = int.Parse(lastItem.Substring(8, 2));
                var hour = int.Parse(lastItem.Substring(11, 2));
                var mim = int.Parse(lastItem.Substring(14, 2));
                var sec = int.Parse(lastItem.Substring(17, 2));
                lastDateTime = new DateTime(year, month, day, hour, mim, sec);
            }
            return lastDateTime;
        }

        private static FileInfo GetLatestWomLogFile(string womdir)
        {
            FileInfo result = null;
            var directory = new DirectoryInfo(womdir);
            //var list = directory.GetFiles("wom*.log");
            var list = directory.GetFiles("wom.2016.07.27.1.0.log");
            if (list.Any())
            {
                result = list.OrderByDescending(f => f.LastWriteTime).First();

            }

            return result;
        }

        private static void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText("LogFile.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}
