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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using TIP_Client.Helpers;
using TIP_Client.View;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    

    public class AudioVM : ViewModelBase
    {
        private Dictionary<int,BufferedWaveProvider> bwp;

        private WaveInEvent waveIn;

        private Dictionary<int, WaveOut> waveOut;

        private UdpClient udpClient;

        private bool loggedIn;

        private MainVM mainVM;

        public AudioVM(MainVM mainVM)
        {
            
            loggedIn = true;
            waveOut = new Dictionary<int, WaveOut>();
            Volume = 100;
            bwp = new Dictionary<int, BufferedWaveProvider>();
            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 41234));
            Rooms = new ObservableCollection<GetRoomsData.RoomData>();
            UsersInRoom = new ObservableCollection<GetUsersData.UserData>();
            OutputDeviceList = new ObservableCollection<WaveOutCapabilities>();
            InputDeviceList = new ObservableCollection<WaveInCapabilities>();
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
                bwp[wo.Key].ClearBuffer();
                wo.Value.Volume = (float)volume/100;
                wo.Value.DeviceNumber = getDeviceOut(device);
                wo.Value.Init(bwp[wo.Key]);
            }
        }

        private void InitWaveIn()
        {
            waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(48000, 2);
            waveIn.DeviceNumber = getDeviceIn(InputDeviceSelected);
            waveIn.BufferMilliseconds = 50;
            waveIn.DataAvailable += new EventHandler<WaveInEventArgs>(SendG722);
        }

        private async void fillRooms((ServerCodes, string) codeData)
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
                    DialogContent = codeData.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    break;
            }
        }

        private async void fillUsers((ServerCodes, string) codeData)
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
                            for(int i = 0; i < userData.Count; i++)
                            {
                                //if (userData[i].UserID == Client.ClientID) continue;
                                if(!bwp.ContainsKey(i))
                                    bwp.Add(i, new BufferedWaveProvider(new WaveFormat(48000, 2)));
                                if(!waveOut.ContainsKey(i))
                                    waveOut.Add(i, new WaveOut());
                            }
                            InitWaveOut(OutputDeviceSelected);
                            foreach (var wo in waveOut) wo.Value.Play();
                        }
                        catch (Exception ex)
                        {
                            DialogContent = ex.Message;
                            await DialogHost.Show(new OkDialog(), "OkDialog");
                        }

                    }
                    break;
                case ServerCodes.USER_NOT_IN_ROOM_ERROR:
                    if(inRoom)
                    {
                        DialogContent = codeData.Item1.ToString();
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                    }
                    break;
                default:
                    DialogContent = codeData.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    break;
            }
        }

        private void playAudio(byte[] data)
        {
            var decoded = Tools.ShortsToBytes(AudioHelper.DecodeG722(data.Skip(1).ToArray(), 48000));
            var id = Convert.ToInt32(data[0]);
            if(bwp.ContainsKey(id)) 
                bwp[id].AddSamples(decoded, 0, decoded.Length);
        }

        private void SendG722(object sender, WaveInEventArgs e)
        {
            var encoded = AudioHelper.EncodeG722(e.Buffer, 48000);
            if (udpClient.Client != null) udpClient.Send(encoded, encoded.Length, new IPEndPoint(IPAddress.Parse(Client.connection.IPAddr), Client.connection.Port));
        }

        private List<T[]> Split<T>(T[] source)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 512)
                .Select(x => x.Select(v => v.Value).ToArray())
                .ToList();
        }

        private async void DeleteRoomAction()
        {
            if (SelectedRoom.RoomCreatorUserID != Client.ClientID) return;
            DialogContent = $"Czy na pewno chcesz usunąć pokój {SelectedRoom.Name}?";
            string result = (string)await DialogHost.Show(new OkCancelDialog(), "DeleteRoomDialog");
            if (result == "Accept")
            {
                if(currentRoomId == SelectedRoom.RoomID) LeaveRoomAction();
                var codeResp = Client.DeleteRoom(SelectedRoom.RoomID);
            }
        }

        private async void LeaveRoomAction()
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
                    DialogContent = resp.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
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

        private async void EnterRoomAction()
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
                    DialogContent = resp.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    break;
            }
        }

        private async void NewRoomAction()
        {
            var resp = Client.CreateRoom(NewRoomName, "", NewRoomLimit);
            switch (resp.Item1)
            {
                case ServerCodes.OK:
                    NewRoomName = "";
                    NewRoomLimit = 0;
                    break;
                default:
                    DialogContent = resp.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    break;
            }
        }

        private async void LogoutAction()
        {
            if (inRoom) LeaveRoomAction();
            var t = Client.Logout();
            switch (t.Item1)
            {
                case 0:
                    loggedIn = false;
                    udpClient.Close();
                    Client.ClientID = null;
                    mainVM.NavigateTo("Login");
                    break;
                case ServerCodes.USER_NOT_LOGGED_ERROR:
                    DialogContent = "Użytkownik nie jest zalogowany";
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    break;
                default:
                    DialogContent = t.Item1.ToString();
                    await DialogHost.Show(new OkDialog(), "OkDialog");
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
                if(inRoom) foreach(var wo in waveOut) wo.Value.Stop();
                if (waveOut != null) InitWaveOut(value);
                if (inRoom) foreach (var wo in waveOut) wo.Value.Play();
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

        private int volume;

        public int Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
                if(inRoom) foreach (var wo in waveOut) wo.Value.Stop();
                InitWaveOut(OutputDeviceSelected);
                if (inRoom)
                {
                    foreach (var wo in waveOut)
                    {
                        wo.Value.Play();
                    }
                }
                OnPropertyChanged(nameof(Volume));
            }
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
        private string dialogContent;
        public string DialogContent
        {
            get
            {
                return dialogContent;
            }
            set
            {
                dialogContent = value;
                OnPropertyChanged(nameof(DialogContent));
            }
        }
    }
}
