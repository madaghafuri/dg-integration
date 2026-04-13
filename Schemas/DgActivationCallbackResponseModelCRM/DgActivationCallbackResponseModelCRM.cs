using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgActivationCallbackService.CRMResponse
{
    [XmlRoot(Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatusResponse 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatusResult UpdateOrderStatusResult { get; set; }

		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("xsd", "http://www.w3.org/2001/XMLSchema");
				ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
				ns.Add("", "http://www.digi.com.my/");
				return ns;
			}
			set { }
		}
	}

    [XmlRoot(Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatusResult 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public responseHeader responseHeader { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public resultStatus resultStatus { get; set; }
	}

    [XmlRoot(Namespace="http://www.digi.com.my/")]
	public class responseHeader 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ReferenceId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; }
	}

    [XmlRoot(Namespace="http://www.digi.com.my/")]
	public class resultStatus 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string StatusCode { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ErrorCode { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ErrorDescription { get; set; }
	}
}