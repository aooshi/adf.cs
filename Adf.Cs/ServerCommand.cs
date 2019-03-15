using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.Collections.Specialized;

namespace Adf.Cs
{
    /// <summary>
    /// 命令管理器
    /// </summary>
    internal static class ServerCommand
    {
        static bool isInit = false;
        /// <summary>
        /// 操作方法
        /// </summary>
        /// <remarks>key: namespace_methodname </remarks>
        static Dictionary<string, CommandInfo> commandsDictionary;
        /// <summary>
        /// 服务列表
        /// </summary>
        public static string[] Servers
        {
            get;
            private set;
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        /// <param name="logManager"></param>
        public static void Init(LogManager logManager)
        {
            if (isInit)
                throw new CsException("Repeated invoke");

            isInit = true;
            var serverMap = ConfigurationManager.GetSection("ServerMap") as NameValueCollection;
            if (serverMap == null)
                throw new ConfigurationErrorsException("Not Config ServerMap");
            //server
            Servers = serverMap.AllKeys;

            //command
            var commands = new Dictionary<string, CommandInfo>(serverMap.AllKeys.Length);
            foreach (var serverName in serverMap.AllKeys)
            {
                var typename = serverMap[serverName];
                var type = Type.GetType(typename, true);
                var commandCount = AddType(commands, type, serverName);
                //
                logManager.Message.WriteLine("{0} Commmand Count: {1}", serverName, commandCount);
            }
            //add to command
            commandsDictionary = commands;
        }

        /// <summary>
        /// 添加类型
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="type"></param>
        /// <param name="server"></param>
        private static int AddType(Dictionary<string, CommandInfo> commands, Type type, string server)
        {
            object instance = Activator.CreateInstance(type);
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var count = 0;
            foreach (var method in methods)
            {
                var commandInfo = new CommandInfo()
                {
                    Instance = instance
                    ,
                    Method = method
                    ,
                    Type = type
                    ,
                    Parameters = method.GetParameters()
                    ,
                    ReturnType = method.ReturnType
                    ,
                    Server = server
                    ,
                    Name = method.Name
                };

                if (commandInfo.Parameters.Length > byte.MaxValue)
                {
                    throw new CsException("Command Max Support 255 Parameter, {0}.{1}", server, method.Name);
                }

                for (byte i = 0; i < commandInfo.Parameters.Length; i++)
                    commandInfo.NameIndex.Add(commandInfo.Parameters[i].Name, i);

                commands[BuildCommandKey(server, method.Name)] = commandInfo;
                count++;
            }

            return count;
        }

        /// <summary>
        /// 获取命令
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        public static CommandInfo GetCommand(string serverName, string commandName)
        {
            //var key = BuildCommandKey(serverName, commandName);
            var key = serverName + "-" + commandName;
            CommandInfo commandInfo = null;
            commandsDictionary.TryGetValue(key, out commandInfo);
            //if (commandInfo == null)
            //{
            //    throw new NotFindCommandException(nameSpace, commandName);
            //}
            return commandInfo;
        }
        

        /// <summary>
        /// 构建命令键值
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        /// <returns></returns>
        public static string BuildCommandKey(string serverName, string commandName)
        {
            return string.Concat(serverName, "-", commandName);
        }
    }
}
