using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Globalization;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCommonInventory
{
    public class UnreserveStock
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;
        private UnreserveStockRequest request;
        private UnreserveStockResponse response;

        public UnreserveStock(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "Common Inventory", "UnreserveStock", string.Empty);
            if(setup == null) {
                throw new Exception("Common Inventory: Unreserve Stock hasn't been set up for integration");
            }

            return setup;
        }

        public virtual UnreserveStockRequest GetParam(string ReservationId, string StoreId)
        {
            var requestBody = new requestBody()
            {
                reservationId = ReservationId,
                storeId = StoreId,
				userId = "NCCF"
            };

            var requestHeader = new requestHeader()
            {
                eventName = "StockUnreserveQuantity",
                sourceSystem = "NCCG"
            };

            var stockUnreserveQuantityRequest = new stockUnreserveQuantityRequest
            {
                requestBody = requestBody,
                requestHeader = requestHeader
            };

            var Param = new UnreserveStockRequest
            {
                stockUnreserveQuantityRequest = stockUnreserveQuantityRequest
            };

            return GetParam(Param);
        }

        public virtual UnreserveStockRequest GetParam(UnreserveStockRequest Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            if (string.IsNullOrEmpty(Param.stockUnreserveQuantityRequest.requestBody.reservationId))
            {
               throw new Exception("Reservation Id cannot be null or empty");
            }

            if (string.IsNullOrEmpty(Param.stockUnreserveQuantityRequest.requestBody.storeId))
            {
                throw new Exception("Store Id cannot be null or empty");
            }
            
            if(string.IsNullOrEmpty(Param.stockUnreserveQuantityRequest.requestBody.userId)) {
                throw new Exception("User Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.stockUnreserveQuantityRequest.requestHeader.eventName)) {
                throw new Exception("Event Name cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.stockUnreserveQuantityRequest.requestHeader.sourceSystem)) {
                throw new Exception("Source System cannot be null or empty");
            }

            return Param;
        }

        public virtual bool IsSuccess(UnreserveStockResponse Response)
        {
            if(Response == null) {
                return false;
            }
			
			var responseHeader = Response.stockUnreserveQuantityResponse.responseHeader;
			string statusCode = responseHeader.statusCode;
			string statusDescription = responseHeader.statusDescription;
			
			if(statusCode == "OK" && statusDescription == "Success") {
				return true;
			}
            
            return false;
        }

        public virtual string GetErrorResponse(UnreserveStockResponse Response)
        {
            if(Response == null) {
                return string.Empty;
            }

            var responseHeader = Response.stockUnreserveQuantityResponse.responseHeader;
            string statusCode = responseHeader.statusCode;
            string statusDescription = responseHeader.statusDescription;
            
            return $"{statusCode}: {statusDescription}";
        }

        public virtual string GetErrorResponse(string ResponseBody)
        {
            if(string.IsNullOrEmpty(ResponseBody)) {
                return string.Empty;
            }

            var settings = new JsonSerializerSettings {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            try {
                var authError = JsonConvert.DeserializeObject<UnauthorizedResponse>(ResponseBody, settings);
                return $"[{authError.fault.code}] {authError.fault.message}: {authError.fault.description}";
            } catch (Exception) {}

            try {
                var reqError = JsonConvert.DeserializeObject<ErrorResponse>(ResponseBody, settings);
                return $"{reqError.requestError.serviceException.messageId}: {reqError.requestError.serviceException.text}";
            } catch (Exception) {}

            return ResponseBody;
        }
    }
}