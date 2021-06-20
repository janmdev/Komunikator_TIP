using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Codecs;
using NAudio.Wave;

namespace TIP_Client.Helpers
{
    public static class AudioHelper
    {
        public static byte[] EncodeG722(byte[] data, int sampleRate)
        {
            var codec = new G722Codec();
            var state = new G722CodecState(sampleRate, G722Flags.SampleRate8000);

            var wb = new WaveBuffer(data);
            var encodedLength = data.Length / 2;
            var outputBuffer = new byte[encodedLength];
            var length = codec.Encode(state, outputBuffer, wb.ShortBuffer, data.Length / 2);

            if (length != outputBuffer.Length)
            {
                var outputBuffer2 = new byte[length];
                Buffer.BlockCopy(outputBuffer, 0, outputBuffer2, 0, length);
                outputBuffer = outputBuffer2;
            }
            return outputBuffer;
        }

        public static short[] DecodeG722(byte[] data, int sampleRate)
        {
            var codec = new G722Codec();
            var state = new G722CodecState(sampleRate, G722Flags.SampleRate8000);

            var wb = new WaveBuffer(data);
            var outputBuffer = new short[data.Length];
            var length = codec.Decode(state, outputBuffer, data, data.Length);

            if (length != outputBuffer.Length)
            {
                var outputBuffer2 = new short[length];
                Buffer.BlockCopy(outputBuffer, 0, outputBuffer2, 0, length);
                outputBuffer = outputBuffer2;
            }

            return outputBuffer;
        }
    }
}
