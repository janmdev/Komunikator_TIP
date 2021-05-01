using System;
using System.Windows.Input;

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
            if (LoginValidated())
            {
                mainVM.NavigateTo("Testing");
            }
        }

        private bool LoginValidated()
        {
            //NOT IMPLEMENTED
            return true;
        }

        public ICommand RegisterCommand { get; set; }

        private void RegisterNav()
        {

            mainVM.NavigateTo("RegisterAction");
        }
    }
}
