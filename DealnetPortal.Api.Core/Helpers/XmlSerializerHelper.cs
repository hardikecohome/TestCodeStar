using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Core.Helpers
{
    public static class XmlSerializerHelper
    {
        public static T DeserializeFromString<T>(string text)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(text))
            {
                return (T)ser.Deserialize(reader);
            }
        }

        public static async Task<T> DeserializeFromStringAsync<T>(this HttpContent content)
        {
            var contentStr = await content.ReadAsStringAsync();
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(contentStr))
            {
                return (T)ser.Deserialize(reader);
            }
        }

        public static void SerializeToFile<T>(T objToSerialize, string outputFileName)
        {
            var x = new XmlSerializer(objToSerialize.GetType());
            var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
            FileStream fs = new FileStream(outputFileName, FileMode.Create);
            var writer = XmlWriter.Create(fs, settings);
            x.Serialize(writer, objToSerialize);
            writer.Flush();
        }
    }
}
