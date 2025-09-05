using PlusStudioConverterTool.Extensions;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool
{
	internal static class Program
	{
		private static void Main(string[] args)
		{

		// Debug operation to get the json file all ready
		// File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "FilterObjectSample.json"),
		// 	Newtonsoft.Json.JsonConvert.SerializeObject(new FilterObject()
		// 	{
		// 		AreaType = LevelFieldType.Object,
		// 		replacements = new()
		// 		{
		// 		{ "examination", "examinationtable" },
		// 		{ "cabinettall", "cabinet" },
		// 		}
		// 	}, Newtonsoft.Json.Formatting.Indented)
		// );
		// File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "FilterDoorSample.json"),
		// 	Newtonsoft.Json.JsonConvert.SerializeObject(new FilterObject()
		// 	{
		// 		AreaType = LevelFieldType.Door,
		// 		replacements = new()
		// 		{
		// 		{ "swing", "swinging" },
		// 		{ "swingsilent", "swinging_silent" },
		// 		{ "coin", "coinswinging" },
		// 		}
		// 	}, Newtonsoft.Json.Formatting.Indented)
		// );
		// File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "FilterTextureSample.json"),
		// 	Newtonsoft.Json.JsonConvert.SerializeObject(new FilterObject()
		// 	{
		// 		AreaType = LevelFieldType.RoomTexture,
		// 		replacements = new()
		// 		{
		// 		{ "FacultyWall", "WallWithMolding" },
		// 		{ "Actual", "TileFloor" }
		// 		}
		// 	}, Newtonsoft.Json.Formatting.Indented)
		// );
		// return;
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
			ConfigurationHandler.InitializeConfigFile();

			Console.WriteLine();

			bool emptyOutArgs = false, promptRestartTool = true;

			// **) Between options
			var optionTuple = ConsoleHelper.RetrieveUserSelection("Here\'s a list of the available menus to explore inside this tool.",
					"Converter Tool", // 1
					"JSON-Filter Settings" // 2
					);
			Console.Clear();
			switch (optionTuple.Item1)
			{
				case 1:
					emptyOutArgs = ConverterField(ref args);
					if (!emptyOutArgs) // If false, it wants to exit
						goto exit;
					break;
				case 2:
					JSONConfigField();
					promptRestartTool = false;
					break;
			}



		exit:
			if (emptyOutArgs && args.Length != 0)
				args = []; // Empties out args to now repeat the same files
			if (!promptRestartTool || ConsoleHelper.CheckIfUserInputsYOrN("Restart the tool?"))
				goto start;

			Console.WriteLine("====\nPress any key to quit...");
			Console.ReadKey(true);
		}

		// If False, goes directly to exit
		static bool ConverterField(ref string[] args)
		{
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
				return false;
			}
			// Get an export folder, then do the conversion setup
			var exportFolder = ConsoleHelper.PromptForExportFolder();
			ConverterService.ConvertFiles(files, exportFolder, type);
			return true;
		}

		static void JSONConfigField()
		{
			while (true)
			{
				var jsonPaths = ConfigurationHandler.configFile.jsonFilterPaths;
				for (int i = 0; i < jsonPaths.Count; i++)
				{
					var fName = Path.GetFileName(jsonPaths[i]);
					Console.WriteLine($"[{i}] \'{fName}\'");
				}
				Console.WriteLine($"There are currently loaded in {jsonPaths.Count} JSON files.");
				var optionTuple = ConsoleHelper.RetrieveUserSelection("What do you want to do with them?",
						"Add", // 1
						"Remove", // 2
						"Exit"
						);
				switch (optionTuple.Item1)
				{
					case 1:
					addNew:
						string? jsonPath = ConsoleHelper.RetrieveUserFilePath(".json", "Please, input the JSON file to be scanned.");
						if (string.IsNullOrEmpty(jsonPath))
							break;

						jsonPaths.Add(jsonPath);
						if (ConsoleHelper.CheckIfUserInputsYOrN("Is there a new JSON to be inserted before re-loading the filters?"))
							goto addNew;

						ConfigurationHandler.DeserializeFilters();
						ConfigurationHandler.TryReserializeConfigFile();
						break;
					case 2:
					removeOld:
						int num = ConsoleHelper.RetrieveUserNumber(0, jsonPaths.Count - 1, "Check the JSON list logged above and select the number you want to delete. Select \'-1\' to cancel.");
						if (num == -1)
							break;

						ConsoleHelper.LogSuccess($"Removed {Path.GetFileName(jsonPaths[num])} successfully!");
						jsonPaths.RemoveAt(num);

						if (ConsoleHelper.CheckIfUserInputsYOrN("Do you wish to delete another JSON from this list?"))
							goto removeOld;

						break;
					case 3:
						return; // Exit this while(true) loop
				}
			}
		}
	}
}