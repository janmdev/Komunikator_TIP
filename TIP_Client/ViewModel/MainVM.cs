using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class MainVM : ViewModelBase
    {
        private WaveInEvent waveIn;
        public MainVM()
        {
            OutputDeviceList = new ObservableCollection<WaveOutCapabilities>();
            int waveOutDevices = WaveOut.DeviceCount;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                OutputDeviceList.Add(WaveOut.GetCapabilities(waveOutDevice));
            }

            InputDeviceList = new ObservableCollection<WaveInCapabilities>();
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                InputDeviceList.Add(WaveIn.GetCapabilities(waveInDevice));
            }



            RecordCommand = new Command((args) =>
            {
                init();
            });
        }
        private ViewModelBase selectedVM;
        public ViewModelBase SelectedVM
        {
            get
            {
                return selectedVM;
            }
            set
            {
                selectedVM = value;
                OnPropertyChanged(nameof(SelectedVM));
            }
        }
        public ICommand RecordCommand { get; set; }

        private ObservableCollection<WaveOutCapabilities> outputDeviceList;
        public ObservableCollection<WaveOutCapabilities> OutputDeviceList
        {
            get
            {
                return outputDeviceList;
            }
            set
            {
                outputDeviceList = value;
                OnPropertyChanged(nameof(OutputDeviceList));
            }
        }
        private WaveOutCapabilities outputDeviceSelected;
        public WaveOutCapabilities OutputDeviceSelected
        {
            get
            {
                return outputDeviceSelected;
            }
            set
            {
                outputDeviceSelected = value;
                OnPropertyChanged(nameof(OutputDeviceSelected));
            }
        }

        private ObservableCollection<WaveInCapabilities> inputDeviceList;
        public ObservableCollection<WaveInCapabilities> InputDeviceList
        {
            get
            {
                return inputDeviceList;
            }
            set
            {
                inputDeviceList = value;
                OnPropertyChanged(nameof(InputDeviceList));
            }
        }
        private WaveInCapabilities inputDeviceSelected;
        public WaveInCapabilities InputDeviceSelected
        {
            get
            {
                return inputDeviceSelected;
            }
            set
            {
                inputDeviceSelected = value;
                OnPropertyChanged(nameof(InputDeviceSelected));
            }
        }
        private bool recordChecked;
        public bool RecordChecked
        {
            get
            {
                return recordChecked;
            }
            set
            {
                recordChecked = value;
                OnPropertyChanged(nameof(RecordChecked));
            }
        }


        private void init()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(44100, 2);
            waveIn.DeviceNumber = getDevice(InputDeviceSelected);
            waveIn.BufferMilliseconds = 1000;
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendCaptureSamples);
            waveIn.StartRecording();
        }

        static void SendCaptureSamples(object sender, WaveInEventArgs e)
        {
            Console.WriteLine("Bytes recorded: {0}", e.BytesRecorded);
            byte[] bytes = new byte[1024];

        }
        private int getDevice(WaveInCapabilities wIn)
        {
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                if (wIn.ProductName == WaveIn.GetCapabilities(waveInDevice).ProductName) return waveInDevice;
            }
            return 0;
        }
    }
}
