using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
        string foldersAdded = "";
        int counter = 0;
        int filesPerThread = 10;

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
                foldersAdded += new DirectoryInfo(folder).Name + "\n";
                CzyOkProg.Text = foldersAdded;
                SearchDirs(folder, FilesToFind, false);
                listViewFind.ItemsSource = DisplayResults(FilesToFind);
            }

            if (FilesToFind == null)
                System.Windows.MessageBox.Show("Lista plików do sprawdzenia jest pusta", "Uwaga!");
        }

        private void Clean_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog()
            {
                ShowNewFolderButton = false
            };
            DialogResult path = folderBrowserDialog.ShowDialog();
            if (path == System.Windows.Forms.DialogResult.OK)
            {
                string folderName = folderBrowserDialog.SelectedPath;
                foreach (string s in unusedFiles)
                {
                    string targetFolder = folderName+"\\"+ Path.GetFileName(Path.GetDirectoryName(s));

                    if (!System.IO.Directory.Exists(targetFolder))
                    {
                        System.IO.Directory.CreateDirectory(targetFolder);
                    }
                    System.IO.File.Copy(s, targetFolder + "\\" + Path.GetFileName(s), false);

                }
            }
        }


        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            string repositoryPath = ReadFilesFromFolder();
            if (repositoryPath != null)
            {
                CzyOkRepo.Text = repositoryPath;
                SearchDirs(repositoryPath, FilesFromRepository, false);
            }

            if (FilesFromRepository == null)
                System.Windows.MessageBox.Show("Sciezka do repo pusta", "Uwaga!");
        }

        //tworzy listę programów do wyszukania
        void SearchDirs(string dirToSearch, HashSet<string> ListOfFile, bool ignoreOnline = true)
        {
            try
            {
                //jeżeli są podkatalogi to idzie tu:
                foreach (string d in Directory.GetDirectories(dirToSearch))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        if (ignoreOnline)
                        {
                            string tmpF = Path.GetFileName(f);
                            if (tmpF.Length < 2 || tmpF[2] == '1' || tmpF[2] == '2')
                            {
                                Console.WriteLine("Ignored Online: " + tmpF);
                                continue;
                            }
                        }
                        ListOfFile.Add(f);
                    }
                    SearchDirs(d, ListOfFile, ignoreOnline);

                }
                //jeżeli nie ma podkatalogów to bierze pliki ze ścieżki
                if (ListOfFile.Count == 0)
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
                        ListOfFile.Add(f);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
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
                    string matched = Regex.Match(s, "[A-Z][A-Z][1-2]C...").Value;
                    if (matched != null)
                    {
                        excludedFiles.Add(matched);
                    }
                    matched = Regex.Match(s, "[A-Z][A-Z]J[A-Z]...").Value;
                    if (matched != null)
                        excludedFiles.Add(matched);
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
                SaveFiles();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Wystąpił błąd:" + ex, "Uwaga!");
            }
        }

        void SaveFiles()
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Zapisz plik"
            };
            saveFileDialog.ShowDialog();
            if (saveFileDialog.FileName != "")
            {
                StreamWriter sw = File.CreateText(saveFileDialog.FileName);
                foreach (string file in unusedFiles)
                {
                    sw.WriteLine(file);
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
                string[] lines = File.ReadAllLines(file);
                foreach (string line in lines)
                {
                    bool verifyIfLineHasToBeChecked = isJCL ? !(line.Length > 6 && line[6] == '*' || line.Contains("PROGRAM-ID")) : !(line.Length > 6 && line[6] == '*' || line.Contains("PROGRAM-ID"));
                    if (verifyIfLineHasToBeChecked)
                    {
                        for (int i = toCheck.Count - 1; i > 0; i--)
                        {
                            if (line.Contains(Path.GetFileName(toCheck[i])))
                            {
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

        //Dictionary<string, string> GetNameVariable(string[] lines)
        //{
        //    Dictionary<string, string> NamesOfVariables = new Dictionary<string, string>();
        //    foreach (string line in lines)
        //    {
        //        if (line.Length > 7 && line.Contains("value"))
        //        {
        //            //wyszukiwanie nazwy zmiennej

        //            string patternKey = @"[A-Z]+(-[A-Z]+)+";
        //            string NameVariableKey = Regex.Match(line, patternKey).ToString() ?? null;
        //            //wyszukiwanie wartości zmiennej, czyli nazwy wywoływanego programu
        //            string patternValue = "\".......\"";
        //            string NameVariableValue1 = Regex.Match(line, patternValue).ToString();
        //            string NameVariableValue = NameVariableValue1.Substring(1, NameVariableValue1.Length - 2);
        //            //    ////sprawdzanie czy wartość zmiennej może być nazwą programu (zwykle to jest 7 znaków) i dodanie zmiennej wywołującej program do listy
        //            //    if (NameVariableValue.Length == 7)
        //            //        NamesOfVariables.Add(NameVariableKey, NameVariableValue);
        //        }
        //    }
        //    return NamesOfVariables;
        //}

        private void CloseProgram_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
