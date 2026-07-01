using System;
using System.Collections.Generic;
using System.IO;
using AiBatchRenamer.Core.Models;
using AiBatchRenamer.Infrastructure.Services;

namespace AiBatchRenamer.Tests
{
    internal static class PreviewCsvExporterTests
    {
        public static void Export_WritesEscapedCsvRows()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerCsvTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            try
            {
                var path = Path.Combine(root, "preview.csv");
                var item = new RenameItem(Path.Combine(root, "old,name.txt"))
                {
                    Index = 1,
                    ProposedBaseName = "new\"name",
                    Status = RenameStatus.Ready,
                    Message = "可重命名"
                };

                new PreviewCsvExporter().Export(path, new List<RenameItem> { item });
                var csv = File.ReadAllText(path);

                TestAssert.True(csv.Contains("Index,OriginalName,ProposedName"), "csv header");
                TestAssert.True(csv.Contains("\"old,name.txt\""), "csv escapes comma");
                TestAssert.True(csv.Contains("\"new\"\"name.txt\""), "csv escapes quote");
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
