﻿using CustomDiscordClient.Internal;
using DiscordSharp;
using DiscordSharp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
#if WIN10NOTIF
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#endif

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : CustomWindow
    {
        #region Notification stuffs
#if WIN10NOTIF
        ToastManager toastManager = new ToastManager("DiscordWPF");
#else
        NotifyIcon notificationIcon;
        SoundPlayer notificationSound = new SoundPlayer(CustomDiscordClient.Properties.Resources.notification);
#endif
#endregion


        private List<ServerView> openServerViews;
        DiscordClient MainClient;
        Task discordTask;
        bool closing = false;

        Uri MagicalDiscordIcon = new Uri("https://pbs.twimg.com/media/CSA9MacUcAAdY8h.png");

        string lastNotification;

        public MainWindow()
        {
            InitializeComponent();

#if WIN10NOTIF
#else
            notificationIcon = new NotifyIcon();
            notificationIcon.Text = "WPF Discord";
            notificationIcon.Visible = true;
            notificationIcon.Icon = CustomDiscordClient.Properties.Resources.taskbar;
            notificationIcon.BalloonTipIcon = ToolTipIcon.None;
#endif

            Icon = new BitmapImage(MagicalDiscordIcon);
            //channelsList.Visibility = Visibility.Hidden;

            MainClient = new DiscordClient();
            MainClient.RequestAllUsersOnStartup = true;

            SetupEvents();

            Title = "Connecting..";
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            if(MainClient.SendLoginRequest() != null)
                discordTask = Task.Run(() => MainClient.Connect());

            openServerViews = new List<ServerView>();

            SetupTheme();
        }

        private void SetupTheme()
        {
            serversListView.BorderThickness = new Thickness(0);
            if (DiscordClientConfig.DarkTheme)
            {
                Foreground = DiscordClientConfig.DarkThemeForeground;
                Background = DiscordClientConfig.DarkThemeBackground;

                Console.WriteLine(this.GetWindowButtonStyle());

                serversListView.Foreground = DiscordClientConfig.DarkThemeForeground;
                serversListView.Background = DiscordClientConfig.DarkThemeBackground;
                serversListView.BorderThickness = new Thickness(0);
            }
        }

        [STAThread]
        private void SetupEvents()
        {
            MainClient.Connected += (sender, e) =>
            {
                Dispatcher.Invoke(()=>this.Title = "Discord - " + e.user.Username);
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
                    var serverView = openServerViews.Find(x => x.Server.id == e.Channel.parent.id);
                    if(serverView != null)
                    {
                        serverView.RemoveMessage(e.DeletedMessage);
                    }
                }
                else
                {
                    var serverView = openServerViews.Find(x => x.Server.id == e.Channel.parent.id);
                    if (serverView != null)
                    {
                        serverView.RemoveMessage(e.RawJson["d"]["id"].ToString(), e.RawJson["d"]["channel_id"].ToString());
                    }
                }
            };
            MainClient.MentionReceived += (sender, e) =>
            {
                string toReplace = $"<@{MainClient.Me.ID}>";
                string message = e.message.content;
                message = message.Replace(toReplace, $"@{MainClient.Me.Username}");

                if (e.author.Avatar != null)
                {
                    e.author.GetAvatar().Save("temp.png");
                }
                string messageToShow = e.message.content;
                string idToReplace = $"<@{MainClient.Me.ID}>";
                messageToShow = messageToShow.Replace(idToReplace, $"@{MainClient.Me.Username}");
#if WIN10NOTIF
                var toast = toastManager.CreateToast(System.IO.Path.GetFullPath("temp.png"), $"Mention received from {e.author.Username}", $"{messageToShow}", "");
                lastNotification = $"{e.Channel.parent.id}:{e.Channel.ID}"; //server:channel
                toast.Activated += (sxc, exc) =>
                {
                    string[] split = lastNotification.Split(new char[] { ':' }, 2);
                    lastNotification = null;
                    string serverID = split[0];
                    string channelID = split[1];
                    bool hasServer = false;
                    foreach (var serverView in openServerViews)
                        if (serverView.Server.id == serverID)
                            hasServer = true;

                    if (hasServer)
                    {
                        Dispatcher.Invoke(()=>openServerViews.Find(x => x.Server.id == serverID).Activate());
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ServerView view = new ServerView(MainClient.GetServersList().Find(x => x.id == serverID), MainClient);
                            view.Closed += (x, f) =>
                            {
                                openServerViews.Remove(x as ServerView);
                            };
                            openServerViews.Add(view);
                            view.LoadChannel(view.Server.channels.Find(x => x.ID == channelID));
                            view.Show();
                            view.Activate();
                        });
                    }
                };

                ToastNotificationManager.CreateToastNotifier(toastManager.GetAppID).Show(toast);
#else
                lastNotification = $"{e.Channel.parent.id}:{e.Channel.ID}"; //server:channel
                notificationIcon.BalloonTipClicked += (sxc, exc) =>
                {
                    string[] split = lastNotification.Split(new char[] { ':' }, 2);
                    lastNotification = null;
                    string serverID = split[0];
                    string channelID = split[1];
                    bool hasServer = false;
                    foreach (var serverView in openServerViews)
                        if (serverView.Server.id == serverID)
                            hasServer = true;

                    if (hasServer)
                        Dispatcher.Invoke(() => openServerViews.Find(x => x.Server.id == serverID).Activate());
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ServerView view = new ServerView(MainClient.GetServersList().Find(x => x.id == serverID), MainClient);
                            view.Closed += (x, f) =>
                            {
                                openServerViews.Remove(x as ServerView);
                            };
                            openServerViews.Add(view);
                            view.LoadChannel(view.Server.channels.Find(x => x.ID == channelID));
                            view.Show();
                            view.Activate();
                        });
                    }
                };
                notificationIcon.BalloonTipTitle = $"Mentioned received from {e.author.Username}";
                notificationIcon.BalloonTipText = $"{messageToShow}";
                notificationIcon.ShowBalloonTip(2500); //2.5 seconds
                notificationSound.PlaySync();
#endif
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
                DiscordServer serverIn = e.Channel.parent;
                openServerViews.ForEach(x =>
                {
                    if(x.Server.id == serverIn.id)
                    {
                        x.AddMessage(e.message);
                    }
                });
            };
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
#if WIN10NOTIF
#else
            notificationIcon.Visible = false;
            notificationIcon.Dispose();
#endif
            if (Keyboard.IsKeyDown(Key.LeftShift))
                System.IO.File.Delete("token_cache");
            try
            {
                openServerViews.ForEach(x => x.Close());
            }
            catch { }
            MainClient.Logout();
            MainClient.Dispose();
        }
    }
}
