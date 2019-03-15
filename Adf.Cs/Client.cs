using System;
using System.Collections.Generic;
using System.Text;
using Adf.Config;
using System.Configuration;
using System.Diagnostics;
using System.Net.Sockets;

namespace Adf.Cs
{
    /// <summary>
    /// 客户端
    /// </summary>
    /// <remarks>
    /// Cs:MemberThreadSize:    配置每成员默认线程数，默认800
    /// Cs:HashVNodeCount:      配置成员哈希应用时虚拟节点数，默认100
    /// </remarks>
    public abstract class Client : IDisposable
    {
        static int MEMBER_THREADSIZE = Adf.ConfigHelper.GetSettingAsInt("Cs:MemberThreadSize", 800);
        static int HASH_VNODE_COUNT = Adf.ConfigHelper.GetSettingAsInt("Cs:HashVNodeCount", 100);

        int socketErrorRetry = 3;
        /// <summary>
        /// Socket Error Max Retry, Default 3,  此属性会强制基础 Pool.Retry 属性为0
        /// </summary>
        public int SocketErrorRetry
        {
            get { return this.socketErrorRetry; }
            set { this.socketErrorRetry = value; }
        }

        /// <summary>
        /// 每成员线程数, 默认800，配置： Cs:MemberThreadSize
        /// </summary>
        public virtual int PoolMemberThreadSize
        {
            get
            {
                return MEMBER_THREADSIZE;
            }
        }

        Pool<ClientSession> socketPool;
        //List<Pool<ClientSession>> obsoleteSocketPoolList;

        string server;
        /// <summary>
        /// 获取当前实例服务名
        /// </summary>
        public string Server
        {
            get { return this.server; }
        }

        /// <summary>
        /// 可用会话数
        /// </summary>
        public int AvailableSession
        {
            get
            {
                return this.socketPool.ActiveCount;
            }
        }

        /// <summary>
        /// 正运行的会话数
        /// </summary>
        public int RuningSession
        {
            get
            {
                return this.socketPool.RuningCount;
            }
        }

        int timeout = 30000;
        /// <summary>
        /// 获取或设置请求超时超时值（以毫秒为单位）。默认值为 30000，指定 -1 还会指示超时期限无限大。
        /// </summary>
        public int Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        LogManager logManager;
        /// <summary>
        /// get log manage handler
        /// </summary>
        public LogManager LogManager
        {
            get { return this.logManager; }
        }

        /// <summary>
        /// 根据配置节初始化实例
        /// </summary>
        /// <param name="server">服务名</param>
        /// <param name="configName">配置节点名</param>
        protected Client(string server, string configName)
        {
            this.name = this.GetType().Name;
            this.server = server;
            this.logManager = new LogManager(this.Name);
            //
            var section = System.Configuration.ConfigurationManager.GetSection(configName);
            if (section == null)
                throw new ConfigurationErrorsException("no find config section " + configName);
            //
            //IpGroupSection config;
            //if (section is CsRegistrySection)
            //{
            //    config = (CsRegistrySection)section;
            //    this.registry = new CsRegistry(config);
            //    this.registry.NodeChanged += this.RegistryNodeChanged;
            //    //
            //    var nodes = this.registry.GetNodes();
            //    var ipcount = nodes.Length;

            //    if (ipcount == 0)
            //    {
            //        throw new ConfigurationErrorsException("no active node from registry " + config.SectionInformation.Name);
            //    }

            //    var clientSessionPoolItems = new ClientSessionPoolMember[ipcount];
            //    for (int i = 0; i < ipcount; i++)
            //        clientSessionPoolItems[i] = new ClientSessionPoolMember(nodes[i].Host, nodes[i].Port);
            //    //
            //    this.socketPool = new Pool<ClientSession>(this.PoolMemberThreadSize, clientSessionPoolItems, HASH_VNODE_COUNT);
            //    this.socketPool.NewInstanceException += this.socketPool_NewInstanceException;

            //    //
            //    if (this.logManager.Message.Enable)
            //    {
            //        this.logManager.Message.WriteTimeLine("registry load node: " + Adf.ConvertHelper.ArrayToString<CsRegistryNode>(nodes, ",", n => { return n.Host + ":" + n.Port; }));
            //    }
            //}
            //else
            //{
            var config = (IpGroupSection)section;
            //
            var ipcount = config.IpList.Count;
            if (ipcount == 0)
            {
                throw new ConfigurationErrorsException("no config active member from " + config.SectionInformation.Name);
            }
            var clientSessionPoolItems = new ClientSessionPoolMember[ipcount];
            for (int i = 0; i < ipcount; i++)
                clientSessionPoolItems[i] = new ClientSessionPoolMember(config.IpList[i].Ip, config.IpList[i].Port);
            //
            this.socketPool = new Pool<ClientSession>(this.PoolMemberThreadSize, clientSessionPoolItems, config.Hash);
            //禁止pool的重试机制
            this.socketPool.Retry = 0;
            //}
        }

