using System.Text;

namespace GptreeParser;

public class BlockExtractor
{
    public void ExtractFiles(string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            Console.WriteLine("Usage: GptreeParser <inputFile> [outputFolder] [--structured]");
            Console.WriteLine("Example: GptreeParser C:\\big.txt C:\\output --structured");
            return;
        }

        var inputFile = args[0];
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File not found -> {inputFile}");
            return;
        }

        var outputBase =
            args.Length >= 2 && !args[1].StartsWith("--")
                ? args[1]
                : Path.Combine(Directory.GetCurrentDirectory(), "parsed");

        var structuredMode = args.Contains("--structured");

        using var reader = new StreamReader(inputFile);
        string? currentRelativePath = null;
        var buffer = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("# File: "))
            {
                if (currentRelativePath != null && buffer.Count > 0)
                {
                    WriteToFile(outputBase, currentRelativePath, buffer, structuredMode);
                }

                currentRelativePath = line.Substring("# File: ".Length).Trim();
                buffer.Clear();
            }
            else if (line == "# END FILE CONTENTS")
            {
                if (currentRelativePath != null && buffer.Count > 0)
                {
                    WriteToFile(outputBase, currentRelativePath, buffer, structuredMode);
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
            WriteToFile(outputBase, currentRelativePath, buffer, structuredMode);
        }
    }

    private static void WriteToFile(
        string outputBase,
        string relativePath,
        List<string> lines,
        bool structured
    )
    {
        var sanitizedName = SanitizePathToFilename(relativePath);

        if (structured)
        {
            var fullDir = Path.Combine(outputBase, Path.GetDirectoryName(relativePath)!);
            Directory.CreateDirectory(fullDir);

            var filename = $"{Path.GetFileNameWithoutExtension(relativePath)}_{sanitizedName}.txt";
            var fullPath = Path.Combine(fullDir, filename);

            File.WriteAllLines(fullPath, lines, Encoding.UTF8);
            Console.WriteLine($"✓ [structured] {fullPath}");
        }
        else
        {
            var filename = $"{sanitizedName}.txt";
            var fullPath = Path.Combine(outputBase, filename);

            Directory.CreateDirectory(outputBase);
            File.WriteAllLines(fullPath, lines, Encoding.UTF8);
            Console.WriteLine($"✓ [flat] {fullPath}");
        }
    }

    private static string SanitizePathToFilename(string path)
    {
        var flattened = path.Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            flattened = flattened.Replace(c, '_');
        }

        return Path.GetFileNameWithoutExtension(flattened);
    }
}
