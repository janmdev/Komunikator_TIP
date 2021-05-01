﻿using TIP_Client.ViewModel.MVVM;

namespace TIP_Client.ViewModel
{
    public class MainVM : ViewModelBase
    {

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
                        SelectedVM = new TestingVM(this);
                        break;
                    case "Register":
                        SelectedVM = new RegisterVM(this);
                        break;
                }
            }
        }
    }
}