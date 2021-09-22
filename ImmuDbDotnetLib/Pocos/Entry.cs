using Newtonsoft.Json;

namespace ImmuDbDotnetLib.Pocos
{
    public class Entry
    {
        [JsonProperty("key")]
        public string Key
        {
            get; set;
        }

        [JsonProperty("hValue")]
        public string HValue
        {
            get; set;
        }

        [JsonProperty("vLen")]
        public int VLen
        {
            get; set;
        }
    }


}







