using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TestClient
{
    ///// <summary>
    ///// 测试队列读取
    ///// </summary>
    //public class TestQueueReceive : TaskQueueReceive<DateTime>
    //{
    //    const string CONFIG_NAME = "TestQueue";
    //    const int MAX_THREAD_COUNT = 2;

    //    /// <summary>
    //    /// 初始化
    //    /// </summary>
    //    public TestQueueReceive()
    //        : base(CONFIG_NAME,typeof(TestQueueReceive).Name,MAX_THREAD_COUNT)
    //    {
    //    }

    //    /// <summary>
    //    /// 新项
    //    /// </summary>
    //    /// <param name="item"></param>
    //    protected override void NewItem(DateTime item)
    //    {
    //        Thread.Sleep(1000);
    //        //Console.WriteLine(item);
    //        base.Log.Write("{0}",item);
    //    }
    //}
}
