
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool
{
	internal static partial class Program
	{
		static void JSONConfigField()
		{
			while (true)
			{
				Console.Clear();
				var jsonPaths = ConfigurationHandler.configFile.jsonFilterPaths;
				List<string> backupJsonPaths = [.. jsonPaths];
				void LogAllJsons()
				{
					for (int i = 0; i < jsonPaths.Count; i++)
					{
						var fName = Path.GetFileName(jsonPaths[i]);
						Console.WriteLine($"[{i}] \'{fName}\'");
					}
				}
				void ReloadBackupJsonPaths()
				{
					jsonPaths.Clear();
					jsonPaths.AddRange(backupJsonPaths);
				}
				void UpdateBackupJsonPaths()
				{
					backupJsonPaths.Clear();
					backupJsonPaths.AddRange(jsonPaths);
				}

				LogAllJsons();

				Console.WriteLine($"There are currently loaded in {jsonPaths.Count} JSON files.");
				var optionTuple = ConsoleHelper.RetrieveUserSelection("What do you want to do with them?",
						"Add", // 1
						jsonPaths.Count != 0 ? "Remove" : "Remove (Unavailable)", // 2
						jsonPaths.Count != 0 ? "Remove Range" : "Remove Range (Unavailable)", // 3
						"Reload filters", // 4
						"Display active filters", // 5
						"Exit"
						);
				bool hasRetryed = false;
				int oldJsonPathsCount = jsonPaths.Count;
				int num;
				switch (optionTuple.Item1)
				{
					case 1:
					addNew:
						string? jsonPath = ConsoleHelper.RetrieveUserFilePath(".json", "Please, input the JSON file to be scanned. Leave empty to reload the filters (if there are any changes).");
						if (!string.IsNullOrEmpty(jsonPath))
						{
							if (jsonPaths.Contains(jsonPath))
							{
								ConsoleHelper.LogError("Looks like this path is already registered in the tool!");
								goto addNew;
							}

							jsonPaths.Add(jsonPath);
							if (ConsoleHelper.CheckIfUserInputsYOrN("Is there a new JSON to be inserted before re-loading the filters?"))
								goto addNew;
						}
						if (jsonPaths.Count != oldJsonPathsCount)
						{
							ConfigurationHandler.DeserializeFilters();
							ConfigurationHandler.TryReserializeConfigFile();
							UpdateBackupJsonPaths();
							ConsoleHelper.WaitToProceed();
						}
						break;
					case 2:
						if (jsonPaths.Count == 0) break;
						removeOld:
						if (hasRetryed)
							LogAllJsons();

						num = ConsoleHelper.RetrieveUserNumber(0, jsonPaths.Count - 1, -1, "Check the JSON list logged above and select the number you want to delete. Type \'-1\' to select none.");
						if (num != -1)
						{
							ConsoleHelper.LogSuccess($"Removed {Path.GetFileName(jsonPaths[num])} successfully!");
							jsonPaths.RemoveAt(num);

							if (jsonPaths.Count == 0 && !ConsoleHelper.CheckIfUserInputsYOrN("Are you sure you want to delete all the JSON filters?"))
							{
								ReloadBackupJsonPaths();
								break; // Stop from here
							}
							if (ConsoleHelper.CheckIfUserInputsYOrN("Do you wish to delete another JSON from this list before re-loading the filters?"))
							{
								hasRetryed = true;
								goto removeOld;
							}
						}

						if (oldJsonPathsCount != jsonPaths.Count)
						{
							ConfigurationHandler.DeserializeFilters();
							ConfigurationHandler.TryReserializeConfigFile();
							UpdateBackupJsonPaths();
							ConsoleHelper.WaitToProceed();
						}
						break;
					case 3:
						if (jsonPaths.Count == 0) break;
						removeJsons:
						if (hasRetryed)
							LogAllJsons();

						if (ConsoleHelper.TryRetrieveUserRange(0, jsonPaths.Count - 1, "-1", out var range, "Check the JSON list logged above and select the range you want to delete. Type \'-1\' to select none."))
						{
							int diff = range.Difference + 1;
							ConsoleHelper.LogSuccess($"Removed {diff} successfully!");
							jsonPaths.RemoveRange(range.Min, diff);

							if (jsonPaths.Count == 0 && !ConsoleHelper.CheckIfUserInputsYOrN("Are you sure you want to delete all the JSON filters?"))
							{
								ReloadBackupJsonPaths();
								break; // Stop from here
							}
							if (ConsoleHelper.CheckIfUserInputsYOrN("Do you wish to perform another JSON mass-deletion inside this list before re-loading the filters?"))
							{
								hasRetryed = true;
								goto removeJsons;
							}
						}

						if (oldJsonPathsCount != jsonPaths.Count)
						{
							ConfigurationHandler.DeserializeFilters();
							ConfigurationHandler.TryReserializeConfigFile();
							UpdateBackupJsonPaths();
							ConsoleHelper.WaitToProceed();
						}
						break;
					case 4:
						ConfigurationHandler.DeserializeFilters();
						ConsoleHelper.WaitToProceed();
						break;
					case 5:
						foreach (var kvp in ConfigurationHandler.filterKeyPairs)
						{
							ConsoleHelper.LogInfo($"### FILTER FOR TYPE \'{kvp.Key}\' ###");
							if (kvp.Value.replacements.Count != 0)
							{
								ConsoleHelper.LogConverterInfo("-- Replacements --");
								foreach (var replacement in kvp.Value.replacements) ConsoleHelper.LogSuccess($"\'{replacement.Key}\' => \'{replacement.Value}\'");
							}
							if (kvp.Value.exclusions.Count != 0)
							{
								ConsoleHelper.LogConverterInfo("-- Exclusions --");
								foreach (var exclusion in kvp.Value.exclusions) ConsoleHelper.LogError(exclusion);
							}
						}
						ConsoleHelper.WaitToProceed();
						break;
					case 6:
						return; // Exit this while(true) loop
				}
			}
		}
	}
}