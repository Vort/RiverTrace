using System.Net;
using System.Text.RegularExpressions;

namespace RiverTrace
{
    class Bing : ImageSource
    {
        private string tileUrl;
        private WebClient wc;

        public Bing()
        {
            tileUrl = "";
            wc = new WebClient();
        }

        private static string GetQuadkey(int tileIndexX, int tileIndexY, int zoom)
        {
            string quadKey = "";
            for (int i = 0; i < zoom; i++)
            {
                quadKey = ((tileIndexX & 1) + ((tileIndexY & 1) << 1)).ToString() + quadKey;
                tileIndexX >>= 1;
                tileIndexY >>= 1;
            }
            return quadKey;
        }

        public byte[] GetTile(int tileIndexX, int tileIndexY, int zoom)
        {
            if (tileUrl == "")
            {
                string apiKey = "Arzdiw4nlOJzRwOz__qailc8NiR31Tt51dN2D7cm57NrnceZnCpgOkmJhNpGoppU";
                string restUrl = "http://dev.virtualearth.net/REST/v1/Imagery/Metadata/Aerial?" + 
                    "include=ImageryProviders&output=xml&key=" + apiKey;
                string xml = wc.DownloadString(restUrl);
                Match match1 = Regex.Match(xml, "<ImageryMetadata><ImageUrl>([^<]*)<");
                Match match2 = Regex.Match(xml, "<ImageUrlSubdomains><string>([^<]*)<");
                tileUrl = match1.Groups[1].Value.Replace("{subdomain}", match2.Groups[1].Value);
            }

            string qk = GetQuadkey(tileIndexX, tileIndexY, zoom);
            string finalUrl = tileUrl.Replace("{quadkey}", qk);
            byte[] data = wc.DownloadData(finalUrl);
            return data;
        }
    }
}
