﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using AutoX.Basic;
using AutoX.Client.Core;
using Microsoft.Win32;

#endregion

namespace AutoX.Client
{
    /// <summary>
    ///   Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private WindowState _lastWindowState;
        private volatile bool _registered;
        private volatile bool _runFlag;
        private bool _shouldClose;
        private ClientInstance _defaultClientInstance = new ClientInstance();

        public MainWindow()
        {
            InitializeComponent();
            Hide();
            //Task.Factory.StartNew(DoWhileWork);
            //Dispatcher.Invoke(new Action(DoWhileWork));
        }

        private void DoWhileWork()
        {
            while (true)
            {
                if (_runFlag)
                {
                    if (!_registered)
                    {
                        Register();
                    }
                    RequestCommand();
                }
                else
                {
                    Thread.Sleep(11*1000);
                }
            }
        }

        private void MenuItemExit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SwitchBrowser(object sender, RoutedEventArgs e)
        {
            Browser.GetInstance().SwitchToAnotherBrowser();
        }

        private void GetUIObjects(object sender, RoutedEventArgs e)
        {
            SetPanel(Browser.GetInstance().GetAllValuableObjects());
        }

        private void Register(object sender, RoutedEventArgs e)
        {
            Register();
        }

        private void Register()
        {
            _registered = _defaultClientInstance.Register();
           
        }

        private void RequestCommand(object sender, RoutedEventArgs e)
        {
            RequestCommand();
        }

        private void RequestCommand()
        {
            var result = _defaultClientInstance.RequestCommand();

            SetPanel(result.ToString());
        }

        private void DoActions(object sender, RoutedEventArgs e)
        {
            //read the selected text in logpanel, turn it to xelement
            var content = ReadSelectedOrWholeText();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("No selected text.");
                return;
            }
            var steps = XElement.Parse(content);
            var result = ActionsFactory.Execute(steps);

            SetPanel(result.ToString());
        }

        private void SetPanel(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => LogPanel.Text += text));
        }

        private void SendResult(object sender, RoutedEventArgs e)
        {
            //read the selected text in logpanel, turn it to xelement
            var content = ReadSelectedOrWholeText();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("No selected text.");
                return;
            }
            var steps = XElement.Parse(content);
            var ret = _defaultClientInstance.SendResult(steps);
            Log.Debug(ret);
            SetPanel(ret);
        }

        private void OpenTestFile(object sender, RoutedEventArgs e)
        {
            var openFile = new OpenFileDialog();
            if (openFile.ShowDialog().Value)
            {
                var fileName = openFile.FileName;
                var content = File.ReadAllText(fileName);
                SetPanel(content);
            }
        }

        private void SaveSelectedToFile(object sender, RoutedEventArgs e)
        {
            var openFile = new SaveFileDialog();
            if (openFile.ShowDialog().Value)
            {
                var fileName = openFile.FileName;
                var content = ReadSelectedOrWholeText();
                //File.CreateText(fileName);
                //File.AppendAllText(fileName, content);
                File.WriteAllText(fileName, content);
            }
        }

        private string ReadSelectedOrWholeText()
        {
            var content = LogPanel.SelectedText;
            if (string.IsNullOrWhiteSpace(content))
                content = LogPanel.Text;
            return content;
        }

        #region UI things

        protected override void OnStateChanged(EventArgs e)
        {
            _lastWindowState = WindowState;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_shouldClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void OnNotificationAreaIconDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Open();
            }
        }

        private void OnMenuItemOpenClick(object sender, EventArgs e)
        {
            Open();
        }

        private void Open()
        {
            Show();
            WindowState = _lastWindowState;
        }

        private void OnMenuItemExitClick(object sender, EventArgs e)
        {
            _shouldClose = true;
            Close();
        }

        private void NotificationAreaIconMouseClick(object sender, MouseButtonEventArgs e)
        {
            //TODO have not decided what to do on it
            //MessageBox.Show(_lastMessage);
        }

        private void OnMenuItemStartClick(object sender, EventArgs e)
        {
            //if (_commadThread != null)
            //    _commadThread.Abort();

            //_threadState = ThreadState.Running;
            //_commadThread = new Thread(CommandReader);
            //// add this line, or the WatiN would show Error message!
            //_commadThread.SetApartmentState(ApartmentState.STA);
            //_commadThread.Start();
            _runFlag = true;
            _registered = false;
        }

        private void OnMenuItemStopClick(object sender, EventArgs e)
        {
            //if (_commadThread != null)
            //    _threadState = ThreadState.Stopped;
            _runFlag = false;
            _registered = false;
        }

        private void OnMenuItemRegisterClick(object sender, EventArgs e)
        {
            var ret = _defaultClientInstance.Register();
            Log.Debug(ret.ToString());
        }

        #endregion UI things
    }
}