using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using System.Data;
using Adf.Db;
using System.Collections.Generic;
using ProtoBuf;
using System.IO;
using System.Collections;

namespace TestClient
{
    public class SerializeTest
    {
        public static void Test()
        {
            var userinfo = new SerializeTestUserInfo();
            userinfo.EmailState = 1;
            userinfo.FaceBookName = Guid.NewGuid().ToString("N");
            userinfo.FaceBookState = 1;
            userinfo.Follow = 100;
            userinfo.GoogleName = Guid.NewGuid().ToString("N");
            userinfo.GoogleState = 2;
            userinfo.PhoneState = 3;
            userinfo.Subscribe = 10;
            userinfo.ToFollow = 1000;
            userinfo.TwitterName = Guid.NewGuid().ToString("N");
            userinfo.TwitterState = 132;
            userinfo.UserAvatarUrl = "http://www.example.com/useravatar";
            userinfo.UserId = 1;

            int size = 10000;


            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < size; i++)
            {
                var u = userinfo.Clone();
                Adf.JsonHelper.Serialize(u);
            }
            stopwatch.Stop();
            Console.WriteLine("json:"+stopwatch.ElapsedMilliseconds);


            stopwatch.Reset();
            stopwatch.Start();
            for (int i = 0; i < size; i++)
            {
                var u = userinfo.Clone();
                using (var m = new MemoryStream())
                {
                    ProtoBuf.Serializer.NonGeneric.Serialize(m, u);
                }
            }
            stopwatch.Stop();
            Console.WriteLine("protobuf:" + stopwatch.ElapsedMilliseconds);



            Console.ReadLine();
        }

        private static SerializeTestUserInfo createUserInfo()
        {
            var userinfo = new SerializeTestUserInfo();
            userinfo.EmailState = 1;
            userinfo.FaceBookName = Guid.NewGuid().ToString("N");
            userinfo.FaceBookState = 1;
            userinfo.Follow = 100;
            userinfo.GoogleName = Guid.NewGuid().ToString("N");
            userinfo.GoogleState = 2;
            userinfo.PhoneState = 3;
            userinfo.Subscribe = 10;
            userinfo.ToFollow = 1000;
            userinfo.TwitterName = Guid.NewGuid().ToString("N");
            userinfo.TwitterState = 132;
            userinfo.UserAvatarUrl = "http://www.example.com/useravatar";
            userinfo.UserId = 1;
            return userinfo;
        }

