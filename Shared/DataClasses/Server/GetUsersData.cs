using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataClasses.Server
{
    public class GetUsersData
    {
        public class UserData
        {
            public long UserID { get; set; }
            public string UserName { get; set; }
            public bool Talking { get; set; }
        }

        public List<UserData> Users { get; set; }
        public byte UsersCount { get; set; }
    }
}
