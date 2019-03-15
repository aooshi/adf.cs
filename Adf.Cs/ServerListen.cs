using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Configuration;
using System.Net;
using Timer = System.Timers.Timer;

namespace Adf.Cs
{
    /// <summary>
    /// 服务对象
    /// </summary>
    public class ServerListen : IDisposable
    {
        const int LISTEN_BACKLOG = 1024;

        readonly Socket Socket;
        /// <summary>
        /// 监听端口
        /// </summary>
        public readonly int Port;
        bool isDispose = false;

        /// <summary>
        /// 服务日志管理器
        /// </summary>
        ServerLogger serverLogger;

        readonly LogManager logManager;
        /// <summary>
        /// 获取当前日志管理器
        /// </summary>
        public LogManager LogManager
        {
            get { return this.logManager; }
        }

        readonly LogWriter connectionUnfinishedLogWriter;

        /// <summary>
        /// 应用名称
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime
        {
            get;
            private set;
        }

        object sessionCountLock = new object();
        int sessionCount = 0;
        /// <summary>
        /// 获取会话数
        /// </summary>
        public int SessionCount
        {
            get { return this.sessionCount; }
        }

        
        /// <summary>
        /// 以配置项中的Port设置做为端口初始新实例
        /// </summary>
        /// <param name="logManager"></param>
        /// <exception cref="ArgumentNullException">logManager</exception>
        public ServerListen(LogManager logManager)
            : this(logManager,0)
        {
        }

        /// <summary>
        /// 初始新实例
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="port"></param>
        /// <exception cref="ArgumentNullException">logManager</exception>
        public ServerListen(LogManager logManager,int port)
        {
            //config
            if (port == 0)
            {
                this.Port = int.Parse(ConfigurationManager.AppSettings["Port"]);
            }
            else
            {
                this.Port = port;
            }
            this.Name = ConfigHelper.AppName;

            //
            if (string.IsNullOrEmpty(this.Name))
                throw new ApplicationException("Not Config AppName");

            //
            if (logManager == null)
                throw new ArgumentNullException("logManager");

            //
            this.isDispose = false;
            this.StartTime = DateTime.Now;
            //init
            logManager.Message.WriteLine("Name:{0}", this.Name);
            logManager.Message.WriteLine("Port:{0}", this.Port);
            logManager.Message.WriteLine("Time:{0}", this.StartTime);
            this.logManager = logManager;
            this.connectionUnfinishedLogWriter = logManager.GetWriter("ConnectionUnfinished");
            //
            this.serverLogger = new ServerLogger(logManager);
            ServerCache.Init(logManager);

            //init context
            ServerCommand.Init(logManager);
            ServerConfig.Init(logManager, ServerCommand.Servers);

            //socket
            this.Socket = new Socket(AddressFamily.InterNetwork,
                 SocketType.Stream,
                  ProtocolType.Tcp);
            this.Socket.Bind(new IPEndPoint(IPAddress.Any, this.Port));

            //为了性能保证，服务暂不做连接数限制
            this.Socket.Listen(LISTEN_BACKLOG);

            this.Socket.BeginAccept(this.AcceptCallback, this.Socket);
        }

        /// <summary>
        /// new accept
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            var listenSocket = (Socket)ar.AsyncState;
            Socket socket = null;
            try
            {
                socket = listenSocket.EndAccept(ar);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                this.logManager.Exception(exception);
                throw;
            }

            //new accept
            if (this.isDispose == false)
            {
                this.Socket.BeginAccept(this.AcceptCallback, this.Socket);
            }

            if (socket != null)
            {
                try
                {
                    this.NewSession(socket);
                }
                catch (Exception)
                {
                    if (this.connectionUnfinishedLogWriter.Enable)
                    {
                        var endPoint = (IPEndPoint)socket.RemoteEndPoint;
                        this.connectionUnfinishedLogWriter.WriteTimeLine("{0}:{1}: New Connection Unfinished", endPoint.Address, endPoint.Port);
                    }
                    socket.Close();
                }
            }
        }

        /// <summary>
        /// new session
        /// </summary>
        /// <param name="socket"></param>
        private void NewSession(Socket socket)
        {
            var headBuffer = SocketHelper.Receive(socket, 4);
            var clientInfoDataLength = BitConverter.ToInt32(headBuffer, 0);
            if (clientInfoDataLength >= 0 && clientInfoDataLength < 65535)
            {
                //旧有版本使用的BitConvert.GetBytes序列化因此 根据旧版本长度不可能超过1000字节的依据，使用 >  [0,0,0,0] = 0 && < [255,255,0,0] = 65535

                CsInfo clientInfo = null;
                //body
                using (var m = SocketHelper.Receive(socket, clientInfoDataLength, CsSocket.BODY_BUFFER_SIZE))
                {
                    try
                    {
                        clientInfo = CsSerializer.Deserialize(typeof(CsInfo), m) as CsInfo;
                    }
                    catch (Exception)
                    {
                        socket.Close();
                        return;
                    }
                }
                //
                if (clientInfo != null)
                {
                    var session = new ServerSession(socket, this.serverLogger, this, clientInfo);
                    lock (this.sessionCountLock)
                    {
                        session.Disposed += this.SessionDisposedCallback;
                        this.sessionCount++;
                    }

                    session.ReceiveCommand();
                }
            }
            //client >= 1.4
            else
            {
                var session = new ServerSession2(socket, this.logManager, headBuffer);
                //
                lock (this.sessionCountLock)
                {
                    session.Disposed += this.Session2DisposedCallback;
                    this.sessionCount++;
                }
                session.ReceiveCommand();
            }
        }

        private void Session2DisposedCallback(object sender, EventArgs e)
        {
            var session = (ServerSession2)sender;
            session.Disposed -= this.Session2DisposedCallback;

            lock (this.sessionCountLock)
            {
                this.sessionCount--;
            }
        }

        private void SessionDisposedCallback(object sender, EventArgs e)
        {
            var session = (ServerSession)sender;
            session.Disposed -= this.SessionDisposedCallback;

            lock (this.sessionCountLock)
            {
                this.sessionCount--;
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
                this.Socket.Close();
                this.LogManager.Message.WriteLine("Listen Stop, Name:{0},Port:{1},Start:{2}, Stop:{3}", this.Name, this.Port, this.StartTime, DateTime.Now);
                this.LogManager.Flush();
            }
        }
    }
}