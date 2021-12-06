using CustomDiscordClient.Internal;
using DiscordSharp;
using DiscordSharp.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.UI.Notifications;
#if WIN10NOTIF
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#endif

namespace CustomDiscordClient
{
    public class FakeDiscordClient : DiscordClient
    {
        public new event EventHandler<DiscordConnectEventArgs> Connected;
        public new void Connect(bool useDotNetWebsocket)
        {
            Console.WriteLine("Faking Discord session.....");

            Thread.Sleep(1000);

            
            Connected.Invoke(this, new DiscordConnectEventArgs());
        }

        public new string SendLoginRequest()
        {
            return "OK";
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : CustomWindow
    {
        #region Notification stuffs
        ToastManager toastManager = new ToastManager("Dissonance");
        NotifyIcon notificationIcon;
        SoundPlayer notificationSound = new SoundPlayer(CustomDiscordClient.Properties.Resources.notification);
#endregion


        private List<ServerView> openServerViews;
        FakeDiscordClient MainClient;
        Task discordTask;
        bool closing = false;

        Uri MagicalDiscordIcon = new Uri("https://pbs.twimg.com/media/CSA9MacUcAAdY8h.png");

        string lastNotification;

        public MainWindow()
        {
            InitializeComponent();

            if (!App.ClientConfiguration.Settings.UseWindows10Notifications)
            {
                notificationIcon = new NotifyIcon();
                notificationIcon.Text = "Dissonance";
                notificationIcon.Visible = true;
                notificationIcon.Icon = CustomDiscordClient.Properties.Resources.taskbar;
                notificationIcon.BalloonTipIcon = ToolTipIcon.None;
            }

            Icon = new BitmapImage(MagicalDiscordIcon);
            //channelsList.Visibility = Visibility.Hidden;

            MainClient = new FakeDiscordClient();
            MainClient.RequestAllUsersOnStartup = true;

            SetupEvents();

            Title = "Dissonance - Connecting..";
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            if(MainClient.SendLoginRequest() != null)
                discordTask = Task.Run(() => ((FakeDiscordClient)MainClient).Connect());


            openServerViews = new List<ServerView>();

            SettingsGearClicked += MainWindow_SettingsGearClicked;

            SetupTheme();
        }

        private void MainWindow_SettingsGearClicked(object sender, EventArgs e)
        {
            Settings settings = new Settings(MainClient);
            settings.Closed += (sxc, exc) =>
            {
                if (settings.NeedsRestart)
                    App.RestartClient();
            };
            settings.ShowDialog();
        }

        private void SetupTheme()
        {
            serversListView.BorderThickness = new Thickness(0);
            if (App.ClientConfiguration.Settings.DarkTheme)
            {
                Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                Background = App.ClientConfiguration.Settings.DarkThemeBackground;

                Console.WriteLine(this.GetWindowButtonStyle());

                serversListView.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                serversListView.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                serversListView.BorderThickness = new Thickness(0);
            }
        }

        [STAThread]
        private void SetupEvents()
        {
            MainClient.Connected += (sender, e) =>
            {
                Dispatcher.Invoke(()=>this.Title = "Dissonance - " + e.User.Username);
                PopulateLists();
                
                Dispatcher.Invoke(()=>
                {
                    Mouse.OverrideCursor = null;
                });
            };
            MainClient.TextClientDebugMessageReceived += (sender, e) =>
            {
                //if(e.message.Level == MessageLevel.Critical)
                //{
                //    MessageBox.Show("Critical DiscordSharp Error Ocurred: " + e.message.Message);
                //}
            };
            MainClient.MessageDeleted += (sender, e) =>
            {
                if(e.DeletedMessage != null)
                {
                    var serverView = openServerViews.Find(x => x.Server.ID == e.Channel.Parent.ID);
                    if(serverView != null)
                    {
                        serverView.RemoveMessage(e.DeletedMessage);
                    }
                }
                else
                {
                    var serverView = openServerViews.Find(x => x.Server.ID == e.Channel.Parent.ID);
                    if (serverView != null)
                    {
                        serverView.RemoveMessage(e.RawJson["d"]["id"].ToString(), e.RawJson["d"]["channel_id"].ToString());
                    }
                }
            };
            MainClient.MentionReceived += (sender, e) =>
            {
                string toReplace = $"<@{MainClient.Me.ID}>";
                string message = e.Message.Content;
                message = message.Replace(toReplace, $"@{MainClient.Me.Username}");

                if (e.Author.Avatar != null)
                {
                    e.Author.GetAvatar().Save("temp.png");
                }
                string messageToShow = e.Message.Content;
                string idToReplace = $"<@{MainClient.Me.ID}>";
                messageToShow = messageToShow.Replace(idToReplace, $"@{MainClient.Me.Username}");
                if (App.ClientConfiguration.Settings.UseWindows10Notifications)
                {
                    var toast = toastManager.CreateToast(System.IO.Path.GetFullPath("temp.png"), $"Mention received from {e.Author.Username}", $"{messageToShow}", "");
                    lastNotification = $"{e.Channel.Parent.ID}:{e.Channel.ID}"; //server:channel
                    toast.Activated += (sxc, exc) =>
                    {
                        string[] split = lastNotification.Split(new char[] { ':' }, 2);
                        lastNotification = null;
                        string serverID = split[0];
                        string channelID = split[1];
                        bool hasServer = false;
                        foreach (var serverView in openServerViews)
                            if (serverView.Server.ID == serverID)
                                hasServer = true;

                        if (hasServer)
                        {
                            Dispatcher.Invoke(() => openServerViews.Find(x => x.Server.ID == serverID).Activate());
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ServerView view = new ServerView(MainClient.GetServersList().Find(x => x.ID == serverID), MainClient);
                                view.Closed += (x, f) =>
                                {
                                    openServerViews.Remove(x as ServerView);
                                };
                                openServerViews.Add(view);
                                view.LoadChannel(view.Server.Channels.Find(x => x.ID == channelID));
                                view.Show();
                                view.Activate();
                            });
                        }
                    };

                    ToastNotificationManager.CreateToastNotifier(toastManager.GetAppID).Show(toast);
                }
                else
                {
                    lastNotification = $"{e.Channel.Parent.ID}:{e.Channel.ID}"; //server:channel
                    notificationIcon.BalloonTipClicked += (sxc, exc) =>
                    {
                        string[] split = lastNotification.Split(new char[] { ':' }, 2);
                        lastNotification = null;
                        string serverID = split[0];
                        string channelID = split[1];
                        bool hasServer = false;
                        foreach (var serverView in openServerViews)
                            if (serverView.Server.ID == serverID)
                                hasServer = true;

                        if (hasServer)
                            Dispatcher.Invoke(() => openServerViews.Find(x => x.Server.ID == serverID).Activate());
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ServerView view = new ServerView(MainClient.GetServersList().Find(x => x.ID == serverID), MainClient);
                                view.Closed += (x, f) =>
                                {
                                    openServerViews.Remove(x as ServerView);
                                };
                                openServerViews.Add(view);
                                view.LoadChannel(view.Server.Channels.Find(x => x.ID == channelID));
                                view.Show();
                                view.Activate();
                            });
                        }
                    };
                    notificationIcon.BalloonTipTitle = $"Mentioned received from {e.Author.Username}";
                    notificationIcon.BalloonTipText = $"{messageToShow}";
                    notificationIcon.ShowBalloonTip(2500); //2.5 seconds
                    notificationSound.PlaySync();
                }
            };
            MainClient.SocketClosed += (sender, e) =>
            {
                if (closing)
                    return;
                Dispatcher.Invoke(() => Title = "Connection lost, retrying..");
                discordTask.Dispose();
                Task.Delay(3000).Wait();
                if (MainClient.SendLoginRequest() != null)
                    discordTask = Task.Run(() => MainClient.Connect());
            };
            MainClient.MessageReceived += (sender, e) =>
            {
                DiscordServer serverIn = e.Channel.Parent;
                openServerViews.ForEach(x =>
                {
                    if(x.Server.ID == serverIn.ID)
                    {
                        x.AddMessage(e.Message);
                    }
                });
            };
        }

        private void PopulateLists()
        {
            Dispatcher.Invoke(() =>
            {
                serversListView.Items.Clear();
                List<DiscordServer> tempCopy = MainClient.GetServersList();
                tempCopy.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
                foreach (var server in tempCopy)
                {
                    ServerStub stub = new ServerStub(server);
                    
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
                server.Channels.ForEach(x =>
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;

            if (notificationIcon != null)
            {
                notificationIcon.Visible = false;
                notificationIcon.Dispose();
            }

            //Write settings
            string settingsAsJson = JsonConvert.SerializeObject(App.ClientConfiguration.Settings);
            File.WriteAllText("settings.json", settingsAsJson);

            //If we hold left shift, we'll logout. Temporary.
            if (Keyboard.IsKeyDown(Key.LeftShift))
                System.IO.File.Delete("token_cache");

            //Try catch in case of any errors. Later disregard them.
            try
            {
                openServerViews.ForEach(x => x.Close());
            }
            catch { }

            //Finally, do a proper closing of the socket. Not a real logout as the token_cache is still present
            MainClient.Logout();
            MainClient.Dispose();
        }
    }
}
