using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public enum ClientCodes
    {
        DISCONNECT = 0,
        LOGIN = 1,
        LOGOUT = 2,
        REGISTRATION = 3

    }

    public enum ServerCodes
    {
        OK = 100,
        WRONG_USERNAME_OR_PASSWORD = 101,
        REGISTRATION_ERROR = 102
    }
}
