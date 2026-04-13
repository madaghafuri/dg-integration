using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgMMAGOrderCreateService.Request
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
		public OrderCreate OrderCreate { get; set; }
	}

	public class OrderCreate 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public Order Order { get; set; }

		[XmlAttribute(AttributeName="xmlns")]
		public string xmlns 
        { 
            get {
				return "https://csg.mmag.com.my/rest/digi/nccf/";
			} 
			set {}
        }
	}

	public class Order 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public Customers Customers { get; set; }

		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public Items Items { get; set; }

		[XmlAttribute]
		public string OrderID { get; set; }

		[XmlAttribute]
		public string CenterID { get; set; }

		[XmlAttribute]
		public string ChannelID { get; set; }

		[XmlAttribute]
		public string RegionCode { get; set; }

		[XmlAttribute]
		public string OrderType { get; set; }

		[XmlAttribute]
		public string OrderDT { get; set; }

		[XmlAttribute]
		public string DeliveryDT { get; set; }

		[XmlAttribute]
		public string CollectDoc { get; set; }

		[XmlAttribute]
		public string Remarks { get; set; }

		[XmlAttribute]
		public string LeadsID { get; set; }
	}

	public class Customers 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public Customer Customer { get; set; }
	}

	public class Customer 
    {
		[XmlAttribute]
		public string Name { get; set; }

		[XmlAttribute]
		public string Address { get; set; }

		[XmlAttribute]
		public string City { get; set; }

		[XmlAttribute]
		public string State { get; set; }

		[XmlAttribute]
		public string PostCode { get; set; }

		[XmlAttribute]
		public string Country { get; set; }

		[XmlAttribute]
		public string AuthorizePerson1 { get; set; }

		[XmlAttribute]
		public string AP1_ICNo { get; set; }

		[XmlAttribute]
		public string AP1_OfficeNo { get; set; }

		[XmlAttribute]
		public string AP1_MobileNo { get; set; }

		[XmlAttribute]
		public string AuthorizePerson2 { get; set; }

		[XmlAttribute]
		public string AP2_ICNo { get; set; }

		[XmlAttribute]
		public string AP2_OfficeNo { get; set; }

		[XmlAttribute]
		public string AP2_MobileNo { get; set; }

		[XmlAttribute]
		public string SalesPerson { get; set; }

		[XmlAttribute]
		public string ContactPrimary { get; set; }

		[XmlAttribute]
		public string CP1_ICNo { get; set; }

		[XmlAttribute]
		public string CP1_OfficeNo { get; set; }

		[XmlAttribute]
		public string CP1_MobileNo { get; set; }

		[XmlAttribute]
		public string ContactSecondary { get; set; }

		[XmlAttribute]
		public string CS_ICNo { get; set; }

		[XmlAttribute]
		public string CS_OfficeNo { get; set; }

		[XmlAttribute]
		public string CS_MobileNo { get; set; }
	}
    
	public class Items 
    {
		[XmlElement(Namespace="https://csg.mmag.com.my/rest/digi/nccf/")]
		public List<Item> Item { get; set; }
	}

	public class Item 
    {
		[XmlAttribute]
		public string ItemCode { get; set; }

		[XmlAttribute]
		public string ItemDesc { get; set; }

		[XmlAttribute]
		public string Quantity { get; set; }

		[XmlAttribute]
		public string SimType { get; set; }

		[XmlAttribute]
		public string PackageCode { get; set; }

		[XmlAttribute]
		public string PromoCode { get; set; }

		[XmlAttribute]
		public string RatePlan { get; set; }

		[XmlAttribute]
		public string MSISDN { get; set; }

		[XmlAttribute]
		public string PackagePrice { get; set; }

		[XmlAttribute]
		public string NCCFLineID { get; set; }
	}
}