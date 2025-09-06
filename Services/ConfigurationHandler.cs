using System.Text.Json;
using PlusStudioConverterTool.Models;


namespace PlusStudioConverterTool.Services;

internal static class ConfigurationHandler
{
    // Some important constants
    const string jsonConfigFileName = "configs.json";
    static string configPath = string.Empty;
    // marker
    static bool hasStartedConfigFile = false;

    // Actual handle here
    public static void InitializeConfigFile()
    {
        if (hasStartedConfigFile)
            return;
        hasStartedConfigFile = true;

        configPath = Path.Combine(Directory.GetCurrentDirectory(), jsonConfigFileName);
        if (File.Exists(configPath))
        {
            try
            {
                var preConfigFile = JsonSerializer.Deserialize(File.ReadAllText(configPath), AppJsonContext.Default.ConfigFile) ?? throw new Exception("Failed to load config file.");
                configFile = (ConfigFile)preConfigFile;
                InternalDeserializeFilters(false);
            }
            catch (Exception e)
            {
                ConsoleHelper.LogError("Failed to read the config file as the content may be corrupted or wrongly structured. Using default settings...");
                ConsoleHelper.LogError(e.ToString());
            }
            return;
        }
        ConsoleHelper.LogWarn("No config file found! Creating a new one...");
        InternalTryReserializeConfigFile(false);
    }
    public static bool TryReserializeConfigFile() =>
        InternalTryReserializeConfigFile(true);
    static bool InternalTryReserializeConfigFile(bool logSaving)
    {
        if (string.IsNullOrEmpty(configPath)) return false;
        if (logSaving)
            ConsoleHelper.LogInfo("Saving config file...");
        try
        {
            File.WriteAllText(configPath, JsonSerializer.Serialize(configFile, AppJsonContext.Default.ConfigFile));
            if (logSaving)
                ConsoleHelper.LogSuccess("Saved config file with success!");
            return true;
        }
        catch (Exception e) // Should never happen...
        {
            ConsoleHelper.LogError("Failed to write the config file! Please contact the developer with the following log below:");
            ConsoleHelper.LogError(e.ToString());
            return false;
        }
    }
    // For each field type, there's a key-pair check for replacing names in this global filter dictionary
    public static Dictionary<LevelFieldType, FilterObject> filterKeyPairs = [];
    public static ConfigFile configFile = new();

    public static void DeserializeFilters() => InternalDeserializeFilters(true);
    static void InternalDeserializeFilters(bool log)
    {
        if (log)
            ConsoleHelper.LogInfo("Deserializing the JSONs available...");
        for (int i = 0; i < configFile.jsonFilterPaths.Count; i++)
        {
            var jsonPath = configFile.jsonFilterPaths[i];
            // Invalid json, just skip and remove it from the list
            if (!File.Exists(jsonPath) || Path.GetExtension(jsonPath) != ".json")
            {
                ConsoleHelper.LogWarn($"Removed a filter due to not existing or having the invalid extension (\'{jsonPath}\')");
                configFile.jsonFilterPaths.RemoveAt(i--);
                continue;
            }
            string fileName = Path.GetFileName(jsonPath);
            try
            {
                // Read file contents and deserialize
                var fileContent = File.ReadAllText(jsonPath);
                var filterObj = JsonSerializer.Deserialize(fileContent, AppJsonContext.Default.FilterObject) ?? throw new Exception("Failed to load filter object.");


                if (filterObj.replacements.Count == 0 && filterObj.exclusions.Count == 0)
                {
                    ConsoleHelper.LogWarn($"Filter '{Path.GetFileName(jsonPath)}' contains no replacements, nor exclusions. Skipping.");
                    configFile.jsonFilterPaths.RemoveAt(i--);
                    continue;
                }

                if (!Enum.TryParse<LevelFieldType>(filterObj.AreaType, out var areaType))
                {
                    ConsoleHelper.LogWarn($"Filter '{Path.GetFileName(jsonPath)}' contains an invalid AreaType. Skipping.");
                    configFile.jsonFilterPaths.RemoveAt(i--);
                    continue;
                }

                // Ensure a dictionary exists for this AreaType
                if (!filterKeyPairs.TryGetValue(areaType, out var newFilterObj))
                {
                    filterKeyPairs.Add(areaType, filterObj);
                    continue; // Go to the next filter object
                }

                // Merge entries with validation
                foreach (var kv in filterObj.replacements)
                {
                    if (newFilterObj.exclusions.Contains(kv.Key) || newFilterObj.exclusions.Contains(kv.Value))
                    {
                        ConsoleHelper.LogWarn($"Skipped replacement (\'{kv.Key}\' => \'{kv.Value}\') also appearing as an exclusion. May be from the JSON: \'{fileName}\'.");
                        continue;
                    }
                    // already present in target dictionary (duplicate key)
                    if (newFilterObj.replacements.ContainsKey(kv.Key))
                    {
                        ConsoleHelper.LogWarn($"Skipped duplicate replacement\'s key \'{kv.Key}\' from \'{fileName}\'.");
                        continue;
                    }

                    // If the key is already used as a replacement value in the target dictionary, skip
                    if (newFilterObj.replacements.ContainsValue(kv.Key))
                    {
                        ConsoleHelper.LogWarn($"Skipped mapping \'{kv.Key}\' => \'{kv.Value}\' from \'{fileName}\' because \'{kv.Key}\' already exists as a replacement value.");
                        continue;
                    }

                    newFilterObj.replacements.Add(kv.Key, kv.Value);
                }

                foreach (var ex in filterObj.exclusions)
                {
                    if (newFilterObj.replacements.ContainsKey(ex) || newFilterObj.replacements.ContainsValue(ex))
                    {
                        ConsoleHelper.LogWarn($"Skipped exclusion (\'{ex}\') also appearing as a replacement in key or value. May be the JSON: \'{fileName}\'.");
                        continue;
                    }
                    newFilterObj.exclusions.Add(ex);
                }
            }
            catch (Exception e)
            {
                ConsoleHelper.LogError($"Failed to read the json file \'{fileName}\' due to corruption or invalid structure.");
                ConsoleHelper.LogError(e.ToString());
                configFile.jsonFilterPaths.RemoveAt(i--);
            }
        }
        if (log)
            ConsoleHelper.LogSuccess($"Successfully mapped {filterKeyPairs.Count} filters to all the required field types!");
    }
}