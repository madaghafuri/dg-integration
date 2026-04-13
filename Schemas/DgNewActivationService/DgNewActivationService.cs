using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgCRMIntegration;
using DgSubmission.DgHistorySubmissionService;
using DgSubmission.DgLineDetail;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using LookupConst = DgMasterData.DgLookupConst;
using ModifyAccountInfo_Request = DgCRMIntegration.DgModifyAccountInfo.Request;

namespace DgIntegration.DgLineActivation
{
    public class NewActivationService
    {
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private CRMService CRMService;
        private List<LineDetail> lineDetailSelected;
        private Guid submissionId;

        public NewActivationService(UserConnection UserConnection, List<LineDetail> LineSelected)
        {
            this.userConnection = UserConnection;
            this.lineDetailSelected = LineSelected;
            this.submissionId = LineSelected.Select(item => item.SubmissionId).FirstOrDefault();

            this.CRMService = new CRMService(UserConnection, true, "NEW");
        }

        public virtual async Task<List<LineResult>> Process()
        {
            var result = new List<LineResult>();
            
            // getPhoneNumbers validation
            this.lineDetailSelected = await this.lineDetailSelected.NewIntegration(UserConnection);	
            List<int> indexValidLine = this.lineDetailSelected
                .Where(item => item.IntegrationMessage.Count == 0)
                .Select((item, index) => index)
                .ToList();
            
            // jika tidak ada yg valid dari getPhoneNumbes
            if(indexValidLine.Count == 0) {
                return LineActivation.Response(this.lineDetailSelected);
            }

            var firstLine = this.lineDetailSelected.FirstOrDefault();
            
            string groupId = firstLine.SubParentGroupID;
            if(string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(firstLine.SubParentGroupNo)) {
                var queryVPN = await CRMService.QueryVPNGroupSubscriberByGroupNo(firstLine.SubParentGroupNo);
                if(queryVPN == null || (queryVPN != null && queryVPN.Count == 0)) {
                    throw new Exception($"Corporate customer cannot be found in CRM based on submitted Group Number {firstLine.SubParentGroupNo}");
                }

                groupId = queryVPN.FirstOrDefault().groupId;
                UpdateGroupId(firstLine.CRMGroupId, groupId);
            }

            string subParentGroupCustomerId = firstLine.SubParentGroupCustomerID;
            if(string.IsNullOrEmpty(subParentGroupCustomerId) && !string.IsNullOrEmpty(firstLine.SubParentGroupNo)) {
                subParentGroupCustomerId = await GetCustomerID(firstLine.SubParentGroupNo);
                if(string.IsNullOrEmpty(subParentGroupCustomerId)) {
                    throw new Exception($"Corporate customer cannot be found in CRM based on submitted Group Number {firstLine.SubParentGroupNo}");
                }

                UpdateCustomerId(firstLine.CRMGroupId, subParentGroupCustomerId);
            }

            string subParentGroupAccountId = firstLine.SubParentGroupAccountID;
            string subParentGroupAccountCode = firstLine.SubParentGroupAccountCode;
            string subParentGroupPaymentId = firstLine.SubParentGroupPaymentID;

            if(!string.IsNullOrEmpty(subParentGroupCustomerId) && (string.IsNullOrEmpty(subParentGroupAccountId) || string.IsNullOrEmpty(subParentGroupPaymentId))) {
                var account = await GetAccount(subParentGroupCustomerId);
                if(account != null) {
                    subParentGroupAccountId = account.accountId;
                    if(string.IsNullOrEmpty(subParentGroupAccountCode)) {
                        subParentGroupAccountCode = account.accountCode;
                    }

                    if(string.IsNullOrEmpty(subParentGroupPaymentId)) {
                        subParentGroupPaymentId = account.paymentModeInfo?.paymentId;
                    }
                    
                    UpdateAccountInfo(firstLine.CRMGroupId, subParentGroupAccountId, subParentGroupAccountCode, account.billcycleType, subParentGroupPaymentId);
                }
            }

            // modify account if account code not null && credit card && pr mode full
            // lakukan untuk semua line yang sesuai dengan kategori
            if(!string.IsNullOrEmpty(subParentGroupAccountCode)) {
                var lineForModify = new List<LineDetail>();
                foreach (var index in indexValidLine) {
                    var item = this.lineDetailSelected[index];
                    if(item.PaymentMode?.Code == "DDCC" && item.PRPC?.Code == "0") {
                        lineForModify.Add(item);
                    }
                }

                if(lineForModify.Count > 0) {
                    var tasksModifyAccount = new List<Task>();
                    foreach (var item in lineForModify) {
                        tasksModifyAccount.Add(ModifyAccountInfo(subParentGroupPaymentId, item));
                    }

                    Task tasksModifyAccountResult = null;
                    try {
                        tasksModifyAccountResult = Task.WhenAll(tasksModifyAccount);
                        await tasksModifyAccountResult;
                    } catch(Exception e) {}
                }
            }

            bool isCreateCustomer = false;
            for (int i = 0; i < indexValidLine.Count; i++) {
                var item = this.lineDetailSelected[indexValidLine[i]];

                string customerID = item.CustomerID;
                if(string.IsNullOrEmpty(customerID)) {
                    var idType = item.IDType;
                    string idNo = item.IDNo;

                    customerID = await GetCustomerID(idType.Code, idNo);
                    if(string.IsNullOrEmpty(customerID)) {
                        isCreateCustomer = true;
                    } else {
                        UpdateCustomerId(customerID);
                    }
                }

                await CreateNewSubscriber(this.lineDetailSelected[indexValidLine[i]]);
                if(isCreateCustomer) {
                    await Task.Delay(500);
                    isCreateCustomer = false;
                }
            }

            return LineActivation.Response(this.lineDetailSelected);

            /*
            string customerId = firstLine.CustomerID;

            // jika customerId sudah ada, lakukan createNew untuk semua line
            if(!string.IsNullOrEmpty(customerId)) {
                for (int i = 0; i < indexValidLine.Count; i++) {
                    await CreateNewSubscriber(this.lineDetailSelected[indexValidLine[i]]);
                }

                return LineActivation.Response(this.lineDetailSelected);
            }

            var idType = this.lineDetailSelected.Select(item => item.IDType).FirstOrDefault();
            string idNo = this.lineDetailSelected.Select(item => item.IDNo).FirstOrDefault();
            
            // jika belum ada, maka lakukan get customer id
            // jika ketemu, maka lakukan createNew untuk semua line
            customerId = await GetCustomerID(idType.Code, idNo);
            if(!string.IsNullOrEmpty(customerId)) {
                UpdateCustomerId(customerId);

                for (int i = 0; i < indexValidLine.Count; i++) {
                    await CreateNewSubscriber(this.lineDetailSelected[indexValidLine[i]]);
                }

                return LineActivation.Response(this.lineDetailSelected);
            }

            // jika belum ada, maka lakukan dlu createNew untuk line pertama
            int firstActivationIndex = indexValidLine.FirstOrDefault();
            await CreateNewSubscriber(this.lineDetailSelected[firstActivationIndex]);
            
            if(this.lineDetailSelected[firstActivationIndex].IntegrationMessage.Count == 0) {
                await Task.Delay(500);

                customerId = await GetCustomerID(idType.Code, idNo);
                if(string.IsNullOrEmpty(customerId)) {
                    throw new Exception($"Customer ID with [{idType.Name}] {idNo} not found");
                }

                UpdateCustomerId(customerId);
            }

            // jika hanya 1 line, maka langsung return
            if(indexValidLine.Count == 1) {
                var line = this.lineDetailSelected[firstActivationIndex];
                bool isSuccess = line.IntegrationMessage.Count == 0;

                return LineActivation.Response(this.lineDetailSelected);
            }

            // lakukan aktivasi ke sisa line
            for (int i = 1; i < indexValidLine.Count; i++) {
                await CreateNewSubscriber(this.lineDetailSelected[indexValidLine[i]]);
            }

            return LineActivation.Response(this.lineDetailSelected);
            */
        }

