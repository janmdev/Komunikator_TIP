using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared;

namespace TIP_Client
{
    public static class Client
    {
        public static TcpClient TCP { get; set; }
        private static TcpListener listener { get; set; }
        private static string login;
        public static string ApiKey;

        public static async void Send(string message)
        {
            await TCP.GetStream().WriteAsync(Encoding.UTF8.GetBytes(message));
        }

        public static async Task<bool> Connect(string ipAddress, int port)
        {
            TCP = new TcpClient();
            if (TCP.Connected)
            {
                return true;
            }

            try
            {
                await TCP.ConnectAsync(IPAddress.Parse(ipAddress), port);
                listener = new TcpListener(IPAddress.Parse(ipAddress), port);
                return true;
            }
            catch (FormatException)
            {
                throw new Exception("Błędny format adresu ip");
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async static Task<bool> Register(string login_, string password)
        {
            string md5 = Hasher.CreateMD5(password);
            var code = Shared.ClientCodes.REGISTRATION;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Registration()
            {
                Username = login_,
                Password = md5
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(),Convert.ToByte(code),data);
            string result = await getUserInput(new byte[1024]);

            if (result == "ACCOUNTCREATED")
            {
                //login = login_;
                return true;
            }
            else return false;
        }


        private async static Task<string> getUserInput(byte[] buffer)
        {
            await TCP.GetStream().ReadAsync(buffer, 0, buffer.Length);
            //await TCP.GetStream().ReadAsync(new byte[10]);
            return Encoding.UTF8.GetString(buffer).Trim().Replace("\0", String.Empty);
        }
    }
}