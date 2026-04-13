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
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using ISAHttpRequest.ISAHttpRequest;
using ISAIntegrationSetup;

namespace DgIntegration.DgRejectCRAndOPService
{
    public class RejectCRAndOPService
    {
        private CreatioIntegration creatio;
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        
        private string url { get; }
        public string endpoint { get; }
        private string userName { get; }
        private string userPassword { get; }
        public RejectCRAndOPService(UserConnection userConnection_ = null) 
        {
        	if(userConnection_ != null) {
				userConnection = userConnection_;
			}

            var setup = IntegrationSetup.Get(UserConnection, "SFA", "CROPReject");
            if(setup == null) {
                throw new Exception("CROPReject hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.userName = setup.Authentication.Basic.Username;
			this.userPassword = setup.Authentication.Basic.Password;;

            this.creatio = new CreatioIntegration(this.url, this.userName, this.userPassword, UserConnection);
        }

        public async Task<GeneralResponse> SendCRAndORRejectStatus(Guid SubmissionId, string Type)
        {
            var result = new GeneralResponse();

            try {
                var requestList = new List<RequestList>();
                
                if (Type == "CR") {
                    requestList = GetMessageCR(SubmissionId);
                } else if (Type == "OP") {
                    requestList = GetMessageOP(SubmissionId);
                } else {
                    throw new Exception("Type not found!, there only CR and OP.");
                }

                var uriAPI = $"{url}/0/rest/LMSAPI/CROPReject";
                var data = new RequestDataCRandOP() {
                    Type = Type,
                    RequestList = requestList
                };

                var req = await this.creatio
                    .Rest("POST",  this.endpoint, data);

                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    throw new Exception(req.Error ?? req.StatusCode);
                }

                if(string.IsNullOrEmpty(req.Body)) {
                    throw new Exception("Response is empty");
                }

                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        public List<RequestList> GetMessageCR(Guid SubmissionId) 
        {
            var result = new List<RequestList>();
                var select = new Select(UserConnection)
                    .Column("DgSubmission", "DgSerialNumber").As("SerialNumber")
                    .Column("DgSubmission", "DgCRRemark").As("RejectMessage")
					.Column("CRStatus", "Name").As("Status")
                .From("DgSubmission")
				.Join(JoinType.LeftOuter, "DgApproval").As("CRStatus")
                    .On("CRStatus", "Id").IsEqual("DgSubmission", "DgApprovalId")
                .Where("DgSubmission", "Id").IsEqual(Column.Parameter(SubmissionId)) as Select;
            
                using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) 
                {
                    using (IDataReader dataReader = select.ExecuteReader(dbExecutor))
                    {
                        while (dataReader.Read()) 
                        {
                            var data = new RequestList();

                            data.SerialNumber = dataReader.GetColumnValue("SerialNumber") != null ? 
                                dataReader.GetColumnValue("SerialNumber").ToString() : String.Empty;

                            data.RejectMessage = dataReader.GetColumnValue("RejectMessage") != null ? 
                                dataReader.GetColumnValue("RejectMessage").ToString() : String.Empty;

                            data.Status = dataReader.GetColumnValue<string>("Status");

                            result.Add(data);
                        }
                    }
                }
                
          	return result;
        }

        public List<RequestList> GetMessageOP(Guid SubmissionId) 
        {
            var result = new List<RequestList>();
                var select = new Select(UserConnection)
                    .Column("DgSubmission", "DgSerialNumber").As("SerialNumber")
                    .Column("DgSubmission", "DgOPRemark").As("RejectMessage")
					.Column("OPStatus", "Name").As("Status")
                .From("DgSubmission")
				.Join(JoinType.LeftOuter, "DgProgressStatus").As("OPStatus")
                    .On("OPStatus", "Id").IsEqual("DgSubmission", "DgProgressStatusId")
                .Where("DgSubmission", "Id").IsEqual(Column.Parameter(SubmissionId)) as Select;
            
                using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) 
                {
                    using (IDataReader dataReader = select.ExecuteReader(dbExecutor))
                    {
                        while (dataReader.Read()) 
                        {
                            var data = new RequestList();

                            data.SerialNumber = dataReader.GetColumnValue("SerialNumber") != null ? 
                                dataReader.GetColumnValue("SerialNumber").ToString() : String.Empty;

                            data.RejectMessage = dataReader.GetColumnValue("RejectMessage") != null ? 
                                dataReader.GetColumnValue("RejectMessage").ToString() : String.Empty;

                            data.Status = dataReader.GetColumnValue<string>("Status");

                            result.Add(data);
                        }
                    }
                }
                
          	return result;
        }
    }

    public class RequestList
    {
        public string SerialNumber { get; set; }
        public string RejectMessage { get; set; }
        public string Status { get; set; }
    }

    public class RequestDataCRandOP
    {
        public string Type { get; set; }
        public List<RequestList> RequestList { get; set; }
    }
}