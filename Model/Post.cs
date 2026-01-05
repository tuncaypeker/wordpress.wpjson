using Newtonsoft.Json;
using Wordpress.WpJson.Converter;
using System;

namespace Wordpress.WpJson.Model
{
    public class Post
    {
        [JsonProperty(ItemConverterType = typeof(HtmlDecodeSerializer))]
        public Render title { get; set; }

        [JsonProperty(ItemConverterType = typeof(HtmlDecodeSerializer))]
        public Render content { get; set; }

        [JsonProperty(ItemConverterType = typeof(HtmlDecodeSerializer))]
        public Render excerpt { get; set; }

        public string slug { get; set; }
        public DateTime date { get; set; }
        public DateTime modified { get; set; }
        
        /// <summary>
        /// integer deger ama, bazi id'ler cok buyuk olabiliyor
        /// </summary>
        public string featured_media { get; set; }
        public int?[] categories { get; set; }
        public int[] tags { get; set; }
        public string yoast_head { get; set; }

        public string link { get; set; }
        public int author { get; set; }
        public string id { get; set; }
        public string status { get; set; }
        public string type { get; set; }
        public string featured_img { get; set; }
    }
}
