using CustomDiscordClient.Internal;
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
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : CustomWindow
    {
        public LoginForm()
        {
            InitializeComponent();
            SetupTheme();
        }

        private void SetupTheme()
        {
            if(App.ClientConfiguration.Settings.DarkTheme)
            {
                this.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                this.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                label.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                label1.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;

                emailTextBox.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                emailTextBox.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                emailTextBox.BorderThickness = new Thickness(0, 0, 0, 1);

                passwordTextBox.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                passwordTextBox.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
                passwordTextBox.BorderThickness = new Thickness(0, 0, 0, 1);

                loginButton.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                loginButton.Background = App.ClientConfiguration.Settings.DarkThemeBackground;

                cancelButton.Foreground = App.ClientConfiguration.Settings.DarkThemeForeground;
                cancelButton.Background = App.ClientConfiguration.Settings.DarkThemeBackground;
            }
        }

        private void SetMainControls(bool enabled)
        {
            emailTextBox.IsEnabled = enabled;
            passwordTextBox.IsEnabled = enabled;
            loginButton.IsEnabled = enabled;
            cancelButton.IsEnabled = enabled;
        }

        private async void loginButton_Click(object sender, RoutedEventArgs e)
        {
            if(!String.IsNullOrEmpty(emailTextBox.Text) && !String.IsNullOrEmpty(passwordTextBox.Password))
            {
                SetMainControls(false);
                Mouse.OverrideCursor = Cursors.Wait;

                bool loggedIn = await doLoginStuff(emailTextBox.Text, passwordTextBox.Password);

                if (loggedIn)
                {
                    MessageBox.Show("Logged in successfully!");
                    MainWindow window = new MainWindow();
                    window.Show();
                    this.Close();
                }
                else
                    MessageBox.Show("Couldn't login!");

                SetMainControls(true);
                Mouse.OverrideCursor = null;
            }
            else
            {
                MessageBox.Show("Please enter your username and password for Discord.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> doLoginStuff(string user, string pass)
        {
#if DEBUG
            return true;
#else
            bool returnval = false;

            await Task.Run(() =>
            {
                DiscordClient tempClient = new DiscordClient();
                tempClient.ClientPrivateInformation = new DiscordUserInformation { Email = user, Password = pass };
                if (tempClient.SendLoginRequest() != null)
                {
                    returnval = true;
                }
                else
                    returnval = false;
            });

            return returnval;
#endif
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void passwordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                loginButton_Click(this, new RoutedEventArgs());
            }
        }
    }
}