        public static void Test2()
        {
            var userInfo = createUserInfo();

            var list = new List<SerializeTestUserInfo>(){
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                 createUserInfo(),
                };


            //deimail 不允许强转
            //userInfo.FaceBookState = (decimal)((object)1.0d);
            var listType = new List<int>().GetType();
            var total = 0L;
            var count = 1000000;
            byte[] dataBuffer = null,protoBuffer = null,csDataBuffer=null;
            string jsonBuffer = null;
            var stopwatch = new Stopwatch();
            stopwatch.Reset();
            stopwatch.Start();
            while (++total != 20000)
            {
                //Adf.DataSerializable.DefaultInstance.Serialize(new Decimal[] { 1, -20, 3, 4, 5, 6, 7, 8, 9, 0 });
                //Adf.Cs.CsSerializer.Serialize(new Decimal[] { 1, -20, 3, 4, 5, 6, 7, 8, 9, 0 });

                //dataBuffer = Adf.DataSerializable.DefaultInstance.Serialize(userInfo);
                //var df = Adf.DataSerializable.DefaultInstance.Deserialize(typeof(Adf.Cs.CsInfo), bf);
                
                //var af = Adf.Cs.CsDataSerializer.Instance.Serialize(list);
                //var bf = (List<SerializeTestUserInfo>)Adf.Cs.CsDataSerializer.Instance.Deserialize(typeof(List<SerializeTestUserInfo>), af);
                //var cf = Adf.Cs.CsSerializer.Serialize(list);
                //var ef = Adf.Cs.CsSerializer.Deserialize(typeof(Adf.Cs.CsInfo),cf);
                
                //var ae = Adf.DataSerializable.DefaultInstance.Serialize(list.ToArray());
                //var be = Adf.DataSerializable.DefaultInstance.Serialize(list);



                //Adf.DataSerializable.DefaultInstance.Serialize(new SerializeTestUserInfo[]{
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo()
                //});

                //Adf.Cs.CsSerializer.Serialize(new SerializeTestUserInfo[]{
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo()
                //});

                //Adf.JsonHelper.Serialize(new SerializeTestUserInfo[]{
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo(),
                // new SerializeTestUserInfo()
                //});
            }
            stopwatch.Stop();
            Console.WriteLine("total:{0}, seconds:{1}, {2} call/s"
                , total
                , (double)(stopwatch.ElapsedMilliseconds / 1000)
                , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                );
            //
            total = 0;
            stopwatch.Reset();
            stopwatch.Start();
            //while (++total != count)
            //{
            //    csDataBuffer = Adf.Cs.CsDataSerializer.Instance.Serialize(userInfo);
            //}
            stopwatch.Stop();
            Console.WriteLine("CsDataSerializer.Serialize total:{0}, seconds:{1}, {2} call/s"
                , total
                , (double)(stopwatch.ElapsedMilliseconds / 1000)
                , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                );
            //
            //
            total = 0;
            stopwatch.Reset();
            stopwatch.Start();
            while (++total != count)
            {
                dataBuffer = Adf.DataSerializable.DefaultInstance.Serialize(userInfo);
            }
            stopwatch.Stop();
            Console.WriteLine("Adf.DataSerializable.DefaultInstance.Serialize total:{0}, seconds:{1}, {2} call/s"
                , total
                , (double)(stopwatch.ElapsedMilliseconds / 1000)
                , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                );
            //
            total = 0;
            stopwatch.Reset();
            stopwatch.Start();
            while (++total != count)
            {
                protoBuffer = Adf.Cs.CsSerializer.Serialize(userInfo);
            }
            stopwatch.Stop();
            Console.WriteLine("Adf.Cs.CsSerializer.Serialize total:{0}, seconds:{1}, {2} call/s"
                , total
                , (double)(stopwatch.ElapsedMilliseconds / 1000)
                , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                );
            //
            //total = 0;
            //stopwatch.Reset();
            //stopwatch.Start();
            //while (++total != count)
            //{
            //    var bf = Adf.Cs.CsDataSerializer.Instance.Deserialize(typeof(SerializeTestUserInfo), csDataBuffer);
            //}
            //stopwatch.Stop();
            //Console.WriteLine("CsDataSerializer.Deserialize total:{0}, seconds:{1}, {2} call/s"
            //    , total
            //    , (double)(stopwatch.ElapsedMilliseconds / 1000)
            //    , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
            //    );
            
            total = 0;
            stopwatch.Reset();
            stopwatch.Start();
            while (++total != count)
            {
                var bf = Adf.DataSerializable.DefaultInstance.Deserialize(typeof(SerializeTestUserInfo), dataBuffer);
            }
            stopwatch.Stop();
            Console.WriteLine("Adf.DataSerializable.DefaultInstance.Deserialize total:{0}, seconds:{1}, {2} call/s"
                , total
                , (double)(stopwatch.ElapsedMilliseconds / 1000)
                , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                );
            //
            //total = 0;
            //stopwatch.Reset();
            //stopwatch.Start();
            //while (++total != count)
            //{
            //    var pf = Adf.Cs.CsSerializer.Deserialize(typeof(SerializeTestUserInfo), protoBuffer);
            //}
            //stopwatch.Stop();
            //Console.WriteLine("Adf.Cs.CsSerializer.Deserialize total:{0}, seconds:{1}, {2} call/s"
            //    , total
            //    , (double)(stopwatch.ElapsedMilliseconds / 1000)
            //    , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
            //    );

            Console.Read();
            Environment.Exit(0);
        }
    }


    /// <summary>
    /// UserInfo
    /// </summary>
    [ProtoContract]
    public class SerializeTestUserInfo 
    {
        /// <summary>
        /// Initialize
        /// </summary>
        public SerializeTestUserInfo() { }
        
        /// <summary>
        /// clone a new object
        /// </summary>
        public object Clone()
        {
            var obj = new SerializeTestUserInfo();
            obj.UserId = this.UserId;
            obj.Follow = this.Follow;
            obj.ToFollow = this.ToFollow;
            obj.Subscribe = this.Subscribe;
            obj.PhoneState = this.PhoneState;
            obj.EmailState = this.EmailState;
            obj.FaceBookState = this.FaceBookState;
            obj.TwitterState = this.TwitterState;
            obj.GoogleState = this.GoogleState;
            obj.FaceBookName = this.FaceBookName;
            obj.TwitterName = this.TwitterName;
            obj.GoogleName = this.GoogleName;
            obj.UserAvatarUrl = this.UserAvatarUrl;
            return obj;
        }
        /// <summary>
        /// 用户ID
        /// </summary>
        [ProtoMember(1)]
        
        public UInt16 UserId { get; set; }
        /// <summary>
        /// 关注数
        /// </summary>
        [ProtoMember(2)]
        
        public UInt32 Follow { get; set; }
        /// <summary>
        /// 被关注数
        /// </summary>
        [ProtoMember(3)]
        
        public UInt64 ToFollow { get; set; }
        /// <summary>
        /// 订阅数
        /// </summary>
        [ProtoMember(4)]
        
        public long Subscribe { get; set; }
        /// <summary>
        /// 手机认证状态'0'未认证，'1'已认证
        /// </summary>
        [ProtoMember(5)]
        
        public short PhoneState { get; set; }
        /// <summary>
        /// 邮箱认证状态'2'未认证，'4'已认证
        /// </summary>
        [ProtoMember(6)]
        
        public byte EmailState { get; set; }

        /// <summary>
        /// FaceBook认证状态'2'未认证，'4'已认证
        /// </summary>
        [ProtoMember(7)]
        
        public decimal FaceBookState { get; set; }

        /// <summary>
        /// Twitter认证状态'2'未认证，'4'已认证
        /// </summary>
        [ProtoMember(8)]
        
        public double TwitterState { get; set; }

        /// <summary>
        /// Google认证状态'2'未认证，'4'已认证
        /// </summary>
        [ProtoMember(9)]
        
        public float GoogleState { get; set; }

        /// <summary>
        /// FaceBook账号
        /// </summary>
        [ProtoMember(10)]
        
        public string FaceBookName { get; set; }

        /// <summary>
        /// Twitter账号
        /// </summary>
        [ProtoMember(11)]
        
        public string TwitterName { get; set; }

        /// <summary>
        /// Google账号
        /// </summary>
        [ProtoMember(12)]

        public string GoogleName { get; set; }

        /// <summary>
        ///用户头像链接地址
        /// </summary>
        [ProtoMember(13)]

        public string UserAvatarUrl { get; set; }

        /// <summary>
        ///用户头像链接地址
        /// </summary>
        [ProtoMember(14)]

        public int Status { get; set; }


    }
}
