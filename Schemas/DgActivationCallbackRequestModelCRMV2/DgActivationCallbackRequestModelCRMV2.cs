using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgActivationCallbackService.CRMRequestV2
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
				ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
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

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string To { get; set; }

		[XmlElement(Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public ReplyTo ReplyTo { get; set; }

		[XmlElement(ElementName="MessageID", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string MessageID { get; set; }

		[XmlElement(ElementName="Action", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Action { get; set; }

		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("wsa", "http://schemas.xmlsoap.org/ws/2004/08/addressing");

                return ns;
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
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatus UpdateOrderStatus { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatus 
	{
		[XmlElement(ElementName="request", Namespace="http://www.digi.com.my/")]
		public request request { get; set; }

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

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class request 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public requestHeader requestHeader { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public order order { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public taskList taskList { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class requestHeader 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ReferenceId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class order 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderType { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderStatus { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string Remark { get; set; }
		
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string StartDate { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string EndDate { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class taskList 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public TaskRecords TaskRecord { get; set; }
	}

	[XmlRoot(ElementName = "TaskRecord", Namespace="http://www.digi.com.my/")]
	public class TaskRecords 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public List<TaskRecord> TaskRecord { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class TaskRecord 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string TaskId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string PortId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string CorrelationId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string TaskStatus { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string StartDate { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string EndDate { get; set; }
		
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public CreatedId CreatedId { get; set; }
	}

	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class CreatedId 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string CustomerId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string AccountId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string SubscriberId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string GroupId { get; set; }
	}
}
