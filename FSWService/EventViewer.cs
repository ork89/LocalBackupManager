using System;
using System.Diagnostics;

namespace FSWService
{
    class EventViewer
    {
        public static void WrireToEventViewer( string msg, int code, EventLogEntryType logtype )
        {
            try
            {
                //EventLog eventLog = new EventLog("BLM_Log" );
                //Console.WriteLine( $"Machine Name: {eventLog.MachineName}" );

                var machineName = Environment.MachineName;

                if (!EventLog.Exists( "BLM_Log" ) )
                {
                    if ( !EventLog.SourceExists( "LocalBackupManager", machineName ))
                    {
                        EventSourceCreationData escd = new EventSourceCreationData( "LocalBackupManager", "BLM_Log" )
                        {
                            MachineName = machineName
                        };

                        EventLog.CreateEventSource( escd );
                    }
                }
                else
                {
                    using ( EventLog eventLog = new EventLog( "BLM_Log", machineName, "LocalBackupManager" ) )
                    {
                        eventLog.WriteEntry( msg, logtype, code );
                    }
                }
            }
            catch ( Exception exc )
            {
                throw new Exception( $"Error while writing to EventViewer:\nError: {exc.Message}\nStack Trace: {exc.StackTrace}", exc );
            }
        }
    }
}
