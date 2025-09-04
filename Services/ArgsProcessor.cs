namespace CBLDtoBLD.Services
{
    internal static class ArgsProcessor
    {
        // Returns a list of input paths (files or directories). If args are empty,
        // prompt the user to enter a path or folder.
        public static List<string> GetInputPaths(TargetType type, params string[] args) =>
            InternalGetInputPaths(args, type.ToExtension());
        static List<string> InternalGetInputPaths(string[] args, string expectedExtension)
        {
            var results = new List<string>();
            var inputs = new List<string>();

            if (args.Length != 0) // Get any args if they are available
            {
                foreach (var a in args)
                {
                    if (!string.IsNullOrWhiteSpace(a))
                        inputs.Add(a.Trim('"'));
                }
            }
            else
            {
                Console.WriteLine($"Please input the path to a {expectedExtension} file or a folder containing {expectedExtension} files:");
                while (inputs.Count == 0)
                {
                    string? input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                        inputs.Add(input.Trim('"'));
                    else
                        Console.WriteLine("Please enter a valid file or folder path:");
                }
            }

            foreach (var raw in inputs) // Subfolder sarch part
            {
                string fullPath;
                try { fullPath = Path.GetFullPath(raw); }
                catch { fullPath = raw; }

                if (Directory.Exists(fullPath))
                {
                    // Ask the user whether to search subfolders
                    // To go recursive, it needs to know if there are actual directories to search for
                    bool recursive = Directory.EnumerateDirectories(fullPath).Any() && ConsoleHelper.CheckIfUserInputsYOrN($"Folder detected: '{fullPath}'. Do you want to search its subfolders recursively?");
                    try
                    {
                        var found = Directory.EnumerateFiles(fullPath, $"*{expectedExtension}", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        var added = 0;
                        foreach (var f in found)
                        {
                            results.Add(f);
                            added++;
                        }
                        if (added == 0)
                            ConsoleHelper.LogWarn($"No {expectedExtension} files found in folder '{fullPath}'.");
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.LogError($"Error while enumerating files in '{fullPath}': {ex.Message}");
                    }
                }
                else if (File.Exists(fullPath))
                {
                    if (string.IsNullOrEmpty(expectedExtension) || string.Equals(Path.GetExtension(fullPath), expectedExtension, StringComparison.OrdinalIgnoreCase))
                        results.Add(fullPath);
                    else
                        ConsoleHelper.LogWarn($"File '{fullPath}' does not match expected extension '{expectedExtension}', skipping.");
                }
                else
                {
                    ConsoleHelper.LogWarn($"Path '{raw}' does not exist, skipping.");
                }
            }

            return results;
        }
    }
}
