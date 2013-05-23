﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using AutoX.Basic;
using AutoX.Basic.Model;
using AutoX.Comm;
using AutoX.DB;
using Microsoft.Win32;
using AutoX.WF.Core;
using AutoX.Client.Core;
using System.Threading;

#endregion

namespace AutoX
{
    public partial class MainWindow
    {
        private readonly XElement _xValidation = XElement.Parse(@"
<Validation>
	<Rule Action='CreateFolder' Type='Folder' Object='Project' />
	<Rule Action='CreateSuite' Type='Folder' Object='Project' />
	<Rule Action='CreateCase' Type='Folder' Object='Project' />
	<Rule Action='CreateScreen' Type='Folder' Object='Project' />
	<Rule Action='CreateFolder' Type='Folder' Object='Data' />
	<Rule Action='CreateFolder' Type='Folder' Object='UI' />
    <Rule Action='CreateData' Type='Folder' Object='Data' />
    <Rule Action='CreateData' Type='Folder' Object='Folder' />
</Validation>"
            );

        private void BrowserSetting(object sender, RoutedEventArgs e)
        {

            PopupBrowsersDialogSetDefaultConfig();
        }

        private void PopupBrowsersDialogSetDefaultConfig()
        {
            var browsersDialog = new BrowsersDialog();
            browsersDialog.ShowDialog();
            if (browsersDialog.DialogResult == true)
            {
                _config.Set("AUTVersion", browsersDialog.BrowserSetting.GetAttributeValue("AUTVersion"));
                _config.Set("AUTBuild", browsersDialog.BrowserSetting.GetAttributeValue("AUTBuild"));
                _config.Set("BrowserType", browsersDialog.BrowserSetting.Name.ToString());
                _config.Set("BrowserPlatform", browsersDialog.BrowserSetting.GetAttributeValue("Platform"));
                _config.Set("BrowserVersion", browsersDialog.BrowserSetting.GetAttributeValue("Version"));
            }
        }

        AutoClient _autoClient = new AutoClient();
        private void StartBrowser(object sender, RoutedEventArgs e)
        {
            _autoClient.Browser.StartBrowser();
        }   

        private void GetUIObjectsSaveToFile(object sender, RoutedEventArgs e)
        {
            var uiObjectsString = _autoClient.Browser.GetAllValuableObjects();
            var fileDialog = new SaveFileDialog();
            fileDialog.FileName = "UI";
            fileDialog.DefaultExt = "xml";
            fileDialog.Filter = "XML format (*.xml)|*.xml";
            if (fileDialog.ShowDialog().Value)
            {
                var fileName = fileDialog.FileName;
                File.WriteAllText(fileName, uiObjectsString);
            }
        }

        private void CloseBrowser(object sender, RoutedEventArgs e)
        {
            _autoClient.Browser.CloseBrowser();
        }  

        private void RunSauceTest(object sender, RoutedEventArgs e)
        {
            //get workflowid from project tree
            var selected = ProjectTreeView.SelectedItem as TreeViewItem;
            if (selected == null)
            {
                MessageBox.Show("Please select a test suite from the project tree!");
                return;
            }
            var selectedItem = selected.DataContext as XElement;
            if (selectedItem == null) return;
            var workflowId = selectedItem.GetAttributeValue(Constants._ID);
            var type = selectedItem.GetAttributeValue(Constants._TYPE);
            var scriptType = selectedItem.GetAttributeValue(Constants.SCRIPT_TYPE);
            if (!type.Equals(Constants.SCRIPT) || !scriptType.Equals("TestSuite"))
            {
                MessageBox.Show("Selected Item MUST be a Test Script!");
                return;
            }
            //popup a dialog to get the browser os version
            PopupBrowsersDialogSetDefaultConfig();
            _config.Set("HostType", "Sauce");
            _autoClient = new AutoClient(_config);
            RunWorkflowById(workflowId);
            //when finished, show a message
            MessageBox.Show("Your Test finished.");
        }
        private void RunTest(object sender, RoutedEventArgs e)
        {
            //get workflowid from project tree
            var selected = ProjectTreeView.SelectedItem as TreeViewItem;
            if (selected == null)
            {
                MessageBox.Show("Please select a test suite from the project tree!");
                return;
            }
            var selectedItem = selected.DataContext as XElement;
            if (selectedItem == null) return;
            var workflowId = selectedItem.GetAttributeValue(Constants._ID);
            var type = selectedItem.GetAttributeValue(Constants._TYPE);
            var scriptType = selectedItem.GetAttributeValue(Constants.SCRIPT_TYPE);
            if (!type.Equals(Constants.SCRIPT)||!scriptType.Equals("TestSuite"))
            {
                MessageBox.Show("Selected Item MUST be a Test Script!");
                return;
            }
            /**********This is a simple instance***********/
            _autoClient.Config.Set("HostType", "Local");
            RunWorkflowById(workflowId);
            /***********end of instance*******************/
            //when finished, show a message
            MessageBox.Show("Your Test finished.");
            
        }

