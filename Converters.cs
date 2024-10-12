using BaldiLevelEditor;
using PlusLevelFormat;
using PlusLevelLoader;

namespace CBLDtoBLD;

internal static class Converters
{
	public static EditorLevel ConvertToEditor(this Level level)
	{
		EditorLevel newLevel = new(level.width, level.height)
		{
			blockedWalls = level.blockedWalls,
			buttons = level.buttons,
			doors = level.doors,
			elevators = level.elevators,
			entitySafeTiles = level.entitySafeTiles,
			eventSafeTiles = level.eventSafeTiles,
			rooms = level.rooms,
			npcSpawns = level.npcSpawns,
			tiledPrefabs = level.tiledPrefabs,
			tiles = level.tiles,
			windows = level.windows
		};

		Console.WriteLine("Initializing EditorLevel...");
		Console.WriteLine($"Size of level: {level.tiles.GetLength(0)},{level.tiles.GetLength(1)}");

		for (int i = 0; i < level.rooms.Count; i++)
		{
			newLevel.items.AddRange(level.rooms[i].items);
			newLevel.prefabs.AddRange(level.rooms[i].prefabs);
			newLevel.defaultTextures.TryAdd(level.rooms[i].type, level.rooms[i].textures);
		}

		Console.WriteLine("Initializing tile data...");

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
                    Console.WriteLine($"Marked wall at ({tile.position.x},{tile.position.y}) as placed in dir: {dir}");
					newLevel.manualWalls.Add(new() { direction = dir, position = new(tile.position.x, tile.position.y) }); // converts to int which is equal to the PlusDirection
					level.tiles[tile.position.x, tile.position.y].walls = level.tiles[tile.position.x, tile.position.y].walls.ToggleBit((int)dir);
				}

				x = tile.position.x;
				y = tile.position.y;
			}
		}


		Console.WriteLine("Initializing elevator areas...");

		foreach (var elevator in level.elevators)
		{
            Console.WriteLine($"Added elevator at ({elevator.position.x},{elevator.position.y}) at direction {elevator.direction}");
			newLevel.elevatorAreas.Add(new(elevator.position, 1, elevator.direction.ToStandard()), elevator);
		}


		Console.WriteLine("Initializing general areas...");
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
			Console.WriteLine($"Area {newLevel.areas.Count} created with size: ({size.x},{size.y}) at pos: ({ogX},{ogY})");

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



		Console.WriteLine("EditorLevel created with success!");

		return newLevel;
	}

	static List<PlusDirection> DirsFromTile(this PlusLevelFormat.Tile t)
	{
		List<PlusDirection> list = [];
		for (int i = 1; i <= 4; i++)
			if (IsBitSet(t.walls, i))
				list.Add((PlusDirection)i);

		return list;
	}

	static (int, int) ToByteVector2(this PlusDirection dir) => dir switch
	{
		PlusDirection.North => (0, 1),
		PlusDirection.West => (-1, 0),
		PlusDirection.East => (1, 0),
		PlusDirection.South => (0, -1),
		_ => new(0, 0)
	};

	static bool IsValid(this PlusLevelFormat.Tile t, int expectedId = -1) =>
		t.type != 16 && (expectedId == -1 || t.roomId == expectedId);

	static bool InBounds<T>(this T[,] vals, int x, int y) =>
		x >= 0 && y >= 0 && x < vals.GetLength(0) && y < vals.GetLength(1);

	static Nybble ToggleBit(this Nybble flag, int position) // Thanks ChatGPT
	{
		// Use XOR to flip the bit at the specified position
		return new(flag ^ (1 << position));
	}

	static bool IsBitSet(Nybble flag, int position) // Thanks ChatGPT
	{
		// Check if the bit at the specified position is set (1)
		return (flag & (1 << position)) != 0;
	}

}
