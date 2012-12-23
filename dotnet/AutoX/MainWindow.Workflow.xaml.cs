﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.Toolbox;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using AutoX.Activities;
using AutoX.Activities.AutoActivities;
using AutoX.Basic;
using AutoX.Comm;
using Image = System.Drawing.Image;

#endregion

namespace AutoX
{
    public partial class MainWindow
    {
        private readonly AttributeTableBuilder _builder = new AttributeTableBuilder();
        //private System.Drawing.Point _startPoint;
        private UndoEngine _undoEngineService;
        private WorkflowDesigner _workflowDesigner = new WorkflowDesigner();

        private void AddTestDesigner(TreeViewItem treeViewItem)
        {
            var xElement = treeViewItem.DataContext as XElement;
            string type = xElement.GetAttributeValue("ScriptType");
            string name = xElement.GetAttributeValue("Name");
            string guid = xElement.GetAttributeValue("GUID");
            string description = xElement.GetAttributeValue("Description");
            string content = xElement.GetAttributeValue("Content");

            Activity newActivity = null;

            if (string.IsNullOrEmpty(content))
            {
                if (type.Equals("TestSuite"))
                {
                    var ta = new TestSuiteActivity {Description = description, GUID = guid, Name = name};
                    ta.SetHost(this);
                    newActivity = ta;
                }

                if (type.Equals("TestCase"))
                {
                    var ta = new TestCaseActivity {Description = description, GUID = guid, Name = name};
                    ta.SetHost(this);
                    newActivity = ta;
                }

                if (type.Equals("TestScreen"))
                {
                    var ta = new TestScreenActivity {Description = description, GUID = guid, Name = name};
                    ta.SetHost(this);
                    newActivity = ta;
                }

                if (newActivity == null)
                {
                    var ta = new TestSuiteActivity {DisplayName = "new Test suite (by default)"};
                    ta.SetHost(this);
                    newActivity = ta;
                }

                AddDesigner(newActivity, treeViewItem);
                //Dispatcher.Invoke(() => AddDesigner(newDesigner, treeViewItem));
            }
            else
            {
                Activity activity = Utilities.GetActivityFromContentString(content);
                AddDesigner(activity, treeViewItem);
                //Dispatcher.Invoke(() => AddDesigner(activity, treeViewItem));
            }
        }

        protected void LoadToolBox()
        {
            var tbc = new ToolboxControl
                          {
                              CategoryItemStyle = new Style(typeof (TreeViewItem))
                                                      {
                                                          Setters = {new Setter(TreeViewItem.IsExpandedProperty, false)}
                                                      }
                          };

            Dispatcher.BeginInvoke(new Action(() => ToolboxBorder.Child = tbc));
            Dispatcher.BeginInvoke(new Action(() => LoadDefaultActivities(tbc)));
            //Load the dll contain TestSuite, it means the ActivityLib.dll loaded here
            //var customAss = typeof (TestSuiteActivity).Assembly;
            //const string categoryTitle = "AutoX Automation";
            //LoadCustomActivities(tbc, customAss, categoryTitle);

            //support the users create there own automation activities, just add the dll name to appsettings
            string ass = Configuration.Settings("Assemmblies", "");
            if (string.IsNullOrEmpty(ass)) return;
            var split = new[] {';'};
            string[] asses = ass.Split(split);
            foreach (string a in asses)
            {
                if (!string.IsNullOrWhiteSpace(a))
                {
                    //Dispatcher.BeginInvoke(
                    //    new Action(() => LoadCustomActivities(tbc, Assembly.LoadFrom(a), "Custom Activities")));
                    LoadCustomActivities(tbc, Assembly.LoadFrom(a), "Custom Activities");
                }
            }
        }

