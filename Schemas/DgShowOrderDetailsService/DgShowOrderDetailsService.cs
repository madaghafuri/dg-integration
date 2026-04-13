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
using ShowOrderDetails_Request = DgIntegration.DgShowOrderDetailsService.Request;
using ShowOrderDetails_Response = DgIntegration.DgShowOrderDetailsService.Response;
using ISAIntegrationSetup;
using DgMasterData;
using DgMasterData.DgLookupConst;

namespace DgIntegration.DgShowOrderDetailsService
{
    public class ShowOrderDetailsService
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
        protected ShowOrderDetails_Request.RequestShowOrderDetails param;
        private ShowOrderDetails_Response.ResponseShowOrderDetails response;
        private HttpResponseHeaders headerResponse;
        private string errorResponse;
        private string statusCode;
        private HTTPRequest httpRequest;

        public ShowOrderDetailsService(UserConnection userConnection, bool isCelcom = false) 
        {
            this.userConnection = userConnection;

            var setup = IntegrationSetup.Get(UserConnection, "Apple", "ShowOrderDetails");
            if(setup == null) {
                throw new Exception("ShowOrderDetails hasn't been set up for integration");
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
            this.statusCode = string.Empty;
            this.errorResponse = string.Empty;
            this.response = null;
            this.headerResponse = null;

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                var req = await this.httpRequest
                    .SetLogName($"Apple: Show Order Detail Device")
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
                this.response = JsonConvert.DeserializeObject<ShowOrderDetails_Response.ResponseShowOrderDetails>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }
		
        public virtual ShowOrderDetailsService SetParam(string Json)
        {
			try {
				return this.SetParam(JsonConvert.DeserializeObject<ShowOrderDetails_Request.RequestShowOrderDetails>(Json));
			} catch (Exception e) {
				throw new Exception($"Json is not valid: {e.Message}");
			}
        }

        public virtual ShowOrderDetailsService SetParam(ShowOrderDetails_Request.RequestShowOrderDetails Param)
        {
            this.param = Param;
            return this;
        }

        public virtual ShowOrderDetailsService SetParam(Guid RecordId)
        {
            this.recordId = RecordId;
            this.param = BuildRequest();
            return this;
        }

        public virtual ShowOrderDetails_Request.RequestShowOrderDetails GetRequest()
        {
            return this.param;
        }

        public virtual string GetStringRequest()
        {
            return JsonConvert.SerializeObject(this.param);
        }

        public virtual ShowOrderDetails_Response.ResponseShowOrderDetails GetResponse()
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

            if(!string.IsNullOrEmpty(this.response.orders[0].showOrderStatusCode)) {
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

        protected virtual ShowOrderDetails_Request.RequestShowOrderDetails BuildRequest()
        {
            var orderNumber = String.Empty;
            var reseller = GetReseller();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("OrderNumber", esq.AddColumn("DgOrderNumber"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordId));
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                orderNumber = entity.GetTypedColumnValue<string>(columns["OrderNumber"].Name);
            }

            return new ShowOrderDetails_Request.RequestShowOrderDetails() {
                requestContext = new ShowOrderDetails_Request.requestContext() {
                    shipTo = reseller.Name, 
                    timeZone = "-480",
                    langCode = "en",
                },
                depResellerId = reseller.Code,
                orderNumbers = new List<string>() {
                    orderNumber
                }
            };
        }
    }
}