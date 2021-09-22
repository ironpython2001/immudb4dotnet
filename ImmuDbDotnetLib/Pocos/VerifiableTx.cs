using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImmuDbDotnetLib.Pocos
{
    public class VerifiableTx
    {
        [JsonProperty("metadata")]
        public Metadata Metadata
        {
            get; set;
        }

        [JsonProperty("entries")]
        public List<Entry> Entries
        {
            get; set;
        }
    }


}







