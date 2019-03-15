using System;
using System.Text;
using System.Collections;

namespace Adf.Cs
{
    /// <summary>
    /// 客户端帮助类
    /// </summary>
    public static class CsHelper
    {
        /// <summary>
        /// 参数串联
        /// </summary>
        /// <param name="splitChar"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Join(string splitChar, ICollection args)
        {
            if (args.Count == 0)
            {
                return string.Empty;
            }

            var build = new StringBuilder();
            var first = true;
            foreach (var item in args)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    build.Append(splitChar);
                }
                //
                if (item == null)
                {
                }
                else if (item is ICollection)
                {
                    //嵌套
                    build.Append(Join(splitChar, (ICollection)item));
                }
                else
                {
                    build.Append(item);
                }
            }
            return build.ToString();
        }

        /// <summary>
        /// 获取一个客户端接口实例,高速访问时，相同参数不应多次调用， 应在获取到实例后存储到相应的变量中进行变量访问
        /// </summary>
        /// <typeparam name="T">必需是接口类型,不能含有泛型成员，不能是嵌套类型(类中类)</typeparam>
        /// <param name="server">服务名</param>
        /// <param name="configName">配置节点名</param>
        /// <returns></returns>
        public static T GetClient<T>(string server, string configName)
        {
            var type = typeof(T);
            if (type.IsInterface == false)
            {
                throw new ArgumentOutOfRangeException("T must be is a interface");
            }

            var identifier = configName + "_" + server;
            return (T)ClientBuilder.CreateInstance(type, identifier, server, configName);
        }

        /// <summary>
        /// 获取一个客户端接口实例，高速访问时，相同参数不应多次调用， 应在获取到实例后存储到相应的变量中进行变量访问
        /// </summary>
        /// <typeparam name="T">必需是接口类型,不能含有泛型成员，不能是嵌套类型(类中类)</typeparam>
        /// <param name="server">服务名</param>
        /// <param name="hostOrIp">主机</param>
        /// <param name="port">端口</param>
        /// <returns></returns>
        public static T GetClient<T>(string server, string hostOrIp, int port)
        {
            var type = typeof(T);
            if (type.IsInterface == false)
            {
                throw new ArgumentOutOfRangeException("T must be is a interface");
            }

            var identifier = server + hostOrIp + port;
            identifier = "hostOrIp" + identifier.GetHashCode();
            return (T)ClientBuilder.CreateInstance(type, identifier, server, hostOrIp, port);
        }

        /// <summary>
        /// 获取一个客户端接口实例，高速访问时，相同参数不应多次调用， 应在获取到实例后存储到相应的变量中进行变量访问
        /// </summary>
        /// <typeparam name="T">必需是接口类型,不能含有泛型成员，不能是嵌套类型(类中类)</typeparam>
        /// <param name="server">服务名</param>
        /// <param name="hostOrIps">主机,格式为：  ipOrHost:port,</param>
        /// <returns></returns>
        public static T GetClient<T>(string server, string[] hostOrIps)
        {
            var type = typeof(T);
            if (type.IsInterface == false)
            {
                throw new ArgumentOutOfRangeException("T must be is a interface");
            }

            var identifier = server + string.Join(",", hostOrIps);
            identifier = "hostOrIps" + identifier.GetHashCode();
            return (T)ClientBuilder.CreateInstance(type, identifier, server, hostOrIps);
        }
    }
}