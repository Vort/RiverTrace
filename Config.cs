using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;

namespace RiverTrace
{
    [DataContract]
    class ConfigData
    {
        [DataMember]
        public int zoom;

        [DataMember]
        public double lat1;
        [DataMember]
        public double lon1;
        [DataMember]
        public double lat2;
        [DataMember]
        public double lon2;

        [DataMember]
        public int iterationCount;
        [DataMember]
        public double scanRadiusScale;
        [DataMember]
        public double angleRange;
        [DataMember]
        public double angleStep;
        [DataMember]
        public double shoreContrast;
        [DataMember]
        public double advanceRate;

        [DataMember]
        public bool debug;
    }

    class Config
    {
        public static ConfigData Data;
        private static string fileName;

        static Config()
        {
            fileName = "config.json";
            Data = new ConfigData
            {
                zoom = 15,
                lat1 = 64.9035637,
                lon1 = 52.2209239,
                lat2 = 64.9032122,
                lon2 = 52.2213061,
                iterationCount = 300,
                scanRadiusScale = 2.0,
                angleRange = 90.0,
                angleStep = 4.0,
                shoreContrast = 10.0,
                advanceRate = 0.5,
                debug = false
            };
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
