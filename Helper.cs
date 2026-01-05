namespace Wordpress.WpJson
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Wordpress.WpJson.Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    public class Helper
    {
        private string _basePath;
        private string _baseApiPath;

        /// <summary>
        /// "http://google.com/";
        /// </summary>
        public Helper(string basePath)
        {
            if (!string.IsNullOrEmpty(basePath))
            {
                _basePath = basePath.TrimEnd('/');
                _baseApiPath = _basePath + "/wp-json/wp/v2";
            }
        }

        public bool HasApi()
        {
            var jsonStartStr = "[{\"id\":";
            using (var client = BuildWebClient())
            {
                var pathJson = _baseApiPath + "/posts?per_page=10&page=1";
                var json = "";

                try
                {
                    json = client.DownloadString(pathJson);
                }
                catch (Exception exc)
                {
                    return false;
                }

                //wordpress json'a benziyor
                if (json.Contains(jsonStartStr))
                {
                    var jsonStartIndex = json.IndexOf(jsonStartStr);
                    var jsonStrAfterSplit = json.Substring(jsonStartIndex);

                    return (IsValidJson(jsonStrAfterSplit));
                }

                return false;
            }
        }

        public List<Post> ParseFromTxt(string postsResultTextJson)
        {
            try
            {
                var wpPosts = JsonConvert.DeserializeObject<List<Post>>(postsResultTextJson);

                return wpPosts;
            }
            catch
            {
                return null;
            }

        }

        /// <summary>
        /// bazi sayfalarda pagination calismiyor ?page=1 de ?page=11111 de aynı sonuclari donuyor
        /// bu metodu cagirdigimiz yerde bu detay'a dikkat etmemiz gerekiyor
        /// https://binaryterms.com/wp-json/wp/v2/posts?per_page=100&page=83
        /// </summary>
        /// <param name="page"></param>
        /// <param name="rowCount"></param>
        /// <param name="categoryIds"></param>
        /// <returns></returns>
        public List<Post> GetPosts(int page, int rowCount = 10, string categoryIds = "", int timeout = 100)
        {
            using (var client = BuildWebClient(timeout))
            {
                var path = $"{_baseApiPath}/posts?per_page={rowCount}&page={page}";

                if (!string.IsNullOrEmpty(categoryIds))
                    path = $"{path}&categories={categoryIds}";

                var json = "";
                try
                {
                    json = client.DownloadString(path);
                    if (!IsValidJson(json))
                        return null;
                }
                catch (Exception exc)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(json) || json == "[]")
                    return null;

                if (json.Contains("rest_post_invalid_page_number"))
                    return null;

                json = RemoveUnicodeCharacter(json.Replace("\n", ""));

                var wpPosts = new List<Post>();
                try
                {
                    wpPosts = JsonConvert.DeserializeObject<List<Post>>(json);
                }
                catch (Exception exc)
                {
                    return null;
                }

                return wpPosts;
            }
        }

        public Post GetPostById(string id)
        {
            using (var client = BuildWebClient())
            {
                var path = $"{_baseApiPath}/posts/{id}";
                var json = "";
                try
                {
                    json = client.DownloadString(path);
                    if (string.IsNullOrEmpty(json) || json == "[]")
                        return null;

                    var wpPost = JsonConvert.DeserializeObject<Post>(json);

                    return wpPost;
                }
                catch (Exception exc)
                {
                    return null;
                }
            }
        }

        public Post GetPostBySlug(string slug)
        {
            using (var client = BuildWebClient())
            {
                var path = $"{_baseApiPath}/posts/?slug={slug}";
                var json = "";
                try
                {
                    json = client.DownloadString(path);
                    if (string.IsNullOrEmpty(json) || json == "[]")
                        return null;

                    var wpPosts = JsonConvert.DeserializeObject<List<Post>>(json);

                    return wpPosts.FirstOrDefault();
                }
                catch (Exception exc)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// json cevap vermeyene kadar sitedeki tum commentleri ceker
        /// </summary>
        /// <returns></returns>
        public List<Comment> GetComments(string postId = "")
        {
            var comments = new List<Comment>();

            using (var client = BuildWebClient(10))
            {
                for (int i = 1; i <= int.MaxValue; i++)
                {
                    var path = $"{_baseApiPath}/comments?per_page=100&page={i}";
                    if (!string.IsNullOrEmpty(postId))
                        path = $"{_baseApiPath}/comments?per_page=100&page={i}&post={postId}";

                    var json = "";
                    try
                    {
                        json = client.DownloadString(path);
                    }
                    catch (Exception exc)
                    {
                        return comments;
                    }

                    if (json == "[]")
                        return comments;
                    else if (json.Contains("\n[]"))
                        return comments;
                    else if (json.Contains("\r\n[]"))
                        return comments;

                    json = RemoveUnicodeCharacter(json.Replace("\n", "").Replace("-0001-11-30T00:00:00", "2022-01-24T16:46:01"));

                    try
                    {
                        comments.AddRange(JsonConvert.DeserializeObject<List<Comment>>(json));
                    }
                    catch (Exception exj)
                    {
                        return comments;
                    }

                }
            }

            return comments;
        }

        /// <summary>
        /// json cevap vermeyene kadar sitedeki tum tag'leri ceker
        /// </summary>
        /// <returns></returns>
        public List<Tag> GetTags()
        {
            var tags = new List<Tag>();

            using (WebClient wc = BuildWebClient())
            {
                for (int i = 1; i <= int.MaxValue; i++)
                {
                    var path = $"{_baseApiPath}/tags?per_page=100&page={i}";
                    var json = "";

                    try
                    {
                        json = wc.DownloadString(path);

                        if (string.IsNullOrEmpty(json) || json == "[]")
                            return tags;

                        json = RemoveUnicodeCharacter(json.Replace("\n", ""));
                        var newTags = JsonConvert.DeserializeObject<List<Tag>>(json);
                        if (newTags.Count == 0)
                            return tags;

                        //https://activeforlife.com/wp-json/wp/v2/tags?per_page=100&page=14
                        //Bu site sayfa numarası farketmeksizin tamamını veriyor, bu yüzden aynı seyi defalarca ekliyor
                        //Bunu engellemek icin ilk ekledigimiz ile yeni aldigimizin ilk tag'i ayni ise devm etmeye gerek yok
                        if (tags.Count > 0 && newTags.FirstOrDefault().name == tags.FirstOrDefault().name)
                            return tags;

                        tags.AddRange(newTags);

                        //https://activeforlife.com/wp-json/wp/v2/tags?per_page=100&page=14
                        //Bu site sayfa numarasi farketmeksizin tüm tag'leri donuyor 1300 tane, dolayısıyla ben 100 tane istedigim halde
                        //fazla geliyorsa tum tag'leri ztn donmus diye dusunebiliriz.
                        if (newTags.Count > 100)
                            return tags;
                    }
                    catch (Exception exc)
                    {
                        return tags;
                    }
                }
            }

            return tags;
        }

        /// <summary>
        /// json cevap vermeyene kadar sitedeki tum tag'leri ceker
        /// </summary>
        /// <returns></returns>
        public List<Category> GetCategories()
        {
            var categories = new List<Category>();

            using (WebClient wc = BuildWebClient())
            {
                for (int i = 1; i <= int.MaxValue; i++)
                {
                    var path = $"{_baseApiPath}/categories?per_page=100&page={i}";
                    var json = "";
                    try
                    {
                        json = wc.DownloadString(path);

                        if (string.IsNullOrEmpty(json) || json == "[]")
                            return categories;

                        json = RemoveUnicodeCharacter(json.Replace("\n", ""));

                        var newCategories = JsonConvert.DeserializeObject<List<Category>>(json);
                        if (newCategories.Count == 0)
                            return categories;

                        //https://binaryterms.com/wp-json/wp/v2/categories?per_page=100&page=4
                        //Bu site sayfa numarası farketmeksizin tamamını veriyor, bu yüzden aynı seyi defalarca ekliyor
                        //Bunu engellemek icin ilk ekledigimiz ile yeni aldigimizin ilk tag'i ayni ise devm etmeye gerek yok
                        if (categories.Count > 0 && newCategories.FirstOrDefault().name == categories.FirstOrDefault().name)
                            return categories;

                        categories.AddRange(newCategories);
                    }
                    catch (Exception exc)
                    {
                        return categories;
                    }
                }
            }

            return categories;
        }

        public Media GetMedia(string mediaId)
        {
            using (var client = BuildWebClient())
            {
                var path = $"{_baseApiPath}/media/{mediaId}";
                var json = "";
                try
                {
                    json = client.DownloadString(path);
                }
                catch (Exception ex)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(json) || json == "[]")
                    return null;

                //litespeed eklentisi var ise, notice vs uyari cikariyor jsoın oncesinde
                json = RemoveUnicodeCharacter(json.Replace("\n", ""));
                var splitForNotice = json.Split("<br />", StringSplitOptions.None);

                try
                {
                    var mediaJson = JsonConvert.DeserializeObject<Media>(splitForNotice.LastOrDefault());
                    if (mediaJson.guid != null && !string.IsNullOrEmpty(mediaJson.guid.rendered))
                        mediaJson.media_url = mediaJson.guid.rendered;

                    if (string.IsNullOrEmpty(mediaJson.media_url) && mediaJson.description != null)
                    {
                        var regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);
                        if (regImg.IsMatch(mediaJson.description.rendered))
                            mediaJson.media_url = regImg.Match(mediaJson.description.rendered).Groups["imgUrl"].Value;
                    }

                    if (string.IsNullOrEmpty(mediaJson.media_url))
                        return null;

                    return mediaJson;
                }
                catch
                {
                    return null;
                }
            }
        }

        private bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private WebClient BuildWebClient(int timeout = 100)
        {
            var client = new CustomWebClient();

            client.TimeOutInSeconds = timeout;
            client.Encoding = System.Text.Encoding.UTF8;

            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            client.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.Headers.Add("cache-control", "max-age=0");

            return client;
        }

        private string RemoveUnicodeCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            value = value.Replace(@"\ç", "ç")
                        .Replace(@"\u00e7", "ç")
                        .Replace(@"\u00c7", "Ç")
                        .Replace(@"\Ç", "Ç")
                        .Replace(@"\u015f", "ş")
                        .Replace(@"\ş", "ş")
                        .Replace(@"\u0131", "ı")
                        .Replace(@"\ı", "ı")
                        .Replace(@"\u022b", "ö")
                        .Replace(@"\ö", "ö")
                        .Replace(@"\u022a", "Ö")
                        .Replace(@"\Ö", "Ö")
                        .Replace(@"\u011e", "Ğ")
                        .Replace(@"\Ğ", "Ğ")
                        .Replace(@"\u011f", "ğ")
                        .Replace(@"\ğ", "ğ");

            value = value.Replace(@"\ç", "ç");

            return value;
        }
    }
}
