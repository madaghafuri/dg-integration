namespace DgIntegration.DgAppleIntegrationService
{    
	using System;
	using System.Collections;
    using System.Collections.Generic;
	
	/**------------------------------
    ** Bulk Enroll
    **/
    public class RequestBulkEnrollDevices
    {
        public requestContext requestContext { get; set; }
        public string transactionId { get; set; }
        public string depResellerId { get; set; }
        public List<orders> orders { get; set; }
    }

    public class requestContext 
    {
        public string shipTo { get; set; }
        public string timeZone { get; set; }
        public string langCode { get; set; }
    }

    public class orders 
    {
        public string orderNumber { get; set; }
        public string orderDate { get; set; }
        public string orderType { get; set; }
        public string customerId { get; set; }
        public string poNumber { get; set; }
        public List<deliveries> deliveries { get; set; }
    }

    public class deliveries 
    {
        public string deliveryNumber { get; set; }
        public string shipDate { get; set; }
        public List<devices> devices { get; set; }
    }

    public class devices
    {
        public string deviceId { get; set; }
        public string assetTag { get; set; }
    }

    public class RequestVoidEnrollDevices
    {
        public requestContext requestContext { get; set; }
        public string transactionId { get; set; }
        public string depResellerId { get; set; }
        public List<ordersEnroll> orders { get; set; }
    }

    public class ordersEnroll 
    {
        public string orderNumber { get; set; }
        public string orderDate { get; set; }
        public string orderType { get; set; }
        public string customerId { get; set; }
        public string poNumber { get; set; }
    }

    public class ResponseBulkEnrollDevices
    {
		public string errorCode { get; set; }
		public string errorMessage { get; set; }
		public string transactionId { get; set; }
        public string deviceEnrollmentTransactionId { get; set; }
        public EnrollDevicesResponse enrollDevicesResponse { get; set; }
        public EnrollDeviceErrorResponse enrollDeviceErrorResponse { get; set; }
    }

    public class EnrollDevicesResponse 
    {
        public string statusCode { get; set; }
        public string statusMessage { get; set; }
    }

    public class EnrollDeviceErrorResponse 
    {
        public string errorMessage { get; set; }
        public string errorCode { get; set; }
    }

    /**------------------------------
    ** Show Order Details
    **/
    
    public class RequestShowOrderDetails
    {
        public requestContext requestContext { get; set; }
        public string depResellerId { get; set; }
        public List<string> orderNumbers { get; set; }
    }

    public class ResponseShowOrderDetails
    {
        public string statusCode { get; set; }
        public List<ordersShowOrderDetails> orders { get; set; }
        public string respondedOn { get; set; }
    }

    public class ordersShowOrderDetails
    {
        public string orderNumber { get; set; }
        public string showOrderStatusCode { get; set; }
        public string showOrderStatusMessage { get; set; }
    }

    /**------------------------------
    ** Check Transaction Status 
    **/
    
    public class RequestCheckTransactionStatus 
    {
        public requestContext requestContext { get; set; }
        public string depResellerId { get; set; }
        public string deviceEnrollmentTransactionId { get; set; }
    }
	
    public class ResponseCheckTransactionStatus 
    {
        public string deviceEnrollmentTransactionID { get; set; }
        public string statusCode { get; set; }
        public List<ordersCheckTransactionStatus> orders { get; set; }
        public List<checkTransactionErrorResponse> checkTransactionErrorResponse { get; set; }
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

    /**------------------------------
    ** Others Models
    **/

    public class DEPRegistrationDetail
	{
		public Guid DEPRegistrationId { get; set; }
		public string DeviceEnrollmentTransactionId { get; set; }
		public string TransactionNumber { get; set; }
		public string OrderType { get; set; }
		public Guid DEPRegistrationDetailId { get; set; }
		public string IMEINumber { get; set; }
		public string OrderNumber { get; set; }
	}

}