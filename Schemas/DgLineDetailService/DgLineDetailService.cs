using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData;
using LookupConst = DgMasterData.DgLookupConst;
using DgCRMIntegration;
using GetPhoneNumbersResponse = DgCRMIntegration.DgGetPhoneNumbers.Response;
using CheckSimCard_Response = DgCRMIntegration.DgCheckSimCard.Response;
using ISAEntityHelper.EntityHelper;

namespace DgSubmission.DgLineDetail
{
    public class LineDetail
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public Guid Id { get; set; }
        public int No { get; set; }
        public int LineId { get; set; }
        public string MSISDN { get; set; }
		public Guid ReleasedById { get; set; }
		public DgMasterData.Lookup ActivationStatus { get; set; }
		public string ActivationOrderID { get; set; }
		public string ActivationTransactionID { get; set; }
		public string ActivationPortInTransactionID { get; set; }
		public string ActivationPortInMessageID { get; set; }
        public Guid SubmissionId { get; set; }
        public Guid CRMGroupId { get; set; }
        public string CustomerID { get; set; }
        public string SubscriberID { get; set; }
        public string AccountID { get; set; }
        public DgMasterData.Lookup SubmissionType { get; set; }
        public string SerialNumber { get; set; }
        public string SubParentGroupName { get; set; }
        public string SubParentGroupNo { get; set; }
        public string SubParentGroupID { get; set; }
        public string SubParentGroupCustomerID { get; set; }
        public string SubParentGroupAccountID { get; set; }
        public string SubParentGroupAccountCode { get; set; }
        public string SubParentGroupBRN { get; set; }
		public string SubParentGroupPaymentID { get; set; }
        public DgMasterData.Lookup Source { get; set; }
        public string IDNo { get; set; }
        public DgMasterData.Lookup IDType { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DgMasterData.Lookup Gender { get; set; }
        public DgMasterData.Lookup Title { get; set; }
        public DgMasterData.Country Nationality { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; }
        public string CustomerName { get; set; }
        public Address LegalAddress { get; set; }
		public Address BillAddress { get; set; }
        public string TelNo { get; set; }
        public string BillCycle { get; set; }
        public DgMasterData.Lookup BillMedium { get; set; }
        public DgMasterData.Lookup PaymentMode { get; set; }
        public string SIMCardSerialNumber { get; set; }
        public decimal CreditLimit { get; set; }
        public DgMasterData.Lookup PRPC { get; set; }
        public string Remark { get; set; }
        public DgMasterData.Lookup Dealer { get; set; }
        public Offering PrimaryOffer { get; set; }
        public List<Offering> SupplementaryOffer { get; set; }
        public string CardOwnerName { get; set; }
        public string PlainCardNumber { get; set; }
        public string CardExpiryDate { get; set; }
        public string CardNumberEncrypt { get; set; }
        public string TokenID { get; set; }
        public DgMasterData.Lookup Bank { get; set; }
        public DgMasterData.Lookup CardType { get; set; }
        public DgMasterData.Lookup SubscriberType { get; set; }
        public DgMasterData.Lookup P2P { get; set; }
        public LookupV2 DNO { get; set; }
        public DgMasterData.Lookup DNOIDType { get; set; }
        public string DNOIDNo { get; set; }
        public string DNOAccountCode { get; set; }
        public string DNOCompanyName { get; set; }
        public List<BusinessFeeValue> FeeInfos { get; set; }
        public List<OrderResourceValue> PurchaseResourceInfos { get; set; }
        public List<OrderContractValue> ContractInfos { get; set; }
        public List<PaymentDetailsInfoValue> PaymentDetailsInfos { get; set; }
        public List<SubscriberOfferValue> UsingOffers { get; set; }
        public List<string> IntegrationMessage { get; set; }
        public DateTime OPPageOpen { get; set; }

        public LineDetail() {}

        public LineDetail(UserConnection UserConnection) 
        {
            this.userConnection = UserConnection;
        }

        public LineDetail(UserConnection UserConnection, Guid RecordId)
        {
            this.userConnection = UserConnection;
            this.Id = RecordId;

            Get();
        }

        #region Get Data
        
        public LineDetail Get()
        {
            if(this.Id == null || this.Id == Guid.Empty) {
                return null;
            }

            if(UserConnection == null) {
                throw new Exception("UserConnection is null. Please use construct with UserConnection");
            }

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            var entity = esq.GetEntity(UserConnection, this.Id);
            SetSingleLine(entity, columns);

            return this;
        }

        public List<LineDetail> GetLines(Guid SubmissionId)
        {
			if(SubmissionId == Guid.Empty) {
				throw new Exception("Submission Id cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission", SubmissionId));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return result;
        }

        public List<LineDetail> GetLines(string SerialNumber)
        {
			if(string.IsNullOrEmpty(SerialNumber)) {
				throw new Exception("Serial Number cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.DgSerialNumber", SerialNumber));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return result;
        }
		
		public List<LineDetail> GetLines(List<Guid> RecordIds)
        {
			if(RecordIds == null || (RecordIds != null && RecordIds.Count == 0)) {
				throw new Exception("Record Ids cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
			
			var filterIds = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
			foreach(Guid id in RecordIds) {
				filterIds.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", id));
			}
			
            esq.Filters.Add(filterIds);

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return result;
        }

        public List<LineDetail> GetLinesActivation(Guid SubmissionId)
        {
			if(SubmissionId == Guid.Empty) {
				throw new Exception("Submission Id cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQueryActivation();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
			
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission", SubmissionId));
			
            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return GetLinesActivation(result);
        }

        public List<LineDetail> GetLinesActivation(string SerialNumber)
        {
			if(string.IsNullOrEmpty(SerialNumber)) {
				throw new Exception("Serial Number cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQueryActivation();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
			
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.DgSerialNumber", SerialNumber));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return GetLinesActivation(result);
        }
		
		public List<LineDetail> GetLinesActivation(List<Guid> RecordIds)
        {
			if(RecordIds == null || (RecordIds != null && RecordIds.Count == 0)) {
				throw new Exception("Record Ids cannot be null or empty");
			}
			
            var result = new List<LineDetail>();

            var query = BuildQueryActivation();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
			
			var filterIds = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
			foreach(Guid id in RecordIds) {
				filterIds.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", id));
			}
			
            esq.Filters.Add(filterIds);
			
            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(SetData(entity, columns));
            }

            return GetLinesActivation(result);
        }

        public List<LineDetail> GetLinesActivation(List<LineDetail> Lines)
        {
            if(Lines == null || (Lines != null && Lines.Count == 0)) {
                throw new Exception("No data can be provision to CRM");
            }

            var line = Lines.FirstOrDefault();

            string tokenId = line.TokenID;
            DgMasterData.Lookup cardType = line.CardType;
            DgMasterData.Lookup bank = line.Bank;
            string plainCardNumber = line.PlainCardNumber;
            string cardNumberEncrypt = line.CardNumberEncrypt;
            string cardExpiryDate = line.CardExpiryDate;

            if(line.PaymentMode != null && line.PaymentMode.Code == "DDCC") {
                var creditCardToken = CRMHelper.GetCreditCardTokenByLineDetail(UserConnection, line.Id);
                tokenId = creditCardToken?.TokenId ?? string.Empty;

                if(creditCardToken != null) {
                    if(cardType == null) {
                        var cardTypeInfo = EntityHelper.GetEntity(UserConnection, "DgCardType", creditCardToken.CardTypeId, new Dictionary<string, string>() {
                            {"DgCode", "string"}
                        });
                        cardType = new DgMasterData.Lookup() {
                            Code = cardTypeInfo["DgCode"]?.ToString() ?? string.Empty
                        };
                    }

                    if(bank == null) {
                        var bankIssuerInfo = EntityHelper.GetEntity(UserConnection, "DgBankIssuer", creditCardToken.BankIssuerId, new Dictionary<string, string>() {
                            {"DgCode", "string"}
                        });
                        bank = new DgMasterData.Lookup() {
                            Code = bankIssuerInfo["DgCode"]?.ToString() ?? string.Empty
                        };
                    }

                    if(string.IsNullOrEmpty(plainCardNumber)) {
                        plainCardNumber = creditCardToken.CardNumber;
                    }

                    if(string.IsNullOrEmpty(cardNumberEncrypt) && !string.IsNullOrEmpty(plainCardNumber)) {
                        cardNumberEncrypt = CRMHelper.EncryptCardNumber(UserConnection, plainCardNumber);
                    }

                    if(string.IsNullOrEmpty(cardExpiryDate)) {
                        cardExpiryDate = creditCardToken.CardExp.ToString("yyyyMM");
                    }
                }
            }

            return Lines.Select(item => {
                if(item.PaymentMode != null && item.PaymentMode.Code == "DDCC") {
                    item.TokenID = tokenId;
                    item.CardType = cardType;
                    item.Bank = bank;
                    item.PlainCardNumber = plainCardNumber;
                    item.CardNumberEncrypt = cardNumberEncrypt;
                    item.CardExpiryDate = cardExpiryDate;
                }

                item.PaymentDetailsInfos = new List<PaymentDetailsInfoValue>();
                foreach(var feeInfo in item.FeeInfos) {
                    if(feeInfo.payType == "2") {
                        continue;
                    }

                    var paymentDetailInfo = new PaymentDetailsInfoValue();
                    
                    string paymentMethod = "1001";
                    if(item.PaymentMode != null && item.PaymentMode.Code == "DDCC") {
                        if(!string.IsNullOrEmpty(item.Bank?.Code)) {
                            paymentDetailInfo.bankCode = item.Bank.Code;
                        }

                        if(!string.IsNullOrEmpty(item.CardType?.Code)) {
                            paymentDetailInfo.cardType = item.CardType?.Code;
                        }

                        if(!string.IsNullOrEmpty(item.PlainCardNumber)) {
                            paymentDetailInfo.cardNo = CRMHelper.GetMaskCardNumber(item.PlainCardNumber);
                        }

                        paymentMethod = "6200";
                    }

                    paymentDetailInfo.paymentMethod = feeInfo.feeItemCode.StartsWith("6") ? "V"+paymentMethod : paymentMethod;
                    paymentDetailInfo.feeAmt = feeInfo.feeAmt;
                    paymentDetailInfo.additionalRemark = string.Empty;

                    item.PaymentDetailsInfos.Add(paymentDetailInfo);
                }
                
				if(item.IntegrationMessage == null) {
					item.IntegrationMessage = new List<string>();
				}
				
                return item;
            }).ToList();
        }

        public List<LineDetail> GetLinesAddVPNGroupMember()
        {
            string sql = @"SELECT 
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
                AND DgSubmission.DgSubmissionTypeId != @submissionTypeId
                AND DgPRPC.DgCode = @prpc
                AND DgAddVPNGroupMember = @addVPN
				AND DgAddVPNQueueDetail.DgLineDetailId IS NULL
                AND CONVERT(DATE, DgDateTimeActivated) = CONVERT(DATE, GETUTCDATE())";

            var query = new CustomQuery(UserConnection, sql);
            query.Parameters.Add("@activationStatusId", LookupConst.ActivationStatus.Activated.ToString());
            query.Parameters.Add("@submissionTypeId", LookupConst.SubmissionType.COP.ToString());
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

        protected dynamic BuildQuery()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("No", esq.AddColumn("DgNo"));
            columns.Add("LineId", esq.AddColumn("DgLineId"));
            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("SubmissionId", esq.AddColumn("DgSubmission.Id"));

            columns.Add("SubmissionType_Id", esq.AddColumn("DgSubmission.DgSubmissionType.Id"));
            columns.Add("SubmissionType_Name", esq.AddColumn("DgSubmission.DgSubmissionType.Name"));
            columns.Add("SubmissionType_Code", esq.AddColumn("DgSubmission.DgSubmissionType.DgCode"));
            
            columns.Add("CRMGroupId", esq.AddColumn("DgSubmission.DgCRMGroup.Id"));
            columns.Add("SerialNumber", esq.AddColumn("DgSubmission.DgSerialNumber"));
            
            columns.Add("SubParentGroupName", esq.AddColumn("DgSubmission.DgCRMGroup.DgGroupSubParentName"));
            columns.Add("SubParentGroupNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgGroupSubParentNo"));
            columns.Add("SubParentGroupID", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentGroupID"));
            columns.Add("SubParentGroupCustomerID", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentCustomerId"));
            columns.Add("SubParentGroupAccountID", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentAccountId"));
            columns.Add("SubParentGroupAccountCode", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentAccountCode"));
            columns.Add("SubParentGroupBRN", esq.AddColumn("DgSubmission.DgCRMGroup.DgBRN"));
			columns.Add("SubParentGroupPaymentID", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentPaymentId"));

            columns.Add("Source_Id", esq.AddColumn("DgSubmission.DgSource.Id"));
            columns.Add("Source_Name", esq.AddColumn("DgSubmission.DgSource.Name"));

            columns.Add("CustomerID", esq.AddColumn("DgSubmission.DgCustomerId"));
            columns.Add("AccountID", esq.AddColumn("DgActivationAccountId"));
            columns.Add("IDNo", esq.AddColumn("DgSubmission.DgIDNo"));

            columns.Add("IdType_Id", esq.AddColumn("DgSubmission.DgIDType.Id"));
            columns.Add("IdType_Name", esq.AddColumn("DgSubmission.DgIDType.Name"));
            columns.Add("IdType_Code", esq.AddColumn("DgSubmission.DgIDType.DgCode"));
            
            columns.Add("DateOfBirth", esq.AddColumn("DgSubmission.DgDateOfBirth"));

            columns.Add("DNO_Id", esq.AddColumn("DgDNO.Id"));
            columns.Add("DNO_Name", esq.AddColumn("DgDNO.Name"));
            columns.Add("DNO_CRMCode", esq.AddColumn("DgDNO.DgCode"));
            columns.Add("DNO_CSGCode", esq.AddColumn("DgDNO.DgCSGCode"));
            columns.Add("DNO_Position", esq.AddColumn("DgDNO.DgPosition"));

            columns.Add("DNOIDType_Id", esq.AddColumn("DgDNOIDType.Id"));
            columns.Add("DNOIDType_Name", esq.AddColumn("DgDNOIDType.Name"));
            columns.Add("DNOIDType_Code", esq.AddColumn("DgDNOIDType.DgCode"));

            columns.Add("DNOIDNo", esq.AddColumn("DgDNOIdNo"));
            columns.Add("DNOAccountCode", esq.AddColumn("DgDNOAccNo"));
            columns.Add("DNOCompanyName", esq.AddColumn("DgDNOCompanyName"));
            
            columns.Add("Gender_Id", esq.AddColumn("DgSubmission.DgGender.Id"));
            columns.Add("Gender_Name", esq.AddColumn("DgSubmission.DgGender.Name"));
            columns.Add("Gender_Code", esq.AddColumn("DgSubmission.DgGender.DgCode"));
            
            columns.Add("Title_Id", esq.AddColumn("DgSubmission.DgTitle.Id"));
            columns.Add("Title_Name", esq.AddColumn("DgSubmission.DgTitle.Name"));
            columns.Add("Title_Code", esq.AddColumn("DgSubmission.DgTitle.DgCode"));
            
            columns.Add("Nationality_Id", esq.AddColumn("DgSubmission.DgNationality.Id"));
            columns.Add("Nationality_Name", esq.AddColumn("DgSubmission.DgNationality.Name"));
            columns.Add("Nationality_CRMCode", esq.AddColumn("DgSubmission.DgNationality.DgCode"));
            
            columns.Add("Email", esq.AddColumn("DgSubmission.DgEmail"));
            columns.Add("CompanyName", esq.AddColumn("DgSubmission.DgCompanyName"));
            columns.Add("CustomerName", esq.AddColumn("DgUsername"));

            columns.Add("LegalAddress", esq.AddColumn("DgSubmission.DgCRMGroup.DgLegalAddress"));
			columns.Add("BillAddress", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillingAddress"));
            
            columns.Add("LegalCountry_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountry.Id"));
            columns.Add("LegalCountry_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountry.Name"));
            columns.Add("LegalCountry_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountry.DgCode"));
			
			columns.Add("BillCountry_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationBilling.Id"));
            columns.Add("BillCountry_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationBilling.Name"));
            columns.Add("BillCountry_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationBilling.DgCode"));

            columns.Add("BillProvince_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoBilling.Id"));
            columns.Add("BillProvince_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoBilling.Name"));
            columns.Add("BillProvince_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoBilling.DgCode"));
			
			columns.Add("LegalProvince_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgState.Id"));
            columns.Add("LegalProvince_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgState.Name"));
            columns.Add("LegalProvince_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgState.DgCode"));

