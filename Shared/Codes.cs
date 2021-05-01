﻿using System;
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
        REGISTRATION = 3,
        CREATE_ROOM = 4,
        DELETE_ROOM = 5,
        ENTER_ROOM = 6

    }

    public enum ServerCodes
    {
        OK = 100,
        WRONG_USERNAME_OR_PASSWORD_ERROR = 101,
        USER_ALREADY_LOGGED_ERROR = 102,
        REGISTRATION_ERROR = 103,
        USER_ALREADY_EXIST_ERROR = 104,
        CREATE_ROOM_ERROR = 105,
        ROOM_ALREDY_EXIST_ERROR = 106,
        USER_NOT_LOGGED_ERROR = 107,
        USER_LOGGED_ERROR = 108,
        DELETE_ROOM_ERROR = 109,
        ENTER_ROOM_ERROR = 110,

        UNKNOWN_CLIENT_CODE_ERROR = 255
    }
}
