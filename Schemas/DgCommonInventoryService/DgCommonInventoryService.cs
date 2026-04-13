using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService;
using DgSubmission.DgHistorySubmissionService;
using ISAIntegrationSetup;
using LookupConst = DgMasterData.DgLookupConst;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using SolarisCore;

namespace DgIntegration.DgCommonInventory
{
    public class CommonInventoryService : BaseHttpRequest
    {
		private int delayInSecond = 1;
        public CommonInventoryService(UserConnection UserConnection) : base(UserConnection, BaseHttpRequest.GetBaseUrl(UserConnection, "Common Inventory"))
        {
            this.UserConnection = UserConnection;
        }

        public async Task<TokenSuccessResponse> GetToken(string ClientId, string ClientSecret, string GrantType)
        {
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: Get Token",
                Section = "Common Inventory"
            };
            var token = new Token(UserConnection);
            var data = token.GetParam(ClientId, ClientSecret, GrantType).ToDictionary();

            var request = new HttpRequestMessage(HttpMethod.Post, token.EndpointUrl);
            request.Headers.Add("Accept", "application/json");
            request.Content = new FormUrlEncodedContent(data);
            
			var response = await SendRequest<TokenSuccessResponse>(request, logInfo);
            if(!response.IsSuccess && (response.StatusCode >= 400 && response.StatusCode < 500)) {
                throw new Exception(response.RawBody);
            }
            
