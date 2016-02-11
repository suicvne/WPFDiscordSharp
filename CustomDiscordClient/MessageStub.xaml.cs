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

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for MessageStub.xaml
    /// </summary>
    public partial class MessageStub : UserControl
    {
        public DiscordMessage Message { get; internal set; }

        public MessageStub()
        {
            InitializeComponent();
        }

        public MessageStub(DiscordMessage message)
        {
            InitializeComponent();
            Message = message;
            RefreshContent();
        }

        public void RefreshContent()
        {
            if (Message.author == null)
                usernameLabel.Content = "Removed User.";
            else
            {
                usernameLabel.Content = Message.author.Username;
                Message.author.Roles.ForEach(x =>
                {
                    if (x.position > -1)
                    {
                        Color roleColour = new Color();
                        roleColour.A = 255;
                        roleColour.R = (byte)x.color.R;
                        roleColour.G = (byte)x.color.G;
                        roleColour.B = (byte)x.color.B;
                        usernameLabel.Foreground = new SolidColorBrush(roleColour);
                    }
                });
            }

            if (Message.author.Avatar == null)
                userAvatar.Source = new BitmapImage(DiscordClientConfig.DefaultAvatarBlue);
            else
                userAvatar.Source = new BitmapImage(Message.author.GetAvatarURL());
            if (Message.content.Trim() == "" && Message.attachments.Length > 0)
                message.Text = "Attachment posted. Coming soon!";
            else
                message.Text = Message.content;
        }
    }
}
