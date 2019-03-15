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
using System.IO.Compression;

namespace Adf.Cs
{
    /// <summary>
    /// Session
    /// </summary>
    internal class ServerSession2 : IDisposable
    {
        readonly static Type STRING_TYPE = typeof(string);
        readonly Socket socket;
        readonly ServerSocketState receiveCommandState;
        readonly LogManager logManager;
        readonly LogWriter logMessage;
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
        string id;
        /// <summary>
        /// get Session Id
        /// </summary>
        public string Id
        {
            get { return this.id; }
        }
        string clientIp;
        /// <summary>
        /// get Client Ip
        /// </summary>
        public string ClientIp
        {
            get { return this.clientIp; }
        }
        int clientPort;
        /// <summary>
        /// get Client Port
        /// </summary>
        public int ClientPort
        {
            get { return this.clientPort; }
        }

        byte protocolVersion;
        /// <summary>
        /// 协议版本
        /// </summary>
        public byte ProtocolVersion
        {
            get { return this.protocolVersion; }
        }
        
        /// <summary>
        /// 初始化新实例
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="logManager"></param>
        /// <param name="headBuffer"></param>
        public ServerSession2(Socket socket, LogManager logManager, byte[] headBuffer)
        {
            this.logManager = logManager;
            this.logMessage = logManager.Message;
            this.socket = socket;
            //
            //reserve1(byte) + reserve2(byte) + reserve3(byte) + Protocol Version(byte)
            this.protocolVersion = headBuffer[3];
            //
            this.begin = DateTime.Now;
            //
            var endPoint = (IPEndPoint)socket.RemoteEndPoint;
            this.clientIp = endPoint.Address.ToString();
            this.clientPort = endPoint.Port;
            //
            this.id = this.ClientIp + ":" + this.ClientPort;
            //
            if (logMessage.Enable)
            {
                logMessage.WriteTimeLine("{0}: New Session, Protocol Version:{1}",
                    this.id, protocolVersion);
            }
            //
            this.receiveCommandState = new ServerSocketState(socket, 1);
        }

        /// <summary>
        /// 读取命令
        /// </summary>
        public void ReceiveCommand()
        {
            try
            {
                socket.BeginReceive(this.receiveCommandState.Buffer, 0, 1, SocketFlags.None, this.ReceiveCommandCallback, null);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                this.Dispose();
            }
            catch (Exception)
            {
                this.Dispose();
            }
        }

        /// <summary>
        /// 异步读取
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCommandCallback(IAsyncResult ar)
        {
            var count = 0;
            try
            {
                count = this.socket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception)
            {
                this.Dispose();
                return;
            }

            //socket closed
            if (count == 0)
            {
                this.Dispose();
                return;
            }

            //execute command
            if (this.isDispose == false)
            {
                try
                {
                    this.NewCommand(this.socket, this.receiveCommandState.Buffer[0]);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException exception)
                {
                    if (exception.SocketErrorCode == SocketError.TimedOut)
                    {
                        if (this.logMessage.Enable)
                        {
                            this.logMessage.WriteTimeLine("{0}: Timeout", this.id);
                        }
                    }
                    this.Dispose();
                    return;
                }
                catch (Exception exception)
                {
                    this.logManager.Exception(exception);
                    this.Dispose();
                    return;
                }
            }

            //new receive
            if (this.isDispose == false)
            {
                this.ReceiveCommand();
            }
        }

        /// <summary>
        /// new command
        /// </summary>
        /// <param name="parameterLength"></param>
        /// <param name="socket"></param>
        private void NewCommand(Socket socket, byte parameterLength)
        {
            //Stopwatch stopwatch = null;
            int commandStart = 0;
            if (this.logMessage.Enable)
            {
                commandStart = Environment.TickCount;
                //stopwatch = Stopwatch.StartNew();
            }

            //exec
            var serverName = (string)CsSocket.Receive(socket, STRING_TYPE);
            var commandName = (string)CsSocket.Receive(socket, STRING_TYPE);

            //invloke
            try
            {
                this.NewCommandInvoke(socket, parameterLength, serverName, commandName, commandStart);
            }
            catch (Exception exception)
            {
                throw new CsInvokeException(serverName, commandName, exception);
            }
        }

