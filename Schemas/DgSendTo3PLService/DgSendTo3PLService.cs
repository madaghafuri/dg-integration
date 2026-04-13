using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgSubmission.DgHistorySubmissionService;
using DgIntegration.DgCalculateTaxFeeService;
using DgIntegration.DgCreateCustomerSalesOrderUERPService;
using DgIntegration.DgMMAGOrderCreateService;
using DgIntegration.DgCommonInventory;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;
using OrderCreateRequest = DgIntegration.DgMMAGOrderCreateService.Request;
using OrderCreateResponse = DgIntegration.DgMMAGOrderCreateService.Response;

namespace DgIntegration.DgSendTo3PL
{
    public class SendTo3PL
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private Guid SubmissionId;
        private List<LineDetailSelected> lineDetailSelected;
        private List<CommonInventoryGroup> commonInventoryList;
        private CommonInventoryService commonInventoryService;
        
        public SendTo3PL(UserConnection userConnection, Guid SubmissionId)
        {
            this.userConnection = userConnection;
            this.SubmissionId = SubmissionId;
            this.commonInventoryService = new CommonInventoryService(userConnection);
        }
		
		// deprecated, remove soon
        public virtual async Task<GeneralResponse> Process()
        {
            var result = new GeneralResponse();
            
            try {
                GetLineDetail();

                // Default process
                var lineDetailForDefault = this.lineDetailSelected
                    .Where(item => IsUERP(item))
                    .ToList();

                // UERP Success
                // MMAG fail
                var lineDetailForMMAG = this.lineDetailSelected
                    .Where(item => IsMMAG(item))
                    .ToList();
                
                string soNumber = string.Empty;
                string uerpMessage = string.Empty;
                string mmagMessage = string.Empty;
                bool isUERPSuccess = true;
                bool isMMAGSuccess = true;

                try {
                    if(lineDetailForDefault.Count > 0) {
                        var lineDetailDefaultIds = lineDetailForDefault.Select(item => item.Id).ToList();
                        MMAGValidation(lineDetailDefaultIds);
                        soNumber = await SendToUERP(lineDetailDefaultIds);

                        uerpMessage = $"Request Received to create SO against {soNumber}. Please wait callback SO DO Number from UERP";
                        if(lineDetailForMMAG.Count == 0) {
                            result.Message = uerpMessage;
                            result.Success = true;

                            return result;
                        }
                    }
                } catch (Exception e) {
                    isUERPSuccess = false;
                    uerpMessage = e.Message;
                }
                
                try {
                    if(lineDetailForMMAG.Count > 0) {
                        await SendToMMAG(lineDetailForMMAG);
                        mmagMessage = "Send to MMAG: Success";
                    }
                } catch (Exception e) {
                    isMMAGSuccess = false;
                    mmagMessage = e.Message;
                }

                result.Success = isUERPSuccess && isMMAGSuccess;

                if(lineDetailForDefault.Count == 0 && lineDetailForMMAG.Count == 0) {
                    result.Success = false;
                    result.Message = "No data can be processed to 3PL";
                } else {
                    var messages = new List<string>() {uerpMessage, mmagMessage};
                    result.Message = !result.Success ?
                        JsonConvert.SerializeObject(new List<string>() {
                            string.Join("<br><br>", messages.Where(item => !string.IsNullOrEmpty(item)).ToArray())
                        }) : null;
                }
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }

        public virtual async Task<GeneralResponse> ProcessV2()
        {
            var result = new GeneralResponse();
            try {
                GetLineDetail();

                // Default process
                // Common Inventory > UERP
                var lineDetailForDefault = this.lineDetailSelected
                    .Where(item => IsUERP(item))
                    .ToList();

                var lineDetailForCommonInventory = lineDetailForDefault
                    .Where(item => IsCommonInventory(item))
                    .ToList();

                // UERP Success
                // MMAG fail
                var lineDetailForMMAG = this.lineDetailSelected
                    .Where(item => IsMMAG(item))
                    .ToList();

                string soNumber = string.Empty;
                string uerpMessage = string.Empty;
                string mmagMessage = string.Empty;
                bool isCommonInventorySuccess = true;
                bool isUERPSuccess = true;
                bool isMMAGSuccess = true;
                bool isRunCommonInvent = lineDetailForCommonInventory.Count > 0;
                
                try {
                    if(lineDetailForDefault.Count > 0) {
                        var lineDetailDefaultIds = lineDetailForDefault.Select(item => item.Id).ToList();
                        MMAGValidation(lineDetailDefaultIds);

                        if(isRunCommonInvent) {
                            try {
                                await CommonInventoryProcess(lineDetailForCommonInventory);
                            } catch (Exception e) {
                                isCommonInventorySuccess = false;
                                throw;
                            }

                        } else {
                            var lineWithoutIMSI = lineDetailForDefault
                                .Where(item => string.IsNullOrEmpty(item.IMSIType))
                                .Select(item => $"<li>Line No. {item.No}. IMSI Type cannot be null or empty</li>")
                                .ToArray();
                            string lineWithoutIMSIMessage = string.Join("", lineWithoutIMSI);

                            if(lineWithoutIMSI.Length > 0) {
                                throw new Exception($"The selected line cannot be processed in Common Inventory. Therefore, the IMSI is required.<br>{lineWithoutIMSIMessage}");
                            }
                        }

                        soNumber = await SendToUERP(lineDetailDefaultIds);
						if(isRunCommonInvent) {
							string reserveHistoryMessage = string.Join(". ", this.commonInventoryList
								.Select(el => $"Item Code: {el.ItemCode} Store ID: {el.StoreID} Qty: {el.Qty.ToString()}")
								.ToArray());
							HistorySubmissionService.InsertHistory(
								UserConnection: UserConnection,
								SubmissionId: this.SubmissionId,
								CreatedById: UserConnection.CurrentUser.ContactId,
								OpsId: LookupConst.Ops.ADD,
								SectionId: LookupConst.Section.RELEASED_TO_MESAD,
								Remark: $"[Reserve] {reserveHistoryMessage}"
							);	
						}

                        uerpMessage = $"Request Received to create SO against {soNumber}. Please wait callback SO DO Number from UERP";
                        if(lineDetailForMMAG.Count == 0) {
                            result.Message = uerpMessage;
                            result.Success = true;

                            return result;
                        }
                    }
                } catch (Exception e) {
                    isUERPSuccess = false;
                    uerpMessage = e.Message;
                }
				
                try {
                    if(!isUERPSuccess && isRunCommonInvent && isCommonInventorySuccess) {
                        await UnreserveStockProcess(this.commonInventoryList);

                        string errorUnserverStock = string.Join(
                            "", 
                            this.commonInventoryList
                                .Where(item => !string.IsNullOrEmpty(item.UnreserveMessage))
                                .Select(item => $"<li>{item.UnreserveMessage}</li>")
                                .ToArray()
                        );
                        if(!string.IsNullOrEmpty(errorUnserverStock)) {
                            throw new Exception($"Common Inventory: Unreserve Stock failed.<br><ul>{errorUnserverStock}</ul>");
                        }
                    }
                } catch (Exception e) {
                    uerpMessage += $"<br>{e.Message}";
                }
                
                try {
                    if(lineDetailForMMAG.Count > 0) {
                        await SendToMMAG(lineDetailForMMAG);
                    }
                } catch (Exception e) {
                    isMMAGSuccess = false;
                    mmagMessage = e.Message;
                }

                result.Success = isUERPSuccess && isMMAGSuccess;
                
                if(lineDetailForDefault.Count == 0 && lineDetailForMMAG.Count == 0) {
                    result.Success = false;
                    result.Message = "No data can be processed to 3PL";
                } else {
                    var messages = new List<string>() {uerpMessage, mmagMessage};
                    result.Message = !result.Success ?
                        JsonConvert.SerializeObject(new List<string>() {
                            string.Join("<br>", messages.Where(item => !string.IsNullOrEmpty(item)).ToArray())
                        }) : null;
                }
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }

        protected virtual void GetLineDetail()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");

            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("No", esq.AddColumn("DgNo"));
            columns.Add("LineId", esq.AddColumn("DgLineId"));
            columns.Add("SONumber", esq.AddColumn("DgSOID"));
            columns.Add("OFSDoNo", esq.AddColumn("DgOFSDoNo"));
            columns.Add("SODoID", esq.AddColumn("DgSODoID"));
            columns.Add("IsUERP", esq.AddColumn("DgIsUERP"));
            columns.Add("IsMMAG", esq.AddColumn("DgIsMMAG"));
            columns.Add("ReservationID", esq.AddColumn("DgReservationID"));
            columns.Add("StoreID", esq.AddColumn("Dg3PLService.DgStoreID"));
            columns.Add("IMSIType", esq.AddColumn("DgOrderIMSIType.Name"));

            columns["No"].OrderByAsc(0);
            columns["LineId"].OrderByAsc(1);

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToIPL", true));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsMMAG", false));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission", this.SubmissionId));

