using BaldiLevelEditor;
using PlusLevelFormat;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Converters;
using PlusStudioConverterTool.Extensions;
using PlusStudioConverterTool.Models;
using PlusStudioLevelFormat;

namespace PlusStudioConverterTool.Services
{
    internal static class ConverterService
    {
        // Convert a list of absolute file paths (.cbld) and optionally export all
        // converted files into exportFolder. If exportFolder is null the converted
        // file is written next to the original file.
        public static void ConvertFiles(List<string> files, string? exportFolder, TargetType fileType)
        {
            string extension = fileType.ToExtension();
            // Get the specific method and settings depending on the options
            ConversionMethod action;
            ConversionSettings? settings = null;
            switch (fileType)
            {
                case TargetType.CBLDtoBLD:
                    action = ConvertCBLDtoBLDFiles;
                    break;
                case TargetType.BLDtoEBPL:
                    settings = new BLDtoEBPLSettings(
                        ConsoleHelper.CheckIfUserInputsYOrN("Should the converter automatically include procedually generated lighting into the level(s)?"),
                        ConsoleHelper.RetrieveUserSelection("Which editor mode should the converted level(s) use?", "Full", "Compliant").Item2.ToLower()
                    );
                    action = ConvertBLDtoEBPLFiles;
                    break;
                case TargetType.CBLDtoRBPL:
                    settings = new CBLDtoRBPLSettings(
                            ConsoleHelper.CheckIfUserInputsYOrN("Should the converter automatically add potential door spots for hallway-only rooms?")
                        );
                    action = ConvertCBLDtoRBPLFiles;
                    break;
                case TargetType.RBPLtoEBPL:
                    action = ConvertRBPLtoEBPLFiles;
                    break;
                case TargetType.PBPLtoEBPL:
                    settings = new EditorSettings(
                        ConsoleHelper.RetrieveUserSelection("Which editor mode should the converted level(s) use?", "Full", "Compliant").Item2.ToLower()
                    );
                    action = ConvertPBPLoEBPLFiles;
                    break;
                case TargetType.BPLtoEBPL:
                    settings = new EditorSettings(
                        ConsoleHelper.RetrieveUserSelection("Which editor mode should the converted level(s) use?", "Full", "Compliant").Item2.ToLower()
                    );
                    action = ConvertBPLoEBPLFiles;
                    break;
                default:
                    throw new ArgumentException("Invalid TargetType");
            }

            foreach (var file in files)
            {
                ConsoleHelper.LogInfo($"== Loading file: {Path.GetFileName(file)} ==\n");

                if (!File.Exists(file) || Path.GetExtension(file)?.Equals(extension, StringComparison.OrdinalIgnoreCase) != true)
                {
                    ConsoleHelper.LogError($"Invalid file detected ({file}). Skipping.");
                    continue;
                }

                try
                {
                    action(file, exportFolder, out string fname, settings);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.LogError($"Failed to convert file ({file}).");
                    ConsoleHelper.LogError(ex.ToString());
                    return;
                }
            }

            Console.WriteLine("============");
            ConsoleHelper.LogSuccess($"{files.Count} files were successfully converted!");
        }

