using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Reflection;
using System.IO;
using System.Collections.Specialized;
using System.Xml;

namespace Adf.Cs
{
    /// <summary>
    /// 服务管理
    /// </summary>
    internal static class ServerConfig
    {
        static System.Text.RegularExpressions.Regex CACHEKEY_REGEX = new System.Text.RegularExpressions.Regex(@"\{([\w]+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);
        static bool isInit = false;

        /// <summary>
        /// 配置项
        /// </summary>
        static Dictionary<string, ServerConfigItem> items;
        /// <summary>
        /// 版本项记录
        /// </summary>
        static Dictionary<string, int> versions;

        /// <summary>
        /// 初始化配置
        /// </summary>
        /// <param name="logManager"></param>
        /// <param name="servers"></param>
        public static void Init(LogManager logManager, string[] servers)
        {
            if (isInit)
                throw new CsException("Repeated invoke");

            isInit = true;

            var configPath = ConfigurationManager.AppSettings["Adf.Cs:ConfigPath"];
            if (string.IsNullOrEmpty(configPath))
                configPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Config\\");

            logManager.Message.WriteLine("Config Path: {0}", configPath);

            //
            versions = new Dictionary<string, int>(10);
            items = new Dictionary<string, ServerConfigItem>(10);
            //
            if (ServerCache.Enable)
            {
                if (Directory.Exists(configPath))
                {
                    foreach (var server in servers)
                    {
                        var file = Path.Combine(configPath, string.Concat(server, ".config"));
                        if (File.Exists(file))
                        {
                            logManager.Message.WriteLine("Load Config: {0}", file);
                            LoadServerConfig(server, file);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 服务配置读取
        /// </summary>
        /// <param name="server"></param>
        /// <param name="filepath"></param>
        private static void LoadServerConfig(string server, string filepath)
        {
            var doc = new XmlDocument();
            doc.Load(filepath);
            //
            var expires = 0;
            //
            int.TryParse(XmlHelper.GetAttribute(doc.DocumentElement, "expires", null), out expires);
            LoadVersionConfig(server, doc.DocumentElement.SelectNodes("version"));
            LoadServerConfig(server, expires, doc.DocumentElement.SelectNodes("command"));
        }

        /// <summary>
        /// 导入版本配置
        /// </summary>
        /// <param name="server"></param>
        /// <param name="xmlNodeList"></param>
        private static void LoadVersionConfig(string server, XmlNodeList xmlNodeList)
        {
            int expires;
            string expiresString,name;
            foreach (XmlNode node in xmlNodeList)
            {
                expiresString = XmlHelper.GetAttribute(node, "expires",null);
                name = XmlHelper.GetAttribute(node, "name",null);
                if (string.IsNullOrEmpty(name))
                {
                    throw new ConfigurationErrorsException(string.Format("error object node ,not find attribute \"name\" in {0}.config", server));
                }

                if (!int.TryParse(expiresString, out expires) || expires == 0)
                {
                    throw new ConfigurationErrorsException(string.Format("error version attribute expires=\"{1}\" in {0}.config", server,expiresString));
                }

                //concat key
                name = BuildVersionKey(server, name);
                if (versions.ContainsKey(name))
                {
                   throw new ConfigurationErrorsException(string.Format("Repeated version config {1} in {0}.config",server,name));
                }

                versions.Add(name, expires);
            }
        }

        /// <summary>
        /// 服务配置读取
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="serverExpire"></param>
        /// <param name="nodes"></param>
        private static void LoadServerConfig(string serverName, int serverExpire, XmlNodeList nodes)
        {
            var commandName = string.Empty;
            var expires = 0;
            var mixedsplit = string.Empty;
            var version = string.Empty;
            var cacheDeleteAction = string.Empty;
            var objectName = string.Empty;
            foreach (XmlNode node in nodes)
            {
                commandName = XmlHelper.GetAttribute(node, "name", null);
                if (string.IsNullOrEmpty(commandName))
                {
                    continue;
                }
                if (Exists(serverName,commandName))
                {
                       throw new ConfigurationErrorsException(string.Format("Repeated Command Item {0}.{1} in {0}.config",serverName,commandName));
                }
                var commandInfo = ServerCommand.GetCommand(serverName, commandName);
                if (commandInfo == null)
                {
                    throw new ConfigurationErrorsException(string.Format("No Find Server Command {0}.{1} in {0}.config",serverName,commandName));
                }
                var settingItem = new ServerConfigItem();
                //cache expires
                settingItem.CacheExpires = int.TryParse(XmlHelper.GetAttribute(node, "expires", null), out expires) ? expires : serverExpire;
                //version
                version = XmlHelper.GetAttribute(node, "version");
                if (!string.IsNullOrEmpty(version))
                {
                    if (GetVersionExpires(serverName, version) == 0)
                    {
                        throw new ConfigurationErrorsException(string.Format("Not Find Version:{2} config node, version info {0}.{1} version attribute in {0}.config", serverName, commandName, version));
                    }
                    settingItem.CacheVersion = version;
                }
                settingItem.CacheKey = XmlHelper.GetAttribute(node, "key", null);
                if (string.IsNullOrEmpty(settingItem.CacheKey))
                    settingItem.CacheKey = string.Concat(serverName,".",commandName);
                
                //deletes
                var deletes = node.SelectNodes("delete");
                if (deletes.Count > 0)
                {
                   settingItem.CacheDeletes = InitCacheDelete(commandInfo, deletes);
                }

                items.Add(ServerCommand.BuildCommandKey(serverName, commandName), settingItem);
            }
        }

        private static List<ServerCacheDelete> InitCacheDelete(CommandInfo commandInfo, XmlNodeList deleteNodes)
        {
            //loop delete item
            ServerCacheDelete delete = null;
            byte parameterIndex = 0;
            string parameters = null;
            string[] parameterArray = null;
            var deletes = new List<ServerCacheDelete>(deleteNodes.Count);
            foreach (XmlNode node in deleteNodes)
            {
                delete = new ServerCacheDelete();
                delete.Command = XmlHelper.GetAttribute(node, "name", null);
                if (string.IsNullOrEmpty(delete.Command))
                {
                    continue;
                }

                //server
                delete.Server = XmlHelper.GetAttribute(node, "server", commandInfo.Server);
                delete.Separator = XmlHelper.GetAttribute(node, "separator", ",");
                parameters = XmlHelper.GetAttribute(node, "parameters", null);
                if (parameters != null)
                {
                    if (string.IsNullOrEmpty(parameters))
                        throw new ConfigurationErrorsException(string.Format("empty config delete item parameters {0}.{1} from {2}.{3} in {2}.config ",
                            delete.Server, delete.Command, commandInfo.Server,commandInfo.Name));

                    parameterArray = parameters.Split(',');
                    foreach (var parameterName in parameterArray)
                    {
                        if (!commandInfo.NameIndex.TryGetValue(parameterName, out parameterIndex))
                            throw new ConfigurationErrorsException(string.Format("{4} is not {0}.{1} parameter list from {2}.{3} in {2}.config ",
                                delete.Server, delete.Command, commandInfo.Server, commandInfo.Name, parameterName));

                        delete.ParameterIndexs.Add(parameterIndex);
                    }
                    //if (delete.ParameterIndexs.Count > 0)
                    //    delete.ArrayIndex = Convert.ToInt32(XmlHelper.GetAttribute(node, "arrayindex", "-1"));
                }
                deletes.Add(delete);
            }
            return deletes.Count > 0 ? deletes : null;
        }
        
        /// <summary>
        /// 是否具有指定项
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        private static bool Exists(string serverName, string commandName)
        {
            return items.ContainsKey(ServerCommand.BuildCommandKey(serverName, commandName));
        }

        /// <summary>
        /// 获取单项配置
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        public static ServerConfigItem GetItem(string serverName, string commandName)
        {
            var key = ServerCommand.BuildCommandKey(serverName, commandName);
            ServerConfigItem item = null;
            items.TryGetValue(key, out item);
            return item;
        }

        /// <summary>
        /// 构建版本配置键
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="versionName"></param>
        /// <returns></returns>
        private static string BuildVersionKey(string serverName, string versionName)
        {
            return string.Concat(serverName, ":", versionName);
        }

        /// <summary>
        /// 获取版本配置
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="versionName"></param>
        public static int GetVersionExpires(string serverName, string versionName)
        {
            var key = BuildVersionKey(serverName, versionName);
            int expires = 0;
            versions.TryGetValue(key, out expires);
            return expires;
        }
    }
}
