using BaldiLevelEditor;
using CBLDtoBLD.Services;
using PlusLevelFormat;
using PlusLevelLoader;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioLevelFormat;
using UnityEngine;

namespace CBLDtoBLD;

internal static class Converters
{
	#region RBPLtoEBPL
	public static EditorFileContainer ConvertRBPLtoEBPLFormat(this BaldiRoomAsset roomAsset)
	{
		var fileContainer = new EditorFileContainer();
		ConsoleHelper.LogInfo("Converting RBPL room asset to EBPL format...");

		const string editorMode = "rooms";

		// 1. Determine map size from the room's cells to create a fitting canvas.
		ConsoleHelper.LogConverterInfo("Calculating map size from room cells...");
		int maxX = 0;
		int maxY = 0;
		if (roomAsset.cells.Count > 0)
		{
			maxX = roomAsset.cells.Max(c => c.position.x);
			maxY = roomAsset.cells.Max(c => c.position.y);
		}
		// Add a small buffer to ensure the entire room fits comfortably.
		var mapSize = new IntVector2(maxX + 2, maxY + 2);
		ConsoleHelper.LogConverterInfo($"Calculated map size: {mapSize.x}x{mapSize.z}");

		// 2. Initialize EditorLevelData with the calculated size and default metadata.
		var newData = new EditorLevelData(mapSize)
		{
			elevatorTitle = "WIP",
			skybox = "daystandard", // A sensible default
			meta = new PlayableLevelMeta()
			{
				name = roomAsset.name ?? $"Converted_{roomAsset.type}",
				author = "RBPL Converted",
				gameMode = "standard",
				contentPackage = new EditorCustomContentPackage(true)
			},
		};

		// 3. Set up rooms. The hall (ID 1) is created by default. We add our new room, which will get ID 2.
		ConsoleHelper.LogConverterInfo($"Setting up room of type \'{roomAsset.type}\'..");
		var newRoom = new EditorRoom(roomAsset.type, new PlusStudioLevelFormat.TextureContainer()
		{
			wall = UpdateOldTextureName(roomAsset.textureContainer.wall),
			ceiling = UpdateOldTextureName(roomAsset.textureContainer.ceiling),
			floor = UpdateOldTextureName(roomAsset.textureContainer.floor)
		});

		newData.rooms.Add(newRoom);
		ushort newRoomId = newData.IdFromRoom(newRoom);

		// 4. Handle the room's activity, if it exists.
		if (roomAsset.activity != null)
		{
			ConsoleHelper.LogConverterInfo($"Adding activity: {roomAsset.activity.type}");
			var activity = new ActivityLocation()
			{
				type = roomAsset.activity.type,
				position = new Vector3(roomAsset.activity.position.x, roomAsset.activity.position.y, roomAsset.activity.position.z),
				direction = (Direction)roomAsset.activity.direction
			};
			activity.Setup(newRoom); // Assigns the activity to the room.
		}

		// 5. Create efficient CellAreas using a greedy rectangular decomposition algorithm.
		ConsoleHelper.LogConverterInfo("Creating areas...");
		// Create a grid representing the room's shape for easy lookup.
		bool[,] roomShape = new bool[mapSize.x, mapSize.z];
		foreach (var cell in roomAsset.cells)
		{
			if (cell.position.x < mapSize.x && cell.position.y < mapSize.z)
			{
				roomShape[cell.position.x, cell.position.y] = true;
			}
		}

		bool[,] accessedTiles = new bool[mapSize.x, mapSize.z];

		while (true)
		{
			int ogX = -1;
			int ogY = -1;
			bool foundStart = false;

			// Find the first un-accessed tile belonging to the room.
			for (int x = 0; x < mapSize.x && !foundStart; x++)
			{
				for (int y = 0; y < mapSize.z && !foundStart; y++)
				{
					if (roomShape[x, y] && !accessedTiles[x, y])
					{
						ogX = x;
						ogY = y;
						foundStart = true;
					}
				}
			}

			if (!foundStart)
			{
				// No more un-accessed tiles found
				break;
			}

			// Expand downwards to find the maximum possible height for a rectangle starting at (ogX, ogY).
			int height = 1;
			for (int y = ogY + 1; y < mapSize.z; y++)
			{
				if (roomShape[ogX, y] && !accessedTiles[ogX, y])
				{
					height++;
				}
				else
				{
					break;
				}
			}

			// Expand rightwards to find the maximum width for the rectangle of found height.
			int width = 1;
			for (int x = ogX + 1; x < mapSize.x; x++)
			{
				bool canExpand = true;
				for (int y = ogY; y < ogY + height; y++)
				{
					// Ensure the cell is within bounds before checking
					if (y >= mapSize.z || !roomShape[x, y] || accessedTiles[x, y])
					{
						canExpand = false;
						break;
					}
				}

				if (canExpand)
					width++;
				else
					break;

			}

			// Create the area and mark its tiles as accessed.
			var areaOrigin = new IntVector2(ogX, ogY);
			var areaSize = new IntVector2(width, height);
			newData.areas.Add(new RectCellArea(areaOrigin, areaSize, newRoomId));

			for (int x = ogX; x < ogX + width; x++)
			{
				for (int y = ogY; y < ogY + height; y++)
				{
					accessedTiles[x, y] = true;
				}
			}
		}
		ConsoleHelper.LogConverterInfo($"{newData.areas.Count} cell areas created.");

		// 6. Convert standard placements like items, lights, and posters.
		ConsoleHelper.LogConverterInfo("Converting items...");
		foreach (var item in roomAsset.items)
		{
			newData.items.Add(new ItemPlacement() { item = item.item, position = new Vector2(item.position.x, item.position.y) });
		}
		ConsoleHelper.LogConverterInfo($"{newData.items.Count} items created.");
		ConsoleHelper.LogConverterInfo("Converting item spawns...");
		foreach (var spawn in roomAsset.itemSpawns)
		{
			newData.itemSpawns.Add(new ItemSpawnPlacement() { weight = spawn.weight, position = new Vector2(spawn.position.x, spawn.position.y) });
		}
		ConsoleHelper.LogConverterInfo($"{newData.itemSpawns.Count} item spawns created.");
		ConsoleHelper.LogConverterInfo("Converting light objects...");
		foreach (var light in roomAsset.lights)
		{
			// All lights from a room asset are assigned to the default light group (0).
			newData.lights.Add(new LightPlacement() { type = light.prefab, position = light.position.ToInt(), lightGroup = 0 });
		}
		ConsoleHelper.LogConverterInfo($"{newData.lights.Count} light objects created.");
		ConsoleHelper.LogConverterInfo("Converting posters...");
		foreach (var poster in roomAsset.posters)
		{
			newData.posters.Add(new PosterPlacement() { type = poster.poster, position = poster.position.ToInt(), direction = (Direction)poster.direction });
		}
		ConsoleHelper.LogConverterInfo($"{newData.posters.Count} posters created.");

		// 7. Convert technical data from lists (e.g., potential doors) into Structure markers.
		ConsoleHelper.LogConverterInfo("Converting technical data from lists into markers...");
		foreach (var pos in roomAsset.potentialDoorPositions)
		{
			newData.structures.Add(new PotentialDoorLocation() { position = pos.ToInt(), type = "technical_potentialdoor" });
		}
		foreach (var pos in roomAsset.forcedDoorPositions)
		{
			newData.structures.Add(new ForcedDoorLocation() { position = pos.ToInt(), type = "technical_forceddoor" });
		}
		foreach (var pos in roomAsset.standardLightCells)
		{
			newData.structures.Add(new RoomLightLocation() { position = pos.ToInt(), type = "technical_lightspot" });
		}
		ConsoleHelper.LogConverterInfo($"{roomAsset.potentialDoorPositions.Count} potential door position markers created.");
		ConsoleHelper.LogConverterInfo($"{roomAsset.forcedDoorPositions.Count} forced door position markers created.");
		ConsoleHelper.LogConverterInfo($"{roomAsset.standardLightCells.Count} light markers created.");

		// 8. Add UnsafeCellLocation markers for any cell not marked as safe for entities or events.
		ConsoleHelper.LogConverterInfo("Marking unsafe cells...");
		var entitySafePositions = new HashSet<IntVector2>(roomAsset.entitySafeCells.Select(p => p.ToInt()));
		var eventSafePositions = new HashSet<IntVector2>(roomAsset.eventSafeCells.Select(p => p.ToInt()));
		int counter = 0;
		foreach (var cell in roomAsset.cells)
		{
			var pos = cell.position.ToInt();
			if (!entitySafePositions.Contains(pos) && !eventSafePositions.Contains(pos))
			{
				newData.structures.Add(new UnsafeCellLocation() { position = pos, type = "technical_nosafe" });
				counter++;
			}
		}

		ConsoleHelper.LogConverterInfo($"{counter} unsafe cell markers created.");

		// 9. Process basic objects, converting special marker objects into their corresponding structures or placements.
		counter = 0;
		ConsoleHelper.LogConverterInfo("Processing objects and converting special markers...");
		foreach (var obj in roomAsset.basicObjects)
		{
			var objPos = new Vector3(obj.position.x, obj.position.y, obj.position.z);
			var cellPos = new IntVector2(Mathf.RoundToInt((objPos.x - 5f) / 10f), Mathf.RoundToInt((objPos.z - 5f) / 10f));

			bool isMarker = true;
			switch (obj.prefab)
			{
				case "potentialDoorMarker":
					newData.structures.Add(new PotentialDoorLocation() { position = cellPos, type = "technical_potentialdoor" });
					break;
				case "forcedDoorMarker":
					newData.structures.Add(new ForcedDoorLocation() { position = cellPos, type = "technical_forceddoor" });
					break;
				case "itemSpawnMarker":
					// This marker becomes an ItemSpawnPlacement, not a structure, with a default weight.
					newData.itemSpawns.Add(new ItemSpawnPlacement() { weight = 100, position = new Vector2(objPos.x, objPos.z) });
					break;
				case "nonSafeCellMarker":
					newData.structures.Add(new UnsafeCellLocation() { position = cellPos, type = "technical_nosafe" });
					break;
				case "lightSpotMarker":
					newData.structures.Add(new RoomLightLocation() { position = cellPos, type = "technical_lightspot" });
					break;
				default:
					isMarker = false;
					break;
			}

			if (!isMarker)
			{
				// This is a regular object, so add it to the objects list.
				newData.objects.Add(new BasicObjectLocation()
				{
					prefab = UpdateOldObjectNames(obj.prefab),
					position = objPos,
					rotation = new Quaternion(obj.rotation.x, obj.rotation.y, obj.rotation.z, obj.rotation.w)
				});
			}
			else counter++;
		}

		newData.minLightColor = Color.white; // Already adjusts to white
		ConsoleHelper.LogConverterInfo($"Processed {newData.objects.Count} objects and {newData.structures.Count} structures.");
		if (counter != 0)
			ConsoleHelper.LogConverterInfo($"{counter} objects were detected as legacy markers and were properly replaced.");
		ConsoleHelper.LogInfo("Conversion completed!");

		// file container meta stuff
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
		fileContainer.data = newData;

		return fileContainer;
	}
	#endregion
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

