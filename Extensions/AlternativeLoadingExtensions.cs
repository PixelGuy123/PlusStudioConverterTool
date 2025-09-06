using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.ModeSettings;
using PlusLevelStudio.Lua;
using PlusStudioLevelFormat;

namespace PlusStudioConverterTool.Extensions;

internal static class AltLevelLoaderExtensions
{
    public static PlayableEditorLevel ReadPlayableLevelWithoutThumbnail(this BinaryReader reader, out byte[]? thumbnailData)
    {
        PlayableEditorLevel playableEditorLevel = new();
        thumbnailData = null;
        int num;
        if (reader.ReadByte() >= 1)
        {
            num = reader.ReadInt32();
            if (num > 0)
                thumbnailData = reader.ReadBytes(num); // Discard whatever is done here
        }

        // ** Manually load PlayableLevelMeta to prevent using StudioPlugin
        PlayableLevelMeta playableLevelMeta = new();
        byte byNum = reader.ReadByte();
        playableLevelMeta.name = reader.ReadString();
        if (byNum >= 1)
        {
            playableLevelMeta.author = reader.ReadString();
        }

        playableLevelMeta.gameMode = reader.ReadString();
        bool readIntoBoolean = reader.ReadBoolean();
        if (gameModeAliases.ContainsKey(playableLevelMeta.gameMode))
        {
            if (!readIntoBoolean)
            {
                playableLevelMeta.modeSettings = gameModeAliases[playableLevelMeta.gameMode].CreateSettings();
            }
            else
            {
                playableLevelMeta.modeSettings = gameModeAliases[playableLevelMeta.gameMode].CreateSettings();
                playableLevelMeta.modeSettings.ReadInto(reader);
            }
        }

        if (byNum < 2)
        {
            playableLevelMeta.contentPackage = new EditorCustomContentPackage(false);
        }
        else
        {
            playableLevelMeta.contentPackage = EditorCustomContentPackage.Read(reader);
        }

        playableEditorLevel.meta = playableLevelMeta;
        playableEditorLevel.data = BaldiLevel.Read(reader);
        return playableEditorLevel;
    }

    internal static void InitializeSettings()
    {
        gameModeAliases.Add("standard", new MainGameMode());
        gameModeAliases.Add("stealthy", new StealthyGameMode());
        gameModeAliases.Add("custom", new CustomChallengeGameMode());
    }

    readonly static Dictionary<string, EditorGameMode> gameModeAliases = [];
}