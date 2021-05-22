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
using System.Xml.Serialization;
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
        private BackgroundWorker _bwFindFiles;

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
        bool isFindOnly;

        public MainViewModel()
        {
            isFindOnly = false;
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
            //List<string> files = new List<string>();
            var files = SearchFiles(DirectoryPath, FileMask, ExcludeFileMask, IncludeSubDirectories).ToList();
            TotalCountFiles = files.Count();
            int count = TotalCountFiles;
            string findText = FindText;
            string replaceText = ReplaceText;
            RenderedCountFiles = 0;
            int matches = 0;

            int progress = 0;
            int i = 0;
            foreach (var file in files)
            {
                if (_bwFindFiles.CancellationPending)
                {
                    isFindOnly = false;
                    e.Cancel = true;
                    return;
                }

                progress = (int)(((double)(i + 1) / (double)count) * 100);

                try
                {
                    string textFile = File.ReadAllText(file);
                    matches = SearchText(textFile, findText);
                    if (isFindOnly == false)
                    {
                        textFile = textFile.Replace(findText, replaceText);
                        File.WriteAllText(file, textFile);
                    }
                }
                catch
                {
                    matches = 0;
                    foreach (var line in File.ReadLines(file))
                    {
                        if (_bwFindFiles.CancellationPending)
                        {
                            isFindOnly = false;
                            e.Cancel = true;
                            return;
                        }
                        matches += SearchText(line, findText);
                        if (isFindOnly == false)
                        {
                            line.Replace(findText, replaceText);
                        }
                    }
                }

                if (matches == 0)
                {
                    i++;
                    bw.ReportProgress(progress);
                    RenderedCountFiles++;
                    continue;
                }
                bw.ReportProgress(progress, new DataTable()
                {
                    FileName = getNameFromPath(file),
                    FilePath = getPathWithoutName(file),
                    Matches = matches
                });
                i++;
                RenderedCountFiles++;
                Thread.Sleep(1);
            }
            e.Result = progress;
        }

        private void BwFindFilesRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;
            isFindOnly = false;
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

        private string _replaceText;
        public string ReplaceText
        {
            get => _replaceText;
            set
            {
                _replaceText = value;
                OnPropertyChanged(nameof(ReplaceText));
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

        public ICommand ReplaceButton
        {
            get => new RelayCommand(() =>
            {
                FoundFilesCollection.Clear();
                IsBusy = true;
                isFindOnly = false;

                _bwFindFiles.RunWorkerAsync(this);
            }, () => (IsBusy == false) && (ReplaceText != null));
        }

        public ICommand FindOnlyButton
        {
            get => new RelayCommand(() =>
            {
                FoundFilesCollection.Clear();
                IsBusy = true;
                isFindOnly = true;

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
        private int SearchText(string text, string findText)
        {
            int matches = (text.Length - text.Replace(findText, "").Length) / findText.Length;
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

        public IEnumerable<string> SearchFiles(string path, string includeMask, string excludeMask, bool includeSubDirectories)
        {
            //List<string> sortFiles = new List<string>();
            //string[] masks = excludeMask.Split(new[] { '*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            //var files;
            var searchOption = includeSubDirectories == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            IEnumerable<string> sortFiles;
            string[] foundFiles = new string[0];
            string[] excludeFiles = new string[0];

            var allIncludeMasks = includeMask.Replace(" ", "").Split(',');
            var allExcludeMasks = excludeMask?.Replace(" ", "").Split(',');

            foreach (var include in allIncludeMasks)
            {
                foundFiles = foundFiles.Concat(Directory.GetFiles(path, include, searchOption)).ToArray();
            }

            if (string.IsNullOrWhiteSpace(excludeMask) == false)
            {
                foreach (var exclude in allExcludeMasks)
                {
                    excludeFiles = excludeFiles.Concat(Directory.GetFiles(path, exclude, searchOption)).ToArray();
                }
            }

            sortFiles = string.IsNullOrWhiteSpace(excludeMask) == false
                ? foundFiles.Except(excludeFiles).AsEnumerable()
                : foundFiles.AsEnumerable();

            /*if (includeSubDirectories) { var files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, searchOption); }
            else { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, searchOption); }*/

            /*int lenght = files.Length; //use for debugging
            bool isChecked = true;
            foreach (string file in files)
            {
                foreach (string m in masks)
                {
                    if (file.IndexOf(m) != -1) isChecked = false;
                }
                if (isChecked) sortFiles.Add(file);
            }*/
            return sortFiles;
        }
    }
}
