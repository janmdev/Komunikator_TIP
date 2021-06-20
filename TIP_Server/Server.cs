using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TIP_Server
{
    class Server
    {

        private static readonly ushort serverPort = 41234;
        private static ServerEngine serverEngine;
        private static bool runServer;
        private static TCP_Connection tcpConnection;

        static void Main(string[] args) {

            Server.serverEngine = new ServerEngine();
            Server.runServer = true;

            ServerEngine serverEngine = new ServerEngine();

            Task tcpTask = Task.Run(() => {
                Server.tcpConnection = new TCP_Connection(Server.serverPort);
                tcpConnection.Start();
                List<Task> clientsTasks = new List<Task>();
                while (Server.runServer) {
                    TcpClient tcpClient = tcpConnection.GetClient();
                    clientsTasks.Add(Task.Run(() => {
                        Server.serverEngine.ClientProcessAsync(tcpClient);
                    }));
                }
                tcpConnection.Stop();
            });

            Task udpTask = Task.Run(() => {
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, serverPort));
                serverEngine.AudioListenerAsync(udpClient, ref runServer);
            });

            tcpTask.Wait();
            udpTask.Wait();
        }
    }
}
