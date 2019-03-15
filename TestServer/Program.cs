using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestServer
{
    class Program : Adf.Service.IService
    {
        Adf.Cs.ServerListen listen;

        static void Main(string[] args)
        {
            //var key = "user_{userId}_{abc}";
            //var regex = new System.Text.RegularExpressions.Regex(@"\{([\w]+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);
            //var index = 0;
            //foreach (System.Text.RegularExpressions.Match match in regex.Matches(key))
            //{
            //    Console.WriteLine(match.Groups[1].Value);
            //    Console.WriteLine(match.Value);

            //    key = key.Replace(match.Value, "{0}");
            //}

            //Console.WriteLine(key);

            //Console.Read();

            Adf.Service.ServiceHelper.Entry(args);
        }

        public System.Net.HttpStatusCode HttpAction(string action, Adf.Service.ServiceContext serviceContext, Adf.HttpServerContext httpServerContext)
        {
            return System.Net.HttpStatusCode.OK;
        }

        public void Initialize(Adf.Service.ServiceContext serviceContext)
        {
        }

        public void Start(Adf.Service.ServiceContext serviceContext)
        {
            this.listen = new Adf.Cs.ServerListen(serviceContext.LogManager);

            //if (serviceContext.Registry.Enable)
            //{
            //    var node = new Dictionary<string, object>();
            //    node.Add("group", serviceContext.Setting.ServiceName);
            //    node.Add("port", this.listen.Port);

            //    serviceContext.Registry.RegisterNode(node);
            //}
        }

        public string Status(Adf.Service.ServiceContext serviceContext, System.Collections.Specialized.NameValueCollection queryString)
        {
            var htmlBuilder = new StringBuilder();
            htmlBuilder.AppendLine();
            htmlBuilder.AppendFormat("<div>Session Count: {0}</div>", this.listen.SessionCount);
            htmlBuilder.AppendLine();


            return htmlBuilder.ToString();
        }

        public void Stop(Adf.Service.ServiceContext serviceContext)
        {
            this.listen.Dispose();
            this.listen = null;
        }

        public void Dispose()
        {

        }
    }
}
