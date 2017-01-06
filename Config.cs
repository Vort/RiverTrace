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
        public double sampleWidthScale;
        [DataMember]
        public double sampleLengthScale;
        [DataMember]
        public double shoreContrast;
        [DataMember]
        public double maxDifference;

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
                iterationCount = 1000,
                sampleWidthScale = 1.7,
                sampleLengthScale = 0.7,
                shoreContrast = 10.0,
                maxDifference = 28.0,
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
