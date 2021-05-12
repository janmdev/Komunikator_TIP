using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Shared;
using Shared.DataClasses.Server;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class TestingVM : ViewModelBase
    {
        private BufferedWaveProvider bwp;
        private WaveInEvent waveIn;
        private WaveOut waveOut;
        public TestingVM(MainVM mainVM)
        {
            Rooms = new ObservableCollection<GetRoomsData.RoomData>();
            UsersInRoom = new ObservableCollection<GetUsersData.UserData>();
            this.mainVM = mainVM;
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
            LogoutCommand = new Command(args => LogoutAction());
            
            Task.Run(async () => await Client.GetRooms()).ContinueWith(t =>
            {
                switch (t.Result.Item1)
                {
                    case ServerCodes.OK:
                        Rooms = new ObservableCollection<GetRoomsData.RoomData>(t.Result.Item2.Rooms);
                        break;
                    case ServerCodes.GET_ROOMS_ERROR:
                        MessageBox.Show("Nie można pobrać pokojów");
                        break;
                    case ServerCodes.NO_ROOMS_ERROR:
                        MessageBox.Show("Brak pokojów");
                        break;
                    default:
                        MessageBox.Show("Nie można pobrać pokojów");
                        break;
                }
            });
            NewRoomCommand = new Command(args => NewRoomAction());
            EnterRoomCommand = new Command(args => EnterRoomAction());
            LeaveRoomCommand = new Command(async args => await LeaveRoomAction());
        }

        private async Task LeaveRoomAction()
        {
            var code_resp = await Client.LeaveRoom();
            switch (code_resp)
            {
                case ServerCodes.OK:
                    InRoom = false;
                    currentRoomId = 0;
                    UsersInRoom = new ObservableCollection<GetUsersData.UserData>();
                    break;
                default:
                    MessageBox.Show("TODO " + code_resp.ToString());
                    break;
            }
        }

        private bool inRoom;

        public bool InRoom
        {
            get
            {
                return inRoom;
            }
            set
            {
                inRoom = value;
                OnPropertyChanged(nameof(InRoom));
            }
        }

        private long currentRoomId;
        private async void EnterRoomAction()
        {
            if (InRoom && SelectedRoom.RoomID != currentRoomId)
            {
                await LeaveRoomAction();
            }

            if (InRoom) throw new Exception("Nie udało się opuścić pokoju");
            var code_resp = await Client.EnterRoom(SelectedRoom.RoomID);
            switch (code_resp)
            {
                case ServerCodes.OK:
                    FillUsers();
                    InRoom = true;
                    currentRoomId = SelectedRoom.RoomID;
                    break;
                default:
                    MessageBox.Show("TODO " + code_resp.ToString());
                    break;
            }
        }

        private async void FillUsers()
        {
            var users = await Client.GetUsers();
            switch (users.Item1)
            {
                case ServerCodes.OK:
                    UsersInRoom = new ObservableCollection<GetUsersData.UserData>(users.Item2.Users);
                    break;
                default:
                    MessageBox.Show("TODO " + users.Item1.ToString());
                    break;
            }
        }

        private async void NewRoomAction()
        {
            var code_resp = await Client.CreateRoom(NewRoomName, "", NewRoomLimit);
            switch (code_resp)
            {
                case ServerCodes.OK:
                    NewRoomName = "";
                    NewRoomLimit = 0;
                    break;
                default:
                    MessageBox.Show("TODO " + code_resp.ToString());
                    break;
            }
        }

        private GetRoomsData.RoomData selectedRoom;

        public GetRoomsData.RoomData SelectedRoom
        {
            get
            {
                return selectedRoom;
            }
            set
            {
                selectedRoom = value;
                OnPropertyChanged(nameof(SelectedRoom));
            }
        }

        private ObservableCollection<GetUsersData.UserData> usersInRoom;

        public ObservableCollection<GetUsersData.UserData> UsersInRoom
        {
            get
            {
                return usersInRoom;
            }
            set
            {
                usersInRoom = value;
                OnPropertyChanged(nameof(UsersInRoom));
            }
        }

        private ObservableCollection<GetRoomsData.RoomData> rooms;
        public ObservableCollection<GetRoomsData.RoomData> Rooms {
            get
            {
                return rooms;
            }
            set
            {
                rooms = value;
                OnPropertyChanged(nameof(Rooms));
            }

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
        public ICommand LogoutCommand { get; set; }
        public ICommand NewRoomCommand { get; set; }
        public ICommand EnterRoomCommand { get; set; }
        public ICommand LeaveRoomCommand { get; set; }

        private string newRoomName;
        public string NewRoomName
        {
            get
            {
                return newRoomName;
            }
            set
            {
                newRoomName = value;
                OnPropertyChanged(nameof(NewRoomName));
            }
        }

        private int newRoomLimit;

        public int NewRoomLimit
        {
            get
            {
                return newRoomLimit;
            }
            set
            {
                newRoomLimit = value;
                OnPropertyChanged(nameof(NewRoomName));
            }
        }

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
        private MainVM mainVM;

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

        private void LogoutAction()
        {
            Task.Run(async () => await Client.Logout()).ContinueWith(t =>
            {
                
                switch (t.Result)
                {
                    case 0:
                        mainVM.NavigateTo("Login");
                        break;
                    case ServerCodes.USER_NOT_LOGGED_ERROR:
                        MessageBox.Show("Użytkownik nie jest zalogowany");
                        break;
                    default:
                        MessageBox.Show("Nierozpoznany błąd");
                        break;
                }
                
            });
        }

        private void SendCaptureSamples(object sender, WaveInEventArgs e)
        {
            byte[] bytes = new byte[1024];
            bwp.ClearBuffer();
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
