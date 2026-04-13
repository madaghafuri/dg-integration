using System;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgUpdateOrderStatusService31V2.Response
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope { 

		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }
		
		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; } 

		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
				ns.Add("digi", "http://www.digi.com.my/");

				return ns;
			}
			set { }
		}
	}

	[XmlRoot(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header { 
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public CSGHeader CSGHeader { get; set; } 
	}

	[XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body { 
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string UpdateOrderStatusResponse { get; set; } 
	}

	[XmlRoot(ElementName = "CSGHeader", Namespace = "http://www.digi.com.my/")]
	public class CSGHeader { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string SourceSystemID { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ReferenceID { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Status { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ErrorCode { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ErrorDescription { get; set; } 
		
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string BusinessUnit { get; set; } 
	}
}