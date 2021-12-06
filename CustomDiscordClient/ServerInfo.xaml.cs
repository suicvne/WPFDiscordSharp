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
    /// Interaction logic for ServerInfo.xaml
    /// </summary>
    public partial class ServerInfo : Window
    {
        public DiscordServer Server { get; set; }

        public ServerInfo()
        {
            InitializeComponent();
        }

        public ServerInfo(DiscordServer server)
        {
            Server = server;
            InitializeComponent();
            Icon = new BitmapImage(new Uri(server.IconURL));
            Title = $"Info for Server {Server.Name}";


            serverIcon.Source = Icon;
            serverNameLabel.Content = Server.Name;
            owner.Content = $"Owner: {Server.Owner.Username} ({Server.Owner.ID})";
            channelsNumeber.Content = $"Channels Count: {Server.Channels.Count}";
            membersNumber.Content = $"Members Count: {Server.Members.Count}";
        }
        private int angle = 0;
        private void serverIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            angle++;
            if (angle > 360)
                angle = 0;
            serverIcon.RenderTransform = new RotateTransform((angle));
        }
    }
}
