using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgUpdateStatusDelivery.Response
{
	
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope { 

		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; } 

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
				ns.Add("wsa", "http://schemas.xmlsoap.org/ws/2004/08/addressing");
				ns.Add("wsse", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
				ns.Add("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

				return ns;
			}
			set { }
		}
	}

	public class Header { 
		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Action { get; set; } 

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string MessageID { get; set; } 

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string RelatesTo { get; set; } 

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string To { get; set; } 

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; } 
	}

	public class Security { 

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public Timestamp Timestamp { get; set; } 

		[XmlAttribute(AttributeName="Id", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Id { get; set; }
	}

	public class Timestamp { 

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Created { get; set; } 

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Expires { get; set; } 
	}

	public class Body { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public UpdateDeliveryResponse UpdateDeliveryResponse { get; set; } 
	}

	public class UpdateDeliveryResponse { 
		public UpdateDeliveryResult UpdateDeliveryResult { get; set; } 
	}

	public class UpdateDeliveryResult { 
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public int Code { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Message { get; set; } 
	}
}