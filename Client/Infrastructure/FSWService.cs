using Client.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Infrastructure
{
    public class FSWService
    {
        private readonly WriteToLog logger = Logger.WriteToLog;
        private List<FileSystemWatcher> listFileSystemWatcher;
        private static List<FoldersCollection> xmlConfigFields = new List<FoldersCollection>();

        public FSWService()
        {
            CreateListOfDirectoriesToWatch();
        }

        private async Task CreateListOfDirectoriesToWatch()
        {
            var backupsList = await HandleXMLConfigFile.GetListOfBackupsFromConfigFile().ConfigureAwait( false );

            foreach ( var backup in backupsList )
            {
                xmlConfigFields.Add( new FoldersCollection
                {
                    FolderPath = backup.FolderPath,
                    IncludeSubfolders = backup.IncludeSubfolders,
                } );
            }
        }

        public async Task StartFileSystemWatcher()
        {
            try
            {
                await Task.Run( () =>
                {
                    // Creates a new instance of the list
                    listFileSystemWatcher = new List<FileSystemWatcher>();

                    // Loop the list to process each of the folder specifications found
                    foreach ( var folder in xmlConfigFields )
                    {
                        DirectoryInfo dir = new DirectoryInfo( folder.FolderPath );
                        if ( dir.Exists )
                        {
                            // Creates a new instance of FileSystemWatcher
                            FileSystemWatcher fileSysWatch = new FileSystemWatcher
                            {
                                IncludeSubdirectories = folder.IncludeSubfolders == true,
                                InternalBufferSize = 64000,

                                // Sets the filter
                                Filter = "*",

                                // Sets the folder location
                                Path = folder.FolderPath,

                                // Subscribe to notify filters
                                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
                            };

                            // Associate the event that will be triggered when a new file
                            // is added to the monitored folder, using a lambda expression                   
                            fileSysWatch.Created += ( senderObj, fileSysArgs ) => FileSWatch_Created( senderObj, fileSysArgs );

                            fileSysWatch.Changed += ( senderObj, fileSysArgs ) => FileSWatch_Changed( senderObj, fileSysArgs );

                            fileSysWatch.Error += ( senderObj, fileSysArgs ) => FileSysWatch_Error( senderObj, fileSysArgs );

                            // Begin watching
                            fileSysWatch.EnableRaisingEvents = true;

                            // Add the systemWatcher to the list
                            listFileSystemWatcher.Add( fileSysWatch );

                            // Record a log entry into Windows Event Log
                            logger( LogLevel.Info, $"Starting to monitor source directory {fileSysWatch.Path}" );
                        }
                    }
                } );
            }
            catch ( Exception ex )
            {
                logger( LogLevel.Error, $"Error while setting FileSystemWatcher: {ex.Message}\n{ex.StackTrace}\n{ex.StackTrace}" );
            }
        }

        private void FileSysWatch_Error( object senderObj, ErrorEventArgs fileSysArgs )
        {
            var exception = fileSysArgs.GetException();
            logger( LogLevel.Error, $"FileSystemWatcher has encountered an error: {exception.Message}\n{exception.StackTrace}\n{exception.StackTrace}" );
        }

        private void FileSWatch_Changed( object senderObj, FileSystemEventArgs fileSysArgs )
        {
            logger(LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" was just changed" );
        }

        private void FileSWatch_Created( object senderObj, FileSystemEventArgs fileSysArgs )
        {
            logger( LogLevel.Info, $"File \"{fileSysArgs.Name}\" in directory \"{Path.GetDirectoryName( fileSysArgs.FullPath )}\" was just created" );
        }
    }
}
