using Newtonsoft.Json;
using System;
using System.Net;

namespace Wordpress.WpJson.Converter
{
    public class HtmlDecodeSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
                return null;

            string content = reader.Value.ToString();
            content = WebUtility.HtmlDecode(content);

            return content;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(string).IsAssignableFrom(objectType);
        }
    }
}
