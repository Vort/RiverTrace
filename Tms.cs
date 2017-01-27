using System;
using System.Net;
using System.Text.RegularExpressions;

namespace RiverTrace
{
    class Tms : ImageSource
    {
        private string tileUrl;
        private static WebClient wc;

        public Tms(string tileUrl)
        {
            this.tileUrl = tileUrl;

            Match match = Regex.Match(tileUrl, "\\{switch:(([^,}]),?)+}");
            if (match.Success)
            {
                var capt = match.Groups[2].Captures;
                this.tileUrl = tileUrl.Replace(match.Value,
                    capt[new Random().Next(capt.Count)].Value);
            }

            wc = new WebClient();
        }

        public byte[] GetTile(int tileIndexX, int tileIndexY, int zoom)
        {
            string finalUrl = tileUrl.
                Replace("{x}", tileIndexX.ToString()).
                Replace("{y}", tileIndexY.ToString()).
                Replace("{zoom}", zoom.ToString());
            byte[] data = wc.DownloadData(finalUrl);
            return data;
        }
    }
}
