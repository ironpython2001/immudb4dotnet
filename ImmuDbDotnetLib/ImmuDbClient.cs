using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeNotary.ImmuDb.ImmudbProto;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Empty = Google.Protobuf.WellKnownTypes.Empty;

namespace ImmuDbDotnetLib
{
    public class ImmuDbClient
    {
        private readonly Channel channel;
        private readonly ImmuService.ImmuServiceClient client;
        private string authToken;
        private string activeDatabaseName = "defaultdb";

        private Metadata securityHeader
        {
            get
            {
                if (string.IsNullOrEmpty(this.authToken))
                {
                    throw new AuthenticationException("You need to log in before performing this operation");
                }
                var metadata = new Metadata();
                metadata.Add("authorization", "Bearer " + this.authToken);
                return metadata;
            }
        }

        public ImmuDbClient(string address = "localhost", int port = 3322)
        {
            this.channel = new Channel(address, port, ChannelCredentials.Insecure);
            this.client = new ImmuService.ImmuServiceClient(this.channel);
            
        }

       
        public async Task<(bool IsSuccess,string Warning)> LoginAsync(string user, string password, string databaseName = null)
        {
            (bool IsSuccess, string Warning) result = (false,string.Empty);
            var request = new LoginRequest()
            {
                User = ByteString.CopyFromUtf8(user),
                Password = ByteString.CopyFromUtf8(password),
            };

            var response = await this.client.LoginAsync(request, new CallOptions() { });

            this.authToken = response.Token;

            if (!response.Warning.IsEmpty)
            {
                result.IsSuccess = true;
                result.Warning = response.Warning.ToStringUtf8();
                
            }
            if (!string.IsNullOrEmpty(databaseName))
            {
                await this.UseDatabaseAsync(databaseName);
                result.IsSuccess = true;
                result.Warning = string.Empty;
            }
            return result;
        }

        public async Task UseDatabaseAsync(string databaseName, bool createIfNotExists = true)
        {
            var databases = await this.GetDatabasesAsync();

            if (!databases.Contains(databaseName))
            {
                if (createIfNotExists)
                {
                    await this.CreateDatabaseAsync(databaseName);
                }
                else
                {
                    throw new Exception($"Database {databaseName} does not exists");
                }
            }

            var result = await this.client.UseDatabaseAsync(new Database() { DatabaseName = databaseName }, this.securityHeader);

            this.activeDatabaseName = databaseName;


            this.authToken = result.Token;
        }

        public async Task<IEnumerable<string>> GetDatabasesAsync()
        {
            var databases = await this.client.DatabaseListAsync(new Empty(), this.securityHeader);
            return databases.Databases.Select(db => db.DatabaseName);
        }

        public async Task LogoutAsync()
        {
            if (this.client != null && !string.IsNullOrEmpty(this.authToken))
            {
                await this.client.LogoutAsync(new Empty(), this.securityHeader);
                this.authToken = null;
            }
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            await this.client.CreateDatabaseAsync(new Database() { DatabaseName = databaseName }, this.securityHeader);
        }

        public async Task<ulong> SetAsync(string key, string value)
        {
            var request = new SetRequest();
            request.KVs.Add(new KeyValue()
            {
                //Key = ByteString.CopyFrom(Encoding.ASCII.GetBytes(key)), 
                //Value = ByteString.CopyFrom(Encoding.ASCII.GetBytes(value))
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(value)
            });

            var reply = await this.client.SetAsync(request, this.securityHeader);

            return reply.Id;
        }

        public async Task<string> GetAsync(string key)
        {
            var request = new KeyRequest()
            {
                Key = ByteString.CopyFromUtf8(key)
            };
            var reply = await this.client.GetAsync(request, this.securityHeader);
            return reply.Value.ToStringUtf8();
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var json = await this.GetAsync(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        //public async Task StreamSet()
        //{
        //    var chunk = new Chunk()
        //    {
        //        Content = 
        //    };
        //    this.client.streamSet(null).RequestStream.WriteAsync()
        //}

        public async Task StreamGet(string key)
        {
            var kr = new KeyRequest()
            {
                Key = ByteString.CopyFromUtf8(key)
            };
            var cts = new CancellationTokenSource(15000); //15 seconds
            
            var chunk = this.client.streamGet(kr,this.securityHeader,cancellationToken:cts.Token);
            while (await chunk.ResponseStream.MoveNext(cts.Token))
            {
                var bs = chunk.ResponseStream.Current.Content;
                Console.WriteLine(bs.ToStringUtf8());
            }
        }

        public void Close()
        {
            try
            {
                if (this.client != null && !string.IsNullOrEmpty(this.authToken))
                {
                    this.client.Logout(new Empty(), this.securityHeader);
                    this.authToken = null;
                }

                if (this.channel != null && this.channel.State != ChannelState.Shutdown)
                {
                    this.channel.ShutdownAsync();
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}




//https://eddyf1xxxer.medium.com/bi-directional-streaming-and-introduction-to-grpc-on-asp-net-core-3-0-part-2-d9127a58dcdb
//https://referbruv.com/blog/posts/implementing-stream-based-communication-with-grpc-and-aspnet-core
//https://www.codemartini.com/grpc-net-core-sample-with-stream-call/
//https://stackoverflow.com/questions/69029481/grpc-web-supporting-client-streaming-in-net
//https://www.c-sharpcorner.com/chapters/
//https://medium.com/@ricardo.torres89.rt/asynchronous-data-streaming-with-net-core-3-0-grpc-and-iasyncenumerable-d970b53177e
//https://stackoverflow.com/questions/15067865/how-to-use-the-cancellationtoken-property
//https://www.browserling.com/tools/base64-decode
//http://string-functions.com/encodedecode.aspx
//https://www.webatic.com/encoding-explorer


