using ShareX.HelpersLib;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecifiedRecordsExporter
{
    public partial class MainWindow : Window
    {
        private Worker worker;

        public MainWindow()
        {
            InitializeComponent();
            txtFolderNameSplit.Text = "-";
            txtFilePrefix.Text = "FPC";
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFolderNameSplit.Text))
            {
                FolderBrowserForWPF.Dialog dlg = new FolderBrowserForWPF.Dialog();
                dlg.Title = "Browse for the Specified Records folder...";

                if (dlg.ShowDialog() == true)
                {
                    txtRootDir.Text = dlg.FileName;
                    btnPreview.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("You have not completed Step 1 above!", Application.Current.MainWindow.Title);
            }

        }

        private async void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtRootDir.Text))
            {
                lvFiles.Items.Clear();
                worker = new Worker(txtRootDir.Text, txtFolderNameSplit.Text, txtFilePrefix.Text);
                worker.PreviewProgressChanged += Worker_PreviewProgressChanged;
                await worker.PreviewAsync();
            }
        }

        private void Worker_PreviewProgressChanged(string progress)
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Foreground = progress.Length > 260 ? new SolidColorBrush(Colors.Yellow) : new SolidColorBrush(Colors.Green);
            lvi.Content = progress;
            lvFiles.Items.Add(lvi);
            btnGo.IsEnabled = lvFiles.Items.Count > 0;
        }

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePrefix.Text))
            {
                MessageBox.Show("Free Text is empty!", Application.Current.MainWindow.Title);
            }
            else if (string.IsNullOrEmpty(txtFolderNameSplit.Text))
            {
                MessageBox.Show("You have not completed Step 1 above!", Application.Current.MainWindow.Title);
            }
            else if (lvFiles.Items.Count == 0)
            {
                MessageBox.Show("Please press the Preview button before trying to rename.", Application.Current.MainWindow.Title);
            }
            else
            {
                btnGo.IsEnabled = false;
                pBar.Value = 0;

                worker = new Worker(txtRootDir.Text, txtFolderNameSplit.Text, txtFilePrefix.Text);
                worker.RenameProgressChanged += Worker_FileMoveProgressChanged;
                await worker.RenameAsync();

                btnGo.IsEnabled = true;
            }
        }

        private void Worker_FileMoveProgressChanged(float progress)
        {
            if (!string.IsNullOrEmpty(worker.Error))
            {
                tbStatus.Text = worker.Error;
            }

            if (worker.FilesCount > 0)
            {
                pBar.Maximum = worker.FilesCount;
                tbStatus.Text = "Export complete!";
            }

            pBar.Value = progress;
        }

        private void lvFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string fp = lvFiles.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(fp))
            {
                string dir = Path.GetDirectoryName(fp);
                if (Directory.Exists(dir))
                {
                    Helpers.OpenFolder(dir);
                }
            }
        }
    }
}