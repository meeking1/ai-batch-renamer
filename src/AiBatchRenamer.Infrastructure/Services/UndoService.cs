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

            var success = 0;
            var failed = 0;

            for (var i = log.Items.Count - 1; i >= 0; i--)
            {
                var item = log.Items[i];
                if (!string.Equals(item.Status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (!File.Exists(item.NewPath))
                    {
                        failed++;
                        continue;
                    }

                    if (File.Exists(item.OldPath))
                    {
                        failed++;
                        continue;
                    }

                    File.Move(item.NewPath, item.OldPath);
                    success++;
                }
                catch
                {
                    failed++;
                }
            }

            if (failed == 0)
            {
                logRepository.ClearLatestPointer();
            }

            var message = failed == 0
                ? "撤销完成"
                : "撤销部分完成，存在失败项";

            return new UndoResult(success > 0 && failed == 0, success, failed, message);
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
