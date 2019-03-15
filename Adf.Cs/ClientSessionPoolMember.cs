using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using Adf.Config;
using System.Configuration;

namespace Adf.Cs
{
    /// <summary>
    /// 会话池项
    /// </summary>
    internal class ClientSessionPoolMember : Adf.IPoolMember
    {
        string ip;
        int port;

        public ClientSessionPoolMember(string ip, int port)
        {
            this.PoolActive = true;
            this.ip = ip;
            this.port = port;
            this.PoolMemberId = string.Concat(ip, ":", port);
        }

        /// <summary>
        /// is active
        /// </summary>
        public bool PoolActive
        {
            get;
            set;
        }

        public IPoolInstance CreatePoolInstance()
        {
            return new ClientSession(this.ip, this.port);
        }


        public string PoolMemberId
        {
            get;
            private set;
        }
    }
}