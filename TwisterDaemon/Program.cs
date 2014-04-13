using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace TwisterDaemon
{
    class Program: ServiceBase
    {

        public Program()
        {
            this.ServiceName = "TwisterDaemon";
        }

        private static string dir = Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\twisterd\");
        [System.STAThreadAttribute()]
        static int Main(string[] args)
        {
            if ( !Directory.Exists(dir) )
                Directory.CreateDirectory(dir);
            Directory.SetCurrentDirectory(dir);
                
            InitLogger();
            // Старт сервиса
            if ( !System.Environment.UserInteractive )
            {
                ServiceBase.Run(new Program());
                return 0;
            }
            if ( args.Length != 1 )
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("   --install");
                Console.WriteLine("   --uninstall");
                Console.WriteLine("   --run");

                return 0;
            }
            if ( args[0] == "--install" )
            {
                Install();
            }
            else if ( args[0] == "--uninstall" )
            {
                Uninstall();
            }
            else if ( args[0] == "--run" )
            {
                daemon();
            }
            return 0;
        }
        public static void InitLogger()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            consoleTarget.Layout = "${date:format=HH\\:mm\\:ss.ffffff}\t${logger}\t${threadname}\t${message:exceptionSeparator=String}\t${exception}";
            LoggingRule ruleconsole = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(ruleconsole);

            LogManager.Configuration = config;
        }
        //
        // Код, необходимый для работы в качестве службы Windows
        //
        #region Service OnStart OnStop

        Thread MyThread;
        protected override void OnStart(string[] args)
        {
            MyThread = new Thread(new ThreadStart(daemon));
            MyThread.Name = "TwisterDaemon.mainThread";
            MyThread.Start();

            base.OnStart(args);
        }

        private static Process p;
        private static void daemon()
        {
            TryToCloseProcessByPids(getPIDsByFullPath(dir + @"twister\twisterd.exe"));
            while ( true )
            {
                p = new Process();
                p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", @"-datadir=./data -htmldir=./html -rpcuser=user -rpcpassword=pwd -rpcallowip=127.0.0.1");
                p.StartInfo.WorkingDirectory = dir;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                AddPeer();
                p.WaitForExit();
            }
        }

        private static  void AddPeer()
        {
            Thread.Sleep(2000);
            p = new Process();
            p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", "-rpcuser=user -rpcpassword=pwd addnode seed3.twister.net.co onetry");
            p.Start();
            Thread.Sleep(2000);
            p = new Process();
            p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", "-rpcuser=user -rpcpassword=pwd addnode seed2.twister.net.co onetry");
            p.Start();
            Thread.Sleep(2000);
            p = new Process();
            p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", "-rpcuser=user -rpcpassword=pwd addnode seed.twister.net.co onetry");
            p.Start();
            Thread.Sleep(2000);
            p = new Process();
            p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", "-rpcuser=user -rpcpassword=pwd addnode dnsseed.gombadi.com onetry");
            p.Start();
            Thread.Sleep(2000);
            p = new Process();
            p.StartInfo = new ProcessStartInfo(dir + @"twister\twisterd.exe", "-rpcuser=user -rpcpassword=pwd addnode dnsseed.gombadi.com onetry");
            p.Start();
            Thread.Sleep(2000);
        }


        public static List<int> getPIDsByFullPath(string path)
        {
            ConcurrentQueue<int> pids = new ConcurrentQueue<int>();
            object lk = new object();
            Process[] processlist = Process.GetProcesses();

            // Console.ReadLine();
            path = path.Replace(@"\\", @"\").Replace(@"\\", @"\").Replace(@"\\", @"\").Replace(@"\\", @"\");

            string Filename;
            foreach ( var process in processlist )
            {
                try
                {
                    Filename = process.MainModule.FileName;
                }
                catch
                {
                    Filename = "";
                }
                if ( Path.Equals(Filename, path) )
                {
                    pids.Enqueue(process.Id);
                }
            }

            return pids.ToList<int>();
        }

        public static void TryToCloseProcessByPids(List<int> pids, bool safe = true)
        {
            foreach ( var pid in pids )
            {
                var process = Process.GetProcessById(pid);
                if ( !process.HasExited )
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            return;
        }

        protected override void OnStop()
        {
            MyThread.Abort();
            p.Kill();
            Stop();
            base.OnStop();
        }

        #endregion

        #region Install Ununstall
        //
        // Установка сервиса
        //
        public static void Install()
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            try
            {
                if ( ServiceCtl.isInstalled() )
                {
                    // FIXME если интерактивный режим, можно сначала спросить подтверждение переустановки, 
                    // если сервис уже установлен
                    // Uninstall();

                    throw new Exception("Сервис уже установлен в системе.");
                }

                // FIXME путь до файла надо сделать менее конкретным, что ли.
                ManagedInstallerClass.InstallHelper(new string[] {
                  "/LogToConsole=true", 
                  "/LogFile=", 
                  "/ShowCallStack=false", 
                  dir + "TwisterDaemon.exe"
                });


                logger.Info("Service installed");
                ServiceCtl.StartService("TwisterDaemon", 3000);
                logger.Info("Service started");
            }
            catch ( Exception e )
            {
                logger.Debug("Error : " + e.Message);
            }
        }

        //
        // Удаление сервиса
        //
        public static void Uninstall()
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            try
            {
                if ( !ServiceCtl.isInstalled() )
                    throw new Exception("Сервис не установлен в системе.");

                ManagedInstallerClass.InstallHelper(new string[] { 
                  "/u", 
                  "/LogToConsole=false", 
                  "/LogFile=", 
                  "/ShowCallStack=false", 
                  dir + "TwisterDaemon.exe"
                });

                logger.Info("Service deleted");
            }
            catch ( Exception e )
            {
                logger.Debug("Error : " + e.Message);
            }
        }
        #endregion

    }
    class ServiceCtl
    {
        public static bool isInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "TwisterDaemon");
        }

        public static int StartService(string serviceName, int timeoutMilliseconds)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            ServiceController sc = new ServiceController();
            sc.ServiceName = serviceName;

            if ( sc.Status == ServiceControllerStatus.Stopped )
            {
                // Start the service if the current status is stopped.
                logger.Info("Starting the {0} service.", serviceName);

                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(timeoutMilliseconds));

                    // Display the current service status.
                    logger.Info("The Alerter service status is now set to {0}.", sc.Status.ToString());
                }
                catch ( Exception ex )
                {
                    logger.Error("Could not start the {0} service.", serviceName);
                    logger.Error(ex.InnerException.Message);
                    return 1;
                }
            }
            return 0;
        }

        public static int StopService(string serviceName, int timeoutMilliseconds)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            ServiceController sc = new ServiceController();
            sc.ServiceName = serviceName;

            if ( sc.Status == ServiceControllerStatus.Running )
            {
                // Start the service if the current status is stopped.
                logger.Info("Starting the {0} service.", serviceName);
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(timeoutMilliseconds));

                    // Display the current service status.
                    logger.Info("The Alerter service status is now set to {0}.", sc.Status.ToString());
                }
                catch ( Exception ex )
                {
                    logger.Error("Could not start the {0} service.", serviceName);
                    logger.Error(ex.InnerException.Message);
                    return 1;
                }
            }

            return 0;
        }

        public static bool IsServiceRunningOrPaused(string service)
        {
            ServiceController sc = new ServiceController(service);

            if ( sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused )
                return true;

            return false;
        }
    }
}
