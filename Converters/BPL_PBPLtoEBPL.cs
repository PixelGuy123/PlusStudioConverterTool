using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;
using PlusStudioLevelFormat;
using PlusStudioLevelLoader;
using UnityEngine;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
{
    #region BPLtoEBPL
    public static EditorFileContainer ConvertBPLtoEBPLFormat(this BaldiLevel level, PlayableLevelMeta? meta, string editorMode)
    {
        ConsoleHelper.LogInfo("Converting BPL/PBPL to EBPL format...");
        var fileContainer = new EditorFileContainer();

        // 1. Initialize EditorLevelData with metadata from the compiled level.
        var newData = new EditorLevelData(PlusStudioLevelLoader.Extensions.ToInt(level.levelSize))
        {
            elevatorTitle = level.levelTitle,
            timeLimit = level.timeLimit,
            skybox = level.skybox,
            minLightColor = level.minLightColor.ToStandard(),
            lightMode = (LightMode)level.lightMode,
            spawnPoint = level.spawnPoint.ToUnity(),
            spawnDirection = (Direction)level.spawnDirection,
            initialRandomEventGap = level.initialRandomEventGap,
            minRandomEventGap = level.minRandomEventGap,
            maxRandomEventGap = level.maxRandomEventGap,
            randomEvents = [.. level.randomEvents],
            meta = new PlayableLevelMeta() // Use provided meta or create a default one.
            {
                name = level.levelTitle,
                author = meta != null ? meta.author : "BPL Converted",
                gameMode = meta != null ? meta.gameMode : "standard",
                contentPackage = new EditorCustomContentPackage(true) { entries = meta != null ? meta.contentPackage.entries : [] }
            }
        };

        // 2. Convert rooms and their contained objects.
        ConsoleHelper.LogConverterInfo("Converting rooms and their objects...");
        newData.rooms.Clear();
        foreach (var roomInfo in level.rooms)
        {
            var newRoom = new EditorRoom(roomInfo.type, new TextureContainer(roomInfo.textureContainer));
            string renamedType = newRoom.roomType;
            UpdateOldAssetName(ref renamedType, LevelFieldType.RoomTexture);
            newRoom.roomType = renamedType;

            // In BaldiLevel, objects are nested in rooms. In EditorLevelData, they are in global lists.
            foreach (var item in roomInfo.items)
            {
                string renamedItem = item.item;
                if (UpdateOldAssetName(ref renamedItem, LevelFieldType.Item))
                    newData.items.Add(new ItemPlacement { item = renamedItem, position = item.position.ToUnity() });
            }
            foreach (var spawn in roomInfo.itemSpawns)
            {
                newData.itemSpawns.Add(new ItemSpawnPlacement { weight = spawn.weight, position = spawn.position.ToUnity() });
            }
            foreach (var obj in roomInfo.basicObjects)
            {
                string renamedObj = obj.prefab;
                if (UpdateOldAssetName(ref renamedObj, LevelFieldType.Object))
                {
                    var marker = TryConvertToMarker(obj, newData);
                    if (marker == null)
                        newData.objects.Add(new BasicObjectLocation { prefab = renamedObj, position = obj.position.ToUnity(), rotation = obj.rotation.ToUnity() });
                    else
                        newData.markers.Add(marker);
                }
            }

            if (roomInfo.activity != null)
            {
                string renamedActivity = roomInfo.activity.type;
                if (UpdateOldAssetName(ref renamedActivity, LevelFieldType.Activity))
                {
                    var activity = new ActivityLocation
                    {
                        type = renamedActivity,
                        position = roomInfo.activity.position.ToUnity(),
                        direction = (Direction)roomInfo.activity.direction
                    };
                    activity.Setup(newRoom);
                }
            }
            newData.rooms.Add(newRoom);
        }
        ConsoleHelper.LogConverterInfo($"{newData.rooms.Count} rooms converted.");

        // 3. Reconstruct CellAreas from the cell grid.
        ConsoleHelper.LogConverterInfo("Reconstructing room areas...");
        bool[,] visited = new bool[newData.mapSize.x, newData.mapSize.z];
        for (int y = 0; y < newData.mapSize.z; y++)
        {
            for (int x = 0; x < newData.mapSize.x; x++)
            {
                if (visited[x, y] || level.cells[x, y].roomId == 0) continue;

                ushort currentRoomId = level.cells[x, y].roomId;
                int width = 1;
                while (x + width < newData.mapSize.x && !visited[x + width, y] && level.cells[x + width, y].roomId == currentRoomId)
                    width++;

                int height = 1;
                bool canExpandDown = true;
                while (y + height < newData.mapSize.z && canExpandDown)
                {
                    for (int i = 0; i < width; i++)
                    {
                        if (visited[x + i, y + height] || level.cells[x + i, y + height].roomId != currentRoomId)
                        {
                            canExpandDown = false;
                            break;
                        }
                    }
                    if (canExpandDown) height++;
                }

                newData.areas.Add(new RectCellArea(new IntVector2(x, y), new IntVector2(width, height), currentRoomId));
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                        visited[x + i, y + j] = true;
                }
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.areas.Count} areas reconstructed.");

        // 4. Convert simple placements.
        ConsoleHelper.LogConverterInfo("Converting windows...");
        foreach (var window in level.windows)
        {
            string renamedWindow = window.prefab;
            if (UpdateOldAssetName(ref renamedWindow, LevelFieldType.Window))
                newData.windows.Add(new WindowLocation { type = renamedWindow, position = PlusStudioLevelLoader.Extensions.ToInt(window.position), direction = (Direction)window.direction });
        }
        ConsoleHelper.LogConverterInfo($"{newData.windows.Count} windows converted!");
        ConsoleHelper.LogConverterInfo("Converting exits...");
        foreach (var exit in level.exits)
        {
            string renamedExit = exit.type;
            if (UpdateOldAssetName(ref renamedExit, LevelFieldType.Exit))
                newData.exits.Add(new ExitLocation { type = renamedExit, position = PlusStudioLevelLoader.Extensions.ToInt(exit.position), direction = (Direction)exit.direction, isSpawn = exit.isSpawn });
        }
        ConsoleHelper.LogConverterInfo($"{newData.exits.Count} exits converted!");
        ConsoleHelper.LogConverterInfo("Converting NPCs...");
        foreach (var npc in level.npcs)
        {
            string renamedNpc = npc.npc;
            if (UpdateOldAssetName(ref renamedNpc, LevelFieldType.NPC))
                newData.npcs.Add(new NPCPlacement { npc = renamedNpc, position = PlusStudioLevelLoader.Extensions.ToInt(npc.position) });
        }
        ConsoleHelper.LogConverterInfo($"{newData.npcs.Count} NPCs converted!");
        ConsoleHelper.LogConverterInfo("Converting posters...");
        foreach (var poster in level.posters)
        {
            string renamedPoster = poster.poster;
            if (UpdateOldAssetName(ref renamedPoster, LevelFieldType.Poster))
                newData.posters.Add(new PosterPlacement { type = renamedPoster, position = PlusStudioLevelLoader.Extensions.ToInt(poster.position), direction = (Direction)poster.direction });
        }
        ConsoleHelper.LogConverterInfo($"{newData.posters.Count} posters converted!");

        // 5. Convert doors from both DoorInfo and TileObjectInfo lists.
        ConsoleHelper.LogConverterInfo("Converting doors...");
        foreach (var door in level.doors)
        {
            string renamedDoor = door.prefab;
            if (UpdateOldAssetName(ref renamedDoor, LevelFieldType.Door))
            {
                newData.doors.Add(new DoorLocation { type = renamedDoor, position = PlusStudioLevelLoader.Extensions.ToInt(door.position), direction = (Direction)door.direction });
                // Console.WriteLine(renamedDoor);
                // Console.WriteLine(door.direction);
            }
        }
        ConsoleHelper.LogConverterInfo($"{newData.doors.Count} doors converted!");
        ConsoleHelper.LogConverterInfo("Converting tile objects to other editor-acceptable types...");
        int counter = 0;
        foreach (var tileObj in level.tileObjects)
        {
            string renamedDoor = tileObj.prefab;
            if (UpdateOldAssetName(ref renamedDoor, LevelFieldType.Door))
            {
                newData.doors.Add(new DoorLocation { type = renamedDoor, position = PlusStudioLevelLoader.Extensions.ToInt(tileObj.position), direction = (Direction)tileObj.direction });
                // Console.WriteLine(renamedDoor);
                // Console.WriteLine(tileObj.direction);
                counter++;
            }
        }
        ConsoleHelper.LogConverterInfo($"{counter} tile objects converted to other types!");

        // 6. Reconstruct LightGroups from individual LightInfo entries.
        ConsoleHelper.LogConverterInfo("Reconstructing light groups...");
        newData.lightGroups.Clear();
        var lightGroupMap = new Dictionary<Tuple<Color, byte>, ushort>();
        foreach (var light in level.lights)
        {
            var color = light.color.ToStandard();
            var key = Tuple.Create(color, light.strength);
            if (!lightGroupMap.TryGetValue(key, out ushort groupId))
            {
                var newGroup = new LightGroup { color = color, strength = light.strength };
                newData.lightGroups.Add(newGroup);
                groupId = (ushort)(newData.lightGroups.Count - 1);
                lightGroupMap[key] = groupId;
            }
            string renamedLight = light.prefab;
            if (UpdateOldAssetName(ref renamedLight, LevelFieldType.Light))
                newData.lights.Add(new LightPlacement { type = renamedLight, position = PlusStudioLevelLoader.Extensions.ToInt(light.position), lightGroup = groupId });
        }
        if (newData.lightGroups.Count == 0) newData.lightGroups.Add(new LightGroup());
        ConsoleHelper.LogConverterInfo($"{newData.lightGroups.Count} light groups created.");

        // 7. Detect and create manual (internal) walls.

        ConsoleHelper.LogConverterInfo("Detecting manual walls...");
        foreach (var tile in level.cells)
        {
            int x = tile.position.x;
            int y = tile.position.y;

            if (!level.cells.InBounds(x, y) || level.cells[x, y].roomId == 0) continue;
            // Console.WriteLine($"Checking wall position at ({x},{y})");
            bool checkForWalls = false;
            for (int i = 0; i <= 1; i++)
            {
                foreach (var dir in tile.DirsFromTile(checkForWalls))
                {
                    var vec = dir.ToByteVector2();
                    // Console.WriteLine($"Checking wall at ({x},{y}) as placed in dir: {dir} with offset: ({vec.Item1},{vec.Item2})"); // 3 walls?? Check with another level I guess
                    x += vec.Item1;
                    y += vec.Item2;
                    // Console.WriteLine($"Normal ID: {tile.roomId} | OffsetID: {level.cells[x, y].roomId}");

                    if (level.cells.InBounds(x, y) &&
                    level.cells[x, y].roomId != 0 && // Must be non-zero to actually count as a room, not void
                    (level.cells[x, y].roomId == tile.roomId == checkForWalls)) // If checkForWalls, it should expect the tiles to be from the same room id
                    {
                        ConsoleHelper.LogConverterInfo($"Marked {(checkForWalls ? "wall" : "wall-remover")} at ({tile.position.x},{tile.position.y}) as placed in dir: {dir}");
                        newData.walls.Add(new() { direction = (Direction)dir, position = new(tile.position.x, tile.position.y), wallState = checkForWalls }); // converts to int which is equal to the PlusDirection
                        newData.cells[tile.position.x, tile.position.y].walls = (Nybble)ToggleBit(newData.cells[tile.position.x, tile.position.y].walls, (int)dir);
                    }

                    x = tile.position.x;
                    y = tile.position.y;
                }
                checkForWalls = !checkForWalls;
            }
        }
        // Failsafe to remove duplicated walls
        for (int i = 0; i < newData.walls.Count; i++)
        {
            var currentWall = newData.walls[i];
            var adjacentPosition = currentWall.position + currentWall.direction.ToNETIntVector2();
            var oppositeDir = currentWall.direction.GetNETOpposite();
            // If in the currentWall, another adjacent wall placement exists with the same exact direction opposition, then this currentWall shouldn't exist
            if (newData.walls.Exists(wall => currentWall != wall && wall.position == adjacentPosition && wall.direction == oppositeDir))
                newData.walls.RemoveAt(i--);
        }
        ConsoleHelper.LogConverterInfo($"{newData.walls.Count} manual walls detected.");

        // 8. Reverse-engineer structures from their compiled data.
        ConsoleHelper.LogConverterInfo("Converting structures...");
        foreach (var structureInfo in level.structures)
        {
            StructureLocation? newStructure = ConvertStructure(structureInfo, newData);
            if (newStructure != null)
                newData.structures.Add(newStructure);
        }
        ConsoleHelper.LogConverterInfo($"{newData.structures.Count} structures converted.");

        // 9. Finalize the file container with default editor metadata.
        fileContainer.data = newData;

        string[] barTools = new string[9];
        for (int i = 0; i < barTools.Length; i++)
            barTools[i] = string.Empty;

        fileContainer.meta = new EditorFileMeta
        {
            cameraPosition = Vector3.zero,
            cameraRotation = Quaternion.identity,
            editorMode = editorMode,
            toolbarTools = barTools
        };

        ConsoleHelper.LogInfo("Conversion to EBPL format completed.");
        return fileContainer;
    }

    private static StructureLocation? ConvertStructure(StructureInfo info, EditorLevelData data)
    {
        switch (info.type)
        {
            case "conveyorbelt":
                return ConvertConveyorBeltStructure(info);
            case "halldoor":
            case "swingdoor":
            case "facultyonlydoor":
            case "regionlockdowndoors":
                return ConvertHallDoorStructure(info);
            case "halldoor_button":
                return ConvertHallDoorWithButtonsOrLeversStructure<HallDoorStructureLocationWithButtons>(info);
            case "lockdowndoor":
                return ConvertHallDoorWithButtonsOrLeversStructure<LockdownDoorStructureLocation>(info);
            case "lockdowndoor_button":
                return ConvertHallDoorWithButtonsOrLeversStructure<LockdownDoorWithButtonsStructureLocation>(info);
            case "powerlever":
                return ConvertPowerLeverStructure(info, data);
            case "shapelock":
                return ConvertShapeLockStructure(info, data);
            case "steamvalves":
                return ConvertSteamValveStructure(info);
            case "vent":
                return ConvertVentStructure(info);
            case "teleporters":
                return ConvertTeleporterStructure(info);
            case "region":
                return ConvertRegionStructure(info, data);
            case "factorybox":
                return new FactoryBoxStructureLocation() { type = info.type };
            default:
                ConsoleHelper.LogWarn($"Structure of type '{info.type}' found but conversion is not yet implemented. This structure will be missing from the level.");
                return null;
        }
    }

    private static HallDoorStructureLocation ConvertHallDoorStructure(StructureInfo info)
    {
        var structure = new HallDoorStructureLocation { type = info.type };
        foreach (var data in info.data)
        {
            var child = structure.CreateNewChild();
            child.prefab = data.prefab;
            child.position = data.position.ToStandard();
            child.direction = (Direction)data.direction;
            structure.myChildren.Add(child);
        }
        return structure;
    }


    private static T ConvertHallDoorWithButtonsOrLeversStructure<T>(StructureInfo info) where T : HallDoorStructureLocationWithButtons, new()
    {
        var structure = new T { type = info.type };
        for (int i = 0; i < info.data.Count; i += 2)
        {
            var doorData = info.data[i];
            var buttonData = info.data[i + 1];

            var child = structure.CreateNewChild();
            child.prefab = doorData.prefab;
            child.position = doorData.position.ToStandard();
            child.direction = (Direction)doorData.direction;
            structure.myChildren.Add(child);

            var button = structure.CreateNewButton();
            button.position = buttonData.position.ToStandard();
            button.direction = (Direction)buttonData.direction;
            structure.buttons.Add(button);
        }
        return structure;
    }

    private static ConveyorBeltStructureLocation ConvertConveyorBeltStructure(StructureInfo info)
    {
        var structure = new ConveyorBeltStructureLocation { type = info.type };
        var tempBelts = new List<ConveyorBeltLocation>();
        var tempButtons = new List<SimpleButtonLocation>();
        var beltToButtonMap = new Dictionary<ConveyorBeltLocation, SimpleButtonLocation>();

        int i = 0;
        while (i < info.data.Count)
        {
            var beltsInGroup = new List<ConveyorBeltLocation>();
            while (i < info.data.Count && info.data[i].data == 0)
            {
                var startData = info.data[i];
                var endData = info.data[i + 1];
                var belt = structure.CreateBelt();
                belt.startPosition = startData.position.ToStandard();
                var endPosition = endData.position.ToStandard();
                var diff = endPosition - belt.startPosition;
                belt.distance = (byte)(Math.Abs(diff.x) + Math.Abs(diff.z) + 1);
                belt.direction = diff.ToDirection();
                tempBelts.Add(belt);
                beltsInGroup.Add(belt);
                i += 2;
            }

            if (i < info.data.Count && info.data[i].data == 1)
            {
                var buttonData = info.data[i];
                var button = structure.CreateButton();
                button.position = buttonData.position.ToStandard();
                button.direction = (Direction)buttonData.direction;
                tempButtons.Add(button);
                foreach (var belt in beltsInGroup)
                    beltToButtonMap[belt] = button;
                i++;
            }
        }

        structure.buttons = tempButtons;
        structure.belts = tempBelts;
        foreach (var belt in structure.belts)
            if (beltToButtonMap.TryGetValue(belt, out var button))
                belt.buttonIndex = structure.buttons.IndexOf(button);
            else
                belt.buttonIndex = -1;

        return structure;
    }

    private static RegionStructureLocation ConvertRegionStructure(StructureInfo info, EditorLevelData data)
    {
        var regionStructLoc = new RegionStructureLocation() { type = info.type };
        for (int i = 0; i < info.data.Count; i++)
        {
            regionStructLoc.regions.Add(new RegionLocation()
            {
                id = info.data[i].position.z, // For some reason, it's assigned in a position. 
                room = data.rooms[info.data[i].position.x],
                myStructure = regionStructLoc
            });
        }
        return regionStructLoc;
    }
    private static TeleporterStructureLocation ConvertTeleporterStructure(StructureInfo info)
    {
        var teleporterStructLoc = new TeleporterStructureLocation() { type = info.type };
        for (int i = 0; i < info.data.Count; i++) // Basically 3 StructureDataInfos are added per Teleporter, so read all three
        {
            // 1 - Room ID (Useless, so can be skipped)
            // 2 - Position of the teleporterLocation / data = direction as integer
            // 3 - position of the machine / data = direction as integer

            TeleporterLocation teleporterLoc = new();
            i++; // Skips the room ID thing, since it's unused here

            // We get to 2 here
            teleporterLoc.position = new(info.data[i].position.x.ConvertToFloatNoRecast(), info.data[i].position.z.ConvertToFloatNoRecast()); // pos
            teleporterLoc.direction = info.data[i].data.ConvertToFloatNoRecast(); // int bit struct, whatever the editor calls this lol

            i++; // Goes to 3: machine

            // Do the machine thing
            teleporterLoc.machine = new()
            {
                position = new(info.data[i].position.x.ConvertToFloatNoRecast(), info.data[i].position.z.ConvertToFloatNoRecast()),
                direction = info.data[i].data.ConvertToFloatNoRecast()
            };

            // Assign teleporter location to the structure
            teleporterLoc.myStructure = teleporterStructLoc;
            teleporterStructLoc.teleporters.Add(teleporterLoc);
        }
        return teleporterStructLoc;
    }

    private static PowerLeverStructureLocation ConvertPowerLeverStructure(StructureInfo info, EditorLevelData data)
    {
        var structure = new PowerLeverStructureLocation { type = info.type };
        if (info.data.Count < 2) return structure;

        structure.maxLevers = info.data[0].data;
        structure.poweredRoomChance = info.data[1].data;

        int i = 2;
        while (i < info.data.Count)
        {
            var marker = info.data[i];
            switch (marker.data)
            {
                case 0: // Alarm light
                    var alarm = structure.CreateAlarmLight();
                    alarm.position = marker.position.ToStandard();
                    structure.alarmLights.Add(alarm);
                    i++;
                    break;
                case 1: // Power lever
                    var lever = structure.CreatePowerLever();
                    lever.position = marker.position.ToStandard();
                    lever.direction = (Direction)marker.direction;
                    lever.color = (CableColor)info.data[i + 1].data;
                    int roomIndex = info.data[i + 2].data;
                    if (roomIndex >= 0 && roomIndex < data.rooms.Count)
                        lever.room = data.rooms[roomIndex];
                    structure.powerLevers.Add(lever);
                    i += 3;
                    break;
                case 2: // Breaker
                    var breaker = structure.CreateBreaker();
                    breaker.position = marker.position.ToStandard();
                    breaker.direction = (Direction)marker.direction;
                    structure.breakers.Add(breaker);
                    i++;
                    break;
                default: i++; break; // Failsafe
            }
        }
        return structure;
    }

    private static ShapeLockStructureLocation ConvertShapeLockStructure(StructureInfo info, EditorLevelData data)
    {
        var structure = new ShapeLockStructureLocation { type = info.type };
        foreach (var entry in info.data)
        {
            int roomIndex = entry.data;
            string lockType = entry.prefab;
            if (roomIndex >= 0 && roomIndex < data.rooms.Count)
                structure.CreateAndAddRoom(lockType, data.rooms[roomIndex]);
        }
        return structure;
    }

    private static SteamValveStructureLocation ConvertSteamValveStructure(StructureInfo info)
    {
        var structure = new SteamValveStructureLocation { type = info.type };
        if (info.data.Count == 0) return structure;

        structure.startOnChance = (byte)info.data[0].data;
        for (int i = 1; i < info.data.Count; i += 2)
        {
            var valveData = info.data[i];
            var buttonData = info.data[i + 1];
            var valve = structure.CreateValve();
            valve.position = valveData.position.ToStandard();
            valve.strength = (byte)valveData.data;
            valve.valve.position = buttonData.position.ToStandard();
            valve.valve.direction = (Direction)buttonData.direction;
            structure.valves.Add(valve);
        }
        return structure;
    }

    private static VentStructureLocation ConvertVentStructure(StructureInfo info)
    {
        var structure = new VentStructureLocation { type = info.type };
        if (info.data.Count == 0) return structure;

        var currentVent = structure.CreateVentLocation();
        currentVent.direction = (Direction)info.data[0].direction;

        foreach (var data in info.data)
        {
            currentVent.positions.Add(data.position.ToStandard());
            if (data.data == 1) // End of a vent path
            {
                structure.ventLocations.Add(currentVent);
                currentVent = structure.CreateVentLocation();
                int nextIndex = info.data.IndexOf(data) + 1;
                if (nextIndex < info.data.Count)
                    currentVent.direction = (Direction)info.data[nextIndex].direction;
            }
        }
        return structure;
    }
    #endregion
}