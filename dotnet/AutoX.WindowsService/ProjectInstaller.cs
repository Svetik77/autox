﻿using System.ComponentModel;
using System.Configuration.Install;

namespace AutoX.WindowsService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void AutoXServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
