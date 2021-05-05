using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TIP_Server
{
    class Server
    {
        static void Main(string[] args) {

            string serverIP = "127.0.0.1";
            ushort serverPort = 41234;
            bool runServer = true;

            ServerEngine serverEngine = new ServerEngine();

            Task tcpTask = Task.Run(() => {
                TCP_Connection tcpConnection = new TCP_Connection(serverIP, serverPort);
                tcpConnection.Start();
                List<Task> clientsTasks = new List<Task>();
                while (runServer) {
                    TcpClient tcpClient = tcpConnection.GetClient();
                    clientsTasks.Add(Task.Run(() => {
                        serverEngine.ClientProcessAsync(tcpClient);
                    }));
                }
                tcpConnection.Stop();
            });

            Task udpTask = Task.Run(() => {
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(serverIP), serverPort));
                serverEngine.AudioListenerAsync(udpClient, ref runServer);
            });

            tcpTask.Wait();
            udpTask.Wait();
        }
    }
}
