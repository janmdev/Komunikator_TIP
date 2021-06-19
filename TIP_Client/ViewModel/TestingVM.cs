using System.Collections.Concurrent;
using System.Printing.IndexedProperties;
using System.Threading;
using NAudio.Wave;
using Shared;
using Shared.DataClasses.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TIP_Client.Helpers;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    

    public class TestingVM : ViewModelBase
    {
        private Dictionary<int,BufferedWaveProvider> bwp;

        private WaveInEvent waveIn;

        private Dictionary<int, WaveOut> waveOut;

        private UdpClient udpClient;

        private bool loggedIn;


        public TestingVM(MainVM mainVM)
        {
            loggedIn = true;
            waveOut = new Dictionary<int, WaveOut>();
            bwp = new Dictionary<int, BufferedWaveProvider>();
            udpClient = new UdpClient();
            udpClient.Client.Bind(Client.TCP.Client.LocalEndPoint);
            Rooms = new ObservableCollection<GetRoomsData.RoomData>();
            UsersInRoom = new ObservableCollection<GetUsersData.UserData>();
            OutputDeviceList = new ObservableCollection<WaveOutCapabilities>();
            InputDeviceList = new ObservableCollection<WaveInCapabilities>();
            //InitWaveIn();
            fillInDevices();
            fillOutDevices();
            InputDeviceSelected = InputDeviceList.First();
            OutputDeviceSelected = OutputDeviceList.First();
            Task.Factory.StartNew(async () =>
            {
                while (loggedIn)
                {
                    var rooms = Client.GetRooms();
                    fillRooms(rooms);
                    await Task.Delay(300);
                }
            });
            Task.Factory.StartNew(async () =>
            {
                while (loggedIn)
                {
                    if (inRoom)
                    {
                        var users = Client.GetUsers();
                        fillUsers(users);
                    }
                    await Task.Delay(300);
                }
            });
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (inRoom)
                    {
                        var data = await udpClient.ReceiveAsync();
                        playAudio(data.Buffer);
                    }
                }
            });
            LogoutCommand = new Command(args => LogoutAction());
            NewRoomCommand = new Command(args => NewRoomAction());
            EnterRoomCommand = new Command(args => EnterRoomAction());
            LeaveRoomCommand = new Command(args => LeaveRoomAction());
            DeleteRoomCommand = new Command(args => DeleteRoomAction());
            this.mainVM = mainVM;
        }

        private void InitWaveOut(WaveOutCapabilities device)
        {
            foreach(var wo in waveOut)
            {
                wo.Value.DeviceNumber = getDeviceOut(device);
                wo.Value.Init(bwp[wo.Key]);
            }
        }

        private void InitWaveIn()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(44100, 2);
            waveIn.DeviceNumber = getDeviceIn(InputDeviceSelected);
            waveIn.BufferMilliseconds = 50;
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendG722);
        }

        private void fillRooms((ServerCodes, string) codeData)
        {
            switch (codeData.Item1)
            {
                case ServerCodes.OK:
                    var roomData = JsonSerializer.Deserialize<GetRoomsData>(codeData.Item2).Rooms;
                    if (!roomData.Select(p => p.Name).SequenceEqual(rooms.Select(p => p.Name)))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Rooms.Clear();
                            roomData.ForEach(p => Rooms.Add(p));
                        });
                    }
                    else if(!roomData.Select(p => p.UsersInRoomCount).SequenceEqual(rooms.Select(p => p.UsersInRoomCount)))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            for (int i = 0; i < Rooms.Count; i++)
                            {
                                Rooms[i] = roomData.First(p => p.RoomID == Rooms[i].RoomID);
                            }
                        });
                    }
                    break;
                case ServerCodes.NO_ROOMS_ERROR:
                    App.Current.Dispatcher.Invoke(() => Rooms.Clear());
                    break;
                default:
                    MessageBox.Show(codeData.Item1.ToString());
                    break;
            }
        }

        private void fillUsers((ServerCodes, string) codeData)
        {
            switch (codeData.Item1)
            {
                case ServerCodes.OK:
                    var userData = JsonSerializer.Deserialize<GetUsersData>(codeData.Item2).Users;
                    if (!userData.Select(p => p.UserID).SequenceEqual(usersInRoom.Select(p => p.UserID)))
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            UsersInRoom.Clear();
                            userData.ForEach(p => UsersInRoom.Add(p));
                        });
                        try
                        {
                            for(int i = 0; i <= userData.Count; i++)
                            {
                                if(!bwp.ContainsKey(i))
                                    bwp.Add(i, new BufferedWaveProvider(new WaveFormat(44100, 2)));
                                if(!waveOut.ContainsKey(i))
                                    waveOut.Add(i, new WaveOut());
                            }
                            InitWaveOut(OutputDeviceSelected);
                            foreach (var wo in waveOut) wo.Value.Play();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                    }
                    break;
                case ServerCodes.USER_NOT_IN_ROOM_ERROR:
                    if(inRoom) MessageBox.Show(codeData.Item1.ToString());
                    break;
                default:
                    MessageBox.Show(codeData.Item1.ToString());
                    break;
            }
        }

        private void playAudio(byte[] data)
        {
            var decoded = Tools.ShortsToBytes(AudioHelper.DecodeG722(data.Skip(1).ToArray(), 48000));
            //bwp.ClearBuffer();
            var id = Convert.ToInt32(data[0]);
            if(bwp.ContainsKey(id)) 
                bwp[id].AddSamples(decoded, 0, decoded.Length);
        }

        private void SendG722(object sender, WaveInEventArgs e)
        {
            var encoded = AudioHelper.EncodeG722(e.Buffer, 48000);
            if(udpClient.Client != null) udpClient.Send(encoded, encoded.Length, new IPEndPoint(IPAddress.Parse(Client.connection.IPAddr), Client.connection.Port));
        }

        private void DeleteRoomAction()
        {
            if (currentRoomId == SelectedRoom.RoomID) LeaveRoomAction();
            var codeResp = Client.DeleteRoom(SelectedRoom.RoomID);
        }

        private void LeaveRoomAction()
        {
            var resp = Client.LeaveRoom();
            switch (resp.Item1)
            {
                case ServerCodes.OK:
                    InRoom = false;
                    currentRoomId = 0;
                    UsersInRoom.Clear();
                    break;
                default:
                    MessageBox.Show(resp.Item1.ToString());
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
                if (value)
                {
                    InitWaveIn();
                    waveIn.StartRecording();
                }
                else
                {
                    UsersInRoom.Clear();
                    foreach (var wo in waveOut) wo.Value.Stop();
                    waveIn.StopRecording();
                    waveIn.Dispose();
                }
                OnPropertyChanged(nameof(InRoom));
            }
        }

        private long currentRoomId;

        private void EnterRoomAction()
        {
            if (inRoom && selectedRoom.RoomID != currentRoomId)
            {
                LeaveRoomAction();
            }
            else if (inRoom && selectedRoom.RoomID == currentRoomId) return;
            if (InRoom) throw new Exception("Nie udało się opuścić pokoju");
            var resp = Client.EnterRoom(SelectedRoom.RoomID);
            switch (resp.Item1)
            {
                case ServerCodes.OK:
                    InRoom = true;
                    currentRoomId = SelectedRoom.RoomID;
                    break;
                default:
                    MessageBox.Show("TODO " + resp.Item1.ToString());
                    break;
            }
        }

        private void NewRoomAction()
        {
            var resp = Client.CreateRoom(NewRoomName, "", NewRoomLimit);
            switch (resp.Item1)
            {
                case ServerCodes.OK:
                    NewRoomName = "";
                    NewRoomLimit = 0;
                    break;
                default:
                    MessageBox.Show("TODO " + resp.Item1.ToString());
                    break;
            }
        }

        private void LogoutAction()
        {
            if (inRoom) LeaveRoomAction();
            Task.Run(() => Client.Logout()).ContinueWith(t =>
            {

                switch (t.Result.Item1)
                {
                    case 0:
                        loggedIn = false;
                        udpClient.Close();
                        mainVM.NavigateTo("Login");
                        break;
                    case ServerCodes.USER_NOT_LOGGED_ERROR:
                        MessageBox.Show("Użytkownik nie jest zalogowany");
                        break;
                    default:
                        MessageBox.Show("TODO " + t.Result.Item1.ToString());
                        break;
                }

            });
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

        public ObservableCollection<GetRoomsData.RoomData> Rooms
        {
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

        public ICommand LogoutCommand { get; set; }

        public ICommand NewRoomCommand { get; set; }

        public ICommand EnterRoomCommand { get; set; }

        public ICommand LeaveRoomCommand { get; set; }

        public ICommand DeleteRoomCommand { get; set; }

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
                OnPropertyChanged(nameof(NewRoomLimit));
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
                if (waveOut != null) InitWaveOut(value);

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
                if (inRoom)
                {
                    waveIn.StopRecording();
                    waveIn.DeviceNumber = getDeviceIn(value);
                    waveIn.StartRecording();
                }
                else
                {
                    if(waveIn != null) waveIn.DeviceNumber = getDeviceIn(value);
                }


                OnPropertyChanged(nameof(InputDeviceSelected));
            }
        }

        private MainVM mainVM;

        

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

        private void fillOutDevices()
        {
            int waveOutDevices = WaveOut.DeviceCount;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                OutputDeviceList.Add(WaveOut.GetCapabilities(waveOutDevice));
            }
        }

        private void fillInDevices()
        {
            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                InputDeviceList.Add(WaveIn.GetCapabilities(waveInDevice));
            }
        }
    }
}
