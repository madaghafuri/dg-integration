using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgUpdatePostDelivery.Response
{
	public class PostDeliveryResponse {
		public Timestamp Timestamp { get; set; }
		public Security Security { get; set; }
		public Header Header { get; set; }
		public UpdatePostDeliveryResult UpdatePostDeliveryResult { get; set; }
		public UpdatePostDeliveryResponse UpdatePostDeliveryResponse { get; set; }
		public Body Body { get; set; }
		public Envelope Envelope { get; set; }
	}
	
	[XmlRoot(ElementName="Timestamp", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
	public class Timestamp {
		[XmlElement(ElementName="Created", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Created { get; set; }
		[XmlElement(ElementName="Expires", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Expires { get; set; }
		[XmlAttribute(AttributeName="Id", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Id { get; set; }
	}

	[XmlRoot(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Security {
		[XmlElement(ElementName="Timestamp", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public Timestamp Timestamp { get; set; }
	}

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="Action", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Action { get; set; }
		[XmlElement(ElementName="MessageID", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string MessageID { get; set; }
		[XmlElement(ElementName="RelatesTo", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string RelatesTo { get; set; }
		[XmlElement(ElementName="To", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string To { get; set; }
		[XmlElement(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
	}

	[XmlRoot(ElementName="UpdatePostDeliveryResult", Namespace="http://www.digi.com.my/")]
	public class UpdatePostDeliveryResult {
		[XmlElement(ElementName="Code", Namespace="http://www.digi.com.my/")]
		public string Code { get; set; }
		[XmlElement(ElementName="Message", Namespace="http://www.digi.com.my/")]
		public string Message { get; set; }
	}

	[XmlRoot(ElementName="UpdatePostDeliveryResponse", Namespace="http://www.digi.com.my/")]
	public class UpdatePostDeliveryResponse {
		[XmlElement(ElementName="UpdatePostDeliveryResult", Namespace="http://www.digi.com.my/")]
		public UpdatePostDeliveryResult UpdatePostDeliveryResult { get; set; }
		[XmlAttribute(AttributeName="xmlns")]
		public string Xmlns { get; set; }
	}

	[XmlRoot(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body {
		[XmlElement(ElementName="UpdatePostDeliveryResponse", Namespace="http://www.digi.com.my/")]
		public UpdatePostDeliveryResponse UpdatePostDeliveryResponse { get; set; }
	}

	[XmlRoot(ElementName="Envelope", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope {
		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }
		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }
		[XmlAttribute(AttributeName="soap", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Soap { get; set; }
		[XmlAttribute(AttributeName="xsi", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Xsi { get; set; }
		[XmlAttribute(AttributeName="xsd", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Xsd { get; set; }
		[XmlAttribute(AttributeName="wsa", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsa { get; set; }
		[XmlAttribute(AttributeName="wsse", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsse { get; set; }
		[XmlAttribute(AttributeName="wsu", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsu { get; set; }
	}

}
