using PlusStudioConverterTool.Extensions;
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool
{
	internal static partial class Program
	{
		private static void Main(string[] args)
		{

			// Debug operation to get the json file all ready
			// File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "FilterObjectSample.json"),
			// 	System.Text.Json.JsonSerializer.Serialize(new FilterObject(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
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
			// 	System.Text.Json.JsonSerializer.Serialize(new FilterObject(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
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
			// 	System.Text.Json.JsonSerializer.Serialize(new FilterObject(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
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

			// ********* Only-once setup ***********
			AltLevelLoaderExtensions.InitializeSettings();

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

			if (args.Length != 0)
				ConsoleHelper.LogInfo("Some files were detected by this tool! If you\'re wishing to convert them or extract their content, select the approprieate tool below!");

			bool emptyOutArgs = false, promptRestartTool = true;

			// **) Between options
			var optionTuple = ConsoleHelper.RetrieveUserSelection("Here\'s a list of the available menus to explore inside this tool.",
					"Converter Tool", // 1
					"Content Package Extractor", // 2
					"JSON-Filter Settings" // 3
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
					emptyOutArgs = ContentPackageExtractorField(ref args);
					if (!emptyOutArgs) // If false, it wants to exit
						goto exit;
					break;
				case 3:
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
	}
}