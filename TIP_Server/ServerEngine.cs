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
                string dataJSON = command.dataJSON;

                ServerCodes serverCode = ServerCodes.OK;
                switch (clientCode) {
                    case ClientCodes.DISCONNECT:
                        serverCode = Disconnect(SID, ref processClient);
                        break;
                    case ClientCodes.LOGIN:
                        serverCode = Login(SID, dataJSON);
                        break;
                    case ClientCodes.LOGOUT:
                        serverCode = Logout(SID);
                        break;
                    case ClientCodes.REGISTRATION:
                        serverCode = Registration(dataJSON);
                        break;
                }
            }
        }

        //***** OPTIONS METHODS *****

        public ServerCodes Disconnect(long SID, ref bool processClient) {
            processClient = false;
            if (clientsDict[SID].Logged) Logout(SID);
            clientsDict.TryRemove(SID, out _);
            return 0;
        }

        public ServerCodes Login(long SID, string dataJSON) {
            Login loginData = JsonSerializer.Deserialize<Login>(dataJSON);
            //TODO: Server data validation
            if (DatabaseControl.CheckUserPassword(loginData.Username, loginData.Password)) {
                clientsDict[SID].Username = loginData.Username;
                clientsDict[SID].Logged = true;
                return ServerCodes.OK;
            } 
            else return ServerCodes.WRONG_USERNAME_OR_PASSWORD;
        }

        public ServerCodes Logout(long SID) {
            clientsDict[SID].Clean();
            return 0;
        }

        public ServerCodes Registration(string dataJSON) {
            Registration registrationData = JsonSerializer.Deserialize<Registration>(dataJSON);
            //TODO: Server data validation
            if (DatabaseControl.AddNewUser(registrationData.Username, registrationData.Password) >= 0) return ServerCodes.OK;
            else return ServerCodes.REGISTRATION_ERROR;

        }

    }
}
