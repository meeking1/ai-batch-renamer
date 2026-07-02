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

            var replacements = ParseReplaceInstructions(instruction);
            if (replacements.Count > 0)
            {
                foreach (var item in items)
                {
                    var proposedBaseName = item.OriginalBaseName;
                    foreach (var replace in replacements)
                    {
                        proposedBaseName = proposedBaseName.Replace(replace.OldText, replace.NewText);
                    }

                    item.ProposedBaseName = sanitizeNames
                        ? FileNameSanitizer.SanitizeBaseName(proposedBaseName)
                        : proposedBaseName;
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

        private static IList<ReplaceInstruction> ParseReplaceInstructions(string instruction)
        {
            var replacements = new List<ReplaceInstruction>();
            var lines = Regex.Split(instruction ?? string.Empty, @"[\r\n;；]+");
            foreach (var line in lines)
            {
                var replace = TryParseReplaceInstruction(line);
                if (replace != null)
                {
                    replacements.Add(replace);
                }
            }

            if (replacements.Count == 0)
            {
                var replace = TryParseReplaceInstruction(instruction);
                if (replace != null)
                {
                    replacements.Add(replace);
                }
            }

            return replacements;
        }

        private static ReplaceInstruction TryParseReplaceInstruction(string instruction)
        {
            var patterns = new[]
            {
                "把(.+?)替换成(.+)",
                "将(.+?)替换为(.+)",
                "把(.+?)改成(.+)",
                "将(.+?)改为(.+)",
                "(.+?)改成(.+)",
                "(.+?)改为(.+)",
                "replace\\s+(.+?)\\s+with\\s+(.+)"
            };

            instruction = (instruction ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(instruction))
            {
                return null;
            }

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(instruction, "^" + pattern + "$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var oldText = TrimQuotes(match.Groups[1].Value);
                    var newText = TrimQuotes(match.Groups[2].Value);
                    if (!string.IsNullOrEmpty(oldText))
                    {
                        return new ReplaceInstruction
                        {
                            OldText = oldText,
                            NewText = newText
                        };
                    }
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
