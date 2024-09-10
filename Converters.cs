using BaldiLevelEditor;
using PlusLevelFormat;
using PlusLevelLoader;

namespace CBLDtoBLD;
using Console = System.Console;

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

			var newHall = newLevel.rooms.Find(x => x.type == "hall");
			if (newHall != null)
			{
				newLevel.RemoveRoomInternal(newLevel._hallRoom);
				newLevel._hallRoom = newHall;
			}
		}

		Console.WriteLine("Initializing tile data...");

		foreach (var tile in level.tiles)
		{
			int x = tile.position.x;
			int y = tile.position.y;
			foreach (var dir in tile.DirsFromTile())
			{
				var vec = dir.ToByteVector2();
				x += vec.x;
				y += vec.y;

				if (level.tiles.InBounds(x, y) && level.tiles[x, y].roomId == tile.roomId)
				{
					newLevel.manualWalls.Add(new() { direction = dir, position = new(x, y) }); // converts to int which is equal to the PlusDirection
					level.tiles[x, y].walls = level.tiles[x, y].walls.ToggleBit((int)dir);
				}
			}
		}

		Console.WriteLine("Initializing elevator areas...");

		foreach (var elevator in level.elevators)
			newLevel.elevatorAreas.Add(new(elevator.position, 1, elevator.direction.ToStandard()), elevator);


		Console.WriteLine("Initializing general areas...");
		// Area detection algorithm here
		bool[,] accessedTiles = new bool[level.tiles.GetLength(0), level.tiles.GetLength(1)];
		while (true)
		{
			int ogX = 0;
			int ogY = 0;
			int id = -1;
			bool flag = false;

			for (; ogX < level.tiles.GetLength(0); ogX++)
			{
				for (; ogY < level.tiles.GetLength(1); ogY++)
				{
					if (level.tiles[ogX, ogY].IsValid() && !accessedTiles[ogX, ogY])
					{
						id = level.tiles[ogX, ogY].roomId;
						accessedTiles[ogX, ogY] = true;
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

			int bigY = 0;

			for (int y = ogY; y < level.tiles.GetLength(1); y++)
			{
				if (!accessedTiles[ogX, ogY] && level.tiles[ogX, y].IsValid(id))
				{
					bigY = y;
					accessedTiles[ogX, ogY] = true;
				}
				else break;	
			}

			if (bigY < ogY)
				continue; // Just to be sure..
			

			for (int x = ogX; x < level.tiles.GetLength(0); x++)
			{
				for (int y = ogY; y < bigY; y++)
				{

				}
			}

		}



		Console.WriteLine("EditorLevel created with success!");

		return newLevel;
	}

	static List<PlusDirection> DirsFromTile(this PlusLevelFormat.Tile t)
	{
		List<PlusDirection> list = [];
		for (int i = 0; i < 4; i++)
			if ((t.type & (1 << i)) > 0)
				list.Add((PlusDirection)i);

		return list;
	}

	static ByteVector2 ToByteVector2(this PlusDirection dir) => dir switch
	{
		PlusDirection.North => new(0, 1),
		PlusDirection.West => new(-1, 0),
		PlusDirection.East => new(1, 0),
		PlusDirection.South => new(0, -1),
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
}
