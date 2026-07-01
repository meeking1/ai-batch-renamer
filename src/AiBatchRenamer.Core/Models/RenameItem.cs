using System.IO;

namespace AiBatchRenamer.Core.Models
{
    public class RenameItem
    {
        public RenameItem()
        {
        }

        public RenameItem(string originalPath)
        {
            OriginalPath = originalPath;
            DirectoryPath = Path.GetDirectoryName(originalPath) ?? string.Empty;
            OriginalName = Path.GetFileName(originalPath);
            OriginalBaseName = Path.GetFileNameWithoutExtension(originalPath);
            Extension = Path.GetExtension(originalPath);
            ProposedBaseName = OriginalBaseName;
        }

        public int Index { get; set; }

        public string OriginalPath { get; set; }

        public string DirectoryPath { get; set; }

        public string OriginalName { get; set; }

        public string OriginalBaseName { get; set; }

        public string Extension { get; set; }

        public string ProposedBaseName { get; set; }

        public string ProposedName
        {
            get { return (ProposedBaseName ?? string.Empty) + (Extension ?? string.Empty); }
        }

        public string ProposedPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DirectoryPath))
                {
                    return ProposedName;
                }

                return DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar +
                    ProposedName;
            }
        }

        public RenameStatus Status { get; set; }

        public string Message { get; set; }

        public bool IsValid
        {
            get { return Status == RenameStatus.Ready || Status == RenameStatus.Unchanged; }
        }
    }

    public enum RenameStatus
    {
        Pending,
        Ready,
        Unchanged,
        Invalid,
        Conflict,
        Failed,
        Success
    }
}
