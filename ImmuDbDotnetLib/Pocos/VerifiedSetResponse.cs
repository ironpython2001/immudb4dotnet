﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace ImmuDbDotnetLib.Pocos
{
    public class VerifiedSetResponse
    {
        private string json;
        public VerifiedSetResponse(string json)
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







