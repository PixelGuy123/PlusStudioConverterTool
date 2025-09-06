using BaldiLevelEditor;
using PlusLevelFormat;
using PlusLevelLoader;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    #region CBLDTOBLD
    public static EditorLevel ConvertCBLDtoBLDFormat(this Level level)
    {
        ConsoleHelper.LogInfo("Converting CBLD to BLD level...");
        EditorLevel newLevel = new(level.width, level.height)
        {
            blockedWalls = level.blockedWalls,
            buttons = level.buttons,
            entitySafeTiles = level.entitySafeTiles,
            eventSafeTiles = level.eventSafeTiles,
            tiles = level.tiles
        };

        ConsoleHelper.LogConverterInfo("Initializing EditorLevel...");
        ConsoleHelper.LogConverterInfo($"Size of level: {level.tiles.GetLength(0)},{level.tiles.GetLength(1)}");

        foreach (var door in level.doors)
        {
            string renamed = door.type;
            if (UpdateOldAssetName(ref renamed, LevelFieldType.Door))
            {
                var newDoor = door;
                newDoor.type = renamed;
                newLevel.doors.Add(newDoor);
            }
        }
        foreach (var window in level.windows)
        {
            string renamed = window.type;
            if (UpdateOldAssetName(ref renamed, LevelFieldType.Window))
            {
                var newWindow = window;
                newWindow.type = renamed;
                newLevel.windows.Add(newWindow);
            }
        }
        foreach (var exit in level.exits)
        {
            string renamed = exit.type;
            if (UpdateOldAssetName(ref renamed, LevelFieldType.Exit))
            {
                var newExit = exit;
                newExit.type = renamed;
                newLevel.exits.Add(newExit);
            }
        }
        foreach (var npc in level.npcSpawns)
        {
            string renamed = npc.type;
            if (UpdateOldAssetName(ref renamed, LevelFieldType.NPC))
            {
                var newNpc = npc;
                newNpc.type = renamed;
                newLevel.npcSpawns.Add(newNpc);
            }
        }
        foreach (var prefab in level.tiledPrefabs)
        {
            string renamed = prefab.type;
            if (UpdateOldAssetName(ref renamed, LevelFieldType.Structure))
            {
                var newPrefab = prefab;
                newPrefab.type = renamed;
                newLevel.tiledPrefabs.Add(newPrefab);
            }
        }

        for (int i = 0; i < level.rooms.Count; i++)
        {
            var oldRoom = level.rooms[i];
            var newRoom = new RoomProperties { type = oldRoom.type };

            string floor = oldRoom.textures.floor;
            string wall = oldRoom.textures.wall;
            string ceiling = oldRoom.textures.ceiling;
            UpdateOldAssetName(ref floor, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref wall, LevelFieldType.RoomTexture);
            UpdateOldAssetName(ref ceiling, LevelFieldType.RoomTexture);
            newRoom.textures = new() { floor = floor, wall = wall, ceiling = ceiling };
            newLevel.defaultTextures.TryAdd(newRoom.type, newRoom.textures);

            foreach (var item in oldRoom.items)
            {
                string renamed = item.item;
                if (UpdateOldAssetName(ref renamed, LevelFieldType.Item))
                {
                    var newItem = item;
                    newItem.item = renamed;
                    newRoom.items.Add(newItem);
                    newLevel.items.Add(newItem);
                }
            }

            foreach (var prefab in oldRoom.prefabs)
            {
                string renamed = prefab.prefab;
                if (UpdateOldAssetName(ref renamed, LevelFieldType.Object))
                {
                    var newPrefab = prefab;
                    newPrefab.prefab = renamed;
                    newRoom.prefabs.Add(newPrefab);
                    newLevel.prefabs.Add(newPrefab);
                }
            }

            if (oldRoom.activity != null)
            {
                string renamed = oldRoom.activity.activity;
                if (UpdateOldAssetName(ref renamed, LevelFieldType.Activity))
                {
                    newRoom.activity = new RoomActivity
                    {
                        activity = renamed,
                        direction = oldRoom.activity.direction,
                        position = oldRoom.activity.position
                    };
                }
            }
            newLevel.rooms.Add(newRoom);
        }

        ConsoleHelper.LogConverterInfo("Initializing tile data...");

        foreach (var tile in level.tiles)
        {
            int x = tile.position.x;
            int y = tile.position.y;
            //Console.WriteLine($"Checking wall position at ({x},{y}), is it InBounds? {level.tiles.InBounds(x, y)}");
            if (!level.tiles.InBounds(x, y) || !level.tiles[x, y].IsValid()) continue;

            foreach (var dir in tile.DirsFromTile())
            {
                var vec = dir.ToByteVector2();
                //Console.WriteLine($"Checking wall at ({x},{y}) as placed in dir: {dir} with offset: ({vec.Item1},{vec.Item2})"); // 3 walls?? Check with another level I guess
                x += vec.Item1;
                y += vec.Item2;

                if (level.tiles.InBounds(x, y) && level.tiles[x, y].IsValid(tile.roomId))
                {
                    ConsoleHelper.LogConverterInfo($"Marked wall at ({tile.position.x},{tile.position.y}) as placed in dir: {dir}");
                    newLevel.manualWalls.Add(new() { direction = dir, position = new(tile.position.x, tile.position.y) }); // converts to int which is equal to the PlusDirection
                    level.tiles[tile.position.x, tile.position.y].walls = (Nybble)ToggleBit(level.tiles[tile.position.x, tile.position.y].walls, (int)dir);
                }

                x = tile.position.x;
                y = tile.position.y;
            }
        }

        ConsoleHelper.LogConverterInfo($"{newLevel.manualWalls.Count} walls placed in total!");


        ConsoleHelper.LogConverterInfo("Initializing elevator areas...");

        foreach (var elevator in level.exits)
        {
            ConsoleHelper.LogConverterInfo($"Added elevator at ({elevator.position.x},{elevator.position.y}) at direction {elevator.direction}");
            newLevel.elevatorAreas.Add(new(elevator.position, 1, elevator.direction.ToStandard()), elevator);
        }


        ConsoleHelper.LogConverterInfo("Initializing general areas...");
        // Area detection algorithm here
        bool[,] accessedTiles = new bool[level.tiles.GetLength(0), level.tiles.GetLength(1)];


        while (true)
        {
            int ogX = 0;
            int ogY = 0;
            int id = -1;
            bool flag = false;

            //Console.WriteLine("\nStarting new iteration\n");

            for (; ogX < level.tiles.GetLength(0); ogX++)
            {
                for (ogY = 0; ogY < level.tiles.GetLength(1); ogY++)
                {
                    //Console.WriteLine("Checking for id: " + level.tiles[ogX, ogY].roomId + " of type: " + level.tiles[ogX, ogY].type + $" at pos: ({ogX},{ogY}) of boolean: "+ accessedTiles[ogX, ogY]);
                    if (level.tiles[ogX, ogY].IsValid() && !accessedTiles[ogX, ogY])
                    {
                        id = level.tiles[ogX, ogY].roomId; // First get an available tile to begin an area search on
                        accessedTiles[ogX, ogY] = true;
                        //Console.WriteLine("-- Detected for id: " + id + " of type: " + level.tiles[ogX, ogY].type + $" at pos: ({ogX},{ogY})");
                        flag = true;
                        break;
                    }
                    accessedTiles[ogX, ogY] = true;
                }
                if (flag)
                    break;
            }

            if (id == -1) // First phase done
                break;

            int bigY = ogY; // Default is size of 1
            int y;

            for (y = ogY + 1; y < level.tiles.GetLength(1); y++)
            {
                //Console.WriteLine("Checking for id: " + level.tiles[ogX, y].roomId + " of type: " + level.tiles[ogX, y].type + $" at pos: ({ogX},{y})");
                if (!accessedTiles[ogX, y] && level.tiles[ogX, y].IsValid(id))
                {
                    bigY = y;
                    accessedTiles[ogX, y] = true; // Get the highest height of that area to expand
                }
                else
                {
                    //Console.WriteLine("Broke on y: " + y);
                    break;
                }
            }

            //Console.WriteLine("Created bigY of " + bigY);

            int x = ogX + 1;
            flag = false;


            for (; x < level.tiles.GetLength(0); x++)
            {
                for (y = ogY; y <= bigY; y++)
                {
                    //Console.WriteLine("Checking for id: " + level.tiles[x, y].roomId + " of type: " + level.tiles[x, y].type + $" at pos: ({x},{y})");

                    if (accessedTiles[x, y] || !level.tiles[x, y].IsValid(id)) // Just fill up the area
                    {
                        // If an invalid wall was detected, it means the size has been reached
                        flag = true;
                        break;
                    }
                }
                if (flag) break;
            }

            var size = new ByteVector2(x - ogX, 1 + bigY - ogY);
            newLevel.areas.Add(new AreaData(new(ogX, ogY), size, (ushort)id));
            ConsoleHelper.LogConverterInfo($"Area {newLevel.areas.Count} created with size: ({size.x},{size.y}) at pos: ({ogX},{ogY})");

            size = new(size.x + ogX, size.y + ogY); // Update to the actual position

            for (int x2 = ogX; x2 < size.x; x2++)
            {
                for (int y2 = ogY; y2 < size.y; y2++)
                {
                    if (accessedTiles.InBounds(x2, y2))
                        accessedTiles[x2, y2] = true;
                }
            }

        }

        ConsoleHelper.LogConverterInfo($"{newLevel.areas.Count} areas created in total!");

        return newLevel;
    }
    #endregion
}
