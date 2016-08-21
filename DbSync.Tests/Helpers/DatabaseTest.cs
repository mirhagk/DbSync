using DbSync.Core.Transfers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DbSync.Tests.Helpers
{
    public class TempFolder
    {
        public string Path { get; }
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dbsync_test");
            System.IO.Directory.CreateDirectory(Path);
        }
    }
    public class TempFile
    {
        public string File { get; }
        public TempFolder Folder { get; }
        public string Path => System.IO.Path.Combine(Folder.Path, File);
        public TempFile(TempFolder folder)
        {
            File = Guid.NewGuid().ToString();
        }
    }

    public class DatabaseTest<T>:IDisposable
    {
        List<T> LoadedData { get; set; }
        List<T> InitialData { get; set; }
        PetaPoco.Database Db { get; set; }
        TempFolder Folder { get; set; }
        string Name => typeof(T).Name;
        string FQN => "dbo." + Name;
        string FileName => FQN + ".xml";
        string ConnectionString { get; } = @"Data Source =.;Database=tempdb;Integrated Security=True";
        Core.JobSettings Settings => new Core.JobSettings()
        {
            Tables = new List<Core.Table>
                {
                    new Core.Table {Name = FQN }
                },
            ConnectionString = ConnectionString,
            AuditColumns = new Core.JobSettings.AuditSettings(),
            IgnoreAuditColumnsOnExport = true,
            UseAuditColumnsOnImport = false,
            Path = Folder.Path
        };
        public void Create(string foreignKeyName = null, string foreignTable = null)
        {
            Folder = new TempFolder();
            Db = new PetaPoco.Database(ConnectionString, "SqlServer");
            var columns = new List<string>();
            foreach(var property in typeof(T).GetProperties())
            {
                var dbType = "";
                var type = property.PropertyType;
                if (type == typeof(int))
                    dbType = "INT NOT NULL";
                else if (type == typeof(string))
                    dbType = "NVARCHAR(MAX) NULL";
                columns.Add($"{property.Name} {dbType}");
            }
            Db.Execute($"CREATE TABLE [{typeof(T).Name}]({string.Join(", ", columns)})");
            if (foreignKeyName != null)
            {
                string foreignKeyID = "id";
                Db.Execute($"ALTER TABLE [{typeof(T).Name}]  WITH CHECK ADD CONSTRAINT [FK_{typeof(T).Name}_{foreignTable}_{foreignKeyName}] FOREIGN KEY [{foreignKeyName}] REFERENCES [{foreignTable}]([{foreignKeyID}])");
            }
        }
        public void Initialize()
        {
            Initialize(new List<T>());
        }
        public void Initialize(List<T> data)
        {
            InitialData = data;
        }
        public void Load(List<T> data)
        {
            LoadedData = data;

            var xmlSettings = new XmlWriterSettings
            {
                NewLineOnAttributes = true,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                OmitXmlDeclaration = true,
            };
            var writer = XmlWriter.Create(System.IO.Path.Combine(Folder.Path, FileName), xmlSettings);
            writer.WriteStartElement("root");

            foreach (var item in data)
            {
                writer.WriteStartElement("row");
                foreach (var property in typeof(T).GetProperties())
                    writer.WriteAttributeString(property.Name, property.GetValue(item).ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();


            var errorHandler = new Core.Services.DefaultErrorHandler();
            var transfer = DbSync.Core.Transfers.Importer.Instance;

            transfer.Run(Settings, null, errorHandler);
        }
        public void RoundTripCheck()
        {
            Check(LoadedData);
        }
        [Serializable()]
        [XmlRoot("root")]
        public class XmlFileSchema
        {
            [XmlElement("row")]
            public List<T> root { get; set; }
        }
        public void Check(List<T> data)
        {
            Exporter.Instance.Run(Settings, null, new Core.Services.DefaultErrorHandler());

            var serializer = new XmlSerializer(typeof(XmlFileSchema));

            using (var stream = new System.IO.StreamReader(System.IO.Path.Combine(Folder.Path, FileName)))
            {
                var list = (serializer.Deserialize(stream) as XmlFileSchema).root;


                Assert.AreEqual(data.Count, list.Count);
                for (int i = 0; i < data.Count; i++)
                {
                    Assert.AreEqual(data[i], list[i]);
                }
            }
        }
        public void Dispose()
        {
            Db.Execute($"DROP TABLE [{typeof(T).Name}]");
            Db.Dispose();
        }
    }
}
