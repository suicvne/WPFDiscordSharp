using DiscordSharp;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DiscordClient MainClient;
        Task discordTask;

        public MainWindow()
        {
            InitializeComponent();
            MainClient = new DiscordClient();

            SetupEvents();

            Title = "Connecting..";
            Mouse.OverrideCursor = Cursors.Wait;

            if(MainClient.SendLoginRequest() != null)
                discordTask = Task.Run(() => MainClient.Connect());
        }

        private void SetupEvents()
        {
            MainClient.Connected += (sender, e) =>
            {
                Dispatcher.Invoke(()=>this.Title = "Discord - " + e.user.Username);
                PopulateLists();

                Dispatcher.Invoke(()=> Mouse.OverrideCursor = null);
            };
            MainClient.TextClientDebugMessageReceived += (sender, e) =>
            {
            };
        }

        private void PopulateLists()
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var server in MainClient.GetServersList())
                {
                    ServerStub stub = new ServerStub(server);
                    //serversListView.Items.Add(server.name);
                    serversListView.Items.Add(stub);       
                }
            });
        }

        private void serversListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(serversListView.SelectedIndex > -1)
            {
                ServerStub stub = serversListView.SelectedItem as ServerStub;
                ServerInfo info = new ServerInfo(stub.Server);
                info.ShowDialog();
            }
        }
    }
}
