using System;
using System.Collections.Generic;
using System.Text;
using Adf.Cs;
using System.Configuration;

namespace TestClient
{
    public class Test : Client
    {
        public Test()
            : base("Test", "TestServer")
        {
           
        }
        
        /// <summary>
        /// 测试直接返回值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetParameter(int value)
        {
            return base.Command<int>("GetParameter", value);
        }
                
        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public DateTime GetTime(long time)
        {
            return new DateTime(base.Command<long>("GetTime", time));
        }

        /// <summary>
        /// 返回一个数组
        /// </summary>
        /// <returns></returns>
        public int[] GetArray()
        {
            return base.Command<int[]>("GetArray");
        }

        /// <summary>
        /// 请求一个数组
        /// </summary>
        /// <param name="ids"></param>
        public void RequestArray(out string[] ids)
        {
            ids = null;
            var parameters = new object[] { 
                ids
            };
            base.Command<int>("GetArray",parameters);
            ids = (string[])parameters[0];
        }

        /// <summary>
        /// 获取时间,测试带参数返回的方法
        /// </summary>
        /// <returns></returns>
        public void GetServerTime(long clientTime,out long serverTime)
        {
            serverTime = 0;
            var parameters = new object[] { 
                clientTime
                , serverTime
            };
            base.Command<int>("GetServerTime", parameters);
            serverTime = (long)parameters[1];
        }
        /// <summary>
        /// 获取时间,测试带参数返回的方法
        /// </summary>
        /// <returns></returns>
        public void ModifyGetTime()
        {
            base.Command<int>("ModifyGetTime");
        }


        /// <summary>
        /// 测试异常
        /// </summary>
        public void Error()
        {
            base.Command<int>("Error");
        }

    }
}
