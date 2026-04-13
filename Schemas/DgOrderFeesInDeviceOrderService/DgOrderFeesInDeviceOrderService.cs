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
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DgCSGIntegration;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData.DgLookupConst;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgCSGIntegration.DgOrderFees
{
    public class OrderFeesInDeviceOrderService
    {
        private UserConnection UserConnection;
        private Guid submissionId;
        private Guid submissionTypeId;
        private CSGService csgService;

        public OrderFeesInDeviceOrderService(UserConnection UserConnection, Guid SubmissionId)
        {
            this.UserConnection = UserConnection;

            this.submissionId = SubmissionId;
            this.submissionTypeId = GetSubmissionType();
            this.csgService = new CSGService(UserConnection);
        }

        public virtual async Task<GeneralResponse> Process()
        {
            var result = new GeneralResponse();
            try {
                var lineDetailIds = GetLineDetail();
                if(lineDetailIds == null || (lineDetailIds != null && lineDetailIds.Count == 0)) {
                    throw new Exception("No data can be process to Order Fees");
                }

                var orderFees = new OrderFees(UserConnection);
                var paramList = new List<OrderFeesRequestV2>();
                if(this.submissionTypeId != SubmissionType.COP) {
                    paramList = orderFees.GetParamByLineDetail(lineDetailIds);
                } else {
                    paramList = await orderFees.GetParamByLineDetailCOP(lineDetailIds);
                }
				
				List<string> errorMessages = new List<string>();
				var tasks = new List<Task>();
				foreach (var orderFeesParam in paramList) {
					tasks.Add(RequestOrderFees(orderFees, orderFeesParam));
				}
				
				Task tasksResult = null;
				try {
					tasksResult = Task.WhenAll(tasks);
					await tasksResult;
				} catch(Exception e) {}

                var lineIds = paramList
                    .Select(item => item.LineDetail.Id)
                    .ToList();

                new Update(UserConnection, "DgLineDetail")
                    .Set("DgOPPageOpenDate", Column.Parameter(DateTime.UtcNow))
                    .Where("Id").In(Column.Parameters(lineIds))
                    .Execute();
				
				for(int i=0; i<tasks.Count; i++) {
					var task = tasks[i];
					if(task.Status != TaskStatus.RanToCompletion) {
						var exception = task.Exception;
						var innerException = exception?.InnerExceptions;
						string errorMessage = innerException?.FirstOrDefault()?.Message ?? string.Empty;

						if(!string.IsNullOrEmpty(errorMessage)) {
							errorMessages.Add(errorMessage);
						}
					}
				}

                if(errorMessages.Count > 0) {
                    throw new Exception(JsonConvert.SerializeObject(errorMessages));
                }

                result.Success = true;
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }
		
		protected virtual async Task RequestOrderFees(OrderFees orderFees, OrderFeesRequestV2 orderFeesRequest)
		{
			try {
				if(orderFeesRequest.Request == null) {
					throw new Exception(orderFeesRequest.Message);
				}

				var orderFeesRes = await this.csgService.OrderFees(orderFees, orderFeesRequest.Request, "DgLineDetail", orderFeesRequest.LineDetail.Id);
                
				LineDetailProcess(orderFeesRes, orderFeesRequest.LineDetail);

				var offerWithoutBox = GetOfferContractWithoutBox(orderFeesRequest.LineDetail);
				if(offerWithoutBox.Count > 0) {
					string offerMessage = string.Join(", ", offerWithoutBox);
					throw new Exception($"Offer {offerMessage} didn't have any fees");
				}   
			} catch (Exception e) {
				if(!string.IsNullOrEmpty(e.Message)) {
					throw new Exception($"{orderFeesRequest.LineDetail.No} - {orderFeesRequest.LineDetail.MSISDN} fail. {e.Message}");	
				}
			}
		}
		
        protected virtual void LineDetailProcess(OrderFeesResponse Response, LineDetailSelected Selected)
        {
            try {
                var res = Response.RetrieveFeesForOrderResponse;
				var feesRecord = res?.FeesList?.FeesRecord;
                var feeDetails = GetFeeDetail(Selected.Id);
				
                if(res == null || (feesRecord == null || (feesRecord != null && feesRecord.Count < 1))) {
                    foreach (var fee in feeDetails) {
                        if (fee.DgFeeName == "Penalty Fee" && fee.DgFeeItemCode == "910017") {
                            continue;
                        }
					new Delete(UserConnection)
						.From("DgFeeDetail")
						.Where("DgLineDetailId").IsEqual(Column.Parameter(Selected.Id))
						.Execute();
                    }
					
                    // throw new Exception($"FeesRecord is empty");
					throw new Exception("");
                }

                // fix ofs code
                List<string> feesRecordWithDevice = feesRecord
                    .Where(item => item.FeeName == "Handset Fee" && item.Resource != null)
                    .Select(item => item.OfferId)
                    .ToList();
                if(feesRecordWithDevice.Count > 0) {
                    var offerNameDevice = new List<string>();
                    foreach(var offerId in feesRecordWithDevice) {
                        string offerName = Selected.SuppOfferList
                            .Where(item => item.OfferID == offerId)
                            .Select(item => item.OfferName)
                            .FirstOrDefault();
                        if(!string.IsNullOrEmpty(offerName)) {
                            offerNameDevice.Add(offerName);
                        }
                    }

                    List<Dictionary<string, string>> oracleFixCode = GetOracleFixCode(offerNameDevice);
                    foreach(var oracleFix in oracleFixCode) {
                        string ofsCode = oracleFix["OFSCode"];
                        string resourceModelId = oracleFix["ResourceModelId"];
                        string offerId = oracleFix["OfferID"];

                        int index = feesRecord
                            .FindIndex(item => {
                                return item.FeeName == "Handset Fee" 
                                    && item.Resource != null
                                    && item.OfferId == offerId;
                            });
                        if(index != -1) {
                            feesRecord[index].Resource.ResourceModelId = resourceModelId;
                            feesRecord[index].OFSCode = ofsCode;
                        }
                    }
                }

                SaveOrderFees(Selected, feesRecord);

                var missingFees = feeDetails.Where(item => {
                    return !feesRecord.Any(fee => { 
						bool isEqual = fee.FeeName == item.DgFeeName
                            && fee.FeeItemCode == item.DgFeeItemCode
                            && fee.FeeType == item.DgFeeType
                            && fee.PaymentType == item.DgPaymentType
							&& fee.OFSCode == item.DgOFSCode;
						
						if(fee.OfferId != null) {
							return isEqual && fee.OfferId == item.DgOfferID;
						}
						
						return isEqual;
                    });
                });
                foreach(var missingFee in missingFees) {
					bool isPenaltyFees = missingFee.DgFeeName == "Penalty Fee" && missingFee.DgFeeItemCode == "910017";
					if(isPenaltyFees) {
						continue;
					}

                    new Delete(UserConnection)
                        .From("DgFeeDetail")
                        .Where("Id").IsEqual(Column.Parameter(missingFee.Id))
                        .Execute();
                }

            } catch (Exception e) {
                throw;
            }
        }

        protected virtual void SaveOrderFees(LineDetailSelected LineDetail, List<FeesRecord> data)
        {
			var existingFee = GetFeeDetail(LineDetail.Id);
			
            var feesRecordProcess = new List<FeesRecord>();
            foreach (var item in data) {
                if(!string.IsNullOrEmpty(item.OfferId)) {
                    var index = feesRecordProcess.FindIndex(feeRecord => feeRecord.OfferId == item.OfferId);                    
                    if(index == -1) {
                        if(item.FeeType.Contains("SAL")) {
                            if(item.FeeName == "Handset Fee" && 
                                item.Resource != null &&
                                item.Resource.ResourceType == "40") {
                                feesRecordProcess.Add(item);
                            }
                        } else {
                            feesRecordProcess.Add(item);
                        }
                    } else {
                        var hierarcy = new List<string>() {
                            "ADV PAYMENT",
                            "HANDSET",
                            "OCC",
                            "HDL",
                            "SAL",
                        };
                        int feeTypeIndexExists = hierarcy.FindIndex(feeType => feesRecordProcess[index].FeeType.Contains(feeType));
                        int feeTypeNew = hierarcy.FindIndex(feeType => item.FeeType.Contains(feeType));
                        if(feeTypeNew < feeTypeIndexExists) {
                            feesRecordProcess[index] = item;
                        }
                    }
                }

                string payType = string.Empty;
                switch (item.PaymentType) {
                    case "CASH":
                        payType = "1";
                        break;
                    case "CHARGEACCOUNT":
                        payType = "2";
                        break;
                    case "NOLIMIT":
                        payType = "9";
                        break;
                    default:
                        break;
                }

                //CR Penalty Fee 2025-07-30, Mada
                if (item.PaymentType == "NOLIMIT" && item.FeeName == "Penalty Fee")
                {
                    payType = "2";
                    item.PaymentType = "CHARGEACCOUNT";
                }

                var valueForCreate = new Dictionary<string, object>() {
                    {"DgLineDetailId", LineDetail.Id},
					{"DgLineId", LineDetail.LineId},
                    {"DgFeeName", item.FeeName},
                    {"DgFeeItemCode", item.FeeItemCode},
                    {"DgFeeType", item.FeeType},
                    {"DgOFSCode", item.OFSCode},
                    {"DgPaymentType", item.PaymentType},
                    {"DgPayType", payType},
                    {"DgFeeAmount", item.FeeAmount},
					{"DgNetAmount", item.FeeAmount},
					//{"DgWaiveAmount", item.FeeAmount},
					{"DgWaiveAmount", 0},
                    {"DgOriginalFeeAmount", item.OriginalFeeAmount},
                };
                if(item.OfferId != null) {
                    valueForCreate.Add("DgOfferID", item.OfferId);
                }

                if(item.Resource != null) {
                    valueForCreate.Add("DgResCode", item.Resource.ResourceCode);
                    valueForCreate.Add("DgResType", item.Resource.ResourceType);
                    valueForCreate.Add("DgResModeID", item.Resource.ResourceModelId);
                }
				
				bool isPenaltyFees = item.FeeName == "Penalty Fee" && item.FeeItemCode == "910017";
				bool isPenaltyExists = existingFee
					.Where(el => el.DgFeeName == "Penalty Fee" && el.DgFeeItemCode == "910017")
					.ToList()
					.Count > 0 ? true : false;
				
				if(isPenaltyFees && isPenaltyExists) {
					continue;
				}
			
                UpdateFeeDetail(valueForCreate, item.TaxList);
            }

            foreach (var feesRecord in feesRecordProcess) {
                var suppOfferSelected = LineDetail.SuppOfferList.Where(item => item.OfferID == feesRecord.OfferId).FirstOrDefault();
                if(suppOfferSelected != null) {
                    new Update(UserConnection, "DgFeeDetail")
                        .Set("DgSuppOfferIndex", Column.Parameter(suppOfferSelected.Index))
                        .Where("DgLineDetailId").IsEqual(Column.Parameter(LineDetail.Id))
                            .And("DgOfferID").IsEqual(Column.Parameter(suppOfferSelected.OfferID))
                            .And("DgFeeName").IsEqual(Column.Parameter(feesRecord.FeeName))
                            .And("DgFeeType").IsEqual(Column.Parameter(feesRecord.FeeType))
                    .Execute();
                }
            }
        }

        protected virtual void SaveTax(Guid feeDetailId, TaxRecord data)
        {
            new Insert(UserConnection)
                .Into("DgTaxListDetail")
                .Set("DgFeeDetailId", Column.Parameter(feeDetailId))
                .Set("DgTaxAmount", Column.Parameter(data.TaxAmount))
                .Set("DgTaxCode", Column.Parameter(data.TaxCode))
                .Set("DgTaxName", Column.Parameter(data.TaxName))
            .Execute();
        }

        protected virtual List<FeeDetail> GetFeeDetail(Guid lineDetailId)
        {
            var result = new List<FeeDetail>();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("DgFeeItemCode", esq.AddColumn("DgFeeItemCode"));
            columns.Add("DgOfferID", esq.AddColumn("DgOfferID"));
            columns.Add("DgFeeName", esq.AddColumn("DgFeeName"));
            columns.Add("DgFeeType", esq.AddColumn("DgFeeType"));
            columns.Add("DgOFSCode", esq.AddColumn("DgOFSCode"));
            columns.Add("DgPaymentType", esq.AddColumn("DgPaymentType"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", lineDetailId));
            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities)
            {
                result.Add(new FeeDetail() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    DgFeeItemCode = entity.GetTypedColumnValue<string>(columns["DgFeeItemCode"].Name),
                    DgOfferID = entity.GetTypedColumnValue<string>(columns["DgOfferID"].Name),
                    DgFeeName = entity.GetTypedColumnValue<string>(columns["DgFeeName"].Name),
                    DgFeeType = entity.GetTypedColumnValue<string>(columns["DgFeeType"].Name),
                    DgOFSCode = entity.GetTypedColumnValue<string>(columns["DgOFSCode"].Name),
                    DgPaymentType = entity.GetTypedColumnValue<string>(columns["DgPaymentType"].Name),
                });           
            }

            return result;
        }

        protected virtual List<Dictionary<string, string>> GetOracleFixCode(List<string> OfferNames)
        {
            var result = new List<Dictionary<string, string>>();
            if(OfferNames == null || OfferNames.Count == 0) {
                return result;
            }

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOfferingRSRC");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("OfferID", esq.AddColumn("DgOfferID"));
            columns.Add("OfferName", esq.AddColumn("DgContractName"));
            columns.Add("ResourceModelId", esq.AddColumn("DgOracleItemCode"));
            columns.Add("OFSCode", esq.AddColumn("DgOraclePackageCode"));
            
            var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach(var offerName in OfferNames) {
                filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgContractName", offerName));
            }

            esq.Filters.Add(filterGroup);

            var entities = esq.GetEntityCollection(UserConnection);
            if(entities.FirstOrDefault() == null) {
                return result;
            }
            
            foreach (var entity in entities) {
                result.Add(new Dictionary<string, string>() {
                    {"OfferID", entity.GetTypedColumnValue<string>(columns["OfferID"].Name)},
                    {"OfferName", entity.GetTypedColumnValue<string>(columns["OfferName"].Name)},
                    {"ResourceModelId", entity.GetTypedColumnValue<string>(columns["ResourceModelId"].Name)},
                    {"OFSCode", entity.GetTypedColumnValue<string>(columns["OFSCode"].Name)},
                });
            }

            return result;
        }

        protected virtual Guid GetSubmissionType()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubmission");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("SubmissionTypeId", esq.AddColumn("DgSubmissionType.Id"));
            var entity = esq.GetEntity(UserConnection, this.submissionId);

            return entity != null ? 
                entity.GetTypedColumnValue<Guid>(columns["SubmissionTypeId"].Name) : Guid.Empty;
        }

        public virtual List<Guid> GetLineDetail()
        {
            var result = new List<Guid>();

            string sql = $@"SELECT
                    DgLineDetail.Id Id
                FROM DgLineDetail
                WHERE 
                    DgLineDetail.DgSubmissionId = '{this.submissionId.ToString()}'
                ORDER BY DgLineDetail.DgLineId ASC, DgLineDetail.DgNo ASC";

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
				using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
					while (dataReader.Read()) {
						result.Add(dataReader.GetColumnValue<Guid>("Id"));
					}
				}
			}

            return result;
        }

        protected void UpdateFeeDetail(Dictionary<string, object> data, TaxList taxList)
        {
            Guid lineDetailId = (Guid)data["DgLineDetailId"];
            string OfferID = data.ContainsKey("DgOfferID") ? data["DgOfferID"]?.ToString() ?? string.Empty : string.Empty;
            string FeeName = data.ContainsKey("DgFeeName") ? data["DgFeeName"]?.ToString() ?? string.Empty : string.Empty;
            string FeeType = data.ContainsKey("DgFeeType") ? data["DgFeeType"]?.ToString() ?? string.Empty : string.Empty;
            string FeeItemCode = data.ContainsKey("DgFeeItemCode") ? data["DgFeeItemCode"]?.ToString() ?? string.Empty : string.Empty;
            string OFSCode = data.ContainsKey("DgOFSCode") ? data["DgOFSCode"]?.ToString() ?? string.Empty : string.Empty;
            string PaymentType = data.ContainsKey("DgPaymentType") ? data["DgPaymentType"]?.ToString() ?? string.Empty : string.Empty;
			bool isPenaltyFees = FeeName == "Penalty Fee" && FeeItemCode == "910017";

            Guid feeDetailId = EntityHelper.GetEntityId(UserConnection, "DgFeeDetail", new Dictionary<string, object>() {
                {"DgLineDetailId", lineDetailId},
                {"DgOfferID", OfferID},
                {"DgFeeName", FeeName},
                {"DgFeeType", FeeType},
                {"DgFeeItemCode", FeeItemCode},
                {"DgPaymentType", PaymentType}
            });
            if(feeDetailId == Guid.Empty) {
                feeDetailId = EntityHelper.CreateEntity(UserConnection, "DgFeeDetail", data);
            } else {
				if(isPenaltyFees) {
					return;
				}
				
                var feeDetail = EntityHelper.GetEntity(UserConnection, "DgFeeDetail", feeDetailId, new Dictionary<string, string>() {
                    {"DgWaiveAmount", "decimal"}
                });

                decimal originalFeeAmount = Convert.ToDecimal(data["DgOriginalFeeAmount"]);
                decimal waiveAmount = Convert.ToDecimal(feeDetail["DgWaiveAmount"]);
                decimal netAmount = waiveAmount > 0 ? originalFeeAmount - waiveAmount : Convert.ToDecimal(data["DgNetAmount"]);

                var update = new Update(UserConnection, "DgFeeDetail")
					.Set("DgOfferID", Column.Parameter(OfferID))
                    .Set("DgFeeName", Column.Parameter(FeeName))
                    .Set("DgFeeItemCode", Column.Parameter(FeeItemCode))
                    .Set("DgFeeType", Column.Parameter(FeeType))
                    .Set("DgOFSCode", Column.Parameter(OFSCode))
                    .Set("DgPaymentType", Column.Parameter(PaymentType))
                    .Set("DgPayType", Column.Parameter(data["DgPayType"]?.ToString() ?? string.Empty))
                    .Set("DgFeeAmount", Column.Parameter(Convert.ToDecimal(data["DgFeeAmount"])))
                    .Set("DgNetAmount", Column.Parameter(netAmount))
                    .Set("DgOriginalFeeAmount", Column.Parameter(originalFeeAmount));

                if(data.ContainsKey("DgResCode")) {
                    update.Set("DgResCode", Column.Parameter(data["DgResCode"]?.ToString() ?? string.Empty));
                }

                if(data.ContainsKey("DgResType")) {
                    update.Set("DgResType", Column.Parameter(data["DgResType"]?.ToString() ?? string.Empty));
                }

                if(data.ContainsKey("DgResModeID")) {
                    update.Set("DgResModeID", Column.Parameter(data["DgResModeID"]?.ToString() ?? string.Empty));
                }

                update
                    .Where("Id").IsEqual(Column.Parameter(feeDetailId))
                    .Execute();
            }

            if (taxList != null) {
				new Delete(UserConnection)
					.From("DgTaxListDetail")
					.Where("DgFeeDetailId").IsEqual(Column.Parameter(feeDetailId))
				.Execute();
				
                foreach (var taxRecord in taxList.TaxRecord) {
                    SaveTax(feeDetailId, taxRecord);
                }
            }
        }
		
		protected List<string> GetOfferContractWithoutBox(LineDetailSelected Selected)
		{
			var withoutBox = new List<string>();
			
			var offerContractList = Selected.SuppOfferList.Where(item => item.IsContractElement).ToList();
			var offerWithBox = GetOfferIdWithBox(Selected.Id);
			
			foreach(var offerContract in offerContractList) {
				if(offerWithBox.FindIndex(item => item == offerContract.OfferID) == -1) {
					withoutBox.Add(offerContract.OfferName);	
				}
			}
			
			return withoutBox;
		}
		
		protected List<string> GetOfferIdWithBox(Guid LineDetailId)
		{
			var result = new List<string>();
			
			var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
			columns.Add("offerId", esq.AddColumn("DgOfferID"));
			
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", LineDetailId));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));
			
			var entities = esq.GetEntityCollection(UserConnection);
			foreach(var entity in entities) {
				string offerId = entity.GetTypedColumnValue<string>(columns["offerId"].Name);
				result.Add(offerId);
			}
			
			return result;
		}
    }

    public class FeeDetail
    {
        public Guid Id { get; set; }
        public string DgFeeItemCode { get; set; }
        public string DgOfferID { get; set; }
        public string DgFeeName { get; set; }
        public string DgFeeType { get; set; }
        public string DgOFSCode { get; set; }
        public string DgPaymentType { get; set; }
    }
}