using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgActivationCallbackService.CRMRequestV1
{
	[XmlRoot(Namespace="http://www.digi.com.my/")]
	public class request 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public requestHeader requestHeader { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public order order { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public taskList taskList { get; set; }

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
	public class requestHeader 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ReferenceId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; }
	}

	[XmlRoot(ElementName="order", Namespace="http://www.digi.com.my/")]
	public class order 
	{
		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderType { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string OrderStatus { get; set; }

		[XmlElement(Namespace="http://www.digi.com.my/")]
		public string StartDate { get; set; }

		[XmlElement(ElementName="EndDate", Namespace="http://www.digi.com.my/")]
		public string EndDate { get; set; }
		
		[XmlElement(ElementName="Remark", Namespace="http://www.digi.com.my/")]
		public string Remark { get; set; }
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
	}
}