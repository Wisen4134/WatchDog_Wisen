using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatchDog_Wisen
{
    internal class Program
    {
        public static string _sAppName = "";

        public static string _sAppPath = "";

        public static string _sWatchInterval = "";

        public static string _sDBUse = "";

        public static string _sFood = "";

        public static string _sRest = "";

        public static string _sFilewatchpath = "";

        public static string _sFileWatchFilter = "";

        public static string _sTest = "";

        private string mAppName = "";

        private int mWdInterval = 0;

        private FileSystemWatcher mFileWatcher = new FileSystemWatcher();

        private bool mIniChangeIndex = false;

        private object mWatchLock = new object();

        private static Mutex mMutex;


        static void Main(string[] args)
        {
            bool processCreate;

            // 新增互斥鎖,防止重複開啟
            mMutex = new Mutex(true, "MyApplication", out processCreate);

            if (processCreate)
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

                readSettingJson();

                int watchInterval = Int32.Parse(_sWatchInterval);

                Program app = new Program(_sAppName, watchInterval);
            }
        }

        public Program(string appname, int wdinterval)
        {
            this.mAppName = appname;
            this.mWdInterval = wdinterval;

            try
            {

                Thread wdThread = new Thread(new ThreadStart(startwatch));
                wdThread.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine("wdThread error:" + e.StackTrace);
                
            }
        }

        private void startwatch()
        {
            // -- 延遲5秒開始 --
            Thread.Sleep(2000);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // -- 遠端用 --
            string appwatchpath = $@"{_sAppPath}{mAppName}.exe";

            // -- 測試用 --
            if (_sTest == "true")
            {
                _sFood = $@"{desktop}\food";
                _sRest = $@"{desktop}\rest";
                _sDBUse = $@"{desktop}\dbUse";
                _sFilewatchpath = $@"{desktop}\";
                appwatchpath = $@"{desktop}\{mAppName}.exe";
            }

            // -- 開始監測目標檔案是否更動 -- 
            fileWatchStart(_sFilewatchpath, _sFileWatchFilter);

            while (true)
            {
                lock (mWatchLock)
                {
                    // -- 假如Rest存在、程式在操作資料庫則休息 -- 
                    if (!File.Exists(_sRest) || !File.Exists(_sDBUse))
                    {
                        // -- 如果目標程序不存在 --
                        if (!appexist())
                        {
                            // -- 如果被監測程式路徑存在 --
                            if (File.Exists(appwatchpath))
                            {
                                // -- 啟動被監測程序 -- 
                                Process.Start(appwatchpath);
                                Process[] p = Process.GetProcessesByName(mAppName);

                                try
                                {
                                    foreach (var process in p)
                                    {
                                        
                                    }
                                }
                                catch (Exception e)
                                {
                                    foreach (var process in p)
                                    {
                                        
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine("Dog want watch the app which should be monited can`t found at" + appwatchpath);
                            }
                        }

                        // -- 如果目標程序存在 --
                        else
                        {
                            // -- 重啟目標程式 -- (條件＝> 1.沒有food 2.config.ini改變)
                            if (!File.Exists(_sFood) || mIniChangeIndex)
                            {
                                while (appexist())
                                {
                                    try
                                    {
                                        Process[] p = Process.GetProcessesByName(mAppName);
                                        foreach (Process process in p)
                                        {
                                            process.Kill();
                                        }


                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("kill app error:" + e.Message);
                                    }
                                }


                                // -- 啟動被監測程序 --
                                Process.Start(appwatchpath);
                                mIniChangeIndex = false;
                            }
                            else
                            {
                                try
                                {
                                    // -- 狗狗吃飯 -- 
                                    if (File.Exists(_sFood))
                                    {
                                        File.Delete(_sFood);
                                        Console.WriteLine($"Food is been ate!");
                                    }
                                    //continue;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("FOOD Delete error :" + e.Message);
                                }
                            }
                        }



                    }
                    else
                    {
                        Console.WriteLine("Dog is rest because dog is rest or App using DataBase! ");
                    }
                    Thread.Sleep(mWdInterval);
                }

            }
        }

        private bool appexist()
        {
            try
            {
                Process[] p = Process.GetProcessesByName(mAppName);

                if (p.Length == 0)
                {

                    
                    return false;

                }
                else
                {
                    
                    return true;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("appexist error:" + e.Message);
                
                return true;
            }
        }

        private void fileWatchStart(string pPath, string pFilter)
        {
            mFileWatcher.Filter = pFilter;
            mFileWatcher.Path = pPath;
            mFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            mFileWatcher.Changed += new FileSystemEventHandler(OnChanged);
            //mFileWatcher.Created += new FileSystemEventHandler(OnChanged);
            mFileWatcher.Deleted += new FileSystemEventHandler(OnChanged);
            mFileWatcher.Renamed += new RenamedEventHandler(OnRenamed);
            mFileWatcher.EnableRaisingEvents = true;

            //拿來檢查ini是否太久沒被"訪問修改"？ 重啟:維持 ，DateTime dateTime = Directory.GetLastAccessTime($"{_sFilewatchpath}{mFileWatchFilter}"); 
        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            mFileWatcher.EnableRaisingEvents = false; //用這個來避免一次改變 重複觸發兩次此事件的情況
            Console.WriteLine($"檔案變更：{e.FullPath} \t {e.ChangeType}");// 這裡擺重啟程式的方法
            mIniChangeIndex = true; //藉由此index判斷程式是否需要重啟，如果ini有改變 = True ; 沒改變 = False
            Thread.Sleep(10); //要停頓一下才不會重複觸發
            mFileWatcher.EnableRaisingEvents = true;
            
        }
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"檔案改變：{e.OldFullPath} \t 變為{e.FullPath}");
        }
        private static void readSettingJson()
        {
            // -- 根據外部設定檔取得啟動參數 -- (App.config - <appsetting> - <Value[Key]>)
            
            // -- 被監控程式名稱 --
            _sAppName = ConfigurationManager.AppSettings["APP"];

            // -- 被監控程式當前資料夾路徑 -- 
            _sAppPath = ConfigurationManager.AppSettings["PATH"];

            // -- 監控週期 -- 
            _sWatchInterval = ConfigurationManager.AppSettings["INTERVAL"];

            // -- 監測是否正在使用DB --
            _sDBUse = ConfigurationManager.AppSettings["DBUSE"];

            // -- 監測是否程式還在運行 --
            _sFood = ConfigurationManager.AppSettings["FOOD"];

            // -- IF路徑檔案存在,則讓此程式休眠 -- 
            _sRest = ConfigurationManager.AppSettings["REST"];

            // -- 被監測檔案路徑 --
            _sFilewatchpath = ConfigurationManager.AppSettings["FileWatchPath"];

            // -- 被監測檔案名稱的過濾器 --
            _sFileWatchFilter = ConfigurationManager.AppSettings["FileWatchFilter"];

            // -- 是否使用開發中路徑 -- 
            _sTest = ConfigurationManager.AppSettings["Test"];
        }
        private static void OnProcessExit(object sender, EventArgs e)
        {
            
        }
    }
}
