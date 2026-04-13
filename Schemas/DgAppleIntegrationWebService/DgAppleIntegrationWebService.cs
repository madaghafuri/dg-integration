namespace DgIntegrationAPI.DgAppleIntegrationWebService
{
    using System;
  	using System.Collections;
	using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using Terrasoft.Core;
    using Terrasoft.Web.Common;
    using Terrasoft.Core.Entities;
	using System.Threading.Tasks;
	using DgBaseService.DgGenericResponse;
	using DgIntegration.DgAppleIntegrationService;

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class AppleIntegrationWebService: BaseService
    {				
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse RequestDeviceEnroll(Guid DEPRegistrationId, string OrderType, string TransactionNumber = "", Guid DEPRegistrationDetailId = default(Guid), bool isCelcom = false) {
            var result = new GeneralResponse();

            AppleIntegrationService AppleIntegrationService = new AppleIntegrationService(UserConnection, isCelcom);
            result = AppleIntegrationService.RequestDeviceEnroll(DEPRegistrationId, OrderType, TransactionNumber, DEPRegistrationDetailId).GetAwaiter().GetResult();

            return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse CheckTransactionStatus(Guid DEPRegistrationDetailId, bool isCelcom = false) {
            var result = new GeneralResponse();

            AppleIntegrationService AppleIntegrationService = new AppleIntegrationService(UserConnection, isCelcom);
            result = AppleIntegrationService.CheckTransactionStatus(DEPRegistrationDetailId).GetAwaiter().GetResult();

            return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse ShowOrderDetails(Guid DEPRegistrationDetailId, bool isCelcom = false) {
            var result = new GeneralResponse();

            AppleIntegrationService AppleIntegrationService = new AppleIntegrationService(UserConnection, isCelcom);
            result = AppleIntegrationService.ShowOrderDetails(DEPRegistrationDetailId).GetAwaiter().GetResult();

            return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse SubmissionDEP(string SerialNumber, string CustomerID) {
            var result = new GeneralResponse();

            AppleIntegrationService AppleIntegrationService = new AppleIntegrationService(UserConnection);
            result = AppleIntegrationService.SubmissionDEP(SerialNumber, CustomerID);

            return result;
        }
    }
}