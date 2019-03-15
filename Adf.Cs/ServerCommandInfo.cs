using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;

namespace Adf.Cs
{
    /// <summary>
    /// 命令信息
    /// </summary>
    internal class CommandInfo
    {
        public CommandInfo()
        {
            this.NameIndex = new Dictionary<string,byte>(10);
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Server
        /// </summary>
        public string Server
        {
            get;
            set;
        }

        /// <summary>
        /// 实例
        /// </summary>
        public object Instance
        {
            get;
            set;
        }

        /// <summary>
        /// 类
        /// </summary>
        public Type Type
        {
            get;
            set;
        }

        /// <summary>
        /// 返回类型
        /// </summary>
        public Type ReturnType
        {
            get;
            set;
        }

        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo Method
        {
            get;
            set;
        }

        /// <summary>
        /// Out Index
        /// </summary>
        public byte[] OutsIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// out index length
        /// </summary>
        public byte OutsIndexLength
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 名称与索引对
        /// </summary>
        public Dictionary<string, byte> NameIndex
        {
            get;
            set;
        }

        ParameterInfo[] parameters;
        /// <summary>
        /// 方法参数
        /// </summary>
        public ParameterInfo[] Parameters
        {
            get
            {
                return parameters;
            }
            set
            {
                parameters = value;
                var size = (byte)value.Length;
                var outindex = new List<byte>(value.Length);
                for (byte i = 0, l = size; i < l; i++)
                {
                    if (value[i].IsOut)
                    {
                        outindex.Add(i);
                    }
                }

                this.OutsIndexLength = (byte)outindex.Count;
                this.OutsIndex = this.OutsIndexLength > 0  ? outindex.ToArray() : null;
            }
        }
    }
}