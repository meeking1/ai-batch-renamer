using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Core.Services
{
    public class RenameValidationService
    {
        public void Validate(IList<RenameItem> items)
        {
            var proposedPathCounts = items
                .GroupBy(item => NormalizePath(item.ProposedPath))
                .ToDictionary(group => group.Key, group => group.Count());
            var originalItemsByPath = items
                .GroupBy(item => NormalizePath(item.OriginalPath))
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var item in items)
            {
                ValidateItem(item, proposedPathCounts, originalItemsByPath);
            }
        }

        private static void ValidateItem(
            RenameItem item,
            IDictionary<string, int> proposedPathCounts,
            IDictionary<string, RenameItem> originalItemsByPath)
        {
            if (item.Status == RenameStatus.Invalid && !string.IsNullOrWhiteSpace(item.Message))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(item.ProposedBaseName))
            {
                Mark(item, RenameStatus.Invalid, "新文件名不能为空");
                return;
            }

            if (FileNameSanitizer.ContainsInvalidFileNameChars(item.ProposedName))
            {
                Mark(item, RenameStatus.Invalid, "新文件名包含非法字符");
                return;
            }

            if (IsReservedWindowsDeviceName(item.ProposedBaseName))
            {
                Mark(item, RenameStatus.Invalid, "新文件名是 Windows 保留设备名");
                return;
            }

            if (item.ProposedName.EndsWith(".", StringComparison.Ordinal) ||
                item.ProposedName.EndsWith(" ", StringComparison.Ordinal))
            {
                Mark(item, RenameStatus.Invalid, "新文件名不能以点或空格结尾");
                return;
            }

            if (string.Equals(item.OriginalPath, item.ProposedPath, StringComparison.Ordinal))
            {
                Mark(item, RenameStatus.Unchanged, "未变化");
                return;
            }

            var normalizedProposedPath = NormalizePath(item.ProposedPath);
            if (proposedPathCounts.ContainsKey(normalizedProposedPath) && proposedPathCounts[normalizedProposedPath] > 1)
            {
                Mark(item, RenameStatus.Conflict, "与列表中其他新文件名重复");
                return;
            }

            if (File.Exists(item.ProposedPath) &&
                !string.Equals(item.OriginalPath, item.ProposedPath, StringComparison.OrdinalIgnoreCase) &&
                !IsOccupiedByMovingSelectedItem(item.ProposedPath, originalItemsByPath))
            {
                Mark(item, RenameStatus.Conflict, "目标文件已存在");
                return;
            }

            if (item.ProposedPath.Length >= 248)
            {
                Mark(item, RenameStatus.Invalid, "路径过长，可能不兼容 Windows 7");
                return;
            }

            Mark(item, RenameStatus.Ready, "可重命名");
        }

        private static void Mark(RenameItem item, RenameStatus status, string message)
        {
            item.Status = status;
            item.Message = message;
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool IsOccupiedByMovingSelectedItem(
            string proposedPath,
            IDictionary<string, RenameItem> originalItemsByPath)
        {
            RenameItem occupant;
            if (!originalItemsByPath.TryGetValue(NormalizePath(proposedPath), out occupant))
            {
                return false;
            }

            return !string.Equals(
                occupant.OriginalPath,
                occupant.ProposedPath,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReservedWindowsDeviceName(string baseName)
        {
            var normalized = (baseName ?? string.Empty)
                .Trim()
                .TrimEnd('.', ' ')
                .ToUpperInvariant();
            var dotIndex = normalized.IndexOf('.');
            if (dotIndex >= 0)
            {
                normalized = normalized.Substring(0, dotIndex);
            }

            if (normalized == "CON" ||
                normalized == "PRN" ||
                normalized == "AUX" ||
                normalized == "NUL")
            {
                return true;
            }

            if (normalized.Length == 4)
            {
                var prefix = normalized.Substring(0, 3);
                var suffix = normalized[3];
                return (prefix == "COM" || prefix == "LPT") && suffix >= '1' && suffix <= '9';
            }

            return false;
        }
    }
}
