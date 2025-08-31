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
					ConsoleHelper.LogInfo($"Retrieved file: {Path.GetFileName(input)}");
				Console.WriteLine("Looks like you\'ve got some files already! Select the following converter to proceed with the carried content.");
			}
			var optionTuple = ConsoleHelper.RetrieveUserSelection("Here\'s a list of the available modes in this tool.",
				"CBLDtoBLD Converter.",
				"BLDtoEBPL Converter"
				);
			Console.Clear();
			Console.WriteLine($"Selected mode: {optionTuple.Item2}");
			TargetType type = (TargetType)optionTuple.Item1;
			string typeExt = type.ToExtension();

			// 1 - CBLDtoBLD
			// 2 - BLDtoBPL
			if (inputs == null)
				inputs = ArgsProcessor.GetInputPaths(type, args);
			else
			{   // remove all the inputs that don't match the required files
				for (int i = 0; i < inputs.Count; i++)
				{
					var input = inputs[i];
					var inputExt = Path.GetExtension(inputs[i]);
					if (!string.Equals(inputExt, typeExt, StringComparison.OrdinalIgnoreCase))
					{
						inputs.RemoveAt(i--);
						ConsoleHelper.LogWarn($"Removed {Path.GetFileName(input)} for not being of extension {typeExt}.");
					}
				}
			}

			var files = FileEnumerator.ExpandToNewFiles(inputs, type);
			if (files.Count == 0)
			{
				ConsoleHelper.LogError($"No {type.ToExtension()} files found to convert. Exiting.");
				goto exit;
			}
			var exportFolder = ConsoleHelper.PromptForExportFolder();
			ConverterService.ConvertFiles(files, exportFolder, type);

		exit:

			if (ConsoleHelper.CheckIfUserInputsYOrN("Restart the tool?"))
				goto start;

			Console.WriteLine("====\nPress any key to quit...");
			Console.ReadKey(true);
		}
	}
}