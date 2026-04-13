using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCommonInventory
{
    public class ReserveStock
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;

        public ReserveStock(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "Common Inventory", "ReserveStock", string.Empty);
            if(setup == null) {
                throw new Exception("Common Inventory: Reserve Stock hasn't been set up for integration");
            }

            return setup;
        }

        public virtual ReserveStockRequest GetParam(string StoreId, string Quantity, CheckStockResponse CheckStockResponse) 
        {
            var reservationId = GenerateReferenceId();
			var checkStockRes = CheckStockResponse.result.FirstOrDefault();
            var attributes = new List<attributes> {
                new attributes()
                {
                    Name = "BRAND",
                    Value = checkStockRes.manufacturerId
                },
                new attributes()
                {   
                    Name = "CATEGORY",
                    Value = checkStockRes.deviceTypeId
                },
                new attributes()
                {
                    Name = "COLOR",
                    Value = checkStockRes.color
                },
                new attributes()
                {
                    Name = "MODEL",
                    Value = checkStockRes.model
                },
                new attributes()
                {
                    Name = "PRODUCT",
                    Value = checkStockRes.inventoryItemTypeId
                }
            };

            var listOfItemDetailRequest = new listOfItemDetailRequest()
            {
                itemDetailRequest = new List<itemDetailRequest>()
                {
                    new itemDetailRequest()
                    {
                        listOfAttributes = new List<listOfAttributes>() {
							new listOfAttributes() {
								attributes = attributes
							}
						},
                        ProductType = checkStockRes.deviceTypeId,
                        Quantity = Quantity
                    }
                }
            };

            var stockReserveQuantityInput = new stockReserveQuantityInput()
            {
                storeId = StoreId,
                reservationId = reservationId,
                listOfItemDetailRequest = listOfItemDetailRequest
            };

            var Param = new ReserveStockRequest()
            {   
                stockReserveQuantityInput = stockReserveQuantityInput
            };

            return GetParam(Param);
        }

        public virtual ReserveStockRequest GetParam(ReserveStockRequest Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.stockReserveQuantityInput.storeId)) {
                throw new Exception("Store Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.stockReserveQuantityInput.reservationId)) {
                throw new Exception("Reservation Id cannot be null or empty");
            }

            if(Param.stockReserveQuantityInput.listOfItemDetailRequest == null ||
				Param.stockReserveQuantityInput.listOfItemDetailRequest.itemDetailRequest == null ||
				Param.stockReserveQuantityInput.listOfItemDetailRequest.itemDetailRequest.Count == 0) {
                throw new Exception("Request item detail cannot be null or empty");
            }

            return Param;
        }

        public virtual Dictionary<string, string> GetParamByRecordId(Guid RecordId)
        {			
            return BuildRequest(RecordId);
        }

        public virtual bool IsSuccess(ReserveStockResponse Response)
        {
            if(Response == null) {
                return false;
            }
			
			string status = Response.stockReserveQuantityOutput.status;
			string reservationStatus = Response
				.stockReserveQuantityOutput
				.listOfItemDetail
				.itemDetail
				.FirstOrDefault()
				.reservationStatus;
			
			if(status == "Success" && reservationStatus == "Success") {
				return true;
			}
            
            return false;
        }

        public virtual string GetErrorResponse(ReserveStockResponse Response)
        {
            if(Response == null) {
                return string.Empty;
            }

            string status = Response.stockReserveQuantityOutput.status;
            var itemDetail = Response
				.stockReserveQuantityOutput
				.listOfItemDetail
				.itemDetail
				.FirstOrDefault();
            string reservationStatus = itemDetail?.reservationStatus;

            return $"Status: {status}. Part Number: {itemDetail?.partNumber}. Category: {itemDetail?.category}. Brand {itemDetail?.brand}. Reservation Status: {reservationStatus}";
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

        protected virtual Dictionary<string, string> BuildRequest(Guid RecordId)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("ItemCode", esq.AddColumn("DgResModeID"));
            columns.Add("StoreId", esq.AddColumn("DgLineDetail.Dg3PLService.DgStoreID"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgFeeName", "Handset Fee"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.DgReleasedToIPL", 1));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.Id", RecordId));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            if(entity == null) {
                throw new Exception("This line does not have a device");
            }
            
            string itemCode = entity.GetTypedColumnValue<string>(columns["ItemCode"].Name);
            string storeId = entity.GetTypedColumnValue<string>(columns["StoreId"].Name);

            return new Dictionary<string, string>() {
                {"ItemCode", itemCode},
                {"StoreId", storeId},
                {"Quantity", "1"}
            };
        }

        public virtual string GenerateReferenceId()
        {
            try {
                long ticks = DateTime.Now.Ticks;
                string base36Ticks = Base36Encode(ticks);
                string base36Random = GenerateBase36Random(5);

                return "NCCF" + base36Ticks + base36Random;
            }
            catch(Exception e) {
                throw;
            }
        }

        private string Base36Encode(long input)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string result = string.Empty;
            while(input > 0)
            {
                result = chars[(int)(input % 36)] + result;
                input /= 36;
            }

            return result;
        }

        private string GenerateBase36Random(int length)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] randomBytes = new byte[length];
            RNGCryptoServiceProvider RngCsp = new RNGCryptoServiceProvider();
            RngCsp.GetBytes(randomBytes);

            char[] result = new char[length];
            for(int i = 0; i < length; i++)
            {
                result[i] = chars[randomBytes[i] % 36];
            }

            return new string(result);
        }
    }
}