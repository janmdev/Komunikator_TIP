using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
            if (ConfigExists() && ConfigValid())
            {
                ConnectionModel cm = JsonSerializer.Deserialize<ConnectionModel>(File.ReadAllText(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom",
                    "config.json")));
                Task.Run(async () =>
                {
                    mainViewModel.NavigateTo("Connect");
                    return await Client.Connect(cm.IPAddr, cm.Port);
                }
                ).ContinueWith(t =>
                {
                    mainViewModel.NavigateTo(t.Result ? "Login" : "Connect");
                });
            }
            else mainViewModel.NavigateTo("Connect");
            this.DataContext = mainViewModel;
        }
        private bool ConfigExists()
        {
            bool flag = false;

            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom", "config.json")))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom"));
                File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom", "config.json"));
            }
            else flag = true;

            return flag;
        }

        private bool ConfigValid()
        {
            bool flag = false;
            try
            {
                ConnectionModel cm = JsonSerializer.Deserialize<ConnectionModel>(File.ReadAllText(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tip kom",
                    "config.json")));
                if (cm.Port != 0 && Regex.Match(cm.IPAddr,@"^((25[0-5]|(2[0-4]|1[0-9]|[1-9]|)[0-9])(\.(?!$)|$)){4}$").Success)
                {
                    flag = true;
                }
            }
            catch (Exception)
            { }

            return flag;
        }


    }
}
