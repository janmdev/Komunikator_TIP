using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using Shared;
using Shared.DataClasses.Server;
using TIP_Client.View;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{

    internal class LoginVM : ViewModelBase
    {
        private MainVM mainVM;

        public LoginVM(MainVM mainVM)
        {
            this.mainVM = mainVM;
            LoginCommand = new Command((args) => LoginF(args));
            RegisterCommand = new Command((args) => mainVM.NavigateTo("RegisterAction"));
        }


        private string login;

        public string Login
        {
            get
            {
                return login;
            }
            set
            {
                login = value;
                OnPropertyChanged(nameof(Login));
            }
        }
        public ICommand LoginCommand { get; set; }
        private async void LoginF(object args)
        {
            if (args is PasswordBox pb)
            {
                var t = Client.Login(Login, pb.Password);
                switch (t.Item1)
                {
                    case ServerCodes.OK:
                        Client.ClientID = (JsonSerializer.Deserialize<GetUserLogin>(t.Item2)).ClientID;
                        mainVM.NavigateTo("Testing");
                        break;
                    case ServerCodes.USER_ALREADY_LOGGED_ERROR:
                        DialogContent = "Użytkownik już jest zalogowany";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                        break;
                    case ServerCodes.USER_LOGGED_ERROR:
                        DialogContent = "Użytkownik już jest zalogowany";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                        break;
                    case ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR:
                        DialogContent = "Błędne dane logowania";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                        break;
                    default:
                        DialogContent = "Nierozpoznany błąd";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                        break;
                }
                
            }
        }
        public ICommand RegisterCommand { get; set; }


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
