using System.Windows.Input;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    internal class ConnectVM : ViewModelBase
    {
        private MainVM mainVM;
        
        public ConnectVM(MainVM mainVM)
        {
            this.mainVM = mainVM;
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
            mainVM.NavigateTo("Login");
        }
    }
}