using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImmuDbDotnetLib;
using Pocos = ImmuDbDotnetLib.Pocos;
using static System.Console;


namespace ImmuDbClientDemoApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new ImmuDbClient();

            var loginRequest = new Pocos.LoginRequest() { User = "immudb", Password = "immudb" };
            var loginResponse = await client.LoginAsync(loginRequest);

            WriteLine(loginResponse.IsSuccess);
            WriteLine(loginResponse.Detail);

            var useDatabaseResponse= await client.UseDatabaseAsync("defaultdb");
            WriteLine(useDatabaseResponse.IsSuccess);
            WriteLine(useDatabaseResponse.Detail);

            //https://docs.immudb.io/master/quickstart.html#basic-operations-with-immuclient
            await client.SetAsync("balance", "100");
            var s = await client.GetAsync("balance");
            await client.VerifiedSet("balance", "100");
            await client.VerifiedGet("balance");

            var result2 = await client.GetAll(new List<string> { "balance" });
            foreach (var res in result2)
            {
                Console.WriteLine(res.Tx);
            }


            //var fi = new FileInfo("sample.txt");
            //await client.UploadFile(fi);
            ////var s2 = await client.GetAsync(fi.Name);
            //await client.DownloadFile(fi);
            //await client.SafeSetAsync("balance", 9001.ToString());

            await client.LogoutAsync();


        }
    }
}