        private void RunWorkflowById(string workflowId)
        {
            
            var workflowInstance = new WorkflowInstance(Guid.NewGuid().ToString(),workflowId, _config.GetList());
            workflowInstance.ClientId = _config.Get("_id", Guid.NewGuid().ToString());
            ClientInstancesManager.GetInstance().Register(_config.SetRegisterBody(XElement.Parse("<Register />")));
            workflowInstance.Start();
            bool debugMode = _config.Get("ModeDebug", "True").Equals("True", StringComparison.CurrentCultureIgnoreCase);
            while (true)
            {
                var xCommand = workflowInstance.GetCommand();
                if (debugMode)
                    MessageBox.Show(xCommand.ToString());
                Log.Info(xCommand.ToString());

                var xResult = _autoClient.Execute(xCommand);
                if (debugMode)
                    MessageBox.Show(xResult.ToString());
                Log.Info(xResult.ToString());
                workflowInstance.SetResult(xResult);
                Thread.Sleep(1000);

                if (workflowInstance.IsFinished())
                    break;
            }
            workflowInstance = null;
        }

        private void GenerateKeyFile(object sender, RoutedEventArgs e)
        {
            
            if (AsymmetricEncryption.GenerateRegisterFile())
            {
                MessageBox.Show(
                    "Create Register File Successfully! \nCopy and send the *.pem file to the Service Provider!",
                    "Congratulation!");
            }
            else
            {
                MessageBox.Show("Please add your user name to the config file, remove entry of \"PublicKey\"",
                                "Failed, Check and Do Again");
            }
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            // generate a XElement from configuration
            var config = new XElement("Configuration");
            Configuration.AddSettingsToXElement(config);
            //show XElementDialog
            var xElementDialog = new XElementDialog(config, false);
            xElementDialog.ShowDialog();
            //if click yes
            if (!xElementDialog.DialogResult.HasValue || !xElementDialog.DialogResult.Value) return;
            //set settings back to configuration, to memory and disk
            foreach (XAttribute attr in config.Attributes())
            {
                var name = attr.Name.ToString();
                var value = attr.Value;
                Configuration.Set(name, value);
            }
            Configuration.SaveSettings();
        }