		// 3. Convert default texture mappings.
		ConsoleHelper.LogConverterInfo("Mapping default textures...");
		newData.defaultTextures.Clear();
		foreach (var kvp in level.defaultTextures)
		{
			ConsoleHelper.LogConverterInfo($"{kvp.Key} => Floor: {kvp.Value.floor} | Wall: {kvp.Value.wall} | Ceiling: {kvp.Value.ceiling}");
			newData.defaultTextures.Add(kvp.Key, new PlusStudioLevelFormat.TextureContainer(kvp.Value.floor, kvp.Value.wall, kvp.Value.ceiling));
		}
		ConsoleHelper.LogConverterInfo($"{newData.defaultTextures.Count} default textures defined in total!");

		// 4. Convert rooms. The order must be preserved to maintain correct Room IDs.
		ConsoleHelper.LogConverterInfo("Converting rooms from BLD file...");
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
		ConsoleHelper.LogConverterInfo($"{newData.rooms.Count} rooms loaded in total!");

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

		ConsoleHelper.LogConverterInfo($"{newData.areas.Count} areas loaded in total!");

		// 6. Convert all placements (doors, windows, objects, etc.).

		// Doors
		ConsoleHelper.LogConverterInfo("Converting doors...");
		foreach (var oldDoor in level.doors)
		{
			newData.doors.Add(new PlusLevelStudio.Editor.DoorLocation()
			{
				type = UpdateOldDoorName(oldDoor.type),
				position = new IntVector2(oldDoor.position.x, oldDoor.position.y),
				direction = (Direction)oldDoor.direction
			});
		}
		ConsoleHelper.LogConverterInfo($"{newData.doors.Count} doors loaded in total!");


