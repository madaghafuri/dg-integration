using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgSubmission.DgLineDetail;
using LookupConst = DgMasterData.DgLookupConst;
using AddVPNGroupMembers_Response = DgCRMIntegration.DgAddVPNGroupMembers.Response;

namespace DgCRMIntegration
{
	[ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class CRMWebService: BaseService
    {		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse AddVPNGroupMembersBySubmission(Guid RecordId) 
		{
			var result = new GeneralResponse();
			
			bool isSuccess = true;
			int maxBatch = 11;
            var processLine = new List<LineDetail>();
			var errorList = new List<string>();
            
			try {
				var data = AddVPNGroupMembersData(RecordId);
				if(data.Count == 0) {
					throw new Exception("There are no lines that can be sent to Add VPN Group Members");
				}
				
				foreach(var line in data) {
					processLine.Add(line);
					if(processLine.Count == maxBatch) {
						try {
							AddVPNGroupMember(processLine);
						} catch(Exception e) {
							errorList.Add(e.Message);
							isSuccess = false;
						} finally {
							processLine.Clear();
						}
					}
				}

				if(processLine.Count > 0) {
					try {
						AddVPNGroupMember(processLine);
					} catch(Exception e) {
						errorList.Add(e.Message);
						isSuccess = false;
					}
				}
				
				result.Success = isSuccess;
				result.Message = string.Join(". ", errorList.ToArray());
			} catch(Exception e) {
				result.Message = e.Message;
			}
			
			return result;
        }
		
		protected void AddVPNGroupMember(List<LineDetail> Data)
		{
			var CRMService = new CRMService(UserConnection, true, "NEW");
			
			var addVPN = CRMService.AddVPNGroupMembers(Data).GetAwaiter().GetResult();
			if(addVPN != null) {
				string orderId = addVPN.ResultOfOperationReply.orderId;
				string transactionId = addVPN.ResultOfOperationReply.transactionId;

				foreach(var item in Data) {
					new Update(UserConnection, "DgLineDetail")
						.Set("DgAddVPNGroupMember", Column.Parameter(true))
						.Where("Id").IsEqual(Column.Parameter(item.Id))
					.Execute();

					string remark = $"{item.MSISDN} added to VPN Group Member. Order ID: {orderId} Transaction ID: {transactionId}";
					new Insert(UserConnection)
						.Into("DgHistorySubmission")
						.Set("CreatedOn", Column.Parameter(DateTime.UtcNow))
						.Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
						.Set("DgSubmissionId", Column.Parameter(item.SubmissionId))
						.Set("DgOpsId", Column.Parameter(LookupConst.Ops.ADDVPN))
						.Set("DgSectionId", Column.Parameter(LookupConst.Section.CRA_LINE))
						.Set("DgRemark", Column.Parameter(remark))
					.Execute();
				}
			}
		}
		
		protected List<LineDetail> AddVPNGroupMembersData(Guid SubmissionId)
		{
			string sql = $@"SELECT 
                DgLineDetail.Id Id,
                DgLineDetail.DgSubmissionId SubmissionId,
				DgSubmission.DgSerialNumber SerialNumber,
                DgLineDetail.DgMSISDN MSISDN,
                DgPRPC.DgCode PRPC_Code,
                DgCRMGroup.DgSubParentGroupID SubParentGroupID,
                DgCRMGroup.DgGroupSubParentNo SubParentGroupNo,
                DgCRMGroup.DgGroupSubParentName SubParentGroupName,
                DgLineDetail.DgCreditLimit CreditLimit,
                DgLineDetail.DgUsername CustomerName,
                DgLineDetail.DgActivationSubscriberId SubscriberID,
				DgDealer.DgDealerID Dealer_Code
            FROM DgLineDetail
            LEFT JOIN DgSubmission ON DgSubmission.Id = DgLineDetail.DgSubmissionId
            LEFT JOIN DgPRPC ON DgPRPC.Id = DgLineDetail.DgPRPCId
            LEFT JOIN DgCRMGroup ON DgCRMGroup.Id = DgSubmission.DgCRMGroupId
            LEFT JOIN DgDealer ON DgDealer.Id = DgCRMGroup.DgDealerId
            LEFT JOIN DgAddVPNQueueDetail ON DgAddVPNQueueDetail.DgLineDetailId = DgLineDetail.Id
            WHERE 
                DgLineDetail.DgActivationStatusId = @activationStatusId
				AND DgSubmission.Id = @submissionId
                AND DgSubmission.DgSubmissionTypeId != @submissionTypeId
                AND DgPRPC.DgCode = @prpc
                AND DgAddVPNGroupMember = @addVPN
                AND DgAddVPNQueueDetail.DgLineDetailId IS NULL";

            var query = new CustomQuery(UserConnection, sql);
            query.Parameters.Add("@activationStatusId", LookupConst.ActivationStatus.Activated.ToString());
			query.Parameters.Add("@submissionTypeId", LookupConst.SubmissionType.COP.ToString());
            query.Parameters.Add("@submissionId", SubmissionId.ToString());
            query.Parameters.Add("@prpc", "3");
            query.Parameters.Add("@addVPN", 0);
            
            var result = new List<LineDetail>();
            using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                using(IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result.Add(new LineDetail() {
                            Id = dataReader.GetColumnValue<Guid>("Id"),
                            SubmissionId = dataReader.GetColumnValue<Guid>("SubmissionId"),
							SerialNumber = dataReader.GetColumnValue<string>("SerialNumber"),
                            MSISDN = dataReader.GetColumnValue<string>("MSISDN"),
                            PRPC = new DgMasterData.Lookup() {
                                Code = dataReader.GetColumnValue<string>("PRPC_Code")
                            },
                            SubParentGroupID = dataReader.GetColumnValue<string>("SubParentGroupID"),
                            SubParentGroupNo = dataReader.GetColumnValue<string>("SubParentGroupNo"),
                            SubParentGroupName = dataReader.GetColumnValue<string>("SubParentGroupName"),
                            CreditLimit = dataReader.GetColumnValue<decimal>("CreditLimit"),
                            CustomerName = dataReader.GetColumnValue<string>("CustomerName"),
                            SubscriberID = dataReader.GetColumnValue<string>("SubscriberID"),
							Dealer = new DgMasterData.Lookup() {
								Code = dataReader.GetColumnValue<string>("Dealer_Code")
							}
                        });
                    }
                }
            }

            return result;
		}
	}
}