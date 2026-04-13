using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using ISAIntegrationSetup;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using ConfirmPortIn_Request = DgIntegration.DgConfirmPortInService.Request;
using ConfirmPortIn_Response = DgIntegration.DgConfirmPortInService.Response;

namespace DgIntegration.DgConfirmPortInService
{
    public class ConfirmPortInService
    {
        private HttpClient httpClient;
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

        private ConfirmPortIn_Request.Envelope param;
        private ConfirmPortIn_Response.Envelope response;
        private string errorResponse;
		private ISAHttpRequest.ISAIntegrationLogService.IntegrationLog log;

        public ConfirmPortInService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
            
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "ConfirmPortIn");
            if(setup == null) {
                throw new Exception("ConfirmPortIn hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.username = setup.Authentication.Basic.Username;
			this.password = setup.Authentication.Basic.Password;

            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(this.url);
        }

        public virtual async Task Request()
        {
            string res = string.Empty;
            this.errorResponse = string.Empty;
            this.response = null;
            
            var _httpRequest = new HTTPRequest(this.httpClient, UserConnection);

            if(!string.IsNullOrEmpty(this.section)) {
                _httpRequest.SetLogSection(this.section);
            }

            if(this.recordId != null && this.recordId != Guid.Empty) {
                _httpRequest.SetLogRecordId(this.recordId);
            }

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                string xml = HTTPRequest.XmlToString<ConfirmPortIn_Request.Envelope>(this.param);
                var req = await _httpRequest
                    .SetLogName("Confirm Port In")
                    .AddHeader("SOAPAction", "urn:ConfirmPortInOperation")
                .Post(this.endpoint, xml, ContentType.Xml);
				
				this.log = _httpRequest.GetLog();

                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    throw new Exception(req.Error ?? req.StatusCode);
                }

                if(string.IsNullOrEmpty(req.Body)) {
                    throw new Exception("Response is empty");
                }

                res = req.Body;
            } catch (Exception e) {
                this.errorResponse = e.Message;

                return;
            }

            try {
                this.response = HTTPRequest.XmlToObject<ConfirmPortIn_Response.Envelope>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }

        public virtual ConfirmPortInService SetParam(string Xml)
        {
            try {
                return this.SetParam(HTTPRequest.XmlToObject<ConfirmPortIn_Request.Envelope>(Xml));   
            } catch (Exception e) {
                throw new Exception($"Xml is not valid: {e.Message}");
            }
        }

        public virtual ConfirmPortInService SetParam(ConfirmPortIn_Request.Envelope Param)
        {
            this.param = Param;
            return this;
        }

        public virtual ConfirmPortInService SetParam(Guid RecordId)
        {
            this.section = "DgLineDetail";
            this.recordId = RecordId;

            this.param = BuildRequest();
            return this;
        }

        public virtual ConfirmPortIn_Request.Envelope GetRequest()
        {
            return this.param;
        }

        public virtual string GetStringRequest()
        {
            return HTTPRequest.XmlToString<ConfirmPortIn_Request.Envelope>(this.param);
        }

        public virtual ConfirmPortIn_Response.Envelope GetResponse()
        {
            return this.response;
        }

        public virtual string GetStringResponse()
        {
            return HTTPRequest.XmlToString<ConfirmPortIn_Response.Envelope>(this.response);
        }

        public virtual bool IsSuccessResponse()
        {
            if(this.response == null || !string.IsNullOrEmpty(this.errorResponse)) {
                return false;
            }
            
            var result = this.response?.Header?.CSGHeader;
            string resultOperationMessage = result?.Status;

            return resultOperationMessage == "success" ? true : false;
        }

        public virtual string GetErrorResponse()
        {
            if(this.response == null) {
                return this.errorResponse ?? string.Empty;
            }

            var result = this.response?.Header?.CSGHeader;
            string resultOperationMessage = result?.Status;
            string resultOperationCode = result?.ErrorCode;

            if(resultOperationMessage != "success") {
                return $"{resultOperationCode}: {resultOperationMessage}";
            }

            return string.Empty;
        }
		
		public ISAHttpRequest.ISAIntegrationLogService.IntegrationLog GetLog()
		{
			return this.log ?? null;
		}

        protected virtual ConfirmPortIn_Request.Envelope BuildRequest()
        {
            var result = new ConfirmPortIn_Request.Envelope();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            
            // transactionCommonInfo
            columns.Add("PortInTransactionID", esq.AddColumn("DgPortInTransactionID"));
            columns.Add("ActivationPortId", esq.AddColumn("DgActivationPortId"));
			columns.Add("PortInMessageID", esq.AddColumn("DgPortInMessageID"));
			
            var entity = esq.GetEntity(UserConnection, this.recordId);

            if(entity == null) {
                return null;
            }

            var portInTransactionID = entity.GetTypedColumnValue<string>(columns["PortInTransactionID"].Name);
            var ActivationPortId = entity.GetTypedColumnValue<string>(columns["ActivationPortId"].Name);
			var PortInMessageID = entity.GetTypedColumnValue<string>(columns["PortInMessageID"].Name);

            result = new ConfirmPortIn_Request.Envelope() {
                Header = new ConfirmPortIn_Request.Header() {
                    Security = Helper.GenerateUsernameToken(this.username, this.password),
                    CSGHeader = new ConfirmPortIn_Request.CSGHeader() {
                        SourceSystemID = "NCCF",
                        ReferenceID = Helper.GenerateReferenceId(),
                        ChannelMedia = "WEB"
                    }
                },
                Body = new ConfirmPortIn_Request.Body() {
                    ConfirmPortInRequest = new ConfirmPortIn_Request.ConfirmPortInRequest() {
                        MNPInformation = new ConfirmPortIn_Request.MNPInformation() {
                            PortInTransactionId = portInTransactionID,
                            PortInMessageId = PortInMessageID,
                            PortId = ActivationPortId,
                        }
                    }
                }
            };

            return result;
        }
    }
}