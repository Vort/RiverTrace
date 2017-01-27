using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace RiverTrace
{
    enum ImageSourceProtocol
    {
        bing,
        tms,
        wms
    }

    class ConfigData
    {
        public int zoom;

        public double lat1;
        public double lon1;
        public double lat2;
        public double lon2;

        public int iterationCount;
        public double shoreContrast;
        public double scanRadiusScale;
        public double angleRange;
        public double advanceRate;
        public double noiseReduction;
        public double resamplingFactor;
        public double simplificationStrength;

        public bool debug;

        public string imageSourceName;
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageSourceProtocol imageSourceProtocol;
        public string imageSourceUrl;

        public ConfigData()
        {
            zoom = 15;
            lat1 = 64.9035637;
            lon1 = 52.2209239;
            lat2 = 64.9032122;
            lon2 = 52.2213061;
            iterationCount = 300;
            shoreContrast = 10.0;
            scanRadiusScale = 2.0;
            angleRange = 90.0;
            advanceRate = 0.5;
            noiseReduction = 0.5;
            resamplingFactor = 1.5;
            simplificationStrength = 0.1;
            debug = false;
            imageSourceName = "Bing";
            imageSourceProtocol = ImageSourceProtocol.bing;
            imageSourceUrl = "";
        }
    }

    class Config
    {
        public static ConfigData Data;
        private static string fileName;

        static Config()
        {
            fileName = "config.json";
            Data = new ConfigData();
            if (File.Exists(fileName))
                Data = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(fileName));
        }

        public static void Write()
        {
            File.WriteAllText(fileName,
                JsonConvert.SerializeObject(Data, Formatting.Indented));
        }
    }
}
