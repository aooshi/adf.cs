using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Reflection;

namespace Adf.Cs
{
    /// <summary>
    /// Session
    /// </summary>
    public class ServerSession : IDisposable
    {
        readonly object lockObject = new object();
        readonly static Type STRING_TYPE = typeof(string);
        readonly static Type CsInfoType = typeof(CsInfo);
        readonly Socket socket;
        readonly ServerListen listen;
        readonly ServerLogger serverLogger;
        readonly DateTime begin;

        /// <summary>
        /// disposed event
        /// </summary>
        public event EventHandler Disposed = null;


        bool isDispose = false;
        /// <summary>
        /// get is invoke dispose
        /// </summary>
        public bool IsDispose
        {
            get
            {
                return this.isDispose;
            }
        }

        /// <summary>
        /// Session Id
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Client Ip
        /// </summary>
        public string ClientIp
        {
            get;
            set;
        }

        /// <summary>
        /// Client Port
        /// </summary>
        public int ClientPort
        {
            get;
            set;
        }

        /// <summary>
        /// Get Client Info
        /// </summary>
        public CsInfo ClientInfo
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="listen"></param>
        /// <param name="logger"></param>
        /// <param name="clientInfo"></param>
        internal ServerSession(Socket socket, ServerLogger logger, ServerListen listen, CsInfo clientInfo)
        {
            this.listen = listen;
            this.socket = socket;
            //
            this.serverLogger = logger;
            this.begin = DateTime.Now;
            //
            var endPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.ClientIp = endPoint.Address.ToString();
            this.ClientPort = endPoint.Port;
            //
            this.Id = this.ClientIp + ":" + this.ClientPort;
            //
            this.ClientInfo = clientInfo;
            //
            this.serverLogger.NewSession(this);
        }

        /// <summary>
        /// 读取命令
        /// </summary>
        public void ReceiveCommand()
        {
            var state = new ServerSocketState(this.socket, CsSocket.LENGTH_BUFFER_SIZE);
            try
            {
                socket.BeginReceive(state.Buffer, state.Offset, state.BufferSize, SocketFlags.None, this.ReceiveCommandCallback, state);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                this.Dispose();
            }
            catch (Exception exception)
            {
                this.serverLogger.SessionException(this, exception);

                this.Dispose();
            }
        }

