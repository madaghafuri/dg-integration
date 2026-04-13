namespace DgIntegration.DgShowOrderDetailsService.Response
{    
	using System;
	using System.Collections;
    using System.Collections.Generic;
	 
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
}