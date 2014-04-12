using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwisterDaemon
{
    [RunInstaller(true)]
    public class CustomInstaller: Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public CustomInstaller()
        {
            process = new ServiceProcessInstaller();

            process.Account = ServiceAccount.LocalSystem;

            service = new ServiceInstaller();

            service.ServiceName = "TwisterDaemon";
            service.StartType = ServiceStartMode.Automatic;
            service.DisplayName = "TwisterDaemon";
            service.Description = "Turns your computer into a Twister Net.";

            Installers.Add(process);
            Installers.Add(service);
        }
    }
}