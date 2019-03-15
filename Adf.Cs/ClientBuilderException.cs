using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Specialized;

namespace Adf.Cs
{
    /// <summary>
    /// 客户端构建器异常
    /// </summary>
    public class ClientBuilderException : Exception
    {
        /// <summary>
        /// 获取客户端源代码
        /// </summary>
        public string ClientSource
        {
            get;
            internal set;
        }

        /// <summary>
        /// 获取编译器引入的程序集清单
        /// </summary>
        public StringCollection ReferencedAssemblies { get; internal set; }


        /// <summary>
        /// 初始一个新实例
        /// </summary>
        /// <param name="innerException"></param>
        public ClientBuilderException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        /// <summary>
        /// 初始一个新实例
        /// </summary>
        /// <param name="message"></param>
        public ClientBuilderException(string message)
            : base(message)
        {
        }
    }
}