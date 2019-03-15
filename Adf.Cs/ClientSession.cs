using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using Adf.Config;

namespace Adf.Cs
{
    /// <summary>
    /// 客户端会话
    /// </summary>
    internal class ClientSession : IPoolInstance, IDisposable
    {
        const byte PROTOCOL_VERSION = 2;        

        /// <summary>
        /// 获取会话 ID
        /// </summary>
        public string Id
        {
            get;
            private set;
        }

        Socket socket;
        /// <summary>
        /// 获取连接套接字
        /// </summary>
        public Socket Socket
        {
            get
            {
                return this.socket;
            }
        }
        /// <summary>
        /// 本地端口
        /// </summary>
        public int Port
        {
            get;
            private set;
        }
        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIp
        {
            get;
            private set;
        }
        /// <summary>
        /// 服务端口
        /// </summary>
        public int ServerPort
        {
            get;
            private set;
        }

        int invokeCount = 0;
        /// <summary>
        /// invoke count
        /// </summary>
        public int InvokeCount
        {
            get
            {
                return this.invokeCount;
            }
        }

        /// <summary>
        /// 初始一个Session
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public ClientSession(string ip, int port)
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Connect(ip, port);

            IPEndPoint ep = (IPEndPoint)this.socket.LocalEndPoint;
            this.Port = ep.Port;

            ep = (IPEndPoint)this.socket.RemoteEndPoint;
            this.ServerIp = ep.Address.ToString();
            this.ServerPort = ep.Port;

            //同一时间内本地端口不可能重复
            this.Id = ep.Address.ToString() + "." + this.Port.ToString("D5");
            //
            //reserve1(byte) + reserve2(byte) + reserve3(byte) + Protocol Version(byte)
            var headBuffer = new byte[] { 0, 0, 0, PROTOCOL_VERSION };
            this.socket.Send(headBuffer);
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void Dispose()
        {
            this.socket.Shutdown(SocketShutdown.Both);
            //this.socket.Dispose();
            this.socket.Close();
        }

        /// <summary>
        /// 方法调用
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal T Invoke<T>(string server, string command, params object[] parameters)
        {
            //仅最大支持255参数, max allow 255 parameter
            var parameterLength = (byte)parameters.Length;
            var value = default(T);
            //send
            using (var m = new MemoryStream())
            {
                //length
                m.WriteByte(parameterLength);
                //namespace
                CsSocket.WriteDataItem(m, server);
                //name
                CsSocket.WriteDataItem(m, command);
                //parameters
                if (parameterLength > 0)
                    foreach (var param in parameters)
                        CsSocket.WriteDataItem(m, param);
                this.socket.Send(m.GetBuffer(), 0, (int)m.Length, SocketFlags.None);
            }
            this.invokeCount++;
            //
            var result = (CsResult)(SocketHelper.Receive(this.socket, 1)[0]);
            if (result == CsResult.Success)
            {
                //buffer parameter length , parameter index + parameter length + parameter body + bodylength + body
                var outputParameterLength = (SocketHelper.Receive(this.socket, 1)[0]);
                if (outputParameterLength > 0)
                {
                    byte parameterIndex = 0;
                    for (byte i = 0; i < outputParameterLength; i++)
                    {
                        parameterIndex = (SocketHelper.Receive(this.socket, 1)[0]);

                        if (parameters[parameterIndex] == null)
                            throw new ArgumentNullException("out/ref parameter not allow is null");

                        //read parameter
                        parameters[parameterIndex] = CsSocket.Receive(socket, parameters[parameterIndex].GetType());
                    }
                }

                //read body
                value = (T)CsSocket.Receive(this.socket, typeof(T));
            }
            else
            {
                var failureMessage = (string)CsSocket.Receive(this.socket, typeof(string));
                throw new CsException(server, command, failureMessage);
            }
            //
            return value;
        }

        /// <summary>
        /// 是否废弃此实例
        /// </summary>
        public bool PoolAbandon
        {
            get;
            set;
        }
    }
}