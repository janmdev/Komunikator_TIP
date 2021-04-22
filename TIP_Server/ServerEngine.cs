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

namespace TIP_Server
{
    public class ServerEngine
    {
        private long lastSID;

        private ConcurrentDictionary<long, Client> clientsDict;

        private readonly object lastSIDIncLock;

        public ServerEngine() {
            lastSID = 0;
            clientsDict = new ConcurrentDictionary<long, Client>();
            lastSIDIncLock = new object();
        }

        public void ClientProcessAsync(TcpClient tcpClient) {
            NetworkStream stream = tcpClient.GetStream();
            bool processClient = true;
            long SID = 0;

            lock (lastSIDIncLock) {
                SID = lastSID + 1;
                lastSID = SID;
            }

            clientsDict.TryAdd(SID, new Client());

            while (processClient) {
                var command = TCP_Protocol.Read(stream);
                ClientCodes clientCode = (ClientCodes)command.code;
                string clientDataJSON = command.dataJSON;

                ServerCodes serverCode = ServerCodes.OK;
                switch (clientCode) {
                    case ClientCodes.DISCONNECT:
                        serverCode = Disconnect(SID, ref processClient);
                        break;
                    case ClientCodes.LOGIN:
                        serverCode = Login(SID, clientDataJSON);
                        break;
                    case ClientCodes.LOGOUT:
                        serverCode = Logout(SID);
                        break;
                    case ClientCodes.REGISTRATION:
                        serverCode = Registration(clientDataJSON);
                        break;
                }
            }
        }

        //***** CLIENT CODES METHODS *****

        public ServerCodes Disconnect(long SID, ref bool processClient) {
            processClient = false;
            if (clientsDict[SID].Logged) Logout(SID);
            clientsDict.TryRemove(SID, out _);
            return 0;
        }

        public ServerCodes Login(long SID, string dataJSON) {
            Login loginData = JsonSerializer.Deserialize<Login>(dataJSON);
            if (!Regex.Match(loginData.Username, "^[\\w]{3,}$").Success) return ServerCodes.WRONG_USERNAME_OR_PASSWORD;
            if (!Regex.Match(loginData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return ServerCodes.WRONG_USERNAME_OR_PASSWORD;
            if (!DatabaseControl.CheckUserPassword(loginData.Username, loginData.Password)) return ServerCodes.WRONG_USERNAME_OR_PASSWORD;
            clientsDict[SID].Username = loginData.Username;
            clientsDict[SID].Logged = true;
            return ServerCodes.OK;
        }

        public ServerCodes Logout(long SID) {
            clientsDict[SID].Clean();
            return 0;
        }

        public ServerCodes Registration(string dataJSON) {
            Registration registrationData = JsonSerializer.Deserialize<Registration>(dataJSON);
            if (!Regex.Match(registrationData.Username, "^[\\w]{3,}$").Success) return ServerCodes.REGISTRATION_ERROR;
            if (!Regex.Match(registrationData.Password, "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success) return ServerCodes.REGISTRATION_ERROR;
            if (DatabaseControl.AddNewUser(registrationData.Username, registrationData.Password) < 0) return ServerCodes.REGISTRATION_ERROR;
            return ServerCodes.OK;

        }

    }
}
