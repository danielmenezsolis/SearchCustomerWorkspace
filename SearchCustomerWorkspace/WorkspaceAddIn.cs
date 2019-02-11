using System;
using System.AddIn;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using RestSharp;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;
using SearchCustomerWorkspace.SOAPICCS;


namespace SearchCustomerWorkspace
{

    public class Component : IWorkspaceComponent2
    {
        private SearchCustomer control;
        private RightNowSyncPortClient clientRN { get; set; }
        private IRecordContext recordContext { get; set; }
        private IGlobalContext globalContext { get; set; }
        private IIncident Incident { get; set; }
        private int IncidentID { get; set; }
        private int pay { get; set; }

        public Component(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext)
        {
            try
            {
                this.recordContext = recordContext;
                this.globalContext = globalContext;

                control = new SearchCustomer(inDesignMode, recordContext, globalContext);
                if (!inDesignMode)
                {
                    recordContext.Saved += new EventHandler(RecordContext_Saving);
                    recordContext.DataLoaded += (o, e) =>
                        {
                            control.LoadData();
                        };
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void RecordContext_Saving(object sender, EventArgs e)
        {
            try
            {
                if (Init())
                {
                    pay = 0;
                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IncidentID = Incident.ID;
                    pay = UpdpatePayables();
                    if (pay > 0)
                    {

                        recordContext.RefreshWorkspace();
                        MessageBox.Show("Refreshed");
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("RecordContext_Saving" + ex.Message + " Det :" + ex.StackTrace);

            }
        }

        private int UpdpatePayables()
        {
            try
            {
                int i = 0;
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Sum(TicketAmount),Services FROM CO.Payables WHERE Services.Incident =" + IncidentID + " GROUP BY Services";
                globalContext.LogMessage(queryString);
                clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        string[] substrings = data.Split(delimiter);
                        double amount = Convert.ToDouble(substrings[0]);
                        int service = Convert.ToInt32(substrings[1]);
                        UpdatePaxPrice(service, amount);
                        i++;
                    }
                }
                return i;
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdpatePayables" + ex.Message + " Det :" + ex.StackTrace);
                return 0;
            }
        }
        public void UpdatePaxPrice(int id, double costo)
        {
            try
            {
                var client = new RestClient("https://iccsmx.custhelp.com/");
                var request = new RestRequest("/services/rest/connect/v1.4/CO.Services/" + id + "", Method.POST)
                {
                    RequestFormat = DataFormat.Json
                };
                var body = "{";
                // Información de precios costos
                body +=
                    "\"Costo\":\"" + costo + "\"";

                body += "}";
                globalContext.LogMessage("Actualiza al guardar:" + body);
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                // easily add HTTP Headers
                request.AddHeader("Authorization", "Basic ZW9saXZhczpTaW5lcmd5KjIwMTg=");
                request.AddHeader("X-HTTP-Method-Override", "PATCH");
                request.AddHeader("OSvC-CREST-Application-Context", "Update Service {id}");
                // execute the request
                IRestResponse response = client.Execute(request);
                var content = response.Content; // raw content as string
                if (content == "")
                {

                }
                else
                {
                    MessageBox.Show(response.Content);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdatePaxPrice:" + ex.Message + " Det: " + ex.StackTrace);
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
                    int Aircraft = 0;
                    Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    IncidentID = Incident.ID;

                    IList<ICustomAttribute> cfVals = Incident.CustomAttributes;
                    foreach (ICustomAttribute custom in cfVals)
                    {
                        if (custom.GenericField.Name == "CO$Aircraft")
                        {
                            Aircraft = Convert.ToInt32(custom.GenericField.DataValue.Value);
                        }
                    }

                    IList<ICfVal> incCustomFieldList = Incident.CustomField;
                    if (incCustomFieldList != null)
                    {
                        string[] values = GetAircraftType(Aircraft);
                        foreach (ICfVal inccampos in incCustomFieldList)
                        {

                            if (inccampos.CfId == 96)
                            {
                                inccampos.ValStr = values[0];
                            }

                            if (inccampos.CfId == 97)
                            {
                                inccampos.ValStr = values[1];
                            }
                        }
                    }
                }
            }
        }
        public string[] GetAircraftType(int aircraft)
        {
            string[] substring = new string[3];
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT AircraftType1.LookupName,Organization.LookupName FROM CO.Aircraft  WHERE ID = " + aircraft;
            globalContext.LogMessage("QueryGetAircraftType: " + queryString);
            clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Char delimiter = '|';
                    substring = data.Split(delimiter);
                }
            }
            return substring;

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
            get { return "Search SR Customer "; }
        }

        public string Tooltip
        {
            get { return "Buscar Customer en SR"; }
        }

        public bool Initialize(IGlobalContext GlobalContext)
        {
            this.globalContext = GlobalContext;
            return true;
        }
    }


}