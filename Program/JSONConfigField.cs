
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool
{
	internal static partial class Program
	{
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