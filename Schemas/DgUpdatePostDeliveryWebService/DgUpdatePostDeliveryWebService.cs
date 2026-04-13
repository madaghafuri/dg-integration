namespace Terrasoft.Configuration.DgUpdatePostDeliveryWebService
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Xml.Serialization;
    using Core;
    using Core.DB;
    using Terrasoft.Web.Common;
	using ISAHttpRequest.ISAHttpRequest;
	using Newtonsoft.Json;
	using System.Linq;
	using System.Xml.Linq;
	using Request = DgIntegration.DgUpdatePostDelivery.Request;
	using Response = DgIntegration.DgUpdatePostDelivery.Response;
	using DgIntegration.DgUpdatePostDelivery;
 
    [XmlSerializerFormat]
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class UpdatePostDeliveryWebService: BaseService
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
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "UpdatePostDelivery")]
		public Response.Envelope UpdatePostDelivery(Stream Envelope)
		{
			var UpdatePostDeliveryService = new UpdatePostDeliveryService(SystemUserConnection);		
			return UpdatePostDeliveryService.Init(Envelope);
		}
	}
}