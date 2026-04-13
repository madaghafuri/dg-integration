namespace DgSubmission.DgSubmissionService
{
	using System;
    using System.Collections;
    using System.Collections.Generic;
	using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using Terrasoft.Common;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;
    using Terrasoft.Web.Common;
    using DocumentFormat.OpenXml;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;
    using DocumentFormat.OpenXml.ExtendedProperties;
    using Newtonsoft.Json;
	
	[ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class ECRAWebService: BaseService
    {
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public ECRAServiceResult Import(string ImportId) {
			var result = new ECRAServiceResult();
			result.UploadId = ImportId;
			
            var ecraService = new ECRAService(UserConnection, ImportId);
			
			string type = string.Empty;
			try {
				type = ecraService.GetECRAType();
				switch(type) {
					case "ecra":
						return ecraService.Import();

						break;

					case "dsms":
						var dsmsService = new ECRADSMSService(UserConnection, ImportId);
						return dsmsService.Import();

						break;

					case "m2m":
						var m2mService = new ECRAM2MService(UserConnection, ImportId);
						return m2mService.Import();

						break;

					case "eira":
						var eiraService = new EIRAService(UserConnection, ImportId);
						return eiraService.Import();

						break;

					case "dch":
						var dchService = new DCHService(UserConnection, ImportId);
						return dchService.Import();

						break;
					default:
						result.Message = "Document ECRA/EIRA is not supported by the system.";
						break;
				}

				result.Success = false;
			} catch(Exception e) {
				result.Message = e.Message;
			}
			
			return result;
        }
	
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public ECRAServiceResult ECRA(string ImportId) {
            var ecraService = new ECRAService(UserConnection, ImportId);
            return ecraService.Import();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public ECRAServiceResult DSMS(string ImportId) {
            var dsmsService = new ECRADSMSService(UserConnection, ImportId);
            return dsmsService.Import();

            return new ECRAServiceResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public ECRAServiceResult M2M(string ImportId) {
            var m2mService = new ECRAM2MService(UserConnection, ImportId);
            return m2mService.Import();

            return new ECRAServiceResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public ECRAServiceResult EIRA(string ImportId) {
            var eiraService = new EIRAService(UserConnection, ImportId);
            return eiraService.Import();
        }
	}
}