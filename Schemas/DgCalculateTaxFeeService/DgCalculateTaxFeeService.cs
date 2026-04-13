using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Configuration;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using ISAHttpRequest.ISAHttpRequest;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using Newtonsoft.Json;
using CalculateTaxFeeRequest = DgIntegration.DgCalculateTaxFeeService.Request;
using CalculateTaxFeeResponse = DgIntegration.DgCalculateTaxFeeService.Response;
using System.Net.Http;
using ISAIntegrationSetup;

namespace DgIntegration.DgCalculateTaxFeeService
{
    public class CalculateTaxFeeService
    {
        private UserConnection userConnection;
        private UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }
        
        public string url { get; }
        public string endpoint { get; }
        private string section;
        private Guid recordId;
        private List<Guid> recordIdList;
        public List<Guid> LineDetailList;
        private List<CalculateTaxFeeResponse.Envelope> xmlResponse;
        private List<CalculateTaxFeeRequest.Envelope> xmlRequest;
        private HttpClient httpClient;
        private List<string> errorList;
        protected EntityCollection Entities;
        protected Dictionary<string, EntitySchemaQueryColumn> Columns;

        public CalculateTaxFeeService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;

           var setup = IntegrationSetup.Get(UserConnection, "CRM", "CalculateTaxFee");
            if(setup == null) {
                throw new Exception("CalculateTaxFee hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
            
            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(url);

            this.xmlResponse = new List<CalculateTaxFeeResponse.Envelope>();
        }

        public virtual async Task Request()
        {
            this.errorList = new List<string>();
            this.xmlResponse = new List<CalculateTaxFeeResponse.Envelope>();

            if(IsSingleRequest()) {
                this.xmlResponse.Add(await SingleRequest(this.xmlRequest.FirstOrDefault()));
                return;
            }

            if(this.xmlRequest.Count == 0) {
                throw new Exception("Param List is empty");
            }

            this.xmlResponse = await BatchRequest(this.xmlRequest);
            return;
        }

        public virtual string GetStringResponse()
        {
            return HTTPRequest.XmlToString<CalculateTaxFeeResponse.Envelope>(GetResponse());
        }

        public virtual CalculateTaxFeeResponse.Envelope GetResponse()
        {
            if(this.xmlResponse.Count == 0) {
                return null;
            }

            return this.xmlResponse.FirstOrDefault();
        }

        public virtual List<string> GetStringBatchResponse()
        {
            return GetBatchResponse()
                .Select(item => HTTPRequest.XmlToString<CalculateTaxFeeResponse.Envelope>(item))
                .ToList();
        }

        public virtual List<CalculateTaxFeeResponse.Envelope> GetBatchResponse()
        {
            return this.xmlResponse;
        }

        public virtual List<string> GetBatchError()
        {
            return this.errorList;
        }

        public virtual List<CalculateTaxFeeRequest.Envelope> GetParam()
        {
            return this.xmlRequest;
        }

        public virtual List<string> GetStringParam()
        {
            return GetParam()
                .Select(item => HTTPRequest.XmlToString<CalculateTaxFeeRequest.Envelope>(item))
                .ToList();
        }

        public virtual CalculateTaxFeeService SetParam(CalculateTaxFeeRequest.Envelope param)
        {
            this.xmlRequest = new List<CalculateTaxFeeRequest.Envelope>();
            this.xmlRequest.Add(param);
            return this;
        }

        public virtual CalculateTaxFeeService SetParam(List<CalculateTaxFeeRequest.Envelope> param)
        {
            this.xmlRequest = param;
            return this;
        }

        public virtual CalculateTaxFeeService SetParam(string xml)
        {
            SetParam(HTTPRequest.XmlToObject<CalculateTaxFeeRequest.Envelope>(xml));
            return this;
        }

        public virtual CalculateTaxFeeService SetParam(string Section, Guid RecordId)
        {
            this.section = Section;
            this.recordId = RecordId;

            if((Section != "DgSubmission" && Section != "Submission") && (Section != "DgLineDetail" && Section != "Line Detail")) {
                throw new Exception($"Section {Section} is not support");
            }

            this.xmlRequest = GetRequest();

            return this;
        }

        public virtual CalculateTaxFeeService SetParam(List<Guid> LineDetailListId)
        {
            this.section = "DgLineDetail";
            this.recordIdList = LineDetailListId;
            this.xmlRequest = GetRequest();

            return this;
        }

        protected virtual bool IsSingleRequest()
        {
            return this.xmlRequest != null && this.xmlRequest.Count == 1 ? true : false;
        }

        protected virtual async Task<CalculateTaxFeeResponse.Envelope> SingleRequest(CalculateTaxFeeRequest.Envelope param)
        {
            var httpRequest = new HTTPRequest(this.httpClient, UserConnection);
            if(!string.IsNullOrEmpty(this.section)) {
                httpRequest.SetLogSection(this.section);
            }

            if(this.recordId != null && this.recordId != Guid.Empty) {
                httpRequest.SetLogRecordId(this.recordId);
            }

            var req = await httpRequest
                .SetLogName("Calculate Tax Fee")
                .Post(this.endpoint, HTTPRequest.XmlToString<CalculateTaxFeeRequest.Envelope>(param), ContentType.Xml);
            
            if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                throw new Exception("Request Calculate Tax Fee Error: "+req.Error);
            }

            if(string.IsNullOrEmpty(req.Body)) {
                throw new Exception("Response is empty");
            }

            return HTTPRequest.XmlToObject<CalculateTaxFeeResponse.Envelope>(req.Body);
        }

