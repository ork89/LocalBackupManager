using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Models
{
    [Serializable()]
    public class FoldersCollection
    {
        [XmlElement]
        public string BackupName { get; set; }

        [XmlElement]
        public string SourcePath { get; set; }

        [XmlElement]
        public string DestinationPath { get; set; }

        [XmlElement]
        public string FolderSize { get; set; }

        [XmlElement]
        public int BackupLimit { get; set; }

        [XmlElement]
        public bool IncludeSubfolders { get; set; }

        [XmlElement]
        public bool IsIncrementalBackup { get; set; }

        [XmlElement]
        public bool IsDifferentialBackup { get; set; }

        [XmlElement]
        public bool IsSchedualedBackup { get; set; }
        
        [XmlElement]
        public bool IsAutomatic { get; set; }

        [XmlElement]
        public bool IsArchive { get; set; }
    }

    [XmlTypeAttribute( AnonymousType = true )]
    public class BackupsData
    {
        [XmlElement( "SourceFolder" )]
        public ObservableCollection<FoldersCollection> FoldersCollectionList { get; set; }

        public BackupsData()
        {
            FoldersCollectionList = new ObservableCollection<FoldersCollection>();
        }
    }
}
