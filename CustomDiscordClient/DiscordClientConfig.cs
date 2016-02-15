using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CustomDiscordClient
{
    //TODO: everything
    internal class DiscordClientConfig
    {
        /// <summary>
        /// #36393E
        /// #282b30
        /// </summary>

        public static bool DarkTheme = true;

        public static Color ColorDarkBackground = new Color { R = 16, G = 17, B = 19, A = 255 };
        public static Color ColorDarkForeground = new Color { R = 207, G = 207, B = 207, A = 255 };

        public static SolidColorBrush DarkThemeForeground = new SolidColorBrush(ColorDarkForeground);
        public static SolidColorBrush DarkThemeBackground = new SolidColorBrush(ColorDarkBackground);

        public static Uri DefaultAvatarBlue = new Uri("https://discordapp.com/assets/b3afd12bc47a87507780ce5f53a9d6a1.png");
    }
}
