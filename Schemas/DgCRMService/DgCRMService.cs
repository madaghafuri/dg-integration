using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Linq;
using Terrasoft.Core;
using Terrasoft.Configuration;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using DgBaseService.DgHelpers;
using DgCRMIntegration.DgGetCustomers;
using DgCRMIntegration.DgGetAccounts;
using DgCRMIntegration.DgQueryVPNGroupSubscriber;
using DgCRMIntegration.DgCreateNewSubscriber;
using DgCRMIntegration.DgGetSubscribers;
using DgCRMIntegration.DgGetUsingOffers;
using DgCRMIntegration.DgModifyAccountInfo;
using DgCRMIntegration.DgAddVPNGroupMembers;
using DgCRMIntegration.DgGetPhoneNumbers;
using DgCRMIntegration.DgCheckSimCard;
using DgCRMIntegration.DgPortIn;
using DgCRMIntegration.DgQueryMemberPaymentRelationship;
using DgCRMIntegration.DgChangeSubscriberOffers;
using GetCustomers_Request = DgCRMIntegration.DgGetCustomers.Request;
using GetAccounts_Request = DgCRMIntegration.DgGetAccounts.Request;
using QueryVPNGroupSubscriber_Request = DgCRMIntegration.DgQueryVPNGroupSubscriber.Request;
using CreateNewSubscriber_Request = DgCRMIntegration.DgCreateNewSubscriber.Request;
using CreateNewSubscriber_Response = DgCRMIntegration.DgCreateNewSubscriber.Response;
using GetSubscribers_Request = DgCRMIntegration.DgGetSubscribers.Request;
using GetUsingOffers_Request = DgCRMIntegration.DgGetUsingOffers.Request;
using ModifyAccountInfo_Request = DgCRMIntegration.DgModifyAccountInfo.Request;
using AddVPNGroupMembers_Request = DgCRMIntegration.DgAddVPNGroupMembers.Request;
using AddVPNGroupMembers_Response = DgCRMIntegration.DgAddVPNGroupMembers.Response;
using GetPhoneNumbers_Request = DgCRMIntegration.DgGetPhoneNumbers.Request;
using GetPhoneNumbers_Response = DgCRMIntegration.DgGetPhoneNumbers.Response;
using CheckSimCard_Request = DgCRMIntegration.DgCheckSimCard.Request;
using PortIn_Request = DgCRMIntegration.DgPortIn.Request;
using PortIn_Response = DgCRMIntegration.DgPortIn.Response;
using QueryMemberPaymentRelationship_Request = DgCRMIntegration.DgQueryMemberPaymentRelationship.Request;
using QueryMemberPaymentRelationship_Response = DgCRMIntegration.DgQueryMemberPaymentRelationship.Response;
using ChangeSubscriberOffers_Request = DgCRMIntegration.DgChangeSubscriberOffers.Request;
using DgSubmission.DgLineDetail;
using DgMasterData;
using LookupConst = DgMasterData.DgLookupConst;
using ISAHttpRequest.ISAHttpRequest;
using Newtonsoft.Json;
using DgCRMIntegration.DgGetSignedContract.Response;
using DgCRMIntegration.DgGetSignedContract;

namespace DgCRMIntegration
{
    public class CRMService
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        
        private bool isACDCLog;
		private string transactionType;

        public CRMService(UserConnection UserConnection, bool IsACDCLog = false, string TransactionType = "")
        {
            this.userConnection = UserConnection;
            this.isACDCLog = IsACDCLog;
			this.transactionType = TransactionType;
        }

        #region GetCustomers

