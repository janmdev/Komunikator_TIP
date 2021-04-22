using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIP_Client.Helpers
{
    public static class Tools
    {
        public static byte[] ShortsToBytes(short[] shArr)
        {
            List<byte> bytesList = new List<byte>();
            foreach (var sh in shArr)
            {
                bytesList.AddRange(BitConverter.GetBytes(sh));
            }
            return bytesList.ToArray();
        }
    }
}
