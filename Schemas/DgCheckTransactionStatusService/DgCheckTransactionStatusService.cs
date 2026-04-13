using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DgBaseService.DgGenericResponse;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using System.Security.Cryptography.X509Certificates;
using ISAHttpRequest.ISAIntegrationLogService;
using System.Text.RegularExpressions;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using CheckTransactionStatusService_Request = DgIntegration.DgCheckTransactionStatusService.Request;
using CheckTransactionStatusService_Response = DgIntegration.DgCheckTransactionStatusService.Response;
using ISAIntegrationSetup;
using DgMasterData;
using DgMasterData.DgLookupConst;

namespace DgIntegration.DgCheckTransactionStatusService
{
    public class CheckTransactionStatusService
    {
        private HttpClient httpClient;
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        
        private string url { get; }
        private string endpoint { get; }
        protected Guid recordId;
        protected bool isCelcom;
        protected CheckTransactionStatusService_Request.RequestCheckTransactionStatus param;
        private CheckTransactionStatusService_Response.ResponseCheckTransactionStatus response;
        private HttpResponseHeaders headerResponse;
        private string errorResponse;
        private string statusCode;
        private HTTPRequest httpRequest;

        public CheckTransactionStatusService(UserConnection userConnection, bool isCelcom = false) 
        {
            this.userConnection = userConnection;

            var setup = IntegrationSetup.Get(UserConnection, "Apple", "CheckTransactionStatus");
            if(setup == null) {
                throw new Exception("CheckTransactionStatus hasn't been set up for integration");
            }
			
			this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
            this.isCelcom = isCelcom;

            var pathCertificateDigi = (string)SysSettings.GetValue(UserConnection, "DgFilePathCertificateAppleDigi");
            var passwordCertifcateDigi = (string)SysSettings.GetValue(UserConnection, "DgPasswordCertificateAppleDigi");
            var pathCertificateCelcom = (string)SysSettings.GetValue(UserConnection, "DgFilePathCertificateAppleCelcom");
            var passwordCertifcateCelcom = (string)SysSettings.GetValue(UserConnection, "DgPasswordCertificateAppleCelcom");
            
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            
            X509Certificate2 certificate = isCelcom ? new X509Certificate2(pathCertificateCelcom, passwordCertifcateCelcom) : new X509Certificate2(pathCertificateDigi, passwordCertifcateDigi);

            var handler = new HttpClientHandler
            {
                ClientCertificates = { certificate }
            };

            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(this.url);
            this.httpRequest = new HTTPRequest(this.url, UserConnection, handler);
        }

        public virtual async Task Request()
        {
            string res = string.Empty;
            this.errorResponse = string.Empty;
            this.response = null;
            this.headerResponse = null;

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                var req = await this.httpRequest
                    .SetLogName($"Apple: Check Transaction Status Device")
                    .SetLogSection("DEP/ABM Registration")
                    .SetLogRecordId(this.recordId)
                .Post(this.endpoint, this.param);

                this.statusCode = req.StatusCode;
                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    throw new Exception(req.Error ?? req.StatusCode);
                }

                if(string.IsNullOrEmpty(req.Body) || req.Body == "{}") {
                    throw new Exception("Response is empty");
                }

                res = req.Body;
                this.headerResponse = req.Headers;
            } catch (Exception e) {
                this.errorResponse = e.Message;

                return;
            }

            try {
                this.response = JsonConvert.DeserializeObject<CheckTransactionStatusService_Response.ResponseCheckTransactionStatus>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }
		
        public virtual CheckTransactionStatusService SetParam(string Json)
        {
			try {
				return this.SetParam(JsonConvert.DeserializeObject<CheckTransactionStatusService_Request.RequestCheckTransactionStatus>(Json));
			} catch (Exception e) {
				throw new Exception($"Json is not valid: {e.Message}");
			}
        }

        public virtual CheckTransactionStatusService SetParam(CheckTransactionStatusService_Request.RequestCheckTransactionStatus Param)
        {
            this.param = Param;
            return this;
        }

        public virtual CheckTransactionStatusService SetParam(Guid RecordId)
        {
            this.recordId = RecordId;
            this.param = BuildRequest();
            return this;
        }

        public virtual CheckTransactionStatusService_Request.RequestCheckTransactionStatus GetRequest()
        {
            return this.param;
        }

        public virtual string GetStringRequest()
        {
            return JsonConvert.SerializeObject(this.param);
        }

        public virtual CheckTransactionStatusService_Response.ResponseCheckTransactionStatus GetResponse()
        {
            return this.response;
        }

        public virtual string GetStringResponse()
        {
            return JsonConvert.SerializeObject(this.response);
        }

        public virtual bool IsSuccessResponse()
        {
            if(this.headerResponse == null || this.response == null) {
                return false;
            }
			
			if(!string.IsNullOrEmpty(this.response.errorCode)) {
                return false;
            }

            return true;
        }

        public virtual string GetErrorResponse()
        {
            var result = string.Empty;

            if(this.headerResponse == null) {
                return this.errorResponse ?? string.Empty;
            }

            return result;
        }

        public virtual string GetStatusCode()
        {
            return this.statusCode;
        }

        protected virtual DgMasterData.Lookup GetReseller()
        {
            var result = new DgMasterData.Lookup();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPReseller");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
                columns.Add("Name", esq.AddColumn("DgShipTo"));
                columns.Add("Code", esq.AddColumn("DgCode"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.isCelcom ? Reseller.Celcom : Reseller.DEP));

            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result.Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
                result.Name = entity.GetTypedColumnValue<string>(columns["Name"].Name);
                result.Code = entity.GetTypedColumnValue<string>(columns["Code"].Name);
            }

            return result;
        }

        protected virtual CheckTransactionStatusService_Request.RequestCheckTransactionStatus BuildRequest()
        {
            var reseller = GetReseller();
            var deviceEnrollmentTransactionId = String.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("DeviceEnrollmentTransactionId", esq.AddColumn("DgDeviceEnrollmentTransactionId"));
                columns.Add("ResellerID", esq.AddColumn("DgReseller.DgCode"));
                columns.Add("ShipTo", esq.AddColumn("DgReseller.DgShipTo"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordId));
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                deviceEnrollmentTransactionId = entity.GetTypedColumnValue<string>(columns["DeviceEnrollmentTransactionId"].Name);
            }

            return new CheckTransactionStatusService_Request.RequestCheckTransactionStatus() {
                requestContext = new CheckTransactionStatusService_Request.requestContext() {
                    shipTo = reseller.Name,
                    timeZone = "-480",
                    langCode = "en",
                },
                depResellerId = reseller.Code,
                deviceEnrollmentTransactionId = deviceEnrollmentTransactionId
            };
        }

        protected void UpdateStatusDEPLine(string Status, Guid DEPRegistrationDetailId)
        {
            var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                .Set("DgStatus", Column.Parameter(Status))
                .Where("Id").IsEqual(Column.Parameter(DEPRegistrationDetailId));
            update.Execute();
        }
    }
}