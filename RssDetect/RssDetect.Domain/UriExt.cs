namespace RssDetect.Domain;

public static class UriExt
{
    /// <summary>
    /// Example 1:
    /// Uri = test.com/v1/v2/v3
    ///
    /// Result:
    /// [
    ///     test.com/v1/v2/v3,
    ///     test.com/v1/v2,
    ///     test.com/v1,
    ///     test.com
    /// ]
    ///
    /// Example 2:
    /// Uri = test.com
    ///
    /// Result:
    /// [
    ///     test.com
    /// ]
    /// </summary>
    public static IEnumerable<Uri> DescendantUri(this Uri self)
    {
        yield return self;

        var initUri = self.AbsoluteUri ?? string.Empty;
        for (var i = self.Segments.Length - 1; i >= 1; i--)
        {
            var segment = self.Segments[i];
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
}