        private void NewCommandInvoke(Socket socket, byte parameterLength, string serverName, string commandName, int commandStart)
        {
            object value = null;
            object[] parameters = null;

            var commandInfo = ServerCommand.GetCommand(serverName, commandName);
            if (commandInfo == null)
                value = string.Format("Not Find Command:{0}.{1}", serverName, commandName);

            else if (commandInfo.Parameters.Length != parameterLength)
                value = string.Format("{0}.{1} Client Command  Parameters And Server Parameters No Match", serverName, commandName);
            else
            {
                //get parameter
                if (parameterLength > 0)
                {
                    parameters = new object[parameterLength];
                    for (var i = 0; i < parameterLength; i++)
                    {
                        if (commandInfo.Parameters[i].IsOut)
                        {
                            CsSocket.Receive(socket);
                        }
                        else
                        {
                            parameters[i] = CsSocket.Receive(socket, commandInfo.Parameters[i].ParameterType);
                        }
                    }
                }
            }

            //cache
            byte[] cacheBuffer = null;
            String cacheVersion = null, cacheKey = null;
            var cacheType = ServerCacheType.NoCache;
            ServerConfigItem configItem = null;
            bool cacheEnable = false;
            //no error & get cache
            if (value == null)
            {
                if (ServerCache.Enable)
                {
                    configItem = ServerConfig.GetItem(commandInfo.Server, commandInfo.Name);
                    cacheEnable = configItem != null && configItem.CacheExpires > 0;
                }

                //cache get
                if (cacheEnable)
                {
                    //mode = version
                    if (configItem.CacheVersion != null)
                    {
                        try
                        {
                            cacheVersion = ServerCache.Cache.Get(configItem.CacheVersion);
                            if (cacheVersion != null && cacheVersion != "")
                            {
                                //get cache
                                cacheKey = ServerCache.BuildKey(configItem.CacheKey, cacheVersion, parameters);
                                //value = ServerCache.Cache.Get(cacheKey, commandInfo.Method.ReturnType);
                                cacheBuffer = ServerCache.Cache.Get<byte[]>(cacheKey);
                                cacheType = cacheBuffer != null ? ServerCacheType.Cached : ServerCacheType.NoCache;
                            }
                            else
                            {
                                //no version , set no cache
                                cacheEnable = false;
                            }
                        }
                        catch (Exception e)
                        {
                            this.logManager.Exception(e);
                        }
                    }
                    else
                    {
                        //get cache
                        cacheKey = ServerCache.BuildKey(configItem.CacheKey, null, parameters);
                        try
                        {
                            //value = ServerCache.Cache.Get(cacheKey, commandInfo.Method.ReturnType);
                            cacheBuffer = ServerCache.Cache.Get<byte[]>(cacheKey);
                            cacheType = cacheBuffer != null ? ServerCacheType.Cached : ServerCacheType.NoCache;
                        }
                        catch (Exception e)
                        {
                            this.logManager.Exception(e);
                        }
                    }
                }
            }

            var result = false;
            //cached
            if (cacheType == ServerCacheType.Cached)
            {
                socket.Send(cacheBuffer);
                result = true;
            }
            //no error ,invoke
            else if (value == null)
            {
                //var invokeStart = DateTime.Now;
                try
                {
                    value = commandInfo.Method.Invoke(commandInfo.Instance, parameters);
                    result = true;
                }
                catch (TargetInvocationException exception)
                {
                    result = false;
                    this.logManager.Exception(exception.GetBaseException());
                    value = exception.GetBaseException().Message;
                }
                catch (Exception exception)
                {
                    result = false;
                    this.logManager.Exception(exception);
                    value = exception.Message;
                }
                //invokeSeconds = DateTime.Now.Subtract(invokeStart).TotalSeconds;

                //build  value Buffer
                if (result)
                {
                    using (var m = new MemoryStream())
                    {
                        //write result
                        m.WriteByte((byte)CsResult.Success);

                        //send out parameter length

                        //buffer parameter length , parameter index + parameter length + parameter body + bodylength + body
                        //parameter count writer
                        m.WriteByte(commandInfo.OutsIndexLength);
                        //parameter write
                        if (commandInfo.OutsIndexLength > 0)
                        {
                            for (byte i = 0, l = commandInfo.OutsIndexLength; i < l; i++)
                            {
                                //write index
                                m.WriteByte(commandInfo.OutsIndex[i]);
                                //write parameter
                                CsSocket.WriteDataItem(m, parameters[commandInfo.OutsIndex[i]]);
                            }
                        }
                        //body
                        CsSocket.WriteDataItem(m, value);

                        //build value buffer completed

                        //svae value buffer to cache
                        //if (result == CsResult.Success && cacheType == CacheType.NoCache && hasContent && commandInfo.CacheExpires > 0)
                        if (cacheEnable && cacheType == ServerCacheType.NoCache)
                        {
                            cacheBuffer = m.ToArray();
                            try
                            {
                                //set cache
                                ServerCache.Cache.Set(cacheKey, cacheBuffer, configItem.CacheExpires);
                                cacheType = ServerCacheType.NewCache;
                            }
                            catch (Exception e)
                            {
                                this.logManager.Exception(e);
                            }
                        }

                        //delete cache
                        if (ServerCache.Enable && cacheEnable && configItem.CacheDeletes != null)
                        {
                            this.DeleteCache(commandInfo, configItem, parameters);
                        }

                        //send value
                        if (cacheType == ServerCacheType.NewCache)
                        {
                            socket.Send(cacheBuffer);
                        }
                        else
                        {
                            socket.Send(m.GetBuffer(), 0, (int)m.Length, SocketFlags.None);
                        }
                    }
                }
            }
            else
            {
                //parameter error
                result = false;
                //value = "parameter error";
            }

            //send error
            if (result == false)
            {
                using (var m = new MemoryStream())
                {
                    m.WriteByte((byte)CsResult.Fail);
                    //write failure message
                    CsSocket.WriteDataItem(m, (string)value);
                    //
                    socket.Send(m.GetBuffer(), 0, (int)m.Length, SocketFlags.None);
                }
            }

            //
            if (this.logMessage.Enable)
            {
                //this.logMessage.WriteTimeLine("{0}: {1}.{2}:{3}, Cache:{4}, InvokeSeconds:{5},Seconds:{6}",
                //    this.id, serverName, commandName, result ? "Success" : "Failure", cacheType, invokeSeconds, DateTime.Now.Subtract(start).TotalSeconds);
                //stopwatch.Stop();
                //var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                var elapsedMilliseconds = Environment.TickCount - commandStart;

                //this.logMessage.WriteTimeLine("{0}: {1}.{2}:{3}, Cache:{4}, Elapsed:{5}",
                //    this.id, serverName, commandName, result ? "Success" : "Failure", cacheType, stopwatch.Elapsed.TotalSeconds);
                this.logMessage.WriteTimeLine("{0}\t{1}\t{2}.{3}\t{4}\t{5}",
                    this.id
                    , cacheType
                    , serverName
                    , commandName
                    , elapsedMilliseconds
                    , (result ? "OK" : (string)value));
            }
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
                        this.logManager.Exception(new CsException("Not Find Cache Config {0}.{1} By {2}.{3} Cache Delete"
                            , cacheDelete.Server, cacheDelete.Command
                            , commandInfo.Server, commandInfo.Name
                        ));
                        continue;
                    }
                    else if (null != deleteCommandConfig.CacheVersion)
                    {
                        var version = ServerCache.GetVersion();
                        //update version
                        ServerCache.Cache.Set(deleteCommandConfig.CacheVersion, version, ServerConfig.GetVersionExpires(commandInfo.Server, deleteCommandConfig.CacheVersion));

                        if (ServerCache.LogWriter.Enable)
                        {
                            ServerCache.LogWriter.WriteTimeLine("Delete Cache Version: " + deleteCommandConfig.CacheVersion);
                        }
                    }
                    else
                    {
                        //delete
                        deleteDatas = this.GetDeleteParametersDatas(cacheDelete, parameters);

                        var cacheKey = ServerCache.BuildKey(deleteCommandConfig.CacheKey, deleteDatas);
                        ServerCache.Cache.Delete(cacheKey);

                        if (ServerCache.LogWriter.Enable)
                        {
                            ServerCache.LogWriter.WriteTimeLine("Delete Cache: " + cacheKey);
                        }
                    }
                }
            }
            catch (CsException)
            {
                throw;
            }
            catch (Exception e)
            {
                this.logManager.Exception(e);
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
            var count = cacheDelete.ParameterIndexs.Count;
            var parames = new object[count];
            for (int i = 0; i < count; i++)
            {
                parames[i] = parameters[i];
            }
            return parames;
            //var deleteParameters = new ArrayList(cacheDelete.ParameterIndexs.Count);
            //cacheDelete.ParameterIndexs.ForEach(index =>
            //{
            //    deleteParameters.Add(parameters[index]);
            //});
            //return deleteParameters.ToArray();
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
                if (this.logMessage.Enable)
                {
                    this.logMessage.WriteTimeLine("{0}: Close Session", this.id);
                }
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
    }
}