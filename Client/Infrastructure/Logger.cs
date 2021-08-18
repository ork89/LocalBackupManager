using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace Client.Infrastructure
{
    public delegate void WriteToLog( LogLevel logLevel, string message );

    public class Logger
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private static readonly object lockobj = new object();

        public static void WriteToLog( LogLevel level, string message )
        {
            var loggerPath = Path.Combine( ConfigurationManager.AppSettings["LoggerPath"], $"LBM_{DateTime.Today.ToString( "dd-MM-yyyy" )}.log" );
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
                Console.WriteLine($"{exc.Message}\n\n{exc.StackTrace}\n", exc);
            }

            try
            {

                lock ( lockobj )
                {
                    using ( StreamWriter sw = File.AppendText( loggerPath ) )
                    {
                        sw.WriteLine( $"{DateTime.Now} - {level} - {message + Environment.NewLine}" );
                        sw.Close();
                    }
                }
                
                //_readWriteLock.ExitWriteLock();
            }
            catch ( Exception exc )
            {
                if ( exc is IOException )
                    Console.WriteLine($"The program has encountered an IOException when trying to access log file.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}");

                Console.WriteLine( $"{exc.Message}\n\n{exc.StackTrace}\n", exc );
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