		// Windows
		ConsoleHelper.LogConverterInfo("Converting windows...");
		foreach (var oldWindow in level.windows)
		{
			newData.windows.Add(new PlusLevelStudio.Editor.WindowLocation()
			{
				type = oldWindow.type,
				position = new IntVector2(oldWindow.position.x, oldWindow.position.y),
				direction = (Direction)oldWindow.direction
			});
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
			newData.objects.Add(new BasicObjectLocation()
			{
				prefab = UpdateOldObjectNames(oldPrefab.prefab),
				position = new Vector3(oldPrefab.position.x, oldPrefab.position.y, oldPrefab.position.z),
				rotation = new Quaternion(oldPrefab.rotation.x, oldPrefab.rotation.y, oldPrefab.rotation.z, oldPrefab.rotation.w)
			});
		}
		ConsoleHelper.LogConverterInfo($"{newData.objects.Count} objects loaded in total!");


		// Items
		ConsoleHelper.LogConverterInfo("Converting items...");
		foreach (var oldItem in level.items)
		{
			newData.items.Add(new ItemPlacement()
			{
				item = oldItem.item,
				position = new Vector2(oldItem.position.x, oldItem.position.z)
			});
		}
		ConsoleHelper.LogConverterInfo($"{newData.items.Count} items loaded in total!");


