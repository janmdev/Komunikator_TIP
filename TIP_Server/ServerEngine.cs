using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Shared.DataClasses.Client;
using Shared.DataClasses.Server;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Net;

namespace TIP_Server
{
    public class ServerEngine
    {
        private long lastCID;

        private ConcurrentDictionary<long, Client> clients;
        private ConcurrentDictionary<long, Room> rooms;
        private ConcurrentDictionary<string, ConcurrentQueue<byte[]>> recivedAudio;

        private readonly object lastCIDIncLock;

        public ServerEngine() {
            lastCID = 0;
            lastCIDIncLock = new object();
            clients = new ConcurrentDictionary<long, Client>();
            rooms = DatabaseControl.GetRooms();
            recivedAudio = new ConcurrentDictionary<string, ConcurrentQueue<byte[]>>();
        }

        public void ClientProcessAsync(TcpClient tcpClient) {
            NetworkStream stream = tcpClient.GetStream();
            IPEndPoint clientEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            bool processClient = true;
            long CID = 0;

            lock (lastCIDIncLock) {
                CID = lastCID + 1;
                lastCID = CID;
            }

            clients.TryAdd(CID, new Client(clientEndPoint));

            //TODO: Talking
            while (processClient) {
                var command = TCP_Protocol.Read(stream);
                ClientCodes clientCode = (ClientCodes)command.code;
                string clientDataJSON = command.dataJSON;

                ServerCodes serverCode = ServerCodes.OK;
                string serverDataJSON = "";
                switch (clientCode) {
                    case ClientCodes.DISCONNECT:
                        serverCode = DisconnectMethod(CID, ref processClient);
                        break;
                    case ClientCodes.LOGIN:
                        (serverCode, serverDataJSON) = LoginMethod(CID, clientDataJSON);
                        break;
                    case ClientCodes.LOGOUT:
                        serverCode = LogoutMethod(CID);
                        break;
                    case ClientCodes.REGISTRATION:
                        serverCode = RegistrationMethod(CID, clientDataJSON);
                        break;
                    case ClientCodes.CREATE_ROOM:
                        serverCode = CreateRoomMethod(CID, clientDataJSON);
                        break;
                    case ClientCodes.DELETE_ROOM:
                        serverCode = DeleteRoomMethod(CID, clientDataJSON);
                        break;
                    case ClientCodes.ENTER_ROOM:
                        serverCode = EnterRoomMethod(CID, clientDataJSON);
                        break;
                    case ClientCodes.LEAVE_ROOM:
                        serverCode = LeaveRoomMethod(CID);
                        break;
                    case ClientCodes.GET_ROOMS:
                        (serverCode, serverDataJSON) = GetRoomsMethod(CID);
                        break;
                    case ClientCodes.GET_USERS:
                        (serverCode, serverDataJSON) = GetUsersMethod(CID);
                        break;
                    default:
                        serverCode = ServerCodes.UNKNOWN_CLIENT_CODE_ERROR;
                        break;
                }

                TCP_Protocol.Send(stream, (byte)serverCode, serverDataJSON);
            }
        }

        public void AudioListenerAsync(UdpClient udpAudioListener, ref bool runServer) {
            while (runServer) {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] audioBytes = udpAudioListener.Receive(ref clientEndPoint);
                if (!recivedAudio.ContainsKey(clientEndPoint.ToString()) || (recivedAudio[clientEndPoint.ToString()] == null))
                {
                    recivedAudio[clientEndPoint.ToString()] = new ConcurrentQueue<byte[]>();
                }
                recivedAudio[clientEndPoint.ToString()].Enqueue(audioBytes);
            }
        }

