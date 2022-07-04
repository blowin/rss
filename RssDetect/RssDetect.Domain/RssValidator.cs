using System.Xml;
using System.Xml.Linq;

namespace RssDetect.Domain;

public sealed class RssValidator
{
    private readonly Lazy<XDocument> _document;

    public RssValidator(Stream stream) : this(new Lazy<XDocument>(() =>
        {
            using var reader = new StreamReader(stream);
            var xml = reader.ReadToEnd();
            return XDocument.Parse(xml);
        }, LazyThreadSafetyMode.None)
    )
    {
    }

    public RssValidator(string xml) : this(new Lazy<XDocument>(() => XDocument.Parse(xml), LazyThreadSafetyMode.None))
    {
    }

    public RssValidator(XDocument document) : this(new Lazy<XDocument>(() => document, LazyThreadSafetyMode.None))
    {
    }

    public RssValidator(Lazy<XDocument> document)
    {
        _document = document;
    }

    public bool Valid()
    {
        try
        {
            var doc = _document.Value;
            return doc?.Root != null;
        }
        catch
        {
            return false;
        }
    }
}