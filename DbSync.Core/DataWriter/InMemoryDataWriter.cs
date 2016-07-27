using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core.DataWriter
{
    class InMemoryDataWriter<T> : IDataWriter
    {
        public List<T> Data { get; } = new List<T>();
        Dictionary<string,PropertyInfo> PropertyMap { get; }
        public InMemoryDataWriter(Table table)
        {
            Properties = table.Fields.Select(f => f.CanonicalName).Join(typeof(T).GetProperties(), f => f, p => p.Name.ToLower(), (f, p) => p).ToList();
        }
        void Add(Dictionary<string,object> entry){}
        void Update(Dictionary<string, object> entry){}
        void Delete(object key){}
        void Entry(Dictionary<string, object> entry)
        {
            var newEntry = Activator.CreateInstance(typeof(T)) as T;
            foreach(var pair in entry)
            {
                PropertyMap[pair.Key].SetValue(newEntry,pair.Value);
            }
            Data.Add(newEntry);
        }
    }
}