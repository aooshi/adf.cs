using System;
using System.Collections.Generic;
using System.Text;

namespace Adf.Cs
{
    /// <summary>
    /// 命令HashKey生成参数列表
    /// </summary>
    public class ClientHashKeyAttribute : Attribute
    {
        /// <summary>
        /// 获取Hash键生成参数名称列表
        /// </summary>
        public string[] Parameters
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 以指定生成Hash键的参数列表初始化新实例
        /// </summary>
        /// <param name="parameters"></param>
        public ClientHashKeyAttribute(params string[] parameters)
        {
            this.Parameters = parameters;
        }
    }
}