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
            throw new System.NotImplementedException();
        }

        public ICommand RegisterCommand { get; set; }

        private void RegisterNav()
        {
            throw new NotImplementedException();
        }
    }
}
