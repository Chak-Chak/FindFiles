using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
//using FindFiles.Annotations;
using FindFiles.Commands;
using FindFiles.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell.Interop;

namespace FindFiles.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        private BackgroundWorker _bwFindFiles = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        private ObservableCollection<MainModel> _foundFilesCollection;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<MainModel> FoundFilesCollection
        {
            get => _foundFilesCollection;
            set
            {
                _foundFilesCollection = value;
                OnPropertyChanged(nameof(_foundFilesCollection));
            }
        }

        public MainViewModel()
        {
            FileMask = "*.*";
            ExcludeFileMask = "*.dll, *.exe";
            //_foundFilesCollection = new ObservableCollection<MainModel>();
            /*_bwFindFiles = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _bwFindFiles.DoWork += BwFindFilesDoWork;
            _bwFindFiles.ProgressChanged += BwFindFilesProgressChanged;
            _bwFindFiles.RunWorkerCompleted += BwFindFilesRunWorkerCompleted;*/
        }

        private void BwFindFilesProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            Progress = args.ProgressPercentage;
            //var model = (ReportModel)e.UserState;
            //ProgressMessage = model.Message;
        }

        private void BwFindFilesDoWork(object sender, DoWorkEventArgs args)
        {
            var vm = args.Argument as MainViewModel;
            var bw = (BackgroundWorker) sender;
            string[] files = Directory.GetDirectories(DirectoryPath);
            TotalCountFiles = files.Length;

            var result = 0;

            for (int i = 0; i < 100; i++)
            {
                result += i;
                Thread.Sleep(TimeSpan.FromMilliseconds(1));
                bw.ReportProgress(result);
            }
            args.Result = result;
        }

        private void BwFindFilesRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            IsBusy = false;
            _bwFindFiles.DoWork -= BwFindFilesDoWork;
            _bwFindFiles.RunWorkerCompleted -= BwFindFilesRunWorkerCompleted;
            _bwFindFiles.ProgressChanged -= BwFindFilesProgressChanged;
            CommandManager.InvalidateRequerySuggested();
        }

        private string _directoryPath;

        public string DirectoryPath
        {
            get => _directoryPath;
            set
            {
                _directoryPath = value;
                OnPropertyChanged(nameof(DirectoryPath));
            }
        }

        private int _progress;

        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress == value) return;
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private int _renderedCountFiles;
        public int RenderedCountFiles
        {
            get => _renderedCountFiles;
            set
            {
                _renderedCountFiles = value;
                OnPropertyChanged(nameof(RenderedCountFiles));
            }
        }

        private int _totalCountFiles;
        public int TotalCountFiles
        {
            get => _totalCountFiles;
            set
            {
                _totalCountFiles = value;
                OnPropertyChanged(nameof(TotalCountFiles));
            }
        }

        private string _fileMask;
        public string FileMask
        {
            get => _fileMask;
            set
            {
                _fileMask = value;
                OnPropertyChanged(nameof(FileMask));
            }
        }

        private string _excludeFileMask;

        public string ExcludeFileMask
        {
            get => _excludeFileMask;
            set
            {
                _excludeFileMask = value;
                OnPropertyChanged(nameof(ExcludeFileMask));
            }
        }

        public ICommand OpenDirectoryButton
        {
            get => new RelayCommand(() =>
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.InitialDirectory = "C:\\";
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) DirectoryPath = dialog.FileName;
            }, () => true);
        }

        public ICommand FindOnlyButton
        {
            get => new RelayCommand(() =>
            {
                _bwFindFiles.DoWork += BwFindFilesDoWork;
                _bwFindFiles.RunWorkerCompleted += BwFindFilesRunWorkerCompleted;
                _bwFindFiles.ProgressChanged += BwFindFilesProgressChanged;
                IsBusy = true;

                _bwFindFiles.RunWorkerAsync(this);
            }, () => IsBusy == false);
        }
    }
}
