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

    public class DatabaseTest
    {
        public void Create(string sql)
        {

        }
        public void Initialize(string xml)
        {

        }
        public void Load(string xml)
        {

        }
        public void RoundTripCheck()
        {

        }
        public void Check<T>(List<T> data)
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
    }
}
