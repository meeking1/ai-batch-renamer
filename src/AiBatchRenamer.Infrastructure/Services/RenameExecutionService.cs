using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class RenameExecutionService
    {
        private readonly OperationLogRepository logRepository;

        public RenameExecutionService(OperationLogRepository logRepository)
        {
            this.logRepository = logRepository;
        }

        public OperationLog Execute(IList<RenameItem> items)
        {
            var log = new OperationLog
            {
                OperationId = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"),
                CreatedAt = DateTimeOffset.Now.ToString("o")
            };

            var plans = items
                .Where(item => item.Status == RenameStatus.Ready)
                .Select(item => new RenameMovePlan(item))
                .ToList();

            foreach (var plan in plans)
            {
                plan.LogItem = new OperationLogItem
                {
                    OldPath = plan.Item.OriginalPath,
                    NewPath = plan.Item.ProposedPath
                };
                log.Items.Add(plan.LogItem);
            }

            if (RequiresStagedMove(plans))
            {
                WriteDiagnostic("RenameExecutionService using staged move. Count=" + plans.Count);
                ExecuteStaged(plans);
            }
            else
            {
                WriteDiagnostic("RenameExecutionService using direct move. Count=" + plans.Count);
                ExecuteDirect(plans);
            }

            WriteDiagnostic("RenameExecutionService saving operation log. Items=" + log.Items.Count);
            logRepository.Save(log);
            WriteDiagnostic("RenameExecutionService saved operation log.");
            return log;
        }

        private static void ExecuteDirect(IEnumerable<RenameMovePlan> plans)
        {
            foreach (var plan in plans)
            {
                try
                {
                    WriteDiagnostic("Direct move start: " + plan.Item.OriginalName + " -> " + plan.Item.ProposedName);
                    NativeMoveFile(plan.Item.OriginalPath, plan.Item.ProposedPath);
                    MarkSuccess(plan);
                    WriteDiagnostic("Direct move success: " + plan.Item.ProposedName);
                }
                catch (Exception ex)
                {
                    WriteDiagnostic("Direct move failed: " + plan.Item.OriginalName + " Message=" + ex.Message);
                    MarkFailed(plan, ex.Message);
                }
            }
        }

        private static void ExecuteStaged(IList<RenameMovePlan> plans)
        {
            foreach (var plan in plans)
            {
                try
                {
                    WriteDiagnostic("Stage move start: " + plan.Item.OriginalName);
                    plan.TempPath = CreateTempPath(plan.Item.OriginalPath);
                    NativeMoveFile(plan.Item.OriginalPath, plan.TempPath);
                    plan.IsStaged = true;
                    WriteDiagnostic("Stage move success: " + plan.Item.OriginalName);
                }
                catch (Exception ex)
                {
                    WriteDiagnostic("Stage move failed: " + plan.Item.OriginalName + " Message=" + ex.Message);
                    MarkFailed(plan, ex.Message);
                }
            }

            foreach (var plan in plans.Where(plan => plan.IsStaged))
            {
                try
                {
                    WriteDiagnostic("Final move start: " + plan.Item.ProposedName);
                    if (File.Exists(plan.Item.ProposedPath))
                    {
                        throw new IOException("目标文件已存在");
                    }

                    NativeMoveFile(plan.TempPath, plan.Item.ProposedPath);
                    MarkSuccess(plan);
                    WriteDiagnostic("Final move success: " + plan.Item.ProposedName);
                }
                catch (Exception ex)
                {
                    TryRestore(plan);
                    WriteDiagnostic("Final move failed: " + plan.Item.ProposedName + " Message=" + ex.Message);
                    MarkFailed(plan, ex.Message);
                }
            }
        }

        private static bool RequiresStagedMove(IList<RenameMovePlan> plans)
        {
            var originalPaths = new HashSet<string>(
                plans.Select(plan => NormalizePath(plan.Item.OriginalPath)),
                StringComparer.OrdinalIgnoreCase);

            return plans.Any(plan =>
                IsCaseOnlyRename(plan.Item.OriginalPath, plan.Item.ProposedPath) ||
                (originalPaths.Contains(NormalizePath(plan.Item.ProposedPath)) &&
                    !string.Equals(plan.Item.OriginalPath, plan.Item.ProposedPath, StringComparison.OrdinalIgnoreCase)));
        }

        private static void MarkSuccess(RenameMovePlan plan)
        {
            plan.Item.Status = RenameStatus.Success;
            plan.Item.Message = "已重命名";
            plan.LogItem.Status = "success";
            plan.LogItem.Message = string.Empty;
        }

        private static void MarkFailed(RenameMovePlan plan, string message)
        {
            plan.Item.Status = RenameStatus.Failed;
            plan.Item.Message = message;
            plan.LogItem.Status = "failed";
            plan.LogItem.Message = message;
        }

        private static void TryRestore(RenameMovePlan plan)
        {
            try
            {
                if (File.Exists(plan.TempPath) && !File.Exists(plan.Item.OriginalPath))
                {
                    NativeMoveFile(plan.TempPath, plan.Item.OriginalPath);
                }
            }
            catch
            {
                // Keep the original failure visible to the user.
            }
        }

        private static string CreateTempPath(string originalPath)
        {
            var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            var extension = Path.GetExtension(originalPath);
            string tempPath;
            do
            {
                tempPath = Path.Combine(directory, ".abr-" + Guid.NewGuid().ToString("N") + extension);
            }
            while (File.Exists(tempPath));

            return tempPath;
        }

        private static bool IsCaseOnlyRename(string oldPath, string newPath)
        {
            return string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(oldPath, newPath, StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static void NativeMoveFile(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new IOException("源路径为空");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new IOException("目标路径为空");
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("源文件不存在", sourcePath);
            }

            if (File.Exists(destinationPath))
            {
                throw new IOException("目标文件已存在");
            }

            WriteDiagnostic("MoveFileExW call: " + Path.GetFileName(sourcePath) + " -> " + Path.GetFileName(destinationPath));
            if (!MoveFileEx(sourcePath, destinationPath, MoveFileCopyAllowed | MoveFileWriteThrough))
            {
                var error = Marshal.GetLastWin32Error();
                WriteDiagnostic("MoveFileExW failed. Error=" + error);
                throw new Win32Exception(error);
            }

            WriteDiagnostic("MoveFileExW returned success.");
        }

        private static void WriteDiagnostic(string message)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AiBatchRenamer",
                    "CrashLogs");
                Directory.CreateDirectory(logDirectory);
                File.AppendAllText(
                    Path.Combine(logDirectory, "diagnostic.log"),
                    DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine);
            }
            catch
            {
                // Diagnostics must not interrupt rename execution.
            }
        }

        private class RenameMovePlan
        {
            public RenameMovePlan(RenameItem item)
            {
                Item = item;
            }

            public RenameItem Item { get; private set; }

            public OperationLogItem LogItem { get; set; }

            public string TempPath { get; set; }

            public bool IsStaged { get; set; }
        }

        private const int MoveFileCopyAllowed = 0x2;

        private const int MoveFileWriteThrough = 0x8;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);
    }
}
