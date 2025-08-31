using System.Diagnostics;
using BaldiLevelEditor;
using PlusLevelFormat;

namespace CBLDtoBLD.Services
{
    internal static class ConverterService
    {
        // Convert a list of absolute file paths (.cbld) and optionally export all
        // converted files into exportFolder. If exportFolder is null the converted
        // file is written next to the original file.
        public static void ConvertFiles(List<string> cbldFiles, string? exportFolder, TargetType fileType)
        {
            string extension = fileType.ToExtension();
            // Get the specific method and settings depending on the options
            ConversionMethod action;
            ConversionSettings? settings = null;
            switch (fileType)
            {
                case TargetType.CBLD:
                    action = ConvertCBLDtoBLDFiles;
                    break;
                case TargetType.BLD:
                    settings = new BLDtoEBPLSettings(
                        ConsoleHelper.CheckIfUserInputsYOrN("Should the converter automatically include lighting into the level(s)?")
                    );
                    action = ConvertBLDtoEBPLFiles;
                    break;
                default:
                    throw new ArgumentException("Invalid TargetType");
            }

            foreach (var file in cbldFiles)
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
                }
            }
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
                bldSettings.AutoLightFill
                );
            // Comes later to prevent creating an empty file
            using var writer = new BinaryWriter(File.OpenWrite(fname));
            conversion.Write(writer);

            ConsoleHelper.LogSuccess($"EBPL file converted to {Path.GetFileName(fname)}");
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
        record BLDtoEBPLSettings(bool AutoLightFill) : ConversionSettings { }
        delegate void ConversionMethod(string file, string? exportFolder, out string fname, ConversionSettings? settings);
    }
}
