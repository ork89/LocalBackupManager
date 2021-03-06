using Client.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public class FSWHandler
    {
        private readonly WriteToLog logger = Logger.WriteToLog;
        private static List<FileSystemWatcher> listFileSystemWatcher;
        private static List<FoldersCollection> foldersCollectionList = new List<FoldersCollection>();

        private async Task CreateListOfDirectoriesToWatch()
        {
            var backupsList = await HandleXMLConfigFile.GetListOfBackupsFromConfigFile();

            foreach ( var backup in backupsList )
            {
                if ( backup.IsAutomatic )
                    foldersCollectionList.Add( new FoldersCollection
                    {
                        BackupName = backup.BackupName,
                        SourcePath = backup.SourcePath,
                        DestinationPath = backup.DestinationPath,
                        IncludeSubfolders = backup.IncludeSubfolders,
                        IsIncrementalBackup = backup.IsIncrementalBackup,
                        IsDifferentialBackup = backup.IsDifferentialBackup,
                        IsArchive = backup.IsArchive,
                        BackupLimit = backup.BackupLimit,
                        FolderSize = backup.FolderSize,
                        IsSchedualedBackup = backup.IsSchedualedBackup,
                        IsAutomatic = backup.IsAutomatic
                    } );
            }
        }

        public async Task StartFileSystemWatcher()
        {
            try
            {
                await CreateListOfDirectoriesToWatch();
                listFileSystemWatcher = new List<FileSystemWatcher>();

                // Create a listener for each of the folders in the list
                foreach ( var folder in foldersCollectionList )
                {
                    DirectoryInfo dir = new DirectoryInfo( folder.SourcePath );
                    
                    if ( !dir.Exists )
                        return;

                    // Creates a new instance of FileSystemWatcher
                    FileSystemWatcher fileSysWatch = new FileSystemWatcher
                    {
                        IncludeSubdirectories = folder.IncludeSubfolders == true,
                        InternalBufferSize = 64000,

                        // Filter out specific file types
                        Filter = "*",

                        // Path to monitored folder
                        Path = folder.SourcePath,

                        // Subscribe to notifications
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size
                    };

                    // Associate the event that will be triggered when a file
                    // is created/changed/deleted/renamed to the monitored folder, using a lambda expression                   
                    fileSysWatch.Created += async ( senderObj, fileSysArgs ) => await FileSWatch_CreatedAsync( senderObj, fileSysArgs );

                    fileSysWatch.Changed += async ( senderObj, fileSysArgs ) => await FileSWatch_ChangedAsync( senderObj, fileSysArgs );

                    fileSysWatch.Renamed += async ( senderObj, fileSysArgs ) => await FileSWatch_RenamedAsync( senderObj, fileSysArgs );

                    fileSysWatch.Deleted += async ( senderObj, fileSysArgs ) => await FileSWatch_DeletedAsync( senderObj, fileSysArgs );

                    fileSysWatch.Error += ( senderObj, fileSysArgs ) => FileSysWatch_Error( senderObj, fileSysArgs );

                    // Begin monitoring
                    fileSysWatch.EnableRaisingEvents = true;

                    // Add the systemWatcher to the list
                    listFileSystemWatcher.Add( fileSysWatch );

                    // Record a log entry into Windows Event Log
                    logger( LogLevel.Info, $"Starting to monitor source directory {fileSysWatch.Path}" );
                };
            }
            catch ( Exception ex )
            {
                logger( LogLevel.Error, $"Error while setting FileSystemWatcher: {ex.Message}\n{ex.StackTrace}\n{ex.StackTrace}" );
            }
        }

        public async Task StopFileSystemWatcher( string caller )
        {
            try
            {
                await Task.Run( () =>
                 {
                     logger( LogLevel.Info, $"FSW has been stopped by {caller}" );
                     if ( listFileSystemWatcher == null )
                         return;

                     foreach ( FileSystemWatcher fsw in listFileSystemWatcher )
                     {
                         fsw.EnableRaisingEvents = false;
                         fsw.Dispose();
                     }

                     listFileSystemWatcher.Clear();
                 } );
            }
            catch ( Exception exc )
            {
                logger( LogLevel.Error, $"Exception has been thrown while stopping FSW.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
            }
        }

        #region FSW Events
        private async Task FileSWatch_ChangedAsync( object senderObj, FileSystemEventArgs fileSysArgs )
        {
            logger( LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" has been changed" );
            await ExecuteAutomaticBackup( fileSysArgs );
        }

        private async Task FileSWatch_CreatedAsync( object senderObj, FileSystemEventArgs fileSysArgs )
        {
            logger( LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" has been created" );
            await ExecuteAutomaticBackup( fileSysArgs );
        }

        private async Task FileSWatch_DeletedAsync( object senderObj, FileSystemEventArgs fileSysArgs )
        {
            logger( LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" has been deleted" );
            await ExecuteAutomaticBackup( fileSysArgs );
        }

        private async Task FileSWatch_RenamedAsync( object senderObj, RenamedEventArgs fileSysArgs )
        {
            logger( LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" has been renamed" );
            await ExecuteAutomaticBackup( fileSysArgs );
        }

        private void FileSysWatch_Error( object senderObj, ErrorEventArgs fileSysArgs )
        {
            var exception = fileSysArgs.GetException();
            logger( LogLevel.Error, $"FileSystemWatcher has encountered an error:\nError: {exception.Message}\nStack Trace: {exception.StackTrace}" );
        }
        #endregion FSW Events

        private async Task ExecuteAutomaticBackup( FileSystemEventArgs fileSysArgs )
        {
            var sourceDir = foldersCollectionList.Where( folder => folder.SourcePath == Path.GetDirectoryName( fileSysArgs.FullPath ) ).ToList();
            if ( sourceDir.Any() )
                await ExecuteBackups.ExecuteBackupScript( sourceDir );
        }
    }
}
