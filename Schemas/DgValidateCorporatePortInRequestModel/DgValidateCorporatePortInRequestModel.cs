using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgValidateCorporatePortInService.Request
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
                ns.Add("wsa", "http://schemas.xmlsoap.org/ws/2004/08/addressing");
                ns.Add("xsd", "http://www.w3.org/2001/XMLSchema");
				ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

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

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Action { get; set; }

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string MessageID { get; set; }

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public ReplyTo ReplyTo { get; set; }

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string To { get; set; }
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

		[XmlAttribute]
		public string xmlns 
		{ 
			get
			{
				return "http://digi.com.my/";
			} 
			set {} 
		}
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
	public class ReplyTo 
	{
		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Address { get; set; }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public ValidateCorporatePortInRequest ValidateCorporatePortInRequest { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class ValidateCorporatePortInRequest 
	{
		[XmlElement(Namespace="http://digi.com.my/CorporateGroup")]
		public CorporateGroupId CorporateGroupId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public SubscriberList SubscriberList { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public MNPInformation MNPInformation { get; set; }

		[XmlAttribute]
		public string xmlns 
		{ 
			get
			{
				return "http://digi.com.my/";
			} 
			set {} 
		}
	}

	[XmlRoot(Namespace="http://digi.com.my/CorporateGroup")]
	public class CorporateGroupId 
	{
		[XmlAttribute]
		public string xmlns 
		{ 
			get 
			{
				return "http://digi.com.my/CorporateGroup";
			} 
			set {} 
		}

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class SubscriberList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public List<SubscriberRecord> SubscriberRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class SubscriberRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Subscriber")]
		public MSISDN MSISDN { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string MSISDNType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string NumberType { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class MSISDN 
	{
		[XmlAttribute]
		public string xmlns
		{ 
			get
			{
				return "http://digi.com.my/Subscriber";
			}
			set {} 
		}

		[XmlText]
		public string Text { get; set; }
		
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class MNPInformation 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string PortInTransactionId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string PortInMessageId { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DonorNetworkOperator { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string ReceivedNetworkOperator { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public CustomerType CustomerType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Account")]
		public AccountCode AccountCode { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public Individual Individual { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public Corporate Corporate { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class CustomerType 
	{
		[XmlAttribute]
		public string xmlns 
		{ 
			get
			{
				return "http://digi.com.my/Customer";
			}
			set {}  
		}

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class AccountCode 
	{
		[XmlAttribute]
		public string xmlns 
		{ 
			get
			{
				return "http://digi.com.my/Account";
			}
			set {}  
		}

		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class Individual 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public string CustomerName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public IdentificationList IdentificationList { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class IdentificationList 
	{
		[XmlElement(Namespace="http://digi.com.my/")]
		public IdentificationRecord IdentificationRecord { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class IdentificationRecord 
	{
		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public IdType IdType { get; set; }

		[XmlElement(Namespace="http://digi.com.my/Customer")]
		public IdNumber IdNumber { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class IdType 
	{
		[XmlAttribute]
		public string xmlns
		{ 
			get
			{
				return "http://digi.com.my/Customer";
			}
			set {} 
		}

		[XmlText]
		public string Text { get; set; }
		
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class IdNumber 
	{
		[XmlAttribute]
		public string xmlns
		{ 
			get
			{
				return "http://digi.com.my/Customer";
			}
			set {} 
		}

		[XmlText]
		public string Text { get; set; }
		
	}

	[XmlRoot(Namespace="http://digi.com.my/")]
	public class Corporate 
	{
		[XmlElement(Namespace="http://digi.com.my/Organization")]
		public CorporateName CorporateName { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string DonorBusinessRegistrationNumber { get; set; }

		[XmlElement(Namespace="http://digi.com.my/")]
		public string RecipientBusinessRegistrationNumber { get; set; }
	}

	[XmlRoot(Namespace="http://digi.com.my/Subscriber")]
	public class CorporateName 
	{
		[XmlAttribute]
		public string xmlns
		{ 
			get
			{
				return "http://digi.com.my/Organization";
			}
			set {} 
		}

		[XmlText]
		public string Text { get; set; }
	}
}