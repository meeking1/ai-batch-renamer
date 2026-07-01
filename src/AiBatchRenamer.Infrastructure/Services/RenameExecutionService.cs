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

            foreach (var item in items.Where(item => item.Status == RenameStatus.Ready))
            {
                var logItem = new OperationLogItem
                {
                    OldPath = item.OriginalPath,
                    NewPath = item.ProposedPath
                };

                try
                {
                    MoveFile(item.OriginalPath, item.ProposedPath);
                    item.Status = RenameStatus.Success;
                    item.Message = "已重命名";
                    logItem.Status = "success";
                    logItem.Message = string.Empty;
                }
                catch (Exception ex)
                {
                    item.Status = RenameStatus.Failed;
                    item.Message = ex.Message;
                    logItem.Status = "failed";
                    logItem.Message = ex.Message;
                }

                log.Items.Add(logItem);
            }

            logRepository.Save(log);
            return log;
        }

        private static void MoveFile(string oldPath, string newPath)
        {
            if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(oldPath, newPath, StringComparison.Ordinal))
            {
                var tempPath = oldPath + ".rename-tmp-" + Guid.NewGuid().ToString("N");
                File.Move(oldPath, tempPath);
                File.Move(tempPath, newPath);
                return;
            }

            File.Move(oldPath, newPath);
        }
    }
}
