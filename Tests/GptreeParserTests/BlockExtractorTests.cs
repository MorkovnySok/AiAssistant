using GptreeParser;

namespace GptreeParserTests;

public class BlockExtractorTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(
        Path.GetTempPath(),
        "GptreeTest_" + Guid.NewGuid()
    );

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
