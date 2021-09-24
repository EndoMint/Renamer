using System;
using System.IO;
using System.Threading.Tasks;

namespace SpecifiedRecordsExporter
{
    public class Worker
    {
        public delegate void RenameProgressChangedEventHandler(float progress);
        public event RenameProgressChangedEventHandler RenameProgressChanged;

        public delegate void PreviewProgressChangedEventHandler(string progress);
        public event PreviewProgressChangedEventHandler PreviewProgressChanged;

        public int FilesCount { get; private set; }
        public int FileID { get; private set; }
        public string Error { get; private set; }

        private TaskEx<float> taskRename;
        private TaskEx<string> taskPreview;

        private string rootDir;
        private string folderDelimiter;
        private string fileNamePrefix;

        public Worker(string rootDir, string folderDelimiter, string prefix)
        {
            taskPreview = new TaskEx<string>();
            taskPreview.ProgressChanged += OnPreviewProgressChanged;

            taskRename = new TaskEx<float>();
            taskRename.ProgressChanged += OnRenameProgressChanged;


            if (Directory.Exists(rootDir))
            {
                if (!rootDir.EndsWith(@"\"))
                {
                    rootDir += @"\";
                }

                this.rootDir = rootDir;
                this.folderDelimiter = folderDelimiter;
                this.fileNamePrefix = prefix;
            }
        }

        private void OnPreviewProgressChanged(string progress)
        {
            PreviewProgressChanged?.Invoke(progress);
        }

        public async Task PreviewAsync()
        {
            await taskPreview.Run(Preview);
        }

        public async Task RenameAsync()
        {
            await taskRename.Run(Rename);
        }

        private void OnRenameProgressChanged(float progress)
        {
            RenameProgressChanged?.Invoke(progress);
        }

        public void Stop()
        {
            taskRename.Cancel();
        }

        private string GetDestPath(string origPath)
        {
            string origDir = Path.GetDirectoryName(origPath);
            string[] delims = origDir.Split(folderDelimiter);
            string fileNameSuffix = "";
            if (delims.Length > 1)
                fileNameSuffix = delims[1].Replace(" ","_");
            string fn = fileNamePrefix + FileID.ToString("00000") + "_" + fileNameSuffix + Path.GetExtension(origPath);
            return Path.Combine(origDir, fn);
        }

        private void Preview()
        {
            if (Directory.Exists(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, "*.png", SearchOption.AllDirectories);
                Array.Sort(files);
                foreach (string fp in files)
                {
                    FileID++;
                    taskPreview.Report(GetDestPath(fp));
                    taskPreview.ThrowIfCancellationRequested();
                }
            }
        }

        private void Rename()
        {
            FileID = 1;

            if (Directory.Exists(rootDir))
            {
                string[] files = Directory.GetFiles(rootDir, "*.png", SearchOption.AllDirectories);
                Array.Sort(files);
                FilesCount = files.Length;

                foreach (string fp in files)
                {
                    if (MoveFile(fp))
                    {
                        FileID++;
                        taskRename.Report(FileID);
                    }

                    taskRename.ThrowIfCancellationRequested();
                }

                string[] dirs = Directory.GetDirectories(rootDir);
                foreach (string dir in dirs)
                {
                    DeleteEmptyFolders(dir);
                }
            }
        }



        private bool MoveFile(string origPath)
        {
            string destPath = GetDestPath(origPath);

            try
            {
                if (destPath.Length < 260)
                {
                    File.Move(origPath, destPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Error = $"{destPath} ({Path.GetFileName(destPath).Length} characters): {ex.Message}";
            }

            return false;
        }

        private void DeleteEmptyFolders(string dirPath)
        {
            foreach (string subdirPath in Directory.GetDirectories(dirPath))
            {
                DeleteEmptyFolders(subdirPath);
            }

            if (EmptyFolderHelper.CheckDirectoryEmpty(dirPath))
            {
                new DirectoryInfo(dirPath).Delete();
            }
        }
    }
}