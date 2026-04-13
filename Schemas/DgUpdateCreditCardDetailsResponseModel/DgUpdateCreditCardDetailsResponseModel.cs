using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgUpdateCreditCardDetails.Response
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope { 

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
			
				return ns;
			}
			set { }
		}
	}

	public class Body { 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public UpdateCreditCardDetailsResponse updateCreditCardDetailsResponse { get; set; } 
	}

	public class UpdateCreditCardDetailsResponse { 
		public UpdateCreditCardDetailsResult updateCreditCardDetailsResult { get; set; } 
	}

	public class UpdateCreditCardDetailsResult { 
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public int Code { get; set; } 

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Message { get; set; } 
	}
}
