using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Collections;

namespace Adf.Cs
{
    /// <summary>
    /// 缓存管理器
    /// </summary>
    public static class ServerCache
    {
        static bool isInit = false;
        static readonly long UNIXTIMESTAMP_BASE = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(2000, 1, 1)).Ticks;
        static LogWriter logWriter;

        /// <summary>
        /// 日志书写器
        /// </summary>
        public static LogWriter LogWriter
        {
            get { return logWriter; }
        }

        /// <summary>
        /// 缓存项
        /// </summary>
        public static Adf.ICache Cache
        {
            get;
            private set;
        }

        /// <summary>
        /// 是否启用缓存功能
        /// </summary>
        public static Boolean Enable
        {
            get;
            private set;
        }

        /// <summary>
        /// static initialize
        /// </summary>
        /// <param name="logManager"></param>
        internal static void Init(LogManager logManager)
        {
            if (isInit)
                throw new CsException("Repeated invoke");

            isInit = true;

            var cacheType = ConfigurationManager.AppSettings["Adf.Cs:CacheType"];

            if (!string.IsNullOrEmpty(cacheType))
            {
                ServerCache.Cache = (Adf.ICache)Activator.CreateInstance(Type.GetType(cacheType, true));
            }
            else
            {
                //var type = Type.GetType("Adf.Cs.DefaultCache.WebCache,Adf.Cs.DefaultCache", false);
                var type = Type.GetType("Adf.Web.WebProgressCache,Adf.Web", false);
                if (type != null)
                    ServerCache.Cache = (Adf.ICache)Activator.CreateInstance(type);
            }

            logWriter = logManager.GetWriter("ServerCache");
            logWriter.Enable = ConfigurationManager.AppSettings["Adf.Cs:LogCacheDelete"] == "true";

            if (ServerCache.Cache != null)
            {
                logWriter.WriteTimeLine("Cache Type: {0}", ServerCache.Cache.GetType());
                ServerCache.Enable = true;
            }
            else
            {
                ServerCache.Enable = false;
                logWriter.WriteTimeLine("Cache Disabled, Set Adf.Cs:CacheType Config Enable");
            }
        }


        /// <summary>
        /// build cache key
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string BuildKey(string cacheKey, object[] parameters)
        {
            return BuildKey(cacheKey, null, parameters);
        }

        /// <summary>
        /// build cache key
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="parameters"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string BuildKey(string cacheKey, string version, object[] parameters)
        {
            var build = new StringBuilder();
            build.Append(cacheKey);
            if (!string.IsNullOrEmpty(version))
            {
                build.Append('.');
                build.Append(version);
            }
            build.Append("_");
            BuildArray(parameters, build);

            return build.ToString(0, build.Length - 1);
        }

        /// <summary>
        /// build array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="build"></param>
        private static void BuildArray(IEnumerable array, StringBuilder build)
        {
            if (array == null)
                return;

            foreach (var item in array)
            {
                //当为 string 时，有可能进入  char 玫举
                if (item is IEnumerable && !(item is string))
                {
                    build.Append("{");
                    BuildArray((IEnumerable)item, build);
                    build.Append("}");
                }
                else
                {
                    build.Append(item);
                    build.Append('_');
                }
            }
        }

        /// <summary>
        /// 返回新的版本号
        /// </summary>
        public static int GetVersion()
        {
            //根据指定的本地时间返回当前时间的Unix时间戳
            return (int)((DateTime.UtcNow.Ticks - UNIXTIMESTAMP_BASE) / 10000000);
        }

        /// <summary>
        /// 移除一个版本模式缓存
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <exception cref="ArgumentException">not find command config</exception>
        /// <returns>remove cache name</returns>
        public static string RemoveVersion(string server, string command)
        {
            var deleteCommandConfig = ServerConfig.GetItem(server, command);
            if (deleteCommandConfig == null)
            {
                throw new ArgumentException(string.Format("not find {0}.{1} command config", server, command));
            }

            //update version
            ServerCache.Cache.Delete(deleteCommandConfig.CacheVersion);

            if (logWriter.Enable)
            {
                logWriter.WriteTimeLine("Delete Cache Version: " + deleteCommandConfig.CacheVersion);
            }

            return deleteCommandConfig.CacheVersion;
        }

        /// <summary>
        /// 移除一个命令缓存
        /// </summary>
        /// <param name="server"></param>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentException">not find command config</exception>
        /// <returns>remove cache name</returns>
        public static string RemoveCache(string server, string command, params object[] parameters)
        {
            var deleteCommandConfig = ServerConfig.GetItem(server, command);
            if (deleteCommandConfig == null)
            {
                throw new ArgumentException(string.Format("not find {0}.{1} command config", server, command));
            }

            //delete
            var cacheKey = ServerCache.BuildKey(deleteCommandConfig.CacheKey, parameters);
            ServerCache.Cache.Delete(cacheKey);

            if (logWriter.Enable)
            {
                logWriter.WriteTimeLine("Delete Cache: " + cacheKey);
            }

            return cacheKey;
        }
    }
}