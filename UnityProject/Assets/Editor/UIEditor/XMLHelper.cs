using System.Xml;

namespace WorkCard.Editor
{
    public static class XMLHelper
    {
        public static XmlDocument Load(string xmlFile)
        {
            var xml = new XmlDocument();
            xml.Load(XmlReader.Create(xmlFile));
            return xml;
        }
    }
}
