using System.Collections.Generic;

namespace AiBatchRenamer.Core.Models
{
    public class AiNamingRequest
    {
        public AiNamingRequest()
        {
            Files = new List<AiNamingFile>();
        }

        public string Instruction { get; set; }

        public IList<AiNamingFile> Files { get; set; }

        public bool KeepExtension { get; set; }
    }

    public class AiNamingFile
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public string Extension { get; set; }
    }

    public class AiNamingResult
    {
        public AiNamingResult()
        {
            Items = new List<AiNamingItem>();
        }

        public IList<AiNamingItem> Items { get; set; }

        public string Warning { get; set; }
    }

    public class AiNamingItem
    {
        public int Index { get; set; }

        public string NewBaseName { get; set; }
    }
}
