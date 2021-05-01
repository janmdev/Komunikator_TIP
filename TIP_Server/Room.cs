using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace TIP_Server
{
    public class Room
    {
        private List<long> clientsInRoom; //Using CID
        private readonly object clientsListLock;

        public long RoomCreatorUserID { get; set; }
        public string RoomName { get; set; }
        public byte UsersLimit { get; set; }
        public string Description { get; set; }
        public List<long> ClientsInRoom { get { lock (clientsListLock) return clientsInRoom; } }
        public bool Active { get { return ClientsInRoom.Count > 0; } }

        public Room(long roomCreatorUserID, string roomName, byte usersLimit, string description) {
            RoomCreatorUserID = roomCreatorUserID;
            RoomName = roomName;
            UsersLimit = usersLimit;
            Description = description;
            clientsInRoom = new List<long>();
            clientsListLock = new object();
        }

        public void Enter(long CID) {
            lock (clientsListLock) {
                if (!clientsInRoom.Contains(CID)) clientsInRoom.Add(CID);
            }
        }

        public void Leave(long CID) {
            lock (clientsListLock) {
                if (clientsInRoom.Contains(CID)) clientsInRoom.Remove(CID);
            }
        }

    }
}
