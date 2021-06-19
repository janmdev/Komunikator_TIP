using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Shared;
using TIP_Client.View;
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

        public async void RegisterAction(object args)
        {
            //Task.Run(async () => await Client.RegisterAction(Login, "123"));
            if (args is object[] argsArr)
            {
                if (argsArr[0] is PasswordBox pb0 && argsArr[1] is PasswordBox pb1)
                {
                    if (pb0.Password != pb1.Password)
                    {
                        DialogContent = "Hasła nie są takie same";
                        await DialogHost.Show(new OkDialog(), "OkDialog");
                        return;
                    }


#if DEBUG

#else
                if (!Regex.Match(pb1.Password,
                    "(?=.*[!\"#$%&'()*+,\\-\\./:<>=?@\\[\\]\\^_{}|~])(?=.*[A-Z])(?!.*\\$).{8,255}").Success)
                {
                    
                    DialogContent = "Hasło nie spełnia warunków";
                    await DialogHost.Show(new OkDialog(), "OkDialog");
                    return;
                }
#endif
                    var t = Client.Register(Login, pb0.Password);
                    
                    switch (t.Item1)
                    {
                        case ServerCodes.OK:
                            DialogContent = "Konto zostało utworzone";
                            await DialogHost.Show(new OkDialog(), "OkDialog");
                            clear(pb0,pb1);
                            break;
                        case ServerCodes.USER_ALREADY_EXIST_ERROR:
                            DialogContent = "Użytkownik z takim loginem już istnieje";
                            await DialogHost.Show(new OkDialog(), "OkDialog");
                            break;
                        default:
                            DialogContent = "Nierozpoznany błąd";
                            await DialogHost.Show(new OkDialog(), "OkDialog");
                            break;
                    }
                    
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