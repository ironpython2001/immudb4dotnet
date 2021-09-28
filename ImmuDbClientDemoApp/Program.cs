using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImmuDbDotnetLib;
using Pocos = ImmuDbDotnetLib.Pocos;
using static System.Console;
using System.IO;
using System.Text;

namespace ImmuDbClientDemoApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new ImmuDbClient();

            var req1 = new Pocos.LoginRequest()
            {
                User = "immudb",
                Password = "immudb"
            };
            var res1 = await client.LoginAsync(req1);
            WriteLine(new string('*', 20));
            WriteLine($"calling {nameof(client.LoginAsync)}");
            WriteLine($"{nameof(res1.StatusCode)}:- {res1.StatusCode}");
            WriteLine($"{nameof(res1.Detail)}:- {res1.Detail}");
            WriteLine(new string('*', 20));

            var res2 = await client.UseDatabaseAsync("defaultdb");
            WriteLine(res2.StatusCode);
            WriteLine(res2.Detail);

            var res3 = await client.DatabaseListAsync();
            WriteLine(res3.status.StatusCode);
            WriteLine(res3.status.Detail);
            foreach (var dbName in res3.DatabaseNames)
            {
                WriteLine(dbName);
            }

            //https://docs.immudb.io/master/quickstart.html#basic-operations-with-immuclient
            var res4 = await client.SetAsync("balance", "100");
            WriteLine(res4.status.StatusCode);
            WriteLine(res4.status.Detail);
            WriteLine(res4.Id);

            var res5 = await client.GetAsync("balance");
            WriteLine(res5.status.StatusCode);
            WriteLine(res5.status.Detail);
            WriteLine(res5.Value);

            var res6 = await client.VerifiedGet("balance");
            WriteLine(res6.status.StatusCode);
            WriteLine(res6.status.Detail);
            WriteLine(res6.response.TxId);
            WriteLine(res6.response.Key);
            WriteLine(res6.response.Value);
            WriteLine(res6.response.ToString());

            var res7 = await client.VerifiedSet("balance", "100");
            WriteLine(res7.status.StatusCode);
            WriteLine(res7.status.Detail);
            WriteLine(res7.response.TxId);
            WriteLine(res7.response.Key);
            WriteLine(res7.response.Value);
            WriteLine(res7.response.ToString());

            //var res8 = await client.GetTx(196);
            //WriteLine(res8.status.StatusCode);
            //WriteLine(res8.status.Detail);
            //WriteLine(res8.response.TxId);
            //WriteLine(res8.response.Key);
            //WriteLine(res8.response.Value);
            //WriteLine(res8.response.ToString());

            var res9 = await client.Tables();
            WriteLine(res9.status.StatusCode);
            WriteLine(res9.status.Detail);
            foreach (var s in res9.tables)
            {
                WriteLine(s);
            }

            var sql1 = @"BEGIN TRANSACTION
                          CREATE TABLE Employee(id INTEGER, name VARCHAR, salary INTEGER, PRIMARY KEY id);
                        COMMIT";
            //var res10 = await client.SQLExec(sql1);
            //WriteLine(res10.StatusCode);
            //WriteLine(res10.Detail);

            var sql2 = @"UPSERT INTO Employee(id, name, salary) VALUES (1, 'Joe', 1000);";
            var res11 = await client.SQLExec(sql2);
            WriteLine(res11.StatusCode);
            WriteLine(res11.Detail);

            var sql3 = @"SELECT t.id as id,t.name as name,t.salary as salary FROM (Employee AS t) WHERE id <= 3 ";
            var res12 = await client.SQLQuery(sql3);
            WriteLine(res12.StatusCode);
            WriteLine(res12.Detail);

            //var result2 = await client.GetAll(new List<string> { "balance" });
            //foreach (var res in result2)
            //{
            //    Console.WriteLine(res.Tx);
            //}


            //var fi = new FileInfo("sample.txt");
            //await client.UploadFile(fi);
            ////var s2 = await client.GetAsync(fi.Name);
            //await client.DownloadFile(fi);
            //await client.SafeSetAsync("balance", 9001.ToString());

            await client.LogoutAsync();


        }
    }
}
