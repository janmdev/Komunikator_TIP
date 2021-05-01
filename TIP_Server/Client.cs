using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TIP_Server
{
    public class Client
    {
        public long UserID { get; set; }
        public string Username { get; set; }
        public bool Logged { get; set; }
        public long CurrentRoomID { get; set; }
        public bool InRoom { get { return CurrentRoomID > -1; } set { if (!value) CurrentRoomID = -1; } }
        public bool Talking { get; set; }
        public EndPoint ClientEndPoint { get; set; }
        public Task AudioProcessTask { get; set; }

        public Client(EndPoint clientEndPoint, long userID = -1, string username = "", bool logged = false, long currentRoomID = -1, bool talking = false, Task audioProcessTask = null) {
            UserID = userID;
            Username = username;
            Logged = logged;
            CurrentRoomID = currentRoomID;
            Talking = talking;
            ClientEndPoint = clientEndPoint;
            AudioProcessTask = audioProcessTask;
        }
    }
}
