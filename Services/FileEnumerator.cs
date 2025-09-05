using PlusStudioConverterTool.Extensions;
using PlusStudioConverterTool.Models;

namespace PlusStudioConverterTool.Services
{
    internal static class FileEnumerator
    {
        public static List<string> ExpandToNewFiles(List<string> inputs, TargetType type, bool logFoundFiles = true) =>
            InternalExpandToNewFiles(inputs, type.ToExtension(), logFoundFiles);


        // Given input paths (files or directories), return a list of absolute paths
        // pointing to .cbld files. Non-existent inputs are ignored.
        static List<string> InternalExpandToNewFiles(List<string> inputs, string extensionCheck, bool logFoundFiles = true)
        {
            var results = new List<string>();

            foreach (var inPath in inputs)
            {
                if (File.Exists(inPath) && string.Equals(Path.GetExtension(inPath), extensionCheck, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(Path.GetFullPath(inPath));
                    continue;
                }

                if (Directory.Exists(inPath))
                {
                    try
                    {
                        var files = Directory.EnumerateFiles(inPath, "*" + extensionCheck, SearchOption.AllDirectories);
                        results.AddRange(files.Select(Path.GetFullPath));
                    }
                    catch (Exception)
                    {
                        // Ignore directories we can't access.
                    }

                    continue;
                }

                // If path doesn't exist, attempt to treat it as a file with extension.
                var possible = inPath.EndsWith(extensionCheck, StringComparison.OrdinalIgnoreCase) ? inPath : inPath + extensionCheck;
                if (File.Exists(possible))
                    results.Add(Path.GetFullPath(possible));
            }

            // No duplicates allowed
            results = [.. results.Distinct(StringComparer.OrdinalIgnoreCase)];

            if (logFoundFiles)
                results.ForEach(path => ConsoleHelper.LogInfo($"Detected {extensionCheck} file: {Path.GetFileName(path)}"));

            return results;
        }
    }
}
