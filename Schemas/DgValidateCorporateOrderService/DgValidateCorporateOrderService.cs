using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.RegularExpressions;
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
using DgBaseService.DgHelpers;
using ISAIntegrationSetup;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using ValidateCorporateOrder_Request = DgIntegration.DgValidateCorporateOrderService.Request;
using ValidateCorporateOrder_Response = DgIntegration.DgValidateCorporateOrderService.Response;

namespace DgIntegration.DgValidateCorporateOrderService
{
    public class ValidateCorporateOrderService
    {
        private HTTPRequest httpRequest;
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public string url;
        public string endpoint;
		private string username;
		private string password;
        private string section;
        private string hierarcy;
        private Guid recordId;

        private ValidateCorporateOrder_Request.Envelope param;
        private ValidateCorporateOrder_Response.Envelope response;
        private string errorResponse;

        public ValidateCorporateOrderService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
            
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "ValidateCorporateOrder");
            if(setup == null) {
                throw new Exception("ValidateCorporateOrder hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.username = setup.Authentication.Basic.Username;
            this.password = setup.Authentication.Basic.Password;
			
            this.httpRequest = new HTTPRequest(this.url, UserConnection);
        }

        public virtual async Task Request()
        {
            string res = string.Empty;
            this.errorResponse = string.Empty;
            this.response = null;

            if(this.recordId != null && this.recordId != Guid.Empty) {
                this.httpRequest.SetLogRecordId(this.recordId);
            }

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                string xml = HTTPRequest.XmlToString<ValidateCorporateOrder_Request.Envelope>(this.param);
                var req = await this.httpRequest
                    .SetLogName("Save CRM: ValidateCorporateOrder")
                    .SetLogSection("Submission")
                    .AddHeader("User-Agent", "DevDigi")
                .Post(endpoint, xml, ContentType.Xml);

                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    if(!string.IsNullOrEmpty(req.Error)) {
                        throw new Exception(req.Error);
                    }

                    if(!string.IsNullOrEmpty(req.Body)) {
                        throw new Exception(req.Body);
                    }

                    throw new Exception(req.StatusCode);
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
                this.response = HTTPRequest.XmlToObject<ValidateCorporateOrder_Response.Envelope>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }

        public virtual ValidateCorporateOrderService SetParam(string Xml)
        {
            try {
                return this.SetParam(HTTPRequest.XmlToObject<ValidateCorporateOrder_Request.Envelope>(Xml));   
            } catch (Exception e) {
                throw new Exception($"Xml is not valid: {e.Message}");
            }
        }

        public virtual ValidateCorporateOrderService SetParam(ValidateCorporateOrder_Request.Envelope Param)
        {
            this.param = Param;
            return this;
        }

        public virtual ValidateCorporateOrderService SetParam(Guid RecordId, string Hierarcy = "1")
        {
            this.recordId = RecordId;
            this.hierarcy = Hierarcy;

            this.param = BuildRequest();

            return this;
        }

        public virtual ValidateCorporateOrder_Request.Envelope GetRequest()
        {
            return this.param ?? null;
        }

        public virtual string GetStringRequest()
        {
            return HTTPRequest.XmlToString<ValidateCorporateOrder_Request.Envelope>(this.param);
        }

        public virtual ValidateCorporateOrder_Response.Envelope GetResponse()
        {
            return this.response ?? null;
        }

        public virtual string GetStringResponse()
        {
            return this.response == null ? 
                string.Empty : 
                HTTPRequest.XmlToString<ValidateCorporateOrder_Response.Envelope>(this.response);
        }

        public virtual bool IsSuccessResponse()
        {
            if(this.response == null || !string.IsNullOrEmpty(this.errorResponse)) {
                return false;
            }
            
            string status = this.response?.Header?.CSGHeader?.Status;
            return status == "Successful" ? true : false;
        }

        public virtual string GetErrorResponse()
        {
            if(this.response == null) {
                return this.errorResponse ?? string.Empty;
            }

            var csgHeader = this.response?.Header?.CSGHeader;
            string errorCode = csgHeader?.ErrorCode;
            string errorDescription = csgHeader?.ErrorDescription;
            string status = csgHeader?.Status;
            if(status != "Successful") {
                return $"{status} - {errorCode}: {errorDescription}";
            }

            return string.Empty;
        }

        protected virtual ValidateCorporateOrder_Request.Envelope BuildRequest()
        {
            var result = new ValidateCorporateOrder_Request.Envelope();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubmission");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            foreach (string col in GetQuery()) {
                columns.Add(col, esq.AddColumn(col));
            }

            var entity = esq.GetEntity(UserConnection, this.recordId);
            if(entity == null) {
                return null;
            }

            result.Header = new ValidateCorporateOrder_Request.Header() {
                Security = Helper.GenerateUsernameToken(this.username, this.password),
                CSGHeader = new ValidateCorporateOrder_Request.CSGHeader() {
                    SourceSystemID = "NCCF",
                    ReferenceID = Helper.GenerateReferenceId("NCCF"),
                    ChannelMedia = "SOAPUI",
                    BusinessUnit = "Digi"
                }
            };

            string dealerCode = GetValidInput(entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDealer.DgDealerID"].Name));
			string idType = entity.GetTypedColumnValue<string>(columns["DgIDType.Name"].Name);
			if(idType == "Armed Force") {
				idType += "s";
			}
			
            result.Body = new ValidateCorporateOrder_Request.Body() {
                ValidateCorporateOrderRequest = new ValidateCorporateOrder_Request.ValidateCorporateOrderRequest() {
                    OrderType = "21",
                    ValidateCriteria = new ValidateCorporateOrder_Request.ValidateCriteria() {
                        CreateCorporateCustomer = new ValidateCorporateOrder_Request.CreateCorporateCustomer() {
                            BusinessRegistrationNumber = GetValidInput(entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgBRN"].Name)),
                            Hierarchy = this.hierarcy,
                            IdType = GetValidInput(idType),
                            IdNumber = GetValidInput(entity.GetTypedColumnValue<string>(columns["DgIDNo"].Name)),
                            Nationality = "123",
                        }
                    },
                    Dealer = new ValidateCorporateOrder_Request.Dealer() {
                        DealerCode = dealerCode,
                        DealerUserId = dealerCode,
                    }
                }
            };

            return result;
        }

        public virtual List<string> GetQuery()
        {
            return new List<string>() {
                "DgCRMGroup.DgBRN",
                "DgIDType.Name",
                "DgIDNo",
                "DgCRMGroup.DgDealer.DgDealerID"
            };
        }

        public static string GetValidInput(string Value)
        {
            return string.IsNullOrEmpty(Value) ? "?" : HTTPRequest.EscapeXMLValue(Value);
        }
    }
}