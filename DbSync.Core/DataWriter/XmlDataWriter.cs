using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DbSync.Core.DataWriter
{
    class XmlDataWriter : IDataWriter
    {
        XmlWriter writer { get; }
        Dictionary<string, string> canonicalToActualKeyMap { get; }
        public XmlDataWriter(Table table, JobSettings settings)
        {
            string path = Path.Combine(settings.Path, table.Name + ".xml");
            var xmlSettings = new XmlWriterSettings
            {
                NewLineOnAttributes = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                OmitXmlDeclaration = true,
            };
            writer = XmlWriter.Create(path, xmlSettings);
            writer.WriteStartElement("root");

            canonicalToActualKeyMap = table.Fields.ToDictionary(f => f.CanonicalName, f => f.Name);
        }
        public void Entry(Dictionary<string, object> entry)
        {
            writer.WriteStartElement("row");
            foreach (var keyValPair in entry)
            {
                writer.WriteAttributeString(canonicalToActualKeyMap[keyValPair.Key], keyValPair.Value.ToString());
            }
            writer.WriteEndElement();
        }

        public void Delete(object key) { }
        public void Update(Dictionary<string, object> entry) { }
        public void Add(Dictionary<string, object> entry) { }

        public void Dispose()
        {
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
        }
       
    }
}
