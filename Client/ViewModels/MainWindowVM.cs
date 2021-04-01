using Client.Models;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace Backup_Manager.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        private const string DialogIdentifier = "rootDialog";

        public MainWindowVM()
        {
            //Mock FoldersList
            FoldersList = new ObservableCollection<FolderModel>();
            FoldersList.Add(new FolderModel { FolderName = "Repos", FolderPath = @"C:\Users\oriko\source\repos", DestinationPath = @"C:\Users\oriko\OneDrive - Matrix IT Ltd", FolderSize = "125.80", IncludeSubfolders = true } );
            FoldersList.Add(new FolderModel { FolderName = "Documents", FolderPath = @"C:\Users\oriko\Documents", DestinationPath = @"C:\Temp", FolderSize = "34.12", IncludeSubfolders = true } );
            FoldersList.Add(new FolderModel { FolderName = "Desktop", FolderPath = @"C:\Users\oriko\Desktop", DestinationPath = @"C:\Users\oriko\Documents\DesktopBackup", FolderSize = "4.5", IncludeSubfolders = false } );
        }

        //Commands
        public ICommand CreateNewBackupCommand => new RelayCommand( CreateNewBackup, param => CanExecute );
        public ICommand RestoreBackupCommand => new RelayCommand( RestoreBackup, param => CanExecute );
        public ICommand BrowseFoldersCommand => new RelayCommand( BrowseFolders, param => CanExecute );

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

        private string _numberOfBackupsLimit;
        public string NumberOfBackupsLimit
        {
            get { return _numberOfBackupsLimit; }
            set
            {
                if ( value != _numberOfBackupsLimit )
                {
                    _numberOfBackupsLimit = value;
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

        private ObservableCollection<FolderModel> _foldersList;
        public ObservableCollection<FolderModel> FoldersList
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

        private async void CreateNewBackup(object obj)
        {            
            var result = await DialogHost.Show(this);

            if(result != null && (bool)result)
            {
                var folderSize = GetFolderSize();

                FoldersList.Add( new FolderModel
                {
                    FolderName = BackupName,
                    FolderPath = SelectedSourcePath,
                    DestinationPath = SelectedDestinationPath,
                    IncludeSubfolders = IsSubfoldersIncluded,
                    FolderSize = folderSize
                } );
            }
            
            Debug.WriteLine( $"Dialog was closed, the CommandParameter used to close it was: {result ?? "NULL"}" );
        }

        private string GetFolderSize()
        {
            var fileSize = 0.0;
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

            var totalSize = (fileSize / 1024) / 1024;
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
    }
}
