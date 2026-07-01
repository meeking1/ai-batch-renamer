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
    }
}
