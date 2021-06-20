using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TIP_Server
{
    public class TCP_Connection
    {
        private readonly TcpListener tcpListener;

        public TCP_Connection(ushort port) {
            this.tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public void Start() {
            tcpListener.Start();
        }


        public void Stop() {
            tcpListener.Stop();
        }

        public TcpClient GetClient() {
            return tcpListener.AcceptTcpClient();
        }
    }
}
