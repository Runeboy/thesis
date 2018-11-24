using Airswipe.WinRT.Core.Log;
using Airswipe.WinRT.Core.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Airswipe.WinRT.Core.Data
{
    public class StringExpert
    {
        #region Fields

        private static ILogger log = new TypeLogger<StringExpert>();

        #endregion
        #region Methods

        public static string JoinManyByTab(params object[] strings)
        {
            return JoinByTab(strings.ToList());
        }

        public static string JoinByTab<T>(IEnumerable<T> strings)
        {
            return Join(strings, "\t");
        }

        public static string Join<T>(IEnumerable<T> strings, string token)
        {
            return strings.Count() == 0 ? "" : strings.Select(s => s.ToString()).Aggregate((s1, s2) => s1 + token + s2);
        }

        public static string CommaSeparate<T>(IEnumerable<T> strings)
        {
            return CommaSeparate(strings.Select(s => s.ToString()));
        }

        public static string CommaSeparate(IEnumerable<string> strings)
        {
            return strings.Count() == 0 ? "" : strings.Aggregate((s1, s2) => s1 + "," + s2);
        }

        public static T CloneByJson<T>(T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            //// initialize inner objects individually
            //// for example in default constructor some list property initialized with some values,
            //// but in 'source' these items are cleaned -
            //// without ObjectCreationHandling.Replace default constructor values will be added to result
            //var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            //return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);

            return FromJson<T>(ToJson(source));
        }

        public static T FromJson<T>(string json)
        {
            var serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Converters.Add(new DTOJsonConverter());
            //Interfaces.IEntity entity = serializer.Deserialize(jsonReader);
            var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(json));

            return serializer.Deserialize<T>(reader);

            //return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

        }

        public static string ToJson(object obj)
        {
            string result = Newtonsoft.Json.JsonConvert.SerializeObject(
                obj, Newtonsoft.Json.Formatting.Indented,
                new Newtonsoft.Json.JsonSerializerSettings()
                {
                    //NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                }
                );

            //log.Verbose("Serialized oject type '{0}' to: '{1}'", obj.GetType().FullName, result);

            return result;
            //using (var stream = new MemoryStream())
            //{
            //    new DataContractJsonSerializer(obj.GetType()).WriteObject(stream, obj);
            //    using (var streamReader = new StreamReader(stream))
            //        return streamReader.ReadToEnd();
            //}
        }

        #endregion
    }
}
