namespace Adf.Cs.DynamicClient
{
    public class TestClient_ITestTestTestServer : Adf.Cs.Client, TestClient.ITest
    {
        public TestClient_ITestTestTestServer()
            : base("Test", "TestServer")
        { }
        public System.Int32 GetParameter(
        System.Int32 value
        )
        {

            object[] parameters = new object[] { value };
            System.Int32 result = base.Command<System.Int32>("GetParameter", parameters);

            return result;
        }
        public System.DateTime GetTime(
        System.Int64 time
        )
        {

            object[] parameters = new object[] { time };
            System.DateTime result = base.Command<System.DateTime>("GetTime", parameters);

            return result;
        }
        public System.Int32[] GetArray(

        )
        {

            object[] parameters = new object[0];
            System.Int32[] result = base.Command<System.Int32[]>("GetArray", parameters);

            return result;
        }
        public System.Int32 RequestArray(
        out System.String[] ids
        )
        {
            ids = new System.String[0];
            object[] parameters = new object[] { ids };
            System.Int32 result = base.Command<System.Int32>("RequestArray", parameters);
            ids = (System.String[])parameters[0];
            return result;
        }
        public System.Int32 GetServerTime(
        System.Int64 clientTime, out System.Int64 serverTime
        )
        {

            serverTime = new System.Int64();
            object[] parameters = new object[] { clientTime, serverTime };
            string hashKey = string.Concat(clientTime, ".", clientTime, ".", clientTime);
            System.Int32 result = base.HashCommand<System.Int32>("GetServerTime", hashKey, parameters);

            serverTime = (System.Int64)parameters[1];
            return result;
        }
        public System.Int32 ModifyGetTime(

        )
        {

            object[] parameters = new object[0];
            System.Int32 result = base.Command<System.Int32>("ModifyGetTime", parameters);

            return result;
        }
        public System.Int32 Error(

        )
        {

            object[] parameters = new object[0];
            System.Int32 result = base.Command<System.Int32>("Error", parameters);

            return result;
        }
    }
}
