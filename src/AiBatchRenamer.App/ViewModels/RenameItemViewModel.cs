using System.ComponentModel;
using System.IO;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.App.ViewModels
{
    public class RenameItemViewModel : INotifyPropertyChanged
    {
        private readonly RenameItem item;

        public RenameItemViewModel(RenameItem item)
        {
            this.item = item;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RenameItem Model
        {
            get { return item; }
        }

        public int Index
        {
            get { return item.Index; }
            set
            {
                item.Index = value;
                OnPropertyChanged("Index");
            }
        }

        public string OriginalName
        {
            get { return item.OriginalName; }
        }

        public string ProposedBaseName
        {
            get { return item.ProposedBaseName; }
            set
            {
                item.ProposedBaseName = value;
                item.Status = RenameStatus.Pending;
                item.Message = "已手动修改，待校验";
                OnPropertyChanged("ProposedBaseName");
                OnPropertyChanged("ProposedName");
                OnPropertyChanged("ProposedPath");
                OnPropertyChanged("Status");
                OnPropertyChanged("Message");
                OnPropertyChanged("CanExecute");
            }
        }

        public string ProposedName
        {
            get { return item.ProposedName; }
        }

        public string Status
        {
            get { return ToDisplayStatus(item.Status); }
        }

        public string Message
        {
            get { return item.Message; }
        }

        public string DirectoryPath
        {
            get { return item.DirectoryPath; }
        }

        public string ProposedPath
        {
            get { return item.ProposedPath; }
        }

        public bool CanExecute
        {
            get { return item.Status == RenameStatus.Ready; }
        }

        public void RefreshAll()
        {
            OnPropertyChanged("OriginalName");
            OnPropertyChanged("ProposedBaseName");
            OnPropertyChanged("ProposedName");
            OnPropertyChanged("Status");
            OnPropertyChanged("Message");
            OnPropertyChanged("DirectoryPath");
            OnPropertyChanged("ProposedPath");
            OnPropertyChanged("CanExecute");
        }

        public void MarkAsCurrentFile()
        {
            item.OriginalPath = item.ProposedPath;
            item.DirectoryPath = Path.GetDirectoryName(item.OriginalPath) ?? string.Empty;
            item.OriginalName = Path.GetFileName(item.OriginalPath);
            item.OriginalBaseName = Path.GetFileNameWithoutExtension(item.OriginalPath);
            item.Extension = Path.GetExtension(item.OriginalPath);
            item.ProposedBaseName = item.OriginalBaseName;
            item.Status = RenameStatus.Unchanged;
            item.Message = "当前文件";
            RefreshAll();
        }

        private static string ToDisplayStatus(RenameStatus status)
        {
            switch (status)
            {
                case RenameStatus.Ready:
                    return "可执行";
                case RenameStatus.Unchanged:
                    return "未变化";
                case RenameStatus.Invalid:
                    return "无效";
                case RenameStatus.Conflict:
                    return "冲突";
                case RenameStatus.Failed:
                    return "失败";
                case RenameStatus.Success:
                    return "成功";
                default:
                    return "待预览";
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
