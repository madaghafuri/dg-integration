namespace DgIntegration.DgUERPCreateCustomerSalesOrderResponseService
{
    using System;
	using System.IO;
  	using System.Collections;
	using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using Terrasoft.Core;
    using Terrasoft.Web.Common;
    using Terrasoft.Core.Entities;
	using DgIntegration.DgCreateCustomerSalesOrderResponse;

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class UERPCreateCustomerSalesOrderResponseWebService : BaseService
    {
		private SystemUserConnection _systemUserConnection;
		private SystemUserConnection SystemUserConnection
		{
			get
			{
				return _systemUserConnection ?? (_systemUserConnection = (SystemUserConnection)AppConnection.SystemUserConnection);
			}
		}
        
        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        public Response UERPUpdate(Request Data) {
            return Update(Data);
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json, UriTemplate="/UERPUpdate/")]
        public Response UERPUpdateV2(Request Data) {
            return Update(Data);
        }
     	
		protected Response Update(Request Data) {
			var service = new CreateCustomerSalesOrderResponseService(SystemUserConnection);
            return service.Init(Data);
		}
    }
}