using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using System.ServiceModel;
using SearchCustomerWorkspace.SOAPICCS;
using System.ServiceModel.Channels;
using RightNow.AddIns.Common;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SearchCustomerWorkspace
{
    public partial class SearchCustomer : UserControl
    {
        private bool inDesignMode;
        private IRecordContext recordContext;
        private IGlobalContext globalContext;
        public IIncident Incident;
        private RightNowSyncPortClient clientRN;
        private bool blnName { get; set; }
        private bool blnRFC { get; set; }

        public SearchCustomer()
        {
            try
            {
                InitializeComponent();
                txtCustomer.KeyDown += txtCustomer_KeyDown;
                txtRFC.KeyDown += txtCustomer_KeyDown;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void txtCustomer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }
        public SearchCustomer(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext) : this()
        {
            try
            {
                this.inDesignMode = inDesignMode;
                this.recordContext = recordContext;
                this.globalContext = globalContext;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        internal void LoadData()
        {
            try
            {
                long ParyId = 0;
                string RFC = "";
                string RFCCompare = "";
                Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                IList<ICfVal> customAttributes = Incident.CustomField;
                foreach (ICfVal custom in customAttributes)
                {
                    if (custom.CfId == 57)
                    {
                        ParyId = Convert.ToInt64(custom.ValStr);
                    }
                    if (custom.CfId == 59)
                    {
                        RFC = String.IsNullOrEmpty(custom.ValStr) ? "" : custom.ValStr;
                    }
                }
                if (ParyId != 0)
                {
                    getCustomerData(ParyId);
                    RFCCompare = GetRFC(ParyId);

                }
                if (!String.IsNullOrEmpty(RFCCompare))
                {
                    if (RFCCompare != ".")
                    {
                        if (RFCCompare.Trim() != RFC.Trim())
                        {
                            foreach (ICfVal custom1 in customAttributes)
                            {
                                if (custom1.CfId == 59)
                                {
                                    custom1.ValStr = RFCCompare;
                                    recordContext.ExecuteEditorCommand(EditorCommand.Save);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                dataGridResult.DataSource = null;
                validateInput(txtCustomer.Text, txtRFC.Text);
                if (blnName && blnRFC)
                {
                    getOrgs();
                }
                if (blnName && !blnRFC)
                {
                    getOrgs();
                }
                if (!blnName && blnRFC)
                {
                    getOrgs();
                }
                if (!blnName && !blnRFC)
                {
                    MessageBox.Show("At least one textbox must have three characters");
                }
            }
            catch (Exception ex)
            {
                globalContext.LogMessage(ex.Message);

            }
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
        private void dataGridResult_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // MessageBox.Show(dataGridResult.Rows[e.RowIndex].Cells[0].FormattedValue.ToString());
        }
        private void dataGridResult_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string PartyId = "";
            if (e.RowIndex != -1)
            {
                IList<ICfVal> orgCustomFieldList = Incident.CustomField;
                if (orgCustomFieldList != null)
                {
                    foreach (ICfVal inccampos in orgCustomFieldList)
                    {
                        if (inccampos.CfId == 57)
                        {
                            inccampos.ValStr = dataGridResult.Rows[e.RowIndex].Cells[2].FormattedValue.ToString();
                            PartyId = dataGridResult.Rows[e.RowIndex].Cells[2].FormattedValue.ToString();
                        }
                        if (inccampos.CfId == 58)
                        {
                            inccampos.ValStr = dataGridResult.Rows[e.RowIndex].Cells[0].FormattedValue.ToString();
                        }
                        if (inccampos.CfId == 59)
                        {
                            inccampos.ValStr = dataGridResult.Rows[e.RowIndex].Cells[1].FormattedValue.ToString();
                        }
                    }
                    if (!String.IsNullOrEmpty(PartyId))
                    {
                        getCustomerData(Convert.ToInt64(PartyId));
                    }
                }
                else
                {
                    MessageBox.Show("Listavacia");
                }

            }
        }
        private void validateInput(string name, string rfc)
        {
            blnName = false;
            blnRFC = false;
            if (!String.IsNullOrEmpty(name) && name.Length >= 3)
            {
                blnName = true;
            }
            if (!String.IsNullOrEmpty(rfc) && rfc.Length >= 3)
            {
                blnRFC = true;
            }
        }
        private void getOrgs()
        {
            try
            {
                // Construct xml payload to invoke the service. In this example, it is a hard coded string.
                string envelope = "<soapenv:Envelope" +
               "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
               "   xmlns:typ=\"http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/types/\"" +
               "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                    "<typ:findOrganization>" +
                        "<typ:findCriteria>" +
                            "<typ1:fetchStart>0</typ1:fetchStart>" +
                            "<typ1:fetchSize>-1</typ1:fetchSize>" +
                            "<typ1:filter>";
                if (blnName || blnRFC)
                {
                    envelope = envelope + "<typ1:group>" +
                  "<typ1:item>" +
                     "<typ1:conjunction>And</typ1:conjunction>" +
                     "<typ1:attribute>OrganizationProfile</typ1:attribute>" +
                     "<typ1:nested>" +
                        "<typ1:group>";
                    if (blnName)
                    {
                        envelope = envelope + "<typ1:item>" +
                                  "<typ1:conjunction>And</typ1:conjunction>" +
                                  "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                  "<typ1:attribute>OrganizationName</typ1:attribute>" +
                                  "<typ1:operator>CONTAINS</typ1:operator>" +
                                  "<typ1:value>" + txtCustomer.Text + "</typ1:value>" +
                               "</typ1:item>";
                    }
                    if (blnRFC)
                    {
                        envelope = envelope + "<typ1:item>" +
                              "<typ1:conjunction>And</typ1:conjunction>" +
                              "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                              "<typ1:attribute>JgzzFiscalCode</typ1:attribute>" +
                              "<typ1:operator>CONTAINS</typ1:operator>" +
                               "<typ1:value>" + txtRFC.Text + "</typ1:value>" +
                           "</typ1:item>";
                    }
                    envelope = envelope + "<typ1:item>" + 
                              "<typ1:conjunction>And</typ1:conjunction>" +
                              "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                              "<typ1:attribute>PartyUsageAssignment</typ1:attribute>" +
                              "<typ1:nested>" +
                                  "<typ1:group>" +
                                      "<typ1:item>" +
                                          "<typ1:conjunction>And</typ1:conjunction>" +
                                          "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                                          "<typ1:attribute>PartyUsageCode</typ1:attribute>" +
                                          "<typ1:operator><![CDATA[<>]]></typ1:operator>" +
                                          "<typ1:value>SUPPLIER</typ1:value>" +
                                       "</typ1:item>" +
                                   "</typ1:group>" +
                              "</typ1:nested>" +
                           "</typ1:item>" +
                      "</typ1:group>" +
                "</typ1:nested>" +
             "</typ1:item>" +
          "</typ1:group>";
                }
                envelope = envelope + "</typ1:filter>" +
                                            "<typ1:findAttribute>OrganizationProfile</typ1:findAttribute>" +
                                            "<typ1:childFindCriteria>" +
                                            "<typ1:findAttribute>OrganizationName</typ1:findAttribute>" +
                                                "<typ1:findAttribute>JgzzFiscalCode</typ1:findAttribute>" +
                                                "<typ1:findAttribute>OrganizationProfileId</typ1:findAttribute>" +
                                                "<typ1:childAttrName>OrganizationProfile</typ1:childAttrName>" +
                                            "</typ1:childFindCriteria>" +
                                        "</typ:findCriteria>" +
                                        "<typ:findControl>" +
                                            "<typ1:retrieveAllTranslations>false</typ1:retrieveAllTranslations>" +
                                        "</typ:findControl>" +
                                    "</typ:findOrganization>" +
                                "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/crmService/FoundationPartiesOrganizationService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/findOrganizationProfile");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                List<Result> resultadolista = new List<Result>();
                // Get the response and process it; In this example, we simply print out the response XDocument doc;
                using (WebResponse response = request.GetResponse())
                {

                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/types/");
                        nms.AddNamespace("ns3", "http://xmlns.oracle.com/apps/cdm/foundation/parties/flex/organization/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/cdm/foundation/parties/partyService/");

                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns2:Value", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "Value")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "OrganizationProfile")
                                        {
                                            Result resu = new Result();
                                            XmlNodeList nodeListvalueorg = nodeValue.ChildNodes;
                                            foreach (XmlNode nodeValueorg in nodeListvalueorg)
                                            {

                                                if (nodeValueorg.LocalName == "OrganizationProfileId")
                                                {
                                                    resu.ID = Convert.ToInt64(nodeValueorg.InnerText);
                                                }
                                                if (nodeValueorg.LocalName == "OrganizationName")
                                                {
                                                    resu.Customer = nodeValueorg.InnerText;
                                                }
                                                if (nodeValueorg.LocalName == "JgzzFiscalCode")
                                                {
                                                    resu.RFC = nodeValueorg.InnerText;
                                                }
                                            }
                                            resultadolista.Add(resu);
                                        }
                                    }
                                }
                            }
                        }
                        response.Close();
                    }
                    dataGridResult.DataSource = resultadolista;
                    dataGridResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridResult.Focus();
                }

                if (resultadolista.Count == 0)
                {
                    DialogResult dr = MessageBox.Show("¡No data found! ¿Would you like to create a new customer?",
                          "No data found", MessageBoxButtons.YesNo);
                    switch (dr)
                    {
                        case DialogResult.Yes:
                            globalContext.AutomationContext.CreateWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Organization);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }

        }
        private void getCustomerData(long PartyId)
        {
            try
            {
                string AccountNumber = "";
                string CustomerClass = "";
                string CateDescuento = "";
                string CateCliente = "";
                string Royalty = "";
                string Utilidad = "";
                string Seneam = "";
                string SeneamCat = "";
                string Ortodromico = "";
                string Combustible = "";
                string CombustibleI = "";

                string envelope = "<soapenv:Envelope" +
             "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
             "   xmlns:typ=\"http://xmlns.oracle.com/apps/cdm/foundation/parties/customerAccountService/applicationModule/types/\"" +
             "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
                "<typ:findCustomerAccount>" +
                "<typ:findCriteria>" +
                "<typ1:fetchStart>0</typ1:fetchStart>" +
                "<typ1:fetchSize>-1</typ1:fetchSize>" +
                 "<typ1:filter>" +
                 "<typ1:group>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                   "<typ1:attribute>PartyId</typ1:attribute>" +
                   "<typ1:operator>=</typ1:operator>" +
                   "<typ1:value>" + PartyId + "</typ1:value>" +
               "</typ1:item>" +
               "</typ1:group>" +
             "</typ1:filter>" +
             "</typ:findCriteria>" +
             "<typ:findControl>" +
             "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
             "</typ:findControl>" +
             "</typ:findCustomerAccount>" +
             "</soapenv:Body>" +
             "</soapenv:Envelope>";

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/crmService/CustomerAccountService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/customerAccountService/applicationModule/findCustomerAccount");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;

                // Get the response and process it; In this example, we simply print out the response XDocument doc;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("ns0", "http://xmlns.oracle.com/apps/cdm/foundation/parties/customerAccountService/applicationModule/types/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/apps/cdm/foundation/parties/customerAccountService/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/adf/svc/types/");
                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:Value", nms);
                        if (desiredNode != null)
                        {
                            if (desiredNode.HasChildNodes)
                            {
                                for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                                {
                                    if (desiredNode.ChildNodes[i].LocalName == "AccountNumber")
                                    {
                                        AccountNumber = desiredNode.ChildNodes[i].InnerText;
                                    }
                                    if (desiredNode.ChildNodes[i].LocalName == "CustomerClassCode")
                                    {
                                        CustomerClass = desiredNode.ChildNodes[i].InnerText;
                                    }
                                    if (desiredNode.ChildNodes[i].LocalName == "CustAcctInformation")
                                    {
                                        XmlNodeList nodeListvalue = desiredNode.ChildNodes[i].ChildNodes;
                                        foreach (XmlNode nodeValueorg in nodeListvalue)
                                        {
                                            if (nodeValueorg.LocalName == "xxCategoriaUtilidad")
                                            {
                                                Utilidad = nodeValueorg.InnerText;
                                            }
                                            if (nodeValueorg.LocalName == "xxCategoriaRoyalty")
                                            {
                                                Royalty = nodeValueorg.InnerText;
                                            }
                                            if (nodeValueorg.LocalName == "xxCategoriaCombustible")
                                            {
                                                Combustible = nodeValueorg.InnerText;
                                            }
                                            if (nodeValueorg.LocalName == "xxSeneam")
                                            {
                                                Seneam = nodeValueorg.InnerText == "SI" ? "1" : "0";
                                            }
                                            if (nodeValueorg.LocalName == "xxCombustibleFuelI")
                                            {
                                                CombustibleI = nodeValueorg.InnerText;
                                            }
                                            if (nodeValueorg.LocalName == "xxCategoriaSeneam")
                                            {
                                                SeneamCat = nodeValueorg.InnerText;
                                            }
                                            if (nodeValueorg.LocalName == "xxOrtodromico")
                                            {
                                                Ortodromico = nodeValueorg.InnerText == "SI" ? "1" : "0";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    response.Close();
                }
                if (Incident != null)
                {
                    IList<ICfVal> orgCustomFieldList = Incident.CustomField;
                    foreach (ICfVal inccampos in orgCustomFieldList)
                    {
                        if (inccampos.CfId == 60)
                        {
                            inccampos.ValStr = AccountNumber;
                        }
                        if (inccampos.CfId == 61)
                        {
                            inccampos.ValStr = Royalty;
                        }
                        if (inccampos.CfId == 62)
                        {
                            inccampos.ValStr = Utilidad;
                        }
                        if (inccampos.CfId == 63)
                        {
                            inccampos.ValStr = Combustible;
                        }
                        if (inccampos.CfId == 81)
                        {
                            inccampos.ValStr = SeneamCat;
                        }
                        if (inccampos.CfId == 82)
                        {
                            inccampos.ValStr = CombustibleI;
                        }
                        if (inccampos.CfId == 84)
                        {
                            inccampos.ValStr = Ortodromico;
                        }
                        if (inccampos.CfId == 85)
                        {
                            inccampos.ValStr = Seneam;
                        }
                        if (inccampos.CfId == 87)
                        {
                            inccampos.ValStr = CustomerClass;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private string GetRFC(long PartyId)
        {
            string rfc = "";
            try
            {
                string envelope = "<soapenv:Envelope" +
               "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
               "   xmlns:typ=\"http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/types/\"" +
               "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                   "<soapenv:Header/>" +
                   "<soapenv:Body>" +
                       "<typ:findOrganization>" +
                           "<typ:findCriteria>" +
                               "<typ1:fetchStart>0</typ1:fetchStart>" +
                               "<typ1:fetchSize>-1</typ1:fetchSize>" +
                               "<typ1:filter>" +
                                   "<typ1:group>" +
                                       "<typ1:item>" +
                                           "<typ1:conjunction>And</typ1:conjunction>" +
                                           "<typ1:attribute>PartyId</typ1:attribute>" +
                                           "<typ1:operator>=</typ1:operator>" +
                                           "<typ1:value>" + PartyId + "</typ1:value>" +
                                       "</typ1:item>" +
                                   "</typ1:group>" +
                               "</typ1:filter>" +
                               "<typ1:findAttribute>OrganizationProfile</typ1:findAttribute>" +
                               "<typ1:childFindCriteria>" +
                                   "<typ1:findAttribute>JgzzFiscalCode</typ1:findAttribute>" +
                                   "<typ1:childAttrName>OrganizationProfile</typ1:childAttrName>" +
                               "</typ1:childFindCriteria>" +
                           "</typ:findCriteria>" +
                           "<typ:findControl>" +
                               "<typ1:retrieveAllTranslations>false</typ1:retrieveAllTranslations>" +
                           "</typ:findControl>" +
                       "</typ:findOrganization>" +
                   "</soapenv:Body>" +
               "</soapenv:Envelope>";

                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/crmService/FoundationPartiesOrganizationService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/findOrganization");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;

                // Get the response and process it; In this example, we simply print out the response XDocument doc;
                using (WebResponse response = request.GetResponse())
                {

                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);

                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("ns0", "http://xmlns.oracle.com/adf/svc/types/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/cdm/foundation/parties/partyService/");
                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:OrganizationProfile", nms);
                        if (desiredNode.HasChildNodes)
                        {
                            for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                            {
                                if (desiredNode.ChildNodes[i].LocalName == "JgzzFiscalCode")
                                {
                                    rfc = desiredNode.ChildNodes[i].InnerText;
                                }
                            }
                        }

                    }
                    response.Close();
                }
                return rfc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return "";
            }

        }

    }
}



