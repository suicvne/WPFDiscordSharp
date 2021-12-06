﻿using CustomDiscordClient.Internal;
using DiscordSharp;
using DiscordSharp.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : CustomWindow
    {
        //TODO: make side lists collapseable
        public DiscordServer Server {get; internal set;}
        public DiscordClient mainClientReference { get; internal set; }
        private List<DiscordMessage> messageHistoryCache = new List<DiscordMessage>();
        private DiscordChannel currentChannel;

        List<Key> _pressedKeys = new List<Key>(); //ew

        public ServerView()
        {
            InitializeComponent();
            SetTheme();
            AllowResizing = true;
        }

        public ServerView(DiscordServer server)
        {
            InitializeComponent();
            Server = server;
            RefreshContent();
            SetTheme();
            AllowResizing = true;
        }

        public ServerView(DiscordServer server, DiscordClient client)
        {
            InitializeComponent();
            mainClientReference = client;
            Server = server;
            RefreshContent();
            SetTheme();
            AllowResizing = true;
        }

        private void SetTheme()
        {
            this.ShowSettingsButton = Visibility.Hidden;
            messageToSend.AcceptsReturn = false;
            if (App.ClientConfiguration.Settings.DarkTheme)
            {
                this.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                channelsListBox.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                channelsListBox.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                membersListBox.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                membersListBox.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                messagesList.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                messagesList.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                messageToSend.Background = messagesList.Background;
                messageToSend.Foreground = messagesList.Foreground;
                messageToSend.BorderThickness = new Thickness(0, 0, 0, 1);

                foreach(var element in this.GridContainer.Children)
                {
                    try
                    {
                        if ((element as Control).Name == "messageToSend")
                            continue;
                        (element as Control).BorderThickness = new Thickness(0);
                    }
                    catch { }
                }
                foreach(var element in this.AutoSizer.Children)
                {
                    try
                    {
                        if ((element as Control).Name == "messageToSend")
                            continue;
                        (element as Control).BorderThickness = new Thickness(0);
                    }
                    catch { }
                }
            }
        }

        private void RefreshContent()
        {
            if (Server.IconURL != null)
                this.Icon = new BitmapImage(new Uri(Server.IconURL));
            this.Title = $"Dissonance - {Server.Name}";
            //TODO
            Server.Channels.ForEach(x =>
            {
                Dispatcher.Invoke(() =>
                {
                    if(x.Type == ChannelType.Text)
                        channelsListBox.Items.Add("#" + x.Name);
                    if(mainClientReference != null)
                    {
                        var messageHistory = mainClientReference.GetMessageHistory(x, 50, null, null);
                        if (messageHistory != null)
                        {
                            messageHistory.Reverse();
                            messageHistory.ForEach(m => messageHistoryCache.Add(m));
                        }
                    }
                    //messagesList.SelectedIndex = messagesList.Items.Count - 1;
                    //messagesList.ScrollIntoView(messagesList.SelectedItem);
                    
                });
            });

            foreach(var memberKvp in Server.Members)
            {
                Dispatcher.Invoke(() =>
                {
                    //TODO: make nice member stub
                    if (memberKvp.Value.Status == Status.Online)
                        membersListBox.Items.Add(memberKvp.Value.Username);
                });
            }

            channelsListBox.SelectedIndex = 0;
            currentChannel = Server.Channels.Find(x => x.Name == channelsListBox.SelectedItem.ToString().Substring(1));
            LoadChannel(currentChannel);
        }

        int channelsSelectedIndex;
        public int ChSelectedIndex
        {
            get
            {
                return channelsSelectedIndex;
            }
            set
            {
                if (channelsSelectedIndex == value)
                    return;

                channelsSelectedIndex = value;
                RaisePropertyChanged(() => ChSelectedIndex);
            }
        }
        private void RaisePropertyChanged(Func<int> p)
        {
            if (channelsListBox.SelectedIndex > -1)
            {
                currentChannel = Server.Channels.Find(x => x.Name == channelsListBox.Items[channelsSelectedIndex].ToString().Substring(1));
                if (currentChannel != null)
                {
                    LoadChannel(currentChannel);
                    IgnoreUserUpdate(this, new EventArgs()); //force clearing out
                }
            }
        }

        public void AddMessage(DiscordMessage message)
        {
            DiscordChannel channel = Convert.ChangeType(message.Channel(), typeof(DiscordChannel));
            messageHistoryCache.Add(message);
            if(currentChannel == message.Channel())
                Dispatcher.Invoke(() => AppendMessage(message));
        }

        private void AppendMessage(DiscordMessage m)
        {
            if(messagesList.Items.Count > 0)
            {
                var previousStub = (messagesList.Items[messagesList.Items.Count - 1] as MessageStub);
                if ((previousStub.Message.timestamp - m.timestamp) < TimeSpan.FromMinutes(2))
                {
                    if (m.Author == (messagesList.Items[messagesList.Items.Count - 1] as MessageStub).Message.Author)
                    {
                        (messagesList.Items[messagesList.Items.Count - 1] as MessageStub).AppendMessage(m);
                    }
                    else
                    {
                        MessageStub stub = new MessageStub(m, mainClientReference);
                        if (stub.Message.Author != null)
                            if (App.ClientConfiguration.Settings.IgnoredUserIDs.Contains(stub.Message.Author.ID))
                                stub.SetMessageText("<Ignored Message>");
                        stub.IgnoredUserAdded += IgnoreUserUpdate;
                        messagesList.Items.Add(stub);
                        MainScroller.ScrollToBottom();
                    }
                }
                else
                {
                    MessageStub stub = new MessageStub(m, mainClientReference);
                    if (stub.Message.Author != null)
                        if (App.ClientConfiguration.Settings.IgnoredUserIDs.Contains(stub.Message.Author.ID))
                            stub.SetMessageText("<Ignored Message>");
                    stub.IgnoredUserAdded += IgnoreUserUpdate;
                    messagesList.Items.Add(stub);
                    MainScroller.ScrollToBottom();
                }
            }
            else
            {
                MessageStub stub = new MessageStub(m, mainClientReference);
                if(stub.Message.Author != null)
                    if (App.ClientConfiguration.Settings.IgnoredUserIDs.Contains(stub.Message.Author.ID))
                        stub.SetMessageText("<Ignored Message>");
                stub.IgnoredUserAdded += IgnoreUserUpdate;
                messagesList.Items.Add(stub);
                MainScroller.ScrollToBottom();
            }
        }

        public void IgnoreUserUpdate(object sender, EventArgs e)
        {
            foreach(MessageStub stub in messagesList.Items)
            {
                if (stub.Message.Author != null)
                    if (App.ClientConfiguration.Settings.IgnoredUserIDs.Contains(stub.Message.Author.ID))
                    {
                        stub.SetMessageText("<Ignored Message>");
                    }
            }
        }

        public void RemoveMessage(DiscordMessage m)
        {
            RemoveMessage(m.ID, (m.Channel() as DiscordChannel).ID);
        }

        public void RemoveMessage(string id, string channel_id)
        {
            if (messagesList.Items.Count > 0)
            {
                if (channel_id == currentChannel.ID)
                {
                    foreach (MessageStub stubby in messagesList.Items)
                    {
                        if (stubby.MessageIDs.Contains(id))
                        {
                            stubby.RemoveMessage(id);
                        }
                    }
                }
            }
            messageHistoryCache.Remove(messageHistoryCache.Find(x => x.ID == id));
            //TODO: find a better way to do this :(((
        }

        public void LoadChannel(DiscordChannel channel)
        {
            messagesList.Items.Clear();
            foreach(var m in messageHistoryCache)
            {
                if(m.Channel() == channel)
                {
                    AppendMessage(m);
                    Title = $"Dissonance - {Server.Name} - #{channel.Name}";
                }
            }
            MainScroller.ScrollToBottom();
        }

        public void UpdateServer(DiscordServer server)
        {
            Server = server;
        }

        private void channelsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //TODO: channel info
        }

        private void SendMessage(string message)
        {
            if (currentChannel != null && !string.IsNullOrEmpty(message))
            {
                currentChannel.SendMessage(message);
                MainScroller.ScrollToBottom();
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(messageToSend.Text);
            messageToSend.Clear();
        }

        bool simulatedTyping = false;
        private void messageToSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (!simulatedTyping)
            {
                // not in latest versions of DiscordSharp :(
                //mainClientReference.SimulateTyping(currentChannel);

                simulatedTyping = true;
                Task.Run(() =>
                {
                    Task.Delay(10 * 1000).Wait();
                    simulatedTyping = false;
                });
            }

            if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {}
            else if (Keyboard.IsKeyDown(Key.Enter))
            {
                //messageToSend.Text += Environment.NewLine;
                SendMessage(messageToSend.Text);
                messageToSend.Clear();
                simulatedTyping = false;
            }
        }

        private void membersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (membersListBox.SelectedIndex > -1)
            {
                DiscordMember member = Server.Members.Find(x => x.Username == membersListBox.SelectedItem.ToString());
                if (member != null)
                {
                    UserInfo info = new UserInfo(member, mainClientReference);
                    info.ShowDialog();
                }
            }
        }
        
        private void messageToSend_LostFocus(object sender, RoutedEventArgs e)
        {
            _pressedKeys.Clear();
        }

        private void messageToSend_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                messageToSend.AcceptsReturn = true;
            }
            if((ModifierKeys.Shift & Keyboard.Modifiers) == ModifierKeys.Shift)
            {
                if(Keyboard.IsKeyDown(Key.Enter))
                {
                    messageToSend.Text += Environment.NewLine;
                    messageToSend.CaretIndex = messageToSend.Text.Length - 1;
                    e.Handled = true;
                }
            }
            if((ModifierKeys.Control & Keyboard.Modifiers) == ModifierKeys.Control)
            {
                if(Keyboard.IsKeyDown(Key.V)) //override pasting
                {
                    if(Clipboard.ContainsImage()) //upload image, yay
                    {
                        var image = Clipboard.GetImage();
                        using (var fileStream = new FileStream("unknown.png", FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(image));
                            encoder.Save(fileStream);
                        }

                        mainClientReference.AttachFile(currentChannel, "", "unknown.png");

                        File.Delete("unknown.png");
                    }
                }
            }
        }

        private void messageToSend_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift)
            {
                messageToSend.AcceptsReturn = false;
            }
        }

    }
}
