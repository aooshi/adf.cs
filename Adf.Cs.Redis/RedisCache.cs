using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Redis;
using System.Configuration;
using Adf.Cs.Server;

namespace Adf.Cs.Redis
{
    /// <summary>
    /// Redis Pool
    /// </summary>
    public class RedisCache : Adf.ICache
    {
        const string CONFIG_NAME = "RedisCacheServer";
        const string POOL_NAME = "RedisCacheServerPoolSize";

        readonly string[] ReadWriteHosts;
        readonly string[] ReadOnlyHosts;
        int poolSize = 0;
        IRedisClientFactory factory = new RedisCacheClientFactory();
        /// <summary>
        /// Redis Manager
        /// </summary>
        public PooledRedisClientManager ClientManager
        {
            get;
            private set;
        }

        public RedisCache()
        {
            var configs = ((Adf.Config.IpGroupSection)ConfigurationManager.GetSection(CONFIG_NAME));
            if (configs == null)
            {
                throw new ConfigurationErrorsException("Not Find Config " + CONFIG_NAME);
            }

            var serverList = new List<string>(configs.IpList.Count);
            foreach (Adf.Config.IpGroupElement server in configs.IpList)
            {
                serverList.Add(string.Concat(server.Ip, ":", server.Port));
            }
            this.ReadOnlyHosts = serverList.ToArray();
            this.ReadWriteHosts = this.ReadOnlyHosts;

            if (!int.TryParse(ConfigurationManager.AppSettings[POOL_NAME], out this.poolSize))
                poolSize = (this.ReadOnlyHosts.Length + this.ReadWriteHosts.Length) * 100;

            this.ClientManager = this.CreateManager();
        }

        /// <summary>
        /// create manager
        /// </summary>
        /// <returns></returns>
        private PooledRedisClientManager CreateManager()
        {
            return new PooledRedisClientManager(ReadWriteHosts, ReadOnlyHosts,
                new RedisClientManagerConfig
                {
                    MaxWritePoolSize = this.poolSize,
                    MaxReadPoolSize = this.poolSize,
                    AutoStart = true,
                })
            {
                RedisClientFactory = factory,
            };
        }
               
        public void Delete(string key)
        {
            using (var client = ClientManager.GetClient())
            {
                client.Remove(key);
            }
        }
        
        public void Set(string key, object value, int expires)
        {
            using (var client = ClientManager.GetClient())
            {
                if (value is byte[])
                    client.Set<byte[]>(key, (byte[])value, TimeSpan.FromSeconds(expires));
                else
                    client.Set<object>(key, value, TimeSpan.FromSeconds(expires));
            }
        }

        public T Get<T>(string key)
        {
            using (var client = ClientManager.GetClient())
            {
                return client.Get<T>(key);
            } 
        }
        
        public string Get(string key)
        {
            using (var client = ClientManager.GetClient())
            {
                return client.GetValue(key);
            } 
        }

        public void Set(string key, string value, int expires)
        {
            using (var client = ClientManager.GetClient())
            {
                client.SetEntry(key, value, TimeSpan.FromSeconds(expires));
            }
        }
    }
}