            // var filterSoNumber = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", string.Empty));
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSOID"));
            // esq.Filters.Add(filterSoNumber);

            // var filterSODoID = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            // filterSODoID.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSODoID", string.Empty));
            // filterSODoID.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSODoID"));
            // esq.Filters.Add(filterSODoID);

            var entities = esq.GetEntityCollection(UserConnection);
            if(entities.Count == 0) {
                throw new Exception("No data can be processed to 3PL");
            }

            this.lineDetailSelected = new List<LineDetailSelected>();
            foreach (var entity in entities) {
                var temp = new LineDetailSelected() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    No = entity.GetTypedColumnValue<int>(columns["No"].Name),
                    LineId = entity.GetTypedColumnValue<int>(columns["LineId"].Name),
                    SONumber = entity.GetTypedColumnValue<string>(columns["SONumber"].Name),
                    OFSDoNo = entity.GetTypedColumnValue<string>(columns["OFSDoNo"].Name),
                    SODoID = entity.GetTypedColumnValue<string>(columns["SODoID"].Name),
                    ReservationID = entity.GetTypedColumnValue<string>(columns["ReservationID"].Name),
                    StoreID = entity.GetTypedColumnValue<string>(columns["StoreID"].Name),
                    IMSIType = entity.GetTypedColumnValue<string>(columns["IMSIType"].Name),
                    IsUERP = entity.GetTypedColumnValue<bool>(columns["IsUERP"].Name),
                    IsMMAG = entity.GetTypedColumnValue<bool>(columns["IsMMAG"].Name),
                };
                this.lineDetailSelected.Add(temp);
            }

            GetItemCode();
        }

        protected virtual void GetItemCode()
        {
            QueryColumnExpression[] lineDetailIds = this.lineDetailSelected
                .Select(item => Column.Parameter(item.Id))
                .ToArray();

            if(lineDetailIds.Length == 0) {
                return;
            }

            var select = new Select(UserConnection)
                .Column("DgLineDetail", "Id").As("LineDetailId")
                .Column("DgFeeDetail", "DgResModeID").As("ItemCode")
            .From("DgFeeDetail")
            .Join(JoinType.LeftOuter, "DgLineDetail")
                .On("DgLineDetail", "Id").IsEqual("DgFeeDetail", "DgLineDetailId")
            .Where("DgFeeDetail", "DgSuppOfferIndex").IsGreater(Column.Parameter(0))
            .And("DgFeeDetail", "DgFeeName").IsEqual(Column.Parameter("Handset Fee"))
            .And("DgLineDetail", "Id").In(lineDetailIds) as Select;

            using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                using(IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        int index = this.lineDetailSelected.FindIndex(item => item.Id == dataReader.GetColumnValue<Guid>("LineDetailId"));
                        this.lineDetailSelected[index].ItemCode = dataReader.GetColumnValue<string>("ItemCode");
                    }
                }
            }
        }

        protected virtual bool IsCommonInventory(LineDetailSelected LineDetail)
        {
            return !LineDetail.IsUERP 
                && !LineDetail.IsMMAG
                && !string.IsNullOrEmpty(LineDetail.ItemCode)
                && !string.IsNullOrEmpty(LineDetail.StoreID)
                && string.IsNullOrEmpty(LineDetail.ReservationID);
        }

        protected virtual bool IsUERP(LineDetailSelected LineDetail)
        {
            return !LineDetail.IsUERP 
                && !LineDetail.IsMMAG;
        }
		
		// ada bug disini, harus tambah 1 flag baru, IsCallbackUERP
        protected virtual bool IsMMAG(LineDetailSelected LineDetail)
        {
            return LineDetail.IsUERP 
                && !LineDetail.IsMMAG;
        }

        #region CommonInventory
        
        protected virtual async Task CommonInventoryProcess(List<LineDetailSelected> LineDetails)
        {
            string errorMessage = string.Empty;

            this.commonInventoryList = LineDetails
                .GroupBy(item => new { 
                    item.ItemCode, 
                    item.StoreID
                })
                .Select(item => new CommonInventoryGroup() {
                    ItemCode = item.Key.ItemCode,
                    StoreID = item.Key.StoreID,
                    Qty = item.Count()
                })
                .ToList();

            if(this.commonInventoryList
                .GroupBy(item => new {
                    item.StoreID   
                })
                .Select(item => item.Key.StoreID)
                .ToList()
                .Count > 1) {

                throw new Exception("Common Inventory: Please ensure all Store IDs in a single request are the same");
            }
            
            await CheckStockProcess();
            bool isCheckStockSuccess = this.commonInventoryList
                .Where(item => string.IsNullOrEmpty(item.Message))
                .ToList()
                .Count == this.commonInventoryList.Count ? true : false;

            if(!isCheckStockSuccess) {
                errorMessage = string.Join(
                    "", 
                    this.commonInventoryList
                        .Where(item => !string.IsNullOrEmpty(item.Message))
                        .Select(item => $"<li>{item.Message}</li>")
                        .ToArray()
                );
                throw new Exception($"Common Inventory: Check Stock failed, No Stock Available For Selected Devices in the MMAG Channel<br><ul>{errorMessage}</ul>");
            }

            await ReserveStockProcess();
            var reserveSuccess = this.commonInventoryList
                .Where(item => !string.IsNullOrEmpty(item.ReservationID))
                .ToList();

            bool isReserveStockSuccess = reserveSuccess.Count == this.commonInventoryList.Count ? true : false;
            if(!isReserveStockSuccess) {
                if(reserveSuccess.Count > 0) {
                    await UnreserveStockProcess(reserveSuccess);
                }

                string errorReserveStock = string.Join(
                    "", 
                    this.commonInventoryList
                        .Where(item => !string.IsNullOrEmpty(item.Message))
                        .Select(item => $"<li>{item.Message}</li>")
                        .ToArray()
                );
                
                string errorUnserverStock = string.Join(
                    "", 
                    this.commonInventoryList
                        .Where(item => !string.IsNullOrEmpty(item.UnreserveMessage))
                        .Select(item => $"<li>{item.UnreserveMessage}</li>")
                        .ToArray()
                );

                errorMessage = $"Common Inventory: Reserve Stock failed.<br><ul>{errorReserveStock}</ul>";
                throw new Exception(!string.IsNullOrEmpty(errorUnserverStock) ? $"{errorMessage}<br>Unreserve Stock failed.<br><ul>{errorUnserverStock}</ul>" : errorMessage);
            }
        }

        protected virtual async Task CheckStockProcess()
        {
            for (int i = 0; i < this.commonInventoryList.Count; i++) {
                await Task.Delay(1000);

                string storeID = this.commonInventoryList[i].StoreID;
                string itemCode = this.commonInventoryList[i].ItemCode;

                try {
                    var checkStock = await this.commonInventoryService.CheckStock(storeID, itemCode);
                    int qtyReserve = this.commonInventoryList[i].Qty;
                    int totalAvail = checkStock.result[0].TotalAvailableQty;
                    string device = checkStock.result[0].deviceModelDesc;			

                    if(totalAvail < qtyReserve) {
                        throw new Exception($"{device}, Requested {qtyReserve}, Available {totalAvail}");
                    }

                    this.commonInventoryList[i].CheckStockResponse = checkStock;
                } catch(Exception e) {
                    this.commonInventoryList[i].Message = $"Item Code: {itemCode} Store ID: {storeID} failed. {e.Message}";
                }
            }
        }

        protected virtual async Task ReserveStockProcess()
        {
            for (int i = 0; i < this.commonInventoryList.Count; i++) {
                await Task.Delay(1000);

                string storeID = this.commonInventoryList[i].StoreID;
                string itemCode = this.commonInventoryList[i].ItemCode;
                string qty = this.commonInventoryList[i].Qty.ToString();
                var checkStock = this.commonInventoryList[i].CheckStockResponse;

                try {
                    var reserve = await this.commonInventoryService.Reserve(storeID, qty, checkStock);
                    string reserveId = reserve.ReservationID;
                    this.commonInventoryList[i].ReservationID = reserveId;

                    List<Guid> lineDetailIds = this.lineDetailSelected
                        .Where(item => item.ItemCode == itemCode && item.StoreID == storeID)
                        .Select(item => item.Id)
                        .ToList();

                    foreach (Guid id in lineDetailIds) {
                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgReservationID", Column.Parameter(reserveId))
                            .Set("DgIsCommon", Column.Parameter(true))
                            .Where("Id").IsEqual(Column.Parameter(id))
                            .Execute();
                    }
                } catch (Exception e) {
                    this.commonInventoryList[i].Message = $"Item Code: {itemCode} Store ID: {storeID} failed. {e.Message}";
                }
            }
        }

		protected virtual async Task UnreserveStockProcess(List<CommonInventoryGroup> Data)
        {
            for (int i = 0; i < Data.Count; i++) {
                await Task.Delay(1000);

                string itemCode = Data[i].ItemCode;
                string storeID = Data[i].StoreID;
                string reservationId = Data[i].ReservationID;

                try {
                    await this.commonInventoryService.Unreserve(reservationId, storeID);
                    List<Guid> lineDetailIds = this.lineDetailSelected
                        .Where(item => item.ItemCode == itemCode && item.StoreID == storeID)
                        .Select(item => item.Id)
                        .ToList();
                    foreach (Guid id in lineDetailIds) {
                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgReservationID", Column.Parameter(string.Empty))
                            .Set("DgIsCommon", Column.Parameter(false))
                            .Where("Id").IsEqual(Column.Parameter(id))
                            .Execute();
                    }
                } catch (Exception e) {
                    int index = this.commonInventoryList.FindIndex(item => item.ItemCode == itemCode && item.StoreID == storeID && item.ReservationID == reservationId);
                    if(index != -1) {
                        this.commonInventoryList[index].Message = $"Unreserve Reservation ID: {reservationId} Store ID: {storeID} failed. {e.Message}";
                    }
                }
            }
        }

        #endregion
        
        #region UERP
            
        protected virtual async Task CalculateTaxFee(List<Guid> LineDetails)
        {
			try {
				var service = new CalculateTaxFeeService(UserConnection);
				await service
					.SetParam(LineDetails)
					.Request();
				
				var errorReq = service.GetBatchError();
				if(errorReq.Count > 0) {
					throw new Exception(string.Join("\n", errorReq));
				}
				
				var response = service.GetBatchResponse();
				for (int i = 0; i < service.LineDetailList.Count; i++) {
					try {
						var resItem = response[0];
						var result = resItem?.Body?.calculateTaxFeeResponse?.ResultOfOperationReply;
						var success = result?.resultMessage == "success" ? true : false;
						if(!success) {
							throw new Exception(result?.resultMessage);
						}

						var reply = resItem?.Body?.calculateTaxFeeResponse?.CalculateTaxFeeReply;
						var feeAmountCalculated = reply?.feeAmtCalculated;
						var feeItemCode = reply?.feeItemCode;

						new Update(UserConnection, "DgFeeDetail")
							.Set("DgFeeAmount", Column.Parameter(Convert.ToDecimal(feeAmountCalculated)))
							.Where("DgLineDetailId").IsEqual(Column.Parameter(service.LineDetailList[i]))
							.And("DgFeeItemCode").IsEqual(Column.Parameter(feeItemCode))
							.Execute();	
					} catch(Exception e) {
						continue;
					}
				}
			} catch(Exception e) {}
        }

        protected virtual async Task<string> SendToUERP(List<Guid> LineDetails)
        {
            string soNumber = string.Empty;
            try {
				await CalculateTaxFee(LineDetails);
				
                var service = new CreateCustomerSalesOrderUERPService(UserConnection);
                await service
                    .SetParamByLineDetail(LineDetails)
                    .Request();
				
				CreateCustomerSalesOrderUERPService.InsertLog(UserConnection, service.GetLog(), soNumber, service.IsSuccessResponse() ? "SUCCESS" : "FAIL");
				
                if(!service.IsSuccessResponse()) {
                    throw new Exception(service.GetErrorResponse());
                }

                DateTime now = DateTime.UtcNow;
                soNumber = service.GetSONumber();
                
				var salesOrderLines = service.GetRequest().UERPCreateCustomerSalesOrderRequest.SalesOrderLine.Select(item => item.OrigSysLineRef).ToList();
				var lineIdSuccess = salesOrderLines
					.Select(item => item.Substring(0, item.IndexOf('_')))
					.GroupBy(item => item)
					.Select(item => Convert.ToInt32(item.Key))
					.ToList();

                var allLineId = this.lineDetailSelected
                    .Where(item => LineDetails.Contains(item.Id))
                    .Select(item => item.LineId)
                    .ToList();
                
				var lineIdFail = allLineId.Except(lineIdSuccess).ToList();
				
                foreach (int lineId in lineIdSuccess) {
                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgSOID", Column.Parameter(soNumber))
						.Set("DgDateTimeReleased", Column.Parameter(now))
						.Set("Dg3PLReleasedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                        .Set("DgIsUERP", Column.Parameter(true))
						.Set("DgCancelItemIMS", Column.Parameter(false))
                        .Where("DgLineId").IsEqual(Column.Parameter(lineId))
                    .Execute();
                }

                foreach(int lineId in lineIdFail) {
                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgReleasedToIPL", Column.Parameter(false))
                        .Where("DgLineId").IsEqual(Column.Parameter(lineId))
                    .Execute();
                }

                HistorySubmissionService.ReleaseUERP(
                    UserConnection: UserConnection,
                    SubmissionId: this.SubmissionId,
                    SOId: soNumber,
                    CreatedById: UserConnection.CurrentUser.ContactId
                );
            } catch (Exception e) {
                throw new Exception($"Send to UERP: {e.Message}");
            }

            return soNumber;
        }

        #endregion

        #region MMAG
        
        // will throw error if build request not success
        protected virtual void MMAGValidation(List<Guid> LineDetails)
        {
            var service = new MMAGOrderCreateService(UserConnection);
            service.SetParamByLineDetail(LineDetails);
        }

        protected virtual async Task SendToMMAG(List<LineDetailSelected> LineDetails)
        {
            var soNumberGroup = LineDetails
                .GroupBy(item => new {
                    item.SONumber
                })
                .Select(item => item.Key.SONumber)
                .ToList();

            var errorList = new List<string>();
            foreach (var soNumber in soNumberGroup) {
                try {
                    var service = new MMAGOrderCreateService(UserConnection);
                    await service
                        .SetParamBySONumber(soNumber)
                        .Request();
					
					MMAGOrderCreateService.InsertLog(UserConnection, service.GetLog(), soNumber, service.IsSuccessResponse() ? "SUCCESS" : "FAIL");

                    if(!service.IsSuccessResponse()) {
                        string errorMessage = service.GetErrorResponse();

                        HistorySubmissionService.InsertHistory(
                            UserConnection: UserConnection,
                            SubmissionId: this.SubmissionId,
                            CreatedById: UserConnection.CurrentUser.ContactId,
                            OpsId: LookupConst.Ops.ADD,
                            SectionId: LookupConst.Section.RELEASED_TO_MESAD,
                            Remark: $"[3PL] SO {soNumber} failed. {errorMessage}"
                        );

                        throw new Exception(errorMessage);
                    }

                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgSODoID", Column.Parameter(soNumber))
                        .Set("DgIsMMAG", Column.Parameter(true))
                        .Where("DgSOID").IsEqual(Column.Parameter(soNumber))
                    .Execute();

                    HistorySubmissionService.Release3PL(
                        UserConnection: UserConnection,
                        SubmissionId: this.SubmissionId,
                        OFSDoNoId: soNumber,
                        CreatedById: UserConnection.CurrentUser.ContactId
                    );
                } catch (Exception e) {
                    errorList.Add(e.Message);
                }
            }

            if(errorList.Count > 0) {
                string error = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToList());
                throw new Exception($"Send to MMAG:<br><ul>{error}</ul>");
            }
        }

        #endregion
    }

    public class LineDetailSelected
    {
        public Guid Id { get; set; }
        public int No { get; set; }
        public int LineId { get; set; }
        public bool IsUERP { get; set; }
        public bool IsMMAG { get; set; }
        public string SONumber { get; set; }
        public string OFSDoNo { get; set; }
        public string SODoID { get; set; }
        public string ReservationID { get; set; }
        public string ItemCode { get; set; }
        public string StoreID { get; set; }
        public string IMSIType { get; set; }
    }

    public class CommonInventoryGroup
    {
        public string ItemCode { get; set; }
        public string StoreID { get; set; }
        public string ReservationID { get; set; }
        public int Qty { get; set; }
        public CheckStockResponse CheckStockResponse { get; set; }
        public string Message { get; set; }
        public string UnreserveMessage { get; set; }
    }
}