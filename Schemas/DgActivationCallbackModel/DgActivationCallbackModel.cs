using System;
using System.Collections.Generic;

namespace DgIntegration.DgActivationCallbackService
{
	public class ActivationCallbackRequest
	{
		public Header Header { get; set; }
		public Order Order { get; set; }
		public List<TaskRecord> TaskList { get; set; }
	}
	
	public class ActivationCallbackResponse
	{
		public Header Header { get; set; }
		public Status Status { get; set; }
	}

	public class Header
	{
		public string ChannelId { get; set; }
		public string ReferenceId { get; set; }
		public string ChannelMedia { get; set; }
	}

	public class Order
	{
		public string OrderId { get; set; }
		public string OrderType { get; set; }
		public string OrderStatus { get; set; }
		public string OrderStatusDescription { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }
		public string Remark { get; set; }
	}

	public class TaskRecord
	{
		public string TaskId { get; set; }
		public string TaskStatus { get; set; }
		public string CorrelationId { get; set; }
		public string PortId { get; set; }
		public string StartDate { get; set; }
		public string EndDate { get; set; }
		public CreatedId CreatedId { get; set; }
		public List<TaskErrorRecord> TaskErrorList { get; set; }
	}

	public class CreatedId
	{
		public string SubscriberId { get; set; }
		public string AccountId { get; set; }
		public string CustomerId { get; set; }
	}

	public class TaskErrorRecord
	{
		public string ErrorCode { get; set; }
		public string ErrorDescription { get; set; }
	}
	
	public class Status
	{
		public string StatusCode { get; set; }
		public string ErrorCode { get; set; }
		public string ErrorDescription { get; set; }
	}
}