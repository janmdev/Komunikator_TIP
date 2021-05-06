using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataClasses.Server
{
    public class GetRoomsData
    {
        public class RoomData
        {
            public long RoomID { get; set; }
            public long RoomCreatorUserID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public byte UsersLimit { get; set; }
            public byte UsersInRoomCount { get; set; }
        }

        public List<RoomData> Rooms { get; set; }
        public long RoomsCount { get; set; }
    }
}
