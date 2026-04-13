using System;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgCalculateTaxFeeService.Request
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

	public class Body 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public calculateTaxFee calculateTaxFee { get; set; }
	}

	public class calculateTaxFee 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public AccessSessionRequest AccessSessionRequest { get; set; }

		public CalculateTaxFeeRequest CalculateTaxFeeRequest { get; set; }
		
		[XmlAttribute(AttributeName="xmlns")]
		public string xmlns 
        { 
            get {
				return "http://oss.huawei.com/webservice/external/services/schema";
			} 
			set {}
        }
	}

	public class AccessSessionRequest 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string accessChannel { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string operatorCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string password { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string beId { get; set; }
        
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string transactionId { get; set; }
	}

	public class CalculateTaxFeeRequest 
    {
		public string feeItemCode { get; set; }
		public string feeAmt { get; set; }
		// public string dealerCode { get; set; }
	}
}