        private void CreateSuite(object sender, RoutedEventArgs e)
        {
            if (!BeforeActionCheck(ProjectTreeView, "CreateSuite", "Project"))
            {
                MessageBox.Show("Not Valid Operation");
                return;
            }
            var treeViewItem = GetInitScriptXElement("Suite");
            if (treeViewItem != null)
                AddTestDesigner(treeViewItem);
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateCase(object sender, RoutedEventArgs e)
        {
            if (!BeforeActionCheck(ProjectTreeView, "CreateCase", "Project"))
            {
                MessageBox.Show("Not Valid Operation");
                return;
            }
            var treeViewItem = GetInitScriptXElement("Case");
            if (treeViewItem != null)
                AddTestDesigner(treeViewItem);
        }

        private void CreateScreen(object sender, RoutedEventArgs e)
        {
            if (!BeforeActionCheck(ProjectTreeView, "CreateScreen", "Project"))
            {
                MessageBox.Show("Not Valid Operation");
                return;
            }
            var treeViewItem = GetInitScriptXElement("Screen");
            if (treeViewItem != null)
                AddTestDesigner(treeViewItem);
        }

        private void CreateFolderOnDataTree(object sender, RoutedEventArgs e)
        {
            CreateFolderOnTree(DataTree, Constants.DATA);
        }

        private void CreateFolderOnUITree(object sender, RoutedEventArgs e)
        {
            CreateFolderOnTree(GuiObjectTree, "UI");
        }

        private void CreateFolderOnProjectTree(object sender, RoutedEventArgs e)
        {
            CreateFolderOnTree(ProjectTreeView, "Project");
        }

        private void CreateFolderOnTree(TreeView tree, string treeName)
        {
            if (!BeforeActionCheck(tree, "CreateFolder", treeName))
            {
                MessageBox.Show("Not Valid Operation");
                return;
            }
            var xElement =
                XElement.Parse(
                    @"<Folder Name='NewFolder' Type='Folder' Description='Please add description here' _id='" +
                    Guid.NewGuid() + "' />");

            AddNewItemToTree(tree, xElement);
        }


        private void EditOnProjectTree(object sender, RoutedEventArgs e)
        {
            var selected = (TreeViewItem) ProjectTreeView.SelectedItem;
            Edit(selected);
        }

        private void EditOnUITree(object sender, RoutedEventArgs e)
        {
            var selected = (TreeViewItem) GuiObjectTree.SelectedItem;
            Edit(selected);
        }

        private void EditOnDataTree(object sender, RoutedEventArgs e)
        {
            var selected = (TreeViewItem) DataTree.SelectedItem;
            Edit(selected);
        }

        private void EditOnResultTree(object sender, RoutedEventArgs e)
        {
            var selected = TestResultTree.SelectedItem as TreeViewItem;
            Edit(selected);
        }

        private void ReloadOnProjectTree(object sender, RoutedEventArgs e)
        {
            var projectId = Configuration.Settings("ProjectRoot", "0010010000001");
            InitTree(ProjectTreeView, projectId);
        }

        private void ReloadOnSuiteTree(object sender, RoutedEventArgs e)
        {
            var projectId = Configuration.Settings("ProjectRoot", "0010010000001");
            InitTree(SuiteTree, projectId);
        }


        private void ReloadOnResultTree(object sender, RoutedEventArgs e)
        {
            var resultId = Configuration.Settings("ResultsRoot", "0020020000002");
            InitTree(TestResultTree, resultId);
        }


        private void ReloadOnUITree(object sender, RoutedEventArgs e)
        {
            var uiId = Configuration.Settings("ObjectPool", "0040040000004");
            InitTree(GuiObjectTree, uiId);
        }

        private void ReloadOnDataTree(object sender, RoutedEventArgs e)
        {
            var dataId = Configuration.Settings("DataRoot", "0030030000003");
            InitTree(DataTree, dataId);
        }

        //private void ReloadOnResultTree(object sender, RoutedEventArgs e)
        //{
        //    string resultId = Configuration.Settings("ResultsRoot", "0020020000002");

        //}

        private void DeleteOnProjectTree(object sender, RoutedEventArgs e)
        {
            Delete(ProjectTreeView);
        }

        private void DeleteOnUITree(object sender, RoutedEventArgs e)
        {
            Delete(GuiObjectTree);
        }

        private void DeleteOnDataTree(object sender, RoutedEventArgs e)
        {
            Delete(DataTree);
        }

        private void DeleteOnResultTree(object sender, RoutedEventArgs e)
        {
            Delete(TestResultTree);
        }

        private void DoubleClickOnProjectTree(object sender, MouseButtonEventArgs e)
        {
            DoubleClickOnTree(ProjectTreeView);
        }

        private void DoubleClickOnUITree(object sender, MouseButtonEventArgs e)
        {
            DoubleClickOnTree(GuiObjectTree);
        }

        private void DoubleClickOnDataTree(object sender, MouseButtonEventArgs e)
        {
            DoubleClickOnTree(DataTree);
        }

        private void DoubleClickOnSuiteTree(object sender, MouseButtonEventArgs e)
        {
            DoubleClickOnTree(SuiteTree);
        }

        private void DoubleClickOnResultTree(object sender, MouseButtonEventArgs e)
        {
            DoubleClickOnTree(TestResultTree);
        }


        private void AddNewData(object sender, RoutedEventArgs e)
        {
            var selected = DataTree.SelectedItem as TreeViewItem;
            if (selected == null)
                return;
            
            if (!BeforeActionCheck(DataTree, "CreateData", Constants.DATA))
            {
                MessageBox.Show("Not Valid Operation");
                return;
            }
            var xData = new XElement(Constants.DATUM);
            xData.SetAttributeValue(Constants.NAME, "New Data");
            xData.SetAttributeValue("Description", "New Data");
            xData.SetAttributeValue(Constants._TYPE, Constants.DATUM);
            xData.SetAttributeValue(Constants._ID, Guid.NewGuid().ToString());
            AddNewItemToTree(DataTree, xData);
        }

        private void AddNewPage(object sender, RoutedEventArgs e)
        {
            //open a dialog, load xml format file, add it to tree
            var selected = GuiObjectTree.SelectedItem as TreeViewItem;
            if (selected == null)
            {
                return;
            }
            var xParent = selected.DataContext as XElement;
            var dialog = new OpenFileDialog
                {
                    Filter = "Screen GUI File (.xml)|*.xml|All Files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };
            // Set filter options and filter index.

            var dialogResult = dialog.ShowDialog(this);
            if (dialogResult == true)
            {
                var content = File.ReadAllText(dialog.FileName);
                var page = XElement.Parse(content);
                var newItem = GetItemFromXElement(page, xParent.GetAttributeValue(Constants._ID));
                if (newItem != null) selected.Items.Add(newItem);
            }
        }

        private static void InitTree(ItemsControl tree, string rootId)
        {
            var xRoot = DBFactory.GetData().Read(rootId);
            if (xRoot==null)
            {
                MessageBox.Show("Get Tree Root Failed. id=" + rootId);
                return;
            }
            tree.Items.Clear();
            tree.Items.Add(xRoot.GetTreeViewItemFromXElement());
        }

        private void ProjectTreeEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DoFilterOnTree(ProjectTreeView, filterProject);
            e.Handled = true;
        }

        private void UIEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DoFilterOnTree(GuiObjectTree, filterUI);
            e.Handled = true;
        }

