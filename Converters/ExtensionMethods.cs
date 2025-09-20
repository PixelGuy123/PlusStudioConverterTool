using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    public static string[] GetEditorDefaultTools(string editorMode) => editorMode switch
    {
        "full" => ["room_hall", "room_class", "room_faculty", "room_office", "light_fluorescent", "door_swinging", "door_standard", "merge", "delete"],
        "compliant" => ["room_hall", "room_class", "room_faculty", "room_office", "light_fluorescent", "door_swinging", "door_standard", "merge", "delete"],
        "room" => ["room_class", "room_faculty", "room_office", "technical_potentialdoor", "technical_lightspot", "marker_eventunsafe", "itemspawn_100", "merge", "delete"],
        _ => [string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty]
    };
    static MarkerLocation? TryConvertToMarker(this BasicObjectInfo obj, EditorLevelData levelData) => obj.prefab switch
    {
        "matchballoon" => new MatchBalloonMarker() { position = obj.position.ToUnity(), type = obj.prefab },
        _ => null,
    };

    static bool UpdateOldAssetName(ref string assetName, LevelFieldType lvlFieldType)
    {
        if (ConfigurationHandler.filterKeyPairs.TryGetValue(lvlFieldType, out var filterObj))
        {
            if (filterObj.exclusions.Contains(assetName))
            {
                ConsoleHelper.LogWarn($"{lvlFieldType}: Removed an asset named '{assetName}'.");
                return false;
            }
            if (filterObj.replacements.TryGetValue(assetName, out var newAssetName))
            {
                ConsoleHelper.LogWarn($"{lvlFieldType}: Renamed an asset named '{assetName}' to '{newAssetName}'.");
                assetName = newAssetName;
            }
            return true;
        }
        return true;
    }

    static List<PlusLevelFormat.PlusDirection> DirsFromTile(this PlusLevelFormat.Tile t, bool checkForWall)
    {
        List<PlusLevelFormat.PlusDirection> list = [];
        for (int i = 1; i <= 4; i++)
            if (IsBitSet(t.walls, i) == checkForWall)
                list.Add((PlusLevelFormat.PlusDirection)i);

        return list;
    }

    static List<PlusStudioLevelFormat.PlusDirection> DirsFromTile(this PlusStudioLevelFormat.Cell t, bool checkForWall)
    {
        List<PlusStudioLevelFormat.PlusDirection> list = [];
        for (int i = 1; i <= 4; i++)
            if (IsBitSet(t.walls, i) == checkForWall)
                list.Add((PlusStudioLevelFormat.PlusDirection)i);

        return list;
    }

    static PlusCellCoverage CoverageFromTile(this PlusLevelFormat.Tile t)
    {

        PlusCellCoverage coverage = PlusCellCoverage.None;

        for (int i = 1; i <= 4; i++)
        {
            if (IsBitSet(t.walls, i)) // If bit is wall
            {
                coverage |= (PlusLevelFormat.PlusDirection)i switch // Get coverage equivalent of the plus direction
                {
                    PlusLevelFormat.PlusDirection.North => PlusCellCoverage.North,
                    PlusLevelFormat.PlusDirection.East => PlusCellCoverage.East,
                    PlusLevelFormat.PlusDirection.West => PlusCellCoverage.West,
                    PlusLevelFormat.PlusDirection.South => PlusCellCoverage.South,
                    _ => PlusCellCoverage.None
                };
            }
        }
        return coverage;
    }

    static (int, int) ToByteVector2(this PlusLevelFormat.PlusDirection dir) => dir switch
    {
        PlusLevelFormat.PlusDirection.North => (0, 1),
        PlusLevelFormat.PlusDirection.West => (-1, 0),
        PlusLevelFormat.PlusDirection.East => (1, 0),
        PlusLevelFormat.PlusDirection.South => (0, -1),
        _ => new(0, 0)
    };

    static (int, int) ToByteVector2(this PlusStudioLevelFormat.PlusDirection dir) => dir switch
    {
        PlusStudioLevelFormat.PlusDirection.North => (0, 1),
        PlusStudioLevelFormat.PlusDirection.West => (-1, 0),
        PlusStudioLevelFormat.PlusDirection.East => (1, 0),
        PlusStudioLevelFormat.PlusDirection.South => (0, -1),
        _ => new(0, 0)
    };

    static IntVector2 ToNETIntVector2(this Direction dir) => dir switch
    {
        Direction.North => new(0, 1),
        Direction.West => new(-1, 0),
        Direction.East => new(1, 0),
        Direction.South => new(0, -1),
        _ => new(0, 0)
    };

    static Direction GetNETOpposite(this Direction dir) => dir switch
    {
        Direction.North => Direction.South,
        Direction.West => Direction.East,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        _ => Direction.Null
    };

    static PlusLevelFormat.PlusDirection GetOpposite(this PlusLevelFormat.PlusDirection dir) => dir switch
    {
        PlusLevelFormat.PlusDirection.North => PlusLevelFormat.PlusDirection.South,
        PlusLevelFormat.PlusDirection.West => PlusLevelFormat.PlusDirection.East,
        PlusLevelFormat.PlusDirection.East => PlusLevelFormat.PlusDirection.West,
        PlusLevelFormat.PlusDirection.South => PlusLevelFormat.PlusDirection.North,
        _ => PlusLevelFormat.PlusDirection.Null
    };

    private static Direction ToDirection(this IntVector2 vec)
    {
        vec = new(Math.Sign(vec.x), Math.Sign(vec.z)); // Clamp to [-1, 1]
        if (vec.x == 0 && vec.z == 1) return Direction.North; // (0, 1)
        if (vec.x == 1 && vec.z == 0) return Direction.East; // (1, 0)
        if (vec.x == 0 && vec.z == -1) return Direction.South; // (0, -1)
        if (vec.x == -1 && vec.z == 0) return Direction.West; // (-1, 0)
        return Direction.Null;
    }

    static bool IsValid(this PlusLevelFormat.Tile t, int expectedId = -1) =>
        t.type != 16 && (expectedId == -1 || t.roomId == expectedId);

    static bool InBounds<T>(this T[,] vals, int x, int y) =>
        x >= 0 && y >= 0 && x < vals.GetLength(0) && y < vals.GetLength(1);

    static int ToggleBit(int flag, int position)
    {
        // Use XOR to flip the bit at the specified position
        return flag ^ (1 << position);
    }

    static bool IsBitSet(int flag, int position)
    {
        // Check if the bit at the specified position is set (1)
        return (flag & (1 << position)) != 0;
    }
}