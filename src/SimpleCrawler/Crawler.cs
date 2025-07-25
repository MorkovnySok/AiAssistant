﻿using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using HtmlAgilityPack;

namespace SimpleCrawler;

public class Crawler
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    private readonly ConcurrentDictionary<string, bool> _visitedUrls = new();
    private readonly ConcurrentBag<ParseResult> _documents = new();
    private readonly string _xpath;
    private readonly string? _authToken;
    private readonly string? _outputDirectory;
    private const int _maxConcurrency = 8;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public Crawler(
        string baseUrl,
        string xpath,
        string? authToken = null,
        string? outputDirectory = null
    )
    {
        _httpClient = new HttpClient(
            new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            }
        );
        _baseUri = new Uri(baseUrl);
        _xpath = xpath;
        _authToken = authToken;
        _outputDirectory = outputDirectory;

        if (_outputDirectory != null && !Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<List<ParseResult>> CrawlAsync()
    {
        var channel = Channel.CreateUnbounded<string>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        var activeCount = 0;

        void Increment() => Interlocked.Increment(ref activeCount);
        void Decrement()
        {
            if (Interlocked.Decrement(ref activeCount) == 0)
            {
                writer.Complete(); // No more work left — signal end
            }
        }

        Increment(); // Seed URL is one active job
        await writer.WriteAsync(_baseUri.AbsoluteUri);

        var workers = Enumerable
            .Range(0, _maxConcurrency)
            .Select(workerId =>
                Task.Run(async () =>
                {
                    while (await reader.WaitToReadAsync())
                    {
                        while (reader.TryRead(out var url))
                        {
                            try
                            {
                                if (!_visitedUrls.TryAdd(url, true))
                                {
                                    Decrement();
                                    continue;
                                }

                                Console.WriteLine($"[Worker {workerId + 1}] Crawling: {url}");
                                var request = CreateRequest(url);
                                var response = await _httpClient.SendAsync(request);
                                response.EnsureSuccessStatusCode();

                                var html = await response.Content.ReadAsStringAsync();
                                var doc = new HtmlDocument();
                                doc.LoadHtml(html);

                                var contentNode = doc.DocumentNode.SelectSingleNode(_xpath);
                                if (contentNode == null)
                                {
                                    await Console.Error.WriteLineAsync(
                                        "No content node found. Check your xPath."
                                    );
                                    Decrement();
                                    continue;
                                }

                                _documents.Add(
                                    new ParseResult
                                    {
                                        Url = url,
                                        Content = contentNode.InnerText.Trim(),
                                    }
                                );

                                var links = contentNode.SelectNodes("//a[@href]");
                                if (links != null)
                                {
                                    foreach (var link in links)
                                    {
                                        var href = link.GetAttributeValue("href", "");
                                        if (string.IsNullOrWhiteSpace(href))
                                            continue;

                                        var absoluteUrl = new Uri(_baseUri, href).AbsoluteUri;
                                        if (
                                            absoluteUrl.StartsWith(_baseUri.AbsoluteUri)
                                            && !_visitedUrls.ContainsKey(absoluteUrl)
                                            && !absoluteUrl.Contains('#')
                                        )
                                        {
                                            Increment();
                                            await writer.WriteAsync(absoluteUrl);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error crawling {url}: {ex.Message}");
                            }
                            finally
                            {
                                Decrement(); // Job done for this URL
                            }
                        }
                    }
                })
            )
            .ToList();

        await Task.WhenAll(workers);

        if (_outputDirectory != null)
        {
            await SaveResultsAsync();
        }

        return _documents.ToList();
    }

    private HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        );
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
        request.Headers.Add("Connection", "keep-alive");

        if (_authToken != null)
        {
            SetAuthorizationHeader(request, _authToken);
        }
        return request;
    }

    private void SetAuthorizationHeader(HttpRequestMessage request, string token)
    {
        if (token.Contains(':'))
        {
            token = token.Replace("Basic ", string.Empty);
            var data = token.Split(":");
            var username = data[0];
            var password = data[1];
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{username}:{password}")
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
        else
        {
            token = token.Replace("Bearer ", string.Empty);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task SaveResultsAsync()
    {
        var json = JsonSerializer.Serialize(_documents, _options);
        var outputPath = Path.Combine(_outputDirectory!, "crawler_results.json");
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Results saved to {outputPath}");
    }
}
