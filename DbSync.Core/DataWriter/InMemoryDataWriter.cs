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
            PropertyMap = table.Fields.Select(f => f.CanonicalName).Join(typeof(T).GetProperties(), f => f, p => p.Name.ToLower(), (f, p) => new { f, p }).ToDictionary(x => x.f, x => x.p);
        }
        public void Add(Dictionary<string,object> entry){}
        public void Update(Dictionary<string, object> entry){}
        public void Delete(object key){}
        public void SetValue<T>(PropertyInfo property, T entry, object value)
        {
            //Make it handle boolean bit properties correctly
            if (property.PropertyType == typeof(bool) || Nullable.GetUnderlyingType(property.PropertyType) == typeof(bool))
            {
                if (value as string == "0")
                    value = "false";
                else if (value as string == "1")
                    value = "true";
            }
            property.SetValue(entry, Convert.ChangeType(value, property.PropertyType));
        }
        public void Entry(Dictionary<string, object> entry)
        {
            var newEntry = (T)Activator.CreateInstance(typeof(T));
            foreach(var pair in entry)
                SetValue(PropertyMap[pair.Key], newEntry, pair.Value);

            Data.Add(newEntry);
        }

        public void Dispose() { }
    }
}