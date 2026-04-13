using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgSubmitNewCorpCustomerOrderService.Response
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope 
	{
		[XmlElement(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
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

				return ns;
			}
			set { }
		}
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public CSGHeader CSGHeader { get; set; }

		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("digi", "http://digi.com.my/");

				return ns;
			}
			set {}
		}
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CSGHeader 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string SourceSystemID { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string ReferenceID { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string ChannelMedia { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string GUID { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string Status { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string ErrorCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string ErrorDescription { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string UserMessage { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string BusinessUnit { get; set; }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{

	}
}
