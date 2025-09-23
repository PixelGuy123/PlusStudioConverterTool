
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool
{
	internal static partial class Program
	{
		// Bool: Indicate whether to clean up the args or not
		// Bool: indicates whether to prompt restart tool or directly restart the tool (false to direct restart)
		static (bool, bool) EBPLFilterField(ref string[] args)
		{
			while (true)
			{
				Console.WriteLine("Welcome to the EBPL Filter tool!");
				Console.WriteLine("If you want to run the JSON-Filters through a EBPL file, you can do it through this tool.\nIt\'ll return back a filtered version of the EBPL file.");
				// 1) Get the right action from user
				List<string>? inputs = null;
				if (args.Length != 0) // If the files were carried within the program, it'll detect it earlier
				{
					inputs = ArgsProcessor.GetInputPaths(TargetType.Null, args);
					foreach (var input in inputs)
						ConsoleHelper.LogInfo($"Retrieved {(Directory.Exists(input) ? "folder" : "file")}: {Path.GetFileName(input)}");
					Console.WriteLine("Looks like you\'ve got some files already! Select the following converter to proceed with the carried content.");
				}

				// Select a specific extractor, since both files have different encodings
				var optionTuple = ConsoleHelper.RetrieveUserSelection("Here\'s a list of the available modes in this tool.",
					"EBPL Filter",
					"Exit"
					);

				if (optionTuple.Item2 == "Exit") // Literally exits the tool
					return (false, false);

				// Get the right extension
				string extension = optionTuple.Item1 switch
				{
					1 => ".ebpl",
					_ => string.Empty // Should never happen technically
				};

				// Inputs setup here
				if (inputs == null)
					inputs = ArgsProcessor.GetInputPaths(extension, args);
				else
				{   // remove all the inputs that don't match the required files
					for (int i = 0; i < inputs.Count; i++)
					{
						var input = inputs[i];
						if (Directory.Exists(input)) // Ignore directories
							continue;
						var inputExt = Path.GetExtension(inputs[i]);
						if (!string.Equals(inputExt, extension, StringComparison.OrdinalIgnoreCase))
						{
							inputs.RemoveAt(i--);
							ConsoleHelper.LogWarn($"Removed {Path.GetFileName(input)} for not being of extension {extension}.");
						}
					}
				}

				// Get all files based on type
				var files = FileEnumerator.ExpandToNewFiles(inputs, extension);
				if (files.Count == 0)
				{
					ConsoleHelper.LogError($"No {extension} files found to be extracted. Exiting.");
					return (false, true);
				}
				// Get an export folder, then do the conversion setup
				var exportFolder = ConsoleHelper.PromptForExportFolder(false);
				ConverterService.CleanUpEBPLFiles(files, exportFolder);
				return (true, true);
			}
		}
	}
}