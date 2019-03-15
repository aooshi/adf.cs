using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace TestClient
{
    public class PerformanceTest
    {
        public static void Test()
        {
            bool run = true;
            var testClient = new Test();
            //var testClient = Adf.Cs.CsHelper.GetClient<ITest>("Test", "TestServer");

            int threadCount = 10;
            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                int index = 0;
                while (run)
                {
                    Console.WriteLine("Available Session: " + testClient.AvailableSession + "");
                    Console.WriteLine("Member Thread Size: " + testClient.PoolMemberThreadSize + "");
                    System.Threading.Thread.Sleep(1000);

                    if (index++ == 5)
                        run = false;
                }
            });
            
            Console.WriteLine("" + threadCount + " thread single member");
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new System.Threading.Thread((threadIndex)=>
                {
                    var total = 0L;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (run)
                    {
                        //testClient.GetParameterOfMember(1,"127.0.0.1",4562);
                        testClient.GetParameter((int)total);
                        //clientSession.Invoke<int>("Test", "GetParameter", 1);
                        total++;
                    }
                    stopwatch.Stop();

                    Console.WriteLine("{3}-> total:{0}, seconds:{1}, {2} call/s"
                        , total
                        , (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , threadIndex
                        );

                });
                thread.IsBackground = true;
                thread.Start(i);
            }



            //Console.WriteLine("" + threadCount + " thread pool connection");
            //for (int i = 0; i < threadCount; i++)
            //{
            //    System.Threading.ThreadPool.QueueUserWorkItem(state =>
            //    {
            //        var total = 0L;
            //        var stopwatch = new Stopwatch();
            //        stopwatch.Start();
            //        while (run)
            //        {
            //            testClient.GetParameter(1);
            //            total++;
            //        }
            //        stopwatch.Stop();

            //        Console.WriteLine("{3}-> total:{0}, seconds:{1}, {2} set/s"
            //            , total
            //            , (double)(stopwatch.ElapsedMilliseconds / 1000)
            //            , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
            //            , i
            //            );

            //    });
            //}
            

            Console.WriteLine("any key stop");
            Console.ReadLine();
            run = false;

            Console.ReadLine();
        }
    }
}
