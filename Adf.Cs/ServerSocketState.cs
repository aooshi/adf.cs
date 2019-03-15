using System;
using System.Collections.Generic;

using System.Text;
using System.Net.Sockets;

namespace Adf.Cs
{
    /// <summary>
    /// 读取状态
    /// </summary>
    internal class ServerSocketState
    {
        /// <summary>
        /// 缓存
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        /// 缓存大小
        /// </summary>
        public int BufferSize;

        /// <summary>
        /// 缓存存节位置
        /// </summary>
        public int Offset;

        /// <summary>
        /// Socket 对象
        /// </summary>
        public Socket Socket;

        /// <summary>
        /// 初始读状态
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="bufferSize"></param>
        public ServerSocketState(Socket socket, int bufferSize)
        {
            this.Socket = socket;
            this.Buffer = new byte[bufferSize];
            this.BufferSize = bufferSize;
            this.Offset = 0;
        }

        /// <summary>
        /// 设置位置并返回未读余量
        /// </summary>
        /// <param name="readCount">已读量</param>
        /// <returns></returns>
        public int SetOffset(int readCount)
        {
            this.Offset += readCount;
            var surplus = this.BufferSize - this.Offset;
            return surplus < 0 ? 0 : surplus;
        }
    }
}