using BaldiLevelEditor;
using PlusLevelLoader;
using PlusLevelStudio;
using PlusLevelStudio.Editor;
using PlusStudioConverterTool.Models;
using PlusStudioConverterTool.Services;
using PlusStudioLevelFormat;
using UnityEngine;

namespace PlusStudioConverterTool.Converters;

internal static partial class Converters
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
		var newRoom = new EditorRoom(roomAsset.type, new TextureContainer()
		{
			wall = roomAsset.textureContainer.wall,
			ceiling = roomAsset.textureContainer.ceiling,
			floor = roomAsset.textureContainer.floor
		});
		UpdateOldAssetName(ref roomAsset.textureContainer.wall, LevelFieldType.RoomTexture);
		UpdateOldAssetName(ref roomAsset.textureContainer.ceiling, LevelFieldType.RoomTexture);
		UpdateOldAssetName(ref roomAsset.textureContainer.floor, LevelFieldType.RoomTexture);

		newData.rooms.Add(newRoom);
		ushort newRoomId = newData.IdFromRoom(newRoom);

		// 4. Handle the room's activity, if it exists.
		if (roomAsset.activity != null)
		{
			ConsoleHelper.LogConverterInfo($"Adding activity: {roomAsset.activity.type}");
			// Only add activity if its asset name is valid after updates
			string renamedNameAct = roomAsset.activity.type;
			if (UpdateOldAssetName(ref renamedNameAct, LevelFieldType.Activity))
			{
				var activity = new ActivityLocation()
				{
					type = renamedNameAct,
					position = new Vector3(roomAsset.activity.position.x, roomAsset.activity.position.y, roomAsset.activity.position.z),
					direction = (Direction)roomAsset.activity.direction
				};
				activity.Setup(newRoom); // Assigns the activity to the room.
			}
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

		// 5.5. Detect and convert internal "manual" walls.
		ConsoleHelper.LogConverterInfo("Detecting and converting manual walls...");
		var cellPositions = new HashSet<IntVector2>(roomAsset.cells.Select(c => c.position.ToInt()));
		foreach (var cell in roomAsset.cells)
		{
			var currentPos = cell.position.ToInt();
			// Check all four directions
			for (int i = 0; i < 4; i++)
			{
				var dir = (Direction)i;
				// Check if the wall bit is set for this direction
				if ((cell.walls & (1 << i)) != 0)
				{
					var neighborPos = currentPos + dir.ToIntVector2();
					// If the neighbor is also part of the room, this is an internal wall.
					if (cellPositions.Contains(neighborPos))
					{
						newData.walls.Add(new WallLocation
						{
							wallState = true,
							position = currentPos,
							direction = dir
						});
					}
				}
			}
		}
		ConsoleHelper.LogConverterInfo($"{newData.walls.Count} manual walls created.");

		// 6. Convert standard placements like items, lights, and posters.
		ConsoleHelper.LogConverterInfo("Converting items...");
		foreach (var item in roomAsset.items)
		{
			string renamedItem = item.item;
			if (UpdateOldAssetName(ref renamedItem, LevelFieldType.Item))
			{
				newData.items.Add(new ItemPlacement() { item = renamedItem, position = new Vector2(item.position.x, item.position.y) });
			}
		}
		ConsoleHelper.LogConverterInfo($"{newData.items.Count} items created.");
		ConsoleHelper.LogConverterInfo("Converting item spawns...");
		foreach (var spawn in roomAsset.itemSpawns)
		{
			// Item spawns don't reference asset names directly, so keep them
			newData.itemSpawns.Add(new ItemSpawnPlacement() { weight = spawn.weight, position = new Vector2(spawn.position.x, spawn.position.y) });
		}
		ConsoleHelper.LogConverterInfo($"{newData.itemSpawns.Count} item spawns created.");
		ConsoleHelper.LogConverterInfo("Converting light objects...");
		foreach (var light in roomAsset.lights)
		{
			// All lights from a room asset are assigned to the default light group (0).
			string renamedLight = light.prefab;
			if (UpdateOldAssetName(ref renamedLight, LevelFieldType.Light))
			{
				newData.lights.Add(new LightPlacement() { type = renamedLight, position = light.position.ToInt(), lightGroup = 0 });
			}
		}
		ConsoleHelper.LogConverterInfo($"{newData.lights.Count} light objects created.");
		ConsoleHelper.LogConverterInfo("Converting posters...");
		foreach (var poster in roomAsset.posters)
		{
			string renamedPoster = poster.poster;
			if (UpdateOldAssetName(ref renamedPoster, LevelFieldType.Poster))
			{
				newData.posters.Add(new PosterPlacement() { type = renamedPoster, position = poster.position.ToInt(), direction = (Direction)poster.direction });
			}
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
		ConsoleHelper.LogConverterInfo("Processing objects and converting special markers...");
		foreach (var obj in roomAsset.basicObjects)
		{
			// This is a regular object, so add it to the objects list only if the prefab is allowed
			string renamedObj = obj.prefab;
			if (UpdateOldAssetName(ref renamedObj, LevelFieldType.Object))
			{
				newData.objects.Add(new BasicObjectLocation()
				{
					prefab = renamedObj,
					position = new Vector3(obj.position.x, obj.position.y, obj.position.z),
					rotation = new Quaternion(obj.rotation.x, obj.rotation.y, obj.rotation.z, obj.rotation.w)
				});
			}
		}

		newData.minLightColor = Color.white; // Already adjusts to white
		ConsoleHelper.LogConverterInfo($"Processed {newData.objects.Count} objects and {newData.structures.Count} structures.");
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

}
