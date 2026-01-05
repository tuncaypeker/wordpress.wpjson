using Newtonsoft.Json;
using Wordpress.WpJson.Converter;
using System;

namespace Wordpress.WpJson.Model
{
    public class Media
    {
        /// <summary>
        /// bu aslinda resmin path'i
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(HtmlDecodeSerializer))]
        public Render guid { get; set; }

        public Render caption { get; set; }
        public Render description { get; set; }

        public string mime_type { get; set; }
        public string alt_text { get; set; }

        [JsonIgnore]
        public string media_url { get; set; }

        public string code { get; set; }
    }
}
