namespace PlusStudioConverterTool.Services
{
    internal static class ConsoleHelper
    {
        // Logging
        public static void LogInfo(string message) => LogWithColor(message, ConsoleColor.DarkBlue);
        public static void LogConverterInfo(string message) => LogWithColor(message, ConsoleColor.Blue);
        public static void LogWarn(string message) => LogWithColor(message, ConsoleColor.DarkYellow);
        public static void LogSuccess(string message) => LogWithColor(message, ConsoleColor.Green);
        public static void LogError(string message) => LogWithColor(message, ConsoleColor.Red);

        // **************** PROMPTS FOR GETTING USEFUL DATA **********************
        // Y or N question-type prompt
        public static bool CheckIfUserInputsYOrN(string? promptMessage = null) // True if Y, False if anything else
        {
            if (!string.IsNullOrEmpty(promptMessage))
                Console.WriteLine(promptMessage + " (Y/N)");

            var answer = Console.ReadLine()?.Trim();
            return !string.IsNullOrEmpty(answer) && answer.Equals("y", StringComparison.OrdinalIgnoreCase);
        }
        // Create a multi-selection prompt
        public static (int, string) RetrieveUserSelection(string? promptMessage = null, params string[] availableOptions)
        {
            if (availableOptions == null || availableOptions.Length == 0)
                throw new ArgumentException("No options were provided for the user\'s selection input.");

            if (!string.IsNullOrEmpty(promptMessage))
                Console.WriteLine(promptMessage);

            for (int i = 0; i < availableOptions.Length; i++)
                Console.WriteLine($"[{i + 1}] - {availableOptions[i]}");

            Console.WriteLine($"Select an option between 1 and {availableOptions.Length}:");
            var value = Console.ReadLine();

            while (true)
            {
                if (int.TryParse(value, out int option) && option > 0 && option <= availableOptions.Length)
                    return (option, availableOptions[option - 1]);

                Console.WriteLine($"Please, input a number between 1 and {availableOptions.Length}:");
                value = Console.ReadLine();
            }
        }
        // Retrieve an integer
        public static int RetrieveUserNumber(int min, int max, string? promptMessage = null)
        {
            if (min > max || max == 0)
                throw new ArgumentException("No options were provided for the user\'s selection input.");

            if (!string.IsNullOrEmpty(promptMessage))
                Console.WriteLine(promptMessage);

            Console.WriteLine($"Select a number between {min} and {max}:");
            var value = Console.ReadLine();

            while (true)
            {
                if (int.TryParse(value, out int option) && option >= min && option <= max)
                    return option;

                Console.WriteLine($"Select a number between {min} and {max}:");
                value = Console.ReadLine();
            }
        }
        // Retrieve file path
        public static string? RetrieveUserFilePath(string expectedExtension, string? promptMessage = null)
        {
            if (promptMessage != null)
                Console.WriteLine(promptMessage);
            while (true)
            {
                var userPath = Console.ReadLine();
                if (string.Equals(userPath, "C", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (string.IsNullOrWhiteSpace(userPath) || Path.GetExtension(userPath) != expectedExtension)
                {
                    Console.WriteLine("Please enter a valid file path or type \'C\' to cancel this operation.");
                    continue;
                }

                var path = Path.GetFullPath(userPath.Trim('"'));
                if (File.Exists(path))
                    return path;
                else
                    Console.WriteLine("Please input a path to a file that actually exists or type \'C\' to cancel this operation.");
            }
        }
        // Get the export folder for all conversions
        public static string? PromptForExportFolder()
        {
            if (!CheckIfUserInputsYOrN("Do you want to export converted files into a specific folder?"))
                return null;

            Console.WriteLine("Enter the export folder path (will be created if it doesn't exist):");
            while (true)
            {
                var path = Console.ReadLine();
                if (string.Equals(path, "C", StringComparison.OrdinalIgnoreCase))
                    return null;
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine("Please enter a valid folder path or type \'C\' to not set an export folder:");
                    continue;
                }


                var full = Path.GetFullPath(path.Trim('"'));
                try
                {
                    if (Directory.Exists(full) && Directory.EnumerateFileSystemEntries(full).Any()) // If there's anything inside this Directory (as long as it exists)
                    {
                        LogWarn($"The folder you inserted seems to already contain content inside.\nUsing this path as the export folder will automatically delete everything inside this directory.");
                        if (!CheckIfUserInputsYOrN("Are you sure you want to proceed?"))
                        {
                            Console.WriteLine("Enter another folder or press or type \'C\' to not set an export folder:");
                            continue; // Goes back once again
                        }
                        Directory.Delete(full, true); // Deletes the folder, to re-create it
                    }
                    Directory.CreateDirectory(full);
                    return full;
                }
                catch (Exception ex)
                {
                    LogError($"Unable to create, delete or use folder \'{full}\': {ex.Message}");
                    Console.WriteLine("Enter another folder or press or type \'C\' to not set an export folder:");
                }
            }
        }
        // ************** Private Methods for Reutility ************
        static void LogWithColor(string message, ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = prev;
        }
    }
}
