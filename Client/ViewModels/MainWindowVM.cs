using Client.Infrastructure;
using Client.Models;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Backup_Manager.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        private readonly WriteToLog logger = Logger.WriteToLog;
        public MainWindowVM()
        {
            _foldersList = new ObservableCollection<FoldersCollection>();
            InitBackupsList();
        }

        //Commands
        public ICommand CreateNewBackupCommand => new RelayCommand( CreateNewBackup, param => CanExecute );
        public ICommand RestoreBackupCommand => new RelayCommand( RestoreBackup, param => CanExecute );
        public ICommand BrowseFoldersCommand => new RelayCommand( BrowseFolders, param => CanExecute );
        public ICommand ExecuteBackupCommand => new RelayCommand( ExecuteBackup, param => CanExecute );

        //Properties
        private string _backupName;
        public string BackupName
        {
            get { return _backupName; }
            set
            {
                if ( value != _backupName )
                {
                    _backupName = value;
                    OnPropertyChanged( "BackupName" );
                }
            }
        }

        private string _selectedSourcePath;
        public string SelectedSourcePath
        {
            get { return _selectedSourcePath; }
            set 
            { 
                if(value != _selectedSourcePath)
                {
                    _selectedSourcePath = value;
                    OnPropertyChanged( "SelectedSourcePath" );
                }                
            }
        }

        private string _selectedDestinationPath;
        public string SelectedDestinationPath
        {
            get { return _selectedDestinationPath; }
            set 
            { 
                if(value != _selectedDestinationPath)
                {
                    _selectedDestinationPath = value;
                    OnPropertyChanged( "SelectedDestinationPath" );
                }                
            }
        }

        private int _backupLimit;
        public int BackupLimit
        {
            get { return _backupLimit; }
            set
            {
                if ( value != _backupLimit )
                {
                    _backupLimit = value;
                    OnPropertyChanged( "NumberOfBackupsLimit" );
                }
            }
        }

        private bool _isSubfoldersIncluded;
        public bool IsSubfoldersIncluded
        {
            get { return _isSubfoldersIncluded; }
            set
            {
                if ( value != _isSubfoldersIncluded )
                {
                    _isSubfoldersIncluded = value;
                    OnPropertyChanged( "IsSubfoldersIncluded" );
                }
            }
        }

        private bool _isIncrementalBackup;
        public bool IsIncrementalBackup
        {
            get { return _isIncrementalBackup; }
            set
            {
                if ( value != _isIncrementalBackup )
                {
                    _isIncrementalBackup = value;
                    OnPropertyChanged( "IsIncrementalBackup" );
                }
            }
        }

        private bool _isDifferentialBackup;
        public bool IsDifferentialBackup
        {
            get { return _isDifferentialBackup; }
            set
            {
                if ( value != _isDifferentialBackup )
                {
                    _isDifferentialBackup = value;
                    OnPropertyChanged( "IsDifferentialBackup" );
                }
            }
        }

        private bool _isSchedualedBackup;
        public bool IsSchedualedBackup
        {
            get { return _isSchedualedBackup; }
            set
            {
                if ( value != _isSchedualedBackup )
                {
                    _isSchedualedBackup = value;
                    OnPropertyChanged( "IsSchedualedBackup" );
                }
            }
        }

        private bool _isArchive;
        public bool IsArchive
        {
            get { return _isArchive; }
            set
            {
                if ( value != _isArchive )
                {
                    _isArchive = value;
                    OnPropertyChanged( "IsArchive" );
                }
            }
        }

        private ObservableCollection<FoldersCollection> _foldersList;
        public ObservableCollection<FoldersCollection> FoldersList
        {
            get { return _foldersList; }
            set
            {
                if(value != _foldersList)
                {
                    _foldersList = value;
                    OnPropertyChanged( "FoldersList" );
                }
            }

        }

        private bool _canExecute = true;
        public bool CanExecute
        {
            get
            {
                return this._canExecute;
            }

            set
            {
                if (value != this._canExecute)
                    this._canExecute = value;
            }
        }

        //Methods
        private async void InitBackupsList()
        {
            logger( LogLevel.Info, "Loading config file." );
            FoldersList = await HandleXMLConfigFile.GetListOfBackupsFromConfigFile().ConfigureAwait(false);
        }

        private async void CreateNewBackup(object obj)
        {
            var result = await DialogHost.Show(this);

            if (result != null && (bool)result)
            {
                var folderSize = GetFolderSize();

                FoldersList.Add( new FoldersCollection
                {
                    BackupName = BackupName,
                    FolderPath = SelectedSourcePath,
                    DestinationPath = SelectedDestinationPath,
                    IncludeSubfolders = IsSubfoldersIncluded,
                    FolderSize = folderSize,
                    BackupLimit = BackupLimit,
                    IsIncrementalBackup = IsIncrementalBackup,
                    IsDifferentialBackup = IsDifferentialBackup,
                    IsSchedualedBackup = IsSchedualedBackup,
                    IsArchive = IsArchive
                } );

                await HandleXMLConfigFile.CreateNewXmlConfigFile( FoldersList ).ConfigureAwait(false);
                await CreateBackupScript.CreateNewBackupScript(FoldersList.ToList()).ConfigureAwait(false);
            }
        }

        private string GetFolderSize()
        {
            var fileSize = 0.0;
            var totalSize = 0.0;
            try
            {
                var dirInfo = new DirectoryInfo( SelectedSourcePath );

                if ( IsSubfoldersIncluded )
                {
                    var dirList = dirInfo.EnumerateDirectories().ToList();

                    foreach ( var dir in dirList )
                    {
                        fileSize = dir.EnumerateFiles().Sum( file => file.Length );
                    }
                }
                else
                {
                    fileSize = dirInfo.EnumerateFiles().Sum( file => file.Length );
                }

                totalSize = ( fileSize / 1024 ) / 1024;
            }
            catch ( Exception exc )
            {
                logger(LogLevel.Error, $"{exc.Message}\n{exc.StackTrace}");            }

            return totalSize.ToString( "F" );
        }

        private void ClosingEventHandler( object sender, DialogClosingEventArgs eventArgs )
            => Debug.WriteLine( "You can intercept the closing event, and cancel here." );

        private void RestoreBackup(object obj)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.SpecialFolder.MyComputer.ToString(),
                Filter = ""
            };

            dialog.ShowDialog();
        }

        private void BrowseFolders(object obj)
        {
            var folderDialog = new FolderBrowserDialog();

            folderDialog.RootFolder = Environment.SpecialFolder.MyComputer;
            folderDialog.ShowNewFolderButton = true;
            DialogResult dialogResult = folderDialog.ShowDialog();

            if ( dialogResult == DialogResult.None || dialogResult == DialogResult.Cancel )
                return;

            if ( (string)obj == "SourceFolder" )
                SelectedSourcePath = folderDialog.SelectedPath;
            else
                SelectedDestinationPath = folderDialog.SelectedPath;
        }

        private void ExecuteBackup(object obj)
        {
            ExecuteBackups.ExecuteBackupScript(_foldersList.ToList());
        }
    }
}
