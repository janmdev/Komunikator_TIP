using System.Windows.Documents;

namespace TIP_Client
{
    using Shared;
    using Shared.DataClasses.Server;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using TIP_Client.Helpers;

    public static class Client
    {
        public static TcpClient TCP { get; set; }

        private static TcpListener listener { get; set; }

        public static ConnectionModel connection;
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
                connection = new ConnectionModel()
                {
                    IPAddr = ipAddress,
                    Port = port
                };
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
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
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

        public static async Task<(ServerCodes, string)> CreateRoom(string name, string desc, int roomSize)
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

            return (Tools.ServerCodesWrapper(code_res),data_res);
        }

        public static async Task<(ServerCodes,string)> EnterRoom(long id)
        {
            var code = Shared.ClientCodes.ENTER_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.EnterRoomData()
            {
                RoomID = id
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte codeRes, string dataRes) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(codeRes), dataRes);
        }

        public static async Task<(ServerCodes,string)> LeaveRoom()
        {
            var code = ClientCodes.LEAVE_ROOM;
            var data = @"";
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte codeRes, string dataRes) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(codeRes),dataRes);
        }

        public static async Task<(ServerCodes, string)> DeleteRoom(long id)
        {
            var code = Shared.ClientCodes.DELETE_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.DeleteRoomData()
            {
                RoomID = id
            });
            var length = data.Length;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(code_res),data_res);
        }

        public static async Task<(ServerCodes, string)> Logout()
        {
            var code = Shared.ClientCodes.LOGOUT;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), "");
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(code_res),data_res);
        }

        public static async Task<(ServerCodes, string)> GetRooms()
        {
            var code = Shared.ClientCodes.GET_ROOMS;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), "");
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(code_res), data_res);
        }

        public static async Task<(ServerCodes, string)> GetUsers()
        {
            var code = Shared.ClientCodes.GET_USERS;
            TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), "");
            (byte code_res, string data_res) = await TCP_Protocol.ReadAsync(TCP.GetStream());

            return (Tools.ServerCodesWrapper(code_res), data_res);
        }
    }
}
