using System;
using System.Collections;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<DataTable> _foundFilesCollection;
        public ObservableCollection<DataTable> FoundFilesCollection
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
            DirectoryPath = "C:\\";
            FileMask = "*.*";
            ExcludeFileMask = "*.dll, *.exe";
             _foundFilesCollection = new ObservableCollection<DataTable>();
            _bwFindFiles = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _bwFindFiles.DoWork += BwFindFilesDoWork;
            _bwFindFiles.ProgressChanged += BwFindFilesProgressChanged;
            _bwFindFiles.RunWorkerCompleted += BwFindFilesRunWorkerCompleted;
        }

        private void BwFindFilesProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
            if (e.UserState as DataTable != null) FoundFilesCollection.Add(e.UserState as DataTable);
            //var model = (ReportModel)e.UserState;
            //ProgressMessage = model.Message;
        }

        private void BwFindFilesDoWork(object sender, DoWorkEventArgs e)
        {
            var vm = e.Argument as MainViewModel;
            var bw = (BackgroundWorker) sender;
            List<string> files = new List<string>();
            files = SearchFiles(DirectoryPath, FileMask, ExcludeFileMask, IncludeSubDirectories);
            TotalCountFiles = files.Count;
            int count = TotalCountFiles;
            string text = FindText;
            RenderedCountFiles = 0;
            int matches = 0;

            var result = 0;

            for (int i = 0; i < count; i++)
            {
                matches = 0;
                if (_bwFindFiles.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                foreach (var line in File.ReadLines(files[i]))
                {
                    matches = SearchText(line, text);
                }

                result = (int)(((float)(i + 1) / (float)count) * 100);
                if (matches != 0)
                    bw.ReportProgress(result, new DataTable()
                    {
                        FileName = getNameFromPath(files[i]),
                        FilePath = getPathWithoutName(files[i]),
                        Matches = matches
                    });
                else bw.ReportProgress(result);
                RenderedCountFiles++;
                Thread.Sleep(1);
            }
            e.Result = result;
        }

        private void BwFindFilesRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            /*_bwFindFiles.DoWork -= BwFindFilesDoWork;
            _bwFindFiles.RunWorkerCompleted -= BwFindFilesRunWorkerCompleted;
            _bwFindFiles.ProgressChanged -= BwFindFilesProgressChanged;*/
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


        private bool _includeSubDirectories;
        public bool IncludeSubDirectories
        {
            get => _includeSubDirectories;
            set
            {
                _includeSubDirectories = value;
                OnPropertyChanged(nameof(IncludeSubDirectories));
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


        private string _findText;
        public string FindText
        {
            get => _findText;
            set
            {
                _findText = value;
                OnPropertyChanged(nameof(FindText));
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
            }, () => IsBusy == false);
        }

        public ICommand FindOnlyButton
        {
            get => new RelayCommand(() =>
            {
                FoundFilesCollection.Clear();
                IsBusy = true;

                _bwFindFiles.RunWorkerAsync(this);
            }, () => IsBusy == false);
        }

        public ICommand CancelButton
        {
            get => new RelayCommand(() =>
            {
                if (IsBusy)
                {
                    _bwFindFiles.CancelAsync();
                }
            }, () => IsBusy);
        }
        public class DataTable
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public int Matches { get; set; }
        }
        private int SearchText(string line, string text)
        {
            int matches = (line.Length - line.Replace(text, "").Length) / text.Length;
            return matches;
        }

        private string getNameFromPath (string path)
        {
            return System.IO.Path.GetFileName(path); //get file name
        }

        private string getPathWithoutName(string path)
        {
            return ".\\" + System.IO.Path.GetDirectoryName(path).Remove(0, DirectoryPath.Length); //get path to file
        }

        public List<string> SearchFiles(string path, string includeMask, string excludeMask, bool includeSubDirectories)
        {
            List<string> sortFiles = new List<string>();
            string[] masks = excludeMask.Split(new[] { '*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            string[] files;
            if (includeSubDirectories) { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, SearchOption.AllDirectories); }
            else { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, SearchOption.TopDirectoryOnly); }
            int lenght = files.Length; //use for debugging
            bool isChecked = true;
            foreach (string f in files)
            {
                foreach (string m in masks)
                {
                    if (f.IndexOf(m) != -1) isChecked = false;
                }
                if (isChecked) sortFiles.Add(f);
            }
            return sortFiles;
        }
    }
}
