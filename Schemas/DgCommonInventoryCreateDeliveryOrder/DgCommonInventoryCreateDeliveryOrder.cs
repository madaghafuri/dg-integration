using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using ISAIntegrationSetup;
using SolarisCore;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCommonInventory
{
    public class CreateDeliveryOrder
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;

        public CreateDeliveryOrder(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "Common Inventory", "CreateDeliveryOrder", string.Empty);
            if(setup == null) {
                throw new Exception("Common Inventory: Create Delivery Order hasn't been set up for integration");
            }

            return setup;
        }

        public virtual CreateDeliveryRequest GetParam(CreateDeliveryRequest Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            if(Param.requestBody == null) {
                throw new Exception("Request Body cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.brnno)) {
                throw new Exception("BRN cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.city)) {
                throw new Exception("City cannot be null or empty");
            }

            // if(string.IsNullOrEmpty(Param.requestBody.contactno)) {
            //     throw new Exception("Contact number cannot be null or empty");
            // }

            if(string.IsNullOrEmpty(Param.requestBody.name1)) {
                throw new Exception("Name cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.country)) {
                throw new Exception("Country cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.deliveryaddr1)) {
                throw new Exception("Delivery Address cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.postcode)) {
                throw new Exception("Postcode cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.saleschannel)) {
                throw new Exception("Sales channel cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.statecode)) {
                throw new Exception("State code cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.storeid)) {
                throw new Exception("Store Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.requestBody.orderno)) {
                throw new Exception("Order number cannot be null or empty");
            }

            if(Param.requestBody.itemlist == null || (Param.requestBody.itemlist != null && Param.requestBody.itemlist.Count == 0)) {
                throw new Exception("Item list cannot be null or empty");
            }

            return Param;
        }

        public virtual CreateDeliveryRequest GetParam(string SONumber)
        {
            if(string.IsNullOrEmpty(SONumber)) {
                throw new Exception("SO Number cannot be null or empty");
            }

            return GetParam(BuildRequestBySONumber(SONumber));
        }

        public virtual CreateDeliveryRequest GetParam(string ReservationID, string StoreID)
        {
            if(string.IsNullOrEmpty(ReservationID)) {
                throw new Exception("Reservation ID cannot be null or empty");
            }

            if(string.IsNullOrEmpty(StoreID)) {
                throw new Exception("Store ID cannot be null or empty");
            }

            return GetParam(BuildRequestByReservationID(ReservationID, StoreID));
        }

        public virtual CreateDeliveryRequest GetParam(Guid RecordId)
        {
            if(RecordId == Guid.Empty) {
                throw new Exception("Record Id cannot be null or empty");
            }

            return GetParam(BuildRequestByLineDetail(RecordId));
        }

        public virtual bool IsSuccess(CreateDeliveryResponse Response)
        {
            if(Response == null) {
                return false;
            }

            if(Response.responseBody.status == "S") {
                return true;
            }
            
            return false;
        }

        public virtual string GetErrorResponse(CreateDeliveryResponse Response)
        {
            if(Response == null) {
                return string.Empty;
            }

            return $"{Response.responseBody.status}: {Response.responseBody.messages}";
        }

        public virtual string GetErrorResponse(string ResponseBody)
        {
            if(string.IsNullOrEmpty(ResponseBody)) {
                return string.Empty;
            }

            var settings = new JsonSerializerSettings {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            try {
                var authError = JsonConvert.DeserializeObject<UnauthorizedResponse>(ResponseBody, settings);
                return $"[{authError.fault.code}] {authError.fault.message}: {authError.fault.description}";
            } catch (Exception) {}

            try {
                var reqError = JsonConvert.DeserializeObject<ErrorResponse>(ResponseBody, settings);
                return $"{reqError.requestError.serviceException.messageId}: {reqError.requestError.serviceException.text}";
            } catch (Exception) {}

            return ResponseBody;
        }

        protected virtual CreateDeliveryRequest BuildRequestBySONumber(string SONumber)
        {
            var result = new CreateDeliveryRequest();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.DgSOID", SONumber));
            
            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual CreateDeliveryRequest BuildRequestByReservationID(string ReservationID, string StoreID)
        {
            var result = new CreateDeliveryRequest();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.DgReservationID", ReservationID));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.Dg3PLService.DgStoreID", StoreID));
            
            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual CreateDeliveryRequest BuildRequestByLineDetail(Guid RecordId)
        {
            var result = new CreateDeliveryRequest();

            var query = BuildQuery();
            EntitySchemaQuery esq = query.esq;
            Dictionary<string, EntitySchemaQueryColumn> columns = query.columns;

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.Id", RecordId));
            
            return BuildRequest(esq.GetEntityCollection(UserConnection), columns);
        }

        protected virtual CreateDeliveryRequest BuildRequest(EntityCollection entities, Dictionary<string, EntitySchemaQueryColumn> columns)
        {
            var firstEntity = entities.FirstOrDefault();
			if(firstEntity == null) {
				throw new Exception("Data not found");
			}

            var requestBody = new requestBodyCD();
            requestBody.brnno = firstEntity.GetValue<string>(columns, "brnno");
            requestBody.city = firstEntity.GetValue<string>(columns, "city");
            requestBody.contactno = "";
            requestBody.name1 = firstEntity.GetValue<string>(columns, "name1");
            requestBody.country = firstEntity.GetValue<string>(columns, "country");
            requestBody.deliveryaddr1 = firstEntity.GetValue<string>(columns, "deliveryaddr1");
            requestBody.deliveryaddr2 = firstEntity.GetValue<string>(columns, "deliveryaddr1");
            requestBody.postcode = firstEntity.GetValue<string>(columns, "postcode");
            requestBody.statecode = firstEntity.GetValue<string>(columns, "statecode");
            requestBody.storeid = firstEntity.GetValue<string>(columns, "storeid");
            requestBody.orderno = firstEntity.GetValue<string>(columns, "orderno");
			requestBody.saleschannel = "NCCF";

            var temp = new List<Dictionary<string, string>>();
            foreach (var entity in entities) {
                string reservationID = entity.GetValue<string>(columns, "text");
                string itemCode = entity.GetValue<string>(columns, "item_code");

                temp.Add(new Dictionary<string, string>() {
                    {"ReservationID", reservationID},
                    {"ItemCode", itemCode}
                });
            }
            
            List<itemlist> itemlist = temp
                .GroupBy(item => new { ReservationID = item["ReservationID"], ItemCode = item["ItemCode"] })
                .Select((item, index) => new itemlist {
                    item_code = item.Key.ItemCode,
                    item_no = (index+1).ToString(),
                    quantity = item.Count().ToString(),
                    text = item.Key.ReservationID,
                })
                .ToList();
            requestBody.itemlist = itemlist;

            return new CreateDeliveryRequest() {
                requestBody = requestBody
            };
        }

        protected virtual dynamic BuildQuery()
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("No", esq.AddColumn("DgLineDetail.DgNo"));
            columns.Add("LineID", esq.AddColumn("DgLineDetail.DgLineId"));
            columns.Add("brnno", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgBRN"));
            columns.Add("city", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgCityAdmInformationDelivery.Name"));
            columns.Add("name1", esq.AddColumn("DgLineDetail.DgUsername"));
            columns.Add("country", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgCountryAdmInformationDelivery.Name"));
            columns.Add("deliveryaddr1", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgDeliveryaddress"));
            columns.Add("postcode", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgPostcodeAdmInformationDelivery.Name"));
            columns.Add("statecode", esq.AddColumn("DgLineDetail.DgSubmission.DgCRMGroup.DgStateAdmInfoDelivery.Name"));
            columns.Add("storeid", esq.AddColumn("DgLineDetail.Dg3PLService.DgStoreID"));
            columns.Add("orderno", esq.AddColumn("DgLineDetail.DgSOID"));
            columns.Add("item_code", esq.AddColumn("DgResModeID"));
            // columns.Add("msisdn", esq.AddColumn("DgLineDetail.DgMSISDN"));
            columns.Add("text", esq.AddColumn("DgLineDetail.DgReservationID"));

            columns["No"].OrderByAsc(0);
            columns["LineID"].OrderByAsc(1);

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgFeeName", "Handset Fee"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.DgReleasedToIPL", true));

            return new {
                esq = esq,
                columns = columns
            };
        }
    }
}