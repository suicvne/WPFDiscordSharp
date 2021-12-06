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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomDiscordClient
{
    /// <summary>
    /// Interaction logic for ServerStub.xaml
    /// </summary>
    public partial class ServerStub : UserControl
    {
        public DiscordServer Server { get; set; }

        public Dictionary<string, string> Test { get; set; }

        public BitmapImage GetIcon
        {
            get
            {
                if(Server.IconURL != null)
                    return new BitmapImage(new Uri(Server.IconURL));
                return new BitmapImage();
            }
        }

        public ServerStub()
        {
            InitializeComponent();
            SetupTheme();
        }

        public ServerStub(DiscordServer server)
        {
            Server = server;
            InitializeComponent();

            if (server.IconURL != null)
                serverIcon.Source = new BitmapImage(new Uri(server.IconURL));
            serverNameLabel.Content = server.Name;

            this.ToolTip = server.Name;
            SetupTheme();
        }

        private void SetupTheme()
        {
            if(App.ClientConfiguration.Settings.DarkTheme)
                serverNameLabel.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
        }
    }
}
