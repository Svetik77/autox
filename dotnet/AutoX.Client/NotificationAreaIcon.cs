﻿#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

#endregion

namespace AutoX.Client
{
    /// <summary>
    ///   Represents a thin wrapper for <see cref="System.Windows.Forms.NotifyIcon" />
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly"), ContentProperty("Text")]
    [DefaultEvent("MouseDoubleClick")]
    public class NotificationAreaIcon : FrameworkElement, IDisposable
    {
        public static readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent(
            "MouseClick", RoutingStrategy.Bubble, typeof (MouseButtonEventHandler), typeof (NotificationAreaIcon));

        public static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
            "MouseDoubleClick", RoutingStrategy.Bubble, typeof (MouseButtonEventHandler), typeof (NotificationAreaIcon));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof (ImageSource), typeof (NotificationAreaIcon));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof (string), typeof (NotificationAreaIcon));

        public static readonly DependencyProperty FormsContextMenuProperty =
            DependencyProperty.Register("MenuItems", typeof (List<MenuItem>), typeof (NotificationAreaIcon),
                new PropertyMetadata(new List<MenuItem>()));

        private NotifyIcon _notifyIcon;

        public List<MenuItem> MenuItems
        {
            get { return (List<MenuItem>) GetValue(FormsContextMenuProperty); }
            set { SetValue(FormsContextMenuProperty, value); }
        }

        public ImageSource Icon
        {
            get { return (ImageSource) GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #region IDisposable Members

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
        public void Dispose()
        {
            _notifyIcon.Dispose();
        }

        #endregion

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Create and initialize the window forms notify icon based
            _notifyIcon = new NotifyIcon {Text = Text};
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _notifyIcon.Icon = FromImageSource(Icon);
            }
            _notifyIcon.Visible = FromVisibility(Visibility);

            if (MenuItems != null && MenuItems.Count > 0)
            {
                _notifyIcon.ContextMenu = new ContextMenu(MenuItems.ToArray());
            }

            _notifyIcon.MouseDown += OnMouseDown;
            _notifyIcon.MouseUp += OnMouseUp;
            _notifyIcon.MouseClick += OnMouseClick;
            _notifyIcon.MouseDoubleClick += OnMouseDoubleClick;

            Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
        }

        private void OnDispatcherShutdownStarted(object sender, EventArgs e)
        {
            _notifyIcon.Dispose();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            OnRaiseEvent(MouseDownEvent, new MouseButtonEventArgs(
                InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            OnRaiseEvent(MouseUpEvent, new MouseButtonEventArgs(
                InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            OnRaiseEvent(MouseDoubleClickEvent, new MouseButtonEventArgs(
                InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            OnRaiseEvent(MouseClickEvent, new MouseButtonEventArgs(
                InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(e.Button)));
        }

        private void OnRaiseEvent(RoutedEvent handler, RoutedEventArgs e)
        {
            e.RoutedEvent = handler;
            RaiseEvent(e);
        }

        public event MouseButtonEventHandler MouseClick
        {
            add { AddHandler(MouseClickEvent, value); }
            remove { RemoveHandler(MouseClickEvent, value); }
        }

        public event MouseButtonEventHandler MouseDoubleClick
        {
            add { AddHandler(MouseDoubleClickEvent, value); }
            remove { RemoveHandler(MouseDoubleClickEvent, value); }
        }

        #region Conversion members

        private static Icon FromImageSource(ImageSource icon)
        {
            if (icon == null)
            {
                return null;
            }
            var iconUri = new Uri(icon.ToString());

            return new Icon("." + iconUri.LocalPath);
        }

        private static bool FromVisibility(Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }

        private static MouseButton ToMouseButton(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left:
                    return MouseButton.Left;
                case MouseButtons.Right:
                    return MouseButton.Right;
                case MouseButtons.Middle:
                    return MouseButton.Middle;
                case MouseButtons.XButton1:
                    return MouseButton.XButton1;
                case MouseButtons.XButton2:
                    return MouseButton.XButton2;
            }
            throw new InvalidOperationException();
        }

        #endregion Conversion members
    }
}