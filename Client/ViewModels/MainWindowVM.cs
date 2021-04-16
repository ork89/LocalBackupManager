using Client.Infrastructure;
using Client.Models;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            _isExecuteBtnEnabled = true;
        }

        //Commands
        public ICommand CreateNewBackupCommand => new AsyncCommand( async () => await CreateNewBackup(), CanExecuteAsync );
        public ICommand RestoreBackupCommand => new AsyncCommand( async () => await RestoreBackup(), CanExecuteAsync );
        public ICommand BrowseFoldersCommand => new RelayCommand( BrowseFolders, param => CanExecute );
        public ICommand ExecuteBackupCommand => new AsyncCommand( async () => await ExecuteBackup(), CanExecuteAsync );

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
                if ( value != _selectedSourcePath )
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
                if ( value != _selectedDestinationPath )
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

                    if ( value )
                        DiffCheckboxChecked = false;
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

                    if ( value )
                        DiffCheckboxChecked = true;
                }
            }
        }

        private bool _diffCheckboxChecked;
        public bool DiffCheckboxChecked
        {
            get { return _diffCheckboxChecked; }
            set
            {
                if ( value != _diffCheckboxChecked )
                {
                    _diffCheckboxChecked = value;
                    OnPropertyChanged( "DiffCheckboxChecked" );

                    if ( value )
                        IsIncrementalBackup = false;
                    else
                        IsDifferentialBackup = false;
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

        private bool _isAutomatic;
        public bool IsAutomatic
        {
            get { return _isAutomatic; }
            set
            {
                if ( value != _isAutomatic )
                {
                    _isAutomatic = value;
                    OnPropertyChanged( "IsAutomatic" );
                }
            }
        }
        
        private bool _isExecuteBtnEnabled;
        public bool IsExecuteBtnEnabled
        {
            get { return _isExecuteBtnEnabled; }
            set
            {
                if ( value != _isExecuteBtnEnabled )
                {
                    _isExecuteBtnEnabled = value;
                    OnPropertyChanged( "IsExecuteBtnEnabled" );
                }
            }
        }


        private ObservableCollection<FoldersCollection> _foldersList;
        public ObservableCollection<FoldersCollection> FoldersList
        {
            get { return _foldersList; }
            set
            {
                if ( value != _foldersList )
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
                if ( value != this._canExecute )
                    this._canExecute = value;
            }
        }

        private bool CanExecuteAsync() { return true; }

        //Methods
        private async Task InitBackupsList()
        {
            logger( LogLevel.Info, "Loading config file." );
            FoldersList = await HandleXMLConfigFile.GetListOfBackupsFromConfigFile().ConfigureAwait( false );

            foreach ( var folder in FoldersList )
            {
                folder.FolderSize = GetFolderSize( folder.FolderPath, folder.IncludeSubfolders );
            }
        }

        private void ClosingEventHandler( object sender, DialogClosingEventArgs eventArgs )
            => Debug.WriteLine( "You can intercept the closing event, and cancel here." );

        private void BrowseFolders( object obj )
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

        async Task CreateNewBackup()
        {
            var result = await DialogHost.Show( this );

            if ( result == null || !(bool)result )
                return;

            var folderSize = GetFolderSize( SelectedSourcePath, IsSubfoldersIncluded );

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

            await HandleXMLConfigFile.CreateNewXmlConfigFile( FoldersList ).ConfigureAwait( false );
            //await CreateBackupScript.CreateNewBackupScript( FoldersList.ToList() ).ConfigureAwait( false );

            logger( LogLevel.Info, "New backup created" );
        }

        async Task RestoreBackup()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.SpecialFolder.MyComputer.ToString(),
                Filter = "*"
            };

            dialog.ShowDialog();
        }

        async Task ExecuteBackup()
        {
            IsExecuteBtnEnabled = false;
            await ExecuteBackups.ExecuteBackupScript( _foldersList.ToList() );
            IsExecuteBtnEnabled = true;
        }

        // Helper Methods
        private string GetFolderSize( string directory = "", bool isSubfoldersIncluded = false )
        {
            long size = 0;

            logger( LogLevel.Info, $"Calculating directory size for {directory}" );

            try
            {
                if ( Directory.Exists( directory ) == false )
                {
                    return "0";
                }

                DirectoryInfo dirInfo = new DirectoryInfo( directory );

                // Add file sizes.
                IEnumerable<FileInfo> fis = dirInfo.EnumerateFiles( "*.*", SearchOption.AllDirectories );
                foreach ( FileInfo fi in fis )
                {
                    size += fi.Length;
                }
            }
            catch ( Exception exc )
            {
                logger( LogLevel.Error, $"{exc.Message}\n{exc.StackTrace}" );
            }

            var formattedSize = FormatSize( size );

            logger( LogLevel.Info, $"Directory size: {formattedSize}" );
            return formattedSize;
        }

        private string FormatSize( long size )
        {
            // Load all suffixes in an array  
            string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

            int counter = 0;
            decimal number = (decimal)size;
            while ( Math.Round( number / 1024 ) >= 1 )
            {
                number /= 1024;
                counter++;
            }

            return string.Format( "{0:n1}{1}", number, suffixes[counter] );
        }
    }
}
