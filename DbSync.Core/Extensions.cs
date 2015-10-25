using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbSync.Core
{
    public static class Extensions
    {
        public static string FormatWith<T>(this string formatString, T formatObject)
        {
            var resultString = formatString;
            foreach (var property in typeof(T).GetProperties())
            {
                resultString = resultString.Replace("@" + property.Name, property.GetValue(formatObject)?.ToString());
            }
            return resultString;
        }
        public static string AsJson(this object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
        public static T Dump<T>(this T obj)
        {
            string value = "";
            if (typeof(T) == typeof(string))
                value = obj as string;
            else
                value = obj.AsJson();
            Console.WriteLine(value);
            return obj;
        }
    }
}
