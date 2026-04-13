using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgActivationCallbackService.CSGResponse
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope { 		
		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
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
				ns.Add("wsa", "http://schemas.xmlsoap.org/ws/2004/08/addressing");
				ns.Add("wsse", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
				ns.Add("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

				return ns;
			}
			set { }
		}
	}

	[XmlRoot(ElementName="Body", Namespace="http://www.w3.org/2003/05/soap-envelope")]
	public class Body {
		[XmlElement(ElementName="UpdateOrderStatusResponse", Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatusResponse UpdateOrderStatusResponse { get; set; }
	}

	[XmlRoot(ElementName="UpdateOrderStatusResponse", Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatusResponse {
		[XmlElement(ElementName="UpdateOrderStatusResult", Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatusResult UpdateOrderStatusResult { get; set; }
		
		[XmlAttribute(AttributeName="xmlns")]
		public string Xmlns { get; set; }
	}

	[XmlRoot(ElementName="UpdateOrderStatusResult", Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatusResult {
		[XmlElement(ElementName="responseHeader", Namespace="http://www.digi.com.my/")]
		public responseHeader responseHeader { get; set; }

		[XmlElement(ElementName="resultStatus", Namespace="http://www.digi.com.my/")]
		public resultStatus resultStatus { get; set; }
	}

	[XmlRoot(ElementName="resultStatus", Namespace="http://www.digi.com.my/")]
	public class resultStatus {
		[XmlElement(ElementName="StatusCode", Namespace="http://www.digi.com.my/")]
		public string StatusCode { get; set; }

		[XmlElement(ElementName="ErrorCode", Namespace="http://www.digi.com.my/")]
		public string ErrorCode { get; set; }

		[XmlElement(ElementName="ErrorDescription", Namespace="http://www.digi.com.my/")]
		public string ErrorDescription { get; set; }
	}

	[XmlRoot(ElementName="responseHeader", Namespace="http://www.digi.com.my/")]
	public class responseHeader {
		[XmlElement(ElementName="GUID", Namespace="http://www.digi.com.my/")]
		public GUID GUID { get; set; }

		[XmlElement(ElementName="ReferenceId", Namespace="http://www.digi.com.my/")]
		public string ReferenceId { get; set; }

		[XmlElement(ElementName="InstanceId", Namespace="http://www.digi.com.my/")]
		public InstanceId InstanceId { get; set; }

		[XmlElement(ElementName="ChannelId", Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }

		[XmlElement(ElementName="ChannelMedia", Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; }
	}

	[XmlRoot(ElementName="InstanceId", Namespace="http://www.digi.com.my/")]
	public class InstanceId {
		[XmlAttribute(AttributeName="nil", Namespace="http://www.w3.org/2001/XMLSchema-instance")]
		public string Nil { get; set; }
	}

	[XmlRoot(ElementName="GUID", Namespace="http://www.digi.com.my/")]
	public class GUID {
		[XmlAttribute(AttributeName="nil", Namespace="http://www.w3.org/2001/XMLSchema-instance")]
		public string Nil { get; set; }
	}
}