		// Exits and determine spawn point
		ConsoleHelper.LogConverterInfo("Converting exits...");
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
			ConsoleHelper.LogInfo($"Checking room {currentRoomId} ({roomProperties.type})...");

			var roomAsset = new BaldiRoomAsset
			{
				name = roomProperties.type,
				type = roomProperties.type,
				textureContainer = new PlusStudioLevelFormat.TextureContainer(roomProperties.textures.floor, roomProperties.textures.wall, roomProperties.textures.ceiling),
				windowType = "standard" // Default value
			};

			// Gather all cells and data for this room
			ConsoleHelper.LogInfo("Gathering all the cells related to the room...");
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

			ConsoleHelper.LogInfo($"Detected {cells.Count} in total\n{roomAsset.entitySafeCells.Count} entity-safe cells\n{roomAsset.eventSafeCells.Count} event-safe cells\n{roomAsset.secretCells.Count} secret cells");

			ConsoleHelper.LogInfo("Calculating cell offset for room...");

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

			ConsoleHelper.LogInfo("Converting world objects...");

			// Convert and offset Basic Objects (Prefabs)
			foreach (var prefab in roomProperties.prefabs)
			{
				roomAsset.basicObjects.Add(new BasicObjectInfo
				{
					prefab = prefab.prefab,
					position = new PlusStudioLevelFormat.UnityVector3(
						prefab.position.x - (offsetX * 10f),
						prefab.position.y,
						prefab.position.z - (offsetY * 10f)),
					rotation = new(prefab.rotation.x, prefab.rotation.y, prefab.rotation.z, prefab.rotation.w)
				});
			}

