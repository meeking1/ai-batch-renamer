using System;
using System.IO;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class UndoService
    {
        private readonly OperationLogRepository logRepository;

        public UndoService(OperationLogRepository logRepository)
        {
            this.logRepository = logRepository;
        }

        public UndoResult UndoLatest()
        {
            var log = logRepository.LoadLatest();
            if (log == null)
            {
                return new UndoResult(false, 0, 0, "没有可撤销的操作");
            }

            return Undo(log, true);
        }

        public UndoResult Undo(OperationLog log, bool clearLatestPointer)
        {
            if (log == null)
            {
                return new UndoResult(false, 0, 0, "没有可撤销的操作");
            }

            var success = 0;
            var failed = 0;
            var plans = new System.Collections.Generic.List<UndoMovePlan>();

            for (var i = log.Items.Count - 1; i >= 0; i--)
            {
                var item = log.Items[i];
                if (!string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                plans.Add(new UndoMovePlan(item));
            }

            foreach (var plan in plans)
            {
                try
                {
                    if (!File.Exists(plan.Item.NewPath))
                    {
                        failed++;
                        continue;
                    }

                    plan.TempPath = CreateTempPath(plan.Item.NewPath);
                    File.Move(plan.Item.NewPath, plan.TempPath);
                    plan.IsStaged = true;
                }
                catch
                {
                    failed++;
                }
            }

            foreach (var plan in plans)
            {
                if (!plan.IsStaged)
                {
                    continue;
                }

                try
                {
                    if (File.Exists(plan.Item.OldPath))
                    {
                        throw new IOException("原路径已被占用");
                    }

                    File.Move(plan.TempPath, plan.Item.OldPath);
                    success++;
                }
                catch
                {
                    TryRestore(plan);
                    failed++;
                }
            }

            if (failed == 0 && clearLatestPointer)
            {
                logRepository.ClearLatestPointer();
            }

            var message = failed == 0
                ? "撤销完成"
                : "撤销部分完成，存在失败项";

            return new UndoResult(success > 0 && failed == 0, success, failed, message);
        }

        private static void TryRestore(UndoMovePlan plan)
        {
            try
            {
                if (File.Exists(plan.TempPath) && !File.Exists(plan.Item.NewPath))
                {
                    File.Move(plan.TempPath, plan.Item.NewPath);
                }
            }
            catch
            {
                // Keep the undo result focused on the failed item count.
            }
        }

        private static string CreateTempPath(string sourcePath)
        {
            var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            var extension = Path.GetExtension(sourcePath);
            string tempPath;
            do
            {
                tempPath = Path.Combine(directory, ".abr-undo-" + Guid.NewGuid().ToString("N") + extension);
            }
            while (File.Exists(tempPath));

            return tempPath;
        }

        private class UndoMovePlan
        {
            public UndoMovePlan(OperationLogItem item)
            {
                Item = item;
            }

            public OperationLogItem Item { get; private set; }

            public string TempPath { get; set; }

            public bool IsStaged { get; set; }
        }
    }

    public class UndoResult
    {
        public UndoResult(bool isSuccess, int successCount, int failedCount, string message)
        {
            IsSuccess = isSuccess;
            SuccessCount = successCount;
            FailedCount = failedCount;
            Message = message;
        }

        public bool IsSuccess { get; private set; }

        public int SuccessCount { get; private set; }

        public int FailedCount { get; private set; }

        public string Message { get; private set; }
    }
}
