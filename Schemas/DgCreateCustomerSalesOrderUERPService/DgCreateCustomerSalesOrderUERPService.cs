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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCreateCustomerSalesOrderUERPService
{
    public class CreateCustomerSalesOrderUERPService
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public string url { get; }
        public string endpoint { get; }
        private string username;
        private string password;
        private string section;
        private Guid recordId;
        private List<Guid> recordIds;

        protected string soNumber;
        private string timestamp;
		private int increment;

        private UERPRequest request;
        private UERPResponse response;
        private HttpResponseHeaders responseHeader;
        private string errorResponse;
        private ISAHttpRequest.ISAIntegrationLogService.IntegrationLog log;
		
        public CreateCustomerSalesOrderUERPService(UserConnection userConnection) 
        {
        	this.userConnection = userConnection;

            var setup = IntegrationSetup.Get(UserConnection, "CSG", "CreateCustomerSalesOrder");
            if(setup == null) {
                throw new Exception("CreateCustomerSalesOrder hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.username = setup.Authentication.Basic.Username;
			this.password = setup.Authentication.Basic.Password;
            this.timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();
        }

        public virtual async Task Request()
        {
            string res = string.Empty;

            var httpRequest = new HTTPRequest(this.url, UserConnection);
            if(!string.IsNullOrEmpty(this.section)) {
                httpRequest.SetLogSection(this.section);
            }

            if(this.recordId != null && this.recordId != Guid.Empty) {
                httpRequest.SetLogRecordId(this.recordId);
            }

            try {
                var req = await httpRequest
                    .SetLogName("UERP: Create Customer Sales Order")
                    .SetAuthBasic(username, password)
                .Post(this.endpoint, this.request);

                this.log = httpRequest.GetLog();
                this.responseHeader = req.Headers;

                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    if(!string.IsNullOrEmpty(req.Body)) {
                        throw new Exception(req.Body);
                    }

                    throw new Exception(req.Error ?? req.StatusCode);
                }

                if(string.IsNullOrEmpty(req.Body)) {
                    throw new Exception("Response is empty");
                }

                res = req.Body;
            } catch (Exception e) {
                this.errorResponse = e.Message;

                throw;
            }

            try {
                this.response = JsonConvert.DeserializeObject<UERPResponse>(res);             
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }

            // InsertLogUERPOrderTracking(log.Request.Body, log.Response.Body, param.UERPCreateCustomerSalesOrderRequest.OrigSysDocumentRef, this.recordId, status);
        }

        public virtual CreateCustomerSalesOrderUERPService SetParam(UERPRequest Param)
        {
            Validation(Param);

            this.request = Param;
            return this;
        }

        public virtual CreateCustomerSalesOrderUERPService SetParam(string Json)
        {
            try {
				return SetParam(JsonConvert.DeserializeObject<UERPRequest>(Json));
			} catch (Exception e) {
				throw new Exception($"Json is not valid: {e.Message}");
			}
        }

        public virtual CreateCustomerSalesOrderUERPService SetParamBySubmission(Guid RecordId)
        {
            if(RecordId == Guid.Empty) {
                throw new Exception("Record Id is empty");
            }
            
            this.section = "DgSubmission";
            this.recordId = RecordId;

            return SetParam(BuildRequestSubmission());
        }

        public virtual CreateCustomerSalesOrderUERPService SetParamByLineDetail(Guid RecordId)
        {
            if(RecordId == Guid.Empty) {
                throw new Exception("Record Id is empty");
            }

            this.section = "DgLineDetail";
            this.recordId = RecordId;

            return SetParam(BuildRequestLineDetail());
        }

        public virtual CreateCustomerSalesOrderUERPService SetParamByLineDetail(List<Guid> RecordIds)
        {
            if(RecordIds == null || (RecordIds != null && RecordIds.Count == 0)) {
                throw new Exception("Record Ids is empty");
            }

            if(RecordIds.Where(item => item == Guid.Empty).ToList().Count > 0) {
                throw new Exception("There is an empty record Id within the list");
            }

            this.section = "DgLineDetail";
            this.recordIds = RecordIds;

            return SetParam(BuildRequestLineDetail());
        }

        public virtual UERPRequest GetRequest()
        {
            return this.request ?? null;
        }

        public virtual string GetStringRequest()
        {
            return JsonConvert.SerializeObject(this.request, Formatting.Indented);
        }

        public virtual UERPResponse GetResponse()
        {
            return this.response ?? null;
        }

        public virtual string GetStringResponse()
        {
            return JsonConvert.SerializeObject(this.response);
        }

        public virtual bool IsSuccessResponse()
        {
            if(this.response == null || !string.IsNullOrEmpty(this.errorResponse) || this.responseHeader == null) {
                return false;
            }

            string status = HTTPRequest.GetHeader(this.responseHeader, "Status");
            if(status == "Fail") {
                return false;
            }

            return true;
        }

        public virtual string GetErrorResponse()
        {
            if(this.response == null || this.responseHeader == null) {
                return this.errorResponse ?? string.Empty;
            }

            string status = HTTPRequest.GetHeader(this.responseHeader, "Status");
            string errorCode = HTTPRequest.GetHeader(this.responseHeader, "ErrorCode");
            string errorDescription = HTTPRequest.GetHeader(this.responseHeader, "ErrorDescription");
            string moreInfo = HTTPRequest.GetHeader(this.responseHeader, "MoreInfo");

            if(status == "Fail") {
                throw new Exception($"{errorCode}: {errorDescription} - {moreInfo}");
            }

            return string.Empty;
        }

        public ISAHttpRequest.ISAIntegrationLogService.IntegrationLog GetLog()
		{
			return this.log ?? null;
		}
        
        public virtual string GetSONumber()
        {
            return this.soNumber;
        }

        protected virtual dynamic BuildQuery()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("LineDetailId", esq.AddColumn("Id"));
            columns.Add("No", esq.AddColumn("DgNo"));
            columns.Add("LineID", esq.AddColumn("DgLineId"));
            columns.Add("CustomerName", esq.AddColumn("DgSubmission.DgCRMGroup.DgName"));
            columns.Add("CustomerNumber", esq.AddColumn("DgSubmission.DgCRMGroup.DgBRN"));
            columns.Add("CustomerName_CI", esq.AddColumn("DgUsername"));
            columns.Add("LegalAddress", esq.AddColumn("DgSubmission.DgCRMGroup.DgLegalAddress"));
            columns.Add("BillingAddress", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillingAddress"));
            columns.Add("ShipAddress", esq.AddColumn("DgSubmission.DgCRMGroup.DgDeliveryaddress"));
            columns.Add("SalesChannelCode", esq.AddColumn("DgSubmission.DgSubscriberType.Name"));
            columns.Add("SubscriberTypeCode", esq.AddColumn("DgSubmission.DgSubscriberType.DgCode"));
            columns.Add("ShipCity", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationDelivery.Name"));
            columns.Add("ShipCountry", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationDelivery.Name"));
            columns.Add("ShipState", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoDelivery.Name"));
            columns.Add("ShipPostalCode", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationDelivery.Name"));
            columns.Add("BillingCity", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationBilling.DgCode"));
            columns.Add("BillingCountry", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationBilling.DgCode"));
            columns.Add("BillingState", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoBilling.DgCode"));
            columns.Add("BillingPostalCode", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationBilling.Name"));
            columns.Add("CustomerNamePhonetic", esq.AddColumn("DgSubmission.DgCRMGroup.DgGroupNo"));
            columns.Add("TINNumber", esq.AddColumn("DgSubmission.DgCRMGroup.DgTINNumber"));
			columns.Add("SST", esq.AddColumn("DgSubmission.DgCRMGroup.DgSST"));
			columns.Add("Email", esq.AddColumn("DgSubmission.DgCRMGroup.DgBillingEmailAddress"));
            columns.Add("sfaLeadsId", esq.AddColumn("DgSFALead"));
            columns.Add("OrderIMSIType", esq.AddColumn("DgOrderIMSIType.Name"));

            for (int i = 1; i <= 20; i++) {
                columns.Add($"SuppOffer{i}", esq.AddColumn($"DgSuppOffer{i}.DgOfferName"));
            }

            // var filterSoNumber = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", ""));
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSOID"));
            // esq.Filters.Add(filterSoNumber);

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsUERP", false));

            columns["No"].OrderByAsc(0);
            columns["LineID"].OrderByAsc(1);
            
            return new {
                esq = esq,
                columns = columns
            };
        }

        protected virtual UERPRequest BuildRequest(EntityCollection entities, Dictionary<string, EntitySchemaQueryColumn> columns)
        {
            var result = new UERPRequest();

            Entity firstItem = entities.FirstOrDefault();
            if(firstItem == null) {
                throw new Exception("No line detail selected or can't be processed");
            }
            
            string CustomerName = firstItem.GetTypedColumnValue<string>(columns["CustomerName"].Name);
            string CustomerNumber = firstItem.GetTypedColumnValue<string>(columns["CustomerNumber"].Name);
            string SalesChannelCode = firstItem.GetTypedColumnValue<string>(columns["SalesChannelCode"].Name);
            string SubscriberTypeCode = firstItem.GetTypedColumnValue<string>(columns["SubscriberTypeCode"].Name);
            string sfaLeadsId = firstItem.GetTypedColumnValue<string>(columns["sfaLeadsId"].Name);
            this.soNumber = EntityHelper.GetIncrementNumber(UserConnection, "DgSONumberCodeMask", "DgSOLastNumber");
			
			DateTime OrderedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
            result.UERPCreateCustomerSalesOrderRequest = new UERPCreateCustomerSalesOrderRequest() {
                CustomerName = CustomerName,
                PaymentTerms = "Cash",
                CustomerAttributes = "CORPORATE SALE",
                SalesChannelCode = SalesChannelCode,
                // Country = firstItem.GetTypedColumnValue<string>(columns["ShipCountry"].Name),
                Country = "MY",
                City = firstItem.GetTypedColumnValue<string>(columns["ShipCity"].Name),
                State = firstItem.GetTypedColumnValue<string>(columns["ShipState"].Name),
                PostalCode = firstItem.GetTypedColumnValue<string>(columns["ShipPostalCode"].Name),
                OrigSysDocumentRef = this.soNumber,
                Address1 = firstItem.GetTypedColumnValue<string>(columns["ShipAddress"].Name).Replace("\r\n", " ").Replace("\n", " "),
                CustomerNamePhonetic = firstItem.GetTypedColumnValue<string>(columns["CustomerNamePhonetic"].Name),
				TIN = firstItem.GetTypedColumnValue<string>(columns["TINNumber"].Name),
				Email = firstItem.GetTypedColumnValue<string>(columns["Email"].Name),
				SST = firstItem.GetTypedColumnValue<string>(columns["SST"].Name),
				BRN = firstItem.GetTypedColumnValue<string>(columns["CustomerNumber"].Name),
                OrderedDate = OrderedDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                OrderType = "Corp Channel (CEN)",
                SourceTransactionSystem = "NCCF",
                RequestedFulfillmentOrganizationCode = "3PL",
                CustomerNumber = CustomerNumber,
                CustomerProfileClassName = "Std Corporate",
                ShipToOrg = this.soNumber + "_S",
                InvoiceToOrg = this.soNumber + "_B",
                BillToCustomer = new List<BillToCustomer>() {
                    new BillToCustomer() {
                        CustomerName = CustomerName,
                        CustomerNumber = CustomerNumber,
                    }
                },
                ShipToCustomer = new List<ShipToCustomer>() {
                    new ShipToCustomer() {
                        CustomerName = CustomerName
                    }
                }
            };

            result.UERPCreateCustomerSalesOrderRequest.HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO = new List<HeaderEffBCustomer>() {
                new HeaderEffBCustomer() {
                    corporateIndividualCustomerName = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["CustomerName_CI"].Name) : null,
                    addressLine1 = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["ShipAddress"].Name) : null,
                    addressLine2 = null,
                    addressLine3 = null,
                    postCode = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["ShipPostalCode"].Name) : null,
                    city = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["ShipCity"].Name) : null,
                    state = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["ShipState"].Name) : null,
                    country = SubscriberTypeCode == "5" ? 
                        firstItem.GetTypedColumnValue<string>(columns["ShipCountry"].Name) : null,
                }
            };

            List<string> errorList = new List<string>();
			this.increment = 0;

            var salesOrderLine = new List<SalesOrderLine>();
            foreach (var entity in entities) {
                var promoCodeList = new List<string>();
                for (int i = 1; i <= 20; i++) {
                    promoCodeList.Add(entity.GetTypedColumnValue<string>(columns[$"SuppOffer{i}"].Name));
                }

                int no = entity.GetTypedColumnValue<int>(columns["No"].Name);
                Guid lineDetailId = entity.GetTypedColumnValue<Guid>(columns["LineDetailId"].Name);
                var salesOrders = GetSalesOrderLine(
                    lineDetailId,
                    SalesChannelCode,
                    promoCodeList,
                    sfaLeadsId
                );
                
				/*
				if(salesOrders == null || (salesOrders != null && salesOrders.Count == 0)) {
                    errorList.Add($"Line No {no} does not have Sales Order Line");
                }
				*/
				if(salesOrders != null && salesOrders.Count > 0) {
					salesOrderLine.AddRange(salesOrders);	
				}
				
				string orderImsiType = entity.GetTypedColumnValue<string>(columns["OrderIMSIType"].Name);
				if(orderImsiType == "3in1 USIM_Half Size") {
					string lineId = entity.GetTypedColumnValue<string>(columns["LineID"].Name);
					salesOrderLine.Add(GenerateSalesOrder(
						OrigSysLineRef: $"{lineId}_{this.increment}SIM",
						UnitListPrice: 0,
						UnitSellingPrice: 0,
						InventoryItem: "USI_200018342",
						OrderedDate: DateTime.UtcNow.ToString("yyyy-MM-dd"),
						SubscriberType: SalesChannelCode,
						PromoCode: "",
						ChargeToBill: "N"
					));
					this.increment++;
				}
            }

            if(salesOrderLine.Count == 0) {
                throw new Exception("Request does not have any Sales Order Line");
            }

            result.UERPCreateCustomerSalesOrderRequest.SalesOrderLine = salesOrderLine;
            
            return result;
        }

        protected virtual UERPRequest BuildRequestLineDetail()
        {
            var result = new UERPRequest();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            if(this.recordId != Guid.Empty && (this.recordIds == null || (this.recordIds != null && this.recordIds.Count == 0))) {

                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordId));
				esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToUERP", true));
            
            } else if(this.recordId == Guid.Empty && (this.recordIds != null && this.recordIds.Count > 0)) {
                
                var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
                foreach (var recordId in this.recordIds) {
                    filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", recordId));
                }

                esq.Filters.Add(filterGroup);

            }

            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual UERPRequest BuildRequestSubmission()
        {
            var result = new UERPRequest();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;
			
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.Id", this.recordId));
			esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToUERP", true));

            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }
        
        protected virtual List<SalesOrderLine> GetSalesOrderLine(Guid LineDetailId, string SubscriberType, List<string> PromoCode, string SfaLeadsId)
        {
            var result = new List<SalesOrderLine>(); 

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("FeeDetailId", esq.AddColumn("Id"));
            columns.Add("LineDetailId", esq.AddColumn("DgLineDetail.Id"));
            columns.Add("lineDetail_lineID", esq.AddColumn("DgLineDetail.DgLineId"));
            columns.Add("SuppOfferIndex", esq.AddColumn("DgSuppOfferIndex"));
            columns.Add("DgOFSCode", esq.AddColumn("DgOFSCode"));
			columns.Add("DgResModeID", esq.AddColumn("DgResModeID"));
            columns.Add("DgOriginalFeeAmount", esq.AddColumn("DgOriginalFeeAmount"));
            columns.Add("DgFeeAmount", esq.AddColumn("DgFeeAmount"));
            columns.Add("DgWaiveAmount", esq.AddColumn("DgWaiveAmount"));
            columns.Add("DgFeeType", esq.AddColumn("DgFeeType"));
            columns.Add("DgFeeItemCode", esq.AddColumn("DgFeeItemCode"));
            columns.Add("DgFeeName", esq.AddColumn("DgFeeName"));
            
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgPaymentType", "CHARGEACCOUNT"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", LineDetailId));
            
            string OrderedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            List<string> lineDetailList = new List<string>();

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities) {
                Guid feeDetailId = entity.GetTypedColumnValue<Guid>(columns["FeeDetailId"].Name);
                Guid lineDetailId = entity.GetTypedColumnValue<Guid>(columns["LineDetailId"].Name);
                string lineDetail_lineID = entity.GetTypedColumnValue<string>(columns["lineDetail_lineID"].Name);

                decimal DgOriginalFeeAmount = entity.GetTypedColumnValue<decimal>(columns["DgOriginalFeeAmount"].Name);
                decimal DgFeeAmount = entity.GetTypedColumnValue<decimal>(columns["DgFeeAmount"].Name);
                int SuppOfferIndex = entity.GetTypedColumnValue<int>(columns["SuppOfferIndex"].Name);
                string DgFeeType = entity.GetTypedColumnValue<string>(columns["DgFeeType"].Name);
                decimal DgWaiveAmount = entity.GetTypedColumnValue<decimal>(columns["DgWaiveAmount"].Name);
                string DgFeeItemCode = entity.GetTypedColumnValue<string>(columns["DgFeeItemCode"].Name);
                int feeItemCodeInt = !string.IsNullOrEmpty(DgFeeItemCode) ? Convert.ToInt32(DgFeeItemCode) : 0;
                string DgFeeName = entity.GetTypedColumnValue<string>(columns["DgFeeName"].Name);
				
				string DgOFSCode = entity.GetTypedColumnValue<string>(columns["DgOFSCode"].Name);
				string DgResModeID = entity.GetTypedColumnValue<string>(columns["DgResModeID"].Name);
				string InventoryItem = (DgOFSCode != DgResModeID && !string.IsNullOrEmpty(DgResModeID)) ? DgResModeID : DgOFSCode;
				
                var tax = GetTax(feeDetailId);
                int amount = tax != 0 ? Decimal.ToInt32(DgOriginalFeeAmount) : 0;
                int Waive = Convert.ToInt32(DgWaiveAmount);

                string OrigSysLineRef = $"{lineDetail_lineID}_{this.timestamp}";
                string chargeToBill = feeItemCodeInt >= 600000 && feeItemCodeInt <= 699999 ? "N" : "Y";
                string promoCode = PromoCode.Count == 0 ? "" : (SuppOfferIndex > 0 ? PromoCode[SuppOfferIndex-1] : "");

                result.Add(GenerateSalesOrder(
                    OrigSysLineRef: $"{OrigSysLineRef}_{this.increment}",
                    UnitListPrice: amount,
                    UnitSellingPrice: amount,
                    //InventoryItem: orderImsiType == "3in1 USIM_Half Size" ? "USI_200018342" : InventoryItem,
					InventoryItem: InventoryItem,
                    OrderedDate: OrderedDate,
                    SubscriberType: SubscriberType,
                    PromoCode: promoCode,
                    ChargeToBill: chargeToBill
                ));

                if(Waive > 0) {
                    result.Add(GenerateSalesOrder(
                        OrigSysLineRef: $"{OrigSysLineRef}_{this.increment}W",
                        UnitListPrice: 0,
                        UnitSellingPrice: Waive * -1,
                        InventoryItem: "MIS_200016652",
                        OrderedDate: OrderedDate,
                        SubscriberType: SubscriberType,
                        PromoCode: promoCode,
                        ChargeToBill: chargeToBill
                    ));
                }

                if(tax > 0) {
                    result.Add(GenerateSalesOrder(
                        OrigSysLineRef: $"{OrigSysLineRef}_{this.increment}T",
                        UnitListPrice: Convert.ToInt32(tax),
                        UnitSellingPrice: Convert.ToInt32(tax),
                        InventoryItem: "MIS_200016854",
                        OrderedDate: OrderedDate,
                        SubscriberType: SubscriberType,
                        PromoCode: promoCode,
                        ChargeToBill: chargeToBill
                    ));
                }

                if(tax <= 0) {
                    result.Add(GenerateSalesOrder(
                        OrigSysLineRef: $"{OrigSysLineRef}_{this.increment}NT",
                        UnitListPrice: 0,
                        UnitSellingPrice: 0,
                        InventoryItem: "MIS_200016854",
                        OrderedDate: OrderedDate,
                        SubscriberType: SubscriberType,
                        PromoCode: promoCode,
                        ChargeToBill: chargeToBill
                    ));
                }

                this.increment++;
            }

            var FulfillLineEffBSales = result
                .FirstOrDefault()?
                .additionalInformation?.FirstOrDefault()?
                .FulfillLineEffBSales_OrderLineLevelAdditional_InformationprivateVO;

            if(FulfillLineEffBSales != null) {
                FulfillLineEffBSales.Add(new FulfillLineEffBSales() {
                    sfaLeadsId = SfaLeadsId,
                    sfaOrderReservationId = string.Empty
                });
            }

            return result;
        }

        protected virtual decimal GetTax(Guid FeeDetailId)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgTaxListDetail");
            
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("DgTaxAmount", esq.AddColumn("DgTaxAmount"));
            
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgFeeDetail", FeeDetailId));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgTaxAmount", 0));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            return entity != null ? entity.GetTypedColumnValue<decimal>(columns["DgTaxAmount"].Name) : -1;
        }

        protected virtual SalesOrderLine GenerateSalesOrder(string OrigSysLineRef, int UnitListPrice, int UnitSellingPrice, string InventoryItem, string OrderedDate, string SubscriberType, string PromoCode, string ChargeToBill)
        {
            var salesOrder = new SalesOrderLine();

            salesOrder.OrigSysLineRef = OrigSysLineRef;
            salesOrder.OrderedQuantity = 1;
            salesOrder.UnitListPrice = UnitListPrice;
            salesOrder.UnitSellingPrice = UnitSellingPrice;
            salesOrder.InventoryItem = InventoryItem;
            salesOrder.OrderedDate = OrderedDate;
            salesOrder.manualPriceAdjustments = new List<manualPriceAdjustments>() {
                new manualPriceAdjustments() {
                    UnitSellingPrice = UnitSellingPrice
                }
            };
            salesOrder.additionalInformation = new List<additionalInformation>() {
                new additionalInformation() {
                    FulfillLineEffBSales_OrderLineLevelAdditional_InformationprivateVO = new List<FulfillLineEffBSales>(),
                    FulfillLineEffB3PPprivateVO = new List<FulfillLineEffB3PPprivateVO>() {
                        new FulfillLineEffB3PPprivateVO() {
                            SubscriberType = SubscriberType,
                            OfferType = "PostPaid",
                            PromoCode = PromoCode,
                            ChargeToBill = ChargeToBill,
                        }
                    }
                }
            };

            return salesOrder;
        }

        protected virtual void Validation(UERPRequest Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            if(Param.UERPCreateCustomerSalesOrderRequest == null) {
                throw new Exception("Param UERPCreateCustomerSalesOrderRequest cannot be null or empty");
            }

            var UERPCreateCustomerSalesOrderRequest = Param.UERPCreateCustomerSalesOrderRequest;
            if(UERPCreateCustomerSalesOrderRequest.BillToCustomer == null || (UERPCreateCustomerSalesOrderRequest.BillToCustomer != null && UERPCreateCustomerSalesOrderRequest.BillToCustomer.Count == 0)) {
                throw new Exception("Param BillToCustomer cannot be null or empty");
            }

            if(UERPCreateCustomerSalesOrderRequest.ShipToCustomer == null || (UERPCreateCustomerSalesOrderRequest.ShipToCustomer != null && UERPCreateCustomerSalesOrderRequest.ShipToCustomer.Count == 0)) {
                throw new Exception("Param ShipToCustomer cannot be null or empty");
            }

            if(UERPCreateCustomerSalesOrderRequest.HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO == null 
                || (UERPCreateCustomerSalesOrderRequest.HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO != null 
                    && UERPCreateCustomerSalesOrderRequest.HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO.Count == 0)) {
                throw new Exception("Param HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO cannot be null or empty");
            }

            if(UERPCreateCustomerSalesOrderRequest.SalesOrderLine == null || (UERPCreateCustomerSalesOrderRequest.SalesOrderLine != null && UERPCreateCustomerSalesOrderRequest.SalesOrderLine.Count == 0)) {
                throw new Exception("Param SalesOrderLine cannot be null or empty");
            }

            List<string> errorList = new List<string>();
            var check = new Dictionary<string, string>() {
                {"Customer Name", UERPCreateCustomerSalesOrderRequest.CustomerName},
                {"Sales Channel Code", UERPCreateCustomerSalesOrderRequest.SalesChannelCode},
                {"City", UERPCreateCustomerSalesOrderRequest.City},
                {"State", UERPCreateCustomerSalesOrderRequest.State},
                {"Postal Code", UERPCreateCustomerSalesOrderRequest.PostalCode},
                {"OrigSysDocumentRef", UERPCreateCustomerSalesOrderRequest.OrigSysDocumentRef},
                {"Address 1", UERPCreateCustomerSalesOrderRequest.Address1},
                {"Customer Name Phonetic", UERPCreateCustomerSalesOrderRequest.CustomerNamePhonetic},
                {"Ordered Date", UERPCreateCustomerSalesOrderRequest.OrderedDate},
                {"Customer Number", UERPCreateCustomerSalesOrderRequest.CustomerNumber},
            };
            foreach (var item in check) {
                if(string.IsNullOrEmpty(item.Value)) {
                    errorList.Add($"{item.Key} cannot be null or empty");
                }
            }

            foreach (var item in UERPCreateCustomerSalesOrderRequest.SalesOrderLine) {
                var checkSalesOrderLine = new Dictionary<string, string>() {
                    {"OrigSysLineRef", item.OrigSysLineRef},
                    {"Inventory Item", item.InventoryItem},
                    {"Ordered Date", item.OrderedDate},
                };

                foreach (var checkSales in checkSalesOrderLine) {
                    if(string.IsNullOrEmpty(checkSales.Value)) {
                        errorList.Add($"{checkSales.Key} cannot be null or empty");
                    }
                }
            }

            if(errorList.Count > 0) {
                string error = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception(JsonConvert.SerializeObject(new List<string>() {
					$"<ul>{error}</ul>"
				}));
            }
        }

        public static void InsertLog(UserConnection UserConnection, ISAHttpRequest.ISAIntegrationLogService.IntegrationLog Log, string SONumber, string Status) 
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = string.Format("{0}_createCustomerSalesOrderUERP_{1}.txt", currentDateTimeMY, SONumber);
            var log = string.Format("{0}{1}Json Request: {2}{3}{4}{5}Json Response: {6}{7}", 
                currentDateTimeMY, 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                Log.Request?.Body ?? string.Empty, 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                Log.Response?.Body ?? string.Empty
            );

            EntityHelper.CreateEntity(
                UserConnection, 
                section: "DgUERPOrderTracking", 
                values: new Dictionary<string, object>() {
                    {"DgAPIName", "createCustomerSalesOrderUERP"},
                    {"DgFileName", fileName},
                    {"DgLogFile", log},
                    {"DgOriginalSysDocumentRef", SONumber},
                    {"DgStatus", Status},
                    {"CreatedById", UserConnection.CurrentUser.ContactId}
                }
            );
        }
    }
}