            columns.Add("BillCity_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationBilling.Id"));
            columns.Add("BillCity_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationBilling.Name"));
            columns.Add("BillCity_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationBilling.DgCode"));
			
			columns.Add("LegalCity_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgCity.Id"));
            columns.Add("LegalCity_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgCity.Name"));
            columns.Add("LegalCity_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgCity.DgCode"));

            columns.Add("LegalPostcode_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcode.Id"));
            columns.Add("LegalPostcode_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcode.Name"));
            columns.Add("LegalPostcode_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcode.DgCode"));
			
			columns.Add("BillPostcode_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationBilling.Id"));
            columns.Add("BillPostcode_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationBilling.Name"));
            columns.Add("BillPostcode_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationBilling.DgCode"));

            columns.Add("TelNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgTelNo"));
            columns.Add("BillCycle", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillingCycle"));

            columns.Add("BillMedium_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillMediumName.Id"));
            columns.Add("BillMedium_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillMediumName.Name"));
            columns.Add("BillMedium_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillMediumName.DgCode"));
            
            columns.Add("PaymentMode_Id", esq.AddColumn("DgSubmission.DgPaymentMode.Id"));
            columns.Add("PaymentMode_Name", esq.AddColumn("DgSubmission.DgPaymentMode.Name"));
            columns.Add("PaymentMode_Code", esq.AddColumn("DgSubmission.DgPaymentMode.DgCode"));

            columns.Add("SIMCardSerialNumber", esq.AddColumn("DgSIMCardNumber"));
            columns.Add("CreditLimit", esq.AddColumn("DgCreditLimit"));

            columns.Add("PRPC_Id", esq.AddColumn("DgPRPC.Id"));
            columns.Add("PRPC_Name", esq.AddColumn("DgPRPC.Name"));
            columns.Add("PRPC_Code", esq.AddColumn("DgPRPC.DgCode"));
            
            columns.Add("Remark", esq.AddColumn("DgDeviceOrderRemark"));

            columns.Add("Dealer_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.Id"));
            columns.Add("Dealer_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerName"));
            columns.Add("Dealer_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerID"));
            
            columns.Add("PrimaryOffer_Id", esq.AddColumn("DgPrimaryOffering.Id"));
            columns.Add("PrimaryOffer_OfferType_Id", esq.AddColumn("DgPrimaryOffering.DgOfferType.Id"));
            columns.Add("PrimaryOffer_OfferType_Name", esq.AddColumn("DgPrimaryOffering.DgOfferType.Name"));
            columns.Add("PrimaryOffer_OfferID", esq.AddColumn("DgPrimaryOffering.DgOfferID"));
            columns.Add("PrimaryOffer_OfferName", esq.AddColumn("DgPrimaryOffering.DgOfferName"));
            columns.Add("PrimaryOffer_EffectiveDate", esq.AddColumn("DgPrimaryOffering.DgEffectiveDate"));
            columns.Add("PrimaryOffer_ExpiryDate", esq.AddColumn("DgPrimaryOffering.DgExpiryDate"));

            for (int i = 1; i <= 20; i++) {
                columns.Add($"SuppOffer{i}_Id", esq.AddColumn($"DgSuppOffer{i}.Id"));
                columns.Add($"SuppOffer{i}_OfferType_Id", esq.AddColumn($"DgSuppOffer{i}.DgOfferType.Id"));
                columns.Add($"SuppOffer{i}_OfferType_Name", esq.AddColumn($"DgSuppOffer{i}.DgOfferType.Name"));
                columns.Add($"SuppOffer{i}_OfferID", esq.AddColumn($"DgSuppOffer{i}.DgOfferID"));
                columns.Add($"SuppOffer{i}_OfferName", esq.AddColumn($"DgSuppOffer{i}.DgOfferName"));
                columns.Add($"SuppOffer{i}_EffectiveDate", esq.AddColumn($"DgSuppOffer{i}.DgEffectiveDate"));
                columns.Add($"SuppOffer{i}_ExpiryDate", esq.AddColumn($"DgSuppOffer{i}.DgExpiryDate"));
            }

            columns.Add("CardOwnerName", esq.AddColumn("DgSubmission.DgOwnerName"));
            columns.Add("PlainCardNumber", esq.AddColumn("DgSubmission.DgPlainCardNumber"));
            columns.Add("CardExpiryDate", esq.AddColumn("DgSubmission.DgCardExpiredDate"));

            columns.Add("SubscriberType_Id", esq.AddColumn("DgSubmission.DgSubscriberType.Id"));
            columns.Add("SubscriberType_Name", esq.AddColumn("DgSubmission.DgSubscriberType.Name"));
            columns.Add("SubscriberType_Code", esq.AddColumn("DgSubmission.DgSubscriberType.DgCode"));
            
            columns.Add("Bank_Id", esq.AddColumn("DgSubmission.DgBankIssuer.Id"));
            columns.Add("Bank_Name", esq.AddColumn("DgSubmission.DgBankIssuer.Name"));
            columns.Add("Bank_Code", esq.AddColumn("DgSubmission.DgBankIssuer.DgCode"));

            columns.Add("CardType_Id", esq.AddColumn("DgSubmission.DgCardType.Id"));
            columns.Add("CardType_Name", esq.AddColumn("DgSubmission.DgCardType.Name"));
            columns.Add("CardType_Code", esq.AddColumn("DgSubmission.DgCardType.DgCode"));

            columns.Add("P2P_Id", esq.AddColumn("DgToS.Id"));
            columns.Add("P2P_Name", esq.AddColumn("DgToS.Name"));

            columns.Add("OPPageOpenDate", esq.AddColumn("DgOPPageOpenDate"));

            columns["No"].OrderByAsc(0);
            columns["LineId"].OrderByAsc(1);

            return new {
                esq = esq,
                columns = columns
            };
        }

        protected dynamic BuildQueryActivation()
        {
            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
            
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleased", true));
			
			var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
			filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationStatus", LookupConst.ActivationStatus.Fail));
			filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationStatus", LookupConst.ActivationStatus.Pending));
			filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationStatus", LookupConst.ActivationStatus.Reject));
			
            var filterNotActivated = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
            filterNotActivated.Add(new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or) {
                esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationOrderID", string.Empty),
                esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgActivationOrderID")
            });
			filterNotActivated.Add(new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or) {
                esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationStatus", LookupConst.ActivationStatus.NotActivated),
                esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgActivationStatus")
            });
			
			filterGroup.Add(filterNotActivated);

            esq.Filters.Add(filterGroup);
			
            return new {
                esq = esq,
                columns = columns
            };
        }

        protected void SetSingleLine(Entity entity, Dictionary<string, EntitySchemaQueryColumn> columns)
        {
            var line = SetData(entity, columns);
            
            this.No = line.No;
            this.LineId = line.LineId;
            this.MSISDN = line.MSISDN;
            this.SubmissionId = line.SubmissionId;
            this.CRMGroupId = line.CRMGroupId;
            this.SubmissionType = line.SubmissionType;
            this.SerialNumber = line.SerialNumber;
            this.SubParentGroupName = line.SubParentGroupName;
            this.SubParentGroupNo = line.SubParentGroupNo;
            this.SubParentGroupID = line.SubParentGroupID;
            this.SubParentGroupCustomerID = line.SubParentGroupCustomerID;
            this.SubParentGroupAccountID = line.SubParentGroupAccountID;
            this.SubParentGroupAccountCode = line.SubParentGroupAccountCode;
            this.SubParentGroupBRN = line.SubParentGroupBRN;
            this.Source = line.Source;
            this.CustomerID = line.CustomerID;
            this.AccountID = line.AccountID;
            this.SubParentGroupPaymentID = line.SubParentGroupPaymentID;
            this.IDNo = line.IDNo;
            this.IDType = line.IDType;
            this.DateOfBirth = line.DateOfBirth;
            this.DNOIDType = line.DNOIDType;
            this.DNO = line.DNO;
            this.DNOIDNo = line.DNOIDNo;
            this.DNOAccountCode = line.DNOAccountCode;
            this.DNOCompanyName = line.DNOCompanyName;
            this.Gender = line.Gender;
            this.Title = line.Title;
            this.Nationality = line.Nationality;
            this.Email = line.Email;
            this.CompanyName = line.CompanyName;
            this.CustomerName = line.CustomerName;
            this.LegalAddress = line.LegalAddress;
			this.BillAddress = line.BillAddress;
            this.TelNo = line.TelNo;
            this.BillCycle = line.BillCycle;
            this.BillMedium = line.BillMedium;
            this.PaymentMode = line.PaymentMode;
            this.SIMCardSerialNumber = line.SIMCardSerialNumber;
            this.CreditLimit = line.CreditLimit;
            this.PRPC = line.PRPC;
            this.Remark = line.Remark;
            this.Dealer = line.Dealer;
            this.PrimaryOffer = line.PrimaryOffer;
            this.SupplementaryOffer = line.SupplementaryOffer;
            this.CardOwnerName = line.CardOwnerName;
            this.PlainCardNumber = line.PlainCardNumber;
            this.CardExpiryDate = line.CardExpiryDate;
            this.SubscriberType = line.SubscriberType;
            this.Bank = line.Bank;
            this.CardType = line.CardType;
            this.P2P = line.P2P;
            this.FeeInfos = line.FeeInfos;
            this.PurchaseResourceInfos = line.PurchaseResourceInfos;
            this.PaymentDetailsInfos = line.PaymentDetailsInfos;
            this.ContractInfos = line.ContractInfos;
			this.IntegrationMessage = line.IntegrationMessage;
        }

        protected LineDetail SetData(Entity entity, Dictionary<string, EntitySchemaQueryColumn> columns)
        {
            var line = new LineDetail();
            
            line.Id = entity.GetValue<Guid>(columns, "Id");
            line.No = entity.GetValue<int>(columns, "No");
            line.LineId = entity.GetValue<int>(columns, "LineId");
            line.MSISDN = entity.GetValue<string>(columns, "MSISDN");
            line.SubmissionId = entity.GetValue<Guid>(columns, "SubmissionId");
            line.CRMGroupId = entity.GetValue<Guid>(columns, "CRMGroupId");
            line.SerialNumber = entity.GetValue<string>(columns, "SerialNumber");
            line.OPPageOpen = entity.GetValue<DateTime>(columns, "OPPageOpenDate");
            
            line.SubParentGroupName = entity.GetValue<string>(columns, "SubParentGroupName");
            line.SubParentGroupNo = entity.GetValue<string>(columns, "SubParentGroupNo");
            line.SubParentGroupID = entity.GetValue<string>(columns, "SubParentGroupID");
            line.SubParentGroupCustomerID = entity.GetValue<string>(columns, "SubParentGroupCustomerID");
            line.SubParentGroupAccountID = entity.GetValue<string>(columns, "SubParentGroupAccountID");
            line.SubParentGroupAccountCode = entity.GetValue<string>(columns, "SubParentGroupAccountCode");
            line.SubParentGroupBRN = entity.GetValue<string>(columns, "SubParentGroupBRN");
            
            Guid sourceId = entity.GetValue<Guid>(columns, "Source_Id");
            line.Source = sourceId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = sourceId,
                    Name = entity.GetValue<string>(columns, "Source_Name"),
                } : null;

            line.CustomerID = entity.GetValue<string>(columns, "CustomerID");
            line.AccountID = entity.GetValue<string>(columns, "AccountID");
            line.SubParentGroupPaymentID = entity.GetValue<string>(columns, "SubParentGroupPaymentID");
            line.IDNo = entity.GetValue<string>(columns, "IDNo");

            Guid submissionTypeId = entity.GetValue<Guid>(columns, "SubmissionType_Id");
            line.SubmissionType = submissionTypeId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = submissionTypeId,
                    Name = entity.GetValue<string>(columns, "SubmissionType_Name"),
                    Code = entity.GetValue<string>(columns, "SubmissionType_Code")
                } : null;

            Guid idTypeId = entity.GetValue<Guid>(columns, "IdType_Id");
            if(idTypeId != Guid.Empty) {
                string idTypeCode = entity.GetValue<string>(columns, "IdType_Code");
                line.IDType = new DgMasterData.Lookup() {
                    Id = idTypeId,
                    Name = entity.GetValue<string>(columns, "IdType_Name"),
                    Code = idTypeCode
                };

                if(idTypeCode == "1") {
                    line.IDNo = line.IDNo.Replace("-", "");
                }
            }

            line.DateOfBirth = entity.GetValue<DateTime>(columns, "DateOfBirth");

            Guid dnoIDId = entity.GetValue<Guid>(columns, "DNO_Id");
            if(dnoIDId != Guid.Empty) {
                line.DNO = new LookupV2() {
                    Id = dnoIDId,
                    Name = entity.GetValue<string>(columns, "DNO_Name"),
                    CRMCode = entity.GetValue<string>(columns, "DNO_CRMCode"),
                    CSGCode = entity.GetValue<string>(columns, "DNO_CSGCode"),
                    Position = entity.GetValue<int>(columns, "DNO_Position")
                };
            }

            Guid dnoIDType = entity.GetValue<Guid>(columns, "DNOIDType_Id");
            if(dnoIDType != Guid.Empty) {
                line.DNOIDType = new DgMasterData.Lookup() {
                    Id = dnoIDType,
                    Name = entity.GetValue<string>(columns, "DNOIDType_Name"),
                    Code = entity.GetValue<string>(columns, "DNOIDType_Code")
                };
            }

            line.DNOIDNo = entity.GetValue<string>(columns, "DNOIDNo");
            line.DNOAccountCode = entity.GetValue<string>(columns, "DNOAccountCode");
            line.DNOCompanyName = entity.GetValue<string>(columns, "DNOCompanyName");
            
            Guid genderId = entity.GetValue<Guid>(columns, "Gender_Id");
            if(genderId == Guid.Empty) {
                line.Gender = new DgMasterData.Lookup() {
                    Code = "0"
                };
            } else {
                line.Gender = new DgMasterData.Lookup() {
                    Id = genderId,
                    Name = entity.GetValue<string>(columns, "Gender_Name"),
                    Code = entity.GetValue<string>(columns, "Gender_Code")
                };
            }

            Guid titleId = entity.GetValue<Guid>(columns, "Title_Id");
            if(titleId == Guid.Empty) {
                line.Title = new DgMasterData.Lookup();

                if(line.Gender.Code == "1") {
                    line.Title.Code = "0";
                } else if(line.Gender.Code == "2") {
                    line.Title.Code = "4";
                } else {
                    line.Title.Code = "23";
                }
            } else {
                line.Title = new DgMasterData.Lookup() {
                    Id = titleId,
                    Name = entity.GetValue<string>(columns, "Title_Name"),
                    Code = entity.GetValue<string>(columns, "Title_Code")
                };
            } 

            Guid nationalityId = entity.GetValue<Guid>(columns, "Nationality_Id");
            if(nationalityId == Guid.Empty) {
                line.Nationality = new DgMasterData.Country() {
                    CRMCode = "1458"
                };
            } else {
                line.Nationality = new DgMasterData.Country() {
                    Id = nationalityId,
                    Name = entity.GetValue<string>(columns, "Nationality_Name"),
                    CRMCode = entity.GetValue<string>(columns, "Nationality_CRMCode")
                };
            }

            line.Email = entity.GetValue<string>(columns, "Email");
            line.CompanyName = entity.GetValue<string>(columns, "CompanyName");
            line.CustomerName = entity.GetValue<string>(columns, "CustomerName");
            
            Guid postcodeId = entity.GetValue<Guid>(columns, "LegalPostcode_Id");
            Guid cityId = entity.GetValue<Guid>(columns, "LegalCity_Id");
            Guid stateId = entity.GetValue<Guid>(columns, "LegalProvince_Id");
            Guid countryId = entity.GetValue<Guid>(columns, "LegalCountry_Id");

            line.LegalAddress = new Address() {
                StreetAddress = entity.GetValue<string>(columns, "LegalAddress"),
                PostCode = postcodeId != Guid.Empty ? 
                    new PostCode() {
                        Id = postcodeId,
                        Name = entity.GetValue<string>(columns, "LegalPostcode_Name"),
                        Code = entity.GetValue<string>(columns, "LegalPostcode_Code")
                    } : null,
                City = cityId != Guid.Empty ? 
                    new DgMasterData.City() {
                        Id = cityId,
                        Name = entity.GetValue<string>(columns, "LegalCity_Name"),
                        CRMCode = entity.GetValue<string>(columns, "LegalCity_Name") == "Serdang" ? GetProperSerdangCode(entity.GetValue<string>(columns, "LegalProvince_Code"), "Serdang") : entity.GetValue<string>(columns, "LegalCity_Code")
                    } : null,
                State = stateId != Guid.Empty ? 
                    new State() {
                        Id = stateId,
                        Name = entity.GetValue<string>(columns, "LegalProvince_Name"),
                        CRMCode = entity.GetValue<string>(columns, "LegalProvince_Code")
                    } : null,
                Country = countryId != Guid.Empty ? 
                    new DgMasterData.Country() {
                        Id = countryId,
                        Name = entity.GetValue<string>(columns, "LegalCountry_Name"),
                        CRMCode = entity.GetValue<string>(columns, "LegalCountry_Code")
                    } : null
            };
			
			Guid bill_postcodeId = entity.GetValue<Guid>(columns, "BillPostcode_Id");
            Guid bill_cityId = entity.GetValue<Guid>(columns, "BillCity_Id");
            Guid bill_stateId = entity.GetValue<Guid>(columns, "BillProvince_Id");
            Guid bill_countryId = entity.GetValue<Guid>(columns, "BillCountry_Id");

            line.BillAddress = new Address() {
                StreetAddress = entity.GetValue<string>(columns, "BillAddress"),
                PostCode = bill_postcodeId != Guid.Empty ? 
                    new PostCode() {
                        Id = postcodeId,
                        Name = entity.GetValue<string>(columns, "BillPostcode_Name"),
                        Code = entity.GetValue<string>(columns, "BillPostcode_Code")
                    } : null,
                City = bill_cityId != Guid.Empty ? 
                    new DgMasterData.City() {
                        Id = cityId,
                        Name = entity.GetValue<string>(columns, "BillCity_Name"),
                        CRMCode = entity.GetValue<string>(columns, "BillCity_Name") == "Serdang" ? GetProperSerdangCode(entity.GetValue<string>(columns, "BillProvince_Code"), "Serdang") : entity.GetValue<string>(columns, "BillCity_Code")
                    } : null,
                State = bill_stateId != Guid.Empty ? 
                    new State() {
                        Id = stateId,
                        Name = entity.GetValue<string>(columns, "BillProvince_Name"),
                        CRMCode = entity.GetValue<string>(columns, "BillProvince_Code")
                    } : null,
                Country = bill_countryId != Guid.Empty ? 
                    new DgMasterData.Country() {
                        Id = countryId,
                        Name = entity.GetValue<string>(columns, "BillCountry_Name"),
                        CRMCode = entity.GetValue<string>(columns, "BillCountry_Code")
                    } : null
            };

            line.TelNo = entity.GetValue<string>(columns, "TelNo");
            line.BillCycle = entity.GetValue<string>(columns, "BillCycle");

            Guid billMediumId = entity.GetValue<Guid>(columns, "BillMedium_Id");
            line.BillMedium = billMediumId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = billMediumId,
                    Name = entity.GetValue<string>(columns, "BillMedium_Name"),
                    Code = entity.GetValue<string>(columns, "BillMedium_Code")
                } : null;
            
            Guid paymentModeId = entity.GetValue<Guid>(columns, "PaymentMode_Id");
            line.PaymentMode = paymentModeId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = paymentModeId,
                    Name = entity.GetValue<string>(columns, "PaymentMode_Name"),
                    Code = entity.GetValue<string>(columns, "PaymentMode_Code")
                } : null;
            
            line.SIMCardSerialNumber = entity.GetValue<string>(columns, "SIMCardSerialNumber");
            line.CreditLimit = entity.GetValue<decimal>(columns, "CreditLimit");
            
            Guid prpcId = entity.GetValue<Guid>(columns, "PRPC_Id");
            line.PRPC = prpcId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = prpcId,
                    Name = entity.GetValue<string>(columns, "PRPC_Name"),
                    Code = entity.GetValue<string>(columns, "PRPC_Code")
                } : null;
            
            line.Remark = entity.GetValue<string>(columns, "Remark");
            
            Guid dealerId = entity.GetValue<Guid>(columns, "Dealer_Id");
            line.Dealer = dealerId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = dealerId,
                    Name = entity.GetValue<string>(columns, "Dealer_Name"),
                    Code = entity.GetValue<string>(columns, "Dealer_Code")
                } : null;

            Guid primaryOfferId = entity.GetValue<Guid>(columns, "PrimaryOffer_Id");
            Guid primaryOffer_offerTypeId = entity.GetValue<Guid>(columns, "PrimaryOffer_OfferType_Id");

            line.PrimaryOffer = primaryOfferId != Guid.Empty ? 
                new Offering() {
                    Id = entity.GetValue<Guid>(columns, "PrimaryOffer_Id"),
                    OfferID = entity.GetValue<string>(columns, "PrimaryOffer_OfferID"),
                    OfferName = entity.GetValue<string>(columns, "PrimaryOffer_OfferName"),
                    OfferType = primaryOffer_offerTypeId != Guid.Empty ? 
                        new DgMasterData.Lookup() {
                            Id = primaryOffer_offerTypeId,
                            Name = entity.GetValue<string>(columns, "PrimaryOffer_OfferType_Name")
                        } : null,
                    EffectiveDate = entity.GetValue<DateTime>(columns, "PrimaryOffer_EffectiveDate"),
                    ExpiryDate = entity.GetValue<DateTime>(columns, "PrimaryOffer_ExpiryDate"),
                } : null;

            line.SupplementaryOffer = new List<Offering>();
            for(int i=1; i<=20; i++) {
                Guid suppOfferId = entity.GetValue<Guid>(columns, $"SuppOffer{i}_Id");
                if(suppOfferId == Guid.Empty) {
                    line.SupplementaryOffer.Add(null);
                } else {
                    Guid offerTypeId = entity.GetValue<Guid>(columns, $"SuppOffer{i}_OfferType_Id");
                    line.SupplementaryOffer.Add(new Offering() {
                        Id = entity.GetValue<Guid>(columns, $"SuppOffer{i}_Id"),
                        OfferID = entity.GetValue<string>(columns, $"SuppOffer{i}_OfferID"),
                        OfferName = entity.GetValue<string>(columns, $"SuppOffer{i}_OfferName"),
                        OfferType = offerTypeId != Guid.Empty ? 
                            new DgMasterData.Lookup() {
                                Id = offerTypeId,
                                Name = entity.GetValue<string>(columns, $"SuppOffer{i}_OfferType_Name")
                            } : null,
                        EffectiveDate = entity.GetValue<DateTime>(columns, $"SuppOffer{i}_EffectiveDate"),
                        ExpiryDate = entity.GetValue<DateTime>(columns, $"SuppOffer{i}_ExpiryDate")
                    });
                }
            }

            Guid bankId = entity.GetValue<Guid>(columns, "Bank_Id");
            line.Bank = bankId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = bankId,
                    Name = entity.GetValue<string>(columns, "Bank_Name"),
                    Code = entity.GetValue<string>(columns, "Bank_Code")
                } : null;

            Guid cardTypeId = entity.GetValue<Guid>(columns, "CardType_Id");
            line.CardType = cardTypeId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = cardTypeId,
                    Name = entity.GetValue<string>(columns, "CardType_Name"),
                    Code = entity.GetValue<string>(columns, "CardType_Code")
                } : null;

            line.CardOwnerName = entity.GetValue<string>(columns, "CardOwnerName");
            line.PlainCardNumber = entity.GetValue<string>(columns, "PlainCardNumber");
            line.CardNumberEncrypt = !string.IsNullOrEmpty(line.PlainCardNumber) ? 
                CRMHelper.EncryptCardNumber(UserConnection, line.PlainCardNumber) : string.Empty;

            DateTime cardExp = entity.GetValue<DateTime>(columns, "CardExpiryDate");
            line.CardExpiryDate = cardExp != null && cardExp != DateTime.MinValue ? cardExp.ToString("yyyyMM") : string.Empty;
            
            Guid subscriberTypeId = entity.GetValue<Guid>(columns, "SubscriberType_Id");
            line.SubscriberType = subscriberTypeId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = subscriberTypeId,
                    Name = entity.GetValue<string>(columns, "SubscriberType_Name"),
                    Code = entity.GetValue<string>(columns, "SubscriberType_Code")
                } : null;

            Guid p2pId = entity.GetValue<Guid>(columns, "P2P_Id");
            line.P2P = p2pId != Guid.Empty ? 
                new DgMasterData.Lookup() {
                    Id = p2pId,
                    Name = entity.GetValue<string>(columns, "P2P_Name")
                } : null;

            line.FeeInfos = CRMHelper.GetFeeDetail(UserConnection, line.Id);

            var suppOfferList = line.SupplementaryOffer.Where(item => item != null).ToList();
            if(suppOfferList.Count > 0) {
                List<OfferingRSRC> offeringRSRCList = CRMHelper.GetOfferingRSRC(UserConnection, suppOfferList);
                line.PurchaseResourceInfos = CRMHelper.GetPurchaseResourceInfos(offeringRSRCList);
                line.ContractInfos = CRMHelper.GetContractInfos(offeringRSRCList);
            }
			
			line.IntegrationMessage = new List<string>();

            return line;
        }
        
        #endregion

        #region Validation

        public List<LineResult> IsValid(List<LineDetail> Lines)
        {
            var result = new List<LineResult>();

            try {
                var offeringMasterList = CRMHelper.GetOfferingFromDB(UserConnection, Guid.Empty);
                var offeringRelationshipMasterList = CRMHelper.GetOfferRelationshipFromDB(UserConnection);
                var bundleOfferingMasterList = CRMHelper.GetBundleOfferingFromDB(UserConnection);
                // var offeringUpgradeDowngradeMasterList = new List<OfferUpgradeDowngrade>();

                if(offeringMasterList == null || offeringMasterList.Count == 0) {
                    throw new Exception("Offering cannot be found in NCCF database!");
                }

                if(offeringRelationshipMasterList == null || offeringRelationshipMasterList.Count == 0) {
                    throw new Exception("Offering Relationship cannot be found in NCCF database!");
                }

                if(bundleOfferingMasterList == null || bundleOfferingMasterList.Count == 0) {
                    throw new Exception("Bundle Offering cannot be found in NCCF database!");
                }

                var isCOP = Lines.Where(item => IsCOP(item)).ToList().Count > 0 ? true : false;
                if(isCOP) {
                    Lines = Lines.COPIntegration(UserConnection).GetAwaiter().GetResult();
                }
				
				/*
                var isCOP_P2P = Lines.Where(item => IsCOP_P2P(item)).ToList().Count > 0 ? true : false;
                if(isCOP_P2P) {
                    offeringUpgradeDowngradeMasterList = CRMHelper.GetOfferingUpgradeDowngradeFromDB(UserConnection);
                    if(offeringUpgradeDowngradeMasterList == null || offeringUpgradeDowngradeMasterList.Count == 0) {
                        throw new Exception("Upgrade/Downgrade List cannot be found in NCCF database!");
                    }
                }
				*/

                foreach(var line in Lines) {
                    var res = new LineResult();
                    res.Line = line;

                    try {
                        IsLineValid(line);
                        // IsOfferRelationshipValid(line, offeringMasterList, offeringRelationshipMasterList);
                        IsBundleOfferValid(line, offeringMasterList, bundleOfferingMasterList);

                        if(isCOP) {
                            if(line.IntegrationMessage.Count > 0) {
                                string errorMessage = string.Join("", line.IntegrationMessage.Select(item => $"<li>{item}</li>").ToArray());
                                throw new Exception($"<br><ul>{errorMessage}</ul>");
                            }

                            ValidateMaxOder(line, line.UsingOffers, offeringMasterList);
							
							/*
                            if(IsCOP_P2P(line)) {
                                IsOfferUpgradeDowngradeValid(line, offeringMasterList, line.UsingOffers, offeringUpgradeDowngradeMasterList);
                            }
							*/
                        }

                        res.Result.Success = true;
                    } catch(Exception e) {
                        res.Result.Message = e.Message;
                    }

                    result.Add(res);
                }

            } catch(Exception e) {
                throw new Exception($"Line Validation: {e.Message}");
            }

            return result;
        }

        public List<LineResult> IsLineMandatoryValid(List<LineDetail> Lines)
        {
            var result = new List<LineResult>();

            foreach(var line in Lines) {
                var res = new LineResult();
                res.Line = line;

                try {
                    IsLineValid(line);
                    res.Result.Success = true;
                } catch(Exception e) {
                    res.Result.Message = e.Message;
                }

                result.Add(res);
            }

            return result;
        }

        public List<LineResult> IsOfferingMandatoryValid(List<LineDetail> Lines)
        {
            var result = new List<LineResult>();

            var offeringMasterList = CRMHelper.GetOfferingFromDB(UserConnection, Guid.Empty);
            var offeringRelationshipMasterList = CRMHelper.GetOfferRelationshipFromDB(UserConnection);
            var bundleOfferingMasterList = CRMHelper.GetBundleOfferingFromDB(UserConnection);

            if(offeringMasterList == null || offeringMasterList.Count == 0) {
                throw new Exception("Offering cannot be found in NCCF database!");
            }

            if(offeringRelationshipMasterList == null || offeringRelationshipMasterList.Count == 0) {
                throw new Exception("Offering Relationship cannot be found in NCCF database!");
            }

            if(bundleOfferingMasterList == null || bundleOfferingMasterList.Count == 0) {
                throw new Exception("Bundle Offering cannot be found in NCCF database!");
            }

            foreach(var line in Lines) {
                var res = new LineResult();
                res.Line = line;

                try {
                    IsOfferRelationshipValid(line, offeringMasterList, offeringRelationshipMasterList);
                    IsBundleOfferValid(line, offeringMasterList, bundleOfferingMasterList);
                    
                    res.Result.Success = true;
                } catch(Exception e) {
                    res.Result.Message = e.Message;
                }

                result.Add(res);
            }

            return result;
        }

        protected void IsLineValid(LineDetail Line)
        {
            bool isCOP = Line.SubmissionType != null && Line.SubmissionType.Id == LookupConst.SubmissionType.COP;
            bool isMNP = Line.SubmissionType != null && Line.SubmissionType.Id == LookupConst.SubmissionType.MNP;
            bool isCOP_P2P = IsCOP_P2P(Line);

            List<string> errorList = new List<string>();

            if(Line.LineId <= 0) {
                errorList.Add("Line ID cannot be zero");
            }

            if(Line.SubmissionId == null || Line.SubmissionId == Guid.Empty) {
                errorList.Add("Submission not found");
            }

            if(Line.CRMGroupId == null || Line.CRMGroupId == Guid.Empty) {
                errorList.Add("CRM Group not found");
            }

            if(string.IsNullOrEmpty(Line.SubParentGroupNo)) {
                errorList.Add("Sub Parent Group Number cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Line.SubParentGroupBRN)) {
                errorList.Add("BRN cannot be null or empty.");
            }

            if(Line.SubmissionType == null) {
                errorList.Add("Submission Type cannot be null or empty");
            }

            if(Line.Source == null) {
                errorList.Add("Source cannot be null or empty");
            }

            if(Line.Dealer == null || (Line.Dealer != null && string.IsNullOrEmpty(Line.Dealer.Code))) {
                errorList.Add("Dealer Code cannot be null or empty");
            }

            if(Line.SubscriberType == null || (Line.SubscriberType != null && string.IsNullOrEmpty(Line.SubscriberType.Code))) {
                errorList.Add("Subscriber Type cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Line.MSISDN)) {
                errorList.Add("MSISDN cannot be null or empty");
            }
            
            if(string.IsNullOrEmpty(Line.IDNo)) {
                errorList.Add("ID Number cannot be null or empty");
            }

            if(Line.IDType == null || (Line.IDType != null && string.IsNullOrEmpty(Line.IDType.Code))) {
                errorList.Add("ID Type cannot be null or empty");
            }

            if(!isCOP && (Line.PRPC == null || (Line.PRPC != null && string.IsNullOrEmpty(Line.PRPC.Code)))) {
                errorList.Add("PRMode cannot be null or empty");
            }

            if(Line.CreditLimit <= 0) {
                errorList.Add("Credit Limit cannot be zero");
            }

            if(!isCOP && (Line.PaymentMode == null || (Line.PaymentMode != null && string.IsNullOrEmpty(Line.PaymentMode.Code)))) {
                errorList.Add("Payment Mode cannot be null or empty");
            }

            if(Line.PaymentMode != null && Line.PaymentMode.Code == "DDCC") {
                if(string.IsNullOrEmpty(Line.PlainCardNumber)) {
                    errorList.Add("Card Number cannot be null or empty");
                }

                if(string.IsNullOrEmpty(Line.CardExpiryDate)) {
                    errorList.Add("Card Number Expiry Date cannot be null or empty");
                }

                if(string.IsNullOrEmpty(Line.TokenID)) {
                    errorList.Add("Token ID cannot be null or empty");
                }
            }

            if(string.IsNullOrEmpty(Line.CompanyName)) {
                errorList.Add("Company Name cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Line.CustomerName)) {
                errorList.Add("Customer Name cannot be null or empty");
            }

            if(Line.BillAddress != null) {
                if(string.IsNullOrEmpty(Line.BillAddress.StreetAddress)) {
                    errorList.Add("Billing Address: Address 1 cannot be null or empty");
                }

                if(Line.BillAddress.PostCode == null || (Line.BillAddress.PostCode != null && string.IsNullOrEmpty(Line.BillAddress.PostCode.Code))) {
                    errorList.Add("Billing Address: Postcode cannot be null or empty");
                }

                if(Line.BillAddress.City == null || (Line.BillAddress.City != null && string.IsNullOrEmpty(Line.BillAddress.City.CRMCode))) {
                    errorList.Add("Billing Address: City cannot be null or empty");
                }

                if(Line.BillAddress.State == null || (Line.BillAddress.State != null && string.IsNullOrEmpty(Line.BillAddress.State.CRMCode))) {
                    errorList.Add("Billing Address: State cannot be null or empty");
                }

                if(Line.BillAddress.Country == null || (Line.BillAddress.Country != null && string.IsNullOrEmpty(Line.BillAddress.Country.CRMCode))) {
                    errorList.Add("Billing Address: Country cannot be null or empty");
                }
            }

            if(Line.PrimaryOffer == null || (Line.PrimaryOffer != null && (string.IsNullOrEmpty(Line.PrimaryOffer.OfferID) || string.IsNullOrEmpty(Line.PrimaryOffer.OfferName)))) {
                errorList.Add("Primary Offer cannot be null or empty");
            }

            if(Line.PrimaryOffer != null && (Line.PrimaryOffer.OfferType != null && Line.PrimaryOffer.OfferType.Id != LookupConst.OfferType.PrimaryOffering)) {
                errorList.Add($"Offer Type {Line.PrimaryOffer.OfferName} not valid for Primary Offering");
            }

            List<Offering> suppOfferList = Line.SupplementaryOffer != null ? 
                Line.SupplementaryOffer.Where(item => item != null).ToList() : new List<Offering>();
            if(suppOfferList.Count == 0) {
                errorList.Add("Supplementary Offer cannot be null or empty");
            }

            if(suppOfferList.Count > 0) {
                var suppOfferNotValid = suppOfferList
                    .Where(item => item.OfferType != null && item.OfferType.Id != LookupConst.OfferType.SupplementaryOffering)
                    .ToList();
                if(suppOfferNotValid.Count > 0) {
                    foreach(var suppOffer in suppOfferNotValid) {
                        errorList.Add($"Offer Type {suppOffer.OfferName} not valid for Supplementary Offering");
                    }
                }

                for(int i=0; i<Line.SupplementaryOffer.Count; i++) {
                    var suppOffer = Line.SupplementaryOffer[i];
                    if(suppOffer == null) {
                        continue;
                    }

                    if(string.IsNullOrEmpty(suppOffer.OfferID) || string.IsNullOrEmpty(suppOffer.OfferName)) {
                        errorList.Add($"Supp Offer {i+1} - Offer ID / Offer Name cannot be null or empty");
                    }
                }
            }

            if (isMNP) {
                if(Line.DNOIDType == null || (Line.DNOIDType != null && string.IsNullOrEmpty(Line.DNOIDType.Name))) {
                    errorList.Add("Donor ID Type cannot be null or empty.");
                }   

                if (string.IsNullOrEmpty(Line.DNOIDNo)) {
                    errorList.Add("Donor ID Number cannot be null or empty.");
                }   
        
                if(Line.DNO == null || (Line.DNO != null && string.IsNullOrEmpty(Line.DNO.Name))) {
                    errorList.Add("Donor Network Operator cannot be null or empty.");
                }   

                if ((Line.DNOIDType != null && Line.DNOIDType.Code == "3") && string.IsNullOrEmpty(Line.DNOAccountCode)) {
                    errorList.Add("Donor Account Code cannot be null or empty.");
                }
            }

            if(errorList.Count > 0) {
                string errorMessage = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"[Line Validation] No. {Line.No} - {Line.MSISDN} fail.<br><ul>{errorMessage}</ul>");
            }
        }

        protected void IsOfferRelationshipValid(LineDetail Line, List<Offering> OfferingMasterList, List<OfferRelationship> OfferRelationshipMasterList)
        {
            var log = new CustomLog(UserConnection, "DEBUG_OFFERING_RELATIONSHIP_VALIDATION");

            List<string> errorList = new List<string>();

            List<string> andDependentOfferValues = new List<string>();
            List<string> orDependentOfferValues = new List<string>();
            string orDependentOfferSubmittedOffers = "";
            string andDependentOfferSubmittedOffers = "";
            bool isFoundOrDependentOffer = false;
            bool isFoundAndDependentOffer = false;

            log.AddMessage($"No. {Line.No}. Serial Number: {Line.SerialNumber}. MSISDN: {Line.MSISDN}");

            var suppOfferList = Line.SupplementaryOffer
                .Where(item => item != null)
                .ToList();
            suppOfferList.Insert(0, Line.PrimaryOffer);

            log.AddMessage($"List offering: {Environment.NewLine}"+string.Join(Environment.NewLine, suppOfferList.Select((item, index) => $"{index+1}.  [{item.OfferID}] {item.OfferName}").ToArray()));
            log.AddMessage(Environment.NewLine);
                
            foreach(Offering suppOffer in suppOfferList) {
                log.AddMessage($"Offering [{suppOffer.OfferID}] {suppOffer.OfferName} process to validation");

                var offerRelationshipList = OfferRelationshipMasterList
                    .Where(item => item.OtherOfferID == suppOffer.OfferID)
                    .ToList();
                log.AddMessage($"Offering Relationship: {Environment.NewLine}"+string.Join(", ", offerRelationshipList.Select(item => item.OfferID)));
                log.AddMessage(Environment.NewLine);

                foreach(var offerRelationship in offerRelationshipList) {
                    log.AddMessage(JsonConvert.SerializeObject(offerRelationship, Formatting.Indented));

                    if(offerRelationship == null) {
                        continue;
                    }

                    if(string.IsNullOrEmpty(offerRelationship.OfferID)) {
                        continue;
                    }

                    string offerID = suppOfferList
                        .Where(item => item.OfferID == offerRelationship.OfferID)
                        .Select(item => item.OfferID)
                        .FirstOrDefault();
                    string offerName = OfferingMasterList
                        .Where(item => item.OfferID == offerRelationship.OfferID)
                        .Select(item => item.OfferName)
                        .FirstOrDefault();
                    string otherOfferName = OfferingMasterList
                        .Where(item => item.OfferID == offerRelationship.OtherOfferID)
                        .Select(item => item.OfferName)
                        .FirstOrDefault();

                    log.AddMessage($"offerID: {offerID}. offerName: {offerName}. otherOfferName: {otherOfferName}");
                    
                    if(offerRelationship.RelationshipType == "0") { 
                        log.AddMessage("RelationshipType is 0");

                        if(string.IsNullOrEmpty(offerID)) {
                            errorList.Add($"Missing mandatory offer. Offer:[{offerRelationship.OfferID}] {offerName} | Other Offer:[{offerRelationship.OtherOfferID}] {otherOfferName}");
                        }
                    } else if(offerRelationship.RelationshipType == "4") {
                        log.AddMessage("RelationshipType is 4");

                        if(!string.IsNullOrEmpty(offerID)) {
                            errorList.Add($"Found exclusive offer. Offer:[{offerRelationship.OfferID}] {offerName} | Other Offer:[{offerRelationship.OtherOfferID}] {otherOfferName}");
                        }
                    }
                    
                    if(offerRelationship.RelationshipType == "6") {
                        log.AddMessage("RelationshipType is 6");

                        if(!string.IsNullOrEmpty(offerID)) {
                            orDependentOfferValues.Add(offerID);
                            isFoundOrDependentOffer = false;
                        } else {
                            isFoundOrDependentOffer = true;
                            if (!orDependentOfferSubmittedOffers.Contains(offerRelationship.OtherOfferID)) {
                                orDependentOfferSubmittedOffers += $"{offerRelationship.OtherOfferID}-{otherOfferName}|";
                            }
                        }
                    }

                    if(offerRelationship.RelationshipType == "3") {
                        log.AddMessage("RelationshipType is 3");

                        if(!string.IsNullOrEmpty(offerID)) {
                            andDependentOfferValues.Add(offerID);
                            isFoundAndDependentOffer = false;
                        } else {
                            isFoundAndDependentOffer = true;
                            if (!andDependentOfferSubmittedOffers.Contains(offerRelationship.OtherOfferID)) {
                                andDependentOfferSubmittedOffers += $"{offerRelationship.OtherOfferID}-{otherOfferName}|";
                            }
                        }
                    }
                }
            }

            if (isFoundOrDependentOffer) {
                log.AddMessage("isFoundOrDependentOffer");
                log.AddMessage(JsonConvert.SerializeObject(new {
                    orDependentOfferValues = orDependentOfferValues,
                    orDependentOfferSubmittedOffers = orDependentOfferSubmittedOffers
                }, Formatting.Indented));

                if (orDependentOfferValues.Count == 0) {
                    errorList.Add($"Missing OR-dependent offer for [{orDependentOfferSubmittedOffers}]");
                }
            }

            if(isFoundAndDependentOffer) {
                log.AddMessage("isFoundAndDependentOffer");
                log.AddMessage(JsonConvert.SerializeObject(new {
                    andDependentOfferValues = andDependentOfferValues,
                    andDependentOfferSubmittedOffers = andDependentOfferSubmittedOffers
                }, Formatting.Indented));

                foreach(string theAndDependentOffer in andDependentOfferValues) {
                    if(suppOfferList.Exists(item => item.OfferID == theAndDependentOffer)) {
                        errorList.Add($"Missing AND-dependent offer for [{andDependentOfferSubmittedOffers}]");
                    }
                }
            }

            log.AddMessage(Environment.NewLine);
            log.SaveToFile();

            if(errorList.Count > 0) {
                // throw new Exception(string.Join("\n", errorList.ToArray()));
                string errorMessage = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"[Offer Relationship Validation] No. {Line.No} - {Line.MSISDN} fail.<br><ul>{errorMessage}</ul>");
            }
        }

        protected void IsBundleOfferValid(LineDetail Line, List<Offering> OfferingMasterList, List<BundleOffering> BundleOfferingMasterList)
        {
            List<string> errorList = new List<string>();

            var suppOfferList = Line.SupplementaryOffer
                .Where(item => item != null)
                .ToList();
            suppOfferList.Insert(0, Line.PrimaryOffer);

            foreach(Offering suppOffer in suppOfferList) {
                if(suppOffer == null) {
                    continue;
                }

                var submittedOfferDetails = OfferingMasterList
                    .Where(item => item.OfferID == suppOffer.OfferID)
                    .FirstOrDefault();
                if(submittedOfferDetails == null) {
                    continue;
                }

                if(!submittedOfferDetails.BundleFlag) {
                    continue;
                }

                var bundleOfferingDetailsList = BundleOfferingMasterList
                    .Where(item => item.OfferID == suppOffer.OfferID && item.RelationType == "4")
                    .ToList();
                if(bundleOfferingDetailsList == null || (bundleOfferingDetailsList != null && bundleOfferingDetailsList.Count == 0)) {
                    continue;
                }

                int nSubmittedTotalElementCount = 0;
                foreach(var bundleOfferingDetails in bundleOfferingDetailsList) {
                    var repeatedOfferIdList = suppOfferList
                        .Where(item => item.OfferID == bundleOfferingDetails.ElementID)
                        .ToList();
                    if(repeatedOfferIdList == null || (repeatedOfferIdList != null && repeatedOfferIdList.Count == 0)) {
                        continue;
                    }

                    nSubmittedTotalElementCount += 1;

                    int nMin = 0;
                    if(!string.IsNullOrEmpty(bundleOfferingDetails.MinNum)) {
                        nMin = Convert.ToInt32(bundleOfferingDetails.MinNum);
                    }

                    string bundleName = OfferingMasterList
                        .Where(item => item.OfferID == bundleOfferingDetails.OfferID)
                        .Select(item => item.OfferName)
                        .FirstOrDefault();
                    string elementName = OfferingMasterList
                        .Where(item => item.OfferID == bundleOfferingDetails.ElementID)
                        .Select(item => item.OfferName)
                        .FirstOrDefault();

                    if(nSubmittedTotalElementCount < nMin) {
                        errorList.Add($"Submitted element offer is less than minimum requirement. Bundle Offer:[{bundleOfferingDetails.OfferID}]{bundleName} | Element:[{bundleOfferingDetails.ElementID}]{elementName}");
                    }

                    int nMax = 0;
                    if(!string.IsNullOrEmpty(bundleOfferingDetails.MaxNum)) {
                        nMax = Convert.ToInt32(bundleOfferingDetails.MaxNum);
                    }

                    if (nSubmittedTotalElementCount > nMax) {
                        errorList.Add($"Submitted element offer is more than maximum requirement. Bundle Offer:[{bundleOfferingDetails.OfferID}]{bundleName} | Element:[{bundleOfferingDetails.ElementID}]{elementName}");
                    }
                }

                if(nSubmittedTotalElementCount == 0) {
                    string bundleName = OfferingMasterList
                        .Where(item => item.OfferID == suppOffer.OfferID)
                        .Select(item => item.OfferName)
                        .FirstOrDefault();

                    errorList.Add($"There is no element offer selected for submitted Bundle. Bundle Offer:[{suppOffer.OfferID}]{bundleName}");
                }
            }

            if(errorList.Count > 0) {
                string errorMessage = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"[Offer Bundle Validation] No. {Line.No} - {Line.MSISDN} fail.<br><ul>{errorMessage}</ul>");
            }
        }

        public void IsOfferUpgradeDowngradeValid(LineDetail Line, List<Offering> OfferingMasterList, List<SubscriberOfferValue> SubscribedOfferValueList, List<OfferUpgradeDowngrade> OfferingUpgradeDowngradeMasterList)
        {
            try {
                Offering submittedPrimaryOfferDetails = null;
                var SubmittedOfferIdList = Line.SupplementaryOffer
                    .Where(item => item != null)
                    .ToList();
                SubmittedOfferIdList.Insert(0, Line.PrimaryOffer);

                foreach (var submittedOffer in SubmittedOfferIdList) {
                    List<Offering> submittedPrimaryOfferDetailsList = OfferingMasterList.FindAll(item => item.OfferID == submittedOffer.OfferID);

                    if (submittedPrimaryOfferDetailsList != null && submittedPrimaryOfferDetailsList.Count == 1) {
                        submittedPrimaryOfferDetails = submittedPrimaryOfferDetailsList[0];
                    } else if (submittedPrimaryOfferDetailsList != null && submittedPrimaryOfferDetailsList.Count > 1) {
                        submittedPrimaryOfferDetails = submittedPrimaryOfferDetailsList.Find(item => item.SubscriberType != null && item.SubscriberType.Id == Line.SubscriberType?.Id);
                    }

                    if (submittedPrimaryOfferDetails != null && submittedPrimaryOfferDetails.OfferType != null && submittedPrimaryOfferDetails.OfferType.Id == LookupConst.OfferType.PrimaryOffering)
                        break;
                }

                if (submittedPrimaryOfferDetails == null) {
                    throw new Exception("No primary offer found in Offer Master Table based on submitted offer list");
                }

                Offering subscribedPrimaryOfferDetails = null;
                foreach (SubscriberOfferValue subscribedOffer in SubscribedOfferValueList) {
                    subscribedPrimaryOfferDetails = OfferingMasterList.Find(item => item.OfferID == subscribedOffer.offerId);
                    if (subscribedPrimaryOfferDetails != null && subscribedPrimaryOfferDetails.OfferType != null && subscribedPrimaryOfferDetails.OfferType.Id == LookupConst.OfferType.PrimaryOffering)
                        break;
                }

                if (subscribedPrimaryOfferDetails == null) {
                    throw new Exception("No primary offer found in Offer Master Table based on subscribed offer list");
                }

                if (subscribedPrimaryOfferDetails.OfferID == submittedPrimaryOfferDetails.OfferID)
                    return;

                OfferUpgradeDowngrade offeringUpgradeDowngrade = OfferingUpgradeDowngradeMasterList.Find(item => item.OfferID == subscribedPrimaryOfferDetails.OfferID && item.OtherOfferID == submittedPrimaryOfferDetails.OfferID);

                if (offeringUpgradeDowngrade == null) {
                    string strSubscribedOfferName = OfferingMasterList.FirstOrDefault(item => item.OfferID == subscribedPrimaryOfferDetails.OfferID)?.OfferName;
                    string strSubmittedOfferName = OfferingMasterList.FirstOrDefault(item => item.OfferID == submittedPrimaryOfferDetails.OfferID)?.OfferName;

                    throw new Exception($"Submitted offer is not allowed for Upgrade/Downgrade. Subscribed Primary Offer:[{subscribedPrimaryOfferDetails.OfferID}] {strSubscribedOfferName} | Submitted Primary Offer:[{submittedPrimaryOfferDetails.OfferID}] {strSubmittedOfferName}");
                }   
            } catch (Exception e) {
                throw new Exception($"[Offer Upgrade/Downgrade Validation] No. {Line.No} - {Line.MSISDN} fail.<br><ul>{e.Message}</ul>");
            }
        }

        public void ValidateMaxOder(LineDetail Line, List<SubscriberOfferValue> SubscribedOfferValueList, List<Offering> OfferingMasterList) 
        {
            List<string> errorList = new List<string>();

            var suppOfferList = Line.SupplementaryOffer
                .Where(item => item != null)
                .ToList();
            suppOfferList.Insert(0, Line.PrimaryOffer);

            foreach (var submittedOffer in suppOfferList) {
                List<Offering> submittedOfferDetailsList = OfferingMasterList.FindAll(item => item.OfferID == submittedOffer.OfferID);

                Offering submittedOfferDetails = null;
                if (submittedOfferDetailsList != null && submittedOfferDetailsList.Count == 1) {
                    submittedOfferDetails = submittedOfferDetailsList[0];
                } else if (submittedOfferDetailsList != null && submittedOfferDetailsList.Count > 1) {
                    submittedOfferDetails = submittedOfferDetailsList.Find(item => item.SubscriberType != null && item.SubscriberType.Id == Line.SubscriberType?.Id);
                }

                if (submittedOfferDetails == null)
                    continue;

                List<SubscriberOfferValue> numberOfSubscribedOffer = SubscribedOfferValueList.FindAll(item => item.offerId == submittedOffer.OfferID);
                
                int nMaxAllow = submittedOfferDetails.MaxOrdersTime;
                if (submittedOfferDetails.OfferType?.Id == LookupConst.OfferType.SupplementaryOffering && numberOfSubscribedOffer.Count > nMaxAllow) {
                    string strOfferName = OfferingMasterList.FirstOrDefault(item => item.OfferID == submittedOffer.OfferID)?.OfferName;
                    errorList.Add($"Requested offer exceeded number of allowed order. Offer:[{submittedOffer.OfferID}] {strOfferName}");
                }
            }

            if(errorList.Count > 0) {
                string errorMessage = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"[Validate Max Order] No. {Line.No} - {Line.MSISDN} fail.<br><ul>{errorMessage}</ul>");
            }
        }

        protected string GetProperSerdangCode(string StateCode, string CityName) {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgCity");
            var columns = new Dictionary<string, EntitySchemaQueryColumn> {
                {"CityCode", esq.AddColumn("DgCode")}
            };
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgState.DgCode", StateCode));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Name", CityName));
            var entities = esq.GetEntityCollection(UserConnection);
            var entity = entities.FirstOrDefault();
            var result = entity.GetTypedColumnValue<string>(columns["CityCode"].Name);

            return result;
        }

        #endregion

        public bool IsCOP_P2P(LineDetail Line)
        {
            return Line.SubmissionType != null 
                && Line.SubmissionType.Id == LookupConst.SubmissionType.COP 
                && Line.P2P != null
                && (Line.P2P.Id == LookupConst.ToS.Post2Post 
					|| Line.P2P.Id == LookupConst.ToS.Pre2Post
				   	|| Line.P2P.Id == LookupConst.ToS.RenewalAtoB);
        }

        public bool IsCOP(LineDetail Line)
        {
            return Line.SubmissionType != null 
                && Line.SubmissionType.Id == LookupConst.SubmissionType.COP;
        }
    }

    public class LineResult
    {
        public GeneralResponse Result { get; set; }
        public LineDetail Line { get; set; }

        public LineResult()
        {
            this.Result = new GeneralResponse();
        }
    }

    public static class LineIntegration
    {
        // getPhoneNumber
        public static async Task<List<LineDetail>> NewIntegration(this List<LineDetail> Lines, UserConnection UserConnection)
        {
            var crmService = new CRMService(UserConnection, true, "NEW");
            var getPhoneNumberTask = new List<Task<List<GetPhoneNumbersResponse.GetPhoneNumbersOut>>>();

            for(int i=0; i<Lines.Count; i++) {
                var line = Lines[i];
                if(line.IntegrationMessage == null) {
                    Lines[i].IntegrationMessage = new List<string>();
                }
                
                getPhoneNumberTask.Add(crmService.GetPhoneNumbers(line.MSISDN, line.Dealer?.Code));
            }

            Task getPhoneNumberTaskResult = null;
            try {
                getPhoneNumberTaskResult = Task.WhenAll(getPhoneNumberTask);
                await getPhoneNumberTaskResult;
            } catch(Exception e) {}

            for(int i=0; i<getPhoneNumberTask.Count; i++) {
                var task = getPhoneNumberTask[i];

                if(task.Status != TaskStatus.RanToCompletion) {
                    var exception = task.Exception;
                    var innerException = exception?.InnerExceptions;
                    string errorMessage = innerException?.FirstOrDefault()?.Message ?? string.Empty;

                    if(!string.IsNullOrEmpty(errorMessage)) {
                        Lines[i].IntegrationMessage.Add($"[GetPhoneNumbers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. {errorMessage}");
                    }

                    continue;
                }

                var phoneNumbers = task.Result;
                if(phoneNumbers == null || (phoneNumbers != null && phoneNumbers.Count == 0)) {
                    Lines[i].IntegrationMessage.Add($"[GetPhoneNumbers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. MSISDN not found in CRM");
                }
            }

            return Lines;
        }

        // getSubscribers & GetUsingOffers
        public static async Task<List<LineDetail>> COPIntegration(this List<LineDetail> Lines, UserConnection UserConnection)
        {
            var crmService = new CRMService(UserConnection, true, "COP");
            var getSubscribersTask = new List<Task<List<SubscriberValue>>>();
            var getUsingOffersTask = new List<Task<List<SubscriberOfferValue>>>();

            #region getSubscribers

            for(int i=0; i<Lines.Count; i++) {
                var line = Lines[i];
                if(line.IntegrationMessage == null) {
                    Lines[i].IntegrationMessage = new List<string>();
                }

                getSubscribersTask.Add(crmService.GetSubscribersByMSISDN(line.MSISDN));
            }
            
            Task getSubscribersTaskResult = null;
            try {
                getSubscribersTaskResult = Task.WhenAll(getSubscribersTask);
                await getSubscribersTaskResult;
            } catch(Exception e) {}

            for(int i=0; i<getSubscribersTask.Count; i++) {
                var task = getSubscribersTask[i];

                if(task.Status != TaskStatus.RanToCompletion) {
                    var exception = task.Exception;
                    var innerException = exception?.InnerExceptions;
                    string errorMessage = innerException?.FirstOrDefault()?.Message ?? string.Empty;

                    if(!string.IsNullOrEmpty(errorMessage)) {
                        Lines[i].IntegrationMessage.Add($"[GetSubscribers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. {errorMessage}");
                    }

                    continue;
                }

                var subscribers = task.Result;
                string subscriberId = subscribers != null ? subscribers.FirstOrDefault().subscriberId : string.Empty;
                Lines[i].SubscriberID = subscriberId;
                
                if(string.IsNullOrEmpty(subscriberId)) {
                    Lines[i].IntegrationMessage.Add($"[GetSubscribers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. Subscriber ID not found");
                }
            }

            #endregion

            #region getUsingOffers

            for(int i=0; i<Lines.Count; i++) {
                var line = Lines[i];
                getUsingOffersTask.Add(crmService.GetUsingOffersBySubscriberId(line.SubscriberID));
            }

            Task getUsingOffersTaskResult = null;
            try {
                getUsingOffersTaskResult = Task.WhenAll(getUsingOffersTask);
                await getUsingOffersTaskResult;
            } catch(Exception e) {}

            for(int i=0; i<getUsingOffersTask.Count; i++) {
                var task = getUsingOffersTask[i];

                if(task.Status != TaskStatus.RanToCompletion) {
                    var exception = task.Exception;
                    var innerException = exception?.InnerExceptions;
                    string errorMessage = innerException?.FirstOrDefault()?.Message ?? string.Empty;

                    if(!string.IsNullOrEmpty(errorMessage) && !string.IsNullOrEmpty(Lines[i].SubscriberID)) {
                        Lines[i].IntegrationMessage.Add($"[GetUsingOffers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. {errorMessage}");
                    }

                    continue;
                }

                var usingOffers = task.Result;
                Lines[i].UsingOffers = usingOffers;
                
                if(usingOffers == null || (usingOffers != null && usingOffers.Count == 0)) {
                    Lines[i].IntegrationMessage.Add($"[GetUsingOffers] No. {Lines[i].No} - {Lines[i].MSISDN} fail. Using offers not found");
                }
            }

            #endregion

            return Lines;
        }

        //getSIMCard
        public static async Task<List<LineDetail>> MNPIntegration(this List<LineDetail> Lines, UserConnection UserConnection)
        {
            var crmService = new CRMService(UserConnection, true, "MNP");
            var checkSimCardTask = new List<Task<ResultOfOperationValue>>();

            for(int i=0; i<Lines.Count; i++) {
                var line = Lines[i];
                if(line.IntegrationMessage == null) {
                    Lines[i].IntegrationMessage = new List<string>();
                }
                
                checkSimCardTask.Add(crmService.CheckSimCard(line.MSISDN, line.SIMCardSerialNumber));
            }

            Task checkSimCardTaskResult = null;
            try {
                checkSimCardTaskResult = Task.WhenAll(checkSimCardTask);
                await checkSimCardTaskResult;
            } catch(Exception e) {}

            for(int i=0; i<checkSimCardTask.Count; i++) {
                var task = checkSimCardTask[i];

                if(task.Status != TaskStatus.RanToCompletion) {
                    var exception = task.Exception;
                    var innerException = exception?.InnerExceptions;
                    string errorMessage = innerException?.FirstOrDefault()?.Message ?? string.Empty;

                    if(!string.IsNullOrEmpty(errorMessage)) {
                        Lines[i].IntegrationMessage.Add($"[CheckSimCard] No. {Lines[i].No} - {Lines[i].MSISDN} fail. {errorMessage}");
                    }

                    continue;
                }

                var checkSimCard = task.Result;
                if(checkSimCard == null) {
                    Lines[i].IntegrationMessage.Add($"[CheckSimCard] No. {Lines[i].No} - {Lines[i].MSISDN} fail.");
                }
            }

            return Lines;
        }
    }
}