        private void ClientAudioProcessAsync(long CID) {
            IPEndPoint clientEndPoint = clients[CID].ClientEndPoint;
            string clientEndPointString = clientEndPoint.ToString();
            UdpClient udpAudioSender = new UdpClient();

            while (clients[CID].InRoom) {
                if (recivedAudio.ContainsKey(clientEndPointString) && !recivedAudio[clientEndPointString].IsEmpty) {
                    clients[CID].Talking = true;
                    recivedAudio[clientEndPointString].TryDequeue(out byte[] audioBytes);
                    foreach (long clientInRoom in rooms[clients[CID].CurrentRoomID].ClientsInRoom) {
                        if (clientInRoom == CID) continue;
                        var listBytes = audioBytes.ToList();
                        listBytes.Insert(0, Convert.ToByte(rooms[clients[CID].CurrentRoomID].ClientsInRoom.IndexOf(clientInRoom)));
                        audioBytes = listBytes.ToArray();
                        udpAudioSender.Send(audioBytes, audioBytes.Length, clients[clientInRoom].ClientEndPoint);
                    }
                }
            }
        }

        private void ClientTalkingInformationAsync(long CID) {
            while (clients[CID].InRoom) {
                if (clients[CID].TalkingTimer > 0) clients[CID].TalkingTimer--;
                Task.Delay(100);
            }

        }

        //***** CLIENT CODES METHODS *****

        private ServerCodes DisconnectMethod(long CID, ref bool processClient) {
            processClient = false;
            if (clients[CID].Logged) LogoutMethod(CID);
            clients.TryRemove(CID, out _);
            return 0;
        }

