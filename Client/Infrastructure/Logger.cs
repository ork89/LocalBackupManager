using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public delegate void WriteToLog( LogLevel logLevel, string message );

    public class Logger
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void WriteToLog( LogLevel level, string message )
        {
            var loggerPath = Path.Combine( ConfigurationManager.AppSettings["LoggerPath"], $"BM_{DateTime.Today.ToString( "dd-MM-yyyy" )}.log" );
            var loggerDir = Path.GetDirectoryName( loggerPath );

            try
            {
                var dir = new DirectoryInfo( loggerDir );
                if ( !dir.Exists )
                    Directory.CreateDirectory( dir.FullName );

                if ( !File.Exists( loggerPath ) )
                    File.Create( loggerPath );
            }
            catch ( Exception exc)
            {
                throw new Exception($"{exc.Message}\n\n{exc.StackTrace}\n", exc);
            }

            try
            {
                _readWriteLock.EnterWriteLock();
                using ( StreamWriter sw = File.AppendText( loggerPath ) )
                {
                    sw.WriteLine( $"{DateTime.Now} - {level} - {message + Environment.NewLine}" );
                    sw.Close();
                }
                _readWriteLock.ExitWriteLock();
            }
            catch (Exception exc)
            {
                throw new Exception($"{exc.Message}\n\n{exc.StackTrace}\n", exc);
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Error,
        Warning,
        Debug
    }
}
