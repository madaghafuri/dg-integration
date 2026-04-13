namespace DgIntegration.DgCheckTransactionStatusService.Request
{    
	using System;
	using System.Collections;
    using System.Collections.Generic;
	
    public class requestContext 
    {
        public string shipTo { get; set; }
        public string timeZone { get; set; }
        public string langCode { get; set; }
    }

    public class RequestCheckTransactionStatus 
    {
        public requestContext requestContext { get; set; }
        public string depResellerId { get; set; }
        public string deviceEnrollmentTransactionId { get; set; }
    }
}