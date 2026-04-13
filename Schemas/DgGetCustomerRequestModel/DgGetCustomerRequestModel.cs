using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgGetCustomerService.Request
{
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope
    {
        [XmlElement(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public Body Body { get; set; }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                ns.Add("xsd", "http://www.w3.org/2001/XMLSchema");

                return ns;
            }
            set { }
        }
    }

    [XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Body
    {
        [XmlElement(ElementName = "getCustomers", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
        public getCustomers getCustomers { get; set; }
    }

    [XmlRoot(ElementName = "getCustomers", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
    public class getCustomers
    {
        [XmlElement(ElementName = "AccessSessionRequest", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
        public AccessSessionRequest AccessSessionRequest { get; set; }

        [XmlElement(ElementName = "GetCustomersRequest", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
        public GetCustomersRequest GetCustomersRequest { get; set; }
    }

    [XmlRoot(ElementName = "AccessSessionRequest", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
    public class AccessSessionRequest
    {
        [XmlElement(ElementName = "accessChannel", Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string accessChannel { get; set; }

        [XmlElement(ElementName = "operatorCode", Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string operatorCode { get; set; }

        [XmlElement(ElementName = "password", Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string password { get; set; }

        [XmlElement(ElementName = "beId", Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string beId { get; set; }

        [XmlElement(ElementName = "transactionId", Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string transactionId { get; set; }
    }

    [XmlRoot(ElementName = "GetCustomersRequest", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
    public class GetCustomersRequest
    {
		public string customerId { get; set; }
        public string idType { get; set; }
        public string idNumber { get; set; }
        public queryCondForCorp queryCondForCorp { get; set; }
    }

    [XmlRoot(ElementName = "queryCondForCorp", Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
    public class queryCondForCorp
    {
        public string corpNumber { get; set; }
        public string businessRegistrationNumber { get; set; }
		public string groupNumber { get; set; }
        public string memberMsisdn { get; set; }
    }
}
