using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shared;

namespace TIP_Client.ViewModel
{
    using TIP_Client.ViewModel.MVVM;

    internal class LoginVM : ViewModelBase
    {
        private MainVM mainVM;

        public LoginVM(MainVM mainVM)
        {
            this.mainVM = mainVM;
            LoginCommand = new Command((args) => LoginF(args));
            RegisterCommand = new Command((args) => RegisterNav());
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
        private void LoginF(object args)
        {
            if (args is PasswordBox pb)
            {
                Task.Run(async () => await Client.Login(Login, pb.Password)).ContinueWith((t) =>
                {
                    switch (t.Result)
                    {
                        case ServerCodes.OK:
                            mainVM.NavigateTo("Testing");
                            break;
                        case ServerCodes.USER_ALREADY_LOGGED_ERROR:
                            MessageBox.Show("Użytkownik już jest zalogowany");
                            break;
                        case ServerCodes.USER_LOGGED_ERROR:
                            MessageBox.Show("Użytkownik już jest zalogowany");
                            break;
                        case ServerCodes.WRONG_USERNAME_OR_PASSWORD_ERROR:
                            MessageBox.Show("Błędne dane logowani");
                            break;
                        default:
                            MessageBox.Show("Nierozpoznany błąd");
                            break;
                    }
                });
            }
        }


        public ICommand RegisterCommand { get; set; }

        private void RegisterNav()
        {

            mainVM.NavigateTo("RegisterAction");
        }
    }
}
