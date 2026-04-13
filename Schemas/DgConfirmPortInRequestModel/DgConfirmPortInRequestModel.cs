using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgConfirmPortInService.Request
{
	[XmlRoot(ElementName="Envelope", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope {
		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }

		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }

		[XmlAttribute(AttributeName="digi", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Digi { get; set; }

		[XmlAttribute(AttributeName="soapenv", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Soapenv { get; set; }
	}

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="CSGHeader", Namespace="http://digi.com.my/")]
		public CSGHeader CSGHeader { get; set; }

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
	}

	[XmlRoot(ElementName="CSGHeader", Namespace="http://digi.com.my/")]
	public class CSGHeader {
		[XmlElement(ElementName="SourceSystemID", Namespace="http://digi.com.my/")]
		public string SourceSystemID { get; set; }

		[XmlElement(ElementName="ReferenceID", Namespace="http://digi.com.my/")]
		public string ReferenceID { get; set; }

		[XmlElement(ElementName="ChannelMedia", Namespace="http://digi.com.my/")]
		public string ChannelMedia { get; set; }

		[XmlElement(ElementName="BusinessUnit", Namespace="http://digi.com.my/")]
		public string BusinessUnit { get; set; }
	}

	[XmlRoot(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body {
		[XmlElement(ElementName="ConfirmPortInRequest", Namespace="http://digi.com.my/")]
		public ConfirmPortInRequest ConfirmPortInRequest { get; set; }
	}

	[XmlRoot(ElementName="ConfirmPortInRequest", Namespace="http://digi.com.my/")]
	public class ConfirmPortInRequest {
		[XmlElement(ElementName="MNPInformation", Namespace="http://digi.com.my/")]
		public MNPInformation MNPInformation { get; set; }
	}

	[XmlRoot(ElementName="MNPInformation", Namespace="http://digi.com.my/")]
	public class MNPInformation {
		[XmlElement(ElementName="PortInTransactionId", Namespace="http://digi.com.my/")]
		public string PortInTransactionId { get; set; }

		[XmlElement(ElementName="PortInMessageId", Namespace="http://digi.com.my/")]
		public string PortInMessageId { get; set; }

		[XmlElement(ElementName="PortId", Namespace="http://digi.com.my/")]
		public string PortId { get; set; }
	}
}
