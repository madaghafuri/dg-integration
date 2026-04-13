using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using DgBaseService.DgHelpers;
using DgCRMIntegration;
using DgSubmission.DgLineDetail;
using LookupConst = DgMasterData.DgLookupConst;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgCRMIntegration.DgAddVPNGroupMembers
{
    public class AddVPNGroupMembersQueue
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        public static readonly string QueueHeader = "DgAddVPNQueue";
        public static readonly string QueueDetail = "DgAddVPNQueueDetail";
            
        public static void AddQueue(UserConnection UserConnection, Guid SubmissionId, List<Guid> LineDetailIds)
        {
            Guid queueId = Guid.NewGuid();

            new Insert(UserConnection)
                .Into("DgAddVPNQueue")
                .Set("Id", Column.Parameter(queueId))
                .Set("DgSubmissionId", Column.Parameter(SubmissionId))
            .Execute();

            foreach (Guid lineDetailId in LineDetailIds) {
                new Insert(UserConnection)
                    .Into("DgAddVPNQueueDetail")
					.Set("DgQueueId", Column.Parameter(queueId))
                    .Set("DgLineDetailId", Column.Parameter(lineDetailId))
                .Execute();
            }
        }

        public static void RemoveQueue(UserConnection UserConnection, Guid QueueId)
        {
            new Delete(UserConnection)
                .From("DgAddVPNQueue")
                .Where("Id").IsEqual(Column.Parameter(QueueId))
            .Execute();
        }

        public static Guid GetQueueId(UserConnection UserConnection, Guid LineDetailId)
        {
            var select = new Select(UserConnection)
                .Column("DgQueueId")
            .From("DgAddVPNQueueDetail")
            .Where("DgLineDetailId").IsEqual(Column.Parameter(LineDetailId)) as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())  {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        return dataReader.GetColumnValue<Guid>("DgQueueId");
                    }
                }
            }

            return Guid.Empty;
        }

        public static int GetTotalQueue(UserConnection UserConnection, Guid QueueId)
        {
            var select = new Select(UserConnection)
                .Column(Func.Count(Column.Asterisk())).As("Total")
            .From("DgAddVPNQueueDetail")
            .Where("DgQueueId").IsEqual(Column.Parameter(QueueId)) as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())  {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        return dataReader.GetColumnValue<int>("Total");
                    }
                }
            }

            return 0;
        }

        public static int GetTotalProcessQueue(UserConnection UserConnection, Guid QueueId)
        {
            var select = new Select(UserConnection)
                .Column(Func.Count(Column.Asterisk())).As("Total")
            .From("DgAddVPNQueueDetail")
            .Where("DgQueueId").IsEqual(Column.Parameter(QueueId))
			.And()
            .OpenBlock("DgIsProcess").IsEqual(Column.Parameter(true))
				.Or("DgIsFail").IsEqual(Column.Parameter(true))
			.CloseBlock() as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())  {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        return dataReader.GetColumnValue<int>("Total");
                    }
                }
            }

            return 0;
        }

        public static void SetDetailToProcess(UserConnection UserConnection, Guid LineDetailId, bool IsFail = false)
        {
            int rowAffected = new Update(UserConnection, "DgAddVPNQueueDetail")
                .Set("DgIsProcess", Column.Parameter(IsFail ? false : true))
				.Set("DgIsFail", Column.Parameter(IsFail))
            .Where("DgLineDetailId").IsEqual(Column.Parameter(LineDetailId))
            .And("DgIsProcess").IsEqual(Column.Parameter(false))
            .Execute();

            if(rowAffected < 1) {
                return;
            }

            Guid queueId = AddVPNGroupMembersQueue.GetQueueId(UserConnection, LineDetailId);
            int totalQueue = AddVPNGroupMembersQueue.GetTotalQueue(UserConnection, queueId);
            int totalProcess = AddVPNGroupMembersQueue.GetTotalProcessQueue(UserConnection, queueId);
            if(totalQueue > 0 && totalQueue == totalProcess) {
                AddVPNGroupMembersQueue.QueueTrigger(UserConnection, queueId);
            }
        }

        public static void QueueTrigger(UserConnection UserConnection, Guid QueueId)
        {
            var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgAddVPNQueue");
            var entity = schema.CreateEntity(UserConnection);

            entity.FetchFromDB("Id", QueueId);
            entity.SetColumnValue("DgIsProcess", true);
            
            entity.Save(false);
        }

        public async static Task QueueProcess(UserConnection UserConnection, Guid QueueId)
        {
            int maxBatch = 11;

            var processLine = new List<LineDetail>();
            var data = AddVPNGroupMembersQueue.GetQueueData(UserConnection, QueueId);
            foreach(var line in data) {
                processLine.Add(line);
                if(processLine.Count == maxBatch) {
                    string customerName = processLine.FirstOrDefault().CustomerName;
                    string serialNumber = processLine.FirstOrDefault().SerialNumber;
                    string msisdnList = string.Join(", ", processLine.Select(item => item.MSISDN).ToArray());

                    try {
                        await AddVPNGroupMembersQueue.QueueProcess(UserConnection, processLine);
                    } catch(Exception e) {
                        if(e.Message.Contains("20087: ")) {
                            AddVPNGroupMembersQueue.SendEmail(UserConnection, customerName, serialNumber, msisdnList, e.Message);
                        }
                    } finally {
                        processLine.Clear();
                    }
                }
            }

            if(processLine.Count > 0) {
                string customerName = processLine.FirstOrDefault().CustomerName;
                string serialNumber = processLine.FirstOrDefault().SerialNumber;
                string msisdnList = string.Join(", ", processLine.Select(item => item.MSISDN).ToArray());

                try {
                    await AddVPNGroupMembersQueue.QueueProcess(UserConnection, processLine);
                } catch (Exception e) {
                    if(e.Message.Contains("20087: ")) {
                        AddVPNGroupMembersQueue.SendEmail(UserConnection, customerName, serialNumber, msisdnList, e.Message);
                    }
                }
            }

            AddVPNGroupMembersQueue.RemoveQueue(UserConnection, QueueId);
        }

        public async static Task QueueProcess(UserConnection UserConnection, List<LineDetail> Data)
        {
            var CRMService = new CRMService(UserConnection, true, "NEW");
            
            var addVPN = await CRMService.AddVPNGroupMembers(Data);
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

        public static List<LineDetail> GetQueueData(UserConnection UserConnection, Guid QueueId)
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
            LEFT JOIN {AddVPNGroupMembersQueue.QueueDetail} QueueDetail ON QueueDetail.DgLineDetailId = DgLineDetail.Id
            LEFT JOIN {AddVPNGroupMembersQueue.QueueHeader} QueueHeader ON QueueHeader.Id = QueueDetail.DgQueueId
            WHERE 
                DgLineDetail.DgActivationStatusId = @activationStatusId
                AND DgSubmission.DgSubmissionTypeId != @submissionTypeId
                AND DgPRPC.DgCode = @prpc
                AND DgAddVPNGroupMember = @addVPN
                AND QueueHeader.Id = @queueId
                AND QueueHeader.DgIsProcess = @isQueueProcess";

            var query = new CustomQuery(UserConnection, sql);
            query.Parameters.Add("@activationStatusId", LookupConst.ActivationStatus.Activated.ToString());
            query.Parameters.Add("@submissionTypeId", LookupConst.SubmissionType.COP.ToString());
            query.Parameters.Add("@prpc", "3");
            query.Parameters.Add("@addVPN", 0);
            query.Parameters.Add("@queueId", QueueId.ToString());
            query.Parameters.Add("@isQueueProcess", 1);
            
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

        public static void SendEmail(UserConnection UserConnection, string CustomerName, string SerialNumber, string MSISDNList, string Remark)
        {
            try {
                string subject = $"[ERROR] Line Status Update - Pending Business Order - MSISDN {MSISDNList}";		
                string message = $"<b>Your application</b> {CustomerName} Serial Number: {SerialNumber}, MSISDN: {MSISDNList}"
                            + $" is <b>Error in Joining Group</b> with remarks: <br>{Remark}<br>"
                            + $"Please revert back to <b>Order Fulfillment Team</b> for further information."
                            + $"<br><br><b>This message is auto generated by NCCF Web.</b>";
                string email = SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_ErrorAddVPNGroupMember", string.Empty);
                
                var data = new MailParam() {
                    Subject = subject,
                    Message = message,
                    To = email,
					DefaultFooterMessage = true
                };

                Mail.Send(UserConnection, "nccf2-crm@celcomdigi.com", data);
            } catch (Exception e) {}
        }
    }
}