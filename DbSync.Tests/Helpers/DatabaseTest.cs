using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DbSync.Tests.Helpers
{
    public class TempFolder
    {
        public string Path { get; }
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dbsync_test", Guid.NewGuid().ToString());
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
        public void Create()
        {
            var db = new PetaPoco.Database(@"Data Source =.; Database = tempdb; Integrated Security = True", null as string);
            var columns = new List<string>();
            foreach(var property in typeof(T).GetProperties(System.Reflection.BindingFlags.Public))
            {
                var dbType = "";
                var type = property.PropertyType;
                if (type == typeof(int))
                    dbType = "INT NOT NULL";
                else if (type == typeof(string))
                    dbType = "NVARCHAR(MAX) NULL";
                columns.Add($"{property.Name} {dbType}");
            }
            db.Execute($"CREATE TABLE {typeof(T).Name}({string.Join(", ", columns)})");
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
        }
        public void RoundTripCheck()
        {
            Check(LoadedData);
        }
        public void Check(List<T> data)
        {
            var serializer = new XmlSerializer(typeof(List<T>));
            var tempFolder = new TempFolder();
            var file = new TempFile(tempFolder);
            using (var stream = new System.IO.StreamReader(file.Path))
            {
                var list = serializer.Deserialize(stream) as List<T>;
                Assert.AreEqual(data.Count, list.Count);
                for(int i = 0; i < data.Count; i++)
                {
                    Assert.AreEqual(data[i], list[i]);
                }
            }
        }
        public void Dispose()
        {

        }
    }
}
