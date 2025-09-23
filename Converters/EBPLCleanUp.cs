using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Extensions;
using PlusStudioConverterTool.Models;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    public static void PerformFiltering(this EditorLevelData newData)
    {
        // Rooms
        for (int i = 0; i < newData.rooms.Count; i++)
        {
            var room = newData.rooms[i];
            string roomType = room.roomType; // Failsafe with variable bc room removal requires its name
            // Room category type
            if (!UpdateOldAssetName(ref roomType, LevelFieldType.RoomCategory))
            {
                // Removed by exclusion
                newData.RemoveEditorRoomWithoutUnity(room);
                i--;
                continue;
            }
            room.roomType = roomType;

            // Textures stored
            {
                UpdateOldAssetName(ref room.textureContainer.floor, LevelFieldType.RoomTexture);
                UpdateOldAssetName(ref room.textureContainer.wall, LevelFieldType.RoomTexture);
                UpdateOldAssetName(ref room.textureContainer.ceiling, LevelFieldType.RoomTexture);
            }

            // Activity
            if (room.activity != null && !UpdateOldAssetName(ref room.activity.type, LevelFieldType.Activity))
            {
                room.activity = null; // Removes activity
            }
        }

        // Structures
        for (int i = 0; i < newData.structures.Count; i++)
            if (!UpdateOldAssetName(ref newData.structures[i].type, LevelFieldType.Structure, ignoreRenaming: true)) // Do not rename, only allow an exclusion filter
                newData.structures.RemoveAt(i--);

        // ** Use ValidadePosition since rooms can be deleted, and their remnants should go as well
        // Objects
        for (int i = 0; i < newData.objects.Count; i++)
            if (!newData.ValidatePosition(newData.objects[i].position) || !UpdateOldAssetName(ref newData.objects[i].prefab, LevelFieldType.Object))
                newData.objects.RemoveAt(i--);

        // Items
        for (int i = 0; i < newData.items.Count; i++)
            if (!newData.ValidatePosition(newData.items[i].position) || !UpdateOldAssetName(ref newData.items[i].item, LevelFieldType.Item))
                newData.items.RemoveAt(i--);

        // Doors
        for (int i = 0; i < newData.doors.Count; i++)
            if (!newData.ValidatePosition(newData.doors[i].position) || !UpdateOldAssetName(ref newData.doors[i].type, LevelFieldType.Door))
                newData.doors.RemoveAt(i--);

        // Windows
        for (int i = 0; i < newData.windows.Count; i++)
            if (!newData.ValidatePosition(newData.windows[i].position) || !UpdateOldAssetName(ref newData.windows[i].type, LevelFieldType.Window))
                newData.windows.RemoveAt(i--);

        // Exits
        for (int i = 0; i < newData.exits.Count; i++)
            if (!UpdateOldAssetName(ref newData.exits[i].type, LevelFieldType.Exit))
                newData.exits.RemoveAt(i--);

        // NPCs
        for (int i = 0; i < newData.npcs.Count; i++)
            if (!newData.ValidatePosition(newData.npcs[i].position) || !UpdateOldAssetName(ref newData.npcs[i].npc, LevelFieldType.NPC)) // Checks if room is null, that's enough
                newData.npcs.RemoveAt(i--);

        // Posters
        for (int i = 0; i < newData.posters.Count; i++)
            if (!newData.ValidatePosition(newData.posters[i].position) || !UpdateOldAssetName(ref newData.posters[i].type, LevelFieldType.Poster))
                newData.posters.RemoveAt(i--);

        // Lights
        for (int i = 0; i < newData.lights.Count; i++)
            if (!newData.ValidatePosition(newData.lights[i].position) || !UpdateOldAssetName(ref newData.lights[i].type, LevelFieldType.Light))
                newData.lights.RemoveAt(i--);

        // Light group cleanup
        for (int i = 0; i < newData.lightGroups.Count; i++) // They basically work by indexing, if no light is referring to its index, that light group becomes obsolete
        {
            if (!newData.lights.Exists(light => light.lightGroup == i))
                newData.lightGroups.RemoveAt(i--);
        }

        // Random events (string list)
        for (int i = 0; i < newData.randomEvents.Count; i++)
        {
            var ev = newData.randomEvents[i];
            if (!UpdateOldAssetName(ref ev, LevelFieldType.RandomEvent))
            {
                newData.randomEvents.RemoveAt(i--);
                continue;
            }
            newData.randomEvents[i] = ev;
        }

        // // Logging for .Select actions from EditorLevelData.Write
        // // Lights
        // foreach (var x in newData.lights)
        // {
        //     Console.WriteLine($"Light type: '{x.type ?? "<null>"}'");
        // }
        // // Doors
        // foreach (var x in newData.doors)
        // {
        //     Console.WriteLine($"Door type: '{x.type ?? "<null>"}'");
        // }
        // // Exits
        // foreach (var x in newData.exits)
        // {
        //     Console.WriteLine($"Exit type: '{x.type ?? "<null>"}'");
        // }
        // // Windows
        // foreach (var x in newData.windows)
        // {
        //     Console.WriteLine($"Window type: '{x.type ?? "<null>"}'");
        // }
        // // Items
        // foreach (var x in newData.items)
        // {
        //     Console.WriteLine($"Item: '{x.item ?? "<null>"}'");
        // }
        // // Objects
        // foreach (var x in newData.objects)
        // {
        //     Console.WriteLine($"Object prefab: '{x.prefab ?? "<null>"}'");
        // }
        // // NPCs
        // foreach (var x in newData.npcs)
        // {
        //     Console.WriteLine($"NPC: '{x.npc ?? "<null>"}'");
        // }
        // // Posters
        // foreach (var x in newData.posters)
        // {
        //     Console.WriteLine($"Poster type: '{x.type ?? "<null>"}'");
        // }
        // // Rooms
        // foreach (var x in newData.rooms)
        // {
        //     Console.WriteLine($"Room type: '{x.roomType ?? "<null>"}'");
        //     if (x.textureContainer != null)
        //     {
        //         Console.WriteLine($"Room floor texture: '{x.textureContainer.floor ?? "<null>"}'");
        //         Console.WriteLine($"Room wall texture: '{x.textureContainer.wall ?? "<null>"}'");
        //         Console.WriteLine($"Room ceiling texture: '{x.textureContainer.ceiling ?? "<null>"}'");
        //     }
        //     if (x.activity != null)
        //     {
        //         Console.WriteLine($"Room activity type: '{x.activity.type ?? "<null>"}'");
        //     }
        // }
        // // Structures
        // foreach (var x in newData.structures)
        // {
        //     Console.WriteLine($"Structure type: '{x.type ?? "<null>"}'");
        // }
        // // Markers
        // foreach (var x in newData.markers)
        // {
        //     Console.WriteLine($"Marker type: '{x.type ?? "<null>"}'");
        // }
    }
}