namespace DgIntegration.DgEnrollDeviceService.Response
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

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
}