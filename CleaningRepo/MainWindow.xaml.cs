using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
//using System.Windows.Shapes;


namespace CleaningRepo
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        HashSet<string> FilesToFind = new HashSet<string>();
        HashSet<string> FilesFromRepository = new HashSet<string>();
        HashSet<string> RemovedFiles = new HashSet<string>();
        HashSet<string> unusedFiles = new HashSet<string>();
        HashSet<string> excludedFiles = new HashSet<string>();
        HashSet<string> suspiciousFiles = new HashSet<string>();
        Dictionary<string, FilesCounter> log = new Dictionary<string, FilesCounter>();
        string foldersAdded = "";
        int counter = 0;
        int filesPerThread = 10;
        DateTime startTime = DateTime.Now;
        string repoPath;
        //pobieranie ścieżki do folderu
        public static string ReadFilesFromFolder()
        {
            //dostęp do ścieżki plików
            var folderBrowserDialog = new FolderBrowserDialog()
            {
                ShowNewFolderButton = false
            };
            DialogResult path = folderBrowserDialog.ShowDialog();
            if (path == System.Windows.Forms.DialogResult.OK)
            {
                string folderName = folderBrowserDialog.SelectedPath;
                return folderName;
            }
            else return null;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Repository.IsEnabled = true;
            ProgramsToFind.IsEnabled = true;
            RunProgram.IsEnabled = true;
            AddExcludedFiles.IsEnabled = true;
            Clean.IsEnabled = false;
            ExcludedSources.Text = "";
            counter = 0;
            FilesToFind.Clear();
            FilesFromRepository.Clear();
            RemovedFiles.Clear();
            unusedFiles.Clear();
            suspiciousFiles.Clear();
            excludedFiles.Clear();
            log.Clear();
            foldersAdded = null;
            CzyOkProg.Text = foldersAdded;
            CzyOkRepo.Text = "";
            listViewFind.ItemsSource = DisplayResults(FilesToFind);
            listViewUnused.ItemsSource = DisplayResults(unusedFiles);
            progressBar.Value = 0;
        }

        private void ProgramsToFind_Click(object sender, RoutedEventArgs e)
        {
            string folder = ReadFilesFromFolder();

            if (folder != null)
            {
                foldersAdded += new DirectoryInfo(folder).Name + " \n";
                CzyOkProg.Text = foldersAdded;
                SearchDirsAndSubDirs(folder, FilesToFind, false);
                PrepareLog(log, FilesToFind);
                listViewFind.ItemsSource = DisplayResults(FilesToFind);
            }

            if (FilesToFind == null)
                System.Windows.MessageBox.Show("Lista plików do sprawdzenia jest pusta", "Uwaga!");
        }

        private void Clean_Click(object sender, RoutedEventArgs e)
        {

        }


        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            repoPath = ReadFilesFromFolder();
            if (repoPath != null)
            {
                CzyOkRepo.Text = repoPath;
                SearchDirsAndSubDirs(repoPath, FilesFromRepository, false);
            }

            if (FilesFromRepository == null)
                System.Windows.MessageBox.Show("Sciezka do repo pusta", "Uwaga!");
        }

        //tworzy listę programów do wyszukania
        void SearchDirsAndSubDirs(string dirToSearch, HashSet<string> ListOfFiles, bool ignoreOnline = true)
        {
            try
            {
                SearchCurDir(dirToSearch, ListOfFiles, ignoreOnline);
                //jeżeli są podkatalogi to idzie tu:
                foreach (string d in Directory.GetDirectories(dirToSearch))
                {
                    SearchDirsAndSubDirs(d, ListOfFiles, ignoreOnline);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        void SearchCurDir(string dirToSearch, HashSet<string> ListOfFiles, bool ignoreOnline)
        {
            foreach (string f in Directory.GetFiles(dirToSearch))
            {
                if (ignoreOnline)
                {
                    string tmpF = System.IO.Path.GetFileName(f);
                    if (tmpF.Length < 2 || tmpF[2] == '1' || tmpF[2] == '2')
                    {
                        Console.WriteLine("Ignored Online: " + tmpF);
                        continue;
                    }
                }
                ListOfFiles.Add(f);
            }
        }

        private void AddExcludedFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ExcludedSources.Text += Path.GetFileName(openFileDialog.FileName) + "\n";
                foreach (string s in File.ReadLines(openFileDialog.FileName))
                {
                    Match match = Regex.Match(s, "[A-Z][A-Z][1-2]C...");
                    if (match.Success)
                    {
                        excludedFiles.Add(match.Value);
                    }
                    match = Regex.Match(s, "[A-Z][A-Z]J[A-Z]...");
                    if (match.Success)
                        excludedFiles.Add(match.Value);
                }
            }
        }

        private void RunProgram_Click(object sender, RoutedEventArgs e)
        {
            Repository.IsEnabled = false;
            ProgramsToFind.IsEnabled = false;
            RunProgram.IsEnabled = false;
            AddExcludedFiles.IsEnabled = false;
            Clean.IsEnabled = true;
            try
            {
                // robimy kopię z plików do wyszukania
                unusedFiles = new HashSet<string>(FilesToFind);
                //usuwamy z listy używane pliki
                while (counter < FilesFromRepository.Count)
                {
                    FilterUnused(counter, (counter + filesPerThread < FilesFromRepository.Count ? counter + filesPerThread : FilesFromRepository.Count));
                    counter += filesPerThread;
                    progressBar.Dispatcher.Invoke(() => progressBar.Value = 100 * counter / FilesFromRepository.Count, DispatcherPriority.Background);
                }
                listViewUnused.ItemsSource = DisplayResults(unusedFiles);
                SaveLog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Wystąpił błąd:" + ex, "Uwaga!");
            }
        }

        void SaveLog()
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Zapisz plik"
            };
            saveFileDialog.ShowDialog();

            List<KeyValuePair<string,FilesCounter>> tmp = log.ToList();
            tmp.Sort((pair1,pair2)=>pair1.Value.CompareTo(pair2.Value));
            if (saveFileDialog.FileName != "")
            {
                
                StreamWriter sw = File.CreateText(saveFileDialog.FileName);
                sw.WriteLine("Start Time: "+ startTime);
                sw.WriteLine("End Time: "+ DateTime.Now);
                sw.WriteLine("Files in Repository {0}: "+FilesFromRepository.Count, repoPath);
                sw.WriteLine("Files searched for: "+FilesToFind.Count);
                sw.WriteLine("Folders: " + foldersAdded);
                sw.WriteLine("Unused found: " + unusedFiles.Count);
                foreach (var kvp in tmp)
                {
                    sw.Write(Path.GetFileName(kvp.Key)+" ");
                    sw.WriteLine(log[kvp.Key]);
                }
                sw.WriteLine("Suspicious Files:\n"+suspiciousFiles.Count);
                foreach(string s in suspiciousFiles)
                {
                    sw.WriteLine(s);
                }
                sw.WriteLine("Excluded Files:\n"+excludedFiles.Count);
                foreach(string s in excludedFiles)
                {
                    sw.WriteLine(s);
                }
                sw.Close();

            }
        }

        HashSet<string> DisplayResults(HashSet<string> results)
        {
            HashSet<string> lista = new HashSet<string>();
            foreach (string s in results)
            {
                lista.Add(Path.GetFileName(s));
            }
            return lista;
        }

        void FilterUnused(int start, int end)
        {

            List<string> filesList = new List<string>(FilesToFind);
            List<string> filesFromRepoList = new List<string>(FilesFromRepository);
            for (int i = start; i < end; i++)
            {
                if (!excludedFiles.Contains(Path.GetFileName(filesFromRepoList[i])))
                {
                    bool isJCL = filesFromRepoList[i].Contains("JCL");
                    CheckFile(filesFromRepoList[i], filesList, isJCL);
                }
            }
        }


        void CheckFile(string file, List<string> toCheck, bool isJCL)
        {
            HashSet<string> foundProgs = new HashSet<string>();
            try
            {
                int cnt = 0;
                string[] lines = File.ReadAllLines(file);
                foreach (string line in lines)
                {
                    cnt++;
                    bool verifyIfLineHasToBeChecked = isJCL ? !(line.Length > 6 && line[6] == '*' || line.Contains("PROGRAM-ID")) : !(line.Length > 6 && line[6] == '*' || line.Contains("PROGRAM-ID"));
                    if (verifyIfLineHasToBeChecked)
                    {
                        if (!Regex.Match(Path.GetFileName(file), "[A-Z][A-Z].....").Success)
                            suspiciousFiles.Add(file);
                        for (int i = toCheck.Count - 1; i > 0; i--)
                        {
                            if (line.Contains(Path.GetFileName(toCheck[i])))
                            {
                                log[toCheck[i]] += Path.GetFileName(file) + " in line "+ cnt +" ,";
                                unusedFiles.Remove(toCheck[i]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        private void PrepareLog(Dictionary<string,FilesCounter> Log, HashSet<string> Files)
        {
            foreach (string s in Files)
            {
                if (!Log.ContainsKey(s)) Log.Add(s, new FilesCounter());
            }
        }

        private void CloseProgram_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    class FilesCounter :IComparable
    {
        private int occurences = 0;
        public int Occurences { get { return occurences; } }

        private string foundLocation = "";

        public static FilesCounter operator +(FilesCounter FC, string s)
        {
            FC.foundLocation += " " + s;
            FC.occurences++;
            return FC;
        }

        public override string ToString()
        {
            return "Occurences:"+ occurences +" "+ foundLocation;
        }

        public int CompareTo(object obj)
        {
            FilesCounter fc = obj as FilesCounter;
            if (this.occurences < fc.Occurences)
                return -1;
            if (this.occurences == fc.Occurences)
                return 0;
            else
                return 1;
        }
    }
}
