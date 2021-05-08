using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared;
using TIP_Client.Helpers;

namespace TIP_Client
{
    public static class Client
    {
        public static TcpClient TCP { get; set; }
        private static TcpListener listener { get; set; }

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
        public static async Task<ServerCodes> Register(string login_, string password)
        {
            var code = Shared.ClientCodes.REGISTRATION;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.RegistrationData()
            {
                Username = login_,
                Password = password
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(),Convert.ToByte(code),data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> Login(string login_, string password)
        {
            var code = Shared.ClientCodes.LOGIN;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.LoginData()
            {
                Username = login_,
                Password = password
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> CreateRoom(string name, string  desc, int roomSize)
        {
            var code = Shared.ClientCodes.CREATE_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.CreateRoomData()
            {
                Name = name,
                Description = desc,
                UsersLimit = Convert.ToByte(roomSize)
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> EnterRoom(long id)
        {
            var code = Shared.ClientCodes.ENTER_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.EnterRoomData()
            {
                RoomID = id
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> LeaveRoom(long id)
        {
            var code = Shared.ClientCodes.LEAVE_ROOM;
            var data = @"";
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> DeleteRoom(long id)
        {
            var code = Shared.ClientCodes.DELETE_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.DeleteRoomData()
            {
                RoomID = id
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

        public static async Task<ServerCodes> Logout()
        {
            var code = Shared.ClientCodes.LOGOUT;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), "");
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return Tools.ServerCodesWrapper(code_res);
        }

    }
}