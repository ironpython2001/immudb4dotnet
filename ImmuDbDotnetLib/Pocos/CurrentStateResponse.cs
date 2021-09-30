using Google.Protobuf;

namespace ImmuDbDotnetLib.Pocos
{
    public class CurrentStateResponse
    {
        public ulong TxId
        {
            get;
            internal set;
        }
        public string TxHash
        {
            get;
            internal set;
        }
        public string Db
        {
            get;
            internal set;
        }
    }
}