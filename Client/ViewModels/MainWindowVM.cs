using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace Backup_Manager.ViewModels
{
    public class MainWindowVM : BaseViewModel
    {
        public MainWindowVM()
        {
            BrowseFoldersCommand = new RelayCommand(BrowseFolders, param => this.CanExecute);
            BrowseFilesCommand = new RelayCommand(BrowseFiles, param => this.CanExecute);
        }

        //Commands
        public ICommand BrowseFoldersCommand { get; set; }
        public ICommand BrowseFilesCommand { get; set; }

        //Properties
        private string _selectedPath;
        public string SelectedPath
        {
            get { return _selectedPath; }
            set { _selectedPath = value; RaisePropertyChanged("SelectedPath"); }
        }

        public ObservableCollection<string> FoldersList { get; set; }
        public ObservableCollection<string> FilesList { get; set; }

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
        private void BrowseFolders(object obj)
        {
            //var folderDialog = new FolderBrowserDialog();
        }

        private void BrowseFiles(object obj)
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = "",
                Filter = ""
            };

            dialog.ShowDialog();
        }
    }
}
