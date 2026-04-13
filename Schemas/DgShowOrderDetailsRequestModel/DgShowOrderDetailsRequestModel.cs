namespace DgIntegration.DgShowOrderDetailsService.Request
{    
	using System;
	using System.Collections;
    using System.Collections.Generic;
    using DgIntegration.DgEnrollDeviceService.Request;
	
	public class requestContext 
    {
        public string shipTo { get; set; }
        public string timeZone { get; set; }
        public string langCode { get; set; }
    }
	
    public class RequestShowOrderDetails
    {
        public requestContext requestContext { get; set; }
        public string depResellerId { get; set; }
        public List<string> orderNumbers { get; set; }
    }
}