using System;
using System.Collections.Generic;
using System.Text;
using Adf.Cs;

namespace TestServer
{
    /// <summary>
    /// 测试
    /// </summary>
    public class ServerTest
    {
        /// <summary>
        /// 测试时间返回
        /// </summary>
        /// <param name="clientTime"></param>
        /// <returns></returns>
        public DateTime GetTime(long clientTime)
        {
            return DateTime.Now;
            //return new Random().Next(int.MaxValue);
        }

        /// <summary>
        /// 测试直接返回值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetParameter(int value)
        {
            return value;
        }

        /// <summary>
        /// 测试带参数返回的方法
        /// </summary>
        /// <param name="clientTime"></param>
        /// <param name="serverTime"></param>
        public int GetServerTime(long clientTime, out long serverTime)
        {
            System.Threading.Thread.Sleep(new Random().Next(1, 999));
            serverTime = DateTime.Now.Ticks;
            return 1;
        }
        

        /// <summary>
        /// 返回一个数组
        /// </summary>
        /// <returns></returns>
        public int[] GetArray()
        {
            return new int[] { 1, 2, 3, 4 };
        }

        /// <summary>
        /// 请求一个数组
        /// </summary>
        /// <param name="ids"></param>
        public int RequestArray(out string[] ids)
        {
            var ids1 = new List<string>();
            ids1.Add("1");
            ids1.Add("4");
            ids1.Add("2");
            ids1.Add("3");
            ids = ids1.ToArray();

            return ids1.Count;
        }
                
        /// <summary>
        /// 
        /// </summary>
        public int ModifyGetTime()
        {
            return 1;
        }


        /// <summary>
        /// 
        /// </summary>
        public int Error()
        {
            throw new Exception("ERROR TEST");
        }
    }
}
