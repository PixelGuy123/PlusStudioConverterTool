using PlusLevelStudio;
using PlusStudioConverterTool.Extensions;
using SixLabors.ImageSharp;

namespace PlusStudioConverterTool.Services
{
    internal static class ExtractorService
    {
        public static bool ExtractFiles(List<string> files, string exportFolder, bool logActions = true)
        {
            string extension = ".pbpl";

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

        public static List<(string, string)> FullPBPLExtraction(PlayableEditorLevel level, bool logActions, byte[]? thumbnailData, string exportPath)
        {
            List<(string, string)> exportedAssets = [];
            int counter = 0;
            // Include the thumbnail data
            Image img;
            if (thumbnailData != null && thumbnailData.Length != 0)
            {
                img = Image.Load(thumbnailData);
                string ext = img.Configuration.ImageFormats.First().Name;
                string fileName = $"thumbnail.{ext.ToLowerInvariant()}";
                img.Save(Path.Combine(exportPath, fileName));
                if (logActions)
                    ConsoleHelper.LogConverterInfo($"Exported the {fileName} texture into the folder.");
                counter++;
            }

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
                    img = Image.Load(entry.data);
                    string ext = img.Configuration.ImageFormats.First().Name;
                    string fileName = $"{entry.id}.{ext.ToLowerInvariant()}";
                    img.Save(Path.Combine(exportPath, fileName));
                    if (logActions)
                        ConsoleHelper.LogConverterInfo($"Exported the {fileName} texture into the folder.");
                    exportedAssets.Add((entry.contentType, fileName));
                    counter++;
                }
            }

            if (logActions)
                ConsoleHelper.LogSuccess($"Successfully extracted {counter} assets into {exportPath}");
            return exportedAssets;
        }

        static void ScanPBPL(string file, string exportFolder, bool logActions)
        {
            if (logActions)
                ConsoleHelper.LogInfo("Reading PBPL level...");
            PlayableEditorLevel level;
            byte[]? thumbnailData;
            using (var reader = new BinaryReader(File.OpenRead(file)))
            {
                level = reader.ReadPlayableLevelWithoutThumbnail(out thumbnailData);
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

            FullPBPLExtraction(level, logActions, thumbnailData, targetDir);
        }
    }
}
