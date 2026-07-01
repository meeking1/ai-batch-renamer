using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            foreach (var plan in plans)
            {
                try
                {
                    plan.TempPath = CreateTempPath(plan.Item.OriginalPath);
                    File.Move(plan.Item.OriginalPath, plan.TempPath);
                    plan.IsStaged = true;
                }
                catch (Exception ex)
                {
                    MarkFailed(plan, ex.Message);
                }
            }

            foreach (var plan in plans.Where(plan => plan.IsStaged))
            {
                try
                {
                    if (File.Exists(plan.Item.ProposedPath))
                    {
                        throw new IOException("目标文件已存在");
                    }

                    File.Move(plan.TempPath, plan.Item.ProposedPath);
                    plan.Item.Status = RenameStatus.Success;
                    plan.Item.Message = "已重命名";
                    plan.LogItem.Status = "success";
                    plan.LogItem.Message = string.Empty;
                }
                catch (Exception ex)
                {
                    TryRestore(plan);
                    MarkFailed(plan, ex.Message);
                }
            }

            logRepository.Save(log);
            return log;
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
                    File.Move(plan.TempPath, plan.Item.OriginalPath);
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
    }
}