        protected virtual async Task CreateNewSubscriber(LineDetail Selected)
        {
            string message = string.Empty;

            try {
				var createNewSub = await this.CRMService.CreateNewSubscriber(Selected);
                var resReply = createNewSub.ResultOfOperationReply;
                var resOrder = createNewSub.ResultOfMultipleOrdersReply.FirstOrDefault();
                
                string transactionId = resReply.transactionId;
                string orderId = resOrder.orderId;
                if(string.IsNullOrEmpty(orderId)) {
                    throw new Exception("Order Id is empty");
                }

                UpdateLineDetail(Selected.Id, orderId, transactionId);
                HistorySubmissionService.ReleaseActivation(
                    UserConnection: UserConnection,
                    LineDetailId: Selected.Id,
                    CreatedById: UserConnection.CurrentUser.ContactId
                );

                if(Selected.Source.Id == LookupConst.Source.SFA) {
                    await LineActivation.SFAActivationStatus(UserConnection, Selected, "Released");
                }
            } catch (Exception e) {
                string errorMessage = e.Message;
                if(errorMessage.Contains("24002")) {
                    errorMessage = errorMessage.Replace("24002: ", "") + " - Please change the sim card";
                } else if(errorMessage.Contains("24007")) {
                    errorMessage = errorMessage.Replace("24007: ", "") + " - Please check the mobtel or msisdn";
                }

                message = $"No. {Selected.No} - {Selected.MSISDN} fail. {errorMessage}";
                SetIntegrationResult(Selected.Id, "CreateNewSubscriber", message);

                new Update(UserConnection, "DgLineDetail")
                    .Set("DgReleased", Column.Parameter(false))
                    .Set("DgActivationOrderID", Column.Parameter(string.Empty))
                    .Set("DgActivationTransactionId", Column.Parameter(string.Empty))
                    .Set("DgReleasedDate", Column.Parameter(null, "DateTime"))
                    .Set("DgReleasedById", Column.Parameter(null, "Guid"))
                    .Where("Id").IsEqual(Column.Parameter(Selected.Id))
                    .Execute();
            }
        }

