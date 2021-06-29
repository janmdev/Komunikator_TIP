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
        private static G722Codec codec = new G722Codec();
        private static G722CodecState encoderState = new G722CodecState(64000, G722Flags.None);
        private static G722CodecState decoderState = new G722CodecState(64000, G722Flags.None);
        public static byte[] EncodeG722(byte[] data)
        {

            var length = data.Length;
            var wb = new WaveBuffer(data);
            int encodedLength = length / 4;
            var outputBuffer = new byte[encodedLength];
            int encoded = codec.Encode(encoderState, outputBuffer, wb.ShortBuffer, length / 2);
            return outputBuffer;
        }

        public static byte[] DecodeG722(byte[] data)
        {
            var length = data.Length;
            int decodedLength = length * 4;
            var outputBuffer = new byte[decodedLength];
            var wb = new WaveBuffer(outputBuffer);
            int decoded = codec.Decode(decoderState, wb.ShortBuffer, data, length);
            return outputBuffer;
        }




    }
}