        private (ServerCodes,string) LoginMethod(long CID, string dataJSON) {
            long userID;
            LoginData loginData = JsonSerializer.Deserialize<LoginData>(dataJSON);
#if DEBUG
#else
            if (!Regex.Match(loginData.Username, "^[\\w]{3,16}$").Success) return (ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR,"");
            if (!Regex.Match(loginData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return (ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR,"");
#endif
            if ((userID = DatabaseControl.CheckUserPassword(loginData.Username, loginData.Password)) < 0) return (ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR,"");
            if (clients[CID].Logged) return (ServerCodes.USER_ALREADY_LOGGED_ERROR,"");
            clients[CID].UserID = userID;
            clients[CID].Username = loginData.Username;
            clients[CID].Logged = true;
            return (ServerCodes.OK,JsonSerializer.Serialize(new GetUserLogin() { ClientID = userID }));
        }

        private ServerCodes LogoutMethod(long CID) {
            clients[CID].UserID = -1;
            clients[CID].Username = "";
            clients[CID].Logged = false;
            clients[CID].InRoom = false;
            clients[CID].Talking = false;
            return 0;
        }

        private ServerCodes RegistrationMethod(long CID, string dataJSON) {
            if (clients[CID].Logged) return ServerCodes.USER_LOGGED_ERROR;
            RegistrationData registrationData = JsonSerializer.Deserialize<RegistrationData>(dataJSON);
#if DEBUG
#else
            if (!Regex.Match(registrationData.Username, "^[\\w]{3,16}$").Success) return ServerCodes.REGISTRATION_ERROR;
            if (!Regex.Match(registrationData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return ServerCodes.REGISTRATION_ERROR;
#endif
            if (DatabaseControl.CheckIfUserExists(registrationData.Username)) return ServerCodes.USER_ALREADY_EXIST_ERROR;
            if (DatabaseControl.AddNewUser(registrationData.Username, registrationData.Password) < 0) return ServerCodes.REGISTRATION_ERROR;
            return ServerCodes.OK;
        }

        private ServerCodes CreateRoomMethod(long CID, string dataJSON) {
            long roomID;
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            CreateRoomData createRoomData = JsonSerializer.Deserialize<CreateRoomData>(dataJSON);
            if (!Regex.Match(createRoomData.Name, "^[\\S ]{3,32}$").Success) return ServerCodes.CREATE_ROOM_ERROR;
            if (createRoomData.Description.Length > 255) return ServerCodes.CREATE_ROOM_ERROR;
            if (createRoomData.UsersLimit > 8) return ServerCodes.CREATE_ROOM_ERROR;
            if (DatabaseControl.CheckIfUserExists(createRoomData.Name)) return ServerCodes.ROOM_ALREDY_EXIST_ERROR;
            if ((roomID = DatabaseControl.AddNewRoom(clients[CID].UserID, createRoomData.Name, createRoomData.UsersLimit, createRoomData.Description)) < 0) {
                return ServerCodes.CREATE_ROOM_ERROR;
            }
            rooms.TryAdd(roomID, new Room(clients[CID].UserID, createRoomData.Name, createRoomData.UsersLimit, createRoomData.Description));
            return ServerCodes.OK;
        }

        private ServerCodes DeleteRoomMethod(long CID, string dataJSON) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            DeleteRoomData deleteRoomData = JsonSerializer.Deserialize<DeleteRoomData>(dataJSON);
            if (!rooms.ContainsKey(deleteRoomData.RoomID)) return ServerCodes.DELETE_ROOM_ERROR;
            if (rooms[deleteRoomData.RoomID].RoomCreatorUserID != clients[CID].UserID) return ServerCodes.DELETE_ROOM_ERROR;
            if (!DatabaseControl.DeleteRoom(deleteRoomData.RoomID)) return ServerCodes.DELETE_ROOM_ERROR;
            rooms.TryRemove(deleteRoomData.RoomID, out _);
            return ServerCodes.OK;
        }

        private ServerCodes EnterRoomMethod(long CID, string dataJSON) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            EnterRoomData enterRoomData = JsonSerializer.Deserialize<EnterRoomData>(dataJSON);
            if (!rooms.ContainsKey(enterRoomData.RoomID)) return ServerCodes.ENTER_ROOM_ERROR;
            if (clients[CID].AudioProcessTask != null) clients[CID].AudioProcessTask.Wait();
            clients[CID].CurrentRoomID = enterRoomData.RoomID;
            rooms[enterRoomData.RoomID].ClientsInRoom.Add(CID);
            clients[CID].AudioProcessTask = Task.Run(() => ClientAudioProcessAsync(CID));
            clients[CID].TalkingInformationTask = Task.Run(() => ClientTalkingInformationAsync(CID));
            return ServerCodes.OK;
        }

        private ServerCodes LeaveRoomMethod(long CID) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            if (!clients[CID].InRoom) return ServerCodes.LEAVE_ROOM_ERROR;
            long roomID = clients[CID].CurrentRoomID;
            if (!rooms.ContainsKey(roomID)) return ServerCodes.LEAVE_ROOM_ERROR;
            clients[CID].Talking = false;
            clients[CID].InRoom = false;
            rooms[roomID].Leave(CID);
            return ServerCodes.OK;
        }

        private (ServerCodes serverCode, string serverDataJSON) GetRoomsMethod(long CID) {
            if (!clients[CID].Logged) return (ServerCodes.USER_NOT_LOGGED_ERROR, "");
            if (rooms.IsEmpty) return (ServerCodes.NO_ROOMS_ERROR, "");
            GetRoomsData getRoomsData = new GetRoomsData {
                Rooms = new List<GetRoomsData.RoomData>(),
                RoomsCount = rooms.Count
            };
            foreach (KeyValuePair<long, Room> r in rooms) {
                getRoomsData.Rooms.Add(new GetRoomsData.RoomData {
                    RoomID = r.Key,
                    RoomCreatorUserID = r.Value.RoomCreatorUserID,
                    Name = r.Value.RoomName,
                    Description = r.Value.Description,
                    UsersLimit = r.Value.UsersLimit,
                    UsersInRoomCount = r.Value.ClientsInRoomCount
                });
            }
            return (ServerCodes.OK, JsonSerializer.Serialize(getRoomsData));
        }

        private (ServerCodes serverCode, string serverDataJSON) GetUsersMethod(long CID) {
            if (!clients[CID].Logged) return (ServerCodes.USER_NOT_LOGGED_ERROR, "");
            if (!clients[CID].InRoom) return (ServerCodes.USER_NOT_IN_ROOM_ERROR, "");
            GetUsersData getUsersData = new GetUsersData {
                Users = new List<GetUsersData.UserData>(),
                UsersCount = rooms[clients[CID].CurrentRoomID].ClientsInRoomCount
            };
            foreach (long u in rooms[clients[CID].CurrentRoomID].ClientsInRoom) {
                getUsersData.Users.Add(new GetUsersData.UserData {
                    UserID = clients[u].UserID,
                    UserName = clients[u].Username,
                    Talking = clients[u].Talking
                });
            }
            return (ServerCodes.OK, JsonSerializer.Serialize(getUsersData));
        }
    }
}
