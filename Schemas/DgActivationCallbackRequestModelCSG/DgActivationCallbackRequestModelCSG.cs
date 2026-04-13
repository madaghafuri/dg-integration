using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgActivationCallbackService.CSGRequest
{	
	public class RequestData
	{
		public Envelope Envelope { get; set; }
	}
	
	[XmlRoot(ElementName="Envelope", Namespace="http://www.w3.org/2003/05/soap-envelope")]
	public class Envelope {
		[XmlElement(ElementName="Body", Namespace="http://www.w3.org/2003/05/soap-envelope")]
		public Body Body { get; set; }
	}

	[XmlRoot(ElementName="Body", Namespace="http://www.w3.org/2003/05/soap-envelope")]
	public class Body {
		[XmlElement(ElementName="UpdateOrderStatus", Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatus UpdateOrderStatus { get; set; }
	}

	[XmlRoot(ElementName="UpdateOrderStatus", Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatus {
		[XmlElement(ElementName="request", Namespace="http://www.digi.com.my/")]
		public Request Request { get; set; }

		[XmlAttribute(AttributeName="digi1", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Digi1 { get; set; }
	}

	[XmlRoot(ElementName="request", Namespace="http://www.digi.com.my/")]
	public class Request {
		[XmlElement(ElementName="requestHeader", Namespace="http://www.digi.com.my/")]
		public RequestHeader RequestHeader { get; set; }

		[XmlElement(ElementName="order", Namespace="http://www.digi.com.my/")]
		public Order Order { get; set; }

		[XmlElement(ElementName="taskList", Namespace="http://www.digi.com.my/")]
		public TaskList TaskList { get; set; }
	}

	[XmlRoot(ElementName="taskList", Namespace="http://www.digi.com.my/")]
	public class TaskList {
		[XmlElement(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
		public TaskRecord1 TaskRecord { get; set; }
	}

	[XmlRoot(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
	public class TaskRecord1 {
		[XmlElement(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
		public List<TaskRecord> TaskRecord { get; set; }
	}

	[XmlRoot(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
	public class TaskRecord {
		[XmlElement(ElementName="TaskId", Namespace="http://www.digi.com.my/")]
		public string TaskId { get; set; }

		[XmlElement(ElementName="PortId", Namespace="http://www.digi.com.my/")]
		public string PortId { get; set; }

		[XmlElement(ElementName="CorrelationId", Namespace="http://www.digi.com.my/")]
		public string CorrelationId { get; set; }

		[XmlElement(ElementName="TaskStatus", Namespace="http://www.digi.com.my/")]
		public string TaskStatus { get; set; }
		
		[XmlElement(ElementName="TaskErrorList", Namespace="http://www.digi.com.my/")]
		public TaskErrorList TaskErrorList { get; set; }
	}
	
	[XmlRoot(ElementName="TaskErrorList", Namespace="http://www.digi.com.my/")]
	public class TaskErrorList {
		[XmlElement(ElementName="TaskErrorRecord", Namespace="http://www.digi.com.my/")]
		public List<TaskErrorRecord> TaskErrorRecord { get; set; }
	}
	
	[XmlRoot(ElementName="TaskErrorRecord", Namespace="http://www.digi.com.my/")]
	public class TaskErrorRecord {
		[XmlElement(ElementName="ErrorCode", Namespace="http://www.digi.com.my/")]
		public string ErrorCode { get; set; }

		[XmlElement(ElementName="ErrorDescription", Namespace="http://www.digi.com.my/")]
		public string ErrorDescription { get; set; }
	}

	[XmlRoot(ElementName="order", Namespace="http://www.digi.com.my/")]
	public class Order {
		[XmlElement(ElementName="OrderId", Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; }

		[XmlElement(ElementName="OrderType", Namespace="http://www.digi.com.my/")]
		public string OrderType { get; set; }

		[XmlElement(ElementName="OrderStatus", Namespace="http://www.digi.com.my/")]
		public string OrderStatus { get; set; }
		
		[XmlElement(ElementName="OrderStatusDescription", Namespace="http://www.digi.com.my/")]
		public string OrderStatusDescription { get; set; }
	}

	[XmlRoot(ElementName="requestHeader", Namespace="http://www.digi.com.my/")]
	public class RequestHeader {
		[XmlElement(ElementName="ReferenceId", Namespace="http://www.digi.com.my/")]
		public string ReferenceId { get; set; }

		[XmlElement(ElementName="ChannelId", Namespace="http://www.digi.com.my/")]
		public string ChannelId { get; set; }

		[XmlElement(ElementName="ChannelMedia", Namespace="http://www.digi.com.my/")]
		public string ChannelMedia { get; set; }
	}
}
