using System;
using System.Collections.Generic;

using System.Text;

namespace Adf.Cs
{
    /// <summary>
    /// cache delete item
    /// </summary>
    internal class ServerCacheDelete
    {
        /// <summary>
        /// initialize a new instance
        /// </summary>
        public ServerCacheDelete()
        {
            this.ParameterIndexs = new List<int>(5);
        }

        /// <summary>
        /// server name
        /// </summary>
        public string Server
        {
            get;
            set;
        }

        /// <summary>
        /// command name
        /// </summary>
        public string Command
        {
            get;
            set;
        }

        /// <summary>
        /// 参数索引
        /// </summary>
        public List<int> ParameterIndexs
        {
            get;
            private set;
        }

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Separator
        {
            get;
            set;
        }
    }
}