        // Expects EBPL files
        public static void CleanUpEBPLFiles(List<string> files, string? exportFolder)
        {
            const string extension = ".ebpl";
            foreach (var file in files)
            {
                ConsoleHelper.LogInfo($"== Loading file: {Path.GetFileName(file)} ==\n");

                if (!File.Exists(file) || Path.GetExtension(file)?.Equals(extension, StringComparison.OrdinalIgnoreCase) != true)
                {
                    ConsoleHelper.LogError($"Invalid file detected ({file}). Skipping.");
                    continue;
                }

                try
                {
                    // Does the filtering
                    ConsoleHelper.LogInfo("Reading EBPL level...");
                    EditorFileContainer level;
                    using (var reader = new BinaryReader(File.OpenRead(file)))
                    {
                        level = reader.ReadMindfulSafe();
                    }
                    var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

                    if (string.IsNullOrEmpty(targetDir))
                        throw new DirectoryNotFoundException("Could not determine target directory for output file.");

                    string fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + "_filtered.ebpl");

                    // Ensure we don't overwrite an existing file: pick a unique filename
                    fname = GetUniqueFilePath(fname);

                    level.data.PerformFiltering();

                    // Comes later to prevent creating an empty file
                    using var writer = new BinaryWriter(File.OpenWrite(fname));
                    level.Write(writer);

                    ConsoleHelper.LogSuccess($"EBPL file filtered and stored in {Path.GetFileName(fname)}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.LogError($"Failed to filter file ({file}).");
                    ConsoleHelper.LogError(ex.ToString());
                    return;
                }
            }

            Console.WriteLine("============");
            ConsoleHelper.LogSuccess($"{files.Count} files were successfully filtered!");
        }

        private static void ConvertCBLDtoBLDFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ConsoleHelper.LogInfo("Reading CBLD level...");
            Level level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = LevelExtensions.ReadLevel(reader);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + ".bld");

            // Ensure we don't overwrite an existing file: pick a unique filename
            fname = GetUniqueFilePath(fname);

            var conversion = level.ConvertCBLDtoBLDFormat();

            // Comes later to prevent creating an empty file
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.SaveIntoStream(writer);

