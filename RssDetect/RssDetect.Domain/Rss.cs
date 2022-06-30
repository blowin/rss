using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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
    
    public async IAsyncEnumerable<RssLink> DetectAsync(Uri rootUri, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var excludeUrl = new HashSet<RssLink>();

        foreach (var link in CheckLinks(rootUri))
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(link, Encoding.UTF8, null, cancellationToken);

            if (doc == null) 
                yield break;
            
            var rssLinks = doc.DocumentNode.SelectNodes("/html/head/link")
                .Where(RssHtmlLink)
                .Select(n => RssLink.Create(link, n));

            foreach (var rssLink in rssLinks)
            {
                if (rssLink != null && excludeUrl.Add(rssLink.Value))
                    yield return rssLink.Value;
            }
        
            foreach (var rssLink in TypicalRssPath)
            {
                RssLink? createdLink = null;
                try
                {
                    var uri = new Uri(link, rssLink);
                    if(excludeUrl.Contains(new RssLink(uri)))
                        continue;

                    var result = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri), cancellationToken);
                    if (result.IsSuccessStatusCode)
                        createdLink = new RssLink(uri);
                }
                catch
                {
                    // ignored
                }

                if (createdLink.HasValue)
                    yield return createdLink.Value;
            }
        }
    }

    private static IEnumerable<Uri> CheckLinks(Uri link)
    {
        yield return link;

        var initUri = link.AbsoluteUri ?? string.Empty;
        for (var i = link.Segments.Length - 1; i >= 1; i--)
        {
            var segment = link.Segments[i];
            initUri = initUri.Substring(0, initUri.Length - segment.Length);
            Uri? uri = null;
            try
            {
                uri = new Uri(initUri);
            }
            catch
            {
                // ignore
            }

            if (uri != null)
                yield return uri;
        }
        
    }

    private static bool RssHtmlLink(HtmlNode n)
    {
        return n.Attributes != null && 
               n.Attributes.Any(atr => atr.Name == "type" && atr.Value != null && atr.Value.StartsWith("application/rss")) &&
               n.Attributes.Contains("href");
    }
}