        ////创建新实例发生异常时 & 仅 registry 时注册
        //private void socketPool_NewInstanceException(object sender, PoolNewInstanceExceptionEventArgs e)
        //{
        //    //触发节点更新
        //    this.registry.Update();
        //}

        ////注册服务节点发生变更时
        //private void RegistryNodeChanged(object sender, EventArgs e)
        //{
        //    var hash = this.socketPool.HashVNodeCount;
        //    var nodes = this.registry.GetNodes();
        //    var ipcount = nodes.Length;
        //    var clientSessionPoolItems = new ClientSessionPoolMember[ipcount];
        //    for (int i = 0; i < ipcount; i++)
        //        clientSessionPoolItems[i] = new ClientSessionPoolMember(nodes[i].Host, nodes[i].Port);

        //    //
        //    if (this.obsoleteSocketPoolList == null)
        //        this.obsoleteSocketPoolList = new List<Pool<ClientSession>>();

        //    lock (this.obsoleteSocketPoolList)
        //        this.obsoleteSocketPoolList.Add(this.socketPool);
        //    var disposePool = this.socketPool;

        //    //use new ppol
        //    this.socketPool = new Pool<ClientSession>(this.PoolMemberThreadSize, clientSessionPoolItems, HASH_VNODE_COUNT);
        //    this.socketPool.NewInstanceException += this.socketPool_NewInstanceException;

        //    //log
        //    if (this.logManager.Message.Enable)
        //    {
        //        this.logManager.Message.WriteTimeLine("registry load node: " + Adf.ConvertHelper.ArrayToString<CsRegistryNode>(nodes, ",", n => { return n.Host + ":" + n.Port; }));
        //    }

        //    //旧有节点资源释放
        //    System.Threading.ThreadPool.QueueUserWorkItem(state =>
        //    {
        //        while (disposePool.RuningCount > 0)
        //        {
        //            System.Threading.Thread.Sleep(1000);
        //        }
        //        try
        //        {
        //            disposePool.Dispose();
        //        }
        //        catch { }
        //        finally
        //        {
        //            lock (this.obsoleteSocketPoolList)
        //                this.obsoleteSocketPoolList.Remove(disposePool);
        //        }
        //    });
        //}

        /// <summary>
        /// 为单个主机初始化实例
        /// </summary>
        /// <param name="server">服务名</param>
        /// <param name="hostOrIp">主机</param>
        /// <param name="port">端口</param>
        protected Client(string server, string hostOrIp, int port)
        {
            this.name = this.GetType().Name;
            this.server = server;
            this.logManager = new LogManager(this.Name);
            //
            var clientSessionPoolItems = new ClientSessionPoolMember[1];
            clientSessionPoolItems[0] = new ClientSessionPoolMember(hostOrIp, port);
            //
            this.socketPool = new Pool<ClientSession>(this.PoolMemberThreadSize, clientSessionPoolItems, 0);
        }

        /// <summary>
        /// 为一组主机初始化实例
        /// </summary>
        /// <param name="server">服务名</param>
        /// <param name="hostOrIps">主机,格式为：  ipOrHost:port,</param>
        /// <exception cref="ArgumentNullException">hostOrIps</exception>
        protected Client(string server, string[] hostOrIps)
        {
            this.name = this.GetType().Name;
            this.server = server;
            this.logManager = new LogManager(this.Name);
            if (hostOrIps == null || hostOrIps.Length == 0)
                throw new ArgumentNullException("hostOrIps");
            //
            var clientSessionPoolItems = new ClientSessionPoolMember[hostOrIps.Length];
            string[] item;
            int port;
            for (int i = 0, l = hostOrIps.Length; i < l; i++)
            {
                item = hostOrIps[i].Split(':');
                if (item.Length != 2)
                {
                    throw new ArgumentOutOfRangeException("hostOrIps", "format must be ipOrHost:port");
                }
                int.TryParse(item[1], out port);
                if (port == 0)
                {
                    throw new ArgumentOutOfRangeException("hostOrIps", "format must be ipOrHost:port");
                }
                clientSessionPoolItems[i] = new ClientSessionPoolMember(item[0], port);
            }
            //
            this.socketPool = new Pool<ClientSession>(this.PoolMemberThreadSize, clientSessionPoolItems, HASH_VNODE_COUNT);
        }

