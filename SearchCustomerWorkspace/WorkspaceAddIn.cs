using System;
using System.AddIn;
using System.Collections.Generic;
using System.Drawing;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;
using SearchCustomerWorkspace.SOAPICCS;

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

        IGlobalContext globalContext { get; set; }

        public IWorkspaceComponent2 CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new Component(inDesignMode, RecordContext, globalContext);
        }


        public Image Image16
        {
            get { return Properties.Resources.AddIn16; }
        }


        public string Text
        {
            get { return "Customer"; }
        }

        public string Tooltip
        {
            get { return "Buscar Customer en SR"; }
        }

        public bool Initialize(IGlobalContext GlobalContext)
        {
            globalContext = GlobalContext;
            return true;
        }


    }

    public class Component : IWorkspaceComponent2
    {
        private SearchCustomer control;
        private RightNowSyncPortClient clientRN { get; set; }
        private IRecordContext recordContext { get; set; }
        private IGlobalContext globalContext { get; set; }
        private IIncident Incident { get; set; }
        private int IncidentID { get; set; }

        public Component(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext)
        {
            this.recordContext = recordContext;
            this.globalContext = globalContext;
            control = new SearchCustomer(inDesignMode, recordContext, globalContext);
            if (!inDesignMode)
            {

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
            if (actionName == "GetAircraftType")
            {
                if (Init())
                {
                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IncidentID = Incident.ID;
                    IList<ICfVal> incCustomFieldList = Incident.CustomField;
                    if (incCustomFieldList != null)
                    {
                        foreach (ICfVal inccampos in incCustomFieldList)
                        {
                            if (inccampos.CfId == 96)
                            {
                                inccampos.ValStr = GetAircraftType();
                            }
                        }
                    }
                }
            }
        }
        public string GetAircraftType()
        {
            string air = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.CO.Aircraft.AircraftType1.ICAODESIGNATOR FROM  Incident WHERE ID =  " + IncidentID;
            clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    air = data;
                }
            }
            return air;

        }

        public string RuleConditionInvoked(string conditionName)
        {
            throw new NotImplementedException();
        }

        public Control GetControl()
        {
            return control;

        }

        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(globalContext.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                // Minimum required
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                // Create client proxy class
                clientRN = new RightNowSyncPortClient(binding, endPointAddr);
                // Ask the client to not send the timestamp
                BindingElementCollection elements = clientRN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientRN.Endpoint.Binding = new CustomBinding(elements);
                // Ask the Add-In framework the handle the session logic
                globalContext.PrepareConnectSession(clientRN.ChannelFactory);
                if (clientRN != null)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }




    }

}