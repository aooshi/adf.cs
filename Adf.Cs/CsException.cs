using System;
using System.Collections.Generic;

using System.Text;

namespace Adf.Cs
{
    /// <summary>
    /// 命令异常
    /// </summary>
    public class CsException : Exception
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public CsException(string message, params object[] args)
            : base(args.Length > 0 ? string.Format(message, args) : message)
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <param name="message"></param>
        public CsException(string server, string command, string message)
            : base(string.Format("{0}.{1}: {2}", server, command, message))
        {
        }
    }

    /// <summary>
    /// 命令异常
    /// </summary>
    public class CsInvokeException : Exception
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <param name="exception".
        public CsInvokeException(string server, string command, Exception exception)
            : base(string.Format("{0}.{1} => {2}.", server, command, exception.Message), exception)
        {
        }
    }
}