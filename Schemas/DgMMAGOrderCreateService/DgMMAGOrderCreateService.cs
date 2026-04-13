using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Configuration;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService.DgHelpers;
using ISAHttpRequest.ISAHttpRequest;
using ISAIntegrationSetup;
using ISAEntityHelper.EntityHelper;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using Request = DgIntegration.DgMMAGOrderCreateService.Request;
using Response = DgIntegration.DgMMAGOrderCreateService.Response;

namespace DgIntegration.DgMMAGOrderCreateService
{
    public class MMAGOrderCreateService
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

        public string baseUrl { get; }
        public string endpoint { get; }
        private string soapAction;
        private string username;
        private string password;
        private string section;
        private Guid recordId;
        private List<Guid> recordIds;
        private string IMSI_ItemCode;
        private string IMSI_SIMType;

        private Request.Envelope request;
        private Response.Envelope response;
        private string errorResponse;
		private ISAHttpRequest.ISAIntegrationLogService.IntegrationLog log;

        public MMAGOrderCreateService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;

            var setup = IntegrationSetup.Get(UserConnection, "MMAG", "MMAGOrderCreate");
            if(setup == null) {
                throw new Exception("MMAGOrderCreate hasn't been set up for integration");
            }
            
            this.baseUrl = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
            this.username = setup.Authentication.Basic.Username;
			this.password = setup.Authentication.Basic.Password;
            this.soapAction = IntegrationSetup.GetCustomAuthValue(UserConnection, "MMAG", "MMAGOrderCreate", "SOAPAction", "");
            this.IMSI_ItemCode = IntegrationSetup.GetCustomAuthValue(UserConnection, "MMAG", "IMSI_ItemCode", "");
            this.IMSI_SIMType = IntegrationSetup.GetCustomAuthValue(UserConnection, "MMAG", "IMSI_SIMType", "");
        }

        public virtual async Task Request()
        {
            string res = string.Empty;
            string soNumber = string.Empty;

            var httpRequest = new HTTPRequest(this.baseUrl, UserConnection);
            if(!string.IsNullOrEmpty(this.section)) {
                httpRequest.SetLogSection(this.section);
            }

            if(this.recordId != null && this.recordId != Guid.Empty) {
                httpRequest.SetLogRecordId(this.recordId);
            }

            try {
                soNumber = this.request.Body.OrderCreate.Order.OrderID;				
                var req = await httpRequest
                    .SetLogName("MMAG Order Create")
                    .AddHeader("SOAPAction", this.soapAction)
                    .AddHeader("Username", this.username)
                    .AddHeader("Password", this.password)
                    .AddHeader("ReferenceID", Helper.GenerateReferenceId())
                .Post(this.endpoint, HTTPRequest.XmlToString<Request.Envelope>(this.request), ContentType.Xml);
				
				this.log = httpRequest.GetLog();
				
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
            } catch(Exception e) {
                this.errorResponse = $"SO Number {soNumber} fail. {e.Message}";

                return;
            }

            try {
                this.response = HTTPRequest.XmlToObject<Response.Envelope>(res);
            } catch (Exception e) {
                string message = !string.IsNullOrEmpty(res) ? res : e.Message;
                this.errorResponse = $"SO Number {soNumber} fail. {message}";
            }
        }

        public virtual MMAGOrderCreateService SetParam(Request.Envelope Param)
        {
            Validation(Param);

            this.request = Param;
            return this;
        }

        public virtual MMAGOrderCreateService SetParam(string Xml)
        {
            try {
				return SetParam(HTTPRequest.XmlToObject<Request.Envelope>(Xml));	
			} catch (Exception e) {
				throw new Exception($"Xml is not valid: {e.Message}");
			}
        }

        public virtual MMAGOrderCreateService SetParamByLineDetail(Guid RecordId)
        {
            if(RecordId == Guid.Empty) {
                throw new Exception("Record Id is empty");
            }

            this.section = "DgLineDetail";
            this.recordId = RecordId;

            return SetParam(BuildRequestLineDetail());
        }

        public virtual MMAGOrderCreateService SetParamByLineDetail(List<Guid> RecordIds)
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

        public virtual MMAGOrderCreateService SetParamBySubmission(Guid RecordId)
        {
            if(RecordId == Guid.Empty) {
                throw new Exception("Record Id is empty");
            }

            this.section = "DgSubmission";
            this.recordId = RecordId;

            return SetParam(BuildRequestSubmission());
        }

        public virtual MMAGOrderCreateService SetParamBySONumber(string SoNumber)
        {
            if(string.IsNullOrEmpty(SoNumber)) {
                throw new Exception("SO Number is empty");
            }

            return SetParam(BuildRequestSONumber(SoNumber));
        }

        public virtual Request.Envelope GetRequest()
        {
            return this.request;
        }

        public virtual string GetStringRequest()
        {
            return HTTPRequest.XmlToString<Request.Envelope>(this.request);
        }

        public virtual Response.Envelope GetResponse()
        {
            return this.response;
        }

        public virtual string GetStringResponse()
        {
            return HTTPRequest.XmlToString<Response.Envelope>(this.response);
        }

        public virtual Response.Response GetResult()
        {
            return this.response?.Body?.OrderCreateResponse?.Response ?? null;;
        }

        public virtual bool IsSuccessResponse()
        {
            if(this.response == null || !string.IsNullOrEmpty(this.errorResponse)) {
                return false;
            }

            var result = GetResult();
            string message = result.Message;

            return message == "Success" ? true : false;
        }

        public virtual string GetErrorResponse()
        {
            if(this.response == null) {
                return this.errorResponse ?? string.Empty;
            }

            var result = GetResult();
            string soNumber = GetRequest().Body.OrderCreate.Order.OrderID;
            string message = result.Message;

            if(message != "Success") {
                return $"SO Number {soNumber} fail. {message}";
            }

            return string.Empty;
        }
		
		public ISAHttpRequest.ISAIntegrationLogService.IntegrationLog GetLog()
		{
			return this.log ?? null;
		}

        protected virtual Request.Envelope BuildRequestLineDetail()
        {
            var result = new Request.Envelope();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            if(this.recordId != Guid.Empty && (this.recordIds == null || (this.recordIds != null && this.recordIds.Count == 0))) {

                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordId));
            
            } else if(this.recordId == Guid.Empty && (this.recordIds != null && this.recordIds.Count > 0)) {
                
                var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
                foreach (var recordId in this.recordIds) {
                    filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", recordId));
                }

                esq.Filters.Add(filterGroup);

            } else {
                throw new Exception("No line detail selected or can't be processed");
            }

            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual Request.Envelope BuildRequestSubmission()
        {
            var result = new Request.Envelope();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.Id", this.recordId));

            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual Request.Envelope BuildRequestSONumber(string SONumber)
        {
            var result = new Request.Envelope();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", SONumber));

            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual dynamic BuildQuery()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("No", esq.AddColumn("DgNo"));
            columns.Add("LineDetailId", esq.AddColumn("Id"));
            columns.Add("NCCFLineID", esq.AddColumn("DgLineId"));
            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("OrderIMSIType", esq.AddColumn("DgOrderIMSIType.Name"));

            // Order
            columns.Add("OrderID", esq.AddColumn("DgSOID"));
            columns.Add("CenterID", esq.AddColumn("Dg3PLService.DgCode"));
            columns.Add("RegionID", esq.AddColumn("DgSubmission.DgRegion.DgCode"));

            columns.Add("Remarks", esq.AddColumn("DgDeviceOrderRemark")); // DgRemark / DgDeviceOrderRemark ??
            columns.Add("LeadsID", esq.AddColumn("DgSFALead"));

            // Customers
            columns.Add("Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgName"));
            columns.Add("Address", esq.AddColumn("DgSubmission.DgCRMGroup.DgDeliveryaddress"));
            columns.Add("City", esq.AddColumn("DgSubmission.DgCRMGroup.DgCityAdmInformationDelivery.Name"));
            columns.Add("State", esq.AddColumn("DgSubmission.DgCRMGroup.DgStateAdmInfoDelivery.Name"));
            columns.Add("PostCode", esq.AddColumn("DgSubmission.DgCRMGroup.DgPostcodeAdmInformationDelivery.Name"));
            columns.Add("Country", esq.AddColumn("DgSubmission.DgCRMGroup.DgCountryAdmInformationDelivery.Name"));
            columns.Add("AuthorizePerson1", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedName1"));
            columns.Add("AP1_ICNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedIdNo1"));
            columns.Add("AP1_OfficeNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedOfficeTelNo1"));
            columns.Add("AP1_MobileNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedMobilePhone1"));
            columns.Add("AuthorizePerson2", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedName2"));
            columns.Add("AP2_ICNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedIdNo2"));
            columns.Add("AP2_OfficeNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedOfficeTelNo2"));
            columns.Add("AP2_MobileNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgAuthorizedMobilePhone2"));
            columns.Add("SalesPerson", esq.AddColumn("DgSubmission.DgCRMGroup.DgSalespersonID"));
            columns.Add("ContactPrimary", esq.AddColumn("DgSubmission.DgCRMGroup.DgAdministrationName1"));
            columns.Add("CP1_ICNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgIdNo1"));
            columns.Add("CP1_OfficeNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgOfficeTelNo1"));
            columns.Add("CP1_MobileNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgMobilePhone1"));
            columns.Add("ContactSecondary", esq.AddColumn("DgSubmission.DgCRMGroup.DgAdministrationName2"));
            columns.Add("CS_ICNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgIdNo2"));
            columns.Add("CS_OfficeNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgOfficeTelNo2"));
            columns.Add("CS_MobileNo", esq.AddColumn("DgSubmission.DgCRMGroup.DgMobilePhone2"));

            columns.Add("PrimaryOfferName", esq.AddColumn("DgPrimaryOffering.DgOfferName"));

            for (int i = 1; i <= 20; i++) {
                columns.Add($"SuppOffer{i}ID", esq.AddColumn($"DgSuppOffer{i}.DgOfferID"));
                columns.Add($"SuppOffer{i}Name", esq.AddColumn($"DgSuppOffer{i}.DgOfferName"));
            }

            columns["No"].OrderByAsc(0);
            columns["NCCFLineID"].OrderByAsc(1);

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToIPL", true));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsMMAG", false));

            // var filterSoNumber = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgSOID", string.Empty));
            // filterSoNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgSOID"));
            // esq.Filters.Add(filterSoNumber);

            // var filterOFSNumber = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
            // filterOFSNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgOFSDoNo", string.Empty));
            // filterOFSNumber.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgOFSDoNo"));
            // esq.Filters.Add(filterOFSNumber);

            // var filterSODoID = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            // filterSODoID.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSODoID", ""));
            // filterSODoID.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSODoID"));             
            // esq.Filters.Add(filterSODoID);

            // esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgSODate"));

            return new {
                esq = esq,
                columns = columns
            };
        }

        protected virtual Request.Envelope BuildRequest(EntityCollection entities, Dictionary<string, EntitySchemaQueryColumn> columns)
        {
            Entity firstEntity = entities.FirstOrDefault();
            if(firstEntity == null) {
                throw new Exception("No line detail selected or can't be processed");
            }

            string formattedDateTime = GenerateOrderDT();
            string soNumber = firstEntity.GetTypedColumnValue<string>(columns["OrderID"].Name);
            var orderParam = new Request.Order() {
                OrderID = soNumber,
                CenterID = firstEntity.GetTypedColumnValue<string>(columns["CenterID"].Name),
                ChannelID = "2",
                RegionCode = firstEntity.GetTypedColumnValue<string>(columns["RegionID"].Name),
                OrderType = "C",
                OrderDT = formattedDateTime,
                DeliveryDT = "0001-01-01T00:00:00",
                CollectDoc = "N",
                Remarks = firstEntity.GetTypedColumnValue<string>(columns["Remarks"].Name),
                LeadsID = firstEntity.GetTypedColumnValue<string>(columns["LeadsID"].Name),
                Customers = new Request.Customers() {
                    Customer = new Request.Customer() {
                        Name = firstEntity.GetTypedColumnValue<string>(columns["Name"].Name),
                        Address = firstEntity.GetTypedColumnValue<string>(columns["Address"].Name),
                        City = firstEntity.GetTypedColumnValue<string>(columns["City"].Name),
                        State = firstEntity.GetTypedColumnValue<string>(columns["State"].Name),
                        PostCode = firstEntity.GetTypedColumnValue<string>(columns["PostCode"].Name),
                        Country = firstEntity.GetTypedColumnValue<string>(columns["Country"].Name),
                        AuthorizePerson1 = firstEntity.GetTypedColumnValue<string>(columns["AuthorizePerson1"].Name),
                        AP1_ICNo = Masking(firstEntity.GetTypedColumnValue<string>(columns["AP1_ICNo"].Name)),
                        AP1_OfficeNo = firstEntity.GetTypedColumnValue<string>(columns["AP1_OfficeNo"].Name),
                        AP1_MobileNo = firstEntity.GetTypedColumnValue<string>(columns["AP1_MobileNo"].Name),
                        AuthorizePerson2 = firstEntity.GetTypedColumnValue<string>(columns["AuthorizePerson2"].Name),
                        AP2_ICNo = Masking(firstEntity.GetTypedColumnValue<string>(columns["AP2_ICNo"].Name)),
                        AP2_OfficeNo = firstEntity.GetTypedColumnValue<string>(columns["AP2_OfficeNo"].Name),
                        AP2_MobileNo = firstEntity.GetTypedColumnValue<string>(columns["AP2_MobileNo"].Name),
                        SalesPerson = firstEntity.GetTypedColumnValue<string>(columns["SalesPerson"].Name),
                        ContactPrimary = firstEntity.GetTypedColumnValue<string>(columns["ContactPrimary"].Name),
                        CP1_ICNo = Masking(firstEntity.GetTypedColumnValue<string>(columns["CP1_ICNo"].Name)),
                        CP1_OfficeNo = firstEntity.GetTypedColumnValue<string>(columns["CP1_OfficeNo"].Name),
                        CP1_MobileNo = firstEntity.GetTypedColumnValue<string>(columns["CP1_MobileNo"].Name),
                        ContactSecondary = firstEntity.GetTypedColumnValue<string>(columns["ContactSecondary"].Name),
                        CS_ICNo = Masking(firstEntity.GetTypedColumnValue<string>(columns["CS_ICNo"].Name)),
                        CS_OfficeNo = firstEntity.GetTypedColumnValue<string>(columns["CS_OfficeNo"].Name),
                        CS_MobileNo = firstEntity.GetTypedColumnValue<string>(columns["CS_MobileNo"].Name)
                    }
                }
            };

            var result = new Request.Envelope() {
                Body = new Request.Body() {
                    OrderCreate = new Request.OrderCreate() {
                        Order = orderParam
                    }
                }
            };
            
            List<string> errorList = new List<string>();

            var items = new List<Request.Item>();
            foreach(Entity entity in entities) {
                var offerIDList = new List<string>();
                for (int i = 1; i <= 20; i++) {
                    offerIDList.Add(entity.GetTypedColumnValue<string>(columns[$"SuppOffer{i}ID"].Name));
                }

                Guid lineDetailId = entity.GetTypedColumnValue<Guid>(columns["LineDetailId"].Name);
                string MSISDN = entity.GetTypedColumnValue<string>(columns["MSISDN"].Name);
                string NCCFLineID = entity.GetTypedColumnValue<int>(columns["NCCFLineID"].Name).ToString();
                string no = entity.GetTypedColumnValue<int>(columns["No"].Name).ToString();
                string imsiType = entity.GetTypedColumnValue<string>(columns["OrderIMSIType"].Name);
                string ratePlan = entity.GetTypedColumnValue<string>(columns["PrimaryOfferName"].Name);

                Request.Item reqItem = GetItem(
                    lineDetailId, 
                    MSISDN, 
                    NCCFLineID, 
                    offerIDList
                );
                if(reqItem != null) {
                    items.Add(reqItem);
                }

                if(imsiType == "3in1 USIM_Half Size") {
                    items.Add(new Request.Item() {
                        ItemCode = IMSI_ItemCode,
                        ItemDesc = string.Empty,
                        Quantity = "0",
                        SimType = IMSI_SIMType,
                        PackageCode = IMSI_ItemCode,
                        PromoCode = string.Empty,
                        RatePlan = ratePlan,
                        MSISDN = MSISDN,
                        PackagePrice = "0",
                        NCCFLineID = NCCFLineID
                    });
                }

                /*if(items.FindIndex(el => el.NCCFLineID == NCCFLineID) == -1) {
                    errorList.Add($"Line No {no} does not have Order item");
                }*/
            }
			
			/*
            if(errorList.Count > 0) {
                string error = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"<ul>{error}</ul>");
            }
			*/

            if(items.Count == 0) {
                throw new Exception($"SO Number {soNumber} does not have Order item");
            }

            result.Body.OrderCreate.Order.Items = new Request.Items() {
                Item = items
            };
            
            return result;
        }

        protected virtual Request.Item GetItem(Guid LineDetailId, string msisdn, string lineID, List<string> OfferIDList)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("PackageCode", esq.AddColumn("DgOFSCode"));
            columns.Add("ItemCode", esq.AddColumn("DgResModeID"));
            columns.Add("DgSuppOfferIndex", esq.AddColumn("DgSuppOfferIndex"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", LineDetailId));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgFeeName", "Handset Fee"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            if(entity == null) {
                return null;
            }

            return new Request.Item() {
                ItemCode = entity.GetTypedColumnValue<string>(columns["ItemCode"].Name),
                ItemDesc = "",
                Quantity = "0",
                SimType = "",
                PackageCode = entity.GetTypedColumnValue<string>(columns["PackageCode"].Name),
                PromoCode = "",
                RatePlan = OfferIDList[entity.GetTypedColumnValue<int>(columns["DgSuppOfferIndex"].Name)-1],
                MSISDN = msisdn,
                PackagePrice = "0",
                NCCFLineID = lineID,
            };
        }

        protected virtual string GenerateOrderDT()
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            TimeSpan gmtOffset = TimeSpan.FromHours(8);
            DateTimeOffset gmtPlus8 = utcNow.ToOffset(gmtOffset);
            return gmtPlus8.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
        }
		
		protected virtual string Masking(string Text)
		{
			if (Text.Length <= 4) {
				return Text;
			}
			
			string lastFour = Text.Substring(Text.Length - 4);
			return new string('*', Text.Length - 4) + lastFour;
		}

        protected virtual void Validation(Request.Envelope Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }
            
            if(Param.Body == null) {
                throw new Exception("Param Body cannot be null or empty");
            }

            if(Param.Body.OrderCreate == null) {
                throw new Exception("Param Body > OrderCreate cannot be null or empty");
            }

            var order = Param.Body.OrderCreate.Order;
            if(order == null) {
                throw new Exception("Param Body > OrderCreate > Order cannot be null or empty");
            }

            if(order.Customers == null) {
                throw new Exception("Param Customers cannot be null or empty");
            }

            if(order.Customers.Customer == null) {
                throw new Exception("Param Customer cannot be null or empty");
            }

            if(order.Items == null) {
                throw new Exception("Param Items cannot be null or empty");
            }

            if(order.Items.Item == null || (order.Items.Item != null && order.Items.Item.Count == 0)) {
                throw new Exception("Param Items > Item cannot be null or empty");
            }

            List<string> errorList = new List<string>();
            
            var orderCheck = new Dictionary<string, string>() {
                {"Center ID", order.CenterID},
                {"Order DT", order.OrderDT},
				{"Region Code", order.RegionCode}
            };
            foreach (var item in orderCheck) {
                if(string.IsNullOrEmpty(item.Value)) {
                    errorList.Add($"{item.Key} cannot be null or empty");
                }
            }

            var customer = order.Customers.Customer;
            var customerCheck = new Dictionary<string, string>() {
                {"Name", customer.Name},
                {"Address", customer.Address},
                {"City", customer.City},
                {"State", customer.State},
                {"PostCode", customer.PostCode},
                {"Country", customer.Country},
                {"Authorize Person 1", customer.AuthorizePerson1},
                {"Authorize Person 1 IC No", customer.AP1_ICNo},
                {"Authorize Person 1 Office No", customer.AP1_OfficeNo},
                {"Authorize Person 1 Mobile No", customer.AP1_MobileNo},
                {"Sales Person", customer.SalesPerson},
                {"Contact Primary", customer.ContactPrimary},
                {"Contact Primary IC No", customer.CP1_ICNo},
                {"Contact Primary Office No", customer.CP1_OfficeNo},
                {"Contact Primary Mobile No", customer.CP1_MobileNo}
            };
            foreach (var item in customerCheck) {
                if(string.IsNullOrEmpty(item.Value)) {
                    errorList.Add($"{item.Key} cannot be null or empty");
                }
            }

            foreach (var item in order.Items.Item) {
                var itemCheck = new Dictionary<string, string>() {
                    {"Item Code", item.ItemCode},
                    {"Package Code", item.PackageCode},
                    {"MSISDN", item.MSISDN},
                    {"NCCF Line ID", item.NCCFLineID}
                };
                foreach (var check in itemCheck) {
                    if(string.IsNullOrEmpty(check.Value)) {
                        errorList.Add($"{check.Key} cannot be null or empty");
                    }
                }
            }

            if(errorList.Count > 0) {
                string error = string.Join("", errorList.Select(item => $"<li>{item}</li>").ToArray());
                throw new Exception($"<ul>{error}</ul>");
            }
        }
		
		public static void InsertLog(UserConnection UserConnection, ISAHttpRequest.ISAIntegrationLogService.IntegrationLog Log, string SONumber, string Status) 
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = string.Format("{0}_MMAGOrderCreate_{1}.txt", currentDateTimeMY, SONumber);
            var log = string.Format("{0}{1}XML Request: {2}{3}{4}{5}XML Response: {6}{7}", 
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
                    {"DgAPIName", "MMAG Order Create"},
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