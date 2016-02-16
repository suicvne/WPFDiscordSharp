﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;

namespace CustomDiscordClient.Internal
{
    public class CustomWindow : Window
    {
        public event EventHandler<EventArgs> SettingsGearClicked;

        #region Click Events
        protected void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        protected void RestoreClick(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
            Button restoreButton = GetTemplateChild("restoreButton") as Button;
            if (WindowState == WindowState.Maximized)
                restoreButton.Content = "2";
            else
                restoreButton.Content = "1";
        }
        protected void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        public static readonly DependencyProperty ShowSettingsButtonProperty = DependencyProperty.Register("ShowSettingsButton", typeof(Visibility), typeof(CustomWindow));
        public Visibility ShowSettingsButton
        {
            get
            {
                return (Visibility)GetValue(ShowSettingsButtonProperty);
            }
            set
            {
                SetValue(ShowSettingsButtonProperty, value);
            }
        }

        public static readonly DependencyProperty EnableMaximizeButtonProperty =
            DependencyProperty.Register("EnableMaximizeButton", typeof(bool), typeof(CustomWindow));
        public bool EnableMaximizeButton
        {
            get
            {
                return (bool)GetValue(EnableMaximizeButtonProperty);
            }
            set
            {
                SetValue(EnableMaximizeButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowMaximizeButtonProperty =
            DependencyProperty.Register("ShowMaximizeButton", typeof(Visibility), typeof(CustomWindow));
        public Visibility ShowMaximizeButton
        {
            get
            {
                return (Visibility)GetValue(ShowMaximizeButtonProperty);
            }
            set
            {
                SetValue(ShowMaximizeButtonProperty, value);
            }
        }

        public static readonly DependencyProperty AllowResize = DependencyProperty.Register("AllowResizing", typeof(bool), typeof(CustomWindow));
        public bool AllowResizing { get { return (bool)GetValue(AllowResize); } set { SetValue(AllowResize, value); } }
        

        static CustomWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomWindow),
                new FrameworkPropertyMetadata(typeof(CustomWindow)));
        }

        public CustomWindow() : base()
        {
            PreviewMouseMove += OnPreviewMouseMove;
        }

        protected void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                Cursor = Cursors.Arrow;
        } 

        public Style GetWindowButtonStyle()
        {
            return Resources["WindowButtonStyle"] as Style;
        }

        public override void OnApplyTemplate()
        {
            Button minimizeButton = GetTemplateChild("minimizeButton") as Button;
            if (minimizeButton != null)
                minimizeButton.Click += MinimizeClick;

            Button restoreButton = GetTemplateChild("restoreButton") as Button;
            if (restoreButton != null)
                restoreButton.Click += RestoreClick;

            Button closeButton = GetTemplateChild("closeButton") as Button;
            if (closeButton != null)
                closeButton.Click += CloseClick;

            Button settingsButton = GetTemplateChild("settingsButton") as Button;
            if (settingsButton != null)
            {
                settingsButton.Click += (s, e) =>
                {
                    if (SettingsGearClicked != null)
                        SettingsGearClicked(this, new EventArgs());
                };
            }
            settingsButton.Visibility = ShowSettingsButton;


            Rectangle moveRectangle = GetTemplateChild("moveRectangle") as Rectangle;
            moveRectangle.PreviewMouseDown += MoveRectangle_PreviewMouseDown;

            Grid resizeGrid = GetTemplateChild("resizeGrid") as Grid;
            if (resizeGrid != null)
            {
                foreach (UIElement element in resizeGrid.Children)
                {
                    Rectangle resizeRectangle = element as Rectangle;
                    if (resizeRectangle != null)
                    {
                        resizeRectangle.PreviewMouseDown += ResizeRectangle_PreviewMouseDown;
                        resizeRectangle.MouseMove += ResizeRectangle_MouseMove;
                    }
                }
            }

            base.OnApplyTemplate();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wParam, IntPtr lParam);

        protected void ResizeRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AllowResizing)
            {
                Rectangle rectangle = sender as Rectangle;
                switch (rectangle.Name)
                {
                    case "top":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Top);
                        break;
                    case "bottom":
                        Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Bottom);
                        break;
                    case "left":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Left);
                        break;
                    case "right":
                        Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Right);
                        break;
                    case "topLeft":
                        Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.TopLeft);
                        break;
                    case "topRight":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.TopRight);
                        break;
                    case "bottomLeft":
                        Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.BottomLeft);
                        break;
                    case "bottomRight":
                        Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.BottomRight);
                        break;
                    default:
                        break;
                }
            }
        }

        private HwndSource _hwndSource;

        protected override void OnInitialized(EventArgs e)
        {
            SourceInitialized += OnSourceInitialized;
            base.OnInitialized(e);
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            SendMessage(_hwndSource.Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        private void ResizeRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (AllowResizing)
            {
                Rectangle rectangle = sender as Rectangle;
                switch (rectangle.Name)
                {
                    case "top":
                        Cursor = Cursors.SizeNS;
                        break;
                    case "bottom":
                        Cursor = Cursors.SizeNS;
                        break;
                    case "left":
                        Cursor = Cursors.SizeWE;
                        break;
                    case "right":
                        Cursor = Cursors.SizeWE;
                        break;
                    case "topLeft":
                        Cursor = Cursors.SizeNWSE;
                        break;
                    case "topRight":
                        Cursor = Cursors.SizeNESW;
                        break;
                    case "bottomLeft":
                        Cursor = Cursors.SizeNESW;
                        break;
                    case "bottomRight":
                        Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        break;
                }
            }
        }
        
        private void MoveRectangle_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
