using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Core;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;

namespace DgSubmission.DgSubmissionService
{
	[ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SubmissionWebService: BaseService
    {
		private SubmissionService service;
		public SubmissionWebService()
		{
			this.service = new SubmissionService(UserConnection);
		}
	
		[OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/New", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        public SubmitResponse NewSubmission(SubmitRequest Request) 
		{			
			try {
				RequestValidation(Request);
			} catch(Exception e) {
				return new SubmitResponse() {
					Message = e.Message
				};
			}
			
            return this.service.Submission(Request);
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Resubmission", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare, ResponseFormat = WebMessageFormat.Json)]
        public SubmitResponse Resubmission(SubmitRequest Request) 
		{
			try {
				RequestValidation(Request);
			} catch(Exception e) {
				return new SubmitResponse() {
					Message = e.Message
				};
			}
			
            return this.service.ReSubmission(Request);
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/Pullback", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        public SubmitResponse Pullback(string SerialNumber) 
		{
            return this.service.Pullback(SerialNumber);
        }
		
		protected void RequestValidation(SubmitRequest Request)
		{
			if(Request == null) {
				throw new Exception("Request to Submission is null or empty");
			}
			
			if(Request.CRMGroup == null) {
				throw new Exception("CRMGroup parameter is null or empty");
			}
			
			if(Request.Submission == null) {
				throw new Exception("Submission parameter is null or empty");
			}
			
			if(Request.LineDetails == null || (Request.LineDetails != null && Request.LineDetails.Count == 0)) {
				throw new Exception("LineDetails parameter is null or empty");
			}
			
			if(Request.LineDetails.Count > 25) {
				throw new Exception("Line Registration maximum 25");
			}
		}
	}
}