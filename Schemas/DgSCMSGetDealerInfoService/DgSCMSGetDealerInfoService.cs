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
using System.Xml.Linq;
using System.Xml;
using ISAIntegrationSetup;

namespace DgIntegration.DgSCMSGetDealerInfoService
{
    public class SCMSGetDealerInfoService
    {
		private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private HTTPRequest httpRequest;
        private string url;
        private string endpoint;
		public SCMSGetDealerInfoService(UserConnection userConnection_ = null) 
        {
        	if(userConnection_ != null) {
				userConnection = userConnection_;
			}

            var setup = IntegrationSetup.Get(UserConnection, "SCMS", "SCMSDealer");
            if(setup == null) {
                throw new Exception("SCMSDealer hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
            string pathCertificate = (string)SysSettings.GetValue(UserConnection, "DgFilePathCertificateSCMS");

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            X509Certificate2 certificate = new X509Certificate2(pathCertificate);

            var handler = new HttpClientHandler
            {
                ClientCertificates = { certificate },
            };
            this.httpRequest = new HTTPRequest(this.url, userConnection_, handler);
        }

        public async Task<GeneralResponse> GetDealerInfo(string DealerCode)
        {
            var result = new GeneralResponse();

            try {
                var emailFromNCCF = GetEmailFromNCCF(DealerCode);

                if (string.IsNullOrEmpty(emailFromNCCF)) {
                    var getEmailFromDealerInfo = await GetEmailFromDealerInfo(DealerCode);

                    if (!string.IsNullOrEmpty(getEmailFromDealerInfo.Message)) 
                    {
                        UpdateEmailDealer(DealerCode, getEmailFromDealerInfo.Message);
                    }

                    return new GeneralResponse() {
                        Message = !string.IsNullOrEmpty(getEmailFromDealerInfo.Message) ? getEmailFromDealerInfo.Message : "Email does not exist, please check Dealer Id.",
                        Success = !string.IsNullOrEmpty(getEmailFromDealerInfo.Message) ? true : false
                    };
                }

                result.Message = emailFromNCCF;
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        protected virtual string GetEmailFromNCCF(string DealerCode)
        {
            var result = String.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDealer");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("DealerEmail", esq.AddColumn("DgDealerEmail"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgDealerID", DealerCode));
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result = entity.GetTypedColumnValue<string>(columns["DealerEmail"].Name);
            }

            return result;
        }

        protected virtual async Task<GeneralResponse> GetEmailFromDealerInfo(string DealerCode)
        {
            var result = new GeneralResponse();

            try {
                var req = await this.httpRequest
                    .SetLogName($"SCMS: Get Dealer Info")
                    .SetLogSection("Get Dealer Info")
                    .AddQuery("DealerCode", DealerCode)
                    .AddQuery("Brnch", String.Empty)
                    .AddQuery("Status", String.Empty)
                    .AddQuery("LastModifiedDate", String.Empty)
                .Get(this.endpoint);

                XDocument xmlDoc = XDocument.Parse(req.Body);
                string jsonResponse = JsonConvert.SerializeXNode(xmlDoc);
                var cleanResponseAsJson = Regex.Replace(jsonResponse, ":xsd|#|@xml","");
                var scmsDealerInfoResponse = JsonConvert.DeserializeObject<ResponseSCMSDealerInfo>(cleanResponseAsJson);

                result.Message = scmsDealerInfoResponse.ArrayOfSCMSInfoNS.SCMSInfoNS.Email.text;
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        protected void UpdateEmailDealer(string DealerCode, string Email)
        {
            var update = new Update(UserConnection, "DgDealer")
                .Set("DgDealerEmail", Column.Parameter(Email))
                .Where("DgDealerID").IsEqual(Column.Parameter(DealerCode));
            update.Execute();
        }
	}

    public class ResponseSCMSDealerInfo
    {
        public ArrayOfSCMSInfoNS ArrayOfSCMSInfoNS { get; set; }
    }

    public class ArrayOfSCMSInfoNS
    {
        public SCMSInfoNS SCMSInfoNS { get; set; }
    }

    public class SCMSInfoNS
    {
        public Email Email { get; set; }
    }

    public class Email
    {
        public string text { get; set; }
    }
}