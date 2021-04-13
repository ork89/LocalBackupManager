using Client.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public class CreateBackupScript
    {
        private static readonly string scriptFilePath = ConfigurationManager.AppSettings["backupScriptPath"];

        public static async Task<bool> CreateNewBackupScript(List<FoldersCollection> foldersList)
        {
            Logger.WriteToLog(LogLevel.Info, "Creating backup script");
            try
            {
                var scripts = await AddBackupsToScript( foldersList );

                FileStream file = new FileStream( scriptFilePath, FileMode.Create );
                using ( var writer = new StreamWriter( file ) )
                {
                    writer.Write( scripts );
                }
            }
            catch ( Exception exc )
            {
                Logger.WriteToLog( LogLevel.Error, $"{exc.Message}\n{exc.StackTrace}" );
            }
            finally
            {
                Logger.WriteToLog(LogLevel.Info, "Backup script is done.");
            }

            return true;
        }

        private static Task<string> AddBackupsToScript( List<FoldersCollection> foldersList )
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine( "for /f \"tokens = 2-8 delims =.:/ \" %%a in (\" % date % % time: =0 % \") do set Datetime=%%c-%%a-%%b_%%d-%%e-%%f" );

            foreach ( var backup in foldersList )
            {
                builder.Append( "\n7z" );

                // Backup only new modified files.
                if ( backup.IsDifferentialBackup )
                    builder.Append( $" u" );

                if ( backup.IsIncrementalBackup )
                    builder.Append( " a" );

                    builder.Append( $" -tzip \"{Path.Combine( backup.DestinationPath, backup.BackupName )}" );

                // If backup is incremental add date stamp at the end of the backups name
                if ( backup.IsIncrementalBackup )
                    builder.Append( "_%Datetime%" );

                builder.Append($".zip\" \"{backup.FolderPath}\"");

                // -mmt = Use Multi-threaded operation, -mx7 = Compression level - Maximum.
                builder.Append( " -mmt  -mx7\n" );
            }

            return Task.FromResult( builder.ToString() );
        }
    }
}
