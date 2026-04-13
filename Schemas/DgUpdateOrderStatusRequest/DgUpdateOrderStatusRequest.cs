using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.UpdateOrderStatusRequest.Request
{
	[XmlRoot(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Password {
		[XmlAttribute(AttributeName="Type")]
		public string Type { get; set; }
		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(ElementName="UsernameToken", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class UsernameToken {
		[XmlElement(ElementName="Username", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public string Username { get; set; }
		[XmlElement(ElementName="Password", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Password Password { get; set; }
		[XmlElement(ElementName="Nonce", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public string Nonce { get; set; }
		[XmlElement(ElementName="Created", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Created { get; set; }
		[XmlAttribute(AttributeName="wsu", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsu { get; set; }
		[XmlAttribute(AttributeName="Id", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Id { get; set; }
	}

	[XmlRoot(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Security {
		[XmlElement(ElementName="UsernameToken", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public UsernameToken UsernameToken { get; set; }
		[XmlAttribute(AttributeName="wsse", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsse { get; set; }
	}

	[XmlRoot(ElementName="ReplyTo", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
	public class ReplyTo {
		[XmlElement(ElementName="Address", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Address { get; set; }
	}

	[XmlRoot(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Header {
		[XmlElement(ElementName="Security", Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Security Security { get; set; }
		[XmlElement(ElementName="To", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string To { get; set; }
		[XmlElement(ElementName="ReplyTo", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public ReplyTo ReplyTo { get; set; }
		[XmlElement(ElementName="MessageID", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string MessageID { get; set; }
		[XmlElement(ElementName="Action", Namespace="http://schemas.xmlsoap.org/ws/2004/08/addressing")]
		public string Action { get; set; }
		[XmlAttribute(AttributeName="wsa", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Wsa { get; set; }
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

	[XmlRoot(ElementName="order", Namespace="http://www.digi.com.my/")]
	public class Order {
		[XmlElement(ElementName="OrderId", Namespace="http://www.digi.com.my/")]
		public string OrderId { get; set; }
		[XmlElement(ElementName="OrderType", Namespace="http://www.digi.com.my/")]
		public string OrderType { get; set; }
		[XmlElement(ElementName="OrderStatus", Namespace="http://www.digi.com.my/")]
		public string OrderStatus { get; set; }
		[XmlElement(ElementName="StartDate", Namespace="http://www.digi.com.my/")]
		public string StartDate { get; set; }
		[XmlElement(ElementName="EndDate", Namespace="http://www.digi.com.my/")]
		public string EndDate { get; set; }
	}

	[XmlRoot(ElementName="CreatedId", Namespace="http://www.digi.com.my/")]
	public class CreatedId {
		[XmlElement(ElementName="CustomerId", Namespace="http://www.digi.com.my/")]
		public string CustomerId { get; set; }
		[XmlElement(ElementName="AccountId", Namespace="http://www.digi.com.my/")]
		public string AccountId { get; set; }
		[XmlElement(ElementName="SubscriberId", Namespace="http://www.digi.com.my/")]
		public string SubscriberId { get; set; }
	}

	[XmlRoot(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
	public class TaskRecord {
		[XmlElement(ElementName="TaskId", Namespace="http://www.digi.com.my/")]
		public string TaskId { get; set; }
		[XmlElement(ElementName="CorrelationId", Namespace="http://www.digi.com.my/")]
		public string CorrelationId { get; set; }
		[XmlElement(ElementName="TaskStatus", Namespace="http://www.digi.com.my/")]
		public string TaskStatus { get; set; }
		[XmlElement(ElementName="StartDate", Namespace="http://www.digi.com.my/")]
		public string StartDate { get; set; }
		[XmlElement(ElementName="EndDate", Namespace="http://www.digi.com.my/")]
		public string EndDate { get; set; }
		[XmlElement(ElementName="CreatedId", Namespace="http://www.digi.com.my/")]
		public CreatedId CreatedId { get; set; }
	}

	[XmlRoot(ElementName="taskList", Namespace="http://www.digi.com.my/")]
	public class TaskList {
		[XmlElement(ElementName="TaskRecord", Namespace="http://www.digi.com.my/")]
		public TaskRecord TaskRecord { get; set; }
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

	[XmlRoot(ElementName="UpdateOrderStatus", Namespace="http://www.digi.com.my/")]
	public class UpdateOrderStatus {
		[XmlElement(ElementName="request", Namespace="http://www.digi.com.my/")]
		public Request Request { get; set; }
		[XmlAttribute(AttributeName="digi", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Digi { get; set; }
	}

	[XmlRoot(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body {
		[XmlElement(ElementName="UpdateOrderStatus", Namespace="http://www.digi.com.my/")]
		public UpdateOrderStatus UpdateOrderStatus { get; set; }
	}

	[XmlRoot(ElementName="Envelope", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope {
		[XmlElement(ElementName="Header", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Header Header { get; set; }
		[XmlElement(ElementName="Body", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }
		[XmlAttribute(AttributeName="soapenv", Namespace="http://www.w3.org/2000/xmlns/")]
		public string Soapenv { get; set; }
	}

}
