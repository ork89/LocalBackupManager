using Client.Models;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Client.Infrastructure
{
    public class HandleXMLConfigFile
    {
        private static readonly string configFilePath = ConfigurationManager.AppSettings["configFilePath"];

        public static Task<string> CreateNewXmlConfigFile( ObservableCollection<FoldersCollection> foldersToBackup )
        {
            var targetPath = HandleConfigFileTargetPath();

            try
            {
                using ( XmlTextWriter xmltextWriter = new XmlTextWriter( targetPath, Encoding.UTF8 ) { Formatting = Formatting.Indented } )
                {
                    xmltextWriter.WriteStartDocument();
                    xmltextWriter.WriteStartElement( "FoldersCollection" );
                    xmltextWriter.WriteAttributeString( "xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance" );
                    xmltextWriter.WriteAttributeString( "xmlns:xsd", "http://www.w3.org/2001/XMLSchema" );

                    foreach ( var folder in foldersToBackup )
                    {
                        xmltextWriter.WriteStartElement( $"SourceFolder" );
                        xmltextWriter.WriteElementString( "Id", folder.Id );
                        xmltextWriter.WriteElementString( "BackupName", folder.BackupName );
                        xmltextWriter.WriteElementString( "SourcePath", folder.SourcePath );
                        xmltextWriter.WriteElementString( "DestinationPath", folder.DestinationPath );
                        xmltextWriter.WriteElementString( "FolderSize", folder.FolderSize );
                        xmltextWriter.WriteElementString( "BackupLimit", folder.BackupLimit.ToString() );
                        xmltextWriter.WriteElementString( "IncludeSubfolders", folder.IncludeSubfolders.ToString().ToLower() );
                        xmltextWriter.WriteElementString( "IsIncrementalBackup", folder.IsIncrementalBackup.ToString().ToLower() );
                        xmltextWriter.WriteElementString( "IsDifferentialBackup", folder.IsDifferentialBackup.ToString().ToLower() );
                        xmltextWriter.WriteElementString( "IsSchedualedBackup", folder.IsSchedualedBackup.ToString().ToLower() );
                        xmltextWriter.WriteElementString( "IsArchive", folder.IsArchive.ToString().ToLower() );
                        xmltextWriter.WriteElementString( "IsAutomatic", folder.IsAutomatic.ToString().ToLower() );
                        xmltextWriter.WriteEndElement();
                    }

                    xmltextWriter.WriteEndElement();
                    xmltextWriter.Flush();
                    xmltextWriter.Close();
                }               
            }
            catch ( Exception exc )
            {
                Logger.WriteToLog( LogLevel.Error, $"{exc.Message}\n{exc.StackTrace}" );
            }

            return Task.FromResult( targetPath );
        }

        private static string HandleConfigFileTargetPath()
        {
            var targetPath = string.Empty;
            var initialPath = Environment.GetFolderPath( Environment.SpecialFolder.CommonApplicationData );

            try
            {
                if ( !Directory.Exists( configFilePath ) )
                    Directory.CreateDirectory( Path.Combine( initialPath, "BackupManager" ) );

                targetPath = Path.Combine( configFilePath, "BackupManager.config" );

                if ( !File.Exists( targetPath ) )
                    File.Create( targetPath );
            }
            catch ( Exception exc )
            {
                Logger.WriteToLog( LogLevel.Error, $"{exc.Message}\n{exc.StackTrace}" );
            }

            return targetPath;
        }

        public static async Task<ObservableCollection<FoldersCollection>> GetListOfBackupsFromConfigFile()
        {
            var configFile = Path.Combine( configFilePath, "BackupManager.config" );
            var backupList = new BackupsData();

            try
            {
                using ( FileStream fileStream = File.OpenRead( configFile ) )
                {
                    XmlSerializer serializer = new XmlSerializer( typeof( BackupsData ), new XmlRootAttribute( "FoldersCollection" ) );
                    backupList = (BackupsData)serializer.Deserialize( fileStream );
                }
            }
            catch (Exception exc)
            {
                Logger.WriteToLog( LogLevel.Error, $"Error while trying to retrieve backups list from config file.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
            }

            return await Task.FromResult( backupList.FoldersCollectionList.Any() ? backupList.FoldersCollectionList : new ObservableCollection<FoldersCollection>() );            
        }

        public static async Task EditBackupConfigFile(FoldersCollection backupItem)
        {
            var backupsList = await GetListOfBackupsFromConfigFile();

            foreach ( var backup in backupsList )
            {
                if ( backup.Id == backupItem.Id )
                {
                    backup.BackupName = backupItem.BackupName;
                    backup.SourcePath = backupItem.SourcePath;
                    backup.DestinationPath = backupItem.DestinationPath;
                    backup.BackupLimit = backupItem.BackupLimit;
                    backup.IncludeSubfolders= backupItem.IncludeSubfolders;
                    backup.IsDifferentialBackup = backupItem.IsDifferentialBackup;
                    backup.IsIncrementalBackup = backupItem.IsIncrementalBackup;
                    backup.IsArchive = backupItem.IsArchive;
                    backup.IsAutomatic = backupItem.IsAutomatic;
                    backup.IsSchedualedBackup = backupItem.IsSchedualedBackup;
                }                    
            }

            await CreateNewXmlConfigFile( backupsList );
            Logger.WriteToLog( LogLevel.Info, $"\"{backupItem.BackupName}\" Updated Successfully" );
        }

        public static void DeleteBackup(string backupId)
        {
            var configFile = Path.Combine( configFilePath, "BackupManager.config" );
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load( configFile );

                XmlNode node = xDoc.SelectSingleNode( $"//SourceFolder[Id='{backupId}']" );
                node.ParentNode.RemoveChild( node );
                xDoc.Save(configFile);
            }
            catch ( Exception exc )
            {
                Logger.WriteToLog( LogLevel.Error, $"Error while trying to delete a backup with ID: {backupId}.\nError: {exc.Message}\nStack Trace: {exc.StackTrace}" );
            }
        }
    }
}
