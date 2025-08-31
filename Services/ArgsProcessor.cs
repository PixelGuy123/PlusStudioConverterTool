namespace CBLDtoBLD.Services
{
    internal static class ArgsProcessor
    {
        // Returns a list of input paths (files or directories). If args are empty,
        // prompt the user to enter a path or folder.
        public static List<string> GetInputPaths(TargetType type, params string[] args) =>
            InternalGetInputPaths(args, type.ToExtension());
        static List<string> InternalGetInputPaths(string[] args, string expectedExtension)
        {
            var inputs = new List<string>();

            if (args.Length != 0)
            {
                foreach (var a in args)
                    if (!string.IsNullOrWhiteSpace(a))
                        inputs.Add(a.Trim('"'));

                return inputs;
            }

            Console.WriteLine($"Please input the path to a {expectedExtension} file or a folder containing {expectedExtension} files:");
            while (inputs.Count == 0)
            {
                string? input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                    inputs.Add(input.Trim('"'));
                else
                    Console.WriteLine("Please enter a valid file or folder path:");
            }

            return inputs;
        }
    }
}
