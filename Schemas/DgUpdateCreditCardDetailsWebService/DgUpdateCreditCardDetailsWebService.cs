namespace Terrasoft.Configuration.DgUpdateCreditCardDetailsWebService
{
	using System;
	using System.IO;
	using System.ServiceModel;
	using System.ServiceModel.Web;
	using System.ServiceModel.Activation;
	using Terrasoft.Core;
	using Terrasoft.Web.Common;
	using DgIntegration.DgUpdateCreditCardDetails;
	using ResponseModel = DgIntegration.DgUpdateCreditCardDetails.Response;

    [XmlSerializerFormat]
    [ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class UpdateCreditCardDetailsWebService: BaseService
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
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml)]
        public ResponseModel.Envelope UpdateCreditCardDetails(Stream Envelope) 
        {
			SessionHelper.SpecifyWebOperationIdentity(HttpContextAccessor.GetInstance(), SystemUserConnection.CurrentUser);
			
			var service = new UpdateCreditCardDetailsService(SystemUserConnection);
			return service.Process(Envelope);
        }
	}
}