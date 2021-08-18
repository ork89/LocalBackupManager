using Client.Infrastructure;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FSWService
{
    class FSW_AutoBackup_Service
    {
        FSWHandler fsw;
        public FSW_AutoBackup_Service()
        {
            fsw = new FSWHandler();
        }

        public async Task OnStart()
        {
            await fsw.StartFileSystemWatcher();
            Console.WriteLine( $"{DateTime.Now} - Local Backup Manager Service has Started");
        }

        public async Task OnStop()
        {
            await fsw.StopFileSystemWatcher( "FSWService" );
            Console.WriteLine( $"{DateTime.Now} - Local Backup Manager Service has Stopped" );
        }
    }
}
