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
        public bool Talking { get { return TalkingTimer > 0; } set { if (value) TalkingTimer = 10; else TalkingTimer = 0; } }
        public int TalkingTimer { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }
        public Task AudioProcessTask { get; set; }
        public Task TalkingInformationTask { get; set; }

        public Client(IPEndPoint clientEndPoint, long userID = -1, string username = "", bool logged = false, long currentRoomID = -1, int talingTimer = 0, Task audioProcessTask = null, Task tudioInformationTask = null) {
            UserID = userID;
            Username = username;
            Logged = logged;
            CurrentRoomID = currentRoomID;
            TalkingTimer = talingTimer;
            ClientEndPoint = clientEndPoint;
            AudioProcessTask = audioProcessTask;
            TalkingInformationTask = tudioInformationTask;
        }
    }
}
