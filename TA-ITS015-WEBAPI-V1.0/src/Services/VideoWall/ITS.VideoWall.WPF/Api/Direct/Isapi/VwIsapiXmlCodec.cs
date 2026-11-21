using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Module.VideoWall.WPF.Api.Direct.Isapi;

/// <summary>
/// Description: Bộ tiện ích tuần tự hóa và giải tuần tự hóa XML chuẩn ISAPI VideoWall
/// Created date: 13/09/2026
/// </summary>
public static class VwIsapiXmlCodec
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        OmitXmlDeclaration = true,
        Indent = true,
        Encoding = new UTF8Encoding(false),
    };

    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        IgnoreWhitespace = true,
    };

    public static string Serialize<T>(T obj)
    {
        if (obj == null) return string.Empty;

        var ns = new XmlSerializerNamespaces();
        ns.Add("", VwDirectDeviceConstants.XmlNamespace);

        var serializer = new XmlSerializer(typeof(T));
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, WriterSettings);

        serializer.Serialize(xmlWriter, obj, ns);
        return stringWriter.ToString();
    }

    public static T? Deserialize<T>(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return default;

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, ReaderSettings);

            return (T?)serializer.Deserialize(xmlReader);
        }
        catch
        {
            return default;
        }
    }
}
