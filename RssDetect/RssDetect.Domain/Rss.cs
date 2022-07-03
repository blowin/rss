using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using RssDetect.Domain.Core;
using RssDetect.Domain.Extensions;

namespace RssDetect.Domain;

public class Rss
{
    private const int HeadEventCount = 1;

    private readonly RssConfiguration _configuration;
    private readonly HttpClient _client;
    private readonly HtmlWeb _htmlWeb;
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

        _htmlWeb = new HtmlWeb
        {
            UserAgent = configuration.UserAgent,
        };
    }

    public async Task<ICollection<RssLink>> DetectAsync(Uri rootUri, CancellationToken cancellationToken = default)
    {
        var result = new ConcurrentSet<RssLink>();
        
        var checkUri = rootUri.DescendantUri().ToList();

        var totalEventReports = checkUri.Count * (_configuration.RssPath.Length + HeadEventCount);
        _progress?.Report(new StartDetectProgress(totalEventReports));

        try
        {
            await Parallel.ForEachAsync(checkUri, cancellationToken, async (link, token) => await DetectAsyncCore(link, result, token));
            return result;
        }
        finally
        {
            _progress?.Report(FinishDetectProgress.Instance);   
        }
    }

    private Task DetectAsyncCore(Uri rootLink, ConcurrentSet<RssLink> resultSet, CancellationToken cancellationToken)
    {
        var rssPathTask = HandleRssPath(rootLink, resultSet, cancellationToken);
        var headRssLinkTask = HandleHeadRssLinks(rootLink, resultSet, cancellationToken);
        return Task.WhenAll(rssPathTask, headRssLinkTask);
    }

    private Task HandleRssPath(Uri rootLink, ConcurrentSet<RssLink> resultSet, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(_configuration.RssPath, cancellationToken, async (typicalRss, token) =>
        {
            var createdLink = await TypicalRssLink(rootLink, typicalRss, resultSet, token);
            if (createdLink.HasValue)
                resultSet.Add(createdLink.Value);

            _progress?.Report(IncreaseDetectProgress.Instance);
        });
    }

    private async Task HandleHeadRssLinks(Uri rootLink, ConcurrentSet<RssLink> resultSet, CancellationToken cancellationToken)
    {
        var headRssLinks = await HeadRssLinks(rootLink, cancellationToken);
        foreach (var rssLink in headRssLinks)
            resultSet.Add(rssLink);

        _progress?.Report(IncreaseDetectProgress.Instance);
    }

    private async Task<RssLink?> TypicalRssLink(Uri link, string typicalRss, ConcurrentSet<RssLink> resultSet, CancellationToken token)
    {
        try
        {
            var uri = new Uri(link, typicalRss);
            if (resultSet.Contains(new RssLink(uri)))
                return null;
            
            var result = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri), token);
            
            if (result.IsSuccessStatusCode)
                return new RssLink(uri);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private async Task<IEnumerable<RssLink>> HeadRssLinks(Uri rootLink, CancellationToken token)
    {
        var doc = await _htmlWeb.LoadFromWebAsync(rootLink, Encoding.UTF8, (NetworkCredential)null, token);

        if (doc == null)
            return Enumerable.Empty<RssLink>();

        var nodes = doc.DocumentNode.SelectNodes("/html/head/link");

        if (nodes == null)
            return Enumerable.Empty<RssLink>();

        return nodes.Where(v => v.IsRssHtmlLink())
            .Select(n => RssLink.Create(rootLink, n))
            .Where(v => v.HasValue)
            .Select(v => v!.Value);
    }
}
