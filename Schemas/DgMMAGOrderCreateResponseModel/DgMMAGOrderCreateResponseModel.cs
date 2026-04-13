 using System;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgMMAGOrderCreateService.Response
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope 
    {
		[XmlElement(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }

		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
				ns.Add("xsd", "http://www.w3.org/2001/XMLSchema");
				ns.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");
				return ns;
			}
			set { }
		}
	}

	public class Body 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public OrderCreateResponse OrderCreateResponse { get; set; }
	}

	public class OrderCreateResponse 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public Response Response { get; set; }

		[XmlAttribute(AttributeName="xmlns")]
		public string xmlns 
        { 
            get {
				return "https://csg.mmag.com.my/rest/digi/nccf/";
			} 
			set {}
        }
	}

	public class Response
	{
		[XmlAttribute]
		public string Code { get; set; }

		[XmlAttribute]
		public string Message { get; set; }
	}
}