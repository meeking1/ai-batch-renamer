using System;
using System.Collections.Generic;
using System.IO;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Core.Services
{
    public class TemplatePreviewService
    {
        public IList<RenameItem> ApplyTemplate(IList<RenameItem> items, string template, bool sanitizeNames)
        {
            var effectiveTemplate = string.IsNullOrWhiteSpace(template)
                ? "{name}-{index:000}"
                : template.Trim();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var baseName = RenderTemplate(item, i + 1, effectiveTemplate);
                item.ProposedBaseName = sanitizeNames
                    ? FileNameSanitizer.SanitizeBaseName(baseName)
                    : baseName.Trim();
                item.Status = RenameStatus.Pending;
                item.Message = string.Empty;
            }

            return items;
        }

        private static string RenderTemplate(RenameItem item, int sequence, string template)
        {
            var folderName = string.IsNullOrWhiteSpace(item.DirectoryPath)
                ? string.Empty
                : new DirectoryInfo(item.DirectoryPath).Name;
            var lastWriteTime = GetLastWriteTime(item.OriginalPath);

            return template
                .Replace("{name}", item.OriginalBaseName ?? string.Empty)
                .Replace("{filename}", item.OriginalBaseName ?? string.Empty)
                .Replace("{folder}", folderName)
                .Replace("{ext}", TrimExtensionDot(item.Extension))
                .Replace("{date}", lastWriteTime.ToString("yyyy-MM-dd"))
                .Replace("{time}", lastWriteTime.ToString("HHmmss"))
                .Replace("{index}", sequence.ToString())
                .Replace("{index:00}", sequence.ToString("00"))
                .Replace("{index:000}", sequence.ToString("000"))
                .Replace("{index:0000}", sequence.ToString("0000"));
        }

        private static DateTime GetLastWriteTime(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTime(path) : DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private static string TrimExtensionDot(string extension)
        {
            return string.IsNullOrWhiteSpace(extension)
                ? string.Empty
                : extension.TrimStart('.');
        }
    }
}
