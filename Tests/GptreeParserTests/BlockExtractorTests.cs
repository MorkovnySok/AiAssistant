using GptreeParser;

namespace GptreeParserTests;

public class BlockExtractorTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(
        Path.GetTempPath(),
        "GptreeTest_" + Guid.NewGuid()
    );

    [Fact]
    public void ExtractFiles_StructuredMode_CreatesExpectedFiles()
    {
        // Arrange
        var inputPath = Path.Combine("TestData", "Test.txt");

        if (Directory.Exists(_outputDir))
            Directory.Delete(_outputDir, recursive: true);

        var args = new[] { inputPath, _outputDir, "--structured" };
        var extractor = new BlockExtractor();

        // Act
        extractor.ExtractFiles(args);

        // Assert
        var outputFiles = Directory.GetFiles(_outputDir, "*.txt", SearchOption.AllDirectories);
        Assert.Equal(2, outputFiles.Length);

        // Construct expected file paths (structured)
        var file1Dir = Path.Combine(_outputDir, @"configuration\UI\ClientAction");
        var file1Name =
            "onConfirmAmendManualPeriodsView_configuration_UI_ClientAction_onConfirmAmendManualPeriodsView.txt";
        var file1Path = Path.Combine(file1Dir, file1Name);

        var file2Dir = Path.Combine(_outputDir, @"configuration\UI\ClientAction");
        var file2Name =
            "createDataSourceEmptyRequest_configuration_UI_ClientAction_createDataSourceEmptyRequest.txt";
        var file2Path = Path.Combine(file2Dir, file2Name);

        Assert.True(File.Exists(file1Path), $"Expected file not found: {file1Path}");
        Assert.True(File.Exists(file2Path), $"Expected file not found: {file2Path}");

        var content1 = File.ReadAllText(file1Path);
        var content2 = File.ReadAllText(file2Path);

        Assert.Contains("onConfirmAmendManualPeriodsView", content1);
        Assert.Contains("createDataSourceEmptyRequest", content2);
    }

    [Fact]
    public void ExtractFiles_ParsesBlocksCorrectly_AndCreatesTxtFiles()
    {
        // Arrange
        var inputPath = Path.Combine("TestData", "Test.txt");
        var outputDir = Path.Combine(Path.GetTempPath(), "GptreeTest_" + Guid.NewGuid());

        // Ensure clean test state
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, recursive: true);
        }

        var args = new[] { inputPath, outputDir };
        var extractor = new BlockExtractor();

        // Act
        extractor.ExtractFiles(args);

        // Assert
        var outputFiles = Directory.GetFiles(outputDir, "*.txt", SearchOption.AllDirectories);
        Assert.Equal(2, outputFiles.Length);

        var file1 = Path.Combine(
            outputDir,
            @"configuration_UI_ClientAction_onConfirmAmendManualPeriodsView.txt"
        );
        var file2 = Path.Combine(
            outputDir,
            @"configuration_UI_ClientAction_createDataSourceEmptyRequest.txt"
        );

        Assert.True(File.Exists(file1), $"Expected file not found: {file1}");
        Assert.True(File.Exists(file2), $"Expected file not found: {file2}");

        var content1 = File.ReadAllText(file1);
        var content2 = File.ReadAllText(file2);

        Assert.Contains("onConfirmAmendManualPeriodsView", content1);
        Assert.Contains("createDataSourceEmptyRequest", content2);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
        {
            try
            {
                Directory.Delete(_outputDir, recursive: true);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Warning: Could not delete output dir: {ex.Message}");
            }
        }
    }
}