        public async Task<List<CustomerValue>> GetCustomersByLineDetail(Guid RecordId)
        {
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new GetCustomersService(UserConnection);
            try {
                await service.SetParamByLineDetail(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetResult();
        }

        public async Task<List<CustomerValue>> GetCustomersByCRMGroup(Guid RecordId)
        {
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new GetCustomersService(UserConnection);
            try {
                await service.SetParamByCRMGroup(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception){
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetResult();
        }

        public async Task<List<CustomerValue>> GetCustomersById(string CustomerId)
        {
            if(string.IsNullOrEmpty(CustomerId)) {
                throw new Exception("Customer Id cannot be null or empty");
            }

            var param = new GetCustomers_Request.QueryCustomerConditionValue() {
                customerId = CustomerId
            };

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomersByBRN(string BRN)
        {
            if(string.IsNullOrEmpty(BRN)) {
                throw new Exception("BRN cannot be null or empty");
            }

            var param = new GetCustomers_Request.QueryConditionForCorpValue() {
                businessRegistrationNumber = BRN
            };

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomersByGroupNo(string GroupNo)
        {
            if(string.IsNullOrEmpty(GroupNo)) {
                throw new Exception("Group No cannot be null or empty");
            }

            var param = new GetCustomers_Request.QueryConditionForCorpValue() {
                groupNumber = GroupNo
            };

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomers(string IdType, string IdNo)
        {
            if(string.IsNullOrEmpty(IdType) || string.IsNullOrEmpty(IdNo)) {
                throw new Exception("Id Type or Id No cannot be null or empty");
            }

            var param = new GetCustomers_Request.QueryCustomerConditionValue() {
                idType = IdType,
                idNumber = IdNo
            };

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomers(GetCustomers_Request.QueryCustomerConditionValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = GetCustomersService.GetDefaultRequest(UserConnection);
            param.Body.getCustomers.GetCustomersRequest = Param;

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomers(GetCustomers_Request.QueryConditionForCorpValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = GetCustomersService.GetDefaultRequest(UserConnection);
            param.Body.getCustomers.GetCustomersRequest.queryCondForCorp = Param;

            return await GetCustomers(param);
        }

        public async Task<List<CustomerValue>> GetCustomers(GetCustomers_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new GetCustomersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                } 
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
        }

        #endregion

        #region GetAccounts

        public async Task<List<AccountValue>> GetAccountsById(string AccountId)
        {
            if(string.IsNullOrEmpty(AccountId)) {
                throw new Exception("Account Id cannot be null or empty");
            }

            var param = new GetAccounts_Request.QueryAccountConditionValue() {
                accountId = AccountId
            };

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccountsByCode(string AccountCode)
        {
            if(string.IsNullOrEmpty(AccountCode)) {
                throw new Exception("Account Code cannot be null or empty");
            }

            var param = new GetAccounts_Request.QueryAccountConditionValue() {
                accountCode = AccountCode
            };

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccountsBySubscriberId(string SubscriberId)
        {
            if(string.IsNullOrEmpty(SubscriberId)) {
                throw new Exception("Subscriber Id cannot be null or empty");
            }

            var param = new GetAccounts_Request.QueryAccountConditionValue() {
                subscriberId = SubscriberId
            };

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccountsByMSISDN(string MSISDN)
        {
            if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null or empty");
            }

            var param = new GetAccounts_Request.QueryAccountConditionValue() {
                msisdn = Helper.GetValidMSISDN(MSISDN)
            };

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccountsByCustomerId(string CustomerId)
        {
            if(string.IsNullOrEmpty(CustomerId)) {
                throw new Exception("Customer Id cannot be null or empty");
            }

            var param = new GetAccounts_Request.QueryAccountConditionValue() {
                customerId = CustomerId
            };

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccounts(GetAccounts_Request.QueryAccountConditionValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = GetAccountsService.GetDefaultRequest(UserConnection);
            param.Body.getAccounts.GetAccountsRequest = Param;

            return await GetAccounts(param);
        }

        public async Task<List<AccountValue>> GetAccounts(GetAccounts_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new GetAccountsService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.getAccounts.GetAccountsRequest.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
        }

        #endregion

        #region QueryVPNGroupSubscriber

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriberByCustomerId(string CustomerId)
        {
            if(string.IsNullOrEmpty(CustomerId)) {
                throw new Exception("Customer Id cannot be null or empty");
            }

            var param = new QueryVPNGroupSubscriber_Request.QueryGroupSubscriberRequestValue() {
                customerId = CustomerId
            };
            
            return await QueryVPNGroupSubscriber(param);
        }

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriberByGroupId(string GroupId)
        {
            if(string.IsNullOrEmpty(GroupId)) {
                throw new Exception("Group Id cannot be null or empty");
            }

            var param = new QueryVPNGroupSubscriber_Request.QueryGroupSubscriberRequestValue() {
                groupId = GroupId
            };
            
            return await QueryVPNGroupSubscriber(param);
        }

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriberByGroupNo(string GroupNo)
        {
            if(string.IsNullOrEmpty(GroupNo)) {
                throw new Exception("Group No cannot be null or empty");
            }

            var param = new QueryVPNGroupSubscriber_Request.QueryGroupSubscriberRequestValue() {
                groupNumber = GroupNo
            };
            
            return await QueryVPNGroupSubscriber(param);
        }

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriberByMSISDN(string MSISDN)
        {
            if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null or empty");
            }

            var param = new QueryVPNGroupSubscriber_Request.QueryGroupSubscriberRequestValue() {
                memberNumber = Helper.GetValidMSISDN(MSISDN)
            };

            return await QueryVPNGroupSubscriber(param);
        }

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriber(QueryVPNGroupSubscriber_Request.QueryGroupSubscriberRequestValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = QueryVPNGroupSubscriberService.GetDefaultRequest(UserConnection);
            param.Body.queryVPNGroupSubscriber.QueryGroupSubscriberRequest = Param;

            return await QueryVPNGroupSubscriber(param);
        }

        public async Task<List<CorpVPNValue>> QueryVPNGroupSubscriber(QueryVPNGroupSubscriber_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new QueryVPNGroupSubscriberService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.queryVPNGroupSubscriber.QueryGroupSubscriberRequest.memberNumber,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
        }

        #endregion
    
        #region CreateNewSubscriber

        public async Task<CreateNewSubscriber_Response.createNewSubscriberResponse> CreateNewSubscriber(Guid LineDetailId)
        {
            if(LineDetailId == null || LineDetailId == Guid.Empty) {
                throw new Exception("Line Detail Id cannot be null or empty");
            }

            var service = new CreateNewSubscriberService(UserConnection);
            try {
                await service.SetParam(LineDetailId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "NEW",
                        MSISDN: service.GetRequest()?
                            .Body?
                            .createNewSubscriber?
                            .CreateNewSubscriberRequest?
                            .newAcctSubscriberInfos?
                            .FirstOrDefault()?
                            .newSubscriberInfos?
                            .FirstOrDefault()?
                            .subscriberInfo?
                            .msisdn ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: LineDetailId
                    );
                }
            }
			
			var res = service.GetResponse();
            return res.Body.createNewSubscriberResponse;
        }

        public async Task<CreateNewSubscriber_Response.createNewSubscriberResponse> CreateNewSubscriber(CreateNewSubscriber_Request.CreateNewSubscriberValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = CreateNewSubscriberService.GetDefaultRequest(UserConnection);
            param.Body.createNewSubscriber.CreateNewSubscriberRequest = Param;

            return await CreateNewSubscriber(param);
        }

        public async Task<CreateNewSubscriber_Response.createNewSubscriberResponse> CreateNewSubscriber(LineDetail Line)
        {
            if(Line == null) {
                throw new Exception("Line cannot be null or empty");
            }

            var param = CreateNewSubscriberService.GetDefaultRequest(UserConnection);
            
            var customerInfo = new CustomerValue();
            
            var newAcctSubscriberInfos = new List<AcctSubscriberCreationValue>();
            var newAcctSubscriberInfo = new AcctSubscriberCreationValue();
            
            var subscriberInfo = new SubscriberValue();
            var joinCorpGroupInfo = new JoinCorpGroupValue();
            var transactionCommonInfo = new TransactionCommonInfoValue();
            var dealerInfo = new DealerValue();
            
            var paymentCollectionInfos = new List<PaymentCollectionInfoValue>();
            var paymentCollectionInfo = new PaymentCollectionInfoValue();

            string activationReason = "1";

            if(string.IsNullOrEmpty(Line.CustomerID)) {
                if(Line.DateOfBirth != null && Line.DateOfBirth != DateTime.MinValue) {
                    customerInfo.customerDateofBirth = Line.DateOfBirth.ToString("yyyyMMdd");
                } else {
                    customerInfo.customerDateofBirth = "19560603";
                }

                customerInfo.customerFlag = "0";
                customerInfo.customerGroup = "1"; // GETSUBMISSIONBYLINEID > customer_group
                customerInfo.firstName = Line.CustomerName;
                customerInfo.idType = Line.IDType?.Code;
                customerInfo.idNumber = Line.IDNo;
                customerInfo.nationality = Line.Nationality?.CRMCode;
                customerInfo.race = "4";
                customerInfo.customerGender = Line.Gender?.Code;
                customerInfo.title = Line.Title?.Code;

                customerInfo.customerAddressInfos = new List<CustomerAddressValue>() {
                    new CustomerAddressValue() {
                        contactType = "3",
                        address1 = Line.BillAddress?.StreetAddress,
                        addressCountry = Line.BillAddress?.Country?.CRMCode,
                        addressProvince = Line.BillAddress?.State?.CRMCode,
                        addressCity = Line.BillAddress?.City?.CRMCode,
                        addressPostCode = Line.BillAddress?.PostCode?.Code,
                        email1 = Line.Email,
                        smsNo = "2000"
                    }
                };
				
				customerInfo.customerRelationInfos = new List<CustomerRelationInfo>() {
                    new CustomerRelationInfo() {
                        relaSeq = "1",
                        relaType = "1",
                        relaName1 = Line.CustomerName,
                        relaTel1 = Line.TelNo,
                        relaEmail = Line.Email,
                        beginTimeForBusiDay = "0600",
                        endTimeForBusiDay = "0900",
                        beginTimeForWeekend = "0700",
                        endTimeForWeekend= "1500"
                    }
                };
            } else {
                customerInfo.customerId = Line.CustomerID;
                subscriberInfo.customerId = Line.CustomerID;
            }

            var accountInfo = new AccountValue();
            accountInfo.accountName = Line.CustomerName;
            accountInfo.converge_flag = "1";
            accountInfo.title = Line.Title?.Code;
            accountInfo.billcycleType = Line.BillCycle;
            accountInfo.email = Line.PRPC != null && Line.PRPC.Code == "0" ? "dummy@digi.com.my" : Line.Email;
            accountInfo.status = "0";

            accountInfo.addressInfo = new List<CustomerAddressValue>() {
                new CustomerAddressValue() {
                    contactType = "4",
                    address1 = Line.BillAddress?.StreetAddress,
                    addressCountry = Line.BillAddress?.Country?.CRMCode,
                    addressProvince = Line.BillAddress?.State?.CRMCode,
                    addressCity = Line.BillAddress?.City?.CRMCode,
                    addressPostCode = Line.BillAddress?.PostCode?.Code,
                    email1 = Line.Email,
                    smsNo = Helper.GetValidMSISDN(Line.MSISDN)
                }
            };

            accountInfo.paymentModeInfo = new PaymentModeValue();
            accountInfo.paymentModeInfo.paymentMode = Line.PaymentMode?.Code;

            if(Line.PaymentMode?.Code == "DDCC") {
                accountInfo.paymentModeInfo.tokenId = Line.TokenID;
                accountInfo.paymentModeInfo.cardType = Line.CardType?.Code;
                accountInfo.paymentModeInfo.bankAcctNo = Line.CardNumberEncrypt;
                accountInfo.paymentModeInfo.bankIssuer = Line.Bank?.Code;
                accountInfo.paymentModeInfo.cardExpDate = Line.CardExpiryDate;
                accountInfo.paymentModeInfo.ownerName = Line.CardOwnerName;
                accountInfo.paymentModeInfo.ownershipType = "0";
            }
            
            if(Line.PRPC != null && Line.PRPC.Code != "0") {
                if(Line.BillMedium != null && Line.BillMedium.Code == "1007") {
                    subscriberInfo.billType = Line.BillMedium.Code;
                }
            }

            subscriberInfo.iccid = Line.SIMCardSerialNumber;
            subscriberInfo.msisdn = Helper.GetValidMSISDN(Line.MSISDN);
            subscriberInfo.paidFlag = "1"; // Payment Type offering yang mana? 0,1,2.
            subscriberInfo.subscriberSegment = "1";
            subscriberInfo.subscriberType = Line.SubscriberType?.Code;
            subscriberInfo.userName = Line.CustomerName;
            
            newAcctSubscriberInfo.accountInfo = accountInfo;
            
            newAcctSubscriberInfo.billMediumInfos = new List<OrderAccountMediumValue>();
            newAcctSubscriberInfo.billMediumInfos.Add(new OrderAccountMediumValue());
            newAcctSubscriberInfo.billMediumInfos[0].billMediumId = Line.PRPC != null && Line.PRPC.Code == "0" ? "1008" : Line.BillMedium?.Code;
            
            newAcctSubscriberInfo.newSubscriberInfos = new List<SubscriberCreationValue>();
            newAcctSubscriberInfo.newSubscriberInfos.Add(new SubscriberCreationValue());
            newAcctSubscriberInfo.newSubscriberInfos[0].subscriberCountSeq = "1";
            newAcctSubscriberInfo.newSubscriberInfos[0].subscriberInfo = subscriberInfo;
            newAcctSubscriberInfo.newSubscriberInfos[0].primaryOfferInfo = new OrderOfferValue() {
                offerSeq = "1",
                offerId = Line.PrimaryOffer.OfferID,
                effDate = Line.PrimaryOffer.EffectiveDate != null && Line.PrimaryOffer.EffectiveDate != DateTime.MinValue ? 
                    Line.PrimaryOffer.EffectiveDate.ToString("yyyyMMdd") : 
                    DateTime.Now.ToString("yyyyMMdd"),
                expDate = Line.PrimaryOffer.ExpiryDate != null && Line.PrimaryOffer.ExpiryDate != DateTime.MinValue ? 
                    Line.PrimaryOffer.ExpiryDate.ToString("yyyyMMdd") : 
                    "20991231",
            };

            newAcctSubscriberInfo.newSubscriberInfos[0].supplementaryOfferInfos = Line.SupplementaryOffer
                .Where(item => item != null)
				.GroupBy(item => item.OfferID)
                .Select(item => new OrderOfferValue() {
					offerId = item.Key,
					effDate = item.First().EffectiveDate != null && item.First().EffectiveDate != DateTime.MinValue ?
						item.First().EffectiveDate.ToString("yyyyMMdd") : DateTime.Now.ToString("yyyyMMdd"),
					expDate = item.First().ExpiryDate != null && item.First().ExpiryDate != DateTime.MinValue ?
						item.First().ExpiryDate.ToString("yyyyMMdd") : "20991231"
                })
				.Select((item, index) => {
					item.offerSeq = (index+1).ToString();
					return item;
				})
                .ToList();

            newAcctSubscriberInfo.newSubscriberInfos[0].purchaseResourceInfos = Line.PurchaseResourceInfos;
            newAcctSubscriberInfo.newSubscriberInfos[0].contractInfos = Line.ContractInfos;
            newAcctSubscriberInfo.newSubscriberInfos[0].feeInfos = Line.FeeInfos;
            newAcctSubscriberInfos.Add(newAcctSubscriberInfo);
			
			joinCorpGroupInfo.creditControlQuota = Convert.ToInt32(Line.CreditLimit * 100).ToString();
            joinCorpGroupInfo.groupId = Line.SubParentGroupID;
            joinCorpGroupInfo.corporateName = Line.CustomerName;
            joinCorpGroupInfo.userName = Line.CustomerName;

            if(Line.PRPC != null && Line.PRPC.Code != "3") {
                // joinCorpGroupInfo.creditControlQuota = Convert.ToInt32(Line.CreditLimit * 100).ToString();
                activationReason = "2";
            }

            transactionCommonInfo.isPendingQApproved = "true";
            transactionCommonInfo.remark = string.Empty;

            dealerInfo.dealerCode = Line.Dealer?.Code;
            dealerInfo.salesmanCode = Line.Dealer?.Code;
            dealerInfo.userId = "NCCF";
            transactionCommonInfo.dealerInfo = dealerInfo;
            
            if(Line.PaymentDetailsInfos != null && Line.PaymentDetailsInfos.Count > 0) {
                paymentCollectionInfo.subscriberCountSeq = "1";
                paymentCollectionInfo.branchCode = "C101";
                paymentCollectionInfo.channelId = "128";
                paymentCollectionInfo.paymentDetailsInfos = Line.PaymentDetailsInfos;
                paymentCollectionInfos.Add(paymentCollectionInfo);

                transactionCommonInfo.paymentCollectionInfos = paymentCollectionInfos;   
            }

            param.Body.createNewSubscriber.CreateNewSubscriberRequest = new CreateNewSubscriber_Request.CreateNewSubscriberValue() {
                customerInfo = customerInfo,
                newAcctSubscriberInfos = newAcctSubscriberInfos,
                activationReason = activationReason,
                joinCorpGroupInfo = joinCorpGroupInfo,
                transactionCommonInfo = transactionCommonInfo
            };

            return await CreateNewSubscriber(param);
        }

        public async Task<CreateNewSubscriber_Response.createNewSubscriberResponse> CreateNewSubscriber(CreateNewSubscriber_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new CreateNewSubscriberService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "NEW",
                        MSISDN: service.GetRequest()?
                            .Body?
                            .createNewSubscriber?
                            .CreateNewSubscriberRequest?
                            .newAcctSubscriberInfos?
                            .FirstOrDefault()?
                            .newSubscriberInfos?
                            .FirstOrDefault()?
                            .subscriberInfo?
                            .msisdn ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            var res = service.GetResponse();
            return res.Body.createNewSubscriberResponse;
        }

        #endregion
			
		#region GetSubscribers
		
		public async Task<List<SubscriberValue>> GetSubscribersByLineDetail(Guid RecordId)
		{
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

			var service = new GetSubscribersService(UserConnection);
            try {
                await service.SetParam(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {  
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: service.GetRequest()?.Body?.getSubscribers?.GetSubscribersRequest?.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetResult();
		}
		
		public async Task<List<SubscriberValue>> GetSubscribersByMSISDN(string MSISDN)
		{
            if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null or empty");
            }

			var param = GetSubscribersService.GetDefaultRequest(UserConnection);
			param.Body.getSubscribers.GetSubscribersRequest.msisdn = Helper.GetValidMSISDN(MSISDN);
			
			return await GetSubscribers(param);
		}
		
		public async Task<List<SubscriberValue>> GetSubscribersByCustomerId(string CustomerId)
		{
            if(string.IsNullOrEmpty(CustomerId)) {
                throw new Exception("Customer Id cannot be null or empty");
            }

			var param = GetSubscribersService.GetDefaultRequest(UserConnection);
			param.Body.getSubscribers.GetSubscribersRequest.customerId = CustomerId;
			
			return await GetSubscribers(param);
		}
		
		public async Task<List<SubscriberValue>> GetSubscribersByAccountId(string AccountId)
		{
            if(string.IsNullOrEmpty(AccountId)) {
                throw new Exception("Account Id cannot be null or empty");
            }

			var param = GetSubscribersService.GetDefaultRequest(UserConnection);
			param.Body.getSubscribers.GetSubscribersRequest.accountId = AccountId;
			
			return await GetSubscribers(param);
		}
		
		public async Task<List<SubscriberValue>> GetSubscribersBySubscriberId(string SubscriberId)
		{
            if(string.IsNullOrEmpty(SubscriberId)) {
                throw new Exception("Subscriber Id cannot be null or empty");
            }

			var param = GetSubscribersService.GetDefaultRequest(UserConnection);
			param.Body.getSubscribers.GetSubscribersRequest.subscriberId = SubscriberId;
			
			return await GetSubscribers(param);
		}
			
		public async Task<List<SubscriberValue>> GetSubscribers(GetSubscribers_Request.QuerySubscriberConditionValue Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var param = GetSubscribersService.GetDefaultRequest(UserConnection);
			param.Body.getSubscribers.GetSubscribersRequest = Param;
			
			return await GetSubscribers(param);
		}
		
		public async Task<List<SubscriberValue>> GetSubscribers(GetSubscribers_Request.Envelope Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var service = new GetSubscribersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.getSubscribers.GetSubscribersRequest.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
		} 

        public async Task<List<SignedContractValue>> GetSignedContractBySubscriber(string SubscriberId)
        {
            if(string.IsNullOrEmpty(SubscriberId)) {
                throw new Exception("MSISDN cannot be null or empty");
            }

			var service = new GetSignedContractService(UserConnection);
            try {
                await service.SetParam(SubscriberId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
        }
			
		#endregion
			
		#region GetUsingOffers
		
		public async Task<List<SubscriberOfferValue>> GetUsingOffersByMSISDN(string MSISDN)
		{
            if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null or empty");
            }

			var param = GetUsingOffersService.GetDefaultRequest(UserConnection);
			param.Body.getUsingOffers.GetUsingOffersRequest.msisdn = Helper.GetValidMSISDN(MSISDN);
			
			return await GetUsingOffers(param); 
		}
			
		public async Task<List<SubscriberOfferValue>> GetUsingOffersBySubscriberId(string SubscriberId)
		{
            if(string.IsNullOrEmpty(SubscriberId)) {
                throw new Exception("Subscriber Id cannot be null or empty");
            }
            
			var param = GetUsingOffersService.GetDefaultRequest(UserConnection);
			param.Body.getUsingOffers.GetUsingOffersRequest.subId = SubscriberId;
			
			return await GetUsingOffers(param); 
		}
			
		public async Task<List<SubscriberOfferValue>> GetUsingOffers(GetUsingOffers_Request.QueryBySubIdValue Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var param = GetUsingOffersService.GetDefaultRequest(UserConnection);
			param.Body.getUsingOffers.GetUsingOffersRequest = Param;
			
			return await GetUsingOffers(param);
		}
		
		public async Task<List<SubscriberOfferValue>> GetUsingOffers(GetUsingOffers_Request.Envelope Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var service = new GetUsingOffersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.getUsingOffers.GetUsingOffersRequest.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
		}
			
		#endregion
			
		#region ModifyAccountInfo
		
		public async Task<ResultOfOperationValue> ModifyAccountInfoByLineDetail(Guid RecordId)
		{
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

			var service = new ModifyAccountInfoService(UserConnection);
            try {
                await service.SetParam(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetOperationReply();
		}
			
		public async Task<ResultOfOperationValue> ModifyAccountInfo(ModifyAccountInfo_Request.ModifyAccountInfoValue Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var param = ModifyAccountInfoService.GetDefaultRequest(UserConnection);
            param.Body.modifyAccountInfo.ModifyAccountInfoRequest = Param;
			
			return await ModifyAccountInfo(param);
		}
			
		public async Task<ResultOfOperationValue> ModifyAccountInfo(ModifyAccountInfo_Request.Envelope Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var service = new ModifyAccountInfoService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetOperationReply();
		}
			
		#endregion
			
		#region AddVPNGroupMembers
		
		public async Task<AddVPNGroupMembers_Response.addVPNGroupMembersResponse> AddVPNGroupMembersByLineDetail(Guid RecordId)
		{
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

			var service = new AddVPNGroupMembersService(UserConnection);
            try {
                await service.SetParamByLineDetail(RecordId);
                await service.Request();

                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception e) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    List<string> msisdn = service.GetRequest()?
                        .Body?
                        .addVPNGroupMembers?
                        .AddVPNGroupMembersRequest?
                        .memberInfos?
                        .Select(item => item.memberNumber)
                        .ToList() ?? null;

                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: msisdn != null ? string.Join(", ", msisdn.ToArray()) : string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            var res = service.GetResponse();
            return res.Body.addVPNGroupMembersResponse;
		}
		
		public async Task<AddVPNGroupMembers_Response.addVPNGroupMembersResponse> AddVPNGroupMembersBySubmission(Guid RecordId)
		{
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

			var service = new AddVPNGroupMembersService(UserConnection);
            try {
                await service.SetParamBySubmission(RecordId);
                await service.Request();
                
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    List<string> msisdn = service.GetRequest()?
                        .Body?
                        .addVPNGroupMembers?
                        .AddVPNGroupMembersRequest?
                        .memberInfos?
                        .Select(item => item.memberNumber)
                        .ToList() ?? null;

                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: msisdn != null ? string.Join(", ", msisdn.ToArray()) : string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            var res = service.GetResponse();
            return res.Body.addVPNGroupMembersResponse;
		}

        public async Task<AddVPNGroupMembers_Response.addVPNGroupMembersResponse> AddVPNGroupMembers(List<LineDetail> Lines)
        {
            if(Lines == null || (Lines != null && Lines.Count == 0)) {
                throw new Exception("Lines cannot be null or empty");
            }

            var param = new AddVPNGroupMembers_Request.AddVPNGroupMembersValue();

            var firstLine = Lines.FirstOrDefault();
            param.groupId = firstLine.SubParentGroupID;

            if(string.IsNullOrEmpty(param.groupId)) {
                if(string.IsNullOrEmpty(firstLine.SubParentGroupNo)) {
					throw new Exception("Group No is empty");
				}

                var queryVpn = await QueryVPNGroupSubscriberByGroupNo(firstLine.SubParentGroupNo);
                param.groupId = queryVpn?.FirstOrDefault()?.groupId;

                if(string.IsNullOrEmpty(param.groupId)) {
                    throw new Exception("Group ID is empty");
                }
            }

            var memberInfos = new List<CorpGroupMemberInfoValue>();
            int i = 0;
            foreach(var line in Lines) {
                string subscriberId = line.SubscriberID;
                if(string.IsNullOrEmpty(subscriberId)) {
                    var getSub = await GetSubscribersByMSISDN(line.MSISDN);
                    subscriberId = getSub?.FirstOrDefault()?.subscriberId;

                    if(string.IsNullOrEmpty(subscriberId)) {
                        throw new Exception("Subscriber Id is empty");
                    }
                }

                var memberInfo = new CorpGroupMemberInfoValue() {
                    subscriberCountSeq = (i+1).ToString(),
                    memberSubscriberId = subscriberId,
                    memberNumber = Helper.GetValidMSISDN(line.MSISDN),
                    PRMode = line.PRPC?.Code,
                    corporateName = line.SubParentGroupName,
                    userName = line.CustomerName
                };

                if(line.PRPC?.Code != "3") {
                    memberInfo.timeSchemaId = "90";
                    memberInfo.creditControlQuota = (Convert.ToInt32(line.CreditLimit) * 100).ToString();
                }

                memberInfos.Add(memberInfo);
                i++;
            }

            var transactionCommonInfo = new TransactionCommonInfoValue() {
                isPendingQApproved = "true",
                dealerInfo = new DealerValue() {
                    dealerCode = firstLine.Dealer?.Code,
                    userId = "NCCF",
                    salesmanCode = firstLine.Dealer?.Code
                }
            };

            param.memberInfos = memberInfos;
            param.transactionCommonInfo = transactionCommonInfo;

            return await AddVPNGroupMembers(param);
        }

		public async Task<AddVPNGroupMembers_Response.addVPNGroupMembersResponse> AddVPNGroupMembers(AddVPNGroupMembers_Request.AddVPNGroupMembersValue Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var param = AddVPNGroupMembersService.GetDefaultRequest(UserConnection);
            param.Body.addVPNGroupMembers.AddVPNGroupMembersRequest = Param;
			
			return await AddVPNGroupMembers(param);
		}
			
		public async Task<AddVPNGroupMembers_Response.addVPNGroupMembersResponse> AddVPNGroupMembers(AddVPNGroupMembers_Request.Envelope Param)
		{
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

			var service = new AddVPNGroupMembersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    List<string> msisdn = Param
                        .Body
                        .addVPNGroupMembers
                        .AddVPNGroupMembersRequest
                        .memberInfos
                        .Select(item => item.memberNumber)
                        .ToList();

                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: msisdn != null ? string.Join(", ", msisdn.ToArray()) : string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            var res = service.GetResponse();
            return res.Body.addVPNGroupMembersResponse;
		}
			
		#endregion

        #region GetPhoneNumbers
        
        public async Task<List<GetPhoneNumbers_Response.GetPhoneNumbersOut>> GetPhoneNumbers(string MSISDN, string DealerCode)
        {
            if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null");
            }

            if(string.IsNullOrEmpty(DealerCode)) {
                throw new Exception("Dealer Code cannot be null");
            }

            var param = new GetPhoneNumbers_Request.GetPhoneNumbersIn() {
                deptId = DealerCode,
                matchCode = Helper.GetValidMSISDN(MSISDN) + "%",
                startRow = "1",
                endRow = "2"
            };

            return await GetPhoneNumbers(param);
        }

        public async Task<List<GetPhoneNumbers_Response.GetPhoneNumbersOut>> GetPhoneNumbers(GetPhoneNumbers_Request.GetPhoneNumbersIn Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = GetPhoneNumbersService.GetDefaultRequest(UserConnection);
            param.Body.getPhoneNumbers.GetPhoneNumbersRequest.QueryCondition = Param;

            return await GetPhoneNumbers(param);
        }

        public async Task<List<GetPhoneNumbers_Response.GetPhoneNumbersOut>> GetPhoneNumbersByLineDetail(Guid RecordId)
        {
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new GetPhoneNumbersService(UserConnection);
            try {
                await service.SetParam(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: service.GetRequest()?.Body?.getPhoneNumbers?.GetPhoneNumbersRequest?.QueryCondition?.matchCode,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetResult();
        }

        public async Task<List<GetPhoneNumbers_Response.GetPhoneNumbersOut>> GetPhoneNumbers(GetPhoneNumbers_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new GetPhoneNumbersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.getPhoneNumbers.GetPhoneNumbersRequest.QueryCondition.matchCode,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
        }

        #endregion

        #region CheckSimCard
        
        public async Task<ResultOfOperationValue> CheckSimCard(string MSISDN, string ICCID)
        {
            if(string.IsNullOrEmpty(MSISDN) || string.IsNullOrEmpty(ICCID)) {
                throw new Exception("MSISDN or ICCID cannot be null or empty");
            }

            var service = new CheckSimCardService(UserConnection);
            try {
                await service.SetParam(MSISDN, ICCID).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: MSISDN,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetOperationReply();
        }

        public async Task<ResultOfOperationValue> CheckSimCardByLineDetail(Guid RecordId)
        {
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new CheckSimCardService(UserConnection);
            try {
                await service.SetParam(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: service.GetRequest()?.Body?.checkSimCard?.CheckSimCardRequest?.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetOperationReply();
        }

        public async Task<ResultOfOperationValue> CheckSimCard(CheckSimCard_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }
            
            var service = new CheckSimCardService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }    
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: Param.Body.checkSimCard.CheckSimCardRequest.msisdn,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetOperationReply();
        }

        #endregion

        #region PortIn

        public async Task<PortIn_Response.portInResponse> PortIn(Guid LineDetailId)
        {
            if(LineDetailId == null || LineDetailId == Guid.Empty) {
                throw new Exception("Line Detail Id cannot be null or empty");
            }

            var service = new PortInService(UserConnection);
            try {
                await service.SetParam(LineDetailId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }   
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "MNP",
                        MSISDN: service.GetRequest()?
                            .Body?
                            .portIn?
                            .PortInRequest?
                            .newAcctSubscriberInfos?
                            .FirstOrDefault()?
                            .newSubscriberInfos?
                            .FirstOrDefault()?
                            .subscriberInfo?
                            .msisdn ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: LineDetailId
                    );
                }
            }
			
			var res = service.GetResponse();
            return res.Body.portInResponse;
        }

        public async Task<PortIn_Response.portInResponse> PortIn(PortIn_Request.PortInValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = PortInService.GetDefaultRequest(UserConnection);
            param.Body.portIn.PortInRequest = Param;

            return await PortIn(param);
        }

        public async Task<PortIn_Response.portInResponse> PortIn(PortIn_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new PortInService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "MNP",
                        MSISDN: service.GetRequest()?
                            .Body?
                            .portIn?
                            .PortInRequest?
                            .newAcctSubscriberInfos?
                            .FirstOrDefault()?
                            .newSubscriberInfos?
                            .FirstOrDefault()?
                            .subscriberInfo?
                            .msisdn ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            var res = service.GetResponse();
            return res.Body.portInResponse;
        }

        #endregion
			
		#region QueryMemberPaymentRelationship
		
		public async Task<QueryMemberPaymentRelationship_Response.EAIWSResultOfQueryMemberPaymentRelationship> QueryMemberPaymentRelationship(string MSISDN)
		{
			if(string.IsNullOrEmpty(MSISDN)) {
                throw new Exception("MSISDN cannot be null or empty");
            }
			
			var param = QueryMemberPaymentRelationshipService.GetDefaultRequest(UserConnection);
            param.Body.queryMemberPaymentRelationship.QueryMemberPaymentRelationshipRequest.memberNumber = Helper.GetValidMSISDN(MSISDN);
			
			return await QueryMemberPaymentRelationship(param);
		}
		
		public async Task<QueryMemberPaymentRelationship_Response.EAIWSResultOfQueryMemberPaymentRelationship> QueryMemberPaymentRelationship(Guid RecordId)
		{
			if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new QueryMemberPaymentRelationshipService(UserConnection);
            try {
                await service.SetParam(RecordId).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: service.GetRequest()?
                            .Body?
                            .queryMemberPaymentRelationship?
                            .QueryMemberPaymentRelationshipRequest?
                            .memberNumber ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
						LineDetailId: RecordId
                    );
                }
            }

            return service.GetResult();
		}
			
		public async Task<QueryMemberPaymentRelationship_Response.EAIWSResultOfQueryMemberPaymentRelationship> QueryMemberPaymentRelationship(QueryMemberPaymentRelationship_Request.Envelope Param)
		{
			if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new QueryMemberPaymentRelationshipService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: this.transactionType,
                        MSISDN: service.GetRequest()?
                            .Body?
                            .queryMemberPaymentRelationship?
                            .QueryMemberPaymentRelationshipRequest?
                            .memberNumber ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetResult();
		}
			
		#endregion
			
		#region ChangeSubcriberOffers
        
        public async Task<ResultOfOperationValue> ChangeSubscriberOffers(Guid RecordId, List<BundleOffering> BundleOfferingList, List<Offering> OfferingMasterList)
        {
            if(RecordId == null || RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new ChangeSubscriberOffersService(UserConnection);
            try {
                await service.SetParam(RecordId, BundleOfferingList, OfferingMasterList);
                await service.Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "COP",
                        MSISDN: service.GetRequest()?
                            .Body?
                            .changeSubscriberOffers?
                            .ChangeSubscriberOffersRequest?
                            .msisdn ?? string.Empty,
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply(),
                        LineDetailId: RecordId
                    );
                }
            }

            return service.GetOperationReply();
        }

        public async Task<ResultOfOperationValue> ChangeSubscriberOffers(LineDetail Line, List<BundleOffering> BundleOfferingList, List<Offering> OfferingMasterList)
        {
            if(Line == null) {
                throw new Exception("Line cannot be null or empty");
            }

            var param = ChangeSubscriberOffersService.GetDefaultRequest(UserConnection);

            string primaryOfferID = Line.PrimaryOffer?.OfferID;
            List<string> lineSuppOfferIdList = Line.SupplementaryOffer?
                .Where(item => item != null)
                .Select(item => item.OfferID)
                .ToList() ?? new List<string>();

            var copOfferList = ChangeSubscriberOffersService.GetCOPOfferList(
                UserConnection, 
                primaryOfferID, 
                lineSuppOfferIdList, 
                Line.UsingOffers ?? new List<SubscriberOfferValue>(), 
                BundleOfferingList, 
                OfferingMasterList
            );
            var additionOptionalOfferInfos = copOfferList.AdditionalOfferList;
            var unsubscribeOptionalOfferInfos = copOfferList.UnsubscribeOfferList;
            bool isChangePrimaryOffer = copOfferList.IsChangePrimaryOffer;

            var dealerInfo = new DealerValue();
            dealerInfo.dealerCode = Line.Dealer?.Code;
            dealerInfo.userId = "NCCF";
            dealerInfo.salesmanCode = Line.Dealer?.Code;

            var transactionCommonInfo = new TransactionCommonInfoValue();
            transactionCommonInfo.isPendingQApproved = "true";
            transactionCommonInfo.remark = string.Empty;
            transactionCommonInfo.dealerInfo = dealerInfo;

            var ChangeSubscriberOffersRequest = new ChangeSubscriberOffers_Request.ChangeOffersValue();
            ChangeSubscriberOffersRequest.subscriberId = Line.SubscriberID;
            ChangeSubscriberOffersRequest.isChangePrimaryOffer = isChangePrimaryOffer ? "true" : "false";
            ChangeSubscriberOffersRequest.subscriberType = Line.SubscriberType?.Code;
            ChangeSubscriberOffersRequest.businessEffectiveMode = "0";
            ChangeSubscriberOffersRequest.transactionCommonInfo = transactionCommonInfo;
            ChangeSubscriberOffersRequest.msisdn = !string.IsNullOrEmpty(Line?.MSISDN) ? Helper.GetValidMSISDN(Line?.MSISDN) : string.Empty;

            if(isChangePrimaryOffer) {
                ChangeSubscriberOffersRequest.newPrimaryOfferInfo = new OrderOfferValue() {
                    offerId = primaryOfferID
                };
            }

            if(additionOptionalOfferInfos.Count > 0) {
                ChangeSubscriberOffersRequest.additionOptionalOfferInfos = additionOptionalOfferInfos
                    .Where(item => item.offerId != "128102" && item.offerId != "90000202" && item.offerId != "90000208")
                    .Select(item => new OrderOfferValue() {
                        offerId = item.offerId
                    })
                    .ToList();
            }

            if(unsubscribeOptionalOfferInfos.Count > 0) {
                ChangeSubscriberOffersRequest.unsubscribeOptionalOfferInfos = unsubscribeOptionalOfferInfos
                    .Where(item => item.offerId != "128102" && item.offerId != "90000202" && item.offerId != "90000208")
                    .Select(item => new OrderOfferValue() {
                        offerId = item.offerId
                    })
                    .ToList();
            }

            var contractInfos = Line.ContractInfos;
            var purchaseResourceInfos = Line.PurchaseResourceInfos;
            var feeInfos = Line.FeeInfos;

            if(purchaseResourceInfos.Count > 0) {
                ChangeSubscriberOffersRequest.purchaseResourceInfos = purchaseResourceInfos;
            }

            if(contractInfos.Count > 0) {
                ChangeSubscriberOffersRequest.contractInfos = contractInfos;
            }

            if(feeInfos.Count > 0) {
                ChangeSubscriberOffersRequest.feeInfos = feeInfos;
            }

            if(Line.PaymentDetailsInfos != null && Line.PaymentDetailsInfos.Count > 0) {
                var paymentCollectionInfos = new List<PaymentCollectionInfoValue>();
                var paymentCollectionInfo = new PaymentCollectionInfoValue();

                paymentCollectionInfo.subscriberCountSeq = "1";
                paymentCollectionInfo.branchCode = "C101";
                paymentCollectionInfo.channelId = "128";
                paymentCollectionInfo.paymentDetailsInfos = Line.PaymentDetailsInfos;
                paymentCollectionInfos.Add(paymentCollectionInfo);

                transactionCommonInfo.paymentCollectionInfos = paymentCollectionInfos;   
            }

            param.Body.changeSubscriberOffers.ChangeSubscriberOffersRequest = ChangeSubscriberOffersRequest;
			
            return await ChangeSubscriberOffers(param);
        }

        public async Task<ResultOfOperationValue> ChangeSubscriberOffers(ChangeSubscriberOffers_Request.ChangeOffersValue Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            var param = ChangeSubscriberOffersService.GetDefaultRequest(UserConnection);
            param.Body.changeSubscriberOffers.ChangeSubscriberOffersRequest = Param;

            return await ChangeSubscriberOffers(param);
        }

        public async Task<ResultOfOperationValue> ChangeSubscriberOffers(ChangeSubscriberOffers_Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }
						
            var service = new ChangeSubscriberOffersService(UserConnection);
            try {
                await service.SetParam(Param).Request();
                if(!service.IsSuccessResponse()) {
                    string error = service.GetErrorResponse();
                    if(!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }

                    return null;
                }
            } catch (Exception) {
                throw;
            } finally {
                if(this.isACDCLog) {
                    LogHelper.LogACDCTracking(
                        UserConnection: UserConnection, 
                        Log: service.GetLog(),
                        TransactionType: "COP",
                        MSISDN: service.GetMsisdn(),
                        Remarks: service.GetErrorResponse(),
                        ResultOperationReply: service.GetOperationReply()
                    );
                }
            }

            return service.GetOperationReply();
        }

        #endregion
    }
}