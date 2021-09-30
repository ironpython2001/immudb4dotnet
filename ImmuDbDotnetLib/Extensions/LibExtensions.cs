using System;
using System.Collections.Generic;
using System.Text;

namespace ImmuDbDotnetLib.Extensions
{
    public static class LibExtensions
    {
        public static Pocos.StatusCode ToPocoStatusCode(this Grpc.Core.StatusCode obj)
        {
            int enumAsInt = (int)obj;
            Pocos.StatusCode result = (Pocos.StatusCode)enumAsInt;
            return result;
        }
        public static byte[] SHAHash(this byte[] array)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(array);
        }
    }
}
