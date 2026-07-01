using System;
using System.Collections.Generic;
using System.IO;
using AiBatchRenamer.Core.Models;
using AiBatchRenamer.Core.Services;
using AiBatchRenamer.Infrastructure.Services;

namespace AiBatchRenamer.Tests
{
    internal static class RenameExecutionTests
    {
        public static void ExecuteAndUndo_RenamesFilesAndRestoresOriginalNames()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var originalPath = Path.Combine(root, "original.txt");
                File.WriteAllText(originalPath, "content");

                var item = new RenameItem(originalPath)
                {
                    Index = 1,
                    ProposedBaseName = "renamed"
                };
                var items = new List<RenameItem> { item };
                new RenameValidationService().Validate(items);

                var logs = Path.Combine(root, "logs");
                var repository = new OperationLogRepository(logs);
                var executionService = new RenameExecutionService(repository);
                var undoService = new UndoService(repository);

                var log = executionService.Execute(items);

                TestAssert.Equal(1, log.Items.Count, "log item count");
                TestAssert.True(File.Exists(Path.Combine(root, "renamed.txt")), "renamed file exists");
                TestAssert.True(!File.Exists(originalPath), "original file moved");

                var undo = undoService.UndoLatest();

                TestAssert.Equal(1, undo.SuccessCount, "undo success count");
                TestAssert.Equal(0, undo.FailedCount, "undo failed count");
                TestAssert.True(File.Exists(originalPath), "original file restored");
                TestAssert.True(!File.Exists(Path.Combine(root, "renamed.txt")), "renamed file removed");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        public static void ListRecent_ReturnsSavedOperationLogs()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerLogTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var repository = new OperationLogRepository(root);
                repository.Save(new OperationLog
                {
                    OperationId = "20260701-000000-001",
                    CreatedAt = DateTimeOffset.Now.ToString("o")
                });
                repository.Save(new OperationLog
                {
                    OperationId = "20260701-000000-002",
                    CreatedAt = DateTimeOffset.Now.ToString("o")
                });

                var logs = repository.ListRecent(10);

                TestAssert.Equal(2, logs.Count, "recent log count");
                TestAssert.True(logs[0].OperationId.Length > 0, "recent log has id");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        public static void Execute_SwapsFileNamesAndUndoRestoresOriginals()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerSwapTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var firstPath = Path.Combine(root, "a.txt");
                var secondPath = Path.Combine(root, "b.txt");
                File.WriteAllText(firstPath, "first");
                File.WriteAllText(secondPath, "second");

                var first = new RenameItem(firstPath)
                {
                    Index = 1,
                    ProposedBaseName = "b"
                };
                var second = new RenameItem(secondPath)
                {
                    Index = 2,
                    ProposedBaseName = "a"
                };
                var items = new List<RenameItem> { first, second };
                new RenameValidationService().Validate(items);

                var repository = new OperationLogRepository(Path.Combine(root, "logs"));
                var log = new RenameExecutionService(repository).Execute(items);

                TestAssert.Equal(2, log.Items.Count, "swap log item count");
                TestAssert.Equal("second", File.ReadAllText(firstPath), "swap first path content");
                TestAssert.Equal("first", File.ReadAllText(secondPath), "swap second path content");

                var undo = new UndoService(repository).UndoLatest();

                TestAssert.Equal(2, undo.SuccessCount, "swap undo success count");
                TestAssert.Equal(0, undo.FailedCount, "swap undo failed count");
                TestAssert.Equal("first", File.ReadAllText(firstPath), "undo first path content");
                TestAssert.Equal("second", File.ReadAllText(secondPath), "undo second path content");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        public static void Undo_CaseOnlyRename_DoesNotTreatOriginalAsConflict()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerCaseUndoTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var originalPath = Path.Combine(root, "case.txt");
                File.WriteAllText(originalPath, "content");

                var item = new RenameItem(originalPath)
                {
                    Index = 1,
                    ProposedBaseName = "CASE"
                };
                var items = new List<RenameItem> { item };
                new RenameValidationService().Validate(items);

                TestAssert.Equal(RenameStatus.Ready, item.Status, "case-only rename remains executable");

                var repository = new OperationLogRepository(Path.Combine(root, "logs"));
                new RenameExecutionService(repository).Execute(items);

                var result = new UndoService(repository).UndoLatest();

                TestAssert.Equal(1, result.SuccessCount, "case-only undo success count");
                TestAssert.Equal(0, result.FailedCount, "case-only undo failed count");
                TestAssert.True(File.Exists(originalPath), "case-only undo restores original path");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        public static void UndoSpecificLog_DoesNotClearLatestPointer()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerSpecificUndoTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var originalPath = Path.Combine(root, "specific.txt");
                File.WriteAllText(originalPath, "content");

                var item = new RenameItem(originalPath)
                {
                    Index = 1,
                    ProposedBaseName = "specific-renamed"
                };
                var items = new List<RenameItem> { item };
                new RenameValidationService().Validate(items);

                var repository = new OperationLogRepository(Path.Combine(root, "logs"));
                var log = new RenameExecutionService(repository).Execute(items);

                var result = new UndoService(repository).Undo(log, false);

                TestAssert.Equal(1, result.SuccessCount, "specific undo success count");
                TestAssert.True(repository.LoadLatest() != null, "specific undo keeps latest pointer");
                TestAssert.True(File.Exists(originalPath), "specific undo restores file");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
