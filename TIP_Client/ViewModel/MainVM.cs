using System.Windows;
using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class MainVM : ViewModelBase
    {
        public MainVM()
        {
            LoadingCv = Visibility.Hidden;
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

        public void NavigateTo(object arg)
        {
            if (arg is string dest)
            {
                switch (dest)
                {
                    case "Connect":
                        SelectedVM = new ConnectVM(this);
                        break;
                    case "Login":
                        SelectedVM = new LoginVM(this);
                        break;
                    case "Testing":
                        SelectedVM = new AudioVM(this);
                        break;
                    case "RegisterAction":
                        SelectedVM = new RegisterVM(this);
                        break;
                }
            }
        }

        private Visibility loadingCv;
        public Visibility LoadingCv
        {
            get
            {
                return loadingCv;
            }
            set
            {
                loadingCv = value;
                OnPropertyChanged(nameof(LoadingCv));
            }
        }
    }
}