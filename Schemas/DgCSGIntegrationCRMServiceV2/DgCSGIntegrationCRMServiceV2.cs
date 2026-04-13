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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using System.Text.RegularExpressions;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using DgIntegration.DgValidateCorporateOrderService;
using ValidateCorporateOrder_Response = DgIntegration.DgValidateCorporateOrderService.Response;
using DgIntegration.DgSubmitNewCorpCustomerOrderService;
using Quartz;
using Terrasoft.Core.Scheduler;
using DgIntegration.DgGetCustomerService;

namespace DgIntegration.DgCSGIntegrationCRMService
{
    public class CSGIntegrationCRMServiceV2
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private Guid submissionId;
        private Guid crmGroupId;
        private string hierarcy;

        public CSGIntegrationCRMServiceV2(UserConnection UserConnection, Guid SubmissionId, bool IsParent = true)
        {
            this.userConnection = UserConnection;
            this.submissionId = SubmissionId;
            this.hierarcy = IsParent ? "1" : "2";
        }

        public async Task<GeneralResponse> SaveInCRM()
        {
            var result = new GeneralResponse();

            try {
                this.crmGroupId = GetCRMGroupId();
                if(this.crmGroupId == Guid.Empty) {
                    throw new Exception("CRM Group not found");
                }
 
                var validateCorpOrder = await ValidateCorporateOrder();

                if(this.hierarcy == "2") {
                    var customerRecord = validateCorpOrder.Body.ValidateCorporateOrderResponse.CustomerList.CustomerRecord.CorporateInfo;
                    string topParentCustomerId = customerRecord.CorporateHierarchy.TopParentCustomerId.Text;
                    string parentCustomerId = customerRecord.CorporateDetails.CorporateId.Text;

                    new Update(UserConnection, "DgCRMGroup")
                        .Set("DgTopParentCustomerId", Column.Parameter(topParentCustomerId))
                        .Set("DgCorporateNumber", Column.Parameter(parentCustomerId))
                        .Where("Id").IsEqual(Column.Parameter(this.crmGroupId))
                    .Execute();
                }

                var validateCorporateOrderResponse = validateCorpOrder.Body.ValidateCorporateOrderResponse;
                string actionCode = validateCorporateOrderResponse.ValidationResult.ActionCode;
                string orderId = validateCorporateOrderResponse.OrderId;
                await SubmitNewCorpCustomerOrder(actionCode, orderId);

                new Update(UserConnection, "DgCRMGroup")
					.Set(this.hierarcy == "2" ? "DgIsSubGroupCreateInCRM" : "DgIsGroupCreateInCRM", Column.Parameter(true))
                    .Where("Id").IsEqual(Column.Parameter(this.crmGroupId))
				    .Execute();
				
                AutoUpdateGroupNo();

                result.Success = true;
				result.Message = "Successfull send to CRM";
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        public virtual async Task<ValidateCorporateOrder_Response.Envelope> ValidateCorporateOrder()
        {
            var validateCorpOrder = new ValidateCorporateOrderService(UserConnection);
            validateCorpOrder.SetParam(this.submissionId, this.hierarcy);
            
            var param = validateCorpOrder.GetRequest();
            string dealerCode = param.Body.ValidateCorporateOrderRequest.Dealer.DealerCode;
            if(string.IsNullOrEmpty(dealerCode)) {
                throw new Exception("Dealer Name cannot be null");
            }
            
            await validateCorpOrder.Request();
            if(!validateCorpOrder.IsSuccessResponse()) {
                throw new Exception(validateCorpOrder.GetErrorResponse());
            }

            return validateCorpOrder.GetResponse();
        }

        public virtual async Task SubmitNewCorpCustomerOrder(string ActionCode, string OrderId)
        {
            var submitNewCorpCustomerOrder = new SubmitNewCorpCustomerOrderService(UserConnection);
            submitNewCorpCustomerOrder.SetParam(this.submissionId, ActionCode, OrderId, this.hierarcy);

            var param = submitNewCorpCustomerOrder.GetRequest();
            var billMediumList = param.Body.SubmitNewCorpCustomerOrderRequest.CorporateGroup.Account.NewAccount.BillMediumList.BillMedium;
            if(billMediumList.Count == 0) {
                throw new Exception("Bill Medium Name cannot be null");
            }

            var accountManagerInfo = param.Body.SubmitNewCorpCustomerOrderRequest.CorporateCustomer.AccountManagerInfo;
            string dealerName = accountManagerInfo.Name;
            string dealerCode = accountManagerInfo.DealerCode;
            string dealerPhoneNumber = accountManagerInfo.PhoneNumber;
            string dealerEmail = accountManagerInfo.Email;

            if (string.IsNullOrEmpty(dealerName))  {
                throw new Exception("Dealer Name cannot be null");
            }

            if (string.IsNullOrEmpty(dealerPhoneNumber))  {
                throw new Exception("Dealer Phone Number cannot be null");
            }

            if (string.IsNullOrEmpty(dealerEmail))  {
                throw new Exception("Dealer Email cannot be null");
            }

            if (string.IsNullOrEmpty(dealerCode)) {
                throw new Exception("Dealer Code cannot be null");
            }

            await submitNewCorpCustomerOrder.Request();
            if(!submitNewCorpCustomerOrder.IsSuccessResponse()) {
                throw new Exception(submitNewCorpCustomerOrder.GetErrorResponse());
            }
        }

        protected virtual void AutoUpdateGroupNo()
        {
            string businessProcessName = "DgBPAutoUpdateGroupNo";
            var parameters = new Dictionary<string, object> {
				{"CRMGroupId", this.crmGroupId},
                {"Hierarcy", this.hierarcy}
			};
            JobOptions jobOptions;
			if (!UserConnection.GetIsFeatureEnabled("UseDefaultImportJobOptions")) {
				jobOptions = new JobOptions {
					RequestsRecovery = false
				};
			} else {
				jobOptions = JobOptions.Default;
			}

            UserConnection.RunProcess(
                businessProcessName, 
                MisfireInstruction.SimpleTrigger.FireNow, 
                parameters,
				jobOptions
            );
        }

        protected virtual Guid GetCRMGroupId()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubmission");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("CRMGroupId", esq.AddColumn("DgCRMGroup.Id"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.submissionId));
            
            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            return entity != null ? entity.GetTypedColumnValue<Guid>(columns["CRMGroupId"].Name) : Guid.Empty;
        }
    }
}