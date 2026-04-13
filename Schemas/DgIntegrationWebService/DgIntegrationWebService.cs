namespace DgIntegrationAPI.DgIntegrationWebService
{
    using System;
    using System.Linq;
  	using System.Collections;
	using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
	using System.Threading;
	using System.Threading.Tasks;
    using Terrasoft.Core;
    using Terrasoft.Core.Entities;
    using Terrasoft.Core.DB;
    using Terrasoft.Web.Common;
	using Newtonsoft.Json;
	using System.Threading.Tasks;
    using DgIntegrationSFAService;
	using DgIntegration.DgRejectCRAndOPService;
    using ISAEntityHelper = ISAEntityHelper.EntityHelper;
	using DgBaseService.DgGenericResponse;
	using DgBaseService.DgHelpers;
	using DgBaseService.DgCreatioIntegrationHelper;
	using DgCRMIntegration;
	using DgSubmission.DgLineDetail;
    using DgIntegration.DgCreateCustomerSalesOrderUERPService;
	using DgIntegration.DgCSGIntegrationCRMService;
	using DgIntegration.DgSendTo3PL;
	using DgIntegration.DgReleaseTo3PL;
	using DgIntegration.DgSCMSGetDealerInfoService;
	using DgIntegration.DgIntegrationRPALogService;
	using DgIntegration.DgLineActivation;
	using DgIntegration.DgCommonInventory;
	using DgCSGIntegration;
	using DgCSGIntegration.DgOrderFees;
	using DgCRMIntegration.DgPortIn;
	using DgSFAIntegation.DgSFALineActivationStatus;
	using LookupConst = DgMasterData.DgLookupConst;
    using PortIn_Response = DgCRMIntegration.DgPortIn.Response;
	using SysSettings = Terrasoft.Core.Configuration.SysSettings;
	using SolarisCore;
    using DgIntegration.DgERP;
    using DgIntegration.DgSendToERP;
    using System.Data;
    using Terrasoft.Common;
    using DgIntegration.DgCancelItemDMS;
    using System.Data;
    using Terrasoft.Common;

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class IntegrationWebService: BaseService
    {				
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public Task<GeneralResponse> SendCRAndORRejectStatus(Guid SubmissionId, string Type) {
            RejectCRAndOPService RejectCRAndOPService = new RejectCRAndOPService(UserConnection);
            return RejectCRAndOPService.SendCRAndORRejectStatus(SubmissionId, Type);
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse SendTo3PLInit(Guid SubmissionId) {
			bool Is3PLbyRole = SysSettings.GetValue<bool>(UserConnection, "DgRelease3PLbyRole", false);
			var ReleaseTo3PLService = new ReleaseTo3PL(UserConnection, SubmissionId);
			bool isDMS = SysSettings.GetValue<bool>(UserConnection, "DgIs3PLWithDMS", false);
			bool isCommonInventory = SysSettings.GetValue<bool>(UserConnection, "DgIs3PLWithCommonInventory", false);
			var SendTo3PLService = new SendTo3PL(UserConnection, SubmissionId);

			if (Is3PLbyRole)
            {
                var roleList = GetRoles(UserConnection);
				if (roleList != null && roleList.Contains("3PL"))
                {
					if (isDMS)
                    {
						return ReleaseTo3PLService.Process().GetAwaiter().GetResult();                  
                    }

					return !isCommonInventory 
						? SendTo3PLService.Process().GetAwaiter().GetResult()
						: SendTo3PLService.ProcessV2().GetAwaiter().GetResult();
                } else
                {
                    return new GeneralResponse()
                    {
                        Success = false,
						Message = "Your user is not allowed to perform Release To 3PL. Please contact System Administrator for access."
                    };
                }
            } else
            {
				if (isDMS)
                {
					return ReleaseTo3PLService.Process().GetAwaiter().GetResult();
                }
				return !isCommonInventory ? 
					SendTo3PLService.Process().GetAwaiter().GetResult() :
					SendTo3PLService.ProcessV2().GetAwaiter().GetResult();
            }
        }

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse SendToUERP(Guid SubmissionId) {
			var isERP = SysSettings.GetValue<bool>(UserConnection, "DgIs3PLWithERP", false);
            var SendToUERP = new SendToUERP(UserConnection, SubmissionId);
			var SendToERP = new SendToERP(UserConnection, SubmissionId);

			var roleList = GetFunctionalRoles(UserConnection);
			if (roleList.Contains("Admin")) {
				if (isERP) {
					return SendToERP.Process().GetAwaiter().GetResult();
				}

            	return SendToUERP.Process().GetAwaiter().GetResult();
			} else {
				if (roleList.Contains("ERP")) {
					return SendToERP.Process().GetAwaiter().GetResult();
				}

				return SendToUERP.Process().GetAwaiter().GetResult();
			}
        }

		protected virtual List<string> GetFunctionalRoles(UserConnection userConnection)
        {
            var roleList = new List<string>();
            var select = new Select(userConnection)
                .Column("roleunit", "Id").As("Id")
                .Column("roleunit", "Name").As("RoleName")
                .From("SysAdminUnit").As("sau")
                .Join(JoinType.LeftOuter, "SysUserInRole").As("suir")
                    .On("suir", "SysUserId").IsEqual("sau", "Id")
                .Join(JoinType.LeftOuter, "SysAdminUnit").As("roleunit")
                    .On("suir", "SysRoleId").IsEqual("roleunit", "Id")
                .Where("sau", "Id").IsEqual(Column.Parameter(userConnection.CurrentUser.Id)) as Select;

            using (DBExecutor dbExecutor = userConnection.EnsureDBConnection()) {
                using (IDataReader reader = select.ExecuteReader(dbExecutor)) {
                    while (reader.Read()) {
                        string role = reader.GetColumnValue<string>("RoleName");

                        if (role == "Admin" || role == "Operation" || role == "ERP") {
                            roleList.Add(role);
                        }
                    }
                }
            }

            return roleList;
        }

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
		public ResultStatus CancelItemIMS(Guid SubmissionId)
		{
			// var CancelItemIMSService = new CancelItemIMSService(UserConnection, SubmissionId);
			// return CancelItemIMSService.Process().GetAwaiter().GetResult();

			bool isDMS = SysSettings.GetValue<bool>(UserConnection, "DgIs3PLWithDMS", false);
			if (isDMS)
			{
				/**
					Cancel In DMS
				**/
				var cancelService = new CancelItemInDMS(UserConnection);
				return cancelService.Process(SubmissionId).GetAwaiter().GetResult();
			}
			else
			{
				var service = new CommonInventoryService(UserConnection);
				return service.CancelIMS(SubmissionId).GetAwaiter().GetResult();
			}

        }

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle =WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
		public GeneralResponse CancelSalesOrder(Guid SubmissionId) {
			var service = new CancelOrderService(UserConnection, SubmissionId);
			return service.Process().GetAwaiter().GetResult();
		}
		
        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse SendPortIn(Guid LineDetailId) {
            var result = new GeneralResponse();
			var lineDetailInfo = ISAEntityHelper.EntityHelper.GetEntity(
				UserConnection, 
				"DgLineDetail", 
				LineDetailId, 
				new Dictionary<string, string>() {
					{"DgSubmissionId", "guid"},
					{"DgMSISDN", "string"},
					{"DgLineId", "int"},
					{"DgUsername", "string"},
					{"DgSubmission.DgSerialNumber", "string"},
					{"DgSubmission.DgCustomerId", "string"},
					{"DgSubmission.DgIDType.DgCode", "string"},
					{"DgSubmission.DgIDNo", "string"}
				}
			);
			Guid submissionId = (Guid)lineDetailInfo["DgSubmissionId"];
			string msisdn = lineDetailInfo["DgMSISDN"]?.ToString() ?? string.Empty;
			int lineId = (int)lineDetailInfo["DgLineId"];
			string customerName = lineDetailInfo["DgUsername"]?.ToString() ?? string.Empty;
			string serialNumber = lineDetailInfo["DgSubmission.DgSerialNumber"]?.ToString() ?? string.Empty;
			string customerId = lineDetailInfo["DgSubmission.DgCustomerId"]?.ToString() ?? string.Empty;
			string idType = lineDetailInfo["DgSubmission.DgIDType.DgCode"]?.ToString() ?? string.Empty;
			string idNumber = lineDetailInfo["DgSubmission.DgIDNo"]?.ToString() ?? string.Empty;
			string transactionId = string.Empty;
			
            try {
				var crmService = new CRMService(UserConnection, true, "MNP");
		
				if(string.IsNullOrEmpty(customerId)) {
					if(idType == "1") {
						idNumber = idNumber.Replace("-", "");
					}

					var customers = crmService.GetCustomers(idType, idNumber).GetAwaiter().GetResult();
					if(customers != null) {
						customerId = customers.FirstOrDefault()?.customerId ?? string.Empty;
						new Update(UserConnection, "DgSubmission")
							.Set("DgCustomerId", Column.Parameter(customerId))
							.Where("Id").IsEqual(Column.Parameter(submissionId))
						.Execute();
					}
				}
				
				var portIn = crmService.PortIn(LineDetailId).GetAwaiter().GetResult();
				if(portIn != null) {
					transactionId = portIn.ResultOfOperationReply.transactionId;
					new Update(UserConnection, "DgLineDetail")
						.Set("DgActivationTransactionId", Column.Parameter(transactionId))
						.Where("Id").IsEqual(Column.Parameter(LineDetailId))
					.Execute();
					
					result.Success = true;
                	result.Message = portIn.ResultOfOperationReply.resultMessage;
				}
            } catch (Exception e) {
                result.Message = e.Message;
            } finally {
				string remark = string.IsNullOrEmpty(transactionId) ? 
					$"[PortIn] Port In MSISDN {msisdn}. {result.Message}" :
					$"[PortIn] Port In MSISDN {msisdn} [TransactionID]{transactionId}. {result.Message}";
				new Insert(UserConnection)
					.Into("DgHistorySubmission")
					.Set("CreatedOn", Column.Parameter(DateTime.UtcNow))
					.Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
					.Set("DgSubmissionId", Column.Parameter(submissionId))
					.Set("DgOpsId", Column.Parameter(LookupConst.Ops.UPDATE))
					.Set("DgSectionId", Column.Parameter(LookupConst.Section.CRA_LINE))
					.Set("DgRemark", Column.Parameter(remark))
					.Set("DgMSISDN", Column.Parameter(msisdn))
					.Set("DgLINE_ID", Column.Parameter(lineId.ToString()))
				.Execute();
				
				if(!result.Success) {
					string subject = $"[ERROR] Port In - MSISDN {msisdn}";
                	string message = $"<b>Your application</b> {customerName} Serial Number: {serialNumber}, MSISDN: {msisdn}"
                        + $" is <b>Failed</b> with remarks: {result.Message}<br><br>"
						+ $"Please revert back to <b>Order Fulfillment Team</b> for further information."
						+ $"<br><br><b>This message is auto generated by NCCF Web.</b>";
					string email = SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_ErrorPortIn", string.Empty);
					if(!string.IsNullOrEmpty(email)) {
						var data = new MailParam() {
							Subject = subject,
							Message = message,
							To = email,
							DefaultFooterMessage = true
						};

						Mail.Send(UserConnection, "nccf2-crm@celcomdigi.com", data);
					}
				}
			}

            return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse SaveInCRMV2(Guid SubmissionId, bool IsParent) {
            var CSGIntegrationCRMService = new CSGIntegrationCRMServiceV2(UserConnection, SubmissionId, IsParent);
            return CSGIntegrationCRMService.SaveInCRM().GetAwaiter().GetResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse RequestOrderFees(Guid CRMGroupId) {
			var service = new OrderFeesInCRMService(UserConnection, CRMGroupId);
			return service.Process().GetAwaiter().GetResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse RequestOrderFeesDeviceOrder(Guid SubmissionId) {
            var service = new OrderFeesInDeviceOrderService(UserConnection, SubmissionId);
			return service.Process().GetAwaiter().GetResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse GetDealerInfo(string DealerCode) {
			var result = new GeneralResponse();
			try {
				SCMSGetDealerInfoService SCMSGetDealerInfoService = new SCMSGetDealerInfoService(UserConnection);
            	return SCMSGetDealerInfoService.GetDealerInfo(DealerCode).GetAwaiter().GetResult();
			} catch(Exception e) {
				result.Message = e.Message;
			}
			
			return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse Activation(Guid SubmissionId) {
            var lineActivation = new LineActivation(UserConnection, SubmissionId);
            return lineActivation.Activation().GetAwaiter().GetResult();
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse ActivationFromAU(Guid LineDetailId) {
			var result = new GeneralResponse();
			
			try {
				var line = new LineDetail(UserConnection, LineDetailId);
				var lines = line.GetLinesActivation(new List<LineDetail>() { line });
				var validations = line.IsValid(lines);
				if(!validations.FirstOrDefault().Result.Success) {
					throw new Exception(validations.FirstOrDefault().Result.Message);
				}
				
				var lineActivation = new COPActivationService(UserConnection, validations.Select(item => item.Line).ToList(), true);
				var lineResult = lineActivation.Process().GetAwaiter().GetResult();
				
				result = lineResult.Select(item => item.Result).FirstOrDefault();
			} catch(Exception e) {
				result.Message = e.ToString();
			}
			
			return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse Cancellation(Guid LineDetailId) {
            var lineActivation = new LineActivation(UserConnection, LineDetailId, true);
            return lineActivation.Cancellation().GetAwaiter().GetResult();
        }
		
		//RPA LOG
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse IntegrasiRPALog() {
            IntegrationRPALogService IntegrationRPALogService = new IntegrationRPALogService(UserConnection);
            return IntegrationRPALogService.IntegrasiRPALog();
        }

        [OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedResponse, ResponseFormat = WebMessageFormat.Json)]
        public GeneralResponse GroupNoValidation(GroupNoValidationRequest Data) {
            var result = new GeneralResponse();
            var crmService = new CRMService(UserConnection);
			
			List<string> errorList = new List<string>();
			foreach(GroupNoList item in Data.GroupNo) {
				string prefix = item.IsParent ? "Parent" : "Sub Parent";
				
				try {
					var queryVPNs = crmService.QueryVPNGroupSubscriberByGroupNo(item.GroupNo).GetAwaiter().GetResult();
					if(queryVPNs == null) {
						errorList.Add($"{prefix} Group No {item.GroupNo} is not exists");
						continue;
					}

					var queryVPN = queryVPNs.FirstOrDefault();
					string customerId = queryVPN.customerId;

					var customers = crmService.GetCustomersById(customerId).GetAwaiter().GetResult();
					if(customers == null) {
						errorList.Add($"{prefix} Group No {item.GroupNo} is not exists");
						continue;
					}

					var customer = customers.FirstOrDefault();
					string brn = customer.corporationInfo.businessRegistrationNumber;
					if(Data.BRN != brn) {
						errorList.Add($"BRN {brn} from {prefix} Group No {item.GroupNo} is not Match with {Data.BRN}");
						continue;
					}

					var accounts = crmService.GetAccountsByCustomerId(customer.customerId).GetAwaiter().GetResult();
					var account = accounts?.FirstOrDefault();
					
					// update customer, account, group id
					var updateCRMGroup = new Update(UserConnection, "DgCRMGroup")
						.Set(item.IsParent ? "DgParentCustomerId" : "DgSubParentCustomerId", Column.Parameter(customer.customerId))
						.Set(item.IsParent ? "DgCustomerCode" : "DgSubParentCustomerCode", Column.Parameter(customer.customerCode))
						.Set(item.IsParent ? "DgCorporateNumber" : "DgSubParentCorporateNumber", Column.Parameter(customer.corporationInfo.corpNumber))
						.Set(item.IsParent ? "DgAccountId" : "DgSubParentAccountId", Column.Parameter(account?.accountId ?? string.Empty))
						.Set(item.IsParent ? "DgAccountCode" : "DgSubParentAccountCode", Column.Parameter(account?.accountCode ?? string.Empty))
						.Set(item.IsParent ? "DgGroupID" : "DgSubParentGroupID", Column.Parameter(queryVPN.groupId));

					if(item.IsParent) {
						updateCRMGroup.Set("DgPaymentId", Column.Parameter(account?.paymentModeInfo?.paymentId ?? string.Empty));
					}
					
					updateCRMGroup
						.Where("Id").IsEqual(Column.Parameter(Data.CRMGroupId))
						.Execute();
				} catch(Exception e) {
					errorList.Add($"Something wrong happen in {prefix} Group. {e.Message}");
				}
			}

			var customerSubmissions = crmService.GetCustomers(
				Data.IdType, 
				Data.IdType == "1" ? Data.IdNo.Replace("-", "") : Data.IdNo
			).GetAwaiter().GetResult();
			var customerSubmission = customerSubmissions?.FirstOrDefault();

			new Update(UserConnection, "DgSubmission")
				.Set("DgCustomerId", Column.Parameter(customerSubmission?.customerId ?? string.Empty))
			.Where("Id").IsEqual(Column.Parameter(Data.SubmissionId))
			.Execute();

            result.Success = errorList.Count > 0 ? false : true;
			result.Message = string.Join(". ", errorList.ToArray());

            return result;
        }
		
		[OperationContract]
        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
		public GeneralResponse SFALineActivationStatus(string SerialNumber, string MSISDN, string Status) {
			var service = new LineActivationStatus(UserConnection);
			return service.UpdateStatus(SerialNumber, MSISDN, Status).GetAwaiter().GetResult();
		}

		protected virtual List<string> GetRoles(UserConnection userConnection)
        {
            var roleList = new List<string>();
            var select = new Select(userConnection)
                .Column("roleunit", "Id").As("Id")
                .Column("roleunit", "Name").As("RoleName")
                .From("SysAdminUnit").As("sau")
                .Join(JoinType.LeftOuter, "SysUserInRole").As("suir")
                    .On("suir", "SysUserId").IsEqual("sau", "Id")
                .Join(JoinType.LeftOuter, "SysAdminUnit").As("roleunit")
                    .On("suir", "SysRoleId").IsEqual("roleunit", "Id")
                .Where("sau", "Id").IsEqual(Column.Parameter(userConnection.CurrentUser.Id)) as Select;

            using (DBExecutor dbExecutor = userConnection.EnsureDBConnection()) {
                using (IDataReader reader = select.ExecuteReader(dbExecutor)) {
                    while (reader.Read()) {
                        string role = reader.GetColumnValue<string>("RoleName");

						roleList.Add(role);
                    }
                }
            }

            return roleList;
        }
    }
	
	public class GroupNoValidationRequest
	{
		public Guid CRMGroupId { get; set; }
		public Guid SubmissionId { get; set; }
		public string IdNo { get; set; }
		public string IdType { get; set; }
		public string BRN { get; set; }
		public List<GroupNoList> GroupNo { get; set; }
	}
	
	public class GroupNoList
	{
		public string GroupNo { get; set; }
		public bool IsParent { get; set; } 
	}
}