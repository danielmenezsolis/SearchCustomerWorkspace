using System;
using System.AddIn;
using System.Drawing;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;

////////////////////////////////////////////////////////////////////////////////
//
// File: WorkspaceAddIn.cs
//
// Comments:
//
// Notes: 
//
// Pre-Conditions: 
//
////////////////////////////////////////////////////////////////////////////////
namespace SearchCustomerWorkspace
{
    [AddIn("Buscar Información de Cliente", Version = "1.0.0.0")]
    public class WorkspaceAddInFactory : IWorkspaceComponentFactory2
    {
        #region IWorkspaceComponentFactory2 Members
        IGlobalContext globalContext { get; set; }
        /// <summary>
        /// Method which is invoked by the AddIn framework when the control is created.
        /// </summary>
        /// <param name="inDesignMode">Flag which indicates if the control is being drawn on the Workspace Designer. (Use this flag to determine if code should perform any logic on the workspace record)</param>
        /// <param name="RecordContext">The current workspace record context.</param>
        /// <returns>The control which implements the IWorkspaceComponent2 interface.</returns>
        public IWorkspaceComponent2 CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new Component(inDesignMode, RecordContext, globalContext);
        }

        #endregion

        #region IFactoryBase Members

        /// <summary>
        /// The 16x16 pixel icon to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public Image Image16
        {
            get { return Properties.Resources.AddIn16; }
        }

        /// <summary>
        /// The text to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Text
        {
            get { return "Customer"; }
        }

        /// <summary>
        /// The tooltip displayed when hovering over the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Tooltip
        {
            get { return "Buscar Customer en SR"; }
        }

        #endregion

        #region IAddInBase Members

        /// <summary>
        /// Method which is invoked from the Add-In framework and is used to programmatically control whether to load the Add-In.
        /// </summary>
        /// <param name="GlobalContext">The Global Context for the Add-In framework.</param>
        /// <returns>If true the Add-In to be loaded, if false the Add-In will not be loaded.</returns>
        public bool Initialize(IGlobalContext GlobalContext)
        {
            globalContext = GlobalContext;
            return true;
        }

        #endregion
    }

    public class Component : IWorkspaceComponent2
    {
        private SearchCustomer control;

        /// <summary>
        /// create the component
        /// </summary>
        /// <param name="inDesignMode">store the inDesignMode flag</param>
        public Component(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext)
        {
            //create the control and pass all of the information up to it
            control = new SearchCustomer(inDesignMode, recordContext, globalContext);

            //if we're not on a workspace designer listen for the data to finish loading and
            //then load the control information
            if (!inDesignMode)
            {
                //listen for the workspace to finish loading
                recordContext.DataLoaded += (o, e) =>
                {
                    control.LoadData();
                };
            }
        }

        public bool ReadOnly
        {
            get;
            set;
        }

        public void RuleActionInvoked(string actionName)
        {
            throw new NotImplementedException();
        }

        public string RuleConditionInvoked(string conditionName)
        {
            throw new NotImplementedException();
        }

        public Control GetControl()
        {
            return control;

        }
    }

}