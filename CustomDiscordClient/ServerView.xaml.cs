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
using System.Windows.Shapes;

namespace CustomDiscordClient
{
    public static class Exetn
    {
        public static IEnumerable<Visual> GetChildren(this DependencyObject parent, bool recurse = true)
        {
            if (parent != null)
            {
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    // Retrieve child visual at specified index value.
                    var child = VisualTreeHelper.GetChild(parent, i) as Visual;

                    if (child != null)
                    {
                        yield return child;

                        if (recurse)
                        {
                            foreach (var grandChild in child.GetChildren(true))
                            {
                                yield return grandChild;
                            }
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : Window
    {
        //TODO: make side lists collapseable
        public DiscordServer Server {
            get; internal set;
        }
        public DiscordClient mainClientReference { get; internal set; }
        private List<DiscordMessage> messageHistoryCache = new List<DiscordMessage>();
        private DiscordChannel currentChannel;

        List<Key> _pressedKeys = new List<Key>(); //ew

        public ServerView()
        {
            InitializeComponent();
            SetTheme();
        }

        public ServerView(DiscordServer server)
        {
            InitializeComponent();
            Server = server;
            RefreshContent();
            SetTheme();
        }
        public ServerView(DiscordServer server, DiscordClient client)
        {
            InitializeComponent();
            mainClientReference = client;
            Server = server;
            RefreshContent();
            SetTheme();
        }

        private void SetTheme()
        {
            if (DiscordClientConfig.DarkTheme)
            {
                this.Background = DiscordClientConfig.DarkThemeBackground;
                Foreground = DiscordClientConfig.DarkThemeForeground;

                channelsListBox.Background = DiscordClientConfig.DarkThemeBackground;
                channelsListBox.Foreground = DiscordClientConfig.DarkThemeForeground;

                membersListBox.Background = DiscordClientConfig.DarkThemeBackground;
                membersListBox.Foreground = DiscordClientConfig.DarkThemeForeground;

                messagesList.Background = DiscordClientConfig.DarkThemeBackground;
                messagesList.Foreground = DiscordClientConfig.DarkThemeForeground;

                messageToSend.Background = messagesList.Background;
                messageToSend.Foreground = messagesList.Foreground;

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
            this.Title = $"Discord - {Server.name}";
            //TODO
            Server.channels.ForEach(x =>
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
                    messagesList.SelectedIndex = messagesList.Items.Count - 1;
                    messagesList.ScrollIntoView(messagesList.SelectedItem);
                });
            });

            Server.members.ForEach(x =>
            {
                Dispatcher.Invoke(() =>
                {
                    //TODO: make nice member stub
                    if(x.Status == Status.Online)
                        membersListBox.Items.Add(x.Username);
                });
            });

            channelsListBox.SelectedIndex = 0;
            currentChannel = Server.channels.Find(x => x.Name == channelsListBox.SelectedItem.ToString().Substring(1));
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
                currentChannel = Server.channels.Find(x => x.Name == channelsListBox.Items[channelsSelectedIndex].ToString().Substring(1));
                if (currentChannel != null)
                {
                    LoadChannel(currentChannel);
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
            MessageStub stub = new MessageStub(m);
            messagesList.Items.Add(stub);
            messagesList.ScrollIntoView(messagesList.Items[messagesList.Items.Count - 1]);
            //string author = "<Removed User>";
            //if (m.author != null)
            //    author = m.author.Username;

            //if (m.content.Trim() == "" && m.attachments != null)
            //    messageView.Text += $"<{author}> Posted an attachment. Coming soon!" + Environment.NewLine;
            //else
            //    messageView.Text += $"<{author}> {m.content}" + Environment.NewLine;
        }

        public void LoadChannel(DiscordChannel channel)
        {
            messagesList.Items.Clear();
            foreach(var m in messageHistoryCache)
            {
                if(m.Channel() == channel)
                {
                    AppendMessage(m);
                    Title = $"Discord - {Server.name} - #{channel.Name}";
                }
            }
            messagesList.SelectedIndex = messagesList.Items.Count - 1;
            messagesList.ScrollIntoView(messagesList.SelectedItem);
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
                messagesList.SelectedIndex = messagesList.Items.Count - 1;
                messagesList.ScrollIntoView(messagesList.SelectedItem);
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(messageToSend.Text);
            messageToSend.Clear();
        }

        private void messageToSend_KeyDown(object sender, KeyEventArgs e)
        {
            if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {}
            else if (Keyboard.IsKeyDown(Key.Enter))
            {
                //messageToSend.Text += Environment.NewLine;
                SendMessage(messageToSend.Text);
                messageToSend.Clear();
            }
        }

        private void membersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (membersListBox.SelectedIndex > -1)
            {
                DiscordMember member = Server.members.Find(x => x.Username == membersListBox.SelectedItem.ToString());
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
