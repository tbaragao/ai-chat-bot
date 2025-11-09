using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ChatBot.Api.Models;
using Microsoft.VisualBasic.CompilerServices;

namespace ChatBot.Api.Services;

public partial class WikipediaService
{
    private static readonly HttpClient WikipediaHttpClient = new HttpClient();

    static WikipediaService()
    {
        WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Clear();
        WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AiChatBot", "1.0"));
    }

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private sealed class WikiApiResponse
    {
        [JsonPropertyName("query")]
        public WikiQuery? Query { get; set; }
    }

    private sealed class WikiQuery
    {
        [JsonPropertyName("pages")]
        public List<WikiPage> Pages { get; set; } = new();
    }

    private sealed class WikiPage
    {
        [JsonPropertyName("pageid")]
        public long? PageId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("extract")]
        public string? Extract { get; set; }

        [JsonPropertyName("missing")]
        public bool? Missing { get; set; }
    }

    static string CreateUrl(string pageTitle, bool fullArticle = false)
    {
        var uriBuilder = new UriBuilder("https://en.wikipedia.org/w/api.php");

        var queryParameters = new Dictionary<string, string>()
        {
            ["action"] = "query",
            ["prop"] = "extracts",
            ["format"] = "json",
            ["formatversion"] = "2",
            ["redirects"] = "1",
            ["explaintext"] = "1",
            ["exsectionformat"] = "wiki",
            ["titles"]= pageTitle,
        };

        if (fullArticle is false)
        {
            queryParameters["exintro"] = "1";
        }
        
        uriBuilder.Query = string.Join("&", queryParameters.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
        
        return uriBuilder.ToString();
    }
    
    static async Task<Documents> GetWikipediaPage(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await WikipediaHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<WikiApiResponse>(json, JsonOptions)
                          ?? throw new InvalidOperationException("Failed to deserialize Wikipedia response.");

        var firstPage = apiResponse.Query?.Pages?.FirstOrDefault();

        if (firstPage is null || firstPage.Missing is true)
            throw new Exception($"Could not find a Wikipedia page for {url}");

        if (string.IsNullOrWhiteSpace(firstPage.Title) || string.IsNullOrWhiteSpace(firstPage.Extract))
            throw new Exception($"Empty Wikipedia page returned for {url}");

        var title = firstPage.Title!;
        var content = firstPage.Extract!.Trim();

        var id = Utils.ToUrlSafeId(title);
        var pageUrl = $"https://en.wikipedia.org/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}";

        return new Documents(
            Id: id,
            Title: title,
            Content: content,
            PageUrl: pageUrl
        );
    }
    
    public Task<Documents> GetWikipediaPageForTitle(string title, bool full = false)
    {
        var url = CreateUrl(title, full);
        return GetWikipediaPage(url);
    }
}

