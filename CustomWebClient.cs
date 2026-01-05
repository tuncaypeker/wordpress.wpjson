using System;
using System.Net;

namespace Wordpress.WpJson
{
    public class CustomWebClient : WebClient
    {
        public int TimeOutInSeconds { get; set; }
        public bool AllowRedirects { get; set; }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            
            w.Timeout = TimeOutInSeconds * 1000;

            return w;
        }
    }
}
