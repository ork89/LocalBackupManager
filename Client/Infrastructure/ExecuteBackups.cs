using Client.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Client.Infrastructure
{
    public class ExecuteBackups
    {
        private static readonly WriteToLog logger = Logger.WriteToLog;
        private static Stopwatch stopwatch = new Stopwatch();
        private static Stopwatch singleOpStopwatch = new Stopwatch();

        public static async Task ExecuteBackupScript( List<FoldersCollection> backups )
        {
            stopwatch.Start();
            foreach ( var backup in backups )
            {
                var name = new StringBuilder();
                var arguments = new StringBuilder();

                var source = backup.SourcePath;
                var destination = backup.DestinationPath;
                name.Append( backup.BackupName );
                var includeSubfolders = backup.IncludeSubfolders;
                var isIncrementalBackup = backup.IsIncrementalBackup;
                var isDifferentialBackup = backup.IsDifferentialBackup;
                var isArchive = backup.IsArchive;

                arguments.Append( " /i /y /c" );

                if ( includeSubfolders )
                    arguments.Append( " /e" );

                if ( isDifferentialBackup )
                    arguments.Append( " /d" );

                if ( isIncrementalBackup )
                {
                    var date = DateTime.Now.ToString( "yyyy_MM_dd-HH_mm_ss" );
                    name.Append( $"_{date}" );
                }

                if ( !isArchive )
                {
                    await TransferFilesToBackupLocation( name.ToString(), backup.BackupName, source, destination, arguments.ToString() ).ConfigureAwait(false);
                    continue;
                }

                name.Append( ".zip" );
                await ArchiveAndTransferBackup( name.ToString(), backup.BackupName, source, destination, isDifferentialBackup ).ConfigureAwait(false);
            }

            stopwatch.Stop();
            logger( LogLevel.Info, $"Backup Completed. Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
        }

        public static async Task RestoreSingleBackup(FoldersCollection backupToRestore)
        {
            stopwatch.Start();
            logger( LogLevel.Info, $"Restoring backup \"{backupToRestore.BackupName}\" from <= \"{backupToRestore.DestinationPath}\" to => \"{backupToRestore.SourcePath}\"" );
            await RestoreFilesToBackupSource(backupToRestore.DestinationPath, backupToRestore.SourcePath, backupToRestore.BackupName);
        }

        private static async Task ArchiveAndTransferBackup( string name, string originalBackupName, string source, string destination, bool isDifferential )
        {
            // Create a new archive with a time-stamp to differentiate the new backup from the older one/s.
            await Task.Run( () =>
            {
                try
                {
                    if ( !isDifferential )
                    {
                        singleOpStopwatch.Start();
                        logger( LogLevel.Info, $"Archiving: {name}" );

                        ZipFile.CreateFromDirectory( source, destination + name, CompressionLevel.Optimal, false );
                        
                        singleOpStopwatch.Stop();
                        logger( LogLevel.Info, $"Completed \"{originalBackupName}\" backup in: {singleOpStopwatch.Elapsed:hh\\:mm\\:ss}" );
                        return;
                    }
                }
                catch ( Exception exc )
                {
                    logger( LogLevel.Error, $"{exc.Message}\n\n{exc.StackTrace}" );
                }
            } );

            // Update an existing archive with only the new and modified files form the source directory.
            await Task.Run( () =>
             {
                 try
                 {
                     singleOpStopwatch.Start();
                     // Create the archive if the it does not exist in first place.
                     var backupPath = Path.Combine( destination, name );
                     if ( !File.Exists( backupPath ) )
                     {
                         logger( LogLevel.Info, $"Archiving: {name}" );
                         
                         ZipFile.CreateFromDirectory( source, backupPath, CompressionLevel.Optimal, false );

                         singleOpStopwatch.Stop();
                         logger( LogLevel.Info, $"Completed \"{originalBackupName}\" backup in: {singleOpStopwatch.Elapsed:hh\\:mm\\:ss}" );
                         return;
                     }

                     if ( !Directory.Exists( destination ) )
                         Directory.CreateDirectory(destination);

                     var filesInSource = Directory.EnumerateFiles( source ).ToList();                     

                     using ( ZipArchive archive = ZipFile.Open( backupPath, ZipArchiveMode.Update ) )
                     {
                         // Go over the zip archive and the files in the source directory and find only the
                         // files that are not in the archive or files that were modified after the archive was created.
                         foreach ( var entry in archive.Entries )
                         {
                             filesInSource.Where( file => Path.GetFileName( file ) == entry.Name && File.GetLastWriteTime( file ) > entry.LastWriteTime.UtcDateTime );
                         }

                         if ( !filesInSource.Any() )
                             return;

                         // Add the new and/or modified files into the archive.
                         foreach ( var sFile in filesInSource )
                         {
                             var fileToReplace = Path.GetFileName( sFile );
                             logger( LogLevel.Info, $"Replacing file: {fileToReplace}" );

                             var archivedFile = archive.Entries.FirstOrDefault( n => n.Name == fileToReplace );
                             if ( archivedFile == null )
                                 continue;

                             var archivedFileName = archivedFile.FullName;
                             archivedFile.Delete();
                             archive.CreateEntryFromFile( sFile, archivedFileName );

                             archive.GetEntry( archivedFileName ).LastWriteTime = DateTimeOffset.UtcNow.LocalDateTime;
                         }
                     }

                     singleOpStopwatch.Stop();
                     logger( LogLevel.Info, $"Completed \"{originalBackupName}\" backup in: {singleOpStopwatch.Elapsed:hh\\:mm\\:ss}" );
                 }
                 catch ( Exception exc )
                 {
                     logger( LogLevel.Error, $"{exc.Message}\n\n{exc.StackTrace}" );
                 }
             } );
        }

        private static async Task TransferFilesToBackupLocation( string name, string originalBackupName, string source, string destination, string arguments )
        {
            await Task.Run( () =>
            {
                try
                {
                    ProcessStartInfo processInfo;
                    processInfo = new ProcessStartInfo
                    {
                        FileName = "xcopy",
                        Arguments = $"\"{source}\" \"{Path.Combine( destination, name )}\"" + arguments,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    var process = Process.Start( processInfo );

                    process.OutputDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( ( !string.IsNullOrEmpty( e.Data ) ) && !e.Data.Contains( "copied" ) )
                            logger( LogLevel.Info, $"{e.Data} has been added to \"{originalBackupName}\" backup" );

                        if ( ( !string.IsNullOrEmpty( e.Data ) ) && e.Data.Contains( "copied" ) )
                            logger( LogLevel.Info, $"Completed \"{originalBackupName}\" backup in: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
                    };

                    process.BeginOutputReadLine();

                    process.ErrorDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( !string.IsNullOrEmpty( e.Data ) )
                            logger( LogLevel.Error, $"Error has been thrown while backing up file: {e.Data}" );
                    };
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    process.Close();
                }
                catch ( Exception exc )
                {
                    logger( LogLevel.Error, $"Backup script has encountered an error: {exc.Message}\n{exc.StackTrace}" );
                }
            } );
        }

        private static async Task RestoreFilesToBackupSource(string source, string destination, string backupName )
        {
            var arguments = " /i /y /c /e";
            await Task.Run( () =>
            {
                try
                {
                    ProcessStartInfo processInfo;
                    processInfo = new ProcessStartInfo
                    {
                        FileName = "xcopy",
                        Arguments = $"\"{source}\" \"{destination}\"" + arguments,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    var process = Process.Start( processInfo );

                    process.OutputDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( ( !string.IsNullOrEmpty( e.Data ) ) && !e.Data.Contains( "copied" ) )
                            logger( LogLevel.Info, $"{e.Data} has been added to \"{backupName}\" backup" );

                        if ( ( !string.IsNullOrEmpty( e.Data ) ) && e.Data.Contains( "copied" ) )
                            logger( LogLevel.Info, $"Restored \"{backupName}\" backup in: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
                    };

                    process.BeginOutputReadLine();

                    process.ErrorDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( !string.IsNullOrEmpty( e.Data ) )
                            logger( LogLevel.Error, $"Error has been thrown while backing up file: {e.Data}" );
                    };
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    process.Close();
                }
                catch ( Exception exc )
                {
                    logger( LogLevel.Error, $"Backup script has encountered an error: {exc.Message}\n{exc.StackTrace}" );
                }

                stopwatch.Stop();
            } );
        }
    }
}
