namespace ImmuDbDotnetLib.Pocos
{
    public class VerifiedGetResponse
    {
        private string json;
        public VerifiedGetResponse(string json)
        {
            this.json = json;
        }
        public ulong TxId
        {
            get;
            internal set;
        }
        public string Key
        {
            get;
            internal set;
        }
        public string Value
        {
            get;
            internal set;
        }

        public override string ToString()
        {
            return this.json;
        }
    }
}







