using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using RssDetect.Domain.Core;
using RssDetect.Domain.Extensions;
using Serilog.Core;

namespace RssDetect.Domain;

public class Rss
{
    private const int HeadEventCount = 1;

    private readonly RssConfiguration _configuration;
    private readonly HttpClient _client;
    private readonly HtmlWeb _htmlWeb;
    private readonly IProgress<DetectProgress>? _progress;
    private readonly Logger? _logger;

    public Rss(IProgress<DetectProgress>? progress, RssConfiguration configuration, Logger? logger)
    {
        _progress = progress;
        _configuration = configuration;
        _logger = logger;
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
        var context = new DetectContext(_client);
        
        var checkUri = rootUri.DescendantUri().ToList();

        var totalEventReports = checkUri.Count * (_configuration.RssPath.Length + HeadEventCount);
        _progress?.Report(new StartDetectProgress(totalEventReports));

        try
        {
            await Parallel.ForEachAsync(checkUri, cancellationToken, async (link, token) => await DetectAsyncCore(link, context, token));
            return context.Results();
        }
        finally
        {
            _progress?.Report(FinishDetectProgress.Instance);   
        }
    }

    private Task DetectAsyncCore(Uri rootLink, DetectContext resultSet, CancellationToken cancellationToken)
    {
        var rssPathTask = HandleRssPath(rootLink, resultSet, cancellationToken);
        var headRssLinkTask = HandleHeadRssLinks(rootLink, resultSet, cancellationToken);
        return Task.WhenAll(rssPathTask, headRssLinkTask);
    }

    private Task HandleRssPath(Uri rootLink, DetectContext resultSet, CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(_configuration.RssPath, cancellationToken, async (typicalRss, token) =>
        {
            await TypicalRssLink(rootLink, typicalRss, resultSet, token);
            _progress?.Report(IncreaseDetectProgress.Instance);
        });
    }

    private async Task HandleHeadRssLinks(Uri rootLink, DetectContext resultSet, CancellationToken cancellationToken)
    {
        var headRssLinks = await HeadRssLinks(rootLink, cancellationToken);
        foreach (var rssLink in headRssLinks)
        {
            try
            {
                await resultSet.AddResultAsync(rssLink, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (Exception e)
            {
                _logger?.Error(e, "HandleHeadRssLinks \"{Uri}\" {RssLink}", rootLink.AbsoluteUri, rssLink.Link);
            }
        }

        _progress?.Report(IncreaseDetectProgress.Instance);
    }

    private async Task TypicalRssLink(Uri link, string typicalRss, DetectContext resultSet, CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(link, typicalRss);
            await resultSet.AddResultAsync(new RssLink(uri), cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch(Exception e)
        {
            _logger?.Error(e, "TypicalRssLink(\"{Uri}\", \"{typicalRss}\")", link.AbsoluteUri, typicalRss);
        }
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

    private sealed class DetectContext
    {
        private readonly HttpClient _client;

        public DetectContext(HttpClient client) => _client = client;

        private ConcurrentSet<RssLink> Result { get; } = new ConcurrentSet<RssLink>();
        private ConcurrentSet<RssLink> VisitLinks { get; } = new ConcurrentSet<RssLink>();

        public ICollection<RssLink> Results() => Result;
        
        public async Task AddResultAsync(RssLink link, CancellationToken cancellationToken)
        {
            if(VisitLinks.Contains(link))
                return;

            MarkVisited(link);

            var stream = await _client.GetStreamAsync(link.Link, cancellationToken);
            var validator = new RssValidator(stream);

            if(validator.Valid())
                Result.Add(link);
        }

        private void MarkVisited(RssLink link)
        {
            VisitLinks.Add(link);
        }
    }
}
