using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace TIP_Client.Helpers
{
    public static class Tools
    {

        public static ServerCodes ServerCodesWrapper(byte data)
        {
            if (Convert.ToInt32(data) == 79)
            {

            }
            return (ServerCodes)Convert.ToInt32(data);
        }
    }
}
