using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using TIP_Client.ViewModel;
using Path = System.Windows.Shapes.Path;

namespace TIP_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var mainViewModel = new MainVM();
            if (ConfigExists()) mainViewModel.NavigateTo("Login");
            else mainViewModel.NavigateTo("Connect");
            this.DataContext = mainViewModel;
        }
        private bool ConfigExists()
        {
            bool flag = false;

            if (!File.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom", "config.json")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom"));
                File.Create(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom", "config.json"));
            }
            else flag = true;

            return flag;
        }
    }
}
