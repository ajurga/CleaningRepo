﻿using Microsoft.Win32;
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
using System.Windows.Threading;
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
        int counter = 0;
        int filesPerThread = 10;

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
        }

        private void ProgramsToFind_Click(object sender, RoutedEventArgs e)
        {
            string folder = ReadFile();

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
                // robimy kopię z plików do wyszukania
                unusedFiles = new HashSet<string>(FilesToFind);
                //usuwamy z listy używane pliki
                Thread[] threads = new Thread[4];
                while (counter < FilesFromRepository.Count)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        threads[i] = new Thread(() =>
                        {
                            FilterUnused();
                            counter += filesPerThread;
                        });
                        threads[i].Start();
                    }
                    foreach (Thread t in threads)
                    {
                        t.Join();
                        progressBar.Dispatcher.Invoke(() => progressBar.Value = 100 * counter / FilesFromRepository.Count, DispatcherPriority.Background);
                    }
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

        void FilterUnused()
        {
            List<string> filesList = new List<string>(FilesToFind);
            List<string> filesFromRepoList = new List<string>(FilesFromRepository);
            int counter = 0;
            for (int i = counter; i< (counter+filesPerThread<FilesFromRepository.Count ? counter+FilesFromRepository.Count : FilesFromRepository.Count); i++)
            {
                if (filesFromRepoList[i].Contains("JCL"))
                {
                    CheckJCL(filesFromRepoList[i], filesList);
                }
                else
                {
                    CheckNonJCL(filesFromRepoList[i], filesList);
                }
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
    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };


        public static void Refresh(this UIElement uiElement)

        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}
