using Newtonsoft.Json;

namespace ImmuDbDotnetLib.Pocos
{
    public class Metadata
    {
        [JsonProperty("id")]
        public string Id
        {
            get; set;
        }

        [JsonProperty("prevAlh")]
        public string PrevAlh
        {
            get; set;
        }

        [JsonProperty("ts")]
        public string Ts
        {
            get; set;
        }

        [JsonProperty("nentries")]
        public int Nentries
        {
            get; set;
        }

        [JsonProperty("eH")]
        public string EH
        {
            get; set;
        }

        [JsonProperty("blTxId")]
        public string BlTxId
        {
            get; set;
        }

        [JsonProperty("blRoot")]
        public string BlRoot
        {
            get; set;
        }
    }


}







