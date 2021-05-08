using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{

    //  Protokół 
    //              +-----------------------------------------+
    //  Bajty:      |  0  |  1  |  2  |        3-65538        |
    //  Zawartość:  |Code | Data size |      Data (JSON)      |
    //              +-----------------------------------------+

    public static class TCP_Protocol
    {
        private const int codeBufferSize = 1;
        private const int dataSizeBufferSize = 2;
        private const int dataBufferSize = 1024;

        public static void Send(NetworkStream stream, byte code, string dataJSON) {
            byte[] codeBuffer = new byte[codeBufferSize];
            byte[] dataSizeBuffer = new byte[dataSizeBufferSize];
            byte[] dataJSONBytes = Encoding.ASCII.GetBytes(dataJSON);

            codeBuffer[0] = code;
            stream.Write(codeBuffer, 0, codeBufferSize);

            ushort dataSize = (ushort)dataJSONBytes.Length;
            dataSizeBuffer[0] = (byte)dataSize;
            dataSizeBuffer[1] = (byte)(dataSize >> 8);
            stream.Write(dataSizeBuffer, 0, dataSizeBufferSize);

            stream.Write(dataJSONBytes, 0, dataSize);
            stream.Flush();
        }

        public static (byte code, string dataJSON) Read(NetworkStream stream) {
            byte[] codeBuffer = new byte[dataSizeBufferSize];
            byte[] dataSizeBuffer = new byte[dataSizeBufferSize];
            byte[] dataBuffer = new byte[dataBufferSize];

            Decoder decoder = Encoding.ASCII.GetDecoder();

            ushort dataSize;
            byte code;
            string dataJSON = "";

            stream.Read(codeBuffer, 0, codeBufferSize);
            stream.Read(dataSizeBuffer, 0, dataSizeBufferSize);

            code = codeBuffer[0];
            dataSize = BitConverter.ToUInt16(dataSizeBuffer, 0);

            int dataBytesLeft = dataSize;

            while (dataBytesLeft > 0) {
                int dataBytesRead = stream.Read(dataBuffer, 0, dataBufferSize);
                char[] chars = new char[decoder.GetCharCount(dataBuffer, 0, dataBytesRead)];
                decoder.GetChars(dataBuffer, 0, dataBytesRead, chars, 0);
                dataJSON += new string(chars);
                dataBytesLeft -= dataBytesRead;
            }

            return (code, dataJSON);
        }

        public static async Task<(byte code, string dataJSON)> ReadAsync(NetworkStream stream)
        {
            byte[] codeBuffer = new byte[dataSizeBufferSize];
            byte[] dataSizeBuffer = new byte[dataSizeBufferSize];
            byte[] dataBuffer = new byte[dataBufferSize];

            Decoder decoder = Encoding.ASCII.GetDecoder();

            ushort dataSize;
            byte code;
            string dataJSON = "";


            await stream.ReadAsync(codeBuffer, 0, codeBufferSize);
            await stream.ReadAsync(dataSizeBuffer, 0, dataSizeBufferSize);

            code = codeBuffer[0];
            dataSize = BitConverter.ToUInt16(dataSizeBuffer, 0);

            int dataBytesLeft = dataSize;

            while (dataBytesLeft > 0)
            {
                int dataBytesRead = stream.Read(dataBuffer, 0, dataBufferSize);
                char[] chars = new char[decoder.GetCharCount(dataBuffer, 0, dataBytesRead)];
                decoder.GetChars(dataBuffer, 0, dataBytesRead, chars, 0);
                dataJSON += new string(chars);
                dataBytesLeft -= dataBytesRead;
            }

            return (code, dataJSON);
        }
    }
}
