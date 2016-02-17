using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CustomDiscordClient
{
    public class LocalClientSettings
    {
        [JsonProperty("win10_notifications")]
        public bool UseWindows10Notifications { get; internal set; } = false;

        [JsonProperty("enable_dark")]
        public bool DarkTheme { get; internal set; } = false;

        [JsonProperty("custom_background_color")]
        private Color _ColorDarkBackground = new Color { R = 16, G = 17, B = 19, A = 255 };
        public Color ColorDarkBackground
        {
            get
            {
                return _ColorDarkBackground;
            }
            internal set
            {
                _ColorDarkBackground = value;
                DarkThemeBackground = new SolidColorBrush(ColorDarkBackground);
            }
        }

        [JsonProperty("custom_foreground_color")]
        private Color _ColorDarkForeground = new Color { R = 207, G = 207, B = 207, A = 255 };
        public Color ColorDarkForeground
        {
            get
            {
                return _ColorDarkForeground;
            }
            internal set
            {
                _ColorDarkForeground = value;
                DarkThemeForeground = new SolidColorBrush(ColorDarkForeground);
            }
        }

        public SolidColorBrush DarkThemeForeground;
        public SolidColorBrush DarkThemeBackground;

        public LocalClientSettings()
        {
            DarkThemeForeground = new SolidColorBrush(ColorDarkForeground);
            DarkThemeBackground = new SolidColorBrush(ColorDarkBackground);
        }
    }
    //TODO: everything
    public class DiscordClientConfig
    {
        public LocalClientSettings Settings;
        public static Uri DefaultAvatarBlue = new Uri("https://discordapp.com/assets/b3afd12bc47a87507780ce5f53a9d6a1.png");

        public DiscordClientConfig(bool serialize = false)
        {
            if (serialize)
            {
                Settings = JsonConvert.DeserializeObject<LocalClientSettings>(File.ReadAllText("settings.json"));
            }
            else
                Settings = new LocalClientSettings();
        }
    }
}
