namespace DgIntegration.DgCheckTransactionStatusService.Response
{    
	using System;
	using System.Collections;
    using System.Collections.Generic;
	
    public class ResponseCheckTransactionStatus 
    {
        public string deviceEnrollmentTransactionID { get; set; }
        public string statusCode { get; set; }
        public List<ordersCheckTransactionStatus> orders { get; set; }
        public List<checkTransactionErrorResponse> checkTransactionErrorResponse { get; set; }
        public List<checkTransactionErrorResponse> enrollDeviceErrorResponse { get; set; }
        public string completedOn { get; set; }
        public string transactionId { get; set; }
		public string errorCode { get; set; }
		public string errorMessage { get; set; }
    }

    public class checkTransactionErrorResponse
    {
        public string errorMessage { get; set; }
        public string errorCode { get; set; }
    }

    public class ordersCheckTransactionStatus
    {
        public string orderNumber { get; set; }
        public string orderPostStatus { get; set; }
        public string orderPostStatusMessage { get; set; }
        public List<deliveriesCheckTransaction> deliveries { get; set; }
    }

    public class deliveriesCheckTransaction
    {
        public string deliveryNumber { get; set; }
        public string deliveryPostStatus { get; set; }
        public string deliveryPostStatusMessage { get; set; }
        public List<devicesCheckTransaction> devices { get; set; }
    }

    public class devicesCheckTransaction
    {
        public string deviceId { get; set; }
        public string devicePostStatus { get; set; }
        public string devicePostStatusMessage { get; set; }
    }
}