        /// <summary>
        /// 异步读取
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCommandCallback(IAsyncResult ar)
        {
            var state = (ServerSocketState)ar.AsyncState;
            var count = 0;
            var unread = 0;
            try
            {
                count = state.Socket.EndReceive(ar);
                unread = state.SetOffset(count);
                //读取余量
                if (count > 0 && unread > 0)
                {
                    Adf.SocketHelper.Receive(state.Socket, unread, state.Buffer, state.Offset);
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException exception)
            {
                if (exception.SocketErrorCode == SocketError.TimedOut)
                {
                    this.serverLogger.LeisureTimeout(this);
                }
                this.Dispose();
            }
            catch (Exception exception)
            {
                this.serverLogger.SessionException(this, exception);
                this.Dispose();
            }

            //execute command
            if (this.isDispose == false)
            {
                this.ParseCommand(state.Socket, state.Buffer);
            }

            //new receive
            if (this.isDispose == false)
            {
                this.ReceiveCommand();
            }
        }

        /// <summary>
        /// 新命令解析
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="lengthBuffer"></param>
        private void ParseCommand(Socket socket, byte[] lengthBuffer)
        {
            var parameterLength = BitConverter.ToInt32(lengthBuffer, 0);
            try
            {
                this.NewCommand(socket, parameterLength);
            }
            catch (ObjectDisposedException)
            {
                this.Dispose();
            }
            catch (SocketException exception)
            {
                if (exception.SocketErrorCode == SocketError.TimedOut)
                {
                }
                this.Dispose();
            }
            catch (Exception exception)
            {
                this.serverLogger.SessionException(this, exception);
                this.Dispose();
            }
        }

        /// <summary>
        /// new command
        /// </summary>
        /// <param name="parameterLength"></param>
        /// <param name="socket"></param>
        private void NewCommand(Socket socket, int parameterLength)
        {
            object value = null;
            byte[] valueBuffer = null;
            var serverName = string.Empty;
            var commandName = string.Empty;
            var result = CsResult.Fail;
            var begin = DateTime.MaxValue;
            var end = DateTime.MaxValue;
            double invokeSeconds = 0;
            var start = DateTime.Now;
            var cacheType = ServerCacheType.NoCache;
            //exec
            serverName = (string)this.Receive(socket, STRING_TYPE, true);
            commandName = (string)this.Receive(socket, STRING_TYPE, true);

            var commandInfo = ServerCommand.GetCommand(serverName, commandName);
            if (commandInfo == null)
                value = string.Format("Not Find Command:{0}.{1}", serverName, commandName);
            // throw new NotFindCommandException(nameSpace, commandName);

            else if (commandInfo.Parameters.Length != parameterLength)
                value = string.Format("{0}.{1} Parameter Legnth <> Invoke Parameter Length", serverName, commandName);
            //throw new ApplicationException(string.Format("{0}.{1} Parameter Legnth <> Invoke Parameter Length", nameSpace, commandName));

            object[] parameters = null;
            //no error
            if (value == null)
            {
                //get parameter
                if (parameterLength > 0)
                {
                    parameters = new object[parameterLength];
                    for (var i = 0; i < parameterLength; i++)
                    {
                        if (commandInfo.Parameters[i].IsOut)
                        {
                            this.Receive(socket, commandInfo.Parameters[i].ParameterType, false);
                        }
                        else
                        {
                            parameters[i] = this.Receive(socket, commandInfo.Parameters[i].ParameterType, true);
                            if (ReceiveParameterError.ERROR.Equals(parameters[i]))
                            {
                                value = "Parameter Error";
                            }
                        }
                    }
                }

                //
                if (value != null)
                {
                    //parameter error
                    result = CsResult.Fail;
                }
                else
                {
                    //commandStopwatch.Start();
                    var invokeStart = DateTime.Now;
                    try
                    {
                        result = this.Invoke(commandInfo, parameters, out valueBuffer, out cacheType);
                    }
                    catch (TargetInvocationException exception)
                    {
                        result = CsResult.Fail;
                        this.serverLogger.CommandException(serverName, commandName, exception);
                        value = exception.GetBaseException().Message;
                    }
                    catch (Exception exception)
                    {
                        result = CsResult.Fail;
                        this.serverLogger.CommandException(serverName, commandName, exception);
                        value = exception.Message;
                    }
                    //commandStopwatch.Stop();
                    invokeSeconds = DateTime.Now.Subtract(invokeStart).TotalSeconds;
                }
            }
            else
            {
                //read parameters data
                if (parameterLength > 0)
                {
                    for (var i = 0; i < parameterLength; i++)
                    {
                        //length
                        var length = BitConverter.ToInt32(SocketHelper.Receive(socket, CsSocket.LENGTH_BUFFER_SIZE), 0);
                        //body
                        using (var m = SocketHelper.Receive(socket, length, CsSocket.BODY_BUFFER_SIZE))
                        {
                            m.Close();
                        }
                    }
                }
            }

            //send
            using (var stream = new MemoryStream(CsSocket.BODY_BUFFER_SIZE))
            {
                //send length
                stream.Write(BitConverter.GetBytes((int)result), 0, CsSocket.LENGTH_BUFFER_SIZE);

                //send out parameter length
                if (result == CsResult.Success)
                {
                    stream.Write(valueBuffer, 0, valueBuffer.Length);
                }
                else
                {
                    //error output
                    using (var m = new MemoryStream(CsSocket.BODY_BUFFER_SIZE))
                    {
                        CsSerializer.Serialize(m, value);
                        stream.Write(BitConverter.GetBytes((int)m.Length), 0, CsSocket.LENGTH_BUFFER_SIZE);
                        m.WriteTo(stream);
                    }
                }
                socket.Send(stream.GetBuffer(), 0, (int)stream.Length, SocketFlags.None);
            }
            this.serverLogger.Command(this, serverName, commandName, result == CsResult.Success, cacheType, invokeSeconds, DateTime.Now.Subtract(start).TotalSeconds);
        }

        /// <summary>
        /// invoke
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <param name="parameters"></param>
        /// <param name="valueBuffer"></param>
        /// <param name="cacheType"></param>
        /// <returns></returns>
        private CsResult Invoke(CommandInfo commandInfo, object[] parameters, out byte[] valueBuffer, out ServerCacheType cacheType)
        {
            /*
             * this method ,try catch, safe cache error
             */

            //variable init
            string cacheKey = null;
            string cacheVersion = null;
            object invokeValue = null;
            var result = CsResult.Fail;
            //bool hasContent = false;

            //
            ServerConfigItem configItem = null;
            bool isCache = false;
            if (ServerCache.Enable)
            {
                configItem = ServerConfig.GetItem(commandInfo.Server, commandInfo.Name);
                isCache = configItem != null && configItem.CacheExpires > 0;
            }

            //init out
            valueBuffer = null;
            cacheType = ServerCacheType.NoCache;

            //cache get
            if (isCache)
            {
                //mode = version
                if (configItem.CacheVersion != null)
                {
                    cacheVersion = ServerCache.Cache.Get(configItem.CacheVersion);
                }
                //get cache
                cacheKey = ServerCache.BuildKey(configItem.CacheKey, cacheVersion, parameters);

                try
                {
                    valueBuffer = ServerCache.Cache.Get<byte[]>(cacheKey);
                    result = CsResult.Success;
                }
                catch (Exception e)
                {
                    this.serverLogger.CommandCacheException(commandInfo.Server, commandInfo.Name, e);
                }

                cacheType = valueBuffer != null && result == CsResult.Success ? ServerCacheType.Cached : ServerCacheType.NoCache;
            }

            //invoke
            if (valueBuffer == null)
            {
                cacheType = ServerCacheType.NoCache;
                result = CsResult.Success;

                //no ObjectCache
                if (invokeValue == null)
                    invokeValue = commandInfo.Method.Invoke(commandInfo.Instance, parameters);

                //是否有内容
                //hasContent = invokeValue != null;

                //buffer = bodylength+body, parameter index + parameter length + parameter

                using (var m = new MemoryStream(CsSocket.BODY_BUFFER_SIZE))
                {
                    //body
                    using (var m2 = new MemoryStream(CsSocket.BODY_BUFFER_SIZE))
                    {
                        if (invokeValue == null)
                        {
                            //length
                            m.Write(BitConverter.GetBytes((int)-1), 0, CsSocket.LENGTH_BUFFER_SIZE);
                        }
                        else
                        {
                            CsSerializer.Serialize(m2, invokeValue);
                            //length
                            m.Write(BitConverter.GetBytes((int)m2.Length), 0, CsSocket.LENGTH_BUFFER_SIZE);
                            //body
                            m2.WriteTo(m);
                        }
                    }
                    //parameters
                    // count
                    m.Write(BitConverter.GetBytes((int)commandInfo.OutsIndexLength), 0, CsSocket.LENGTH_BUFFER_SIZE);
                    using (var m2 = new MemoryStream(CsSocket.BODY_BUFFER_SIZE))
                    {
                        for (int i = 0, index = 0; i < commandInfo.OutsIndexLength; i++)
                        {
                            m2.SetLength(0);
                            m2.Position = 0;
                            //
                            index = (int)commandInfo.OutsIndex[i];
                            //index
                            m.Write(BitConverter.GetBytes(index), 0, CsSocket.LENGTH_BUFFER_SIZE);
                            //
                            if (parameters[index] == null)
                            {
                                //length
                                m.Write(BitConverter.GetBytes((int)-1), 0, CsSocket.LENGTH_BUFFER_SIZE);
                            }
                            else
                            {
                                CsSerializer.Serialize(m2, parameters[index]);
                                //length
                                m.Write(BitConverter.GetBytes((int)m2.Length), 0, CsSocket.LENGTH_BUFFER_SIZE);
                                //body
                                m2.WriteTo(m);
                            }
                        }
                    }
                    //
                    valueBuffer = m.ToArray();
                }
            }

            //cache set
            //if (result == CsResult.Success && cacheType == CacheType.NoCache && hasContent && commandInfo.CacheExpires > 0)
            if (isCache && result == CsResult.Success && cacheType == ServerCacheType.NoCache)
            {
                try
                {
                    //set cache
                    ServerCache.Cache.Set(cacheKey, valueBuffer, configItem.CacheExpires);
                    cacheType = ServerCacheType.NewCache;
                }
                catch (Exception e)
                {
                    this.serverLogger.CommandCacheException(commandInfo.Server, commandInfo.Name, e);
                }
            }

            //delete cache
            if (ServerCache.Enable && isCache && configItem.CacheDeletes != null)
            {
                this.DeleteCache(commandInfo, configItem, parameters);
            }

            return result;
        }

        /// <summary>
        /// delete cache
        /// </summary>
        /// <param name="commandInfo"></param>
        /// <param name="parameters"></param>
        /// <param name="configItem"></param>
        private void DeleteCache(CommandInfo commandInfo, ServerConfigItem configItem, object[] parameters)
        {
            object[] deleteDatas = null;

            try
            {
                foreach (var cacheDelete in configItem.CacheDeletes)
                {
                    var deleteCommandConfig = ServerConfig.GetItem(cacheDelete.Server, cacheDelete.Command);
                    if (deleteCommandConfig == null)
                    {
                        this.serverLogger.CommandCacheDelete(commandInfo.Server, commandInfo.Name, cacheDelete.Server, cacheDelete.Command, "Not Find");
                        continue;
                    }
                    else if (null != deleteCommandConfig.CacheVersion)
                    {
                        var version = ServerCache.GetVersion();
                        //update version
                        ServerCache.Cache.Set(deleteCommandConfig.CacheVersion, version, ServerConfig.GetVersionExpires(commandInfo.Server, deleteCommandConfig.CacheVersion));
                    }
                    else
                    {
                        //delete
                        deleteDatas = this.GetDeleteParametersDatas(cacheDelete, parameters);

                        var cacheKey = ServerCache.BuildKey(deleteCommandConfig.CacheKey, deleteDatas);
                        ServerCache.Cache.Delete(cacheKey);
                    }

                }
            }
            catch (CsException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.serverLogger.CommandCacheException(commandInfo.Server, commandInfo.Name, e);
            }
        }

        /// <summary>
        /// 获取命令删除数据
        /// </summary>
        /// <param name="cacheDelete"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private object[] GetDeleteParametersDatas(ServerCacheDelete cacheDelete, object[] parameters)
        {
            var deleteParameters = new ArrayList(cacheDelete.ParameterIndexs.Count);
            cacheDelete.ParameterIndexs.ForEach(index =>
            {
                deleteParameters.Add(parameters[index]);
            });
            return deleteParameters.ToArray();
        }

        /// <summary>
        /// 读取
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="type"></param>
        /// <param name="deserialize"></param>
        /// <returns></returns>
        private object Receive(System.Net.Sockets.Socket socket, Type type, bool deserialize)
        {
            //length
            var length = BitConverter.ToInt32(SocketHelper.Receive(socket, CsSocket.LENGTH_BUFFER_SIZE), 0);
            //body
            using (var m = SocketHelper.Receive(socket, length, CsSocket.BODY_BUFFER_SIZE))
            {
                if (deserialize)
                {
                    try
                    {
                        return CsSerializer.Deserialize(type, m);
                    }
                    catch (Exception exception)
                    {
                        this.serverLogger.SessionException(this, exception);
                        return ReceiveParameterError.ERROR;
                    }
                }
                return null;
            }
        }


        /// <summary>
        /// 触发资源释放事件
        /// </summary>
        protected virtual void OnDisposed()
        {
            if (this.Disposed != null)
            {
                this.Disposed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            if (this.isDispose == false)
            {
                this.isDispose = true;
                this.serverLogger.SessionClose(this);
                //
                try
                {
                    this.socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                try
                {
                    this.socket.Close();
                }
                catch { }
                //
                this.OnDisposed();
            }
        }

        private struct ReceiveParameterError
        {
            public static readonly ReceiveParameterError ERROR = new ReceiveParameterError();
        }
    }
}