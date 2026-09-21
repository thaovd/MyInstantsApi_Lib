using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MyInstantsApi;

/// <summary>
/// Client-side port of https://github.com/abdipr/myinstants-api.
/// Calls myinstants.com directly from the caller's machine instead of
/// through a server, so requests use the end user's own IP.
///
/// Fetches are done by shelling out to curl.exe (bundled with Windows 10
/// 1803+ and Windows 11) rather than HttpClient: myinstants.com sits
/// behind Cloudflare, which blocks HttpClient's TLS fingerprint (JA3)
/// even with a matching User-Agent, while curl's fingerprint passes.
/// </summary>
public class MyInstantsClient
{
    private const string BaseUrl = "https://www.myinstants.com";
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private readonly string _curlPath;

    /// <param name="curlPath">Path to curl executable. Defaults to "curl", resolved via PATH
    /// (Windows 10 1803+/11 ship curl.exe in System32).</param>
    public MyInstantsClient(string curlPath = "curl")
    {
        _curlPath = curlPath;
    }

    public Task<List<Sound>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query is required, example: \"vine boom\"", nameof(query));
        var url = $"{BaseUrl}/en/search/?name={Uri.EscapeDataString(query)}";
        return FetchSoundsAsync(url, ct);
    }

    public Task<List<Sound>> GetRecentAsync(CancellationToken ct = default)
        => FetchSoundsAsync($"{BaseUrl}/en/recent", ct);

    /// <param name="regionCode">Country/category code as used by myinstants.com, e.g. "id".</param>
    public Task<List<Sound>> GetTrendingAsync(string regionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
            throw new ArgumentException("Region code is required, example: \"id\"", nameof(regionCode));
        return FetchSoundsAsync($"{BaseUrl}/en/index/{Uri.EscapeDataString(regionCode)}", ct);
    }

    /// <param name="regionCode">Country/category code as used by myinstants.com, e.g. "id".</param>
    public Task<List<Sound>> GetBestAsync(string regionCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
            throw new ArgumentException("Region code is required, example: \"id\"", nameof(regionCode));
        return FetchSoundsAsync($"{BaseUrl}/en/best_of_all_time/{Uri.EscapeDataString(regionCode)}", ct);
    }

    public Task<List<Sound>> GetFavoritesAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required, example: \"hellmouz\"", nameof(username));
        return FetchSoundsAsync($"{BaseUrl}/en/profile/{Uri.EscapeDataString(username)}", ct);
    }

    public Task<List<Sound>> GetUploadedAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required, example: \"hellmouz\"", nameof(username));
        return FetchSoundsAsync($"{BaseUrl}/en/profile/{Uri.EscapeDataString(username)}/uploaded/", ct);
    }

    public async Task<SoundDetail> GetDetailAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required, example: \"akh-26815\"", nameof(id));

        var html = await FetchHtmlAsync($"{BaseUrl}/en/instant/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
        var doc = html.DocumentNode;

        var titleNode = doc.SelectSingleNode("//h1[@id='instant-page-title']")
            ?? throw new MyInstantsException("Page not found or page layout changed");
        var buttonNode = doc.SelectSingleNode("//button[@id='instant-page-button-element']");
        var descriptionNode = doc.SelectSingleNode("//div[@id='instant-page-description']//p");
        var likesNode = doc.SelectSingleNode("//div[@id='instant-page-likes']");
        var authorNode = doc.SelectSingleNode("//div[@id='instant-page-likes']/following-sibling::div[1]");

        var tags = doc.SelectNodes("//div[@id='instant-page-tags']//a")
            ?.Select(a => Clean(a.InnerText)).ToList() ?? new List<string>();

        var favorites = "";
        var likesB = likesNode?.SelectSingleNode(".//b");
        if (likesB != null)
            favorites = Clean(likesB.InnerText).Replace(" users", "");

        var uploaderName = "";
        var uploaderUrl = "";
        var views = "";
        if (authorNode != null)
        {
            var authorLink = authorNode.SelectSingleNode(".//a");
            if (authorLink != null)
            {
                uploaderName = Clean(authorLink.InnerText);
                uploaderUrl = BaseUrl + authorLink.GetAttributeValue("href", "");
            }
            var viewsText = Clean(authorNode.InnerText).Replace("views", "").Trim();
            views = viewsText.Replace($"Uploaded by {uploaderName} - ", "").Trim();
        }

        return new SoundDetail
        {
            Id = id,
            Url = $"{BaseUrl}/en/instant/{id}",
            Title = Clean(titleNode.InnerText),
            Mp3 = BaseUrl + (buttonNode?.GetAttributeValue("data-url", "") ?? ""),
            Description = descriptionNode != null ? Clean(descriptionNode.InnerText) : "",
            Tags = tags,
            Favorites = favorites,
            Views = views,
            Uploader = new Uploader { Username = uploaderName, Url = uploaderUrl },
        };
    }

    private async Task<List<Sound>> FetchSoundsAsync(string url, CancellationToken ct)
    {
        var html = await FetchHtmlAsync(url, ct).ConfigureAwait(false);
        return ParseSounds(html);
    }

    private static List<Sound> ParseSounds(HtmlDocument html)
    {
        var sounds = new List<Sound>();
        var instantNodes = html.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' instant ')]");
        if (instantNodes is null) return sounds;

        foreach (var instant in instantNodes)
        {
            var link = instant.SelectSingleNode(
                ".//a[contains(concat(' ', normalize-space(@class), ' '), ' instant-link ')]");
            if (link is null) continue;

            var href = link.GetAttributeValue("href", "");
            var id = href.Replace("/en/instant/", "").Trim('/');

            var button = instant.SelectSingleNode(
                ".//button[contains(concat(' ', normalize-space(@class), ' '), ' small-button ')]");
            var onclick = button?.GetAttributeValue("onclick", "") ?? "";
            var match = Regex.Match(onclick, @"play\('(.*?)'");
            if (!match.Success) continue;

            sounds.Add(new Sound
            {
                Id = id,
                Title = Clean(link.InnerText),
                Url = BaseUrl + href,
                Mp3 = BaseUrl + match.Groups[1].Value,
            });
        }
        return sounds;
    }

    private async Task<HtmlDocument> FetchHtmlAsync(string url, CancellationToken ct)
    {
        var body = await RunCurlAsync(url, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(body))
            throw new MyInstantsException("Fetch failed: empty response body");

        var doc = new HtmlDocument();
        doc.LoadHtml(body);
        return doc;
    }

    private async Task<string> RunCurlAsync(string url, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _curlPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-s");
        psi.ArgumentList.Add("-S");
        psi.ArgumentList.Add("-L");
        psi.ArgumentList.Add("--fail");
        psi.ArgumentList.Add("-A");
        psi.ArgumentList.Add(UserAgent);
        psi.ArgumentList.Add(url);

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new MyInstantsException(
                $"Could not launch curl (looked for \"{_curlPath}\"). " +
                "Pass the full path via MyInstantsClient(curlPath: ...) if it's not on PATH.", ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
            throw new MyInstantsException($"Fetch failed (curl exit {process.ExitCode}): {stderr.Trim()}");

        return stdout;
    }

    private static string Clean(string text) => WebUtility.HtmlDecode(text).Trim();
}
