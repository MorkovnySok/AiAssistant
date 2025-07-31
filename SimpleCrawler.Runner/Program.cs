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
    options.OutputDir,
    options.Verbose,
    options.SingleFileOutput,
    options.LinksXPath
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
    public string? AuthToken { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output file")]
    public required string OutputDir { get; set; }

    [Option('v', "verbose", Required = false, HelpText = "Add verbose logging")]
    public bool Verbose { get; set; }

    [Option(
        's',
        "single-file",
        Required = false,
        HelpText = "Produce all the crawling results as a single file"
    )]
    public bool SingleFileOutput { get; set; }

    [Option(
        "links-xpath",
        Required = false,
        HelpText = "Xpath to search links to parse (specify if you don't want to parse links from the content path)"
    )]
    public string? LinksXPath { get; set; }
}