            if(string.IsNullOrEmpty(response.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(response.RawBody) && response.Body == null) {
                string errorResponse = $"{response.Message} - {response.StatusCode}: {response.StatusDescription}";
                try {
                    errorResponse = token.GetErrorResponse(response.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }

            return response.Body;
        }

        public async Task<TokenSuccessResponse> GetToken(bool IsRefresh = false)
        {
			var token = new Token(UserConnection);
            string cacheToken = token.GetCacheToken();
            bool isExists = string.IsNullOrEmpty(cacheToken) ? false : true;
            if(isExists && !IsRefresh) {
                return new TokenSuccessResponse() {
                    access_token = cacheToken
                };
            }
            
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: Get Token",
                Section = "Common Inventory"
            };
            
            var data = token.GetParam().ToDictionary();

            var request = new HttpRequestMessage(HttpMethod.Post, token.EndpointUrl);
            request.Headers.Add("Accept", "application/json");
            request.Content = new FormUrlEncodedContent(data);
            
			var response = await SendRequest<TokenSuccessResponse>(request, logInfo);
            
            if(string.IsNullOrEmpty(response.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(response.RawBody) && (response.Body == null)) {
                string errorResponse = $"{response.Message} - {response.StatusCode}: {response.StatusDescription}";
                try {
                    errorResponse = token.GetErrorResponse(response.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }
			
			token.UpdateCacheToken(response.Body.access_token);

            return response.Body;
        }

        #region CheckStock
            
        public async Task<CheckStockResponse> CheckStock(string ItemLocationId, string ItemCode, string UserId = "NCCF")
        {
            var checkStock = new CheckStock(UserConnection);
            var param = new CheckStockRequest() {
                itemLocationId = ItemLocationId,
                itemCode = ItemCode,
                userId = UserId
            };
            return await CheckStock(checkStock, checkStock.GetParam(param));
        }

        public async Task<CheckStockResponse> CheckStock(CheckStockRequest Param)
        {
            var checkStock = new CheckStock(UserConnection);
            return await CheckStock(checkStock, checkStock.GetParam(Param));
        }

        public async Task<CheckStockResponse> CheckStock(Guid RecordId)
        {
            var checkStock = new CheckStock(UserConnection);
            return await CheckStock(checkStock, checkStock.GetParam(RecordId), RecordId);
        }

        public async Task<CheckStockResponse> CheckStock(CheckStock CheckStock, CheckStockRequest Param, Guid RecordId = default(Guid))
        {
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: Check Stock",
                Section = "Common Inventory"
            };

            if(RecordId != Guid.Empty) {
                logInfo.Section += " (DgLineDetail)";
                logInfo.RecordId = RecordId.ToString();
            }

            bool isAuth = false;
            bool refreshToken = false;
            int incrementAuth = 0;

            var res = new Response<CheckStockResponse>();
            while (incrementAuth <= 2) {
                var token = await GetToken(refreshToken);
                if(refreshToken) {
                    await Task.Delay(delayInSecond * 1000);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, CheckStock.EndpointUrl);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", "Bearer "+token.access_token);
                request.Content = ConvertToStringContent(Param, SolarRest.JSON);

                var response = await SendRequest<CheckStockResponse>(request, logInfo);
                if(response.StatusCode == 401) {
                    refreshToken = true;
                    incrementAuth++;
                    await Task.Delay(delayInSecond * 1000);

                    continue;
                }

                isAuth = true;
                res = response;

                break;
            }

            if(string.IsNullOrEmpty(res.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(res.RawBody) && (res.Body == null || res.Body?.result == null)) {
                string errorResponse = $"{res.Message} - {res.StatusCode}: {res.StatusDescription}";
                try {
                    errorResponse = CheckStock.GetErrorResponse(res.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }

            return res.Body;
        }

        #endregion

        #region Reserve
            
        public async Task<ReserveStockResponseV2> Reserve(string StoreId, string Quantity, CheckStockResponse CheckStockResponse)
        {
            var reserve = new ReserveStock(UserConnection);
            return await Reserve(reserve, reserve.GetParam(StoreId, Quantity, CheckStockResponse));
        }

        public async Task<ReserveStockResponseV2> Reserve(ReserveStockRequest Param)
        {
            var reserve = new ReserveStock(UserConnection);
            return await Reserve(reserve, reserve.GetParam(Param));
        }

        public async Task<ReserveStockResponseV2> Reserve(Guid RecordId)
        {
            var reserve = new ReserveStock(UserConnection);
            var param = reserve.GetParamByRecordId(RecordId);

            var checkStock = await CheckStock(param["StoreId"], param["ItemCode"]);
            var reserveParam = reserve.GetParam(param["StoreId"], param["Quantity"], checkStock);

            return await Reserve(reserve, reserveParam, RecordId);
        }

        public async Task<ReserveStockResponseV2> Reserve(ReserveStock Reserve, ReserveStockRequest Param, Guid RecordId = default(Guid))
        {
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: Reserve Stock",
                Section = "Common Inventory"
            };

            if(RecordId != Guid.Empty) {
                logInfo.Section += " (DgLineDetail)";
                logInfo.RecordId = RecordId.ToString();
            }

            bool isAuth = false;
            bool refreshToken = false;
            int incrementAuth = 0;

            var res = new Response<ReserveStockResponse>();
            while (incrementAuth <= 2) {
                var token = await GetToken(refreshToken);
				if(refreshToken) {
                    await Task.Delay(delayInSecond * 1000);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, Reserve.EndpointUrl);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", "Bearer "+token.access_token);
                request.Content = ConvertToStringContent(Param, SolarRest.JSON);

                var response = await SendRequest<ReserveStockResponse>(request, logInfo);
                if(response.StatusCode == 401) {
                    refreshToken = true;
                    incrementAuth++;
					await Task.Delay(delayInSecond * 1000);
					
                    continue;
                }

                isAuth = true;
                res = response;

                break;
            }

            if(string.IsNullOrEmpty(res.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(res.RawBody) && (res.Body == null || res.Body?.stockReserveQuantityOutput == null)) {
                string errorResponse = $"{res.Message} - {res.StatusCode}: {res.StatusDescription}";
                try {
                    errorResponse = Reserve.GetErrorResponse(res.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }

            if(!Reserve.IsSuccess(res.Body)) {
                throw new Exception(Reserve.GetErrorResponse(res.Body));
            }

            return new ReserveStockResponseV2() {
                ReservationID = Param.stockReserveQuantityInput.reservationId,
                Response = res.Body
            };
        }

        #endregion

        #region Unreserve
            
        public async Task<UnreserveStockResponse> Unreserve(string ReservationId, string StoreId)
        {
            var unreserve = new UnreserveStock(UserConnection);
            return await Unreserve(unreserve, unreserve.GetParam(ReservationId, StoreId));
        }

        public async Task<UnreserveStockResponse> Unreserve(UnreserveStockRequest Param)
        {
            var unreserve = new UnreserveStock(UserConnection);
            return await Unreserve(unreserve, unreserve.GetParam(Param));
        }

        public async Task<UnreserveStockResponse> Unreserve(UnreserveStock Unreserve, UnreserveStockRequest Param)
        {
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: Unreserve Stock",
                Section = "Common Inventory"
            };

            bool isAuth = false;
            bool refreshToken = false;
            int incrementAuth = 0;

            var res = new Response<UnreserveStockResponse>();
            while (incrementAuth <= 2) {
                var token = await GetToken(refreshToken);
				if(refreshToken) {
                    await Task.Delay(delayInSecond * 1000);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, Unreserve.EndpointUrl);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", "Bearer "+token.access_token);
                request.Content = ConvertToStringContent(Param, SolarRest.JSON);

                var response = await SendRequest<UnreserveStockResponse>(request, logInfo);
                if(response.StatusCode == 401) {
                    refreshToken = true;
                    incrementAuth++;
					await Task.Delay(delayInSecond * 1000);
					
                    continue;
                }

                isAuth = true;
                res = response;

                break;
            }

            if(string.IsNullOrEmpty(res.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(res.RawBody) && (res.Body == null || res.Body?.stockUnreserveQuantityResponse == null)) {
                string errorResponse = $"{res.Message} - {res.StatusCode}: {res.StatusDescription}";
                try {
                    errorResponse = Unreserve.GetErrorResponse(res.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }

            if(!Unreserve.IsSuccess(res.Body)) {
                throw new Exception(Unreserve.GetErrorResponse(res.Body));
            }

            return res.Body;
        }

        #endregion

        #region CreateDeliveryOrder
            
        public async Task<CreateDeliveryResponse> CreateDeliveryOrder(string SONumber)
        {
            var createDelivery = new CreateDeliveryOrder(UserConnection);
            return await CreateDeliveryOrder(createDelivery, createDelivery.GetParam(SONumber));
        }

        public async Task<CreateDeliveryResponse> CreateDeliveryOrder(string ReservationID, string StoreID)
        {
            var createDelivery = new CreateDeliveryOrder(UserConnection);
            return await CreateDeliveryOrder(createDelivery, createDelivery.GetParam(ReservationID, StoreID));
        }

        public async Task<CreateDeliveryResponse> CreateDeliveryOrder(Guid RecordId)
        {
            var createDelivery = new CreateDeliveryOrder(UserConnection);
            return await CreateDeliveryOrder(createDelivery, createDelivery.GetParam(RecordId), RecordId);
        }

        public async Task<CreateDeliveryResponse> CreateDeliveryOrder(CreateDeliveryRequest Param)
        {
            var createDelivery = new CreateDeliveryOrder(UserConnection);
            return await CreateDeliveryOrder(createDelivery, createDelivery.GetParam(Param));
        }

        public async Task<CreateDeliveryResponse> CreateDeliveryOrder(CreateDeliveryOrder CreateDeliveryOrder, CreateDeliveryRequest Param, Guid RecordId = default(Guid))
        {
            var logInfo = new LogInfo() {
                LogName = "Common Inventory: CreateDeliveryOrder",
                Section = "Common Inventory"
            };

            if(RecordId != Guid.Empty) {
                logInfo.Section += " (DgLineDetail)";
                logInfo.RecordId = RecordId.ToString();
            }

            bool isAuth = false;
            bool refreshToken = false;
            int incrementAuth = 0;

            var res = new Response<CreateDeliveryResponse>();
            while (incrementAuth <= 2) {
                var token = await GetToken(refreshToken);
				if(refreshToken) {
                    await Task.Delay(delayInSecond * 1000);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, CreateDeliveryOrder.EndpointUrl);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", "Bearer "+token.access_token);
                request.Content = ConvertToStringContent(Param, SolarRest.JSON);

                var response = await SendRequest<CreateDeliveryResponse>(request, logInfo);
                if(response.StatusCode == 401) {
                    refreshToken = true;
                    incrementAuth++;
					await Task.Delay(delayInSecond * 1000);
					
                    continue;
                }

                isAuth = true;
                res = response;

                break;
            }

            if(string.IsNullOrEmpty(res.RawBody)) {
                throw new Exception("Response is empty");
            }

            if(!string.IsNullOrEmpty(res.RawBody) && (res.Body == null || res.Body?.responseBody == null)) {
                string errorResponse = $"{res.Message} - {res.StatusCode}: {res.StatusDescription}";
                try {
                    errorResponse = CreateDeliveryOrder.GetErrorResponse(res.RawBody);
                } catch (Exception) {}

                throw new Exception(errorResponse);
            }

            if(!CreateDeliveryOrder.IsSuccess(res.Body)) {
                throw new Exception(CreateDeliveryOrder.GetErrorResponse(res.Body));
            }

            return res.Body;
        }

        #endregion

        #region Cancel IMS
        
        public async Task<ResultStatus> CancelIMS(Guid SubmissionId)
        {
            var result = new ResultStatus();
            try {
				var cancelData = GetCancelIMS(SubmissionId);
				if(cancelData.Count == 0) {
					throw new Exception("No data can be processed to Cancel IMS");
				}
				
				var cancelDataGrouping = cancelData
					.GroupBy(item => new { 
						ReservationID = item["ReservationID"], 
						StoreID = item["StoreID"]
					})
					.Select(item => new Dictionary<string, string>() {
						{"ReservationID", item.Key.ReservationID},
						{"StoreID", item.Key.StoreID}
					})
					.ToList();
				
                var errorList = new List<string>();
                for (int i = 0; i < cancelDataGrouping.Count; i++) {
                    await Task.Delay(delayInSecond * 1000);

                    try {
						string reservationID = cancelDataGrouping[i]["ReservationID"];
						string storeID = cancelDataGrouping[i]["StoreID"];
					
                        await Unreserve(reservationID, storeID);
                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgReservationID", Column.Parameter(string.Empty))
                            .Set("DgIsUERP", Column.Parameter(false))
                            .Set("DgIsMMAG", Column.Parameter(false))
                            .Set("DgIsCommon", Column.Parameter(false))
                            .Set("DgReleasedToIPL", Column.Parameter(false))
                            .Where("DgReservationID").IsEqual(Column.Parameter(reservationID))
                            .And("DgSubmissionId").IsEqual(Column.Parameter(SubmissionId))
                        .Execute();
						
						HistorySubmissionService.InsertHistory(
							UserConnection: UserConnection,
							SubmissionId: SubmissionId,
							CreatedById: UserConnection.CurrentUser.ContactId,
							OpsId: LookupConst.Ops.ADD,
							SectionId: LookupConst.Section.RELEASED_TO_MESAD,
							Remark: $"[Unreserve] Reservation ID: {reservationID} Store ID: {storeID} success"
						);

                    } catch (Exception e) {
                        errorList.Add(e.Message);
                    }
                }

                string errorCancelMessage = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
				result.Message = errorList.Count > 0 ? 
					JsonConvert.SerializeObject(new List<string>() {
						$"Cancel Item IMS: <br><ul>{errorCancelMessage}</ul>"	
					}) : "Successfully Unreserved stock";
				result.Success = errorList.Count == 0 ? true : false;
			} catch(Exception e) {
				result.Message = e.Message;
			}

            return result;
        }

        protected List<Dictionary<string, string>> GetCancelIMS(Guid SubmissionId)
        {
            var result = new List<Dictionary<string, string>>();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");

            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("ReservationID", esq.AddColumn("DgReservationID"));
            columns.Add("StoreID", esq.AddColumn("Dg3PLService.DgStoreID"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission", SubmissionId));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgCancelItemIMS", true));
			
			var filterReservation = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
			filterReservation.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgReservationID", string.Empty));
			filterReservation.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgReservationID"));
			esq.Filters.Add(filterReservation);
			
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgPreDeliveryDate"));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgDeliveryStatus"));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities) {
                result.Add(new Dictionary<string, string>() {
                    {"ReservationID", entity.GetTypedColumnValue<string>(columns["ReservationID"].Name)},
					{"StoreID", entity.GetTypedColumnValue<string>(columns["StoreID"].Name)}
                });
            }

            return result;
        }

        #endregion

    }

    public class ReserveStockResponseV2
    {
        public string ReservationID { get; set; }
        public ReserveStockResponse Response { get; set; }
    }
}