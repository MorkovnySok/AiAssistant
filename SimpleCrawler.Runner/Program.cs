using CommandLine;
using SimpleCrawler;

var parserResult = Parser.Default.ParseArguments<Options>(args);
if (parserResult.Tag == ParserResultType.NotParsed && parserResult.Errors.Any())
{
    Console.WriteLine(string.Join(Environment.NewLine, parserResult.Errors));
}

var options = parserResult.Value;
var crawler = new Crawler(
    options.BaseUrl,
    options.ContentXPath,
    options.AuthToken,
    options.OutputDir
);
await crawler.CrawlAsync();

internal class Options
{
    [Option('u', "url", Required = true, HelpText = "Url to crawl")]
    public required string BaseUrl { get; set; }

    [Option('x', "xpath", Required = true, HelpText = "The xpath to the content")]
    public required string ContentXPath { get; set; }

    [Option(
        't',
        "token",
        Required = false,
        HelpText = "Authentication token or 'login:password' for basic auth"
    )]
    public required string AuthToken { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output file")]
    public required string OutputDir { get; set; }
}
