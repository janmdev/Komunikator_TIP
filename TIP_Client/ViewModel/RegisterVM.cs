using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Shared;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class RegisterVM : ViewModelBase
    {
        private MainVM mainVM;

        public RegisterVM(MainVM mainVm)
        {
            this.mainVM = mainVm;
            RegisterCommand = new Command(args => RegisterAction(args));
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

        public void RegisterAction(object args)
        {
            //Task.Run(async () => await Client.RegisterAction(Login, "123"));
            if (args is object[] argsArr)
            {
                if (argsArr[0] is PasswordBox pb0 && argsArr[1] is PasswordBox pb1)
                {
                    if (pb0.Password != pb1.Password)
                    {
                        MessageBox.Show("Hasła nie są takie same");
                        return;
                    }


#if DEBUG

#else
                if (!Regex.Match(pb1.Password,
                    "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success)
                {
                    MessageBox.Show("Hasło nie spełnia warunków");
                    return;
                }
#endif
                    Task.Run(async () => await Client.Register(Login, pb0.Password)).ContinueWith(t =>
                    {
                        switch (t.Result)
                        {
                            case ServerCodes.OK:
                                MessageBox.Show("Konto zostało utworzone");
                                clear(pb0,pb1);
                                break;
                            case ServerCodes.USER_ALREADY_EXIST_ERROR:
                                MessageBox.Show("Użytkownik z takim loginem już istnieje");
                                break;
                            default:
                                MessageBox.Show("Nierozpoznany błąd");
                                break;
                        }
                    });
                }
            }
            

        }
        public ICommand BackCommand { get; set; }

        private void clear(PasswordBox pb, PasswordBox pbConfirm)
        {
            Login = "";
            Application.Current.Dispatcher.Invoke(() =>
            {
                pb.Password = "";
                pbConfirm.Password = "";
            });
            
        }
        public void GoBack()
        {
            mainVM.NavigateTo("Login");
        }
    }
}