        string name;
        /// <summary>
        /// get client name
        /// </summary>
        /// <returns></returns>
        protected virtual string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// 执行一命令
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        protected T Command<T>(string command, params object[] parameters)
        {
            //if (this.socketPool.SupportHash)
            //{
            //    hashKey = command + "," + CsHelper.Join(",", parameters);
            //}
            //return this.Command<T>(command, hashKey, parameters);

            return this.Invoke<T>(command, null, parameters);
        }

        /// <summary>
        /// 执行一命令，并指定hashKey
        /// </summary>
        /// <param name="command"></param>
        /// <param name="hashKey"></param>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentNullException">hashKey</exception>
        protected T HashCommand<T>(string command, string hashKey, params object[] parameters)
        {
            if (hashKey == null)
                throw new ArgumentNullException("hashKey");

            return this.Invoke<T>(command, hashKey, parameters);
        }

        /// <summary>
        /// 调用命令
        /// </summary>
        /// <param name="command"></param>
        /// <param name="hashKey"></param>
        /// <param name="parameters"></param>
        private T Invoke<T>(string command, string hashKey, object[] parameters)
        {
            Stopwatch stopwatch = null;
            if (this.logManager.Message.Enable)
            {
                stopwatch = Stopwatch.StartNew();
            }

            T result = default(T);
            string serverIp = null;
            string sessionId = null;
            int serverPort = 0;
            //
            if (this.socketErrorRetry == 0)
            {
                this.socketPool.Call((ClientSession session) =>
                {
                    serverIp = session.ServerIp;
                    serverPort = session.ServerPort;
                    sessionId = session.Id;
                    try
                    {
                        session.Socket.ReceiveTimeout = this.timeout;
                        result = session.Invoke<T>(this.server, command, parameters);
                    }
                    catch (SocketException exception)
                    {
                        //socket error, abandon
                        session.PoolAbandon = true;
                        if (exception.SocketErrorCode == SocketError.TimedOut)
                        {
                            var exception2 = new CsTimeoutException(this.server, command, "timeout");
                            //this.logManager.Exception(exception2);
                            throw exception2;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }, hashKey, null);
            }
            else
            {
                var tryCount = 1;
                var called = false;
                do
                {
                    this.socketPool.Call((ClientSession session) =>
                    {
                        serverIp = session.ServerIp;
                        serverPort = session.ServerPort;
                        sessionId = session.Id;
                        try
                        {
                            session.Socket.ReceiveTimeout = this.timeout;
                            result = session.Invoke<T>(this.server, command, parameters);
                            called = true;
                        }
                        catch (SocketException exception)
                        {
                            //socket error, abandon
                            session.PoolAbandon = true;
                            if (exception.SocketErrorCode == SocketError.TimedOut)
                            {
                                var exception2 = new CsTimeoutException(this.server, command, "timeout");
                                //this.logManager.Exception(exception2);
                                throw exception2;
                            }
                            else
                            {
                                if (tryCount++ > this.socketErrorRetry)
                                {
                                    throw;
                                }
                            }
                        }
                    }, hashKey, null);
                }
                while (called == false);
            }

            if (this.logManager.Message.Enable)
            {
                stopwatch.Stop();
                //this.logManager.Message.WriteTimeLine("{0}: {1}.{2}, Elapsed:{3}s",
                //        sessionId, this.server, command, stopwatch.Elapsed.TotalSeconds);
                this.logManager.Message.WriteTimeLine("{0}\t{1}.{2}\t{3}",
                        sessionId, this.server, command, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            //if (this.obsoleteSocketPoolList != null)
            //{
            //    lock (this.obsoleteSocketPoolList)
            //    {
            //        while (this.obsoleteSocketPoolList.Count > 0)
            //        {
            //            try
            //            {
            //                this.obsoleteSocketPoolList[0].Dispose();
            //            }
            //            catch { }
            //            finally
            //            {
            //                this.obsoleteSocketPoolList.RemoveAt(0);
            //            }
            //        }
            //    }
            //}
            this.socketPool.Dispose();
            this.logManager.Dispose();
        }
    }
}