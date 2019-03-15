using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Threading;
using Adf.Cs.Server;

namespace Adf.Cs.DefaultCache
{
    /// <summary>
    /// Local Cache
    /// </summary>
    public class WebCache : Adf.ICache
    {
        /// <summary>
        /// delete cache
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            HttpRuntime.Cache.Remove(key);
        }

        /// <summary>
        /// set cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        public void Set(string key, object value, int expires)
        {
            HttpRuntime.Cache.Add(key, value, null, DateTime.Now.AddSeconds(expires), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }

        /// <summary>
        /// get cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            var value = HttpRuntime.Cache.Get(key);
            if (value == null)
                return default(T);
            return (T)value;
        }


        /// <summary>
        /// get cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            return HttpRuntime.Cache.Get(key) as string;
        }

        /// <summary>
        /// get cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object Get(string key,Type type)
        {
            var result = HttpRuntime.Cache.Get(key);
            if (result != null && type.IsInstanceOfType(result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// set cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        public void Set(string key, string value, int expires)
        {
            HttpRuntime.Cache.Add(key, value, null, DateTime.Now.AddSeconds(expires), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }
    }
}
