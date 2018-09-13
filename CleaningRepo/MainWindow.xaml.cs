using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;


namespace CleaningRepo
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();
        }


        HashSet<string> FilesToFind = new HashSet<string>();
        HashSet<string> FilesFromRepository = new HashSet<string>();
        HashSet<string> RemovedFiles = new HashSet<string>();
        HashSet<string> unusedFiles = new HashSet<string>();
        List<string> JCLFiles = new List<string>();
        List<string> NonJCLFiles = new List<string>();
        string foldersAdded = "";
        
        int percent;

      

        //pobieranie ścieżki do folderu
        public static string ReadFile()
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
            FilesToFind.Clear();
            FilesFromRepository.Clear();
            RemovedFiles.Clear();
            unusedFiles.Clear();
            JCLFiles.Clear();
            NonJCLFiles.Clear();
            foldersAdded = "";
            CzyOkProg.Text = foldersAdded;
            CzyOkRepo.Text = "";
            listViewFind.ItemsSource = DisplayResults(FilesToFind);
            listViewUnused.ItemsSource = DisplayResults(unusedFiles);
            progressBar.Value = 0;
            StatusTextBox.Text = "";
            percent = 0;
        }

        private void ProgramsToFind_Click(object sender, RoutedEventArgs e)
        {
            string folder = ReadFile();

            if (folder != null)
            {
                foldersAdded += new DirectoryInfo(folder).Name+"\n";
                CzyOkProg.Text = foldersAdded;
                SearchDirs(folder, FilesToFind, false);
                listViewFind.ItemsSource = DisplayResults(FilesToFind);
  
            }

            if (FilesToFind == null)
                System.Windows.MessageBox.Show("Lista plików do sprawdzenia jest pusta", "Uwaga!");
        }

        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            string repositoryPath = ReadFile();
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


        HashSet<string> DisplayResults(HashSet<string> results)
        {
            HashSet<string> lista = new HashSet<string>();
            foreach (string s in results)
            {
                lista.Add(Path.GetFileName(s));
            }
            return lista;
        }

        

        private void RunProgram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //blokujemy przyciski
                Repository.IsEnabled = false;
                ProgramsToFind.IsEnabled = false;
                RunProgram.IsEnabled = false;
                Reset.IsEnabled = false;
                CloseProgram.IsEnabled = false;
                // robimy kopię z plików do wyszukania
                unusedFiles = new HashSet<string>(FilesToFind);
                //dzielimy pliki z listy 'unusedFiles' na JCL i inne
                DivideFiles(JCLFiles, NonJCLFiles);
                ProgressBarRun();
                //usuwamy z listy używane pliki
                //FilterUnused(unusedFiles, JCLFiles, NonJCLFiles);

                //listViewUnused.ItemsSource = DisplayResults(unusedFiles);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Wystąpił błąd:" + ex, "Uwaga!");
            }
        }

        void SaveFiles()
        {
            Thread.Sleep(200);
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
        void DivideFiles(List<string> JCL, List<string> NonJCL)
        {
            foreach (string file in FilesFromRepository)
            {
                if (file.Contains("JCL"))
                {
                    JCL.Add(file);
                }
                else
                {
                    NonJCL.Add(file);
                }
            }
        }

        void ProgressBarRun()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            /*for (int i = 1; i <= FilesFromRepository.Count; ++i)
            {
                //Thread.Sleep(200);
                worker.ReportProgress(i);
            }*/

            int counter = 0;
            List<string> filesList = new List<string>(unusedFiles);
            foreach (string file in NonJCLFiles)
            {
                counter++;
                percent = (counter * 100) / FilesFromRepository.Count;
                CheckNonJCL(file, filesList);
            }
            foreach (string file in JCLFiles)
            {
                counter++;
                percent = (counter * 100) / FilesFromRepository.Count;
                CheckJCL(file, filesList);
            }

               // FilterUnused(unusedFiles, JCLFiles, NonJCLFiles);
            worker.ReportProgress(percent);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //double percent = (e.ProgressPercentage * 100) / FilesFromRepository.Count;

            //progressBar.Value = Math.Round(percent, 0);

            //StatusTextBox.Text = Math.Round(percent, 0) + "% percent completed, counter: " + counter;
            progressBar.Value = e.ProgressPercentage;
            StatusTextBox.Text = "Processing......" + progressBar.Value.ToString() + "%";

        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                listViewUnused.ItemsSource = DisplayResults(unusedFiles);
                SaveFiles();
                //potwierdzenie zakończenia procesu
                System.Windows.MessageBox.Show("Przeszukiwanie zakończone", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                //zwalniamy przyciski
                Repository.IsEnabled = true;
                ProgramsToFind.IsEnabled = true;
                RunProgram.IsEnabled = true;
                Reset.IsEnabled = true;
                CloseProgram.IsEnabled = true;
            }
        }



        void FilterUnused(HashSet<string> filesToCheck, List<string> jclFiles, List<string> nonJclFiles)
        {
            
            int counter = 0;
            List<string> filesList = new List<string>(filesToCheck);
            foreach (string file in nonJclFiles)
            {
                counter++;
                percent = (counter * 100) / FilesFromRepository.Count;
                progressBar.Value = percent;
                CheckNonJCL(file, filesList);
            }
            foreach (string file in jclFiles)
            {
                counter++;
                percent = (counter * 100) / FilesFromRepository.Count;
                progressBar.Value = percent;
                CheckJCL(file, filesList);

            }
            
        }
        

        //Usuwa z listy nieużywane programy z nie JCLowych
        void CheckNonJCL(string file, List<string> toCheck)
        {
            HashSet<string> foundProgs = new HashSet<string>();
            Dictionary<string, string> VarsNames;
            try
            {
                ////pobieranie nazw zmiennych do których mogą byc przypisane programy
                string[] lines = File.ReadAllLines(file);
                //VarsNames = GetNameVariable(lines);
                foreach (string line in lines)
                {
                    if (!(line.Length > 6 && line[6] == '*' || line.Contains("PROGRAM-ID")))
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

        //Usuwa z listy nieużywane programy z nie JCLowych
        void CheckJCL(string file, List<string> toCheck)
        {
            HashSet<string> foundProgs = new HashSet<string>();
            Dictionary<string, string> VarsNames;
            try
            {
                ////pobieranie nazw zmiennych do których mogą byc przypisane programy
                string[] lines = File.ReadAllLines(file);
                //VarsNames = GetNameVariable(lines);
                foreach (string line in lines)
                {
                    if (!line.Contains("//*"))
                    {
                        for (int i = toCheck.Count - 1; i > 0; i--)
                        {
                            if (line.Contains(System.IO.Path.GetFileName(toCheck[i])))
                            {
                                unusedFiles.Remove(toCheck[i]);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        Dictionary<string, string> GetNameVariable(string[] lines)
        {
            Dictionary<string, string> NamesOfVariables = new Dictionary<string, string>();
            foreach (string line in lines)
            {
                if (line.Length > 7 && line.Contains("value"))
                {
                    //wyszukiwanie nazwy zmiennej

                    string patternKey = @"[A-Z]+(-[A-Z]+)+";
                    string NameVariableKey = Regex.Match(line, patternKey).ToString() ?? null;
                    //wyszukiwanie wartości zmiennej, czyli nazwy wywoływanego programu
                    string patternValue = "\".......\"";
                    string NameVariableValue1 = Regex.Match(line, patternValue).ToString();
                    string NameVariableValue = NameVariableValue1.Substring(1, NameVariableValue1.Length - 2);
                    //    ////sprawdzanie czy wartość zmiennej może być nazwą programu (zwykle to jest 7 znaków) i dodanie zmiennej wywołującej program do listy
                    //    if (NameVariableValue.Length == 7)
                    //        NamesOfVariables.Add(NameVariableKey, NameVariableValue);
                }
            }
            return NamesOfVariables;
        }

        private void CloseProgram_Click(object sender, RoutedEventArgs e)
        {
            
            this.Close();
        }
    }
}