        private static void LoadCustomActivities(ToolboxControl tbc, Assembly customAss, string categoryTitle)
        {
            IEnumerable<Type> types = customAss.GetTypes().
                Where(t => (typeof (Activity).IsAssignableFrom(t) ||
                            typeof (IActivityTemplateFactory).IsAssignableFrom(t)) && !t.IsAbstract && t.IsPublic &&
                           !t.IsNested);
            var cat = new ToolboxCategory(categoryTitle);
            foreach (Type type in types.OrderBy(t => t.Name))
            {
                //var w = new ToolboxItemWrapper(type, ToGenericTypeString(type));
                if (type.Name.Equals("TestSuiteActivity")
                    || type.Name.Equals("TestCaseActivity")
                    || type.Name.Equals("TestScreenActivity"))
                    continue;
                var w = new ToolboxItemWrapper(type, ImageList.GetInstance().GetFileName(type.Name.ToLower()), type.Name);
                cat.Add(w);
            }

            tbc.Categories.Add(cat);
        }

        private void LoadDefaultActivities(ToolboxControl tbc)
        {
            var dict = new ResourceDictionary
                           {
                               Source =
                                   new Uri(
                                   "pack://application:,,,/System.Activities.Presentation;component/themes/icons.xaml")
                           };
            Resources.MergedDictionaries.Add(dict);

            IEnumerable<Type> standtypes = typeof (Activity).Assembly.GetTypes().
                Where(t => (typeof (Activity).IsAssignableFrom(t) ||
                            typeof (IActivityTemplateFactory).IsAssignableFrom(t)) && !t.IsAbstract && t.IsPublic &&
                           !t.IsNested && HasDefaultConstructor(t));

            var primary = new ToolboxCategory("Native Activities");

            foreach (Type type in standtypes.OrderBy(t => t.Name))
            {
                var w = new ToolboxItemWrapper(type, ToGenericTypeString(type));
                if (!AddIcon(type, _builder))
                {
                    primary.Add(w);
                }
                //else
                //{
                //    //secondary.Add(w);
                //}
            }

            MetadataStore.AddAttributeTable(_builder.CreateTable());
            tbc.Categories.Add(primary);
        }

        protected bool AddIcon(Type type, AttributeTableBuilder builder)
        {
            bool secondary = false;

            Type tbaType = typeof (ToolboxBitmapAttribute);
            Type imageType = typeof (Image);
            ConstructorInfo constructor = tbaType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                                 new[] {imageType, imageType}, null);

