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
    /// <summary>
    /// Interaction logic for ServerView.xaml
    /// </summary>
    public partial class ServerView : Window
    {
        public DiscordServer Server {
            get; internal set;
        }
        public DiscordClient mainClientReference { get; internal set; }
        private List<DiscordMessage> messageHistoryCache = new List<DiscordMessage>();
        private DiscordChannel currentChannel;

        public ServerView()
        {
            InitializeComponent();
        }

        public ServerView(DiscordServer server)
        {
            InitializeComponent();
            Server = server;
            RefreshContent();
        }
        public ServerView(DiscordServer server, DiscordClient client)
        {
            InitializeComponent();
            mainClientReference = client;
            Server = server;
            RefreshContent();
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
            //messageView.Text = "";
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
            if(channelsListBox.SelectedIndex > -1)
            {
                currentChannel = Server.channels.Find(x => x.Name == channelsListBox.SelectedItem.ToString().Substring(1));
                if(currentChannel != null)
                {
                    LoadChannel(currentChannel);
                }
            }
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
            if (e.Key == Key.Enter)
            {
                SendMessage(messageToSend.Text);
                messageToSend.Clear();
            }
        }

        private void membersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(membersListBox.SelectedIndex > -1)
            {
                DiscordMember member = Server.members.Find(x => x.Username == membersListBox.SelectedItem.ToString());
                if(member != null)
                {
                    UserInfo info = new UserInfo(member, mainClientReference);
                    info.ShowDialog();
                }
            }
        }
    }
}
