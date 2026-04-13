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
using DgCRMIntegration;
using ISAIntegrationSetup;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using ValidateCorporatePortIn_Request = DgIntegration.DgValidateCorporatePortInService.Request;
using ValidateCorporatePortIn_Response = DgIntegration.DgValidateCorporatePortInService.Response;

namespace DgIntegration.DgValidateCorporatePortInService
{
    public class ValidateCorporatePortInService
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
        private List<Guid> recordIds;
        private string portInMessageId;

        private ValidateCorporatePortIn_Request.Envelope param;
        private ValidateCorporatePortIn_Response.Envelope response;
        private string errorResponse;
		private ISAHttpRequest.ISAIntegrationLogService.IntegrationLog log;

        public ValidateCorporatePortInService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
            
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "ValidateCorporatePortIn");
            if(setup == null) {
                throw new Exception("ValidateCorporatePortIn hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.username = setup.Authentication.Basic.Username;
			this.password = setup.Authentication.Basic.Password;

            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(this.url);
            this.portInMessageId = GenerateMessageId();
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

                string xml = HTTPRequest.XmlToString<ValidateCorporatePortIn_Request.Envelope>(this.param);
                var req = await _httpRequest
                    .SetLogName("Validate Corporate Port In")
					.AddHeader("User-Agent", "NCCFV2")
                .Post(endpoint, xml, ContentType.Xml);
				
				this.log = _httpRequest.GetLog();

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
                this.response = HTTPRequest.XmlToObject<ValidateCorporatePortIn_Response.Envelope>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }

        public virtual ValidateCorporatePortInService SetParam(string Xml)
        {
            try {
                return this.SetParam(HTTPRequest.XmlToObject<ValidateCorporatePortIn_Request.Envelope>(Xml));   
            } catch (Exception e) {
                throw new Exception($"Xml is not valid: {e.Message}");
            }
        }

        public virtual ValidateCorporatePortInService SetParam(ValidateCorporatePortIn_Request.Envelope Param)
        {
            this.param = Param;
            return this;
        }

        public virtual ValidateCorporatePortInService SetParam(Guid RecordId)
        {
            this.section = "DgLineDetail";
            this.recordId = RecordId;

            this.param = BuildRequest();
            return this;
        }

        public virtual ValidateCorporatePortInService SetParam(List<Guid> RecordIds)
        {
            this.section = "DgLineDetail";
            this.recordIds = RecordIds;

            this.param = BuildRequest();
            return this;
        }

        public virtual ValidateCorporatePortIn_Request.Envelope GetRequest()
        {
            return this.param ?? null;
        }

        public virtual string GetStringRequest()
        {
            return this.param == null ? 
                string.Empty : 
                HTTPRequest.XmlToString<ValidateCorporatePortIn_Request.Envelope>(this.param);
        }

        public virtual ValidateCorporatePortIn_Response.Envelope GetResponse()
        {
            return this.response ?? null;
        }

        public virtual string GetStringResponse()
        {
            return this.response == null ? 
                string.Empty : 
                HTTPRequest.XmlToString<ValidateCorporatePortIn_Response.Envelope>(this.response);
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
		
		public ISAHttpRequest.ISAIntegrationLogService.IntegrationLog GetLog()
		{
			return this.log ?? null;
		}

        public string GetPortMessageId() 
        {
            return this.portInMessageId;
        }

        protected virtual ValidateCorporatePortIn_Request.Envelope BuildRequest()
        {
			var crmService = new CRMService(UserConnection);
			
            var result = new ValidateCorporatePortIn_Request.Envelope();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("CustomerType", esq.AddColumn("DgSubmission.DgSubscriberType.Name"));
			columns.Add("CorporateName", esq.AddColumn("DgDNOCompanyName"));
            columns.Add("DonorBusinessRegistrationNumber", esq.AddColumn("DgDNOIdNo"));
            columns.Add("RecipientBusinessRegistrationNumber", esq.AddColumn("DgSubmission.DgCRMGroup.DgBRN"));
            columns.Add("Username", esq.AddColumn("DgUsername"));
            columns.Add("DonorNetworkOperator", esq.AddColumn("DgDNO.DgCSGCode"));
            columns.Add("IdType", esq.AddColumn("DgSubmission.DgIDType.Name"));
            columns.Add("IdNumber", esq.AddColumn("DgSubmission.DgIDNo"));
            columns.Add("CRMGroupId", esq.AddColumn("DgSubmission.DgCRMGroup.Id"));
            columns.Add("GroupID", esq.AddColumn("DgSubmission.DgCRMGroup.DgSubParentGroupID"));
			columns.Add("AccountCode", esq.AddColumn("DgDNOAccNo"));
			columns.Add("DNOIdType", esq.AddColumn("DgDNOIDType.Name"));
            
            bool isSingle = this.recordId != null && this.recordId != Guid.Empty;
            bool isMultiple = this.recordIds != null && this.recordIds.Count > 0;
            if(isSingle) {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordId));
            } else if(isMultiple) {
                var filterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
                foreach (Guid item in this.recordIds) {
                    filterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", item));
                }
                esq.Filters.Add(filterGroup);
            }
            
            var entities = esq.GetEntityCollection(UserConnection);
            var entity = entities.FirstOrDefault();
            if(entity == null) {
                return null;
            }

            string SubscriberType = entity.GetTypedColumnValue<string>(columns["CustomerType"].Name);
            string CorporateGroupId = entity.GetTypedColumnValue<string>(columns["GroupID"].Name);
            string MSISDN = entity.GetTypedColumnValue<string>(columns["MSISDN"].Name);
            string MSISDNType = "POSTPAID";
            string NumberType = "PRINCIPAL";
            string DonorNetworkOperator = entity.GetTypedColumnValue<string>(columns["DonorNetworkOperator"].Name);
            string ReceivedNetworkOperator = "1";
            string DNOIdType = entity.GetTypedColumnValue<string>(columns["DNOIdType"].Name);
            string CustomerType = DNOIdType == "BRN" ? "CORPORATE" : "INDIVIDUAL";
            string CustomerName = entity.GetTypedColumnValue<string>(columns["CorporateName"].Name);
            string IdType = entity.GetTypedColumnValue<string>(columns["IdType"].Name);
            string IdNumber = entity.GetTypedColumnValue<string>(columns["DonorBusinessRegistrationNumber"].Name);
            string CorporateName = entity.GetTypedColumnValue<string>(columns[SubscriberType == "CI" ? "CorporateName" : "Username"].Name);
            string DonorBusinessRegistrationNumber = entity.GetTypedColumnValue<string>(columns["DonorBusinessRegistrationNumber"].Name);
            string RecipientBusinessRegistrationNumber = entity.GetTypedColumnValue<string>(columns["RecipientBusinessRegistrationNumber"].Name);
            string AccountCode = entity.GetTypedColumnValue<string>(columns["AccountCode"].Name);

			if(string.IsNullOrEmpty(CorporateGroupId)) {
				var getCustomers = crmService.GetCustomersByBRN(RecipientBusinessRegistrationNumber).GetAwaiter().GetResult();
				if(getCustomers == null) {
					throw new Exception("Get Customer ID by BRN "+RecipientBusinessRegistrationNumber+" not found");
				}
				
				string customerId = getCustomers
					.Where(item => item.corporationInfo.hierarchy == "2")
					.Select(item => item.customerId)
					.FirstOrDefault() ?? string.Empty;
				if(string.IsNullOrEmpty(customerId)) {
					throw new Exception("Get Customer ID Hierary 2 by BRN "+RecipientBusinessRegistrationNumber+" not found");
				}
				
				var queryVPNs = crmService.QueryVPNGroupSubscriberByCustomerId(customerId).GetAwaiter().GetResult();
				CorporateGroupId = queryVPNs.FirstOrDefault()?.groupId ?? string.Empty;
				
				if(string.IsNullOrEmpty(CorporateGroupId)) {
					throw new Exception("Query VPN Group Subscriber: Group Id not found");
				}
			}

            if (string.IsNullOrEmpty(DNOIdType))
              throw new Exception("Donor ID Type cannot be null or empty.");

            if (string.IsNullOrEmpty(DonorBusinessRegistrationNumber))
                throw new Exception("Donor ID Number cannot be null or empty.");
     
            if (string.IsNullOrEmpty(DonorNetworkOperator))
                throw new Exception("Donor Network Operator cannot be null or empty.");

            if (DNOIdType == "3" && string.IsNullOrEmpty(AccountCode))
                throw new Exception("Donor Account Code cannot be null or empty.");

            if (string.IsNullOrEmpty(IdType))
                throw new Exception("Receiver ID Type cannot be null or empty.");

            if (string.IsNullOrEmpty(IdNumber))
                throw new Exception("Receiver ID Number cannot be null or empty.");

            if (string.IsNullOrEmpty(ReceivedNetworkOperator))
                throw new Exception("Received Network Operator cannot be null or empty.");

            if (string.IsNullOrEmpty(CorporateName))
                throw new Exception("Donor Company Name cannot be null or empty.");
            
            if (string.IsNullOrEmpty(RecipientBusinessRegistrationNumber))
                throw new Exception("Recipient BRN cannot be null or empty.");
			
			string headerTo = IntegrationSetup.GetCustomAuthValue(UserConnection, "CSG", "ValidateCorporatePortIn", "HeaderTo", "");
            result.Header = new ValidateCorporatePortIn_Request.Header() {
                Security = Helper.GenerateUsernameToken(this.username, this.password),
                CSGHeader = new ValidateCorporatePortIn_Request.CSGHeader() {
                    SourceSystemID = "NCCF",
                    ReferenceID = Helper.GenerateReferenceId("NCCF"),
                    ChannelMedia = "WEB",
                },
                Action = "urn:ValidateCorporatePortInOperation",
                MessageID = "urn:uuid:a51764a9-7edc-460a-8f7d-9bc647db6a61",
                ReplyTo = new ValidateCorporatePortIn_Request.ReplyTo() {
                    Address = "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous"
                },
                To = !string.IsNullOrEmpty(headerTo) ? headerTo : "http://10.89.249.37:16101/cxf/v1/ord/ValidateCorporatePortIn/"
            };
            result.Body = new ValidateCorporatePortIn_Request.Body() {
                ValidateCorporatePortInRequest = new ValidateCorporatePortIn_Request.ValidateCorporatePortInRequest() {
                    CorporateGroupId = new ValidateCorporatePortIn_Request.CorporateGroupId() {
                        Text = CorporateGroupId
                    },
                    MNPInformation = new ValidateCorporatePortIn_Request.MNPInformation() {
                        PortInTransactionId = GenerateTransactionId(),
                        PortInMessageId = this.portInMessageId,
                        DonorNetworkOperator = DonorNetworkOperator,
                        ReceivedNetworkOperator = ReceivedNetworkOperator,
                        CustomerType = new ValidateCorporatePortIn_Request.CustomerType() {
                            Text = CustomerType
                        },
                        Corporate = new ValidateCorporatePortIn_Request.Corporate() {
                            CorporateName = new ValidateCorporatePortIn_Request.CorporateName() {
                                Text = CustomerName
                            },
                            DonorBusinessRegistrationNumber = DonorBusinessRegistrationNumber,
                            RecipientBusinessRegistrationNumber = RecipientBusinessRegistrationNumber
                        }
                    }
                }
            };

            result.Body.ValidateCorporatePortInRequest.SubscriberList = new ValidateCorporatePortIn_Request.SubscriberList();
            result.Body.ValidateCorporatePortInRequest.SubscriberList.SubscriberRecord = new List<ValidateCorporatePortIn_Request.SubscriberRecord>();
            if(isSingle) {
                result.Body.ValidateCorporatePortInRequest.SubscriberList.SubscriberRecord.Add(new ValidateCorporatePortIn_Request.SubscriberRecord() {
                    MSISDN = new ValidateCorporatePortIn_Request.MSISDN() {
                        Text = Helper.GetValidMSISDN(MSISDN)
                    },
                    MSISDNType = MSISDNType,
                    NumberType = NumberType
                });
            } else if(isMultiple) {
                foreach (var item in entities) {
                    result.Body.ValidateCorporatePortInRequest.SubscriberList.SubscriberRecord.Add(new ValidateCorporatePortIn_Request.SubscriberRecord() {
                        MSISDN = new ValidateCorporatePortIn_Request.MSISDN() {
                            Text = Helper.GetValidMSISDN(MSISDN)
                        },
                        MSISDNType = MSISDNType,
                        NumberType = NumberType
                    });
                }
            }
			
			if(DNOIdType == "Armed Force") {
				DNOIdType += "s";
			}
			
            if(DNOIdType == "BRN") {
                result.Body.ValidateCorporatePortInRequest.MNPInformation.AccountCode = new ValidateCorporatePortIn_Request.AccountCode() {
                    Text = AccountCode
                };
            } else {
                result.Body.ValidateCorporatePortInRequest.MNPInformation.Individual = new ValidateCorporatePortIn_Request.Individual() {
                    CustomerName = HTTPRequest.EscapeXMLValue(CustomerName),
                    IdentificationList = new ValidateCorporatePortIn_Request.IdentificationList() {
                        IdentificationRecord = new ValidateCorporatePortIn_Request.IdentificationRecord() {
                            IdType = new ValidateCorporatePortIn_Request.IdType() {
                                Text = DNOIdType.Replace(" ", "").ToUpper()
                            },
                            IdNumber = new ValidateCorporatePortIn_Request.IdNumber() {
                                Text = IdNumber
                            }
                        }
                    }
                };
            }

            return result;
        }

        public static string GenerateTransactionId()
        {
            string transactionId = string.Empty;
            try {
                DateTime now = DateTime.UtcNow;
                var myTZ = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                var nowTZ = TimeZoneInfo.ConvertTimeFromUtc(now, myTZ);

                string dateTime = nowTZ.ToString("yyMMddHHmmss");
                string random = new Random().Next(0, 99).ToString("00");

                transactionId = "N" + dateTime + random;
            } catch (Exception e) {
                throw new Exception(e.Message);
            }

            return transactionId;
        }

        public static string GenerateMessageId()
        {
            string transactionId = string.Empty;
            try {
                DateTime now = DateTime.UtcNow;
                var myTZ = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                var nowTZ = TimeZoneInfo.ConvertTimeFromUtc(now, myTZ);

                string dateTime = nowTZ.ToString("yyyyMMddHHmmss");
                string random = new Random().Next(0, 999).ToString("000");
                string random2 = new Random().Next(0, 9).ToString("0");

                transactionId = dateTime + random + random2;
            } catch (Exception e) {
                throw new Exception(e.Message);
            }

            return transactionId;
        }

        public static ValidateCorporatePortIn_Request.Envelope GetDefaultRequest(UserConnection UserConnection)
        {
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "ValidateCorporatePortIn");
            if(setup == null) {
                throw new Exception("ValidateCorporatePortIn hasn't been set up for integration");
            }
            
			var username = setup.Authentication.Basic.Username;
			var password = setup.Authentication.Basic.Password;

            string headerTo = IntegrationSetup.GetCustomAuthValue(UserConnection, "CSG", "ValidateCorporatePortIn", "HeaderTo", "");
            
            return new ValidateCorporatePortIn_Request.Envelope() {
                Header = new ValidateCorporatePortIn_Request.Header() {
                    Security = Helper.GenerateUsernameToken(username, password),
                    CSGHeader = new ValidateCorporatePortIn_Request.CSGHeader() {
                        SourceSystemID = "NCCF",
                        ReferenceID = Helper.GenerateReferenceId("NCCF"),
                        ChannelMedia = "WEB",
                    },
                    Action = "urn:ValidateCorporatePortInOperation",
                    MessageID = "urn:uuid:a51764a9-7edc-460a-8f7d-9bc647db6a61",
                    ReplyTo = new ValidateCorporatePortIn_Request.ReplyTo() {
                        Address = "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous"
                    },
                    To = !string.IsNullOrEmpty(headerTo) ? headerTo : "http://10.89.249.37:16101/cxf/v1/ord/ValidateCorporatePortIn/"
                }
            };
        }
    }
}