using PlusLevelFormat;

namespace CBLDtoBLD;

using Console = System.Console;

static class BLDConverter
{
	static void Main(string[] args)
	{		
		if (args.Length == 0)
		{
			Console.WriteLine("Please, input the path to the .CBLD file you want to convert.");
			while (args.Length == 0)
			{
				string? input = Console.ReadLine();
				if (!string.IsNullOrEmpty(input) && ValidFile(input))
					args = [input];
				else
				{
					Console.Clear();
					Console.WriteLine("Please, input the path to the .CBLD file you want to convert.");
				}
			}
		}
		string file = args[0];

		if (!ValidFile(file))
		{
			Console.BackgroundColor = ConsoleColor.Red;
			Console.WriteLine($"Invalid file detected ({file}). Please input a .cbld file next time.");
			Console.Read();
			return;
		}
		file = Path.GetFullPath(file);
		Console.Clear();

		try
		{
			Console.WriteLine("Reading level..");
			Level level;
			using (var reader = new BinaryReader(File.OpenRead(file)))
			{
				level = LevelExtensions.ReadLevel(reader);
			}

			Console.WriteLine("Converting level...");
			var dir = Path.GetDirectoryName(file);

			if (string.IsNullOrEmpty(dir))
				throw new DirectoryNotFoundException("Directory for the provided path has not been found.");

			string fname = Path.Combine(dir, Path.GetFileNameWithoutExtension(file) + ".bld");

			using (var writer = new BinaryWriter(File.OpenWrite(fname)))
			{
				level.ConvertToEditor().SaveIntoStream(writer);
			}

			LogAtColor(ConsoleColor.Green, $"CBLD file converted as {Path.GetFileName(fname)}");
			Console.WriteLine("Press any key to quit...");
			Console.Read();
		}
		catch (Exception e)
		{
			Console.BackgroundColor = ConsoleColor.Red;
			Console.WriteLine($"Failed to load file ({file}). Please, make sure the file you\'re using is not corrupted or contain invalid data.");
			Console.WriteLine($"Printing exception...\n{e}");
			Console.Read();
			return;
		}
	}

	static bool ValidFile(string path) =>
		File.Exists(path) && Path.GetExtension(path) == ".cbld";

	static void LogAtColor(ConsoleColor color, string content)
	{
		var c = Console.BackgroundColor;
		Console.BackgroundColor = color;
		Console.WriteLine(content);
		Console.BackgroundColor = c;
	}
}