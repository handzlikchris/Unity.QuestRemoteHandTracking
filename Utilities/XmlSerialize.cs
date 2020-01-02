using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Assets.RemoteHandsTracking.Utilities
{
    public class XmlSerialize
    {
        private static readonly ConcurrentDictionary<Type, XmlSerializer> TypeToCachedSerializer = new ConcurrentDictionary<Type, XmlSerializer>();

        public static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                var stringWriter = new StringWriter();
                using(var writer = XmlWriter.Create(stringWriter))
                {
                    GetSerializer<T>().Serialize(writer, value);
                    return stringWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while serializing (xml)", ex);
            }
        }

        private static XmlSerializer GetSerializer<T>()
        {
            if (!TypeToCachedSerializer.ContainsKey(typeof(T)))
            {
                TypeToCachedSerializer[typeof(T)] = new XmlSerializer(typeof(T));
            }
            return TypeToCachedSerializer[typeof(T)];
        }

        public static T Deserialize<T>(string xml) where T : new()
        {
            var stringReader = new StringReader(xml);
            return (T)GetSerializer<T>().Deserialize(stringReader);
        }
    }

}
