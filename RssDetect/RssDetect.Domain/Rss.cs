using System.Net;
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

    public ICollection<RssLink> Detect(Uri rootUri)
    {
        var result = new ConcurrentSet<RssLink>();
        
        var checkUri = rootUri.DescendantUri().ToList();
        
        _progress?.Report(new StartDetectProgress(checkUri.Count * _configuration.RssPath.Length + 1));

        try
        {
            Parallel.ForEach(checkUri, link => DetectCore(link, result));
            return result;
        }
        finally
        {
            _progress?.Report(new FinishDetectProgress());   
        }
    }

    private void DetectCore(Uri rootLink, ConcurrentSet<RssLink> resultSet)
    {
        Parallel.Invoke(() =>
        {
            foreach (var rssLink in HeadRssLinks(rootLink))
                resultSet.Add(rssLink);

            _progress?.Report(new IncreaseDetectProgress());
        },
        () =>
        {
            Parallel.ForEach(_configuration.RssPath, typicalRss =>
            {
                var createdLink = TypicalRssLink(rootLink, typicalRss, resultSet);
                if (createdLink.HasValue)
                    resultSet.Add(createdLink.Value);

                _progress?.Report(new IncreaseDetectProgress());
            });
        });
    }

    private RssLink? TypicalRssLink(Uri link, string typicalRss, ConcurrentSet<RssLink> resultSet)
    {
        try
        {
            var uri = new Uri(link, typicalRss);
            if (resultSet.Contains(new RssLink(uri)))
                return null;
            
            var result = _client.Send(new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri));
            
            if (result.IsSuccessStatusCode)
                return new RssLink(uri);
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private IEnumerable<RssLink> HeadRssLinks(Uri rootLink)
    {
        var doc = _htmlWeb.Load(rootLink);

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
