using System;
using System.Xml.Serialization;
using System.Collections.Generic;
namespace DgIntegration.DgUpdatePostDelivery.Request
{
	[XmlRoot(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Password {
		[XmlAttribute(AttributeName="Type")]
		public string Type { get; set; }
		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(ElementName="UsernameToken", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class UsernameToken {
		[XmlElement(ElementName="Username", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public string Username { get; set; }
		[XmlElement(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Password Password { get; set; }
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

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
	}

	[XmlRoot(ElementName="postReq", Namespace="http://www.digi.com.my/")]
	public class PostReq {
		[XmlElement(ElementName="OrderId", Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; }
		[XmlElement(ElementName="CenterId", Namespace="http://www.digi.com.my/")]
		public string CenterId { get; set; }
		[XmlElement(ElementName="ChannelId", Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }
		[XmlElement(ElementName="PostDeliveryDateTime", Namespace="http://www.digi.com.my/")]
		public string PostDeliveryDateTime { get; set; }
		[XmlElement(ElementName="DeliveryStatus", Namespace="http://www.digi.com.my/")]
		public string DeliveryStatus { get; set; }
		[XmlElement(ElementName="DeliveryAttempt", Namespace="http://www.digi.com.my/")]
		public string DeliveryAttempt { get; set; }
		[XmlElement(ElementName="StatusRemarks", Namespace="http://www.digi.com.my/")]
		public string StatusRemarks { get; set; }
	}

	[XmlRoot(ElementName="UpdatePostDelivery", Namespace="http://www.digi.com.my/")]
	public class UpdatePostDelivery {
		[XmlElement(ElementName="postReq", Namespace="http://www.digi.com.my/")]
		public PostReq PostReq { get; set; }
	}

	[XmlRoot(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body {
		[XmlElement(ElementName="UpdatePostDelivery", Namespace="http://www.digi.com.my/")]
		public UpdatePostDelivery UpdatePostDelivery { get; set; }
	}

	[XmlRoot(ElementName="Envelope", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope {
		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }
		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }
		[XmlAttribute(AttributeName="soapenv", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Soapenv { get; set; }
		[XmlAttribute(AttributeName="digi", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Digi { get; set; }
	}

}
