using System;
using System.Collections.Generic;
using System.Text;

namespace TestClient
{
    public static class DynamicClientTest
    {
        public static void Test()
        {
            Console.WriteLine("GetParameter: {0}", TestClient.Instance.GetParameter(Adf.UnixTimestampHelper.ToTimestamp()));

            long serverTime;
            TestClient.Instance.GetServerTime(DateTime.Now.Ticks, out serverTime);
            Console.WriteLine("GetServerTime: {0}", serverTime);
            Console.WriteLine("GetTime: {0}", TestClient.Instance.GetTime(DateTime.Now.Ticks));
            Console.WriteLine("GetArray: {0}", Adf.ConvertHelper.ArrayToString(TestClient.Instance.GetArray(), ","));

            string[] list;
            TestClient.Instance.RequestArray(out list);
            Console.WriteLine("RequestArray: {0}", string.Join(",", list));
            
        }

    }


    public class TestClient
    {
        /// <summary>
        /// 通用实例
        /// </summary>
        public static ITest Instance = Adf.Cs.CsHelper.GetClient<ITest>("Test", "TestServer");
    }

    public interface ITest
    {
        /// <summary>
        /// 测试直接返回值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int GetParameter(int value);

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        DateTime GetTime(long time);



        /// <summary>
        /// 返回一个数组
        /// </summary>
        /// <returns></returns>
        int[] GetArray();

        /// <summary>
        /// 请求一个数组
        /// </summary>
        /// <param name="ids"></param>
        int RequestArray(out string[] ids);

        /// <summary>
        /// 获取时间,测试带参数返回的方法,并以参数 clientTime 做为hashKey
        /// </summary>
        /// <returns></returns>
        [Adf.Cs.ClientHashKey("clientTime")]
        int GetServerTime(long clientTime, out long serverTime);


        /// <summary>
        /// 获取时间,测试带参数返回的方法
        /// </summary>
        /// <returns></returns>
        int ModifyGetTime();


        /// <summary>
        /// 测试异常
        /// </summary>
        int Error();
    }

}
