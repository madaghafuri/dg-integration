using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgConfirmPortInService.Response
{
	[XmlRoot(ElementName="Envelope", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope {
		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }

		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public string Body { get; set; }

		[XmlAttribute(AttributeName="soap", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Soap { get; set; }
	}

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="CSGHeader", Namespace="http://digi.com.my/")]
		public CSGHeader CSGHeader { get; set; }
	}

	[XmlRoot(ElementName="CSGHeader", Namespace="http://digi.com.my/")]
	public class CSGHeader {
		[XmlElement(ElementName="SourceSystemID", Namespace="http://digi.com.my/")]
		public string SourceSystemID { get; set; }

		[XmlElement(ElementName="ReferenceID", Namespace="http://digi.com.my/")]
		public string ReferenceID { get; set; }

		[XmlElement(ElementName="ChannelMedia", Namespace="http://digi.com.my/")]
		public string ChannelMedia { get; set; }

		[XmlElement(ElementName="GUID", Namespace="http://digi.com.my/")]
		public string GUID { get; set; }

		[XmlElement(ElementName="Status", Namespace="http://digi.com.my/")]
		public string Status { get; set; }

		[XmlElement(ElementName="ErrorCode", Namespace="http://digi.com.my/")]
		public string ErrorCode { get; set; }

		[XmlElement(ElementName="ErrorDescription", Namespace="http://digi.com.my/")]
		public string ErrorDescription { get; set; }

		[XmlElement(ElementName="UserMessage", Namespace="http://digi.com.my/")]
		public string UserMessage { get; set; }

		[XmlElement(ElementName="BusinessUnit", Namespace="http://digi.com.my/")]
		public string BusinessUnit { get; set; }
		
		[XmlAttribute(AttributeName="digi", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Digi { get; set; }
	}

}
