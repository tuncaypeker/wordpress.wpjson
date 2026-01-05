using Newtonsoft.Json;
using Wordpress.WpJson.Converter;
using System;

namespace Wordpress.WpJson.Model
{
	public class Comment
    {
        [JsonProperty(ItemConverterType = typeof(HtmlDecodeSerializer))]
        public Render content { get; set; }

        public string author_name { get; set; }
        public string id { get; set; }
        public string post { get; set; }
        public string parent { get; set; }
        public string link { get; set; }
        public DateTime date { get; set; }
    }
}
