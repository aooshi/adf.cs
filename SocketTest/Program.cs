using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace SocketTest
{
    class Program
    {
        const int port = 4562;

        static void Main(string[] args)
        {
            Server();
            //Client();
            AsyncClient();

            Console.ReadLine();
        }


        class ServerSocketState
        {
            public byte[] buffer;
            public Socket socket;
        }

        static void Server()
        {
            var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            System.Threading.ThreadPool.QueueUserWorkItem(sta =>
            {
                var socket = tcpListener.AcceptSocket();
                //
                var state = new ServerSocketState();
                state.socket = socket;
                state.buffer = new byte[1];
                //
                socket.BeginReceive(state.buffer, 0, state.buffer.Length, System.Net.Sockets.SocketFlags.None, ServerCallback, state);
            });
        }


        static void ServerCallback(IAsyncResult ar)
        {
            var state = ar.AsyncState as ServerSocketState;
            state.socket.EndReceive(ar);
            //new receive
            state.socket.BeginReceive(state.buffer, 0, state.buffer.Length, System.Net.Sockets.SocketFlags.None, ServerCallback, state);
            //send
            state.socket.Send(new byte[] { 1 });
        }


        static void Client()
        {
            var tcpClient = new System.Net.Sockets.TcpClient();
            tcpClient.Connect("127.0.0.1", port);

            var socket = tcpClient.Client;

            var run = true;
            int threadCount = 1;
            Console.WriteLine("" + threadCount + " thread single");
            for (int i = 0; i < threadCount; i++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(state =>
                {
                    var total = 0L;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (run)
                    {
                        Command(socket);
                        total++;
                    }
                    stopwatch.Stop();

                    Console.WriteLine("{3}-> total:{0}, seconds:{1}, {2} call/s"
                        , total
                        , (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , i
                        );

                });
            }


            Console.WriteLine("any key stop");
            Console.ReadLine();
            run = false;

            Console.ReadLine();
        }

        static void Command(Socket socket)
        {
            socket.Send(new byte[] { 1 });
            //
            var buffer = new byte[1];
            socket.Receive(buffer);
        }





        class AsyncClientSocketState
        {
            public byte[] buffer;
            public Socket socket;
            public Action<int> callback;
        }
        static void AsyncClient()
        {
            var tcpClient = new System.Net.Sockets.TcpClient();
            tcpClient.Connect("127.0.0.1", port);

            var socket = tcpClient.Client;

            var run = true;
            int threadCount = 1;
            Console.WriteLine("" + threadCount + " thread single async");
            for (int i = 0; i < threadCount; i++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(state =>
                {
                    var total = 0L;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (run)
                    {
                        Command(socket, r =>
                        {
                            total++;
                        });
                    }
                    stopwatch.Stop();

                    Console.WriteLine("{3}-> total:{0}, seconds:{1}, {2} call/s"
                        , total
                        , (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , total / (double)(stopwatch.ElapsedMilliseconds / 1000)
                        , i
                        );

                });
            }


            Console.WriteLine("any key stop");
            Console.ReadLine();
            run = false;

            Console.ReadLine();
        }


        static void Command(Socket socket, Action<int> callback)
        {
            socket.Send(new byte[] { 1 });
            //
            var state = new AsyncClientSocketState();
            state.socket = socket;
            state.buffer = new byte[1];
            state.callback = callback;
            state.socket.BeginReceive(state.buffer, 0, state.buffer.Length, System.Net.Sockets.SocketFlags.None, ClientCallback, state);
        }

        static void ClientCallback(IAsyncResult ar)
        {
            var state = ar.AsyncState as AsyncClientSocketState;
            state.socket.EndReceive(ar);
            //
            state.callback(state.buffer[0]);
        }
    }
}