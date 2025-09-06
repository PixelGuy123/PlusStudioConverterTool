using PlusLevelStudio;
using PlusStudioConverterTool.Extensions;
using SixLabors.ImageSharp;

namespace PlusStudioConverterTool.Services
{
    internal static class ExtractorService
    {
        public static bool ExtractFiles(List<string> files, string exportFolder, bool isPBPL, bool logActions = true)
        {
            string extension = isPBPL ? ".pbpl" : ".ebpl";

            foreach (var file in files)
            {
                if (logActions)
                    ConsoleHelper.LogInfo($"== Loading file: {Path.GetFileName(file)} ==\n");

                if (!File.Exists(file) || Path.GetExtension(file)?.Equals(extension, StringComparison.OrdinalIgnoreCase) != true)
                {
                    if (logActions)
                        ConsoleHelper.LogError($"Invalid file detected ({file}). Skipping.");
                    continue;
                }

                try
                {
                    if (isPBPL)
                        ScanPBPL(file, exportFolder, logActions);
                }
                catch (Exception ex)
                {
                    if (logActions)
                    {
                        ConsoleHelper.LogError($"Failed to extract file ({file}).");
                        ConsoleHelper.LogError(ex.ToString());
                    }
                    return false;
                }
            }
            if (logActions)
            {
                Console.WriteLine("============");
                ConsoleHelper.LogSuccess($"{files.Count} files were successfully extracted!");
            }
            return true;
        }

        static void ScanPBPL(string file, string exportFolder, bool logActions)
        {
            if (logActions)
                ConsoleHelper.LogInfo("Reading PBPL level...");
            PlayableEditorLevel level;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = reader.ReadPlayableLevelWithoutThumbnail(out var thumbnailData);
            }
            if (level.meta.contentPackage == null)
            {
                if (logActions)
                    ConsoleHelper.LogWarn($"{Path.GetFileName(file)} skipped because there\'s no meta data to be extracted!");
                return;
            }
            string targetDir = Path.Combine(exportFolder, Path.GetFileNameWithoutExtension(file) + "_Assets");
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            int counter = 0;
            foreach (var entry in level.meta.contentPackage.entries)
            {
                if (entry.usingFilePath || entry.data == null) // IF there's absolutely no data, don't try to extract
                {
                    if (logActions)
                        ConsoleHelper.LogWarn($"Skipped entry (\'{entry.id}\') due to using a file path as reference.");
                    continue;
                }

                if (entry.contentType == "texture" || entry.contentType == "imageposter")
                {
                    var img = Image.Load(entry.data);
                    string ext = img.Configuration.ImageFormats.First().Name;
                    string fileName = $"{entry.id}.{ext.ToLowerInvariant()}";
                    img.Save(Path.Combine(targetDir, fileName));
                    if (logActions)
                        ConsoleHelper.LogInfo($"Exported the {fileName} texture into the folder.");
                    counter++;
                }

            }
            if (logActions)
                ConsoleHelper.LogSuccess($"Successfully extracted {counter} assets into {targetDir}");
        }
    }
}
