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
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgSubmission.DgHistorySubmissionService;
using DgSubmission.DgLineDetail;
using DgIntegration.DgCalculateTaxFeeService;

namespace DgIntegration.DgCreateCustomerSalesOrderUERPService
{
    public class SendToUERP : CreateCustomerSalesOrderUERPService
    {
        private Guid SubmissionId;
        private CalculateTaxFeeService CalculateTaxFeeService;
        public SendToUERP(UserConnection userConnection, Guid SubmissionId) : base(userConnection) 
        {
            this.SubmissionId = SubmissionId;
            CalculateTaxFeeService = new CalculateTaxFeeService(userConnection);
        }

        public virtual async Task<GeneralResponse> Process()
        {
            var result = new GeneralResponse();
			
            try {
				var lineDetails = GetLineDetails();
				if(lineDetails.Count == 0) {
					throw new Exception("No line detail selected or can't be processed");
				}
				
				var lineDetailIds = lineDetails.Select(item => item.Id).ToList();
				
				await CalculateTaxFee(lineDetailIds);
			
                SetParamByLineDetail(lineDetailIds);	
                await Request();
				
				CreateCustomerSalesOrderUERPService.InsertLog(UserConnection, GetLog(), GetSONumber(), IsSuccessResponse() ? "SUCCESS" : "FAIL");

                if(!IsSuccessResponse()) {
                    throw new Exception(GetErrorResponse());
                }
				
				// get line detail need to update
				var salesOrderLines = GetRequest().UERPCreateCustomerSalesOrderRequest.SalesOrderLine.Select(item => item.OrigSysLineRef).ToList();
				var lineIdSuccess = salesOrderLines
					.Select(item => item.Substring(0, item.IndexOf('_')))
					.GroupBy(item => item)
					.Select(item => Convert.ToInt32(item.Key))
					.ToList();
					
				var lineIdFail = lineDetails
                    .Select(item => item.LineId)
                    .ToList()
                    .Except(lineIdSuccess).ToList();
				
				UpdateSONumber(lineIdSuccess, lineIdFail);
				
				result.Message = GetResponse().Acknowledgement;
				result.Success = true;
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }
		
		protected virtual List<LineDetail> GetLineDetails()
		{
			var result = new List<LineDetail>();
			
			var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
			columns.Add("No", esq.AddColumn("DgNo"));
			columns.Add("LineId", esq.AddColumn("DgLineId"));
			
			columns["No"].OrderByAsc(0);
			columns["LineId"].OrderByAsc(1);

            // var filterSoNumber = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", ""));
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSOID"));
            // esq.Filters.Add(filterSoNumber);

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsUERP", false));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToUERP", true));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.Id", this.SubmissionId));
			
			var entities = esq.GetEntityCollection(UserConnection);
			foreach(var entity in entities) {
				var data = new LineDetail();
				data.Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
				data.LineId = entity.GetTypedColumnValue<int>(columns["LineId"].Name);
				
				result.Add(data);
			}
			
			return result;
		}
		
        protected virtual async Task CalculateTaxFee(List<Guid> LineDetails)
        {
			try {
				await CalculateTaxFeeService
					.SetParam(LineDetails)
					.Request();
					
				var errorReq = CalculateTaxFeeService.GetBatchError();
				if(errorReq.Count > 0) {
					throw new Exception(string.Join("\n", errorReq));
				}
				
				var response = CalculateTaxFeeService.GetBatchResponse();
				for (int i = 0; i < CalculateTaxFeeService.LineDetailList.Count; i++) {
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
							.Where("DgLineDetailId").IsEqual(Column.Parameter(CalculateTaxFeeService.LineDetailList[i]))
							.And("DgFeeItemCode").IsEqual(Column.Parameter(feeItemCode))
						.Execute();
					} catch(Exception e) {
						continue;
					}
				}
			} catch(Exception e) {}
        }
		
        protected virtual void UpdateSONumber(List<int> LineIdsSuccess, List<int> LineIdsFail)
        {
            DateTime now = DateTime.UtcNow;
			string soNumber = GetSONumber();

			foreach(int lineId in LineIdsSuccess) {
				new Update(UserConnection, "DgLineDetail")
					.Set("DgSOID", Column.Parameter(soNumber))
					.Set("DgDateTimeReleased", Column.Parameter(now))
					.Set("Dg3PLReleasedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                    .Set("DgIsUERP", Column.Parameter(true))
					.Set("DgCancelItemIMS", Column.Parameter(false))
					.Where("DgLineId").IsEqual(Column.Parameter(lineId))
				.Execute();
			}
			
			foreach(int lineId in LineIdsFail) {
				new Update(UserConnection, "DgLineDetail")
					.Set("DgReleasedToUERP", Column.Parameter(false))
					.Where("DgLineId").IsEqual(Column.Parameter(lineId))
				.Execute();
			}
			
			HistorySubmissionService.ReleaseUERP(
				UserConnection: UserConnection,
				SubmissionId: this.SubmissionId,
				SOId: soNumber,
				CreatedById: UserConnection.CurrentUser.ContactId
			);
        }
    }
}