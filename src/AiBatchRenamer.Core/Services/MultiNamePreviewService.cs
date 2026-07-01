using System.Collections.Generic;
using System.Linq;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Core.Services
{
    public class MultiNamePreviewService
    {
        public IList<RenameItem> ApplyNames(IList<RenameItem> items, string namesText, bool sanitizeNames)
        {
            var names = SplitNames(namesText);
            for (var i = 0; i < items.Count; i++)
            {
                if (i < names.Count)
                {
                    items[i].ProposedBaseName = sanitizeNames
                        ? FileNameSanitizer.SanitizeBaseName(names[i])
                        : names[i].Trim();
                    items[i].Message = string.Empty;
                    items[i].Status = RenameStatus.Pending;
                }
                else
                {
                    items[i].ProposedBaseName = items[i].OriginalBaseName;
                    items[i].Message = "缺少对应的新名称";
                    items[i].Status = RenameStatus.Invalid;
                }
            }

            return items;
        }

        public IList<string> SplitNames(string namesText)
        {
            if (string.IsNullOrWhiteSpace(namesText))
            {
                return new List<string>();
            }

            return namesText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }
    }
}
