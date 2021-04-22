using System.Windows.Input;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class RegisterVM : ViewModelBase
    {
        private MainVM mainVM;

        public RegisterVM(MainVM mainVm)
        {
            this.mainVM = mainVm;
            RegisterCommand = new Command((args) => Register(args));
            BackCommand = new Command((args) => GoBack());
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
        public ICommand RegisterCommand { get; set; }

        public void Register(object args)
        {

        }
        public ICommand BackCommand { get; set; }

        public void GoBack()
        {
            mainVM.NavigateTo("Login");
        }
    }
}