        protected virtual async Task<string> GetCustomerID(string IDType, string IDNo)
        {
            var customers = await CRMService.GetCustomers(IDType, IDNo);
            return customers?.FirstOrDefault().customerId ?? string.Empty;
        }

        protected virtual async Task<string> GetCustomerID(string GroupNo)
        {
            var customers = await CRMService.GetCustomersByGroupNo(GroupNo);
            return customers?.FirstOrDefault().customerId ?? string.Empty;
        }

        protected virtual async Task<AccountValue> GetAccount(string CustomerID)
        {
            var accounts = await CRMService.GetAccountsByCustomerId(CustomerID);
            return accounts?.FirstOrDefault();
        }

        protected virtual async Task ModifyAccountInfo(string PaymentID, LineDetail Line)
        {
            var param = new ModifyAccountInfo_Request.ModifyAccountInfoValue();

            var accountInfo = new AccountValue();
            accountInfo.accountId = Line.SubParentGroupAccountID;
            accountInfo.billcycleType = Line.BillCycle;
            accountInfo.title = Line.Title.Code;
            accountInfo.accountName = Line.SubParentGroupName;
            accountInfo.converge_flag = "1";
            accountInfo.status = "0";
            accountInfo.email = Line.PRPC.Code == "0" ? "dummy@digi.com.my" : Line.Email;

            var paymentModeInfo = new PaymentModeValue();
            paymentModeInfo.paymentId = PaymentID;
            paymentModeInfo.paymentMode = Line.PaymentMode.Code;
            paymentModeInfo.ownerName = Line.CardOwnerName;
            paymentModeInfo.ownershipType = "0";

            if(Line.PaymentMode != null && Line.PaymentMode.Code == "DDCC") {
                paymentModeInfo.tokenId = Line.TokenID;
                paymentModeInfo.cardType = Line.CardType?.Code;
                paymentModeInfo.bankAcctNo = Line.CardNumberEncrypt;
                paymentModeInfo.bankIssuer = Line.Bank?.Code;
                paymentModeInfo.cardExpDate = Line.CardExpiryDate;
            }

            accountInfo.paymentModeInfo = paymentModeInfo;

            param.operationBusinessCode = "CB080";
            param.accountInfo = accountInfo;

            try {
                var modifyAccoutnInfo = await CRMService.ModifyAccountInfo(param);
                if(modifyAccoutnInfo != null) {
                    string remark = $"[ModifyAccountInfo] {Line.MSISDN} Updated credit card during released for {Line.SubmissionType.Code}";
                    new Insert(UserConnection)
                        .Into("DgHistorySubmission")
                        .Set("CreatedOn", Column.Parameter(DateTime.UtcNow))
                        .Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                        .Set("DgSubmissionId", Column.Parameter(this.submissionId))
                        .Set("DgOpsId", Column.Parameter(LookupConst.Ops.UPDATE))
                        .Set("DgSectionId", Column.Parameter(LookupConst.Section.CRA_LINE))
                        .Set("DgRemark", Column.Parameter(remark))
                    .Execute();
                }
            } catch(Exception e) {}
        }

