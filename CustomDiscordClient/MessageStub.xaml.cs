using DiscordSharp;
using DiscordSharp.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
using System.Windows.Shapes;

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for MessageStub.xaml
    /// </summary>
    public partial class MessageStub : UserControl
    {
        public DiscordMessage Message { get; internal set; }
        internal DiscordClient mainClientReference { get; set; }
        public List<DiscordMessage> Messages { get; internal set; } = new List<DiscordMessage>();
        public List<string> MessageIDs { get; internal set; } = new List<string>();

        internal event EventHandler<EventArgs> IgnoredUserAdded;

        public MessageStub()
        {
            InitializeComponent();
        }

        public MessageStub(DiscordMessage message, DiscordClient refer)
        {
            mainClientReference = refer;
            InitializeComponent();
            Message = message;
            RefreshContent();
#if DEBUG
            richTextBox.MouseDoubleClick += RichTextBox_MouseDoubleClick;
#endif
        }

#if DEBUG
        private void RichTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string richText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            Console.WriteLine(richText);
        }
#endif

        private void SetTheme()
        {
            usernameLabel.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
        }

        public void RefreshContent()
        {
            MessageIDs = new List<string>();
            Messages = new List<DiscordMessage>();

            if (Message.author == null)
                usernameLabel.Content = "Removed User.";
            else
            {
                usernameLabel.Content = Message.author.Username;
                Message.author.Roles.ForEach(x =>
                {
                    if (x.position > -1 && x.name != "@everyone")
                    {
                        System.Windows.Media.Color roleColour = new System.Windows.Media.Color();
                        roleColour.A = 255;
                        roleColour.R = (byte)x.color.R;
                        roleColour.G = (byte)x.color.G;
                        roleColour.B = (byte)x.color.B;
                        usernameLabel.Foreground = new SolidColorBrush(roleColour);
                    }
                    else
                        if(App.ClientConfiguration.Settings.DarkTheme)
                            usernameLabel.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                });
            }
            if(Message.author != null)
                if (Message.author.Avatar == null)
                    userAvatar.Source = new BitmapImage(DiscordClientConfig.DefaultAvatarBlue);
                else
                    userAvatar.Source = new BitmapImage(Message.author.GetAvatarURL());

            //parsing
            {
                richTextBox.Document.Blocks.Clear();
                DiscordChannel channel = Message.Channel() as DiscordChannel;
                var markdownParser = new CustomDiscordClient.Markdown(channel.parent, null);
                var blocks = markdownParser.Transform(Message, $"{Message.id};{channel.ID}");
                richTextBox.Document.Blocks.AddRange(blocks);
                message.Text = Message.content;
                if (Message.content.Trim() == "" && Message.attachments.Length > 0)
                    message.Text = "Attachment posted. Coming soon!";
            }

            ToolTip = $"Sent at {Message.timestamp}";

            MessageIDs.Add(Message.id);
            Messages.Add(Message);

            SetupContextMenu();
        }

        public void SetMessageText(string text)
        {
            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        private void SetupContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            MenuItem IgnoreUserMenuItem = new MenuItem();
            IgnoreUserMenuItem.Header = "Ignore user";
            if (Message.author != null)
            {
                if (Message.author.ID == mainClientReference.Me.ID)
                {
                    IgnoreUserMenuItem.IsEnabled = false;
                    IgnoreUserMenuItem.Header = "I wish I could ignore myself too...";
                }
            }
            else
                IgnoreUserMenuItem.IsEnabled = false;

            IgnoreUserMenuItem.Click += (sender, e) =>
            {
                if(IgnoreUserMenuItem.IsEnabled) //precaution
                {
                    App.ClientConfiguration.Settings.IgnoredUserIDs.Add(Message.author.ID);
                    if (IgnoredUserAdded != null)
                        IgnoredUserAdded(this, new EventArgs());
                }
            };
            cm.Items.Add(IgnoreUserMenuItem);

            this.usernameLabel.ContextMenu = cm;
        }

        public void AppendMessage(DiscordMessage message)
        {
            if(message.author != null && App.ClientConfiguration.Settings.IgnoredUserIDs.Contains(message.author.ID))
            { return; }
            DiscordChannel channel = message.Channel() as DiscordChannel;
            var markdownParser = new Markdown(channel.parent, null);
            var blocks = markdownParser.Transform(message, $"{message.id};{channel.ID}");
            richTextBox.Document.Blocks.AddRange(blocks);
            ToolTip = $"Sent at {message.timestamp}";
            MessageIDs.Add(message.id);
            Messages.Add(message);
        }

        public void RemoveMessage(DiscordMessage message)
        {
            MessageIDs.Remove(message.id);
            string oldText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text.Replace(message.content, "");
        }

        public void RemoveMessage(string id)
        {
            var message = Messages.Find(x => x.id == id);
            MessageIDs.Remove(id);
            string oldText = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd).Text;
            Dispatcher.Invoke(() =>
            {
                foreach (var block in richTextBox.Document.Blocks)
                {
                    if ((new TextRange(block.ContentStart, block.ContentEnd).Text.Contains(NoMarkdown(message))))
                    {
                        richTextBox.Document.Blocks.Remove(block);
                        richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                        richTextBox.CaretPosition.InsertTextInRun("\n<Removed message>");
                        break;
                    }
                }
            });
            Messages.Remove(message);
        }

        private string NoMarkdown(DiscordMessage message)
        {
            string returnValue = message.content;
            returnValue = returnValue.Trim(new char[] { '`', '*' });
            returnValue = returnValue.Replace("```", "");
            foreach(Match match in Markdown._username.Matches(returnValue))
            {
                string ID = match.Value.Trim(new char[] { '<', '@', '>' });
                DiscordMember user = (Message.Channel() as DiscordChannel).parent.members.Find(x => x.ID == ID);
                returnValue = match.Result($"@{user.Username}");
                Markdown._username.Replace(returnValue, ID);
            }

            return returnValue;
        }

        private void richTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {}
    }
}
