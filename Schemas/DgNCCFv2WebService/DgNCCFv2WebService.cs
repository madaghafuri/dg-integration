using System;
using System.IO;
using System.Text;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using Terrasoft.Core;
using Terrasoft.Web.Common;
using DgIntegration.DgActivationCallbackService;
using CRMResponse = DgIntegration.DgActivationCallbackService.CRMResponse;
using CSGResponse = DgIntegration.DgActivationCallbackService.CSGResponse;
using DgIntegration.DgUpdateCreditCardDetails;
using UpdateCreditCardDetailsResponse = DgIntegration.DgUpdateCreditCardDetails.Response;
using DgIntegration.DgCreateCustomerSalesOrderResponse;
using DgIntegration.DgUpdateStatusDelivery;
using UpdateStatusDeliveryResponse = DgIntegration.DgUpdateStatusDelivery.Response;
using DgIntegration.DgUpdatePostDelivery;
using UpdatePostDeliveryResponse = DgIntegration.DgUpdatePostDelivery.Response;
using ISAHttpRequest.ISAHttpRequest;
using Newtonsoft.Json;

namespace Terrasoft.Configuration.DgNCCFv2
{
	public class RawContentTypeMapper : WebContentTypeMapper
    {
        public override WebContentFormat GetMessageFormatForContentType(string contentType)
        {
			bool isJson = contentType.Contains("text/json") || contentType.Contains("application/json");
			bool isXml = contentType.Contains("text/xml") || contentType.Contains("application/xml") || contentType.Contains("application/soap+xml");
            if (isJson || isXml) {
                return WebContentFormat.Raw;
            }
			
            return WebContentFormat.Default;
        }
    }

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class NCCFv2: BaseService 
    {
        private SystemUserConnection _systemUserConnection;
        private SystemUserConnection SystemUserConnection {
            get {
                return _systemUserConnection ?? (_systemUserConnection = (SystemUserConnection) AppConnection.SystemUserConnection);
            }
        }
		
		public NCCFv2()
		{
			SessionHelper.SpecifyWebOperationIdentity(HttpContextAccessor.GetInstance(), SystemUserConnection.CurrentUser);
		}
		
        #region CRM
            
        [OperationContract]
        [WebInvoke(Method = "POST")]
        public Stream UpdateOrderStatus(Stream request)
        {
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
			
            var service = new ActivationCallbackService(SystemUserConnection);
			CRMResponse.UpdateOrderStatusResponse result = service.Process<CRMResponse.UpdateOrderStatusResponse>(request);
			string xml = HTTPRequest.XmlToString<CRMResponse.UpdateOrderStatusResponse>(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return stream;
        }

        #endregion
		
        #region CSG
        
        [OperationContract]
        [WebInvoke(Method = "POST")]
        public Stream UpdateOrderStatusValidateCorpPortIn(Stream request)
        {
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
			
            var service = new ActivationCallbackService(SystemUserConnection);
			CSGResponse.Envelope result = service.Process<CSGResponse.Envelope>(request);
			string xml = HTTPRequest.XmlToString<CSGResponse.Envelope>(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return stream;
        }
		
		
        [OperationContract]
        [WebInvoke(Method = "POST")]
        public Stream UpdateCreditCardDetails(Stream request) 
        {
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
			
			var service = new UpdateCreditCardDetailsService(SystemUserConnection);
			UpdateCreditCardDetailsResponse.Envelope result = service.Process(request);
			string xml = HTTPRequest.XmlToString<UpdateCreditCardDetailsResponse.Envelope>(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return stream;
        }
		

        #endregion
		
        #region UERP
		
        [OperationContract]
        [WebInvoke(Method = "POST")]
        public Stream CreateCustomerSalesOrderResponse(Stream request)
        {
            return UERPCallback(request);
        }

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate="/CreateCustomerSalesOrderResponse/")]
        public Stream CreateCustomerSalesOrderResponseV2(Stream request)
        {
            return UERPCallback(request);
        }
		
		protected Stream UERPCallback(Stream request)
		{
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
			
			StreamReader reader = new StreamReader(request);
            string jsonReq = reader.ReadToEnd();
			
			var req = JsonConvert.DeserializeObject<DgIntegration.DgCreateCustomerSalesOrderResponse.Request>(jsonReq);
			
			var service = new CreateCustomerSalesOrderResponseService(SystemUserConnection);
            DgIntegration.DgCreateCustomerSalesOrderResponse.Response result = service.Init(req);
			string json = JsonConvert.SerializeObject(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return stream;
		}
            
        #endregion

        #region MMAG

        [OperationContract]
		[WebInvoke(Method = "POST")]
        public Stream UpdateStatusDelivery(Stream request)
        {
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
			
            var service = new UpdateStatusDeliveryService(SystemUserConnection);
			UpdateStatusDeliveryResponse.Envelope result = service.Process(request);
			string xml = HTTPRequest.XmlToString<UpdateStatusDeliveryResponse.Envelope>(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return stream;
        }

        [OperationContract]
		[WebInvoke(Method = "POST")]
        public Stream UpdatePostDelivery(Stream request)
        {
			WebOperationContext.Current.OutgoingResponse.ContentType = "application/xml; charset=utf-8";
			
            var service = new UpdatePostDeliveryService(SystemUserConnection);		
			UpdatePostDeliveryResponse.Envelope result = service.Init(request);
			string xml = HTTPRequest.XmlToString<UpdatePostDeliveryResponse.Envelope>(result);
			
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return stream;
        }
            
        #endregion
    }
}
