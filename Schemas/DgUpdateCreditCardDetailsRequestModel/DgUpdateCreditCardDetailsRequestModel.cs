using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgUpdateCreditCardDetails.Request
{
	[XmlRoot(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope
    {
        [XmlElement(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
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
		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
	}
	
	[XmlRoot(Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Body
    {
        [XmlElement(Namespace = "http://www.digi.com.my/")]
        public updateCreditCardDetails updateCreditCardDetails { get; set; }
    }
	
	[XmlRoot(Namespace = "http://www.digi.com.my/")]
    public class updateCreditCardDetails
    {
        [XmlElement(Namespace = "http://www.digi.com.my/")]
        public nccfUpdateRequest nccfUpdateRequest { get; set; }
		
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("digi", "http://www.digi.com.my/");

                return ns;
            }
            set { }
        }
    }
	
	[XmlRoot(Namespace = "http://www.digi.com.my/")]
    public class nccfUpdateRequest
    {
		public string ReferenceId { get; set; }
		public string CustomerType { get; set; }
		public string CustomerId { get; set; }
		public string CustomerName { get; set; }
		public string CustomerContact { get; set; }
		public string CustomerEmail { get; set; }
		public string TransactionType { get; set; }
		public string CardType { get; set; }
		public string CardOwner { get; set; }
		public string CardNo { get; set; }
		public string TokenId { get; set; }
		public string CardExpDate { get; set; }
		public string BankIssuer { get; set; }
		public string OwnershipType { get; set; }
		public string CreatedDate { get; set; }
		public string CustomerIdType { get; set; }
    }
}