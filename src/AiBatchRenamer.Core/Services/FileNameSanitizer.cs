using System.IO;
using System.Text;

namespace AiBatchRenamer.Core.Services
{
    public static class FileNameSanitizer
    {
        public static string SanitizeBaseName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Trim());
            for (var i = 0; i < builder.Length; i++)
            {
                for (var j = 0; j < invalid.Length; j++)
                {
                    if (builder[i] == invalid[j])
                    {
                        builder[i] = '-';
                        break;
                    }
                }
            }

            return builder.ToString().TrimEnd('.', ' ');
        }

        public static bool ContainsInvalidFileNameChars(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
        }
    }
}
