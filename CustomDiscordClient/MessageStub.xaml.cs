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
        public List<DiscordMessage> Messages { get; internal set; } = new List<DiscordMessage>();
        public List<string> MessageIDs { get; internal set; } = new List<string>();

        public MessageStub()
        {
            InitializeComponent();
        }

        public MessageStub(DiscordMessage message)
        {
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
            usernameLabel.Foreground = DiscordClientConfig.DarkThemeForeground;
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
                        Color roleColour = new Color();
                        roleColour.A = 255;
                        roleColour.R = (byte)x.color.R;
                        roleColour.G = (byte)x.color.G;
                        roleColour.B = (byte)x.color.B;
                        usernameLabel.Foreground = new SolidColorBrush(roleColour);
                    }
                    else
                        if(DiscordClientConfig.DarkTheme)
                            usernameLabel.Foreground = DiscordClientConfig.DarkThemeForeground;
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
                var blocks = markdownParser.Transform(Message.content, $"{Message.id};{channel.ID}");
                richTextBox.Document.Blocks.AddRange(blocks);
                message.Text = Message.content;
                if (Message.content.Trim() == "" && Message.attachments.Length > 0)
                    message.Text = "Attachment posted. Coming soon!";
            }

            ToolTip = $"Sent at {Message.timestamp}";

            MessageIDs.Add(Message.id);
            Messages.Add(Message);
        }

        public void AppendMessage(DiscordMessage message)
        {
            DiscordChannel channel = message.Channel() as DiscordChannel;
            var markdownParser = new Markdown(channel.parent, null);
            var blocks = markdownParser.Transform(message.content, $"{message.id};{channel.ID}");
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
