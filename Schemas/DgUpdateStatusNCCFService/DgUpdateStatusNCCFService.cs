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
using Terrasoft.Configuration;
using Newtonsoft.Json;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgCreatioIntegrationHelper;
using DgBaseService.DgIntegrationHelperService;

namespace DgIntegration.DgUpdateStatusNCCFService
{
    public class UpdateStatusNCCFService
    {
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public UpdateStatusNCCFService(UserConnection userConnection_ = null) 
        {
        	if(userConnection_ != null) {
				userConnection = userConnection_;
			}
          
        }

        public Guid CheckDataExist(string SchemaName, string FilterColumnName, string ColumName)
        {
            var result = Guid.Empty;
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, SchemaName);

            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("id", esq.AddColumn("Id"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, FilterColumnName, ColumName));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (Entity entity in entities)
            {   
                result =  entity.GetTypedColumnValue<Guid>(columns["id"].Name);
            }

            return result;
        }

        public ResponseUpdateStatus GetUpdateStatus(List<UpdateStatus> Data)
        {
            var result = new ResponseUpdateStatus();

            try {
                var serialNumber = Guid.Empty;
                var statusCode = Guid.Empty;

                // throw new Exception(JsonConvert.SerializeObject( new {
                //     data = Data
                // }, Formatting.Indented));

                foreach (var itemData in Data)
                {
                   
                    serialNumber = CheckDataExist("DgSubmission", "DgSerialNumber", itemData.serialNumber);
                    statusCode = CheckDataExist("DgDeliveryStatus", "DgCode", itemData.statusCode);

                    if (serialNumber == Guid.Empty) {
                        throw new Exception("Serial Number is does not exist");
                    }

                    if (statusCode == Guid.Empty) {
                       throw new Exception("statusCode is does not exist");
                    }

                  
                    var dataInsert = new UpdateStatusParameter() {
                        serialNumber = serialNumber,
                        statusCode = statusCode,
                    };

                    UpdateStatusLineDetail(dataInsert);
                }
                
                result.Success = true;
                result.Message = "Update Status Success";
                result.data = Data;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        public void UpdateStatusLineDetail(UpdateStatusParameter Data) 
        {
            new Update(UserConnection, "DgLineDetail")
                .Set("DgDeliveryStatusId", Column.Parameter(Data.statusCode))
                .Where("DgSubmissionId").IsEqual(Column.Parameter(Data.serialNumber))
            .Execute();
        }

    }

    public class ResponseUpdateStatus
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<UpdateStatus> data { get; set; }
    }

    public class UpdateStatus
    {	
        public string serialNumber { get; set; }
        public string statusCode { get; set; }
    }

    public class UpdateStatusParameter
    {	
        public Guid serialNumber { get; set; }
        public Guid statusCode { get; set; }
    }
}