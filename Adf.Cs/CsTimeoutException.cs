using System;
using System.Collections.Generic;

using System.Text;

namespace Adf.Cs
{
    /// <summary>
    /// 命令调用超时异常
    /// </summary>
    public class CsTimeoutException : Exception
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <param name="message"></param>
        public CsTimeoutException(string server, string command, string message)
            : base(string.Format("{0}.{1}: {2}", server, command, message))
        {
        }
    }
}
