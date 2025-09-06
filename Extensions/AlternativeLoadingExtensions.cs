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

        playableEditorLevel.meta = reader.ReadPlayableLevelMetaWithoutEditor(false);
        playableEditorLevel.data = BaldiLevel.Read(reader);
        return playableEditorLevel;
    }

    public static PlayableLevelMeta ReadPlayableLevelMetaWithoutEditor(this BinaryReader reader, bool useOldFilePaths)
    {
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
            playableLevelMeta.contentPackage = new EditorCustomContentPackage(useOldFilePaths);
        }
        else
        {
            playableLevelMeta.contentPackage = EditorCustomContentPackage.Read(reader);
        }
        return playableLevelMeta;
    }

    internal static void InitializeSettings()
    {
        gameModeAliases.Add("standard", new MainGameMode());
        gameModeAliases.Add("stealthy", new StealthyGameMode());
        gameModeAliases.Add("custom", new CustomChallengeGameMode());
    }

    readonly static Dictionary<string, EditorGameMode> gameModeAliases = [];
    readonly static Dictionary<string, Type> structureTypes = new()
    {
        { "facultyonlydoor", typeof(HallDoorStructureLocation) },
        { "lockdowndoor", typeof(LockdownDoorWithButtonsStructureLocation) },
        { "shapelock", typeof(ShapeLockStructureLocation) },
        { "conveyorbelt", typeof(ConveyorBeltStructureLocation) },
        { "vent", typeof(VentStructureLocation) },
        { "powerlever", typeof(PowerLeverStructureLocation) },
        { "steamvalves", typeof(SteamValveStructureLocation) },
    };
    readonly static Dictionary<string, Type> markerTypes = new()
    {
        { "matchballoon", typeof(MatchBalloonMarker) }
    };
    readonly static Dictionary<Direction, IntVector2> vectors = new()
    {
        { Direction.North, new(0, 1) },
        { Direction.East, new(1, 0) },
        { Direction.South, new(0, -1) },
        { Direction.West, new(-1, 0) }
    };
    readonly static Dictionary<Direction, int> bitPosition = new()
    {
        { Direction.North,  0 },
        {Direction.East, 1 },
        {Direction.South, 2},
        {Direction.West, 3}
    };

}