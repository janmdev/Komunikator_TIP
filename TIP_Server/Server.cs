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

            ushort serverPort = 41234;
            bool runServer = true;

            ServerEngine serverEngine = new ServerEngine();

            Task tcpTask = Task.Run(() => {
                TCP_Connection tcpConnection = new TCP_Connection(serverPort);
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
                UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, serverPort));
                serverEngine.AudioListenerAsync(udpClient, ref runServer);
            });

            tcpTask.Wait();
            udpTask.Wait();
        }
    }
}
