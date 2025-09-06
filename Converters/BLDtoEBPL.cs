using BaldiLevelEditor;
using PlusLevelLoader;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;
using UnityEngine;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    #region BLDTOEBPL
    public static EditorFileContainer ConvertBLDtoEBPLFormat(this EditorLevel level, bool automaticallyIncludeLighting, string editorMode)
    {
        ConsoleHelper.LogInfo("Converting BLD to EBPL level...");
        ConsoleHelper.LogConverterInfo("Initializing EditorFileContainer...");
        var fileContainer = new EditorFileContainer();
        // 1. Initialize the new EditorLevelData with the correct map size.
        // The constructor will set up initial defaults like one light group.
        var newData = new EditorLevelData(new IntVector2(level.width, level.height))
        {
            // 2. Set metadata and global properties with default values.
            // The source EditorLevel does not contain this information directly.
            elevatorTitle = "WIP",
            meta = new PlayableLevelMeta()
            {
                name = "BLDtoEBPL_Level",
                author = "PSCT Converted",
                gameMode = "standard",
                contentPackage = new EditorCustomContentPackage(true) // Old versions used file paths
            }
        };
        // Other defaults like skybox, time limit, etc., are handled by the EditorLevelData constructor.

        // 3. Convert rooms. The order must be preserved to maintain correct Room IDs.
        ConsoleHelper.LogConverterInfo("Converting rooms from BLD file...");
        newData.rooms.Clear();
        foreach (var oldRoom in level.rooms)
        {
            string renamedFloor = oldRoom.textures.floor;
            string renamedWall = oldRoom.textures.wall;
            string renamedCeiling = oldRoom.textures.ceiling;
            UpdateOldAssetName(ref renamedFloor, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref renamedWall, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref renamedCeiling, LevelFieldType.RoomTexture);

            var newRoom = new EditorRoom(oldRoom.type, new PlusStudioLevelFormat.TextureContainer(
                renamedFloor,
                renamedWall,
                renamedCeiling
                ));
            newRoom.activity = oldRoom.activity == null ? null : new()
            {
                direction = oldRoom.activity.direction.ToStandard(),
                position = oldRoom.activity.position.ToUnity(),
                type = oldRoom.activity.activity,
                myRoom = newRoom
            };
            newData.rooms.Add(newRoom);
        }
        ConsoleHelper.LogConverterInfo($"{newData.rooms.Count} rooms loaded in total!");

        // 4. Convert areas.
        ConsoleHelper.LogConverterInfo("Converting BLD to EBPL areas...");
        newData.areas.Clear();
        List<RectCellArea> newCellAreas = new List<RectCellArea>();
        foreach (var oldArea in level.areas)
        {
            if (oldArea is AreaData areaData)
            {
                var cellArea = new RectCellArea(
                    new IntVector2(areaData.origin.x, areaData.origin.y),
                    new IntVector2(areaData.size.x, areaData.size.y),
                    areaData.roomId);

                newCellAreas.Add(cellArea);
                newData.areas.Add(cellArea);
            }
        }

        ConsoleHelper.LogConverterInfo($"{newData.areas.Count} areas loaded in total!");

        // 5. Convert all placements (doors, windows, objects, etc.).

        // Doors
        ConsoleHelper.LogConverterInfo("Converting doors...");
        foreach (var oldDoor in level.doors)
        {
            string renamedDoor = oldDoor.type;
            if (UpdateOldAssetName(ref renamedDoor, LevelFieldType.Door))
            {
                newData.doors.Add(new PlusLevelStudio.Editor.DoorLocation()
                {
                    type = renamedDoor,
                    position = new IntVector2(oldDoor.position.x, oldDoor.position.y),
                    direction = (Direction)oldDoor.direction
                });
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.doors.Count} doors loaded in total!");


        // Windows
        ConsoleHelper.LogConverterInfo("Converting windows...");
        foreach (var oldWindow in level.windows)
        {
            string renamedWindow = oldWindow.type;
            if (UpdateOldAssetName(ref renamedWindow, LevelFieldType.Window))
            {
                newData.windows.Add(new PlusLevelStudio.Editor.WindowLocation()
                {
                    type = renamedWindow,
                    position = new IntVector2(oldWindow.position.x, oldWindow.position.y),
                    direction = (Direction)oldWindow.direction
                });
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.windows.Count} windows loaded in total!");


        // Manual Walls
        ConsoleHelper.LogConverterInfo("Converting manually placed walls...");
        foreach (var oldWall in level.manualWalls)
        {
            newData.walls.Add(new WallLocation()
            {
                wallState = oldWall.wall,
                position = new IntVector2(oldWall.position.x, oldWall.position.y),
                direction = (Direction)oldWall.direction
            });
        }
        ConsoleHelper.LogConverterInfo($"{newData.walls.Count} walls loaded in total!");


        // Prefabs -> Basic Objects
        ConsoleHelper.LogConverterInfo("Converting world objects...");
        foreach (var oldPrefab in level.prefabs)
        {
            string renamedPrefab = oldPrefab.prefab;
            if (UpdateOldAssetName(ref renamedPrefab, LevelFieldType.Object))
            {
                newData.objects.Add(new BasicObjectLocation()
                {
                    prefab = renamedPrefab,
                    position = new Vector3(oldPrefab.position.x, oldPrefab.position.y, oldPrefab.position.z),
                    rotation = new Quaternion(oldPrefab.rotation.x, oldPrefab.rotation.y, oldPrefab.rotation.z, oldPrefab.rotation.w)
                });
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.objects.Count} objects loaded in total!");


        // Items
        ConsoleHelper.LogConverterInfo("Converting items...");
        foreach (var oldItem in level.items)
        {
            string renamedItem = oldItem.item;
            if (UpdateOldAssetName(ref renamedItem, LevelFieldType.Item))
            {
                newData.items.Add(new ItemPlacement()
                {
                    item = renamedItem,
                    position = new Vector2(oldItem.position.x, oldItem.position.z)
                });
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.items.Count} items loaded in total!");


        // Exits and determine spawn point
        ConsoleHelper.LogConverterInfo("Converting exits...");
        PlusLevelStudio.Editor.ExitLocation? spawnExit = null;
        foreach (var oldExit in level.exits)
        {
            string renamedExit = oldExit.type;
            if (UpdateOldAssetName(ref renamedExit, LevelFieldType.Exit))
            {
                var newExit = new PlusLevelStudio.Editor.ExitLocation()
                {
                    type = renamedExit,
                    position = new IntVector2(oldExit.position.x, oldExit.position.y),
                    direction = (Direction)oldExit.direction,
                    isSpawn = oldExit.isSpawn
                };
                newData.exits.Add(newExit);
                if (newExit.isSpawn)
                {
                    spawnExit = newExit; // Track the last found spawn exit
                }
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.exits.Count} exits loaded in total!");


        // Set the level's spawn point based on the 'isSpawn' flag from an exit.
        if (spawnExit != null)
        {
            newData.spawnPoint = new Vector3(spawnExit.position.x * 10f + 5f, 5f, spawnExit.position.z * 10f + 5f);
            newData.spawnDirection = spawnExit.direction;
        }
        else
        {
            // If no spawn exit is defined, use a default value.
            newData.spawnPoint = new Vector3(5f, 5f, 5f);
            newData.spawnDirection = Direction.North;
        }
        ConsoleHelper.LogConverterInfo($"Spawn point set to {newData.spawnPoint} facing {newData.spawnDirection}");

        // NPC Spawns
        ConsoleHelper.LogConverterInfo("Converting NPCs...");
        foreach (var oldNpc in level.npcSpawns)
        {
            string renamedNpc = oldNpc.type;
            if (UpdateOldAssetName(ref renamedNpc, LevelFieldType.NPC))
            {
                newData.npcs.Add(new NPCPlacement()
                {
                    npc = renamedNpc,
                    position = new IntVector2(oldNpc.position.x, oldNpc.position.y),
                });
            }
        }

        fileContainer.data = newData;

        // EditorFileMeta setup
        ConsoleHelper.LogConverterInfo("Adding EditorFileMeta...");
        string[] toolBars = new string[9];
        for (int i = 0; i < toolBars.Length; i++)
            toolBars[i] = string.Empty;

        fileContainer.meta = new()
        {
            cameraPosition = Vector3.zero,
            cameraRotation = Quaternion.identity,
            editorMode = editorMode,
            toolbarTools = toolBars
        };

        // ****** Extra *******
        // Lighting inclusion
        if (automaticallyIncludeLighting)
        {
            ConsoleHelper.LogConverterInfo("Generating artificial lighting...");
            int maxDistance = newData.lightGroups[0].strength - 1;
            int counter = 0;
            for (int x = 0; x < newData.mapSize.x; x++)
            {
                for (int y = 0; y < newData.mapSize.z; y++)
                {
                    IntVector2 pos = new(x, y);
                    ushort id = newData.RoomIdFromPos(pos, true);
                    if (id != 0)
                    {
                        counter++;
                        if (counter % maxDistance == 0)
                        {
                            newData.lights.Add(new()
                            {
                                lightGroup = 0,
                                position = pos,
                                type = newData.RoomFromId(id).textureContainer.ceiling == "None" ? "null" : "fluorescent"
                            });
                        }
                    }
                }
            }
        }
        else
        {
            newData.minLightColor = Color.white;
        }


        return fileContainer;
    }
    #endregion
}