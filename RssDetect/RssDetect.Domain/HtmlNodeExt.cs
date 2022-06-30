using HtmlAgilityPack;

namespace RssDetect.Domain;

public static class HtmlNodeExt
{
    public static bool IsRssHtmlLink(this HtmlNode self)
    {
        return self.Attributes != null && 
               self.Attributes.Any(atr => atr.Name == "type" && atr.Value != null && atr.Value.StartsWith("application/rss")) &&
               self.Attributes.Contains("href");
    }
}