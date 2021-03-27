using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Komunikator_TIP
{
    class Program
    {
        static WaveInEvent s_WaveIn;
        static WaveOut wo;
        static void Main(string[] args)
        {
            getDevices();
            getDevicesOut();
            using (var ms = File.OpenRead("test.mp3"))
            using (var rdr = new Mp3FileReader(ms))
            using (var wavStream = WaveFormatConversionStream.CreatePcmStream(rdr))
            using (var baStream = new BlockAlignReductionStream(wavStream))
            using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
            {
                waveOut.Init(baStream);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
            //init();
            //while (true) /* Yeah, this is bad, but just for testing.... */
            //    System.Threading.Thread.Sleep(3000);
        }

        public static void init()
        {
            s_WaveIn = new WaveInEvent();
            s_WaveIn.WaveFormat = new WaveFormat(44100, 2);
            s_WaveIn.DeviceNumber = 0;
            s_WaveIn.BufferMilliseconds = 1000;
            s_WaveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendCaptureSamples);
            s_WaveIn.StartRecording();
        }

        static void SendCaptureSamples(object sender, WaveInEventArgs e)
        {
            Console.WriteLine("Bytes recorded: {0}", e.BytesRecorded);
            byte[] bytes = new byte[1024];

            IWaveProvider provider = new RawSourceWaveStream(
                                     new MemoryStream(bytes), new WaveFormat());

            wo.Init(provider);
            wo.Play();

        }

        public static void getDevices()
        {
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
        }
        public static void getDevicesOut()
        {
            int waveInDevices = WaveOut.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
        }


    }
}
