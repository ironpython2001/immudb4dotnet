using System;
using System.Threading.Tasks;
using CodeNotary.ImmuDb;
using ImmuDbDotnetLib;
using CodeNotary.ImmuDb.ImmudbProto;

namespace ImmuDbClientDemoApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new  ImmuDbClient();
            var result = await client.LoginAsync("immudb","immudb");
            if (result.IsSuccess)
            {

                await client.UseDatabaseAsync("defaultdb", false);
                //https://docs.immudb.io/master/quickstart.html#basic-operations-with-immuclient
                //ar s = await client.GetAsync("balance");
                await client.SetAsync("balance", "100");
                var s = await client.GetAsync("balance");
                await client.StreamGet("balance");


                //await client.SafeSetAsync("balance", 9001.ToString());

                await client.LogoutAsync();
            }

        }
    }
}