        protected virtual async Task<List<CalculateTaxFeeResponse.Envelope>> BatchRequest(List<CalculateTaxFeeRequest.Envelope> param)
        {
            var result = new List<CalculateTaxFeeResponse.Envelope>();

            var tasks = new List<Task<CalculateTaxFeeResponse.Envelope>>();
            foreach (var item in param) {
                tasks.Add(SingleRequest(item));
            }
            
            bool isSuccess = true;
            var whenAllTask = Task.WhenAll(tasks);
            try {
                foreach(var res in await whenAllTask) {
                    result.Add(res);
                }
            } catch (Exception e) {
                var errors = whenAllTask.Exception?.InnerExceptions;
                if(errors == null) {
                    throw e;
                }

                foreach (var err in errors) {
                    this.errorList.Add(err.Message);
                }
            }

            if(!isSuccess) {
                var complete = tasks
                    .Select((t, i) => new {
                        index = i,
                        task = t
                    })
                    .Where(t => t.task.Status == TaskStatus.RanToCompletion && t.task.Exception == null)
                    .ToList();

                return complete.Select(el => el.task.Result).ToList();
            }

            return result;
        }

        protected string GenerateTransactionId()
        {
            string transactionId = string.Empty;
            try {
                DateTime now = DateTime.UtcNow;
                var myTZ = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                var nowTZ = TimeZoneInfo.ConvertTimeFromUtc(now, myTZ);

                string accessChannel = "10510";
                string dateTime = nowTZ.ToString("yyMMddHHmmss");
                string random = new Random().Next(0, 999).ToString("000");
                string random2 = new Random().Next(0, 9).ToString("0");

                transactionId = accessChannel + dateTime + random + random2;
            }
            catch(Exception e) {
                throw new Exception(e.Message);
            }

            return transactionId;
        }

        protected virtual List<CalculateTaxFeeRequest.Envelope> GetRequest()
        {
            var result = new List<CalculateTaxFeeRequest.Envelope>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgFeeDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("feeItemCode", esq.AddColumn("DgFeeItemCode"));
            columns.Add("feeAmt", esq.AddColumn("DgFeeAmount"));
            columns.Add("LineDetailId", esq.AddColumn("DgLineDetail.Id"));
            // columns.Add("dealerCode", esq.AddColumn("DgFeeItemCode"));

            if(this.section == "DgSubmission" || this.section == "Submission") {
                columns.Add("lineDetailNo", esq.AddColumn("DgLineDetail.DgNo"));
                columns["lineDetailNo"].OrderByAsc(0);

                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail.DgSubmission", this.recordId));
            } else if(this.section == "DgLineDetail" || this.section == "Line Detail") {
                if(this.recordId != null && this.recordId != Guid.Empty) {
                    esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", this.recordId));
                } else if(this.recordIdList != null && this.recordIdList.Count > 0) {
                    var filterIdGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
                    foreach (var lineDetailId in this.recordIdList) {
                        filterIdGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineDetail", lineDetailId));
                    }
                    esq.Filters.Add(filterIdGroup);
                }
            }

            var entities = esq.GetEntityCollection(UserConnection);
            if(entities.FirstOrDefault() == null) {
                throw new Exception("Fee detail is not found");
            }

            this.Entities = entities;
            this.Columns = columns;
            this.LineDetailList = new List<Guid>();

            foreach (var entity in entities) {
                this.LineDetailList.Add(entity.GetTypedColumnValue<Guid>(columns["LineDetailId"].Name));

                result.Add(new CalculateTaxFeeRequest.Envelope() {
                    Body = new CalculateTaxFeeRequest.Body() {
                        calculateTaxFee = new CalculateTaxFeeRequest.calculateTaxFee() {
                            AccessSessionRequest = new CalculateTaxFeeRequest.AccessSessionRequest() {
                                accessChannel = "10510",
                                operatorCode = "NCCF",
                                password = "PkzVHH0odLylDCRIPJM+Mw==",
                                beId = "102",
                                transactionId = GenerateTransactionId(),
                            },
                            CalculateTaxFeeRequest = new CalculateTaxFeeRequest.CalculateTaxFeeRequest() {
                                feeItemCode = entity.GetTypedColumnValue<string>(columns["feeItemCode"].Name),
                                feeAmt = entity.GetTypedColumnValue<decimal>(columns["feeAmt"].Name).ToString(),
                            }
                        }
                    }
                });              
            }

            return result;
        }
    }
}