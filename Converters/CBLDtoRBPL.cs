using PlusLevelFormat;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;
using PlusStudioLevelFormat;
using UnityEngine;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    #region CBLDTORBPL
    public static List<BaldiRoomAsset> ConvertCBLDtoRBPLFormat(this Level level)
    {
        ConsoleHelper.LogInfo("Converting CBLD rooms to RBPL...");
        var roomAssets = new List<BaldiRoomAsset>();

        ConsoleHelper.LogInfo("Analyzing each room...");
        for (int i = 0; i < level.rooms.Count; i++)
        {
            var roomProperties = level.rooms[i];
            ushort currentRoomId = (ushort)(i + 1); // Room IDs are 1-based

            if (roomProperties.type == "hall") continue;
            ConsoleHelper.LogConverterInfo($"Checking room {currentRoomId} ({roomProperties.type})...");

            string floor = roomProperties.textures.floor;
            string wall = roomProperties.textures.wall;
            string ceiling = roomProperties.textures.ceiling;
            UpdateOldAssetName(ref floor, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref wall, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref ceiling, LevelFieldType.RoomTexture);

            var roomAsset = new BaldiRoomAsset
            {
                name = roomProperties.type,
                type = roomProperties.type,
                textureContainer = new PlusStudioLevelFormat.TextureContainer(floor, wall, ceiling),
                windowType = "standard" // Default value
            };

            // Gather all cells and data for this room
            ConsoleHelper.LogConverterInfo("Gathering all the cells related to the room...");
            var cells = new List<RoomCellInfo>();
            var originalOwnedCells = new List<PlusLevelFormat.ByteVector2>();

            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    if (level.tiles[x, y].roomId == currentRoomId)
                    {
                        var currentPos = new PlusLevelFormat.ByteVector2((byte)x, (byte)y);
                        originalOwnedCells.Add(currentPos);
                        cells.Add(new RoomCellInfo
                        {
                            walls = new(level.tiles[x, y].walls),
                            position = new PlusStudioLevelFormat.ByteVector2((byte)x, (byte)y),
                            coverage = level.tiles[x, y].CoverageFromTile()
                        });

                        if (level.entitySafeTiles[x, y])
                        {
                            roomAsset.entitySafeCells.Add(new PlusStudioLevelFormat.ByteVector2((byte)x, (byte)y));
                        }
                        if (level.eventSafeTiles[x, y])
                        {
                            roomAsset.eventSafeCells.Add(new PlusStudioLevelFormat.ByteVector2((byte)x, (byte)y));
                        }
                        // Only plausable way would be this Mystery thing
                        if (roomProperties.type == "mystery")
                        {
                            roomAsset.secretCells.Add(new PlusStudioLevelFormat.ByteVector2((byte)x, (byte)y));
                        }
                    }
                }
            }

            if (cells.Count == 0)
            {
                ConsoleHelper.LogWarn("The room appears to have 0 cells registered. Skipping...");
                continue;
            }

            ConsoleHelper.LogConverterInfo($"Detected {cells.Count} in total\n{roomAsset.entitySafeCells.Count} entity-safe cells\n{roomAsset.eventSafeCells.Count} event-safe cells\n{roomAsset.secretCells.Count} secret cells");

            ConsoleHelper.LogConverterInfo("Calculating cell offset for room...");

            // Calculate the offset to re-align the room to 0,0
            byte minX = cells[0].position.x;
            byte minY = cells[0].position.y;
            for (int j = 1; j < cells.Count; j++)
            {
                if (cells[j].position.x < minX) minX = cells[j].position.x;
                if (cells[j].position.y < minY) minY = cells[j].position.y;
            }
            var offsetX = minX;
            var offsetY = minY;

            // Apply offset to all positions
            for (int j = 0; j < cells.Count; j++)
            {
                cells[j].position = new PlusStudioLevelFormat.ByteVector2((byte)(cells[j].position.x - offsetX), (byte)(cells[j].position.y - offsetY));
            }
            roomAsset.cells = cells;

            for (int j = 0; j < roomAsset.entitySafeCells.Count; j++)
            {
                var pos = roomAsset.entitySafeCells[j];
                roomAsset.entitySafeCells[j] = new PlusStudioLevelFormat.ByteVector2((byte)(pos.x - offsetX), (byte)(pos.y - offsetY));
            }
            for (int j = 0; j < roomAsset.eventSafeCells.Count; j++)
            {
                var pos = roomAsset.eventSafeCells[j];
                roomAsset.eventSafeCells[j] = new PlusStudioLevelFormat.ByteVector2((byte)(pos.x - offsetX), (byte)(pos.y - offsetY));
            }
            for (int j = 0; j < roomAsset.secretCells.Count; j++)
            {
                var pos = roomAsset.secretCells[j];
                roomAsset.secretCells[j] = new PlusStudioLevelFormat.ByteVector2((byte)(pos.x - offsetX), (byte)(pos.y - offsetY));
            }

            // Convert and offset Basic Objects (Prefabs)
            int counter = 0;
            ConsoleHelper.LogConverterInfo("Processing objects and converting special markers...");

            foreach (var obj in roomProperties.prefabs)
            {
                var objPos = new Vector3(obj.position.x, obj.position.y, obj.position.z);
                var cellPos = new IntVector2(Mathf.RoundToInt((objPos.x - 5f) / 10f), Mathf.RoundToInt((objPos.z - 5f) / 10f));
                var byteCellPos = new PlusStudioLevelFormat.ByteVector2(cellPos.x, cellPos.z);
                // Console.WriteLine($"Found prefab: {obj.prefab}");
                bool isMarker = true;
                switch (obj.prefab)
                {
                    case "potentialDoorMarker":
                        roomAsset.potentialDoorPositions.Add(byteCellPos);
                        break;
                    case "forcedDoorMarker":
                        roomAsset.forcedDoorPositions.Add(byteCellPos);
                        break;
                    case "itemSpawnMarker":
                        // This marker becomes an ItemSpawnPlacement, not a structure, with a default weight.
                        roomAsset.itemSpawns.Add(new() { position = new UnityVector2(objPos.x, objPos.z), weight = 100 });
                        break;
                    case "nonSafeCellMarker":
                        roomAsset.entitySafeCells.Remove(byteCellPos);
                        roomAsset.eventSafeCells.Remove(byteCellPos);
                        break;
                    case "lightSpotMarker":
                        roomAsset.standardLightCells.Add(byteCellPos);
                        break;
                    default:
                        isMarker = false;
                        break;
                }
                // Console.WriteLine(isMarker);
                if (!isMarker)
                {
                    // This is a regular object, so add it to the objects list only if the prefab is allowed
                    string renamedObj = obj.prefab;
                    if (UpdateOldAssetName(ref renamedObj, LevelFieldType.Object))
                    {
                        roomAsset.basicObjects.Add(new BasicObjectInfo
                        {
                            prefab = renamedObj,
                            position = new PlusStudioLevelFormat.UnityVector3(
                            obj.position.x - (offsetX * 10f),
                            obj.position.y,
                            obj.position.z - (offsetY * 10f)),
                            rotation = new(obj.rotation.x, obj.rotation.y, obj.rotation.z, obj.rotation.w)
                        });
                    }
                }
                else counter++;
            }

            ConsoleHelper.LogConverterInfo($"Processed {roomAsset.basicObjects.Count} objects.");
            if (counter != 0)
                ConsoleHelper.LogConverterInfo($"{counter} objects were detected as legacy markers and were properly replaced.");

            ConsoleHelper.LogConverterInfo("Converting items...");
            // Convert and offset Items
            foreach (var item in roomProperties.items)
            {
                string renamedItem = item.item;
                if (UpdateOldAssetName(ref renamedItem, LevelFieldType.Item))
                {
                    roomAsset.items.Add(new ItemInfo
                    {
                        item = renamedItem,
                        position = new UnityVector2( // Note: converting from Vector3 to Vector2
                            item.position.x - (offsetX * 10f),
                            item.position.z - (offsetY * 10f))
                    });
                }
            }
            ConsoleHelper.LogConverterInfo($"{roomAsset.items.Count} items added!");

            ConsoleHelper.LogConverterInfo("Converting light objects...");

            // Find and offset lights belonging to this room
            foreach (var light in level.lights)
            {
                if (originalOwnedCells.Any(c => c.x == light.position.x && c.y == light.position.y))
                {
                    string renamedLight = light.type;
                    if (UpdateOldAssetName(ref renamedLight, LevelFieldType.Light))
                    {
                        roomAsset.lights.Add(new LightInfo
                        {
                            prefab = renamedLight,
                            color = new(light.color.r, light.color.g, light.color.b, light.color.a),
                            strength = light.strength,
                            position = new PlusStudioLevelFormat.ByteVector2((byte)(light.position.x - offsetX), (byte)(light.position.y - offsetY))
                        });
                    }
                }
            }

            ConsoleHelper.LogConverterInfo($"{roomAsset.lights.Count} light objects added!");
            // CBLDs were never able of having posters, but I'll leave this in here just in case I need it later
            // ConsoleHelper.LogConverterInfo("Converting posters");

            // // Find and offset posters belonging to this room
            // foreach (var poster in level.posters)
            // {
            // 	if (originalOwnedCells.Exists(c => c.x == poster.position.x && c.y == poster.position.y))
            // 	{
            // 		roomAsset.posters.Add(new PosterInfo
            // 		{
            // 			poster = poster.type,
            // 			direction = (PlusStudioLevelFormat.PlusDirection)(int)poster.direction, // They are numerically the same directions registered
            // 			position = new PlusStudioLevelFormat.ByteVector2((byte)(poster.position.x - offsetX), (byte)(poster.position.y - offsetY))
            // 		});
            // 	}
            // }

            // ConsoleHelper.LogConverterInfo($"{roomAsset.posters.Count} posters added!");

            // Convert and offset Activity
            if (roomProperties.activity != null)
            {
                string renamedActivity = roomProperties.activity.activity;
                if (UpdateOldAssetName(ref renamedActivity, LevelFieldType.Activity))
                {
                    ConsoleHelper.LogConverterInfo("Converting activity...");
                    roomAsset.activity = new ActivityInfo
                    {
                        type = renamedActivity,
                        direction = (PlusStudioLevelFormat.PlusDirection)(int)roomProperties.activity.direction,
                        position = new PlusStudioLevelFormat.UnityVector3(
                            roomProperties.activity.position.x - (offsetX * 10f),
                            roomProperties.activity.position.y,
                            roomProperties.activity.position.z - (offsetY * 10f))
                    };
                    ConsoleHelper.LogConverterInfo($"\'{roomAsset.activity.type}\' activity added!");
                }
            }
            else
                ConsoleHelper.LogConverterInfo("The room has no activity available.");

            // Generate a unique name for the asset file
            roomAsset.name += "_" + i + "_" + roomAsset.cells.Count + "_" + (roomAsset.activity == null ? "null" : roomAsset.activity.type);
            roomAssets.Add(roomAsset);
            ConsoleHelper.LogInfo($"Converted room \'{roomProperties.type}\' (ID: {currentRoomId}) into an RBPL asset.");
        }

        return roomAssets;
    }
    #endregion
}