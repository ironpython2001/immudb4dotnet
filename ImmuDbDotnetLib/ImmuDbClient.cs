using System;
using System.Collections.Generic;
using System.IO;
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
    public class ImmuDbClient : IDisposable
    {
        private readonly Channel channel;
        private readonly ImmuService.ImmuServiceClient client;
        private string authToken;
        private string activeDatabaseName = "defaultdb";
        private bool disposedValue;

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
        public async Task<Pocos.RpcStatus> LoginAsync(Pocos.LoginRequest request)
        {
            var validator = new LoginRequestValidator();
            validator.ValidateAndThrow(request);

            var response = new Pocos.RpcStatus();
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
        public async Task<Pocos.RpcStatus> UseDatabaseAsync(string databaseName)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(databaseName);

            var response = new Pocos.RpcStatus();
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
        public async Task<(Pocos.RpcStatus status, IEnumerable<string> DatabaseNames)> DatabaseListAsync()
        {
            try
            {
                var rpcRequest = new Empty();
                var databases = await this.client.DatabaseListAsync(rpcRequest, this.AuthHeader);
                var dbs = databases.Databases.Select(db => db.DatabaseName).ToList<string>();
                return (new Pocos.RpcStatus { StatusCode = Pocos.StatusCode.OK, Detail = string.Empty }, dbs);
            }
            catch (RpcException ex)
            {
                return (new Pocos.RpcStatus { StatusCode = ex.StatusCode.ToPocoStatusCode(), Detail = ex.Status.Detail }, null);
            }
        }
        public async Task<Pocos.RpcStatus> LogoutAsync()
        {
            try
            {
                if (this.client != null && !string.IsNullOrEmpty(this.authToken))
                {
                    await this.client.LogoutAsync(new Empty(), this.AuthHeader);
                    this.authToken = null;
                }
                return new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
            }
            catch (RpcException ex)
            {
                return new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
            }
        }
        public async Task<Pocos.RpcStatus> CreateDatabaseAsync(string databaseName)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(databaseName);

            try
            {
                await this.client.CreateDatabaseAsync(new Database()
                {
                    DatabaseName = databaseName
                },
                this.AuthHeader);
                return new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
            }
            catch (RpcException ex)
            {
                return new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
            }
        }
        public async Task<(Pocos.RpcStatus status, ulong? Id)> SetAsync(string key, string value)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(key);
            validator.ValidateAndThrow(value);

            (Pocos.RpcStatus status, ulong? Id) result;

            try
            {
                var request = new SetRequest();
                request.KVs.Add(new KeyValue()
                {
                    Key = ByteString.CopyFromUtf8(key),
                    Value = ByteString.CopyFromUtf8(value)
                });
                var reply = await this.client.SetAsync(request, this.AuthHeader);

                result.status = new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
                result.Id = reply.Id;
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.Id = null;
            }
            return result;
        }
        public async Task<(Pocos.RpcStatus status, string Value)> GetAsync(string key)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(key);

            (Pocos.RpcStatus status, string Value) result;

            try
            {
                var mdh = this.AuthHeader;
                var request = new KeyRequest()
                {
                    Key = ByteString.CopyFromUtf8(key)
                };
                var reply = await this.client.GetAsync(request, mdh);

                result.status = new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
                result.Value = reply.Value.ToStringUtf8();
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.Value = null;
            }

            return result;
        }
        public async Task<(Pocos.RpcStatus status, T Value)> GetAsync<T>(string key) where T : class
        {
            var result = await this.GetAsync(key);
            return (result.status, JsonConvert.DeserializeObject<T>(result.Value));
        }
        public async Task<(Pocos.RpcStatus status, Pocos.GetTxResponse response)> GetTx(ulong txno)
        {
            var validator = new ULongValidator();
            validator.ValidateAndThrow(txno);

            (Pocos.RpcStatus status, Pocos.GetTxResponse response) result;
            try
            {
                var rpcRequest = new TxRequest
                {
                    Tx = txno
                };
                using var cts = new CancellationTokenSource();
                var rpcResponse = await this.client.TxByIdAsync(rpcRequest, this.AuthHeader, null, cts.Token);
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
                result.response = new Pocos.GetTxResponse(rpcResponse.ToString());
                result.response.TxId = rpcResponse.Metadata.Id;

                var entriesEnumerator = rpcResponse.Entries.GetEnumerator();
                while (entriesEnumerator.MoveNext())
                {
                    result.response.Key = entriesEnumerator.Current.Key.ToStringUtf8();
                    result.response.Value = entriesEnumerator.Current.HValue.ToStringUtf8();
                }
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.response = null;
            }
            return result;

        }
        public async Task<(Pocos.RpcStatus status, Pocos.VerifiedSetResponse response)> VerifiedSet(string key, string value)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(key);
            validator.ValidateAndThrow(value);

            (Pocos.RpcStatus status, Pocos.VerifiedSetResponse response) result;
            try
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
                var rpcResponse = await this.client.VerifiableSetAsync(request, mdh, null, cts.Token);

                result.status = new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };

                result.response = new Pocos.VerifiedSetResponse(rpcResponse.ToString());
                result.response.TxId = rpcResponse.Tx.Metadata.Id;
                result.response.Key = key;
                result.response.Value = value;
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.response = null;
            }
            return result;
        }
        public async Task<(Pocos.RpcStatus status, Pocos.VerifiedGetResponse response)> VerifiedGet(string key)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(key);

            (Pocos.RpcStatus status, Pocos.VerifiedGetResponse response) result;

            try
            {
                var mdh = this.AuthHeader;
                var rpcRequest = new VerifiableGetRequest()
                {
                    KeyRequest = new KeyRequest()
                    {
                        Key = ByteString.CopyFromUtf8(key)
                    },
                };
                using var cts = new CancellationTokenSource();
                var rpcResponse = await this.client.VerifiableGetAsync(rpcRequest, mdh, null, cts.Token);

                result.status = new Pocos.RpcStatus
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };

                result.response = new Pocos.VerifiedGetResponse(rpcResponse.ToString());
                result.response.TxId = rpcResponse.VerifiableTx.Tx.Metadata.Id;
                result.response.Key = rpcResponse.Entry.Key.ToStringUtf8();
                result.response.Value = rpcResponse.Entry.Value.ToStringUtf8();

                //result.response.PublicKey= rpcResponse.VerifiableTx.Tx.Signature.PublicKey.ToStringUtf8();
                //var entriesEnumerator = rpcResponse.VerifiableTx.Tx.Entries.GetEnumerator();
                //while (entriesEnumerator.MoveNext())
                //{
                //    result.response.Key = entriesEnumerator.Current.Key.ToStringUtf8();
                //    result.response.Value = entriesEnumerator.Current.HValue.ToStringUtf8();
                //}
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.response = null;
            }
            return result;
        }

        public async Task<(Pocos.RpcStatus status, List<string> tables)> Tables()
        {

            (Pocos.RpcStatus status, List<string> tables) result;

            try
            {
                using var cts = new CancellationTokenSource();
                var rpcResponse = this.client.ListTablesAsync(new Empty(), this.AuthHeader, null, cts.Token);
                var rs = await rpcResponse.ResponseAsync;
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = rpcResponse.GetStatus().StatusCode.ToPocoStatusCode(),
                    Detail = rpcResponse.GetStatus().Detail
                };
                result.tables = new List<string>();
                foreach (Row r in rs.Rows)
                {
                    foreach (SQLValue val in r.Values)
                    {
                        result.tables.Add(val.S.ToString());
                    }
                }
            }
            catch (RpcException ex)
            {
                result.status = new Pocos.RpcStatus
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
                result.tables = null;
            }
            return result;
        }

        public async Task<Pocos.RpcStatus> SQLExec(string sql)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(sql);

            Pocos.RpcStatus rpcStatus;

            try
            {
                var sqlExecRequest = new SQLExecRequest();
                sqlExecRequest.Sql = sql;
                using var cts = new CancellationTokenSource();
                var sqlExecResult = await this.client.SQLExecAsync(sqlExecRequest, this.AuthHeader, null, cts.Token);

                rpcStatus = new Pocos.RpcStatus()
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
            }
            catch (RpcException ex)
            {
                rpcStatus = new Pocos.RpcStatus()
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
            }
            return rpcStatus;
        }

        //public async Task<(Pocos.RpcStatus status, List<string> tables)> SQLQuery(string sql)
        public async Task<Pocos.RpcStatus> SQLQuery(string sql)
        {
            var validator = new StringValidator();
            validator.ValidateAndThrow(sql);

            Pocos.RpcStatus status;
            //(Pocos.RpcStatus status, List<string> tables) result;
            try
            {
                var sqlQueryRequest = new SQLQueryRequest();
                sqlQueryRequest.Sql = sql;
                using var cts = new CancellationTokenSource();
                var sqlQueryResult = await this.client.SQLQueryAsync(sqlQueryRequest, this.AuthHeader, null, cts.Token);
                
                for (int i = 0; i < sqlQueryResult.Rows.Count; i++)
                {
                    var rowVals = sqlQueryResult.Rows[i].Values;
                    for (int j = 0; j < rowVals.Count; j++)
                    {
                        var val = rowVals[j];
                        switch (val.ValueCase)
                        {
                            case SQLValue.ValueOneofCase.None:
                                break;
                            case SQLValue.ValueOneofCase.Null:
                                break;
                            case SQLValue.ValueOneofCase.N:
                                break;
                            case SQLValue.ValueOneofCase.S:
                                break;
                            case SQLValue.ValueOneofCase.B:
                                break;
                            case SQLValue.ValueOneofCase.Bs:
                                break;
                            default:
                                break;
                        }
                        
                    }
                }
                

                status = new Pocos.RpcStatus()
                {
                    StatusCode = Pocos.StatusCode.OK,
                    Detail = string.Empty
                };
            }
            catch (RpcException ex)
            {
                status = new Pocos.RpcStatus()
                {
                    StatusCode = ex.StatusCode.ToPocoStatusCode(),
                    Detail = ex.Status.Detail
                };
            }
            //return sqlQueryResult;
            return status;
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
                //catch all when we called it from dispose
                if (!this.disposedValue)
                {
                    throw;
                }
                else
                {
                    throw;
                }
            }
        }
        protected virtual void Dispose(bool _disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                this.Close();
            }
        }
        ~ImmuDbClient()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        //public async Task<List<(ulong Tx, string Key, string Value)>> GetAll(List<string> keys)
        //{
        //    var result = new List<(ulong Tx, string Key, string Value)>();
        //    var klr = new KeyListRequest();
        //    keys.ForEach(key => klr.Keys.Add(ByteString.CopyFromUtf8(key)));
        //    var cts = new CancellationTokenSource(15000);
        //    var entries = await this.client.GetAllAsync(klr, this.AuthHeader, null, cts.Token);
        //    foreach (var e in entries.Entries_)
        //    {
        //        result.Add((e.Tx, e.Key.ToStringUtf8(), e.Value.ToStringUtf8()));
        //    }
        //    return result;
        //}
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