			ConsoleHelper.LogInfo($"{roomAsset.basicObjects.Count} objects added!");

			ConsoleHelper.LogInfo("Converting items...");
			// Convert and offset Items
			foreach (var item in roomProperties.items)
			{
				roomAsset.items.Add(new ItemInfo
				{
					item = item.item,
					position = new UnityVector2( // Note: converting from Vector3 to Vector2
						item.position.x - (offsetX * 10f),
						item.position.z - (offsetY * 10f))
				});
			}
			ConsoleHelper.LogInfo($"{roomAsset.items.Count} items added!");

			ConsoleHelper.LogInfo("Converting light objects...");

			// Find and offset lights belonging to this room
			foreach (var light in level.lights)
			{
				if (originalOwnedCells.Any(c => c.x == light.position.x && c.y == light.position.y))
				{
					roomAsset.lights.Add(new LightInfo
					{
						prefab = light.type,
						color = new(light.color.r, light.color.g, light.color.b, light.color.a),
						strength = light.strength,
						position = new PlusStudioLevelFormat.ByteVector2((byte)(light.position.x - offsetX), (byte)(light.position.y - offsetY))
					});
				}
			}

			ConsoleHelper.LogInfo($"{roomAsset.lights.Count} light objects added!");
			// CBLDs were never able of having posters, but I'll leave this in here just in case I need it later
			// ConsoleHelper.LogInfo("Converting posters");

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

			// ConsoleHelper.LogInfo($"{roomAsset.posters.Count} posters added!");

			// Convert and offset Activity
			if (roomProperties.activity != null)
			{
				ConsoleHelper.LogInfo("Converting activity...");
				roomAsset.activity = new ActivityInfo
				{
					type = roomProperties.activity.activity,
					direction = (PlusStudioLevelFormat.PlusDirection)(int)roomProperties.activity.direction,
					position = new PlusStudioLevelFormat.UnityVector3(
						roomProperties.activity.position.x - (offsetX * 10f),
						roomProperties.activity.position.y,
						roomProperties.activity.position.z - (offsetY * 10f))
				};
				ConsoleHelper.LogInfo($"\'{roomAsset.activity.type}\' activity added!");
			}
			else
				ConsoleHelper.LogInfo("The room has no activity available.");

			// Generate a unique name for the asset file
			roomAsset.name += "_" + i + "_" + roomAsset.cells.Count + "_" + (roomAsset.activity == null ? "null" : roomAsset.activity.type);
			roomAssets.Add(roomAsset);
			ConsoleHelper.LogConverterInfo($"Converted room \'{roomProperties.type}\' (ID: {currentRoomId}) into an RBPL asset.");
		}

		return roomAssets;
	}
	#endregion

	#region CBLDTOBLD
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

		ConsoleHelper.LogConverterInfo($"{newLevel.areas.Count} areas created in total!");

		return newLevel;
	}

	#endregion

	static string UpdateOldObjectNames(string obj) => obj switch
	{
		"examination" => "examinationtable",
		"cabinettall" => "cabinet",
		_ => obj
	};

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

	static List<PlusLevelFormat.PlusDirection> DirsFromTile(this PlusLevelFormat.Tile t)
	{
		List<PlusLevelFormat.PlusDirection> list = [];
		for (int i = 1; i <= 4; i++)
			if (IsBitSet(t.walls, i))
				list.Add((PlusLevelFormat.PlusDirection)i);

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
