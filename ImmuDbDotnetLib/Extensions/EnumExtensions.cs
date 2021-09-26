using System;
using System.Collections.Generic;
using System.Text;

namespace ImmuDbDotnetLib.Extensions
{
    public static class EnumExtensions
    {
        public static Pocos.StatusCode ToPocoStatusCode(this Grpc.Core.StatusCode obj)
        {
            int enumAsInt = (int)obj;
            Pocos.StatusCode result = (Pocos.StatusCode)enumAsInt;
            return result;
        }
    }
}
