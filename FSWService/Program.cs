using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using Topshelf;

namespace FSWService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            // TopShelf Service
            try
            {
                HostFactory.Run( configure =>
                 {
                     configure.Service<FSW_AutoBackup_Service>(service =>
                     {
                         service.ConstructUsing( s => new FSW_AutoBackup_Service() );
                         service.WhenStarted( async s => await s.OnStart() );
                         service.WhenStopped( async s => await s.OnStop() );

                     } );

                     configure.RunAsLocalSystem();
                     configure.SetServiceName("LocalBackupManager");
                     configure.SetDisplayName("Local Backup Manager");
                     configure.SetDescription("Local Backup Manager - Auto backup service");
                 } );
            }
            catch ( Exception exc )
            {
                Console.WriteLine($"TopShelf Error: {exc.Message}\nStackTrace: {exc.StackTrace}");
            }
        }
    }
}
