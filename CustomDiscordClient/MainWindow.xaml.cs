using DiscordSharp;
using DiscordSharp.Objects;
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
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Notification stuffs
        ToastManager toastManager = new ToastManager("DiscordWPF");
        #endregion


        private List<ServerView> openServerViews;
        DiscordClient MainClient;
        Task discordTask;

        Uri MagicalDiscordIcon = new Uri("https://pbs.twimg.com/media/CSA9MacUcAAdY8h.png");

        public MainWindow()
        {
            InitializeComponent();
            Icon = new BitmapImage(MagicalDiscordIcon);
            //channelsList.Visibility = Visibility.Hidden;

            MainClient = new DiscordClient();

            SetupEvents();

            Title = "Connecting..";
            Mouse.OverrideCursor = Cursors.Wait;

            if(MainClient.SendLoginRequest() != null)
                discordTask = Task.Run(() => MainClient.Connect());

            openServerViews = new List<ServerView>();
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
                //if(e.message.Level == MessageLevel.Critical)
                //{
                //    MessageBox.Show("Critical DiscordSharp Error Ocurred: " + e.message.Message);
                //}
            };
            MainClient.MentionReceived += (sender, e) =>
            {
                string toReplace = $"<@{MainClient.Me.ID}>";
                string message = e.message.content;
                message = message.Replace(toReplace, $"@{MainClient.Me.Username}");

                toastManager.CreateToast($"Mention received from {e.author.Username}\n{message}");
            };
            MainClient.SocketClosed += (sender, e) =>
            {
                Dispatcher.Invoke(() => Title = "Connection lost, retrying..");
                discordTask.Dispose();
                Task.Delay(3000).Wait();
                if (MainClient.SendLoginRequest() != null)
                    discordTask = Task.Run(() => MainClient.Connect());
            };
            MainClient.MessageReceived += (sender, e) =>
            {
                DiscordServer serverIn = e.Channel.parent;
                openServerViews.ForEach(x =>
                {
                    if(x.Server.id == serverIn.id)
                    {
                        x.AddMessage(e.message);
                    }
                });
            };

            ToastNotificationManager.CreateToastNotifier(toastManager.GetAppID).Show(toastManager.CreateToast("DiscordWPF", "yum", "topkek"));
        }

        private void PopulateLists()
        {
            Dispatcher.Invoke(() =>
            {
                serversListView.Items.Clear();
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
            if (serversListView.SelectedIndex > -1)
            {
                ServerStub stub = serversListView.SelectedItem as ServerStub;
                ServerView view = new ServerView(stub.Server, MainClient);
                view.Closed += (se, xe) =>
                {
                    openServerViews.Remove(se as ServerView);
                };
                openServerViews.Add(view);
                view.Show();
                //ServerInfo info = new ServerInfo(stub.Server);
                //info.ShowDialog();
            }
        }

        private void PopulateChannelsList(DiscordServer server)
        {
            //channelsList.Items.Clear();
            Dispatcher.Invoke(() =>
            {
                server.channels.ForEach(x =>
                {
                    //if (x.type == "text")
                    //    channelsList.Items.Add($"#{x.name}");
                });
            });
        }

        private void serversListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (serversListView.SelectedIndex > -1)
            {
                //channelsList.Visibility = Visibility.Visible;
                var stub = serversListView.SelectedItem as ServerStub;
                PopulateChannelsList(stub.Server);
            }
            else
            {
                //channelsList.Visibility = Visibility.Hidden;
            }
        }
    }
}
