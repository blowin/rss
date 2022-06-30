using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using HtmlAgilityPack;

namespace RssDetect.Domain;

public class Rss
{
    private static readonly string[] TypicalRssPath = new[]
    {
        "index.xml",
        "index.json",

        "rss.xml",
        "rss.json",
        
        "feed",
    };

    private readonly HttpClient _client = new HttpClient();
    private readonly IProgress<DetectProgress>? _progress;

    public Rss(IProgress<DetectProgress>? progress)
    {
        _progress = progress;
    }

    public async IAsyncEnumerable<RssLink> DetectAsync(Uri rootUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var excludeUrl = new HashSet<RssLink>();

        var checkUri = rootUri.DescendantUri().ToList();
        
        _progress?.Report(new StartDetectProgress(checkUri.Count * TypicalRssPath.Length));
        
        foreach (var link in checkUri)
        {
            await foreach (var p in DetectCoreAsync(link, excludeUrl, cancellationToken)) 
                yield return p;
        }

        _progress?.Report(new FinishDetectProgress());
    }

    private async IAsyncEnumerable<RssLink> DetectCoreAsync(Uri rootLink, HashSet<RssLink> excludeUrl, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var rssLink in HeadRssLinks(rootLink, excludeUrl, cancellationToken))
            yield return rssLink;

        _progress?.Report(new IncreaseDetectProgress());

        foreach (var typicalRss in TypicalRssPath)
        {
            var createdLink = await TypicalRssLink(rootLink, excludeUrl, typicalRss, cancellationToken);
            if (createdLink.HasValue)
                yield return createdLink.Value;

            _progress?.Report(new IncreaseDetectProgress());
        }
    }

    private async Task<RssLink?> TypicalRssLink(Uri link, HashSet<RssLink> excludeUrl, string typicalRss, 
        CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(link, typicalRss);
            if (excludeUrl.Contains(new RssLink(uri)))
                return null;

            var result = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri), cancellationToken);
            
            if (result.IsSuccessStatusCode)
                return new RssLink(uri);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static async IAsyncEnumerable<RssLink> HeadRssLinks(Uri rootLink, HashSet<RssLink> excludeUrl, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(rootLink, Encoding.UTF8, null, cancellationToken);

        if (doc == null)
            yield break;

        var nodes = doc.DocumentNode.SelectNodes("/html/head/link");

        if (nodes == null)
            yield break;

        var rssLinks = nodes.Where(v => v.IsRssHtmlLink()).Select(n => RssLink.Create(rootLink, n));

        foreach (var rssLink in rssLinks)
        {
            if (rssLink != null && excludeUrl.Add(rssLink.Value))
                yield return rssLink.Value;
        }
    }
}
