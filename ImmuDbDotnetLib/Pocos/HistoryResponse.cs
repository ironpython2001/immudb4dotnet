using System;
using System.Collections.Generic;
using System.Text;

namespace ImmuDbDotnetLib.Pocos
{
    public class HistoryResponse
    {
        public ulong Tx
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
    }
}
