using CBLDtoBLD.Services;

namespace CBLDtoBLD
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
		start:
			Console.Clear();
			Console.WriteLine(@"
██████╗ ██╗     ██╗   ██╗███████╗    ███████╗████████╗██╗   ██╗██████╗ ██╗ ██████╗      ██████╗████████╗
██╔══██╗██║     ██║   ██║██╔════╝    ██╔════╝╚══██╔══╝██║   ██║██╔══██╗██║██╔═══██╗    ██╔════╝╚══██╔══╝
██████╔╝██║     ██║   ██║███████╗    ███████╗   ██║   ██║   ██║██║  ██║██║██║   ██║    ██║        ██║   
██╔═══╝ ██║     ██║   ██║╚════██║    ╚════██║   ██║   ██║   ██║██║  ██║██║██║   ██║    ██║        ██║   
██║     ███████╗╚██████╔╝███████║    ███████║   ██║   ╚██████╔╝██████╔╝██║╚██████╔╝    ╚██████╗   ██║   
╚═╝     ╚══════╝ ╚═════╝ ╚══════╝    ╚══════╝   ╚═╝    ╚═════╝ ╚═════╝ ╚═╝ ╚═════╝      ╚═════╝   ╚═╝                                                                                                                                                                                                                                                                                                            
			");

			Console.WriteLine("Plus Studio Converter Tool. Made by PixelGuy. v2.0.0");
			Console.WriteLine("Plus Level Editor and Plus Level Studio were made by MissingTextureMan101.");
			Console.WriteLine();

			// 1) Get the right action from user
			List<string>? inputs = null;
			if (args.Length != 0) // If the files were carried within the program, it'll detect it earlier
			{
				inputs = ArgsProcessor.GetInputPaths(TargetType.Null, args);
				foreach (var input in inputs)
					ConsoleHelper.LogInfo($"Retrieved {(Directory.Exists(input) ? "folder" : "file")}: {Path.GetFileName(input)}");
				Console.WriteLine("Looks like you\'ve got some files already! Select the following converter to proceed with the carried content.");
			}

			var optionTuple = ConsoleHelper.RetrieveUserSelection("Here\'s a list of the available modes in this tool.",
				"CBLDtoBLD Converter",
				"BLDtoEBPL Converter",
				"CBLDtoRBPL Converter",
				"RBPLtoEBPL Converter"
				);
			string[] descriptions = [
				"Convert the old legacy compiled format CBLD to an editor also-legacy format BLD. That could be useful to retrieve old files, for other converters inside this program.",
				"Converts a BLD to a EBPL file. In other words, a legacy editor file to a new one! And you don\'t even lose a bit from what was in the older days.",
				"Converts a CBLD directly to a RBPL file. This is useful if you\'re aiming to port your room assets to the new format! The markers are also included",
				"Converts the RBPL file to a EBPL. This way, you can actually edit back your room!"
			];

			Console.Clear();
			Console.WriteLine($"Selected mode: {optionTuple.Item2}");
			Console.WriteLine($"Description: {descriptions[optionTuple.Item1 - 1]}");
			TargetType type = (TargetType)optionTuple.Item1;
			string typeExt = type.ToExtension();

			// Inputs setup here
			if (inputs == null)
				inputs = ArgsProcessor.GetInputPaths(type, args);
			else
			{   // remove all the inputs that don't match the required files
				for (int i = 0; i < inputs.Count; i++)
				{
					var input = inputs[i];
					if (Directory.Exists(input)) // Ignore directories
						continue;
					var inputExt = Path.GetExtension(inputs[i]);
					if (!string.Equals(inputExt, typeExt, StringComparison.OrdinalIgnoreCase))
					{
						inputs.RemoveAt(i--);
						ConsoleHelper.LogWarn($"Removed {Path.GetFileName(input)} for not being of extension {typeExt}.");
					}
				}
			}

			// Get all files based on type
			var files = FileEnumerator.ExpandToNewFiles(inputs, type);
			if (files.Count == 0)
			{
				ConsoleHelper.LogError($"No {type.ToExtension()} files found to convert. Exiting.");
				goto exit;
			}
			// Get an export folder, then do the conversion setup
			var exportFolder = ConsoleHelper.PromptForExportFolder();
			ConverterService.ConvertFiles(files, exportFolder, type);

		exit:
			args = Array.Empty<string>(); // Empties out args to now repeat the same files
			if (ConsoleHelper.CheckIfUserInputsYOrN("Restart the tool?"))
				goto start;

			Console.WriteLine("====\nPress any key to quit...");
			Console.ReadKey(true);
		}
	}
}