            ConsoleHelper.LogSuccess($"CBLD file converted to {Path.GetFileName(fname)}");
        }

        private static void ConvertBLDtoEBPLFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings is not BLDtoEBPLSettings bldSettings)
                throw new ArgumentException($"ConversionSettings is not of BLDtoEBPLSettings type.");

            ConsoleHelper.LogInfo("Reading BLD level...");
            EditorLevel level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = EditorLevel.LoadFromStream(reader);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + ".ebpl");

            // Ensure we don't overwrite an existing file: pick a unique filename
            fname = GetUniqueFilePath(fname);


            var conversion = level.ConvertBLDtoEBPLFormat(
                bldSettings.AutoLightFill,
                bldSettings.EditorMode
                );
            // Comes later to prevent creating an empty file
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.Write(writer);

            ConsoleHelper.LogSuccess($"BLD file converted to {Path.GetFileName(fname)}");
        }

        private static void ConvertCBLDtoRBPLFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            if (settings is not CBLDtoRBPLSettings cbldSettings)
                throw new ArgumentException($"ConversionSettings is not of CBLDtoRBPLSettings type.");

            ConsoleHelper.LogInfo("Reading CBLD level...");
            Level level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = LevelExtensions.ReadLevel(reader);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file));
            string originalFileName = fname;

            var rooms = level.ConvertCBLDtoRBPLFormat(cbldSettings.AutoPotentialDoorPlacement);
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                fname = GetUniqueFilePath(originalFileName + $"_{room.type}_{i + 1}.rbpl");
                using var writer = new BinaryWriter(File.OpenWrite(fname));
                room.Write(writer);
                ConsoleHelper.LogInfo($"Created {Path.GetFileName(fname)} with success!");
            }

            ConsoleHelper.LogSuccess($"Successfully converted {rooms.Count} rooms into their respective files!");
        }

        private static void ConvertRBPLtoEBPLFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ConsoleHelper.LogInfo("Reading RBPL level...");
            BaldiRoomAsset level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = BaldiRoomAsset.Read(reader);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + ".ebpl");

            // Ensure we don't overwrite an existing file: pick a unique filename
            fname = GetUniqueFilePath(fname);

            var conversion = level.ConvertRBPLtoEBPLFormat();
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.Write(writer);

            ConsoleHelper.LogSuccess($"RBPL file converted to {Path.GetFileName(fname)}");
        }

        private static void ConvertPBPLoEBPLFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings is not EditorSettings editSettings)
                throw new ArgumentException($"ConversionSettings is not of EditorSettings type.");

            ConsoleHelper.LogInfo("Reading PBPL level...");
            PlayableEditorLevel level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = reader.ReadPlayableLevelWithoutThumbnail(out _);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + ".ebpl");

            // Ensure we don't overwrite an existing file: pick a unique filename
            fname = GetUniqueFilePath(fname);

            var conversion = level.data.ConvertBPLtoEBPLFormat(
                level.meta,
                editSettings.EditorMode
                );

            if (level.meta.contentPackage != null &&
            level.meta.contentPackage.entries.Exists(entry => entry.data != null) // if there's any entry with actual data in it, then it must be exported
            )
            {
                conversion.data.meta.contentPackage = new(true) // Convert the contentPackage to path-only
                {
                    entries = level.meta.contentPackage.entries
                };
                ConsoleHelper.LogInfo("Extracting PBPL assets and reassigning packages inside EBPL instance...");
                var fileEntries = ExtractorService.FullPBPLExtraction(level, true, null, targetDir);
                // Updates every entry to be filepath-only
                var entries = conversion.data.meta.contentPackage.entries;
                if (entries.Count < fileEntries.Count) // If the original entries is lower than what was exported, something is missing and needs to be corrected
                {
                    entries = new(fileEntries.Count); // Make a new count
                    for (int i = 0; i < entries.Count; i++) entries[i] = new(fileEntries[i].Item1, Path.GetFileNameWithoutExtension(fileEntries[i].Item2), fileEntries[i].Item2); // Reinitialize all the entries with the basic data
                }
                else
                {
                    // Now reassign the filePaths
                    for (int i = 0; i < entries.Count; i++)
                    {
                        entries[i].filePath = fileEntries[i].Item2;
                        entries[i].data = null; // Use the file path and clean up data to not use it
                    }
                }
                ConsoleHelper.LogWarn("Reminder: the extracted assets must be moved to their respective folders inside \"Level Studio\\User Content\" in order to export the level properly.");
            }

            // Comes later to prevent creating an empty file
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.Write(writer);

            ConsoleHelper.LogSuccess($"PBPL file converted to {Path.GetFileName(fname)}");
        }

        private static void ConvertBPLoEBPLFiles(string file, string? exportFolder, out string fname, ConversionSettings? settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings is not EditorSettings editSettings)
                throw new ArgumentException($"ConversionSettings is not of EditorSettings type.");

            ConsoleHelper.LogInfo("Reading BPL level...");
            BaldiLevel level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = BaldiLevel.Read(reader);
            }
            var targetDir = string.IsNullOrEmpty(exportFolder) ? Path.GetDirectoryName(file) : exportFolder;

            if (string.IsNullOrEmpty(targetDir))
                throw new DirectoryNotFoundException("Could not determine target directory for output file.");

            fname = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(file) + ".ebpl");

            // Ensure we don't overwrite an existing file: pick a unique filename
            fname = GetUniqueFilePath(fname);


            var conversion = level.ConvertBPLtoEBPLFormat(
                null,
                editSettings.EditorMode
                );
            // Comes later to prevent creating an empty file
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.Write(writer);

            ConsoleHelper.LogSuccess($"BPL file converted to {Path.GetFileName(fname)}");
        }

        // Gets unique file path by doing what Windows does
        // Example: "File.bld" -> "File (2).bld" if "File.bld" already exists
        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
                return path;

            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            var counter = 1;
            string candidate;
            do // So long I haven't used this do syntax
            {
                candidate = Path.Combine(dir, $"{name} ({++counter}){ext}");
            } while (File.Exists(candidate));

            return candidate;
        }

        abstract record ConversionSettings { }
        record EditorSettings(string EditorMode) : ConversionSettings { }
        record CBLDtoRBPLSettings(bool AutoPotentialDoorPlacement) : ConversionSettings { }
        record BLDtoEBPLSettings(bool AutoLightFill, string EditorMode) : EditorSettings(EditorMode) { }
        delegate void ConversionMethod(string file, string? exportFolder, out string fname, ConversionSettings? settings);
    }
}
