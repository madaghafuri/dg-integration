using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgValidateCorporateOrderService.Request
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
                ns.Add("digi", "http://digi.com.my/");
				ns.Add("cor", "http://digi.com.my/CorporateGroup");
                ns.Add("acc", "http://digi.com.my/Account");
				ns.Add("org", "http://digi.com.my/Organization");
				ns.Add("cus", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header 
	{
		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
		
		[XmlElement(Namespace="http://digi.com.my/")]
		public CSGHeader CSGHeader { get; set; }
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
		public string BusinessUnit { get; set; }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidateCorporateOrderRequest ValidateCorporateOrderRequest { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidateCorporateOrderRequest 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string OrderType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidateCriteria ValidateCriteria { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public Dealer Dealer { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidateCriteria 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public CreateCorporateCustomer CreateCorporateCustomer { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CreateCorporateCustomer 
	{
		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string BusinessRegistrationNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string Hierarchy { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string IdType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string IdNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Nationality { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class Dealer
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerUserId { get; set; }
	}
}