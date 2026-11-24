using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace lvglConverter
{
    public partial class MainWindow : Window
    {
        private List<string> UnhandledFiles = new List<string>();

        private string FolderPath
        {
            get { return txtFolderPath.Text; }
            set { txtFolderPath.Text = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cbType.Items.Add("RGB332 Not Available");
            cbType.Items.Add("Binary RGB565");
            cbType.Items.Add("Binary RGB565 Swap");
            cbType.Items.Add("RGB888 Not Available");
            cbType.SelectedIndex = 1;
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = folderBrowserDialog.SelectedPath;
            }
        }

        private void GetTileFiles(string folderPath, List<string> path)
        {
            string[] files = Directory.GetFiles(folderPath);
            foreach (string text in files)
            {
                FileInfo fileInfo = new FileInfo(text);
                int result = -1;
                int.TryParse(fileInfo.Name.Replace(fileInfo.Extension, ""), out result);
                if (fileInfo.Extension.ToLower().IndexOf("png") > 0)
                {
                    path.Add(text);
                }
            }
            files = Directory.GetDirectories(folderPath);
            foreach (string text2 in files)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(text2);
                int result2 = -1;
                int.TryParse(directoryInfo.Name, out result2);
                if (result2 >= 0)
                {
                    GetTileFiles(text2, path);
                }
            }
        }

        private void ThreadProcess()
        {
            Dispatcher.Invoke(() => txtTip.Text = "Scanning ...");
            List<string> tileFilePath = new List<string>();
            string folderPath = "";
            Dispatcher.Invoke(() => folderPath = FolderPath);
            GetTileFiles(folderPath, tileFilePath);
            Dispatcher.Invoke(() =>
            {
                if (tileFilePath.Count == 0) txtTip.Text = "Empty folder.";
                else txtTip.Text = $"File count : {tileFilePath.Count}";
                gdProgress.Maximum = tileFilePath.Count;
            });
            if (tileFilePath.Count == 0) return;
            Thread.Sleep(500);
            int num = 16;
            int num2 = tileFilePath.Count / num;
            int num3 = tileFilePath.Count - num2 * num;
            List<FileHandler> fileHandlers = new List<FileHandler>();
            LVGLImageType targetType = LVGLImageType.RGB565;
            bool ignoreFileEnable = false;
            Dispatcher.Invoke(() =>
            {
                switch (cbType.SelectedIndex)
                {
                    case 0: targetType = LVGLImageType.RGB332; break;
                    case 1: targetType = LVGLImageType.RGB565; break;
                    case 2: targetType = LVGLImageType.RGB565SWAP; break;
                    case 3: targetType = LVGLImageType.RGB888; break;
                }
                ignoreFileEnable = cbIgnoreExistedFile.IsChecked.Value;
            });
            for (int num4 = 0; num4 < num; num4++)
            {
                FileHandler fileHandler = new FileHandler(tileFilePath, num4 * num2, num2 + ((num4 == num - 1) ? num3 : 0), targetType, ignoreFileEnable);
                fileHandlers.Add(fileHandler);
                new Thread(fileHandler.Start).Start();
            }
            int totalHandledCount = 0;
            while (true)
            {
                bool allCompleted = true;
                Dispatcher.Invoke(() =>
                {
                    totalHandledCount = 0;
                    foreach (FileHandler item in fileHandlers)
                    {
                        totalHandledCount += item.HandledCount;
                        allCompleted &= item.IsCompleted;
                    }
                    int num5 = 100 * totalHandledCount / tileFilePath.Count;
                    txtProgress.Text = $"{num5}%";
                    gdProgress.Value = totalHandledCount;
                    txtTip.Text = $"Converting ... ({totalHandledCount}/{tileFilePath.Count})";
                });
                if (allCompleted) break;
                Thread.Sleep(10);
            }
            Dispatcher.Invoke(() =>
            {
                foreach (FileHandler item2 in fileHandlers) UnhandledFiles.AddRange(item2.UnhandledFiles);
                if (UnhandledFiles.Count > 0)
                {
                    btnStart.Visibility = Visibility.Hidden;
                    txtTip.Text = $"Conversion Completed. {totalHandledCount} OK, {tileFilePath.Count - totalHandledCount} Error(s)";
                }
                else
                {
                    txtTip.Text = "Conversion Completed. All Done !";
                }
            });
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (FolderPath.Length > 0)
            {
                if (cbType.SelectedIndex == 0 || cbType.SelectedIndex == 3)
                {
                    System.Windows.MessageBox.Show("Target type is not available now.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }
                cbType.IsEnabled = false;
                btnSelectFolder.IsEnabled = false;
                btnStart.IsEnabled = false;
                cbIgnoreExistedFile.IsEnabled = false;
                new Thread(ThreadProcess).Start();
            }
        }

        private void btnHandle_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("These files will be deleted :");
            foreach (string unhandledFile in UnhandledFiles) stringBuilder.Append("\r\n" + unhandledFile);
            if (System.Windows.MessageBox.Show(stringBuilder.ToString(), "Tip", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            btnHandle.IsEnabled = false;
            foreach (string unhandledFile2 in UnhandledFiles) File.Delete(unhandledFile2);
            btnHandle.Content = "All done !";
        }
    }
}


