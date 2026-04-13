using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgGetAccountService.Request
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope 
	{
		[XmlElement(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
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

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public getAccounts getAccounts { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class getAccounts 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public AccessSessionRequest AccessSessionRequest { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public GetAccountsRequest GetAccountsRequest { get; set; }

		[XmlAttribute(AttributeName="xmlns")]
		public string xmlns 
        { 
            get {
				return "http://oss.huawei.com/webservice/external/services/schema";
			} 
			set {}
        }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class AccessSessionRequest 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public accessChannel accessChannel { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public operatorCode operatorCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public password password { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public beId beId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public transactionId transactionId { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class accessChannel 
	{
		[XmlAttribute]
		public string xmlns
		{ 
            get {
				return "http://oss.huawei.com/webservice/external/services/basetype/";
			} 
			set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class operatorCode 
	{
		[XmlAttribute]
		public string xmlns
		{ 
            get {
				return "http://oss.huawei.com/webservice/external/services/basetype/";
			} 
			set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class password 
	{
		[XmlAttribute]
		public string xmlns
		{ 
            get {
				return "http://oss.huawei.com/webservice/external/services/basetype/";
			} 
			set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class beId 
	{
		[XmlAttribute]
		public string xmlns
		{ 
            get {
				return "http://oss.huawei.com/webservice/external/services/basetype/";
			} 
			set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class transactionId 
	{
		[XmlAttribute]
		public string xmlns
		{ 
            get {
				return "http://oss.huawei.com/webservice/external/services/basetype/";
			} 
			set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class GetAccountsRequest 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string accountId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string accountCode { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string subscriberId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string msisdn { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string customerId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string isIncludeAllAcctsUnderCust { get; set; }
	}
}