#region

// Hapa Project, CC
// Created @2012 08 24 09:25
// Last Updated  by Huang, Jien @2012 08 24 09:25

#region

#endregion

#region

using System;

#endregion

#endregion

namespace AutoX.Basic.Model
{
    public class Index : IDataObject
    {
        public string ParentId;
        public string Type;

        #region IDataObject Members

        public string _id { get; set; }

        public DateTime Updated { get; set; }

        public DateTime Created { get; set; }

        public string _parentId { get; set; }

        #endregion
    }
}