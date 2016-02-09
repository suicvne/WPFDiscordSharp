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
    /// Interaction logic for UserInfo.xaml
    /// </summary>
    public partial class UserInfo : Window
    {
        public DiscordMember Member { get; internal set; }
        public DiscordClient mainClientReference { get; internal set; }

        public UserInfo()
        {
            InitializeComponent();
        }

        public UserInfo(DiscordMember member, DiscordClient client)
        {
            InitializeComponent();

            Member = member;
            mainClientReference = client;

            if(Member.Avatar != null)
            {
                BitmapImage _userAvatar = new BitmapImage(Member.GetAvatarURL());
                Icon = _userAvatar;
                try
                {
                    userAvatar.Source = _userAvatar;
                }
                catch (Exception) { }
            }
            usernameLabel.Content = Member.Username + $" (#{Member.Discriminator})";
            Title = "User info for " + Member.Username;
            if (member.CurrentGame != null)
                userID.Content = $"Playing {member.CurrentGame}";

            foreach(var server in mainClientReference.GetServersList())
            {
                foreach(var __member in server.members)
                {
                    if(__member.ID == member.ID)
                    {
                        ServerStub stub = new ServerStub(server);
                        inServers.Items.Add(stub);
                    }
                }
            }
        }
    }
}
