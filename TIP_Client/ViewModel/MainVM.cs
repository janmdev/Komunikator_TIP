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
        private BufferedWaveProvider bwp;
        private WaveInEvent waveIn;
        private WaveOut waveOut;
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
            waveIn.DeviceNumber = getDeviceIn(InputDeviceSelected);
            waveIn.BufferMilliseconds = 100;
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendCaptureSamples);
            waveOut = new WaveOut();
            waveOut.DeviceNumber = getDeviceOut(OutputDeviceSelected);
            bwp = new BufferedWaveProvider(waveIn.WaveFormat);
            waveIn.StartRecording();
        }

        private void SendCaptureSamples(object sender, WaveInEventArgs e)
        {
            byte[] bytes = new byte[1024];
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
            //TAK NIE MOZE BYĆ JAK COŚ BO PAMIĘĆ ŻRE
            waveOut.Init(bwp);
            waveOut.Play();

        }
        private int getDeviceIn(WaveInCapabilities wIn)
        {
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                if (wIn.ProductName == WaveIn.GetCapabilities(waveInDevice).ProductName) return waveInDevice;
            }
            return 0;
        }
        private int getDeviceOut(WaveOutCapabilities wOut)
        {
            int waveOutDevices = WaveOut.DeviceCount;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                if (wOut.ProductName == WaveOut.GetCapabilities(waveOutDevice).ProductName) return waveOutDevice;
            }
            return 0;
        }
    }
}
