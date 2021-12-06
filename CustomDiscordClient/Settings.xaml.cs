using CustomDiscordClient.Internal;
using DiscordSharp;
using DiscordSharp.Objects;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : CustomWindow
    {
        public bool NeedsRestart { get; internal set; } = false;
        internal DiscordClient mainClientReference { get; set; }
        public Settings(DiscordClient clientref)
        {
            mainClientReference = clientref;
            InitializeComponent();
            SetupTheme();
            LoadSettings();
        }

        private void SetupTheme()
        {
            if(App.ClientConfiguration.Settings.DarkTheme)
            {
                this.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                this.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                darkThemeCheckbox.Foreground = Foreground;
                darkThemeCheckbox.Background = Background;
                win10Notifications.Foreground = Foreground;
                win10Notifications.Background = Background;
                button.Foreground = Foreground;
                button.Background = Background;
                button_Copy.Foreground = Foreground;
                button_Copy.Background = Background;
                ignoredUsersLabel.Foreground = Foreground;
                ignoredUsersLabel.Background = Background;
                ignoredUsersListBox.Foreground = Foreground;
                ignoredUsersListBox.Background = Background;
                //ignoredUsersListBox.BorderThickness = new Thickness(0, 0, 0, 0);
            }
        }

        private int SizeOfCacheFolderInMB()
        {
            DirectoryInfo cacheInfo = new DirectoryInfo("compact_cache");
            // TODO: ?

            return -1;
        }

        private void LoadSettings()
        {
            darkThemeCheckbox.IsChecked = App.ClientConfiguration.Settings.DarkTheme;
            if (Utilities.IsWindows10())
                win10Notifications.IsChecked = App.ClientConfiguration.Settings.UseWindows10Notifications;
            else
            {
                win10Notifications.IsEnabled = false;
                win10Notifications.Content += $" (Not on {Utilities.OSName().ToString()})";
            }

            if(App.ClientConfiguration.Settings.IgnoredUserIDs.Count > 0)
            {
                App.ClientConfiguration.Settings.IgnoredUserIDs.ForEach(x =>
                {
                    DiscordMember memberFromID = mainClientReference.Me;
                    mainClientReference.GetServersList().ForEach(server =>
                    {
                        foreach(var kvpMembers in server.Members)
                        {
                            if(kvpMembers.Value.ID == x)
                            {
                                memberFromID = kvpMembers.Value;
                                return;
                            }
                        }
                    });
                    if(memberFromID.ID != mainClientReference.Me.ID)
                    {
                        ignoredUsersListBox.Items.Add($"{memberFromID.Username} ({memberFromID.ID})");
                    }
                    else
                    {
                        ignoredUsersListBox.Items.Add($"Unknown user ({x})");
                    }
                });
            }
            else
            {
                ignoredUsersListBox.Items.Add("None! You sure are patient.");
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (darkThemeCheckbox.IsChecked != App.ClientConfiguration.Settings.DarkTheme)
            {
                MessageBox.Show("Dissonance needs to restart to apply these settings.");
                NeedsRestart = true;
            }

            if(win10Notifications.IsChecked != App.ClientConfiguration.Settings.UseWindows10Notifications)
            {
                if(!NeedsRestart)
                {
                    MessageBox.Show("Dissonance needs to restart to apply these settings.");
                    NeedsRestart = true;
                }
            }

            //save settings
            App.ClientConfiguration.Settings.DarkTheme = ((bool)darkThemeCheckbox.IsChecked); //yay
            App.ClientConfiguration.Settings.UseWindows10Notifications = ((bool)win10Notifications.IsChecked);
            //

            //Close
            Close();
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            System.IO.File.Delete("token_cache");
            NeedsRestart = true;
            Close();
        }
    }
}
