using System.Text;

namespace GptreeParser;

public class BlockExtractor
{
    public void ExtractFiles(string[] args)
    {
        // ==== Argument Handling ====
        if (args.Length is < 1 or > 2)
        {
            Console.WriteLine("Usage: GptreeParser <inputFile> [outputFolder]");
            Console.WriteLine("Example: GptreeParser C:\\big.txt C:\\output");
            return;
        }

        var inputFile = args[0];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File not found -> {inputFile}");
            return;
        }

        var outputBase =
            args.Length == 2 ? args[1] : Path.Combine(Directory.GetCurrentDirectory(), "parsed");

        // ==== Parsing Logic ====
        using var reader = new StreamReader(inputFile);
        string? currentRelativePath = null;
        var buffer = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("# File: "))
            {
                if (currentRelativePath != null && buffer.Count > 0)
                {
                    WriteToFile(outputBase, currentRelativePath, buffer);
                }

                currentRelativePath = line.Substring("# File: ".Length).Trim();
                buffer.Clear();
            }
            else if (line == "# END FILE CONTENTS")
            {
                if (currentRelativePath != null && buffer.Count > 0)
                {
                    WriteToFile(outputBase, currentRelativePath, buffer);
                }

                currentRelativePath = null;
                buffer.Clear();
            }
            else if (!line.StartsWith("# BEGIN FILE CONTENTS"))
            {
                buffer.Add(line);
            }
        }

        if (currentRelativePath != null && buffer.Count > 0)
        {
            WriteToFile(outputBase, currentRelativePath, buffer);
        }

        return;

        static void WriteToFile(string outputBase, string relativePath, List<string> lines)
        {
            // Remove slashes and invalid chars to flatten the path
            var flattenedName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            // Remove any other invalid filename chars (optional but safe)
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                flattenedName = flattenedName.Replace(c, '_');
            }

            // Always .txt extension
            flattenedName = Path.ChangeExtension(flattenedName, ".txt");

            var outputPath = Path.Combine(outputBase, flattenedName);

            Directory.CreateDirectory(outputBase); // ensure output dir exists

            File.WriteAllLines(outputPath, lines, Encoding.UTF8);
            Console.WriteLine($"✓ Wrote: {outputPath}");
        }
    }
}
