using System.Collections.Generic;
using System.IO;
using System.Text;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class PreviewCsvExporter
    {
        public void Export(string filePath, IEnumerable<RenameItem> items)
        {
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Index,OriginalName,ProposedName,Status,Message,DirectoryPath,OriginalPath,ProposedPath");
                foreach (var item in items)
                {
                    writer.WriteLine(string.Join(",", new[]
                    {
                        Csv(item.Index.ToString()),
                        Csv(item.OriginalName),
                        Csv(item.ProposedName),
                        Csv(item.Status.ToString()),
                        Csv(item.Message),
                        Csv(item.DirectoryPath),
                        Csv(item.OriginalPath),
                        Csv(item.ProposedPath)
                    }));
                }
            }
        }

        private static string Csv(string value)
        {
            var safe = value ?? string.Empty;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }
    }
}
