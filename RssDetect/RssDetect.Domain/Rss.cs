using System.Runtime.CompilerServices;
using System.Text;
using HtmlAgilityPack;
using RssDetect.Domain.Core;
using RssDetect.Domain.Extensions;

namespace RssDetect.Domain;

public class Rss
{
    private readonly RssConfiguration _configuration;
    private readonly HttpClient _client;
    private readonly IProgress<DetectProgress>? _progress;

    public Rss(IProgress<DetectProgress>? progress, RssConfiguration configuration)
    {
        _progress = progress;
        _configuration = configuration;
        _client = new HttpClient
        {
            Timeout = configuration.Timeout,
            DefaultRequestHeaders =
            {
                { "User-Agent", configuration.UserAgent }
            }
        };
    }

    public async IAsyncEnumerable<RssLink> DetectAsync(Uri rootUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var excludeUrl = new ConcurrentSet<RssLink>();
        
        var checkUri = rootUri.DescendantUri().ToList();
        
        _progress?.Report(new StartDetectProgress(checkUri.Count * _configuration.RssPath.Length + 1));

        try
        {
            foreach (var link in checkUri)
            {
                await foreach (var p in DetectCoreAsync(link, excludeUrl, cancellationToken)) 
                    yield return p;
            }
        }
        finally
        {
            _progress?.Report(new FinishDetectProgress());   
        }
    }

    private async IAsyncEnumerable<RssLink> DetectCoreAsync(Uri rootLink, ConcurrentSet<RssLink> excludeUrl, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var rssLink in HeadRssLinks(rootLink, excludeUrl, cancellationToken))
            yield return rssLink;

        _progress?.Report(new IncreaseDetectProgress());

        foreach (var typicalRss in _configuration.RssPath)
        {
            var createdLink = await TypicalRssLink(rootLink, excludeUrl, typicalRss, cancellationToken);
            if (createdLink.HasValue)
                yield return createdLink.Value;

            _progress?.Report(new IncreaseDetectProgress());
        }
    }

    private async Task<RssLink?> TypicalRssLink(Uri link, ConcurrentSet<RssLink> excludeUrl, string typicalRss, 
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

    private static async IAsyncEnumerable<RssLink> HeadRssLinks(Uri rootLink, ConcurrentSet<RssLink> excludeUrl, [EnumeratorCancellation] CancellationToken cancellationToken)
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