        // update customerId on submission
        protected virtual void UpdateCustomerId(string CustomerID)
        {
            new Update(UserConnection, "DgSubmission")
                .Set("DgCustomerId", Column.Parameter(CustomerID))
                .Where("Id").IsEqual(Column.Parameter(this.submissionId))
            .Execute();

            for(int i=0; i<this.lineDetailSelected.Count; i++) {
                this.lineDetailSelected[i].CustomerID = CustomerID;
            }
        }

        // update customerId on CRMGroup
        protected virtual void UpdateCustomerId(Guid CRMGroupId, string CustomerID)
        {
            new Update(UserConnection, "DgCRMGroup")
                .Set("DgSubParentCustomerId", Column.Parameter(CustomerID))
                .Where("Id").IsEqual(Column.Parameter(CRMGroupId))
            .Execute();

            for(int i=0; i<this.lineDetailSelected.Count; i++) {
                this.lineDetailSelected[i].SubParentGroupCustomerID = CustomerID;
            }
        }

        // update groupId on crm group
        protected virtual void UpdateGroupId(Guid CRMGroupId, string GroupID)
        {
            new Update(UserConnection, "DgCRMGroup")
                .Set("DgSubParentGroupID", Column.Parameter(GroupID))
                .Where("Id").IsEqual(Column.Parameter(CRMGroupId))
            .Execute();

            for(int i=0; i<this.lineDetailSelected.Count; i++) {
                this.lineDetailSelected[i].SubParentGroupID = GroupID;
            }
        }

        protected virtual void UpdateAccountInfo(Guid CRMGroupId, string AccountID, string AccountCode, string BillCycleType, string PaymentID)
        {
            var update = new Update(UserConnection, "DgCRMGroup");
            if(!string.IsNullOrEmpty(AccountID)) {
                update.Set("DgSubParentAccountId", Column.Parameter(AccountID));
            }

            if(!string.IsNullOrEmpty(AccountCode)) {
                update.Set("DgSubParentAccountCode", Column.Parameter(AccountCode));
            }

            if(!string.IsNullOrEmpty(BillCycleType)) {
                update.Set("DgBillingCycle", Column.Parameter(BillCycleType));
            }

            if(!string.IsNullOrEmpty(PaymentID)) {
                update.Set("DgSubParentPaymentId", Column.Parameter(PaymentID));
            }

            update
                .Where("Id").IsEqual(Column.Parameter(CRMGroupId))
                .Execute();

            for(int i=0; i<this.lineDetailSelected.Count; i++) {
                if(!string.IsNullOrEmpty(AccountID)) {
                    this.lineDetailSelected[i].SubParentGroupAccountID = AccountID;
                }

                if(!string.IsNullOrEmpty(AccountCode)) {
                    this.lineDetailSelected[i].SubParentGroupAccountCode = AccountCode;
                }

                if(!string.IsNullOrEmpty(BillCycleType)) {
                    this.lineDetailSelected[i].BillCycle = BillCycleType;
                }

                if(!string.IsNullOrEmpty(PaymentID)) {
                    this.lineDetailSelected[i].SubParentGroupPaymentID = PaymentID;
                }
            }
        }

        protected virtual void UpdateLineDetail(Guid LineDetailId, string OrderId, string TransactionId)
        {
            var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgLineDetail");
            var entity = schema.CreateEntity(UserConnection);

            entity.FetchFromDB("Id", LineDetailId);
            entity.SetColumnValue("DgActivationOrderID", OrderId);
            entity.SetColumnValue("DgActivationTransactionId", TransactionId);
            entity.SetColumnValue("DgReleasedDate", DateTime.UtcNow);
            entity.SetColumnValue("DgReleasedById", UserConnection.CurrentUser.ContactId);
            entity.SetColumnValue("DgActivationStatusId", LookupConst.ActivationStatus.Released);

            entity.Save(false);
        }

        protected virtual void SetIntegrationResult(Guid LineDetailId, string Name, string Message)
        {
            int index = this.lineDetailSelected.FindIndex(item => item.Id == LineDetailId);
            this.lineDetailSelected[index].IntegrationMessage.Add($"[{Name}] {Message}");
        }
    }
}