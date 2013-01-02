﻿// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

using System;
using System.Activities;
using AutoX.Basic;

#endregion

namespace AutoX.Web.Actions
{
    public class Register : NativeActivity
    {
        protected override void Execute(NativeActivityContext context)
        {
            var commandStr = Utils.GetContextValue(context, "command");
            Log.Debug(commandStr);
            try
            {
                //Computer computer = new Computer(content);
                ComputersManager.GetInstance().Register(commandStr);
            }
            catch (Exception ex)
            {
                Log.Debug("we receive invalid command string:\n" + commandStr + "\n" + ex.Message);
            }
            Utils.SetEmptyReturnMessage(context);
        }
    }
}