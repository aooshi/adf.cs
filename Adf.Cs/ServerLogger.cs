using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections;

namespace Adf.Cs
{
    /// <summary>
    /// 服务器日志
    /// </summary>
    internal class ServerLogger 
    {
        private LogManager logManager;

        /// <summary>
        /// initialize a new instance
        /// </summary>
       public ServerLogger(LogManager logManager)
       {
           this.logManager = logManager;
           logManager.Message.WriteLine("Log Path:{0}", logManager.Path);
       }

       /// <summary>
       /// command exception
       /// </summary>
       /// <param name="serverName"></param>
       /// <param name="commandName"></param>
       /// <param name="exception"></param>
       public void CommandException(string serverName, string commandName, Exception exception)
       {
           this.logManager.Error.WriteLine("{0} CommandException {1}.{2}: {3}", DateTime.Now, serverName, commandName, exception);
       }

       /// <summary>
       /// CommandCacheException
       /// </summary>
       /// <param name="serverName"></param>
       /// <param name="commandName"></param>
       /// <param name="exception"></param>
       public void CommandCacheException(string serverName, string commandName, Exception exception)
       {
           this.logManager.Error.WriteLine("{0} CommandCacheException {1}.{2}: {3}", DateTime.Now, serverName, commandName, exception);
       }

        /// <summary>
        /// CommandCacheDeleteNotFind
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        /// <param name="deleteCommand"></param>
        /// <param name="deleteServer"></param>
        /// <param name="message"></param>
        public void CommandCacheDelete(string serverName, string commandName, string deleteServer, string deleteCommand, string message)
        {
            this.logManager.Message.WriteLine("{0} CommandCacheDelete {5} {1}.{2} From {3}.{4}", DateTime.Now, deleteServer, deleteCommand, serverName, commandName, message);
        }

        ///// <summary>
        ///// accept exception
        ///// </summary>
        ///// <param name="exception"></param>
        //public void AcceptException(Exception exception)
        //{
        //    this.logManager.Error.WriteLine("{0} AcceptException: {1}", DateTime.Now, exception);

        //   // this.logManager.Exception(exception);
        //}

        ///// <summary>
        /////  new session exception
        ///// </summary>
        ///// <param name="exception"></param>
        //public void NewSessionException(Exception exception)
        //{
        //    this.logManager.Error.WriteLine("{0} NewSessionException: {1}", DateTime.Now, exception);

        //   // this.logManager.Exception(exception);
        //}

        /// <summary>
        ///  session exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="session"></param>
        public void SessionException(ServerSession session, Exception exception)
        {
            this.logManager.Error.WriteLine("{0} SessionException SessionId: {1},  {2}", DateTime.Now, session.Id, exception);
        }

        /// <summary>
        /// new session
        /// </summary>
        /// <param name="serverSession"></param>
        public void NewSession(ServerSession serverSession)
        {
            if (serverSession.ClientInfo == null)
            {
                this.logManager.Message.WriteLine("{0} NewSession, SessionId: {1}, ClientInfo Analyze Error.",
                    DateTime.Now,
                    serverSession.Id);
                return;
            }

            DateTime time = DateTime.MinValue;
            try
            {
                time = new DateTime(serverSession.ClientInfo.TimeTicks);
            }
            catch
            {

            }

            this.logManager.Message.WriteLine("{0} NewSession, SessionId: {1}, ClientVersion:{2}, ClientTime:{3}", 
                DateTime.Now,
                serverSession.Id,
                   serverSession.ClientInfo.Version,
                   time);
        }

        /// <summary>
        /// 客户端关闭
        /// </summary>
        /// <param name="session"></param>
        public void SessionClose(ServerSession session)
        {
            this.logManager.Message.WriteLine("{0} SessionClose, SessionId: {1}", DateTime.Now, session.Id);
        }
        /// <summary>
        /// 达到空闲时间
        /// </summary>
        /// <param name="session"></param>
        public void LeisureTimeout(ServerSession session)
        {
            this.logManager.Message.WriteLine("{0} LeisureTimeout, SessionId: {1}", DateTime.Now, session.Id);
        }
        ///// <summary>
        ///// 新连接未成功
        ///// </summary>
        ///// <param name="clientIp"></param>
        ///// <param name="clientPort"></param>
        //public void NewSessionConnectionUnfinished(string clientIp, int clientPort)
        //{
        //    this.logManager.Message.WriteLine("{0} NewSession, Connection Unfinished: Client: {1}:{2}", DateTime.Now, clientIp, clientPort);
        //}

        /// <summary>
        /// 命令执行
        /// </summary>
        /// <param name="session"></param>
        /// <param name="serverName"></param>
        /// <param name="commandName"></param>
        /// <param name="cacheType"></param>
        /// <param name="invokeSeconds"></param>
        /// <param name="seconds"></param>
        /// <param name="result"></param>
        public void Command(ServerSession session, string serverName, string commandName, bool result, ServerCacheType cacheType, double invokeSeconds, double seconds)
        {
            this.logManager.Message.WriteLine("{0} SessionId: {1}, {2}.{3}:{4}, Cache:{5}, InvokeSeconds:{6},Seconds:{7}",
                DateTime.Now, session.Id, serverName, commandName, result ? "Success" : "Failure", cacheType, invokeSeconds, seconds);
        }

        ///// <summary>
        ///// cache delete
        ///// </summary>
        ///// <param name="session"></param>
        ///// <param name="cacheName"></param>
        //public void CacheDelete(ServerSession session, string cacheName)
        //{
        //    this.logManager.Message.WriteLine("{0} SessionId:{1} Cache Delete:{2}",
        //        DateTime.Now, session.Id,cacheName);
        //}


        ///// <summary>
        ///// cache version
        ///// </summary>
        ///// <param name="session"></param>
        ///// <param name="version"></param>
        ///// <param name="versionName"></param>
        //public void CacheVersion(ServerSession session, string versionName,int version)
        //{
        //    this.logManager.Message.WriteLine("{0} SessionId:{1} Cache Version:{2}={3}",
        //        DateTime.Now, session.Id, versionName, version);
        //}

    }
}