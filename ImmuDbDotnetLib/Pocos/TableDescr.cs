using System;
using System.Collections.Generic;
using System.Text;

namespace ImmuDbDotnetLib.Pocos
{
    public class ColumnDescription
    {
        public string Column
        {
            get; set;
        }
        public string Type
        {
            get;
            set;
        }
        public bool Nullable
        {
            get;
            set;
        }
        public string Index
        {
            get;
            set;
        }
        public bool AutoIncrement
        {
            get;
            set;
        }
        public bool Unique
        {
            get;
            set;
        }

    }
}
