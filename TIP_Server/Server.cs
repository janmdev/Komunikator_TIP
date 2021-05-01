using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TIP_Server
{
    class Server
    {
        static void Main(string[] args) {

            bool runServer = true;

            TCP_Connection tcpConnection = new TCP_Connection("127.0.0.1", 41234);
            tcpConnection.Start();
            ServerEngine serverEngine = new ServerEngine();

            List<Task> clientsTasks = new List<Task>();

            while (runServer) {
                TcpClient tcpClient = tcpConnection.GetClient();
                clientsTasks.Add(Task.Run(() => {
                    serverEngine.ClientProcessAsync(tcpClient);
                }));
            }

            tcpConnection.Stop();
        }
    }
}
