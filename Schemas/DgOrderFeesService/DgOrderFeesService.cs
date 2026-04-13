using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService;
using DgBaseService.DgHelpers;
using DgCRMIntegration;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using LookupConst = DgMasterData.DgLookupConst;
using SolarisCore;

namespace DgCSGIntegration.DgOrderFees
{
    public class OrderFees
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;
        public string Username;
        public string Password;
        private CRMService crmService;

        public OrderFees(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;
            this.Username = setup.Authentication.Basic.Username;
            this.Password = setup.Authentication.Basic.Password;

            this.crmService = new CRMService(UserConnection);
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "OrderFees");
            if(setup == null) {
                throw new Exception("OrderFees hasn't been set up for integration");
            }

            return setup;
        }

        public List<OrderFeesRequestV2> GetParamByLineDetail(Guid RecordId)
        {
            return GetRequestForDeviceOrder(new List<Guid>() { RecordId });
        }

        public List<OrderFeesRequestV2> GetParamByLineDetail(List<Guid> RecordIds)
        {
            return GetRequestForDeviceOrder(RecordIds);
        }

        public OrderFeesRequest GetParamByCRMGroup(Guid RecordId)
        {
            return GetRequestForCRMGroup(RecordId);
        }

        public async Task<List<OrderFeesRequestV2>> GetParamByLineDetailCOP(Guid RecordId)
        {
            var req = GetRequestForDeviceOrder(new List<Guid>() { RecordId }).FirstOrDefault();
            var item = new OrderFeesRequestV2();

            try {
                item.LineDetail = req.LineDetail;

                var cop = await GetParamCOP(req);
                item.Request = cop;
            } catch (Exception e) {
                item.Message = e.Message;
            }

            return new List<OrderFeesRequestV2>() { item };
        }

        public async Task<List<OrderFeesRequestV2>> GetParamByLineDetailCOP(List<Guid> RecordIds)
        {
            var param = new List<OrderFeesRequestV2>();
            var req = GetRequestForDeviceOrder(RecordIds);
            for (int i = 0; i < req.Count; i++) {
                var item = new OrderFeesRequestV2();
                try {
                    item.LineDetail = req[i].LineDetail;

                    var cop = await GetParamCOP(req[i]);
                    item.Request = cop;
                } catch (Exception e) {
                    item.Message = e.Message;
                }

                param.Add(item);
            }

            return param;
        }

        protected virtual async Task<OrderFeesRequest> GetParamCOP(OrderFeesRequestV2 Param)
        {
            // get subscribers
            var subsribers = await this.crmService.GetSubscribersByMSISDN(Param.LineDetail.MSISDN);
            if(subsribers == null) {
                throw new Exception("Subscriber not found");
            }

            if(string.IsNullOrEmpty(subsribers.FirstOrDefault()?.subscriberId)) {
                throw new Exception("Subscriber Id is empty");
            }

            // get using offer
            var usingOffers = await this.crmService.GetUsingOffersByMSISDN(Param.LineDetail.MSISDN);
            if(usingOffers == null || (usingOffers != null && usingOffers.Count < 1)) {
                throw new Exception("Using Offers not found");
            }

            string retentionOffer = SysSettings.GetValue<string>(UserConnection, "DgRetentionOffer", string.Empty);
            string[] retentionOffers = retentionOffer.Split('|');
            List<string> lineOfferList = Param.LineDetail.SuppOfferList.Select(item => item.OfferID).ToList();
            List<string> additionalOptionalOfferList = new List<string>();
            List<string> unsubscribeOptionalOfferList = new List<string>();

            foreach(string retention in retentionOffers) {
                var currentOffer = usingOffers.FirstOrDefault(offer => offer.offerId == retention);
                string submittedOffer = lineOfferList.Find(item => item == retention);
                if (currentOffer != null && !string.IsNullOrEmpty(currentOffer.offerId) && string.IsNullOrEmpty(submittedOffer)) {
                    lineOfferList.Add(retentionOffer);
                }
            }

            bool isChangePrimaryOffer = false;
            string newPrimaryOfferID = string.Empty;
            string usingOfferPrimary = usingOffers
                .Where(item => item.offerType == "1")
                .Select(item => item.offerId)
                .FirstOrDefault();
            if(Param.LineDetail.PrimaryOfferID != usingOfferPrimary) {
                newPrimaryOfferID = Param.LineDetail.PrimaryOfferID;
            }

            var usingOfferSupps = usingOffers.Where(item => item.offerType == "0").ToList();
            List<string> currSupplementaryOfferList = new List<string>();
            var bundleOfferList = GetBundleOffering(usingOfferSupps.Select(item => item.offerId).ToList());

            foreach(var usingOfferSupp in usingOfferSupps) {
                currSupplementaryOfferList.Add(usingOfferSupp.offerId);
                string matchSuppId = lineOfferList.Find(item => item == usingOfferSupp.offerId);
                if(string.IsNullOrEmpty(matchSuppId)) {
                    if(usingOfferSupp.isPackage.ToUpper().Contains("Y")) {
                        unsubscribeOptionalOfferList.Add(usingOfferSupp.offerId);

                        var bundleOfferingDetailsList = bundleOfferList.FindAll(item => item["OfferID"] == usingOfferSupp.offerId);
                        foreach(var bundleOffer in bundleOfferingDetailsList) {
                            var submittedElementOffer = usingOffers.FirstOrDefault(item => item.offerId == bundleOffer["ElementID"]);
                            if(submittedElementOffer == null) {
                                continue;
                            }

                            unsubscribeOptionalOfferList.Add(submittedElementOffer.offerId);
                        }
                    } else {
                        unsubscribeOptionalOfferList.Add(usingOfferSupp.offerId);
                    }
                }
            }

            var offeringMasterList = GetOffering(lineOfferList);
            var submittedBundleOfferList = new List<Dictionary<string, string>>();

            foreach(string lineSuppOfferId in lineOfferList) {
                var suppOffer = offeringMasterList.Find(delegate (Dictionary<string, object> offer) {
                    return offer["OfferID"].ToString() == lineSuppOfferId && (Guid)offer["OfferType"] == LookupConst.OfferType.SupplementaryOffering;
                });

                if(suppOffer == null) {
                    throw new Exception("Submitted supplementary offer cannot be found in Offering Master Table.");
                }

                if((bool)suppOffer["BundleFlag"]) {
                    var submittedBundleIdList = bundleOfferList.FindAll(delegate (Dictionary<string, string> offer) {
                        return offer["OfferID"] == suppOffer["OfferID"].ToString();
                    });

                    foreach (var bundleOffer in submittedBundleIdList) {
                        string matchElementId = lineOfferList.Find(delegate (string id) {
                            return id == bundleOffer["ElementID"];
                        });

                        if (!string.IsNullOrEmpty(matchElementId)) {
                            submittedBundleOfferList.Add(bundleOffer);
                        }
                    }

                    foreach(var submittedBundleOffer in submittedBundleOfferList) {
                        string matchElementId = currSupplementaryOfferList.Find(delegate (string id) {
                            return id == submittedBundleOffer["ElementID"];
                        });

                        if (string.IsNullOrEmpty(matchElementId)) {
                            var tempBundleOfferValue = additionalOptionalOfferList.Find(delegate (string order) {
                                return order == submittedBundleOffer["OfferID"];
                            });

                            if (tempBundleOfferValue == null) {
                                additionalOptionalOfferList.Add(submittedBundleOffer["OfferID"]);
                            }

                            additionalOptionalOfferList.Add(submittedBundleOffer["ElementID"]);
                        }
                    }
                } else if(!(bool)suppOffer["BundleFlag"]) {
                    var bundleElement = bundleOfferList.Find(delegate (Dictionary<string, string> offer) {
                        return offer["ElementID"] == suppOffer["OfferID"].ToString();
                    });

                    if (bundleElement != null) {
                        var tempBundleOfferValue = additionalOptionalOfferList.Find(delegate (string order) {
                            return order == suppOffer["OfferID"].ToString();
                        });

                        if (tempBundleOfferValue != null) {
                            continue;
                        }
                    }

                    string matchSuppId = currSupplementaryOfferList.Find(delegate (string id) {
                        return id == lineSuppOfferId;
                    });

                    if (string.IsNullOrEmpty(matchSuppId)) {
                        additionalOptionalOfferList.Add(lineSuppOfferId);
                    }
                }
            }

            if(!string.IsNullOrEmpty(newPrimaryOfferID)) {
                lineOfferList.Add(newPrimaryOfferID);
                isChangePrimaryOffer = true;
            }
            
            if(isChangePrimaryOffer) {
                if(Param.LineDetail.PrimaryOfferID == "78860") {
                    var matchSuppIds = new List<string>() {
                        "78829", "14026", "14027", "14040", "13922", 
                        "14028", "14029", "14030", "14032", "14033",
                        "14039", "82770"
                    };

                    foreach(string matchSupp in matchSuppIds) {
                        string match = lineOfferList.Find(delegate (string id) {
                            return id == matchSupp;
                        });

                        if (match == matchSupp) {
                            additionalOptionalOfferList.Add(matchSupp);
                        }
                    }                                    
                }
            }

            Param.Request.RetrieveFeesForOrderRequest.OrderType = isChangePrimaryOffer ? LookupConst.OrderType.COP_Primary : LookupConst.OrderType.COP_Supp;
            Param.Request.RetrieveFeesForOrderRequest.SubscribedOffersList = new SubscribedOffersList() {
                OfferId = lineOfferList 
            };
            Param.Request.RetrieveFeesForOrderRequest.UnsubscribedOffersList = new UnsubscribedOffersList() {
                OfferId = unsubscribeOptionalOfferList
            };

            return Param.Request;
        }

        public virtual bool IsSuccess(List<KeyValue> Headers)
        {
            if(Headers == null || (Headers != null && Headers.Count == 0)) {
                return false;
            }

            string status = Headers.FirstOrDefault(item => item.Name == "Status")?.Value;
			string errorCode = Headers.FirstOrDefault(item => item.Name == "ErrorCode")?.Value;
            string errorDesc = Headers.FirstOrDefault(item => item.Name == "ErrorDescription")?.Value;
			
			// pengecualian error - No records returned from Host System
			if(errorCode == "OM:60013" || errorDesc == "No records returned from Host System" || errorDesc == "No records returned by CRM") {
				return true;
			}
			
            if(status == "Fail") {
                return false;
            }

            return status == "Successful" ? true : false;
        }

        public virtual string GetErrorResponse(List<KeyValue> Headers)
        {
            if(Headers == null || (Headers != null && Headers.Count == 0)) {
                return string.Empty;
            }

            string status = Headers.FirstOrDefault(item => item.Name == "Status")?.Value;
            if(status != "Fail") {
                return string.Empty;
            }

            string errorCode = Headers.FirstOrDefault(item => item.Name == "ErrorCode")?.Value;
            string errorDescription = Headers.FirstOrDefault(item => item.Name == "ErrorDescription")?.Value;
            string moreInfo = Headers.FirstOrDefault(item => item.Name == "MoreInfo")?.Value;

            return $"{errorCode}: {errorDescription} - {moreInfo}";
        }

        protected virtual OrderFeesRequest GetRequestForCRMGroup(Guid RecordId) 
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgCRMGroup");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("CustomerId", esq.AddColumn("DgParentCustomerId"));
			columns.Add("PrimaryOffer", esq.AddColumn("DgPrimaryOffer.DgCode"));
            for (int i = 1; i <= 6; i++) {
                columns.Add($"SuppOfferId{i}", esq.AddColumn($"DgSuppOffer{i}.DgCode"));
            }

            var entity = esq.GetEntity(UserConnection, RecordId);
            if(entity == null) {
                throw new Exception("No data can be process to Order Fees");
            }

            var listOffer = new List<string>();
            var primaryOffer = entity.GetTypedColumnValue<string>(columns["PrimaryOffer"].Name);
            if(!string.IsNullOrEmpty(primaryOffer)) {
                listOffer.Add(primaryOffer);
            }

            for (int i = 1; i <= 6; i++) {
                var suppOffer = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}"].Name);
                if(!string.IsNullOrEmpty(suppOffer)) {
                    listOffer.Add(suppOffer);
                }
            }

            string customerId = entity.GetTypedColumnValue<string>(columns["CustomerId"].Name);
            if(string.IsNullOrEmpty(customerId)) {
                throw new Exception("Customer Id cannot be null or empty");
            }

          	return new OrderFeesRequest() {
                RetrieveFeesForOrderRequest = new RetrieveFeesForOrderRequest() {
                    OrderType = "22",
                    SubscribedOffersList = new SubscribedOffersList() {
                        OfferId = listOffer 
                    },
                    CustomerId = customerId
                }
            };
        }

        protected virtual List<OrderFeesRequestV2> GetRequestForDeviceOrder(List<Guid> LineDetailIds) 
        {
            if(LineDetailIds == null || (LineDetailIds != null && LineDetailIds.Count == 0)) {
                throw new Exception("No data can be process to Order Fees");
            }

            var result = new List<OrderFeesRequestV2>();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("No", esq.AddColumn("DgNo"));
			columns.Add("LineId", esq.AddColumn("DgLineId"));
            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("DeviceIMEI", esq.AddColumn("DgDeviceIMEI"));
			//columns.Add("SimCardNumber", esq.AddColumn("DgSIMCardNumber"));
            columns.Add("DealerCode", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerID"));
            columns.Add("SubscriberType", esq.AddColumn("DgSubmission.DgSubscriberType.DgCode"));
            columns.Add("SubmissionType", esq.AddColumn("DgSubmission.DgSubmissionType.DgCode"));

            columns.Add("PrimaryOffer", esq.AddColumn("DgPrimaryOffering.DgOfferID"));
            columns.Add("PrimaryOfferName", esq.AddColumn("DgPrimaryOffering.DgOfferName"));
            columns.Add("PrimaryOffer_OfferTypePosition", esq.AddColumn("DgPrimaryOffering.DgOfferType.DgPosition"));

            columns["LineId"].OrderByAsc(0);
            columns["No"].OrderByAsc(1);
            
            var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgPrimaryOffering"));

            for (int i = 1; i <= 20; i++) {
                columns.Add($"SuppOfferId{i}", esq.AddColumn($"DgSuppOffer{i}.DgOfferID"));
                columns.Add($"SuppOfferId{i}Name", esq.AddColumn($"DgSuppOffer{i}.DgOfferName"));
                columns.Add($"SuppOfferId{i}_OfferTypePosition", esq.AddColumn($"DgSuppOffer{i}.DgOfferType.DgPosition"));
				columns.Add($"SuppOfferId{i}ContractId", esq.AddColumn($"DgSuppOffer{i}.DgContractId"));
				columns.Add($"SuppOfferId{i}OracleItemCode", esq.AddColumn($"DgSuppOffer{i}.DgOracleItemCode"));
				columns.Add($"SuppOfferId{i}OraclePackageCode", esq.AddColumn($"DgSuppOffer{i}.DgOraclePackageCode"));

                filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, $"DgSuppOffer{i}"));
            }

            var filterIds = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach (var id in LineDetailIds) {
                filterIds.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", id));
            }

            esq.Filters.Add(filterIds);
            esq.Filters.Add(filterGroup);

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities) {
                var req = new OrderFeesRequestV2();

                Guid lineDetailId = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);

                string msisdn = Helper.GetValidMSISDN(entity.GetTypedColumnValue<string>(columns["MSISDN"].Name));
                if(string.IsNullOrEmpty(msisdn)) {
                    throw new Exception("MSISDN cannot be null or empty.");
                }

                string submissionType = entity.GetTypedColumnValue<string>(columns["SubmissionType"].Name);
                if(submissionType != "NEW" && submissionType != "MNP" && submissionType != "COP") {
                    throw new Exception("Submission Type not found");
                }

                string deviceImei = entity.GetTypedColumnValue<string>(columns["DeviceIMEI"].Name);
                //string simCardNo = "entity.GetTypedColumnValue<string>(columns["SimCardNumber"].Name);"
                // string simCardNo = "89601614052961197991";
                string simCardNo = (string)SysSettings.GetValue(UserConnection, "DgOrderFeesDummySimCardNo");
				
				int no = entity.GetTypedColumnValue<int>(columns["No"].Name);
                int lineId = entity.GetTypedColumnValue<int>(columns["LineId"].Name);

                var contractList = new List<ContractList>();
                var listOffer = new List<Dictionary<string, string>>();
                var resourceList = new ResourceList() {
                    ResourceRecord = new List<ResourceRecord>()
                };

                var primaryOfferName = entity.GetTypedColumnValue<string>(columns["PrimaryOfferName"].Name);
                var primaryOffer = entity.GetTypedColumnValue<string>(columns["PrimaryOffer"].Name);
                int primaryOfferPosition = entity.GetTypedColumnValue<int>(columns["PrimaryOffer_OfferTypePosition"].Name);
                
                if(string.IsNullOrEmpty(primaryOfferName)) {
                    throw new Exception("Primary Offer ID cannot be null or empty.");
                }
                
                var lineDetailOrderFees = new LineDetailSelected() {
                    Id = lineDetailId,
                    No = no,
                    LineId = lineId,
                    MSISDN = msisdn,
                    PrimaryOfferID = primaryOffer,
                    SuppOfferList = new List<SuppOfferOrderFees>()
                };

                listOffer.Add(new Dictionary<string, string>() {
                    {"OfferID", primaryOffer},
                    {"OfferName", primaryOfferName},
                    {"OfferType", primaryOfferPosition.ToString()}
                });

                for (int i = 1; i <= 20; i++) {
                    var suppOfferName = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}Name"].Name);
                    var suppOffer = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}"].Name);
                    int suppOfferPosition = entity.GetTypedColumnValue<int>(columns[$"SuppOfferId{i}_OfferTypePosition"].Name);
                    string contractId = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}ContractId"].Name);
                    string oracleItemCode = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}OracleItemCode"].Name);
                    string oraclePackageCode = entity.GetTypedColumnValue<string>(columns[$"SuppOfferId{i}OraclePackageCode"].Name);
                    
                    if(!string.IsNullOrEmpty(suppOffer)) {
                        listOffer.Add(new Dictionary<string, string>(){
                            {"OfferID", suppOffer},
                            {"OfferName", suppOfferName},
                            {"OfferType", suppOfferPosition.ToString()}
                        });
                        lineDetailOrderFees.SuppOfferList.Add(new SuppOfferOrderFees() {
                            Index = i,
                            OfferID = suppOffer,
                            OfferName = suppOfferName,
                            IsContractElement = !string.IsNullOrEmpty(contractId) 
                                && (!string.IsNullOrEmpty(oracleItemCode) && oracleItemCode != "NA")
                                && (!string.IsNullOrEmpty(oraclePackageCode) && oraclePackageCode != "NA")
                        });
                    }
                }

                if(listOffer.Count < 1) {
                    throw new Exception("Supplementary Offer ID cannot be null or empty.");
                }

                string orderType = string.Empty;
                switch(submissionType) {
                    case "NEW":
                        orderType = LookupConst.OrderType.NEW;
                        break;
                    case "MNP":
                        orderType = LookupConst.OrderType.MNP;
                        break;
                }

                req.Request = new OrderFeesRequest();
                req.Request.RetrieveFeesForOrderRequest = new RetrieveFeesForOrderRequest() {
                    OrderType = orderType,
                    MSISDN = msisdn,
                    SubscribedOffersList = new SubscribedOffersList() {
                        OfferId = listOffer.Select(item => item["OfferID"]).ToList() 
                    },
                    CustomerNationality = "123",
                    DealerCode = entity.GetTypedColumnValue<string>(columns["DealerCode"].Name),
                    //ICCID = deviceImei,
                    ICCID = simCardNo,
                    NewMSISDN = msisdn,
                    PayType = "POSTPAID",
                    PayTypeSpecified = true,
                    SubscriberType = entity.GetTypedColumnValue<string>(columns["SubscriberType"].Name)
                };

                var offeringRSRCList = GetOfferingRSRC(listOffer.Select(item => new KeyValue() {
                    Name = item["OfferID"],
                    Value = item["OfferName"]
                }).ToList());
                foreach (Dictionary<string, string> item in listOffer) {
                    string offerType = item["OfferType"] == "1" ? "PRIMARYOFFERCONTRACT" : "SUPPLEMENTARYOFFERCONTRACT";
                    string offerID = item["OfferID"];
                    string offerName = item["OfferName"];

                    var offeringRSRC = offeringRSRCList.Where(el => el["OfferID"] == offerID && el["ContractName"] == offerName).FirstOrDefault();
                    if(offeringRSRC != null) {
                        contractList.Add(new ContractList() {
                            ContractDuration = offeringRSRC["ContractDuration"],
                            ContractId = offeringRSRC["ContractId"],
                            ContractType = offerType,
                            RelatedOfferId = offeringRSRC["ContractId"],
                        });

                        if(!string.IsNullOrEmpty(offeringRSRC["ProductId"])) {
                            resourceList.ResourceRecord.Add(new ResourceRecord() {
                                IMEI = deviceImei,
                                OfferId = offerID,
                                ProductId = offeringRSRC["ProductId"],
                            });
                        }
                    }
                }

                req.Request.RetrieveFeesForOrderRequest.ContractList = contractList;
                req.Request.RetrieveFeesForOrderRequest.ResourceList = resourceList;
                req.LineDetail = lineDetailOrderFees;
                
                result.Add(req);
            }

            return result;
        }

        protected virtual List<Dictionary<string, string>> GetOfferingRSRC(List<KeyValue> Offers)
        {
            if(Offers == null || (Offers != null && Offers.Count == 0)) {
                return null;
            }

            var result = new List<Dictionary<string, string>>();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOfferingRSRC");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("OfferID", esq.AddColumn("DgOfferID"));
            columns.Add("ContractName", esq.AddColumn("DgContractName"));
            columns.Add("ContractDuration", esq.AddColumn("DgContractTenure"));
            columns.Add("ContractId", esq.AddColumn("DgContractID"));
            columns.Add("ProductId", esq.AddColumn("DgProductID"));
            
            var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach (var item in Offers) {
                var filterOffer = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
                filterOffer.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgOfferID", item.Name));
                filterOffer.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgContractName", item.Value));

                filterGroup.Add(filterOffer);
            }

            esq.Filters.Add(filterGroup);

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities) {
                var data = new Dictionary<string, string>();
                
                data.Add("OfferID", entity.GetTypedColumnValue<string>(columns["OfferID"].Name));
                data.Add("ContractName", entity.GetTypedColumnValue<string>(columns["ContractName"].Name));
                data.Add("ContractDuration", entity.GetTypedColumnValue<int>(columns["ContractDuration"].Name).ToString());
                data.Add("ContractId", entity.GetTypedColumnValue<string>(columns["ContractId"].Name));
                data.Add("ProductId", entity.GetTypedColumnValue<string>(columns["ProductId"].Name));

                result.Add(data);
            }

            return result;
        }

        protected List<Dictionary<string, string>> GetBundleOffering(List<string> OfferIdList)
        {
            var result = new List<Dictionary<string, string>>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOfferingBundle");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
			columns.Add("OfferID", esq.AddColumn("DgOfferID"));
            columns.Add("ElementID", esq.AddColumn("DgElementID"));

            var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach(var offerId in OfferIdList) {
                filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgOfferID", offerId));
            }

            esq.Filters.Add(filterGroup);

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
				string offerId = entity.GetTypedColumnValue<string>(columns["OfferID"].Name);
                string elementId = entity.GetTypedColumnValue<string>(columns["ElementID"].Name);

				result.Add(new Dictionary<string, string>() {
                    {"OfferID", offerId},
                    {"ElementID", elementId}
                });
			}

            return result;
        }

        protected List<Dictionary<string, object>> GetOffering(List<string> OfferIdList)
        {
            var result = new List<Dictionary<string, object>>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOffering");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
			columns.Add("OfferID", esq.AddColumn("DgOfferID"));
            columns.Add("OfferType", esq.AddColumn("DgOfferType.Id"));
            columns.Add("BundleFlag", esq.AddColumn("DgBundleFlag"));

            var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach(var offerId in OfferIdList) {
                filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgOfferID", offerId));
            }

            esq.Filters.Add(filterGroup);

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
				string offerId = entity.GetTypedColumnValue<string>(columns["OfferID"].Name);
                Guid offerType = entity.GetTypedColumnValue<Guid>(columns["OfferType"].Name);
                bool bundleFlag = entity.GetTypedColumnValue<bool>(columns["BundleFlag"].Name);

				result.Add(new Dictionary<string, object>() {
                    {"OfferID", offerId},
                    {"OfferType", offerType},
                    {"BundleFlag", bundleFlag}
                });
			}

            return result;
        }
    }

    public class OrderFeesRequestV2
    {
        public OrderFeesRequest Request { get; set; }
        public LineDetailSelected LineDetail { get; set; }
        public string Message { get; set; }
    }

    public class LineDetailSelected
    {
        public Guid Id { get; set; }
        public int No { get; set; }
		public int LineId { get; set; }
        public string MSISDN { get; set; }
        public string PrimaryOfferID { get; set; }
        public List<SuppOfferOrderFees> SuppOfferList { get; set; }
    }
    

    public class SuppOfferOrderFees
    {
        public int Index { get; set; }
        public string OfferID { get; set; }
		public string OfferName { get; set; }
		public bool IsContractElement { get; set; }
    }
}