            string resourceKey = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
            int index = resourceKey.IndexOf('`');
            if (index > 0)
            {
                resourceKey = resourceKey.Remove(index);
            }
            if (resourceKey == "Flowchart")
            {
                resourceKey = "FlowChart"; // it appears that themes/icons.xaml has a typo here
            }
            resourceKey += "Icon";
            Bitmap small, large;
            object resource = TryFindResource(resourceKey);
            if (!(resource is DrawingBrush))
            {
                resource = FindResource("GenericLeafActivityIcon");
                secondary = true;
            }
            var dv = new DrawingVisual();
            using (DrawingContext context = dv.RenderOpen())
            {
                context.DrawRectangle(((DrawingBrush) resource), null, new Rect(0, 0, 32, 32));
                context.DrawRectangle(((DrawingBrush) resource), null, new Rect(32, 32, 16, 16));
            }
            var rtb = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                enc.Save(outStream);
                outStream.Position = 0;
                large = new Bitmap(outStream);
            }
            rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            dv.Offset = new Vector(-32, -32);
            rtb.Render(dv);
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                enc.Save(outStream);
                outStream.Position = 0;
                small = new Bitmap(outStream);
            }
            var tba = constructor.Invoke(new object[] {small, large}) as ToolboxBitmapAttribute;
            builder.AddCustomAttributes(type, tba);

            return secondary;
        }

        public static string ToGenericTypeString(Type t)
        {
            if (!t.IsGenericType)
                return t.Name;

            string genericTypeName = t.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            string genericArgs = string.Join(",", t.GetGenericArguments().Select(ToGenericTypeString));
            return genericTypeName + "<" + genericArgs + ">";
        }

        public static bool HasDefaultConstructor(Type t)
        {
            return t.GetConstructors().Any(c => c.GetParameters().Length <= 0);
        }


        private void UndoEngineServiceUndoUnitAdded(object sender, UndoUnitEventArgs e)
        {
            undoMenu.IsEnabled = true;
        }

        private static void RegisterMetadata()
        {
            var metaData = new DesignerMetadata();
            metaData.Register();
        }

        private void AddDesigner(Activity activity, TreeViewItem treeViewItem)
        {
            SetWorkflowDesignerView();

            SetWorkflowDesigner(activity, treeViewItem);
        }

        //private void AddDesigner(string fileName, TreeViewItem treeViewItem)
        //{
        //    SetWorkflowDesignerView();
        //    _workflowDesigner.Load(fileName);
        //    SetWorkflowDesigner(treeViewItem);
        //}

        private void SetWorkflowDesignerView()
        {
            //Create an instance of WorkflowDesigner class
            //if(_workflowDesigner==null)
            _workflowDesigner = new WorkflowDesigner();

            //Place the WorkflowDesigner in the middle column of the grid
            Grid.SetColumn(_workflowDesigner.View, 1);
            //Add the WorkflowDesigner to the grid
            DesignerBorder.Child = _workflowDesigner.View;
            //grid1.Children.Add(this.workflowDesigner.View);

            // Add the Property Inspector
            UIElement propertyView = _workflowDesigner.PropertyInspectorView;

            PropertyBorder.Child = propertyView;
        }

        private void SetWorkflowDesigner(Activity activity, TreeViewItem treeViewItem)
        {
            _workflowDesigner.Load(activity);
// Flush the workflow when the model changes
            _workflowDesigner.ModelChanged += (s, e) => WorkflowSourceChanged(treeViewItem);

            WorkflowSourceChanged(treeViewItem);
            // services
            _undoEngineService = _workflowDesigner.Context.Services.GetService<UndoEngine>();
            _undoEngineService.UndoUnitAdded += UndoEngineServiceUndoUnitAdded;

            var designerView = _workflowDesigner.Context.Services.GetService<DesignerView>();
            //hide the shell bar of designer
            //designerView.WorkflowShellBarItemVisibility = ShellBarItemVisibility.None;
            designerView.WorkflowShellBarItemVisibility = ShellBarItemVisibility.MiniMap
                                                          | ShellBarItemVisibility.Zoom |
                                                          ShellBarItemVisibility.Arguments |
                                                          ShellBarItemVisibility.Variables;
        }

        private void WorkflowSourceChanged(TreeViewItem treeViewItem)
        {
            _workflowDesigner.Flush();
            string content = _workflowDesigner.Text;
            debugInfo.Text = content;

            var xElement = treeViewItem.DataContext as XElement;
            if (xElement == null)
            {
                MessageBox.Show("Related treeview item does not contain xml format data.");
                return;
            }
            Activity activity = Utilities.GetActivityFromContentString(content);
            var name = activity.GetType().GetProperty("Name").GetValue(activity, null) as string;
            var description = activity.GetType().GetProperty("Description").GetValue(activity, null) as string;
            if (!string.IsNullOrEmpty(name))
                xElement.SetAttributeValue("Name", name);
            if (!string.IsNullOrEmpty(description))
                xElement.SetAttributeValue("Description", description);

            xElement.SetAttributeValue("Content", content);

            string sRoot = Communication.GetInstance().SetById(xElement);
            XElement xRoot = XElement.Parse(sRoot);
            string result = xRoot.GetAttributeValue("Result");
            if (string.IsNullOrEmpty(result)) return;
            if (result.Equals("Failed"))
            {
                MessageBox.Show("update Tree item Failed. item=\n" + xRoot + "\nReason:" +
                                xRoot.GetAttributeValue("Reason"));
                return;
            }
            Dispatcher.BeginInvoke(new Action(() => UpdateRelatedTreeViewItem(treeViewItem, xElement)));
        }

        private static void UpdateRelatedTreeViewItem(TreeViewItem treeViewItem, XElement xElement)
        {
            treeViewItem.DataContext = xElement;
            treeViewItem.UpdateTreeViewItem(xElement);
        }
    }
}