namespace DgIntegration.DgEnrollDeviceService.Request
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

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
}