using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TestClient
{
    class Program
    {
       static void Main(string[] args)
        {
            //QueueTaskTest();

            //QueueTaskTest2();

            //CSTest();

            //PerformanceTest.Test();
           
           DynamicClientTest.Test();



            //SerializeTest.Test();
            //SerializeTest.Test2();

            Console.Read();
        }

        private static void CSTest()
        {

            var test = new Test();

            //设置监控
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    Console.WriteLine("Active Instance Count(总共可用实例数): {0}", test.AvailableSession);
                    Console.WriteLine("Active Instance Count(当前运行实例数): {0}", test.RuningSession);
                }
            }).Start();
            

            //启用 300线程
            var count = 300;
            var i = 0;
            while (i++ <= count)
            {
                new Thread(o =>
                {
                    while (true)
                    {
                        test.GetTime((int)i);

                        //Console.WriteLine("{0}: begin",i);
                        //Console.WriteLine(test.GetTime((int)i));
                        //Console.WriteLine("{0}: end",i);
                    }

                }).Start(i);
            }

            Console.WriteLine("Thread Create Complete");
            Console.Read();

            Environment.Exit(0);

            //var list = new List<int>();
            //var b = test.Test2(out list);

            //test.Error();

            while (true)
            {
                Thread.Sleep(500);
                try
                {
                    //cahce
                    Console.WriteLine(test.GetTime(1));


                    //无输出参数方法 : no cache
                    //test.GetTime(DateTime.Now.Ticks);
                    //Console.WriteLine(test.GetTime(DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).Ticks));
                    //test.Test2(new Random().Next(1000,9990));
                    // Console.WriteLine(test.GetTime());

                    ////输出参数方法
                    //long serverTime;
                    //test.GetServerTime(DateTime.Now.Ticks, out serverTime);
                    //Console.WriteLine("输出参数:{0}", serverTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        ///// <summary>
        ///// 对象管理式
        ///// </summary>
        //private static void QueueTaskTest2()
        //{
        //    new TestQueueReceive();
        //}

        ///// <summary>
        ///// 对象访问式
        ///// </summary>
        //private static void QueueTaskTest()
        //{
        //    var mq = new TaskQueue("TestQueue", "t");
        //    mq.Receive<DateTime>(10,(d) =>
        //    {

        //        Thread.Sleep(1000);
        //        Console.WriteLine((DateTime)d);

        //    });
        //    Console.Read();
        //}
    }
}
