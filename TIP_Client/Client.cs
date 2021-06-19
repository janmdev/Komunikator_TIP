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
        public static long? ClientID { get; set; }
        private static TcpListener listener { get; set; }
        private static object serverConvLock = new object();
        public static ConnectionModel connection;
        public static async Task<bool> Connect(string ipAddress, int port)
        {
            TCP = new TcpClient(AddressFamily.InterNetwork);

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


        public static (ServerCodes,string) Register(string login_, string password)
        {
            var code = Shared.ClientCodes.REGISTRATION;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.RegistrationData()
            {
                Username = login_,
                Password = password
            });

            return SeverConv(code,data);
        }

        public static (ServerCodes,string) Login(string login_, string password)
        {
            var code = Shared.ClientCodes.LOGIN;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.LoginData()
            {
                Username = login_,
                Password = password
            });

            return SeverConv(code,data);
        }

        public static (ServerCodes, string) CreateRoom(string name, string desc, int roomSize)
        {
            var code = Shared.ClientCodes.CREATE_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.CreateRoomData()
            {
                Name = name,
                Description = desc,
                UsersLimit = Convert.ToByte(roomSize)
            });

            return SeverConv(code,data);
        }

        public static (ServerCodes,string) EnterRoom(long id)
        {
            var code = Shared.ClientCodes.ENTER_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.EnterRoomData()
            {
                RoomID = id
            });

            return SeverConv(code,data);
        }

        public static (ServerCodes,string) LeaveRoom()
        {
            var code = ClientCodes.LEAVE_ROOM;
            return SeverConv(code);
        }

        public static (ServerCodes, string) DeleteRoom(long id)
        {
            var code = Shared.ClientCodes.DELETE_ROOM;
            var data = JsonSerializer.Serialize(new Shared.DataClasses.Client.DeleteRoomData()
            {
                RoomID = id
            });
            var length = data.Length;

            return SeverConv(code,data);
        }

        public static (ServerCodes, string) Logout()
        {
            var code = Shared.ClientCodes.LOGOUT;
            return SeverConv(code);
        }

        public static (ServerCodes, string) GetRooms()
        {
            var code = Shared.ClientCodes.GET_ROOMS;
            return SeverConv(code);
        }

        private static (ServerCodes, string) SeverConv(ClientCodes code, string data = "")
        {
            byte code_res;
            string data_res;
            lock (serverConvLock)
            {
                TCP_Protocol.Send(TCP.GetStream(), Convert.ToByte(code), data);
                (code_res, data_res) = TCP_Protocol.Read(TCP.GetStream());
            }

            return (Tools.ServerCodesWrapper(code_res), data_res);
        }

        public static (ServerCodes, string) GetUsers()
        {
            var code = Shared.ClientCodes.GET_USERS;
            return SeverConv(code);
        }
    }
}
