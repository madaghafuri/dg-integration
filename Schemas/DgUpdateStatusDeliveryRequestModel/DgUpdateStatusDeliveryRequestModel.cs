using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgUpdateStatusDelivery.Request
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
				ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
				ns.Add("digi", "http://www.digi.com.my/");

				return ns;
			}
			set { }
		}
	}

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
	}

	[XmlRoot(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Security {
		[XmlElement(ElementName="UsernameToken", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public UsernameToken UsernameToken { get; set; }
		[XmlAttribute(AttributeName="wsse", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsse { get; set; }
		[XmlAttribute(AttributeName="mustUnderstand", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public string MustUnderstand { get; set; }
	}

	[XmlRoot(ElementName="UsernameToken", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class UsernameToken {
		[XmlElement(ElementName="Username", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public string Username { get; set; }
		[XmlElement(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Password Password { get; set; }
	}

	[XmlRoot(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Password {
		[XmlAttribute(AttributeName="Type")]
		public string Type { get; set; }
		[XmlText]
		public string Text { get; set; }
	}

	public class Body { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public UpdateDelivery UpdateDelivery { get; set; } 
	}

	public class UpdateDelivery { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public req req { get; set; } 
	}

	public class req { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public int CenterId { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public int ChannelId { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public DeliveryItems DeliveryItems { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string DeliveryDT { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string DONo { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string DeliveryStatus { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Remarks { get; set; } 
	}

	public class DeliveryItems { 
	[XmlElement(Namespace="http://www.digi.com.my/")]
		public List<DeliveryItem> DeliveryItem { get; set; } 
	}

	public class DeliveryItem { 
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ItemCode { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Quantity { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string SerialNumber { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string IMEINumber { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string PINNumber { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string MSISDN { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string NCCFLineID { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Remarks { get; set; } 
	}
}