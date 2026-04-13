using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgSubmitNewCorpCustomerOrderService.Request
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
                ns.Add("acc", "http://digi.com.my/Account");
				ns.Add("add", "http://digi.com.my/Address");
				ns.Add("bil", "http://digi.com.my/BillingProfile");
				ns.Add("cor", "http://digi.com.my/CorporateGroup");
				ns.Add("cus", "http://digi.com.my/Customer");
				ns.Add("digi", "http://digi.com.my/");
				ns.Add("fees", "http://digi.com.my/Fees");
				ns.Add("org", "http://digi.com.my/Organization");
				ns.Add("pay", "http://digi.com.my/PaymentData");
				ns.Add("sub", "http://digi.com.my/Subscriber");

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
		public SubmitNewCorpCustomerOrderRequest SubmitNewCorpCustomerOrderRequest { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class SubmitNewCorpCustomerOrderRequest 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidationResult ValidationResult { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string OrderId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string IsRequirePaymentCollection { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateCustomer CorporateCustomer { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateGroup CorporateGroup { get; set; }
		
		[XmlElement(Namespace="http://digi.com.my/")]
		public Dealer Dealer { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidationResult 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string ActionCode { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateCustomer 
	{
		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string CorporateName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string BusinessRegistrationNumber { get; set; }
		
		[XmlElement(Namespace="http://digi.com.my/")]
		public string Tin { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string SST { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string BRNExpiryDate { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string CorporatePhoneNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string CorporateEmail { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string IncorporationDate { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string IndustrySegment { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string BusinessNature { get; set; }
		public bool ShouldSerializeBusinessNature()
		{
			return !string.IsNullOrWhiteSpace(BusinessNature);
		}

		[XmlElement(Namespace = "http://digi.com.my/")]
		public string TelecomUsage { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string CorporateCustomerType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateHierarchy CorporateHierarchy { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PICInfosList PICInfosList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public AccountManagerInfo AccountManagerInfo { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public AddressList AddressList { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateHierarchy 
	{
		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string Hierarchy { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string TopParentCustomerId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public string ParentCustomerId { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PICInfosList
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public List<PICInfosRecord> PICInfosRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PICInfosRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Name { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Race { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string PhoneNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Email { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string IsNotificationPerson { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string PicType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string IdType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string IdNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Nationality { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class AccountManagerInfo 
	{
		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Name { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string PhoneNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public string Email { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerCode { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class AddressList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public List<AddressRecord> AddressRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class AddressRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string AddressType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PrimaryAddress PrimaryAddress { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PrimaryAddress 
	{
		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string AddressLine1 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string AddressLine2 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string PostCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string City { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string State { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public string Country { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateGroup 
	{
		[XmlElement(Namespace="http://digi.com.my/CorporateGroup")]
		public string CorporateGroupName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/CorporateGroup")]
		public string CorporateGroupType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PICInfosList PICInfosList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public Account Account { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PrimaryOffering PrimaryOffering { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public SuppOffList SuppOffList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public FeesList FeesList { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class Account 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public NewAccount NewAccount { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class NewAccount 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public BillMediumList BillMediumList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Account")]
		public string AccountName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Account")]
		public string Email { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string SMSNotificationMSISDN { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public AddressList AddressList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PaymentModeInfo PaymentModeInfo { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class BillMediumList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public List<string> BillMedium { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PaymentModeInfo 
	{
		[XmlElement(Namespace="http://digi.com.my/PaymentData")]
		public string PaymentMode { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PrimaryOffering 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string OfferId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string OfferName { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class SuppOffList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public List<SuppOffRecord> SuppOffRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class SuppOffRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string OfferId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string OfferName { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class Dealer 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerUserId { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Fees")]
	public class FeesList 
	{
		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public FeesRecord FeesRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Fees")]
	public class FeesRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string FeeType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string FeeItemCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string FeeAmount { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string OriginalFeeAmount { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string PaymentType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string OFSCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public Waive Waive { get; set; }
		
		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public TaxList TaxList { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Fees")]
	public class Waive 
	{
		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string ManualWaiveAmount { get; set; }
	}
	
	[XmlRoot(Namespace="http://digi.com.my/Fees")]
	public class TaxList 
	{
		 [XmlElement("TaxRecord", Namespace = "http://digi.com.my/Fees")]
    	public List<TaxRecord> TaxRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Fees")]
	public class TaxRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string TaxCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string TaxName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Fees")]
		public string TaxAmount { get; set; }
	}
}
