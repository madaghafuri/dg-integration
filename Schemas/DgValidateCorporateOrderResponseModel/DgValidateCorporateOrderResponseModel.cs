using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgValidateCorporateOrderService.Response
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
            set {}
        }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header 
	{
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

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidateCorporateOrderResponse ValidateCorporateOrderResponse { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidateCorporateOrderResponse 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidationResult ValidationResult { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string OrderId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CustomerList CustomerList { get; set; }

		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("soap", "http://schemas.xmlsoap.org/soap/envelope/");

                return ns;
            }
            set {}
        }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidationResult 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string ActionCode { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CustomerList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public CustomerRecord CustomerRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CustomerRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public CustomerId CustomerId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public CustomerCode CustomerCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public CustomerType CustomerType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public AddressList AddressList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateInfo CorporateInfo { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class CustomerId 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class CustomerCode 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class CustomerType 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
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
		[XmlElement(Namespace="http://digi.com.my/Address")]
		public AddressType AddressType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PrimaryAddress PrimaryAddress { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class AddressType 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class PrimaryAddress 
	{
		[XmlElement(Namespace="http://digi.com.my/Address")]
		public AddressLine1 AddressLine1 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public AddressLine2 AddressLine2 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public PostCode PostCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public City City { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public State State { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Address")]
		public Country Country { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class AddressLine1 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class AddressLine2 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class PostCode 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class City 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class State 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Address")]
	public class Country 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns5", "http://digi.com.my/Address");

                return ns;
            }
            set {}
        }

		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateInfo 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateDetails CorporateDetails { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public CorporateHierarchy CorporateHierarchy { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public PICInfosList PICInfosList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public AccountManagerInfo AccountManagerInfo { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string CorporateAccountValue { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateDetails 
	{
		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporateId CorporateId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporateName CorporateName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public BusinessRegistrationNumber BusinessRegistrationNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public BRNExpiryDate BRNExpiryDate { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporatePhoneNumber CorporatePhoneNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporateEmail CorporateEmail { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public IncorporationDate IncorporationDate { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public IndustrySegment IndustrySegment { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public GeographicDistribution GeographicDistribution { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string TelecomUsage { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public NumberOfEmployees NumberOfEmployees { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporateCustomerType CorporateCustomerType { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class CorporateId 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class CorporateName 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class BusinessRegistrationNumber 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class BRNExpiryDate 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class CorporatePhoneNumber 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class CorporateEmail 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class IncorporationDate 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class IndustrySegment 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class GeographicDistribution 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class NumberOfEmployees 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class CorporateCustomerType 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class CorporateHierarchy 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public TopParentCustomerId TopParentCustomerId { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class Hierarchy 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Organization")]
	public class TopParentCustomerId 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns4", "http://digi.com.my/Organization");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
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
		[XmlElement(Namespace="http://digi.com.my/")]
		public string SequenceNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Name Name { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Title Title { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Gender Gender { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Race Race { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public PhoneNumber PhoneNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Email Email { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string IsNotificationPerson { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string PicType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public IdType IdType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public IdNumber IdNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public Nationality Nationality { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Name 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Title 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Gender 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Race 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class PhoneNumber 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Email 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class IdType 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class IdNumber 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Customer")]
	public class Nationality 
	{
		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("ns2", "http://digi.com.my/Customer");

                return ns;
            }
            set {}
        }

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class AccountManagerInfo 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string Name2 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string PhoneNumber2 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string Email2 { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DealerCode { get; set; }
	}
}
