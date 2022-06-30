namespace RssDetect.Domain;

public class RssConfiguration
{
    public string[] RssPath { get; set; }
    public TimeSpan Timeout { get; set; }
    public string UserAgent { get; set; }

    public RssConfiguration() : this(Array.Empty<string>(), TimeSpan.Zero, string.Empty)
    {
    }

    public RssConfiguration(string[] rssPath, TimeSpan timeout, string userAgent)
    {
        RssPath = rssPath;
        Timeout = timeout;
        UserAgent = userAgent;
    }
}