        private void DataEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DoFilterOnTree(DataTree, filterData);
            e.Handled = true;
        }

        private void SuiteTreeEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DoFilterOnTree(SuiteTree, filterMonitor);
            e.Handled = true;
        }

        private void ResultEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            DoFilterOnTree(TestResultTree, filterResult);
            e.Handled = true;
        }

        private static void DoFilterOnTree(ItemsControl tree, TextBox filterBox)
        {
            var filterString = filterBox.Text;

            var root = tree.Items.GetItemAt(0);
            (root as TreeViewItem).FilterTreeItem(filterString);
        }

        private void StartSuite(object sender, RoutedEventArgs e)
        {
            var selected = InstanceTable.SelectedItem as Instance;
            if (selected == null) return;
            try
            {
                var sRoot = Communication.GetInstance().StartInstance(selected._id);
                var xRoot = XElement.Parse(sRoot);
                var result = xRoot.GetAttributeValue(Constants.RESULT);
                if (string.IsNullOrEmpty(result)) return;
                if (result.Equals("Error"))
                {
                    MessageBox.Show("Start Instance failed!\nReason:" +
                                    xRoot.GetAttributeValue("Reason"));
                }
                else
                {
                    //update the table
                    RefreshSuite(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopSuite(object sender, RoutedEventArgs e)
        {
            var selected = InstanceTable.SelectedItem as Instance;
            if (selected == null) return;
            try
            {
                var sRoot = Communication.GetInstance().StopInstance(selected._id);
                var xRoot = XElement.Parse(sRoot);
                var result = xRoot.GetAttributeValue(Constants.RESULT);
                if (string.IsNullOrEmpty(result)) return;
                if (result.Equals("Error"))
                {
                    MessageBox.Show("Stop Instance failed!\nReason:" +
                                    xRoot.GetAttributeValue("Reason"));
                }
                else
                {
                    //update the table
                    var source = InstanceTable.ItemsSource as List<Instance>;
                    if (source != null)
                    {
                        var index = source.IndexOf(selected);
                        (source[index]).Status = "Stop";
                    }
                    InstanceTable.ItemsSource = source;
                    InstanceTable.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshSuite(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            try
            {
                var sRoot = Communication.GetInstance().GetInstancesInfo();
                Log.Debug("Instances:\n" + sRoot);
                var xRoot = XElement.Parse(sRoot);

                //update the table
                _instanceSource.Clear();
                //var instances = new List<Instance>();

                foreach (XElement descendant in xRoot.Descendants())
                {
                    descendant.Name = "AutoX.Basic.Model.Instance";
                    var instance = descendant.GetObjectFromXElement() as Instance;
                    if (instance != null) _instanceSource.Add(instance);
                }
                InstanceTable.ItemsSource = _instanceSource.Get();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSuite(object sender, RoutedEventArgs e)
        {
            var selected = InstanceTable.SelectedItem as Instance;
            if (selected == null) return;
            try
            {
                var sRoot = Communication.GetInstance().SetInstanceInfo(selected.GetXElementFromObject());
                var xRoot = XElement.Parse(sRoot);
                var result = xRoot.GetAttributeValue(Constants.RESULT);
                if (string.IsNullOrEmpty(result)) return;
                if (result.Equals("Error"))
                {
                    MessageBox.Show("Update Instance failed!\nReason:" +
                                    xRoot.GetAttributeValue("Reason"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSuite(object sender, RoutedEventArgs e)
        {
            var selected = InstanceTable.SelectedItem as Instance;
            if (selected == null) return;
            try
            {
                var sRoot = Communication.GetInstance().DeleteInstance(selected._id);
                var xRoot = XElement.Parse(sRoot);
                var result = xRoot.GetAttributeValue(Constants.RESULT);
                if (string.IsNullOrEmpty(result)) return;
                if (result.Equals("Error"))
                {
                    MessageBox.Show("Delete Instance failed!\nReason:" +
                                    xRoot.GetAttributeValue("Reason"));
                }
                else
                {
                    var source = InstanceTable.ItemsSource as List<Instance>;
                    if (source != null)
                    {
                        source.Remove(selected);
                        InstanceTable.Items.Clear();
                        InstanceTable.ItemsSource = source;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
