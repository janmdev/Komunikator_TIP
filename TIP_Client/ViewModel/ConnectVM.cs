using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using TIP_Client.View;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    internal class ConnectVM : ViewModelBase
    {
        private MainVM mainVM;
        
        public ConnectVM(MainVM mainVM)
        {
            this.mainVM = mainVM;
            var cm = JsonSerializer.Deserialize<ConnectionModel>(File.ReadAllText(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom",
                "config.json")));
            IPAddr = cm.IPAddr;
            Port = cm.Port;
            ConnectCommand = new Command((args) => Connect());
        }

        private string ipAddr;
        public string IPAddr
        {
            get
            {
                return ipAddr;
            }
            set
            {
                ipAddr = value;
                OnPropertyChanged(nameof(IPAddr));
            }
        }

        private int port;
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }
        public ICommand ConnectCommand { get; set; }

        public void Connect()
        {
            mainVM.LoadingCv = Visibility.Visible;
            ConnectionModel cm = new ConnectionModel
            {
                IPAddr = IPAddr,
                Port = Port
            };
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom",
                "config.json");
            File.WriteAllText(filePath, JsonSerializer.Serialize(cm));
            bool connected = false;
            Task.Run(async () =>
            {
               return await Client.Connect(IPAddr, Port);
            }).ContinueWith( async (t) =>
            {
                App.Current.Dispatcher.Invoke(() => mainVM.LoadingCv = Visibility.Hidden);
                if(t.Result) mainVM.NavigateTo("Login");
                else
                {
                    App.Current.Dispatcher.Invoke(async () =>
                    {
                        DialogContent = "Nie udało się połączyć z serwerem";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                    });

                }
            });
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