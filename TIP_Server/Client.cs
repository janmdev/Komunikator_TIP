using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIP_Server
{
    public class Client
    {
        public string Username { get; set; }
        public bool Logged { get; set; }
        public bool InRoom { get; set; }
        public bool Talking { get; set; }

        public Client(string username = "", bool logged = false, bool inRoom = false, bool talking = false) {
            Username = username;
            Logged = logged;
            InRoom = inRoom;
            Talking = talking;
        }

        public void Clean() {
            Username = "";
            Logged = false;
            InRoom = false;
            Talking = false;
        }
    }
}
