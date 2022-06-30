using HtmlAgilityPack;

namespace RssDetect.Domain;

public readonly record struct RssLink : IComparable<RssLink>
{
    public string Link { get; }

    public RssLink(string link)
    {
        Link = (link ?? string.Empty).Trim('/');
    }

    public RssLink(Uri link) : this(link.AbsoluteUri)
    {
    }

    public static RssLink? Create(Uri rootUri, HtmlNode node)
    {
        var href = node.Attributes["href"].Value ?? string.Empty;
        if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var resUri))
            return null;

        if (resUri.IsAbsoluteUri)
            return new RssLink(href);

        try
        {
            var fullUri = new Uri(rootUri, href);
            if (fullUri.IsAbsoluteUri)
                return new RssLink(fullUri.AbsoluteUri);
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public int CompareTo(RssLink other) => string.Compare(Link, other.Link, StringComparison.InvariantCultureIgnoreCase);
}