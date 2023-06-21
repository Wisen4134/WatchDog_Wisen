
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchDog_Wisen
{
    internal class ProcessLog 
    {
        public StringBuilder builder { get; set;}
        public string folderPath { get; set; }
        public int stringLength { get; set; }
        public object ExportLock { get ; set; }
        public object LogLock { get; set; }
        public DateTime date { get ; set; }

        public ProcessLog()
        {
            builder= new StringBuilder();
            folderPath = "Log";
            stringLength = 40;
            LogLock = new object();
            ExportLock = new object();
            date = DateTime.Now;
        }

        public void Export()
        {
            lock(ExportLock)
            {
                try
                {
                    if(stringLength != 0)
                    {
                        if(!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);  
                        }
                        string filename = date.ToString("yyyy-MM-dd") + ".log";
                        string exportpath = Path.Combine(folderPath, filename);

                        string agofilename = date.AddDays(-7).ToString("yyyy-MM-dd") + ".log";
                        string deletepath = Path.Combine(folderPath, agofilename);
                        if (File.Exists(deletepath))
                            File.Delete(deletepath);

                        File.AppendAllText(exportpath, builder.ToString());
                        builder.Length = 0;

                    }
                }
                catch (Exception exp)
                {
                    Log("SystemLog", "Export", exp);
                }
            }
        }

        public void Log(string pTitle, string pMesssage, Exception pException)
        {
            lock(LogLock)
            {
                try
                {
                    DateTime nowDate = DateTime.Now;
                    if (date.Day != nowDate.Day)
                    {
                        if (builder.Length > 0)
                            Export();
                        date = nowDate;
                    }
                    else if (builder.Length > stringLength)
                        Export();

                    string info = string.Format("{0}\t{1}\t{2}\t{3}", nowDate.ToString("yyyy-MM-dd HH:mm:ss"), pTitle, pMesssage, pException == null ? "" : pException.ToString());

                    builder.AppendLine(info);
                    Console.WriteLine($"SystemLog ： {info}");
                }
                catch (Exception exp) 
                {
                    throw;
                }
                
            }
        }
    }
}
