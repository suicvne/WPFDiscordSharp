using CustomDiscordClient.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : CustomWindow
    {
        public TestWindow()
        {
            InitializeComponent();
            Thread.Sleep(2000);
            DwmDropShadow.DropShadowToWindow(this);
        }
    }
}
