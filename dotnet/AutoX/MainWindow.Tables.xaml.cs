﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using AutoX.Basic.Model;
using IDataObject = AutoX.Basic.Model.IDataObject;
using AutoX.Comm;

#endregion

namespace AutoX
{
    public partial class MainWindow
    {
        private readonly TableItem _clientSource = new TableItem();
        private readonly TableItem _instanceSource = new TableItem();
        private readonly TableItem _testCaseResultSource = new TableItem();
        private readonly TableItem _testStepSource = new TableItem();
        private readonly TableItem _translationSource = new TableItem();


        private void RefreshClientTable(object sender, RoutedEventArgs e)
        {
            var ret = Communication.GetInstance().GetComputersInfo();
            _clientSource.Clear();
            foreach (XElement computer in XElement.Parse(ret).Descendants())
            {
                _clientSource.Add(Computer.FromXElement(computer));
            }
            ClientTable.ItemsSource = _clientSource.Get();
        }

        private void ClientTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = filterClient.Text;
            ClientTable.ItemsSource = _clientSource.GetMatched(filter);
            e.Handled = true;
        }

        private void InstanceTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = filterInstance.Text;
            InstanceTable.ItemsSource = _instanceSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TestCaseTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = filterTestCaseResult.Text;
            TestCaseResultTable.ItemsSource = _testCaseResultSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TestStepTableEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = filterTestCaseResult.Text;
            TestCaseResultTable.ItemsSource = _testCaseResultSource.GetMatched(filter);
            e.Handled = true;
        }

        private void TranslationEnterFilter(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var filter = filterTranslation.Text;
            TranslationTable.ItemsSource = _translationSource.GetMatched(filter);
            e.Handled = true;
        }

        private void DoubleClickOnTable(object sender, MouseButtonEventArgs e)
        {
            var table = sender as DataGrid;
            if (table == null) return;
            var selected = table.SelectedItem;
            if (selected == null) return;
            var element = ((IDataObject) selected).GetXElementFromObject();
            var source = table.ItemsSource as Collection<object>;
            if (source == null) return;
            var index = source.IndexOf(selected);
            var dialog = new XElementDialog(element, false);
            dialog.ShowDialog();
            if (dialog.DialogResult != true) return;
            var updated = dialog.GetElement().GetObjectFromXElement();
            source[index] = updated;
            table.ItemsSource = source;
        }

        private void TestCaseResultTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestCaseResultTable == null) return;
            var selected = TestCaseResultTable.SelectedItem as Result;
            if (selected == null) return;

            var sRoot = Communication.GetInstance().GetChildren(selected._id);
            var xRoot = XElement.Parse(sRoot);
            if (xRoot.HasElements)
            {
                _testStepSource.Clear();
                foreach (XElement kid in xRoot.Descendants())
                {
                    var testcaseresult = kid.GetDataObjectFromXElement() as Result;
                    _testStepSource.Add(testcaseresult);
                }
            }
        }
    }
}