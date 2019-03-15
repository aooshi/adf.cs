using System;
using System.Collections.Generic;
using System.Text;

namespace Adf.Cs
{
    /// <summary>
    /// 配置项
    /// </summary>
    public class ServerConfigItem
    {
        internal ServerConfigItem()
        {
            this.CacheDeletes = null;
            this.CacheVersion = null;
        }

        /// <summary>
        /// 过期时间
        /// </summary>
        public int CacheExpires
        {
            get;
            internal set;
        }

        /// <summary>
        /// 缓存为版本时的键名称
        /// </summary>
        public string CacheVersion
        {
            get;
            internal set;
        }

        /// <summary>
        /// 缓存键
        /// </summary>
        public string CacheKey
        {
            get;
            internal set;
        }

        /// <summary>
        /// cache delete
        /// </summary>
        internal List<ServerCacheDelete> CacheDeletes
        {
            get;
            set;
        }
    }
}
