using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CodeNotary.ImmuDb.ImmudbProto;
using FluentValidation;
using Google.Protobuf;
using Grpc.Core;
using ImmuDbDotnetLib.Extensions;
using ImmuDbDotnetLib.Validators;
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

        public Metadata AuthHeader
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
            var validator1 = new StringValidator();
            validator1.ValidateAndThrow(address);
            var validator2 = new IntValidator();
            validator2.ValidateAndThrow(port);

            this.channel = new Channel(address, port, ChannelCredentials.Insecure);
            this.client = new ImmuService.ImmuServiceClient(this.channel);
        }

        public async Task<Pocos.Status> LoginAsync(Pocos.LoginRequest request)
        {
            var validator = new LoginRequestValidator();
            validator.ValidateAndThrow(request);

            var response = new Pocos.Status();
            try
            {
                var rpcRequest = new LoginRequest()
                {
                    User = ByteString.CopyFromUtf8(request.User),
                    Password = ByteString.CopyFromUtf8(request.Password),
                };

                var rpcResponse = await this.client.LoginAsync(rpcRequest, new CallOptions() { });

                this.authToken = rpcResponse.Token;

                if (!rpcResponse.Warning.IsEmpty)
                {
                    response.StatusCode = Pocos.StatusCode.OK;
                    response.Detail = rpcResponse.Warning.ToStringUtf8();
                }
            }
            catch (RpcException ex)
            {
                response.StatusCode = ex.StatusCode.ToPocoStatusCode();
                response.Detail = ex.Status.Detail;
            }
            return response;
        }

        public async Task<Pocos.Status> UseDatabaseAsync(string databaseName)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(databaseName);

            var response = new Pocos.Status();
            try
            {
                var rpcRequest = new Database()
                {
                    DatabaseName = databaseName
                };
                var rpcResponse = await this.client.UseDatabaseAsync(rpcRequest, this.AuthHeader);
                this.activeDatabaseName = databaseName;
                this.authToken = rpcResponse.Token;

                response.StatusCode = Pocos.StatusCode.OK;
                response.Detail = string.Empty;
            }
            catch (RpcException ex)
            {
                response.StatusCode = ex.StatusCode.ToPocoStatusCode();
                response.Detail = ex.Status.Detail;
            }
            return response;
        }

        public async Task<(Pocos.Status status, IEnumerable<string> DatabaseName)> DatabaseListAsync()
        {
            try
            {
                var rpcRequest = new Empty();
                var databases = await this.client.DatabaseListAsync(rpcRequest, this.AuthHeader);
                var dbs = databases.Databases.Select(db => db.DatabaseName).ToList<string>();
                return (new Pocos.Status { StatusCode = Pocos.StatusCode.OK, Detail = string.Empty }, dbs);
            }
            catch (RpcException ex)
            {
                return (new Pocos.Status { StatusCode = ex.StatusCode.ToPocoStatusCode(), Detail = ex.Status.Detail }, null);
            }
        }

        public async Task LogoutAsync()
        {
            if (this.client != null && !string.IsNullOrEmpty(this.authToken))
            {
                await this.client.LogoutAsync(new Empty(), this.AuthHeader);
                this.authToken = null;
            }
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            await this.client.CreateDatabaseAsync(new Database() { DatabaseName = databaseName }, this.AuthHeader);
        }

        public async Task<ulong> SetAsync(string key, string value)
        {
            var request = new SetRequest();
            request.KVs.Add(new KeyValue()
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(value)
            });
            var reply = await this.client.SetAsync(request, this.AuthHeader);
            return reply.Id;
        }

        public async Task<string> GetAsync(string key)
        {
            var mdh = this.AuthHeader;
            var request = new KeyRequest()
            {
                Key = ByteString.CopyFromUtf8(key)
            };
            var reply = await this.client.GetAsync(request, mdh);
            return reply.Value.ToStringUtf8();
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var json = await this.GetAsync(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<List<(ulong Tx, string Key, string Value)>> GetAll(List<string> keys)
        {
            var result = new List<(ulong Tx, string Key, string Value)>();
            var klr = new KeyListRequest();
            keys.ForEach(key => klr.Keys.Add(ByteString.CopyFromUtf8(key)));
            var cts = new CancellationTokenSource(15000);
            var entries = await this.client.GetAllAsync(klr, this.AuthHeader, null, cts.Token);
            foreach (var e in entries.Entries_)
            {
                result.Add((e.Tx, e.Key.ToStringUtf8(), e.Value.ToStringUtf8()));
            }
            return result;
        }

        public async Task<Pocos.VerifiedSetResponse> VerifiedSet(string key, string value)
        {
            var mdh = this.AuthHeader;
            var request = new VerifiableSetRequest();
            var kv = new KeyValue()
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(value)
            };
            request.SetRequest = new SetRequest();
            request.SetRequest.KVs.Add(kv);
            using var cts = new CancellationTokenSource();
            var verifiableTx = await this.client.VerifiableSetAsync(request, mdh, null, cts.Token);
            var json = verifiableTx.Tx.ToString();
            return JsonConvert.DeserializeObject<Pocos.VerifiedSetResponse>(json);
        }

        public async Task<List<string>> VerifiedGet(string key)
        {
            var result = new List<string>();
            var mdh = this.AuthHeader;
            var request = new VerifiableGetRequest()
            {
                KeyRequest = new KeyRequest() { Key = ByteString.CopyFromUtf8(key) },
            };
            using var cts = new CancellationTokenSource();
            var response = await this.client.VerifiableGetAsync(request, mdh, null, cts.Token);
            var entriesEnumerator = response.VerifiableTx.Tx.Entries.GetEnumerator();
            while (entriesEnumerator.MoveNext())
            {
                //Console.WriteLine(entriesEnumerator.Current);
                result.Add(entriesEnumerator.Current.HValue.ToStringUtf8());
            }
            return result;
        }




        public void Close()
        {
            try
            {
                if (this.client != null && !string.IsNullOrEmpty(this.authToken))
                {
                    this.client.Logout(new Empty(), this.AuthHeader);
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

        //public async Task UploadFile(FileInfo fileInfo)
        //{
        //    var mdh = this.AuthHeader;
        //    using var cts = new CancellationTokenSource();
        //    var fileLines = File.ReadAllText(fileInfo.FullName);
        //    var chunk = new Chunk()
        //    {
        //        Content = ByteString.CopyFromUtf8(fileLines)
        //    };
        //    //var entry = new Metadata.Entry(fileInfo.Name, fileLines);
        //    //mdh.Add(entry);

        //    var ss = this.client.streamSet(mdh, null, cts.Token);

        //    await ss.RequestStream.WriteAsync(chunk);
        //    //var kv = new KeyValue() { Key = ByteString.CopyFromUtf8(fileInfo.Name), Value = ByteString.CopyFromUtf8(fileLines) };
        //    //await ss.RequestStream.WriteAsync(kv);
        //    await ss.RequestStream.CompleteAsync();
        //    await ss;
        //    //ss.Dispose();
        //    //var response = await ss.ResponseAsync;
        //    //var ss2= ss.GetAwaiter().GetResult();
        //    var s1 = ss.GetStatus();
        //    Console.WriteLine(s1.StatusCode);
        //    Console.WriteLine(s1.Detail);

        //    var md = await ss.ResponseHeadersAsync;
        //    Console.WriteLine(md.First().Key);
        //    Console.WriteLine(md.First().Value);
        //}
        //public async Task UploadFile(FileInfo fileInfo)
        //{
        //    AsyncClientStreamingCall<Chunk, TxMetadata> ss=null;
        //    var mdh = this.AuthHeader;

        //    using var cts = new CancellationTokenSource();
        //    var fileBytes = File.ReadAllBytes(fileInfo.FullName);
        //    using var reader = new StreamReader(fileInfo.FullName);
        //    var chunkNo = 0;
        //    while ((!reader.EndOfStream) && (!cts.Token.IsCancellationRequested))
        //    {
        //        var line = reader.ReadLine();
        //        var chunk = new Chunk()
        //        {
        //            Content = ByteString.CopyFromUtf8(line)
        //        };
        //        chunkNo++;
        //        var entry = new Metadata.Entry(fileInfo.Name, $"{chunkNo}");
        //        mdh.Add(entry);
        //        //mdh.Add(fileInfo.Name, fileBytes);
        //        ss = this.client.streamSet(mdh, null, cts.Token);

        //        await ss.RequestStream.WriteAsync(chunk);
        //    }
        //    //await this.client.streamSet(mdh, null, cts.Token).RequestStream.CompleteAsync();
        //    await ss.RequestStream.CompleteAsync();
        //    await ss;
        //    var s1 = ss.GetStatus();
        //    Console.WriteLine(s1.StatusCode);
        //    Console.WriteLine(s1.Detail);

        //}
        //public async Task DownloadFile(FileInfo fileInfo)
        //{
        //    var mdh = this.AuthHeader;
        //    mdh.Add(new Metadata.Entry(fileInfo.Name, string.Empty));
        //    var kr = new KeyRequest()
        //    {
        //        Key = ByteString.CopyFromUtf8(fileInfo.Name)
        //    };
        //    var cts = new CancellationTokenSource(15000); //15 seconds

        //    var chunk = this.client.streamGet(kr, mdh, cancellationToken: cts.Token);
        //    while (await chunk.ResponseStream.MoveNext(cts.Token))
        //    {
        //        var bs = chunk.ResponseStream.Current.Content;
        //        Console.WriteLine(bs.ToStringUtf8());
        //    }
        //}
    }


}







