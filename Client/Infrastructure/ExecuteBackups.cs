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
            try
            {
                foreach ( var backup in backups )
                {
                    logger( LogLevel.Info, $"Starting backup: \"{backup.BackupName}\"\n" );
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
                        await TransferFilesToBackupDestination( name.ToString(), backup.BackupName, source, destination, arguments.ToString() ).ConfigureAwait( false );
                        continue;
                    }

                    name.Append( ".zip" );
                    await ArchiveAndTransferBackup( name.ToString(), backup.BackupName, source, destination, isDifferentialBackup ).ConfigureAwait( false );
                }

                logger( LogLevel.Info, $"Backup Completed. Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
            }
            catch ( Exception exc )
            {
                logger( LogLevel.Error, $"Backup Failed.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
            }
            finally
            {
                stopwatch.Stop();
                stopwatch.Reset();
            }
        }

        public static async Task RestoreSingleBackup( FoldersCollection backupToRestore )
        {
            try
            {
                stopwatch.Start();
                logger( LogLevel.Info, $"Restoring backup \"{backupToRestore.BackupName}\" from <= \"{backupToRestore.DestinationPath}\" to => \"{backupToRestore.SourcePath}\"" );

                if ( backupToRestore.IsAutomatic )
                {
                    // Stop FSW in case its monitoring the directory as to not cause a new backup as a result from the restore process.
                    FSWHandler fsw = new FSWHandler();
                    await fsw.StopFileSystemWatcher( "[ExecuteBackups.cs] RestoreSingleBackup" );
                    await RestoreFilesToBackupSource( backupToRestore.DestinationPath, backupToRestore.SourcePath, backupToRestore.BackupName );
                    await fsw.StartFileSystemWatcher();

                }
                else
                    await RestoreFilesToBackupSource( backupToRestore.DestinationPath, backupToRestore.SourcePath, backupToRestore.BackupName );

                logger( LogLevel.Info, $"Backup restored. Time Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
            }
            catch ( Exception exc )
            {
                logger( LogLevel.Error, $"Restore Failed.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
            }
            finally
            {
                stopwatch.Stop();
                stopwatch.Reset();
            }
        }

        private static async Task ArchiveAndTransferBackup( string name, string originalBackupName, string source, string destination, bool isDifferential )
        {
            singleOpStopwatch.Start();

            // Create a new archive with a time-stamp to differentiate the new backup from the older one/s.
            if ( !isDifferential )
            {
                await Task.Run( () =>
                {
                    try
                    {
                        logger( LogLevel.Info, $"Archiving: {name}" );
                        ZipFile.CreateFromDirectory( source, destination + name, CompressionLevel.Optimal, false );
                        return;
                    }
                    catch ( Exception exc )
                    {
                        logger( LogLevel.Error, $"Error while archiving files.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
                    }
                } );
            }

            // Update an existing archive with only the new and modified files form the source directory.
            await Task.Run( () =>
             {
                 try
                 {
                     // Create the archive if it does not exist.
                     var backupPath = Path.Combine( destination, name );

                     if ( !Directory.Exists( destination ) )
                         Directory.CreateDirectory( destination );

                     if ( !File.Exists( backupPath ) )
                     {
                         logger( LogLevel.Info, $"Archiving: {name}" );
                         ZipFile.CreateFromDirectory( source, backupPath, CompressionLevel.Optimal, false );
                         return;
                     }

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
                             logger( LogLevel.Info, $"Updating file: {fileToReplace}" );

                             var archivedFile = archive.Entries.FirstOrDefault( n => n.Name == fileToReplace );
                             if ( archivedFile == null )
                                 continue;

                             var archivedFileName = archivedFile.FullName;
                             archivedFile.Delete();
                             archive.CreateEntryFromFile( sFile, archivedFileName );

                             archive.GetEntry( archivedFileName ).LastWriteTime = DateTimeOffset.UtcNow.LocalDateTime;
                         }
                     }
                 }
                 catch ( Exception exc )
                 {
                     logger( LogLevel.Error, $"Exception has been thrown while archiving the backup files.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
                 }
             } );
        }

        private static async Task TransferFilesToBackupDestination( string name, string originalBackupName, string source, string destination, string arguments )
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
                            logger( LogLevel.Info, $"copying {e.Data}" );

                        //if ( ( !string.IsNullOrEmpty( e.Data ) ) && e.Data.Contains( "copied" ) )
                        //    logger( LogLevel.Info, $"Completed \"{originalBackupName}\" backup in: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
                    };

                    process.BeginOutputReadLine();

                    process.ErrorDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( !string.IsNullOrEmpty( e.Data ) )
                            logger( LogLevel.Error, $"Exception has been thrown while backing up file: {e.Data}" );
                    };
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    process.Close();
                }
                catch ( Exception exc )
                {
                    logger( LogLevel.Error, $"Exception has been thrown while backing up {originalBackupName}\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
                }
            } );
        }

        private static async Task RestoreFilesToBackupSource( string source, string destination, string backupName )
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
                            logger( LogLevel.Info, $"restoring {e.Data}" );

                        //if ( ( !string.IsNullOrEmpty( e.Data ) ) && e.Data.Contains( "copied" ) )
                        //    logger( LogLevel.Info, $"Restored \"{backupName}\" backup in: {stopwatch.Elapsed:hh\\:mm\\:ss}" );
                    };

                    process.BeginOutputReadLine();

                    process.ErrorDataReceived += ( object sender, DataReceivedEventArgs e ) =>
                    {
                        if ( !string.IsNullOrEmpty( e.Data ) )
                            logger( LogLevel.Error, $"Exception has been thrown while restoring file: {e.Data}" );
                    };
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    process.Close();
                }
                catch ( Exception exc )
                {
                    logger( LogLevel.Error, $"Exception has been thrown while restoring backup.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
                }
            } );
        }
    }
}
