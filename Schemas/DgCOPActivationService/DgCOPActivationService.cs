using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.RegularExpressions;
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
using DgMasterData;
using DgSubmission.DgHistorySubmissionService;
using DgSubmission.DgLineDetail;
using DgCRMIntegration;
using DgCRMIntegration.DgChangeSubscriberOffers;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using LookupConst = DgMasterData.DgLookupConst;
using GetCustomer_Request = DgCRMIntegration.DgGetCustomers.Request;
using ModifyAccountInfo_Request = DgCRMIntegration.DgModifyAccountInfo.Request;

namespace DgIntegration.DgLineActivation
{
    public class COPActivationService
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
        private bool isFromAU;

        public COPActivationService(UserConnection UserConnection, List<LineDetail> LineSelected, bool IsFromAU = false)
        {
            this.userConnection = UserConnection;
			this.lineDetailSelected = LineSelected;
            this.submissionId = LineSelected.Select(item => item.SubmissionId).FirstOrDefault();
            this.isFromAU = IsFromAU;

            this.CRMService = new CRMService(UserConnection, true, "COP");
        }

        public virtual async Task<List<LineResult>> Process()
        {
            var result = new List<LineResult>();

            var BundleOfferingList = CRMHelper.GetBundleOfferingFromDB(UserConnection);
            var OfferingMasterList = CRMHelper.GetOfferingFromDB(UserConnection, Guid.Empty);

            for (int i = 0; i < this.lineDetailSelected.Count; i++) {
                var line = this.lineDetailSelected[i];
                bool isCOP_P2P = line.IsCOP_P2P(line); 

                if(isFromAU || !isCOP_P2P) {
                    bool isCustomerValid = await IsCustomerValid(line);
                    if(isCustomerValid) {
                        await ChangeSubscriberOffers(line, BundleOfferingList, OfferingMasterList);
                    }
                } else {
                    AUProcess(line);
                }
            }

            var response = LineActivation.Response(this.lineDetailSelected);
            if(response.Where(item => item.Result.Success).ToList().Count > 0) {
                var firstLine = this.lineDetailSelected.FirstOrDefault();

                // get customer id jika belum ada
                string subParentGroupCustomerId = firstLine.SubParentGroupCustomerID;
                if(!string.IsNullOrEmpty(firstLine.SubParentGroupNo) && string.IsNullOrEmpty(subParentGroupCustomerId)) {
                    subParentGroupCustomerId = await GetCustomerID(firstLine.SubParentGroupNo);
                    if(!string.IsNullOrEmpty(subParentGroupCustomerId)) {
                        UpdateCustomerId(firstLine.CRMGroupId, subParentGroupCustomerId);
                    }
                }

                // get account id jika belum ada
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
                if(!string.IsNullOrEmpty(subParentGroupAccountCode) && firstLine.PaymentMode.Code == "DDCC" && firstLine.PRPC.Code == "0") {
                    await ModifyAccountInfo(subParentGroupPaymentId, firstLine);
                }   
            }

            return response;
        }

        public virtual void AUProcess(LineDetail Selected)
        {
            Guid auActivationId = EntityHelper.GetId(UserConnection, "DgAuActivation", new Dictionary<string, object>() {
                {"DgLineDetailId", Selected.Id}
            });

            if(auActivationId != Guid.Empty) {
                new Update(UserConnection, "DgAuActivation")
                    .Set("DgIsHidden", Column.Parameter(false))
                    .Where("Id").IsEqual(Column.Parameter(auActivationId))
                .Execute();
            } else {
                new Insert(UserConnection)
                    .Into("DgAuActivation")
                    .Set("DgLineDetailId", Column.Parameter(Selected.Id))
                    .Set("DgName", Column.Parameter(Selected.MSISDN))
                .Execute();
            }

            UpdateLineDetail(Selected.Id, string.Empty, string.Empty);
            HistorySubmissionService.ReleaseAuActivation(
                UserConnection: UserConnection,
                LineDetailId: Selected.Id,
                CreatedById: UserConnection.CurrentUser.ContactId
            );
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

        protected virtual async Task<bool> IsCustomerValid(LineDetail Selected)
        {
            bool isValid = false;
            try {
                var param = new GetCustomer_Request.QueryConditionForCorpValue() {
                    memberMsisdn = Helper.GetValidMSISDN(Selected.MSISDN)
                };
                var customers = await this.CRMService.GetCustomers(param);
                if(customers == null) {
                    throw new Exception("Customer not found"); 
                }

                var customer = customers.FirstOrDefault();
                var companyName = customer?.corporationInfo?.companyName;
                var brn = customer?.corporationInfo?.businessRegistrationNumber;

                if(brn != Selected.SubParentGroupBRN) {
                    throw new Exception("Ownership not valid");
                }

                isValid = true;
            } catch(Exception e) {
                string message = $"No. {Selected.No} - {Selected.MSISDN} fail. {e.Message}";
                SetIntegrationResult(Selected.Id, "Check Customer", message);
            }

            return isValid;
        }

        protected virtual async Task ChangeSubscriberOffers(LineDetail Selected, List<BundleOffering> BundleOfferingList, List<Offering> OfferingMasterList)
        {
            string message = string.Empty;

            try {
				var changeSub = await CRMService.ChangeSubscriberOffers(Selected, BundleOfferingList, OfferingMasterList);
                string orderId = changeSub.orderId;
                string transactionId = changeSub.transactionId;
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
            } catch(Exception e) {
                message = $"No. {Selected.No} - {Selected.MSISDN} fail. {e.Message}";
                SetIntegrationResult(Selected.Id, "ChangeSubscriberOffers", message);

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
                paymentModeInfo.cardType = Line.CardType.Code;
                paymentModeInfo.bankAcctNo = Line.CardNumberEncrypt;
                paymentModeInfo.bankIssuer = Line.Bank.Code;
                paymentModeInfo.cardExpDate = Line.CardExpiryDate;
            }

            accountInfo.paymentModeInfo = paymentModeInfo;

            param.operationBusinessCode = "CB080";
            param.accountInfo = accountInfo;

            try {
                var modifyAccoutnInfo = await CRMService.ModifyAccountInfo(param);
                if(modifyAccoutnInfo != null) {
                    string remark = $"[ModifyAccountInfo] Updated credit card during released for {Line.SubmissionType.Code}";
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