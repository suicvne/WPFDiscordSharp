using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace CustomDiscordClient
{
    public static class Utilities
    {
        public enum OSFriendly { Windows95, Windows98, WindowsME, Windows2000, WindowsXP, WindowsVista, Windows7, Windows8, Windows81, Windows10, Linux, Unknown }

        public static OSFriendly OSName()
        {
            double osVersion = double.Parse(Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString());

            if (osVersion == 5.0)
                return OSFriendly.Windows2000;
            else if (osVersion == 5.1)
                return OSFriendly.WindowsXP;
            else if (osVersion == 6.0)
                return OSFriendly.WindowsVista;
            else if (osVersion == 6.1)
                return OSFriendly.Windows7;
            else if (osVersion == 6.2)
                return OSFriendly.Windows8;
            else if (osVersion == 6.3)
                return OSFriendly.Windows81;
            else if (osVersion == 10)
                return OSFriendly.Windows10;
            else
                return OSFriendly.Unknown;
        }

        public static bool IsWindows10()
        {
            var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            string productName = reg.GetValue("ProductName") as string;
            return productName.StartsWith("Windows 10"); //TODO: better workaround for this
        }

        public static double OSVersion()
        {
            return double.Parse(Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString());
        }
    }
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DiscordClientConfig ClientConfiguration = new DiscordClientConfig();
        private static App app;
        [STAThread]
        public static void Main(string[] args)
        {
            if (File.Exists("settings.json"))
                ClientConfiguration = new DiscordClientConfig(true);

            System.Windows.Forms.Application.EnableVisualStyles();

            app = new App();

            ///Special defines for Windows 10 notification support
            if(Utilities.OSName() != Utilities.OSFriendly.Windows10)
            {
                ClientConfiguration.Settings.UseWindows10Notifications = false;
            }
            ///

            if (!File.Exists("token_cache"))
            {
                var loginWindow = new LoginForm();
                loginWindow.Show();
            }
            else
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            app.Run();
        }

        public static void RestartClient()
        {
            string jsonText = JsonConvert.SerializeObject(ClientConfiguration.Settings);
            File.WriteAllText("settings.json", Assembly.GetExecutingAssembly().Location);
            Process.Start(Application.ResourceAssembly.Location);
            app.Shutdown();
        }
    }

    
}
