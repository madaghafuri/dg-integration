using System;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgCalculateTaxFeeService.Response
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
				ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
				return ns;
			}
			set { }
		}
	}

	public class Body 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public calculateTaxFeeResponse calculateTaxFeeResponse { get; set; }
	}

	public class calculateTaxFeeResponse 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public ResultOfOperationReply ResultOfOperationReply { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public CalculateTaxFeeReply CalculateTaxFeeReply { get; set; }
		
		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("sch", "http://oss.huawei.com/webservice/external/services/schema");
				ns.Add("bas", "http://oss.huawei.com/webservice/external/services/basetype/");
				return ns;
			}
			set { }
		}
	}

	public class ResultOfOperationReply 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string resultCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string resultMessage { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string beId { get; set; }
        
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string transactionId { get; set; }
	}

	public class CalculateTaxFeeReply 
    {
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string feeItemCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string feeAmtCalculated { get; set; }
	}
}