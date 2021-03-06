﻿#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Windows;
using System.Xml.Linq;
using AutoX.Basic;

#endregion

#endregion

namespace AutoX.Activities
{
    // Interaction logic for CallTestScreenDesigner.xaml
    public partial class CallTestScreenDesigner
    {
        public CallTestScreenDesigner()
        {
            InitializeComponent();
        }

        public void Connect(int connectionId, object target)
        {
            throw new NotImplementedException();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
            if (Utilities.CheckValidDrop(data, Constants.DATUM))
            {
                e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                e.Handled = true;
            }
            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
            if (Utilities.CheckValidDrop(data, Constants.DATUM))
            {
                e.Effects = (DragDropEffects.Move & e.AllowedEffects);
                e.Handled = true;
            }
            base.OnDragOver(e);
        }

        protected override void OnDrop(DragEventArgs e)
        {
            e.Handled = true;
            var data = e.Data.GetData(Constants.DATA_FORMAT) as XElement;
            if (data != null)
            {
                var tag = data.Name.ToString();
                if (tag.Equals(Constants.DATUM))
                    Utilities.DropXElementToDesigner(data, "UserData", ModelItem);

                //DragDropHelper.SetDragDropCompletedEffects(e, DragDropEffects.Move);
            }
            base.OnDrop(e);
        }
    }
}