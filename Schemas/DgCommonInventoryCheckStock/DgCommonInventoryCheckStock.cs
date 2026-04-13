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
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCommonInventory
{
    public class CheckStock
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;

        public CheckStock(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "Common Inventory", "CheckStock", string.Empty);
            if(setup == null) {
                throw new Exception("Common Inventory: CheckStock hasn't been set up for integration");
            }

            return setup;
        }

        public virtual CheckStockRequest GetParam(string ItemLocationId, string ItemCode, string UserId)
        {
            var Param = new CheckStockRequest() {
                itemLocationId = ItemLocationId,
                itemCode = ItemCode,
                userId = UserId
            };
            return GetParam(Param);
        }

        public virtual CheckStockRequest GetParam(CheckStockRequest Param)
        {
            if(Param == null) {
                throw new Exception("Param cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.itemLocationId)) {
                throw new Exception("Item Location Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.itemCode)) {
                throw new Exception("Item Code cannot be null or empty");
            }

            if(string.IsNullOrEmpty(Param.userId)) {
                throw new Exception("User Id cannot be null or empty");
            }

            return Param;
        }

        public virtual CheckStockRequest GetParam(Guid RecordId)
        {
            return BuildRequest(RecordId);
        }

        public virtual string GetErrorResponse(string ResponseBody)
        {
            if(string.IsNullOrEmpty(ResponseBody)) {
                return string.Empty;
            }

            var settings = new JsonSerializerSettings {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            // error authetinkasi
            try {
                var authError = JsonConvert.DeserializeObject<UnauthorizedResponse>(ResponseBody, settings);
                return $"[{authError.fault.code}] {authError.fault.message}: {authError.fault.description}";
            } catch (Exception) {}

            // error lainnya
            try {
                var reqError = JsonConvert.DeserializeObject<ErrorResponse>(ResponseBody, settings);
                return $"{reqError.requestError.serviceException.messageId}: {reqError.requestError.serviceException.text}";
            } catch (Exception) {}

            return ResponseBody;
        }

        protected virtual CheckStockRequest BuildRequest(Guid RecordId)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("ItemLocationId", esq.AddColumn("DgLineDetail.Dg3PLService.DgCode"));
            columns.Add("ItemCode", esq.AddColumn("DgResModeID"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Greater, "DgSuppOfferIndex", 0));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgFeeName", "Handset Fee"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.Id", RecordId));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            if(entity == null) {
                throw new Exception("This line does not have a device / item code");
            }
            
            string itemLocationId = entity.GetTypedColumnValue<string>(columns["ItemLocationId"].Name);
            string itemCode = entity.GetTypedColumnValue<string>(columns["ItemCode"].Name);

            return GetParam(itemLocationId, itemCode, "NCCF");
        }
    }
}