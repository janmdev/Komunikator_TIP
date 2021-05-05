using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Shared.DataClasses;
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

            while (processClient) {
                var command = TCP_Protocol.Read(stream);
                ClientCodes clientCode = (ClientCodes)command.code;
                string clientDataJSON = command.dataJSON;

                ServerCodes serverCode = ServerCodes.OK;
                switch (clientCode) {
                    case ClientCodes.DISCONNECT:
                        serverCode = Disconnect(CID, ref processClient);
                        break;
                    case ClientCodes.LOGIN:
                        serverCode = Login(CID, clientDataJSON);
                        break;
                    case ClientCodes.LOGOUT:
                        serverCode = Logout(CID);
                        break;
                    case ClientCodes.REGISTRATION:
                        serverCode = Registration(CID, clientDataJSON);
                        break;
                    case ClientCodes.CREATE_ROOM:
                        serverCode = CreateRoom(CID, clientDataJSON);
                        break;
                    case ClientCodes.DELETE_ROOM:
                        serverCode = DeleteRoom(CID, clientDataJSON);
                        break;
                    case ClientCodes.ENTER_ROOM:
                        serverCode = EnterRoom(CID, clientDataJSON);
                        break;
                    default:
                        serverCode = ServerCodes.UNKNOWN_CLIENT_CODE_ERROR;
                        break;
                }
            }
        }

        public void AudioListenerAsync(UdpClient udpAudioListener, ref bool runServer) {
            while (runServer) {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] audioBytes = udpAudioListener.Receive(ref clientEndPoint);
                recivedAudio[clientEndPoint.ToString()].Enqueue(audioBytes);
            }
        }

        private void ClientAudioProcessAsync(long CID) {
            IPEndPoint clientEndPoint = clients[CID].ClientEndPoint;
            string clientEndPointString = clientEndPoint.ToString();
            UdpClient udpAudioSender = new UdpClient();

            while (clients[CID].InRoom) {
                if (recivedAudio.ContainsKey(clientEndPointString) && !recivedAudio[clientEndPointString].IsEmpty) {
                    recivedAudio[clientEndPointString].TryDequeue(out byte[] audioBytes);
                    foreach (long clientInRoom in rooms[clients[CID].CurrentRoomID].ClientsInRoom) {
                        if (clientInRoom == CID) continue;
                        udpAudioSender.Send(audioBytes, audioBytes.Length, clients[clientInRoom].ClientEndPoint);
                    }
                }
            }
        }

        //***** CLIENT CODES METHODS *****

        private ServerCodes Disconnect(long CID, ref bool processClient) {
            processClient = false;
            if (clients[CID].Logged) Logout(CID);
            clients.TryRemove(CID, out _);
            return 0;
        }

        private ServerCodes Login(long CID, string dataJSON) {
            long userID;
            Login loginData = JsonSerializer.Deserialize<Login>(dataJSON);
            if (!Regex.Match(loginData.Username, "^[\\w]{3,16}$").Success) return ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR;
            if (!Regex.Match(loginData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR;
            if ((userID = DatabaseControl.CheckUserPassword(loginData.Username, loginData.Password)) < 0) return ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR;
            if (clients[CID].Logged) return ServerCodes.USER_ALREADY_LOGGED_ERROR;
            clients[CID].UserID = userID;
            clients[CID].Username = loginData.Username;
            clients[CID].Logged = true;
            return ServerCodes.OK;
        }

        private ServerCodes Logout(long CID) {
            clients[CID].UserID = -1;
            clients[CID].Username = "";
            clients[CID].Logged = false;
            clients[CID].InRoom = false;
            clients[CID].Talking = false;
            return 0;
        }

        private ServerCodes Registration(long CID, string dataJSON) {
            if (clients[CID].Logged) return ServerCodes.USER_LOGGED_ERROR;
            Registration registrationData = JsonSerializer.Deserialize<Registration>(dataJSON);
            if (!Regex.Match(registrationData.Username, "^[\\w]{3,16}$").Success) return ServerCodes.REGISTRATION_ERROR;
            if (!Regex.Match(registrationData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return ServerCodes.REGISTRATION_ERROR;
            if (DatabaseControl.CheckIfUserExists(registrationData.Username)) return ServerCodes.USER_ALREADY_EXIST_ERROR;
            if (DatabaseControl.AddNewUser(registrationData.Username, registrationData.Password) < 0) return ServerCodes.REGISTRATION_ERROR;
            return ServerCodes.OK;
        }

        private ServerCodes CreateRoom(long CID, string dataJSON) {
            long roomID;
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            CreateRoom createRoomData = JsonSerializer.Deserialize<CreateRoom>(dataJSON);
            if (!Regex.Match(createRoomData.Name, "^[\\S ]{3,32}$").Success) return ServerCodes.CREATE_ROOM_ERROR;
            if (createRoomData.Description.Length > 255) return ServerCodes.CREATE_ROOM_ERROR;
            if (createRoomData.UsersLimit > 8) return ServerCodes.CREATE_ROOM_ERROR;
            if (DatabaseControl.CheckIfUserExists(createRoomData.Name)) return ServerCodes.ROOM_ALREDY_EXIST_ERROR;
            if ((roomID = DatabaseControl.AddNewRoom(clients[CID].UserID, createRoomData.Name, createRoomData.UsersLimit, createRoomData.Description)) < 0) return ServerCodes.CREATE_ROOM_ERROR;
            rooms.TryAdd(roomID, new Room(clients[CID].UserID, createRoomData.Name, createRoomData.UsersLimit, createRoomData.Description));
            return ServerCodes.OK;
        }

        private ServerCodes DeleteRoom(long CID, string dataJSON) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            DeleteRoom deleteRoomData = JsonSerializer.Deserialize<DeleteRoom>(dataJSON);
            if (!rooms.ContainsKey(deleteRoomData.RoomID)) return ServerCodes.DELETE_ROOM_ERROR;
            if (rooms[deleteRoomData.RoomID].RoomCreatorUserID != clients[CID].UserID) return ServerCodes.DELETE_ROOM_ERROR;
            if (!DatabaseControl.DeleteRoom(deleteRoomData.RoomID)) return ServerCodes.DELETE_ROOM_ERROR;
            rooms.TryRemove(deleteRoomData.RoomID, out _);
            return ServerCodes.OK;
        }

        private ServerCodes EnterRoom(long CID, string dataJSON) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            EnterRoom enterRoomData = JsonSerializer.Deserialize<EnterRoom>(dataJSON);
            if (!rooms.ContainsKey(enterRoomData.RoomID)) return ServerCodes.ENTER_ROOM_ERROR;
            if (clients[CID].AudioProcessTask != null) clients[CID].AudioProcessTask.Wait();
            clients[CID].CurrentRoomID = enterRoomData.RoomID;
            rooms[enterRoomData.RoomID].ClientsInRoom.Add(CID);
            clients[CID].AudioProcessTask = Task.Run(() => ClientAudioProcessAsync(CID));
            return ServerCodes.OK;
        }
        private ServerCodes LeaveRoom(long CID, string dataJSON) {
            if (!clients[CID].Logged) return ServerCodes.USER_NOT_LOGGED_ERROR;
            LeaveRoom leaveRoomData = JsonSerializer.Deserialize<LeaveRoom>(dataJSON);
            if (!rooms.ContainsKey(leaveRoomData.RoomID)) return ServerCodes.ENTER_ROOM_ERROR;
            clients[CID].InRoom = false;
            rooms[leaveRoomData.RoomID].Leave(CID);
            return ServerCodes.OK;
        }

    }
}
