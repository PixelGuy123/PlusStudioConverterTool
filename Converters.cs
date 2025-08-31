using BaldiLevelEditor;
using CBLDtoBLD.Services;
using PlusLevelFormat;
using PlusLevelLoader;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using UnityEngine;

namespace CBLDtoBLD;

internal static class Converters
{
	public static EditorFileContainer ConvertBLDtoEBPLFormat(this EditorLevel level, bool automaticallyIncludeLighting)
	{
		ConsoleHelper.LogInfo("Converting BLD to EBPL level...");
		const string editorMode = "full";
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

		// 3. Convert default texture mappings.
		ConsoleHelper.LogConverterInfo("Mapping default textures...");
		newData.defaultTextures.Clear();
		foreach (var kvp in level.defaultTextures)
		{
			newData.defaultTextures.Add(kvp.Key, new PlusStudioLevelFormat.TextureContainer(kvp.Value.floor, kvp.Value.wall, kvp.Value.ceiling));
		}

		// 4. Convert rooms. The order must be preserved to maintain correct Room IDs.
		ConsoleHelper.LogConverterInfo("Loading rooms from BLD file...");
		newData.rooms.Clear();
		foreach (var oldRoom in level.rooms)
		{
			var newRoom = new EditorRoom(oldRoom.type, new PlusStudioLevelFormat.TextureContainer(
				UpdateOldTextureName(oldRoom.textures.floor),
				UpdateOldTextureName(oldRoom.textures.wall),
				UpdateOldTextureName(oldRoom.textures.ceiling)
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

		// 5. Convert areas.
		ConsoleHelper.LogConverterInfo("Converting BLD to EBPL areas...");
		newData.areas.Clear();
		List<RectCellArea> newCellAreas = [];
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

		// 6. Convert all placements (doors, windows, objects, etc.).

		// Doors
		ConsoleHelper.LogConverterInfo("Loading doors...");
		foreach (var oldDoor in level.doors)
		{
			newData.doors.Add(new PlusLevelStudio.Editor.DoorLocation()
			{
				type = UpdateOldDoorName(oldDoor.type),
				position = new IntVector2(oldDoor.position.x, oldDoor.position.y),
				direction = (Direction)oldDoor.direction
			});
		}

		// Windows
		ConsoleHelper.LogConverterInfo("Loading windows...");
		foreach (var oldWindow in level.windows)
		{
			newData.windows.Add(new PlusLevelStudio.Editor.WindowLocation()
			{
				type = oldWindow.type,
				position = new IntVector2(oldWindow.position.x, oldWindow.position.y),
				direction = (Direction)oldWindow.direction
			});
		}

		// Manual Walls
		ConsoleHelper.LogConverterInfo("Loading manually placed walls...");
		foreach (var oldWall in level.manualWalls)
		{
			newData.walls.Add(new WallLocation()
			{
				wallState = oldWall.wall,
				position = new IntVector2(oldWall.position.x, oldWall.position.y),
				direction = (Direction)oldWall.direction
			});
		}

		// Prefabs -> Basic Objects
		ConsoleHelper.LogConverterInfo("Loading prefabs...");
		foreach (var oldPrefab in level.prefabs)
		{
			newData.objects.Add(new BasicObjectLocation()
			{
				prefab = UpdateOldObjectNames(oldPrefab.prefab),
				position = new Vector3(oldPrefab.position.x, oldPrefab.position.y, oldPrefab.position.z),
				rotation = new Quaternion(oldPrefab.rotation.x, oldPrefab.rotation.y, oldPrefab.rotation.z, oldPrefab.rotation.w)
			});
		}

		// Items
		ConsoleHelper.LogConverterInfo("Loading items...");
		foreach (var oldItem in level.items)
		{
			newData.items.Add(new ItemPlacement()
			{
				item = oldItem.item,
				position = new Vector2(oldItem.position.x, oldItem.position.z)
			});
		}

		// Exits and determine spawn point
		ConsoleHelper.LogConverterInfo("Loading exits...");
		PlusLevelStudio.Editor.ExitLocation? spawnExit = null;
		foreach (var oldExit in level.exits)
		{
			var newExit = new PlusLevelStudio.Editor.ExitLocation()
			{
				type = oldExit.type,
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

		// Set the level's spawn point based on the 'isSpawn' flag from an exit.
		ConsoleHelper.LogConverterInfo("Converting spawn point...");
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

		// NPC Spawns
		ConsoleHelper.LogConverterInfo("Loading NPCs...");
		foreach (var oldNpc in level.npcSpawns)
		{
			newData.npcs.Add(new NPCPlacement()
			{
				npc = oldNpc.type,
				position = new IntVector2(oldNpc.position.x, oldNpc.position.y),
			});
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

		// ********** INTERNAL METHODS TO CHANGE SOME STRINGS
		static string UpdateOldDoorName(string door) => door switch
		{
			"swing" => "swinging",
			"swingsilent" => "swinging_silent",
			"coin" => "coinswinging",
			_ => door
		};

		static string UpdateOldTextureName(string texName) => texName switch
		{
			"FacultyWall" => "WallWithMolding",
			"Actual" => "TileFloor",
			_ => texName
		};

		static string UpdateOldObjectNames(string obj) => obj switch
		{
			"examination" => "examinationtable",
			"cabinettall" => "cabinet",
			_ => obj
		};

		// Avoids using "Directions" as a non-Unity environment is quite unstable to use things like Quaternions and stuff
		static IntVector2 DirectionToIntVector2(Direction dir) => dir switch
		{
			Direction.North => new IntVector2(0, 1),
			Direction.East => new IntVector2(1, 0),
			Direction.South => new IntVector2(0, -1),
			Direction.West => new IntVector2(-1, 0),
			_ => new IntVector2(0, 0)
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


	public static EditorLevel ConvertCBLDtoBLDFormat(this Level level)
	{
		ConsoleHelper.LogInfo("Converting CBLD to BLD level...");
		EditorLevel newLevel = new(level.width, level.height)
		{
			blockedWalls = level.blockedWalls,
			buttons = level.buttons,
			doors = level.doors,
			entitySafeTiles = level.entitySafeTiles,
			eventSafeTiles = level.eventSafeTiles,
			exits = level.exits,
			rooms = level.rooms,
			npcSpawns = level.npcSpawns,
			tiledPrefabs = level.tiledPrefabs,
			tiles = level.tiles,
			windows = level.windows
		};

		ConsoleHelper.LogConverterInfo("Initializing EditorLevel...");
		ConsoleHelper.LogConverterInfo($"Size of level: {level.tiles.GetLength(0)},{level.tiles.GetLength(1)}");

		for (int i = 0; i < level.rooms.Count; i++)
		{
			newLevel.items.AddRange(level.rooms[i].items);
			newLevel.prefabs.AddRange(level.rooms[i].prefabs);
			newLevel.defaultTextures.TryAdd(level.rooms[i].type, level.rooms[i].textures);
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
					level.tiles[tile.position.x, tile.position.y].walls = level.tiles[tile.position.x, tile.position.y].walls.ToggleBit((int)dir);
				}

				x = tile.position.x;
				y = tile.position.y;
			}
		}


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

			var size = new PlusLevelFormat.ByteVector2(x - ogX, 1 + bigY - ogY);
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

		return newLevel;
	}

	static List<PlusLevelFormat.PlusDirection> DirsFromTile(this PlusLevelFormat.Tile t)
	{
		List<PlusLevelFormat.PlusDirection> list = [];
		for (int i = 1; i <= 4; i++)
			if (IsBitSet(t.walls, i))
				list.Add((PlusLevelFormat.PlusDirection)i);

		return list;
	}

	static (int, int) ToByteVector2(this PlusLevelFormat.PlusDirection dir) => dir switch
	{
		PlusLevelFormat.PlusDirection.North => (0, 1),
		PlusLevelFormat.PlusDirection.West => (-1, 0),
		PlusLevelFormat.PlusDirection.East => (1, 0),
		PlusLevelFormat.PlusDirection.South => (0, -1),
		_ => new(0, 0)
	};

	static bool IsValid(this PlusLevelFormat.Tile t, int expectedId = -1) =>
		t.type != 16 && (expectedId == -1 || t.roomId == expectedId);

	static bool InBounds<T>(this T[,] vals, int x, int y) =>
		x >= 0 && y >= 0 && x < vals.GetLength(0) && y < vals.GetLength(1);

	static PlusLevelFormat.Nybble ToggleBit(this PlusLevelFormat.Nybble flag, int position)
	{
		// Use XOR to flip the bit at the specified position
		return new(flag ^ (1 << position));
	}

	static bool IsBitSet(PlusLevelFormat.Nybble flag, int position)
	{
		// Check if the bit at the specified position is set (1)
		return (flag & (1 << position)) != 0;
	}

}
