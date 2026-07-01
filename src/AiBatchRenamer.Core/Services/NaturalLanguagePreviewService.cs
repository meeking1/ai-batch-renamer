using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Core.Services
{
    public class NaturalLanguagePreviewService
    {
        public IList<RenameItem> ApplyInstruction(IList<RenameItem> items, string instruction, bool sanitizeNames)
        {
            if (string.IsNullOrWhiteSpace(instruction))
            {
                return items;
            }

            var replace = TryParseReplaceInstruction(instruction);
            if (replace != null)
            {
                foreach (var item in items)
                {
                    item.ProposedBaseName = item.OriginalBaseName.Replace(replace.OldText, replace.NewText);
                    item.Status = RenameStatus.Pending;
                    item.Message = string.Empty;
                }

                return items;
            }

            var prefix = ExtractQuotedText(instruction);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = instruction.Trim();
            }

            prefix = RemoveCommonInstructionWords(prefix);
            if (sanitizeNames)
            {
                prefix = FileNameSanitizer.SanitizeBaseName(prefix);
            }

            var width = Math.Max(3, items.Count.ToString().Length);
            for (var i = 0; i < items.Count; i++)
            {
                items[i].ProposedBaseName = string.Format("{0}-{1}", prefix, (i + 1).ToString().PadLeft(width, '0'));
                items[i].Status = RenameStatus.Pending;
                items[i].Message = string.Empty;
            }

            return items;
        }

        private static ReplaceInstruction TryParseReplaceInstruction(string instruction)
        {
            var patterns = new[]
            {
                "把(.+?)替换成(.+)",
                "将(.+?)替换为(.+)",
                "replace\\s+(.+?)\\s+with\\s+(.+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(instruction, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return new ReplaceInstruction
                    {
                        OldText = TrimQuotes(match.Groups[1].Value),
                        NewText = TrimQuotes(match.Groups[2].Value)
                    };
                }
            }

            return null;
        }

        private static string ExtractQuotedText(string instruction)
        {
            var match = Regex.Match(instruction, "[“\"'](.+?)[”\"']");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static string TrimQuotes(string value)
        {
            return (value ?? string.Empty).Trim().Trim('“', '”', '"', '\'', ' ');
        }

        private static string RemoveCommonInstructionWords(string value)
        {
            return (value ?? string.Empty)
                .Replace("命名为", string.Empty)
                .Replace("重命名为", string.Empty)
                .Replace("按", string.Empty)
                .Replace("编号", string.Empty)
                .Trim();
        }

        private class ReplaceInstruction
        {
            public string OldText { get; set; }

            public string NewText { get; set; }
        }
    }
}
