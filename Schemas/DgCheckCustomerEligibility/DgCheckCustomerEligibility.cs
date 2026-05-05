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
using System.Linq;
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
using ISAHttpRequest.ISAIntegrationLogService;
using System.Text.RegularExpressions;
using ISAHttpRequest.ISAHttpRequest;
using CoreEntityHelper = ISAEntityHelper.EntityHelper.EntityHelper;
using DgIntegration.DgEnrollDeviceService;
using DgIntegration.DgOverrideDeviceService;
using DgIntegration.DgVoidDeviceService;
using DgIntegration.DgShowOrderDetailsService;
using DgIntegration.DgCheckTransactionStatusService;
using DgSubmission.DgHistorySubmissionService;
using SolarisCore;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgCheckCustomerEligibility
{
    public class CustomerEligibilityService
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection
        {
            get
            {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

        public CustomerEligibilityService(UserConnection userConnection_ = null, bool isCelcom_ = false)
        {
            userConnection = userConnection_;
        }

        public async Task<CustomResponse> CheckCustomerEligibility(CustomerEligibilityRequest customerEligibilityRequest)
        {
            CustomResponse response = new CustomResponse();
            var log = new SolarLog(UserConnection, "CUSTOMER ELIGIBILITY CHECK", SolarLog.OUTGOING);
            var dbExecutor = UserConnection.EnsureDBConnection();
            var submissionId = Guid.Parse(customerEligibilityRequest.SubmissionId);
            var token = SysSettings.GetValue<string>(UserConnection, "DgCustomerEligibilityToken", "");
            var endpoint = SysSettings.GetValue<string>(UserConnection, "DgCustomerEligibilityURL", "");
            string url = $"{endpoint}?OrderType={customerEligibilityRequest.OrderType}&IdType={customerEligibilityRequest.IdType}&IdNumber={customerEligibilityRequest.IdNumber}&Nationality={customerEligibilityRequest.Nationality}&SubscriberType={customerEligibilityRequest.SubscriberType}&TelecomType={customerEligibilityRequest.TelecomType}&PayType={customerEligibilityRequest.PayType}&DateOfBirth={customerEligibilityRequest.DateOfBirth}&MSISDN={customerEligibilityRequest.MSISDN}";
            IntegrationLogService ISAintegrationLogService = new IntegrationLogService("CUSTOMER ELIGIBILITY CHECK", url, endpoint, userConnection: UserConnection);
            ISAintegrationLogService.AddStartDate();
            ISAintegrationLogService.AddMethod(HttpMethod.Post);
            try
            {
                dbExecutor.StartTransaction();
                var result = String.Empty;
                var responseMessage = new HttpResponseMessage();

                using (HttpClient client = new HttpClient())
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                    requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    requestMessage.Headers.Add("ReferenceID", Helper.GenerateReferenceId());
                    requestMessage.Headers.Add("SourceSystemID", "NCCF");
                    requestMessage.Headers.Add("ChannelMedia", "NCCF2.0");

                    var headersDictionary = requestMessage.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                    ISAintegrationLogService.AddHeadersRequest(headersDictionary);

                    var queryStrings = requestMessage.RequestUri.Query.TrimStart('?')
                                          .Split('&')
                                          .Select(q => q.Split('='))
                                          .ToDictionary(k => k[0], v => v.Length > 1 ? v[1] : string.Empty);
                    ISAintegrationLogService.AddQueryStringsRequest(queryStrings);

                    await log.AddOutgoingRequest(url, requestMessage);
                    responseMessage = await client.SendAsync(requestMessage);
                    await log.AddOutgoingResponse(responseMessage);
                    ISAintegrationLogService.AddHeadersResponse(responseMessage.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)));
                    responseMessage.EnsureSuccessStatusCode();
                    result = await responseMessage.Content.ReadAsStringAsync();

                    if (result != null)
                    {
                        response.Status = "Success";
                        response.Message = "";
                        var headers = JsonConvert.SerializeObject(responseMessage.Headers.ToDictionary(h => h.Key, h => h.Value.ToList().FirstOrDefault()));
                        response.Data = JsonConvert.DeserializeObject<CustomerEligibilityResponseHeaders>(headers);
                        ISAintegrationLogService.AddBodyResponse(result);
                        ISAintegrationLogService.AddStatusCodeResponse((int)responseMessage.StatusCode);

                        var customerBody = JsonConvert.DeserializeObject<CustomerEligibilityBody>(result);
                        response.CustomerEligibilityBody = customerBody;

                        log.Message = response.Message;

                        if (string.IsNullOrEmpty(customerEligibilityRequest.SubmissionId) || !Guid.TryParse(customerEligibilityRequest.SubmissionId, out var parsedSubmissionId) || parsedSubmissionId == Guid.Empty)
                        {
                            throw new Exception("SubmissionID is null, empty, invalid, or an empty GUID.");
                        }
                        var message = response.Data.ErrorCode == string.Empty ? "Validated" : $"{response.Data.ErrorDescription} {response.Data.MoreInfo}";
                        var submissionData = GetSubmissionData(submissionId);
                        if (submissionData == null || submissionData.Count == 0)
                        {
                            throw new Exception("Submission not found");
                        }
                        var userId = UserConnection.CurrentUser.ContactId;
                        var historyMessage = $"[Customer Eligibility Check] Submission {submissionData["DgName"]}, {message} ";
                        InsertSubmissionHistory(submissionId, userId, historyMessage, customerEligibilityRequest.MSISDN);
                        InsertIntegrationLog(historyMessage, response.Data.ErrorCode == string.Empty, submissionId);
                    }
                    else
                    {
                        throw new Exception($"No response from {url}");
                    }
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failed";
                response.Message = ex.Message;
                log.Message = response.Message;
                InsertIntegrationLog(response.Message, false, submissionId);
            }
            log.Save();
            dbExecutor.CommitTransaction();
            ISAintegrationLogService.AddMessage(response.Message);
            ISAintegrationLogService.AddStatusResponse(response.Status);
            ISAintegrationLogService.AddEndDate();
            ISAintegrationLogService.Save();
            return response;
        }

        public void InsertSubmissionHistory(Guid SubmissionId, Guid UserId, string Remark, string MSISDN = "", int LineId = 0)
        {
            HistorySubmissionService.InsertHistory
            (
                UserConnection: this.UserConnection,
                SubmissionId: SubmissionId,
                CreatedById: UserId,
                OpsId: OpsEnum.UPDATE,
                SectionId: SectionEnum.UPDATE,
                Remark: Remark,
                MSISDN: MSISDN
            );
        }

        public void InsertIntegrationLog(string Name, bool IsSuccess, Guid SubmissionId = default)
        {
            var insert = new Insert(this.UserConnection)
            .Into("DgIntegrationLog")
            .Set("CreatedOn", Column.Parameter(DateTime.Now))
            .Set("CreatedById", Column.Parameter(this.UserConnection.CurrentUser.Id))
            .Set("DgName", Column.Parameter(Name))
            .Set("DgSuccess", Column.Parameter(IsSuccess));

            if (SubmissionId != default)
            {
                insert.Set("DgSubmissionId", Column.Parameter(SubmissionId));
            }
            insert.Execute();
        }

        public Dictionary<string, string> GetSubmissionData(Guid submissionId)
        {
            var select = new Select(UserConnection)
                .Column("DgName")
                .From("DgSubmission")
                .Where("Id").IsEqual(Column.Parameter(submissionId)) as Select;

            var data = new Dictionary<string, string>();
            using (var dbExecutor = UserConnection.EnsureDBConnection())
            {
                using (var reader = select.ExecuteReader(dbExecutor))
                {
                    if (reader.Read())
                    {
                        data["DgName"] = reader.GetColumnValue<string>("DgName").ToString();
                    }
                }
            }

            return data;
        }
    }

    public class CustomResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public CustomerEligibilityResponseHeaders Data { get; set; }
        public CustomerEligibilityBody CustomerEligibilityBody { get; set; }
    }

    public class CustomerEligibilityRequest
    {
        public string OrderType { get; set; }
        public string IdType { get; set; }
        public string IdNumber { get; set; }
        public string Nationality { get; set; }
        public string SubscriberType { get; set; }
        public string TelecomType { get; set; }
        public string PayType { get; set; }
        public string DateOfBirth { get; set; }
        public string MSISDN { get; set; }
        public string SubmissionId { get; set; }
    }

    public class CustomerEligibilityResponseHeaders
    {
        public string ErrorDescription { get; set; }
        public string Status { get; set; }
        public string LogPoint { get; set; }
        public string GUID { get; set; }
        public string Severity { get; set; }
        public string UserMessage { get; set; }
        public string MoreInfo { get; set; }
        public string SourceSystemID { get; set; }
        public string Date { get; set; }
        public string Server { get; set; }
        public string ChannelMedia { get; set; }
        public string FreeText { get; set; }
        public string TargetService { get; set; }
        public string ReferenceID { get; set; }
        public string ServiceName { get; set; }
        public string BusinessUnit { get; set; }
        public string ErrorCode { get; set; }
        public string PrimaryKeyforRequest { get; set; }
    }

    public static class OpsEnum
    {
        public static readonly Guid DOA = new Guid("1582BEB1-6689-4649-ABDB-3C03711DB482");
        public static readonly Guid ADDVPN = new Guid("B4E1A7E2-47AF-453C-8835-99D0DC6B6DCF");
        public static readonly Guid UPDATE = new Guid("BB2C3EA4-90BC-4CF4-8EE7-97A57EB9E0D7");
        public static readonly Guid ADD = new Guid("E9AEAAC3-B33B-4A8E-BB04-9C65147DBED0");
        public static readonly Guid CANCEL = new Guid("F1C32E0D-FB6E-47C5-8439-F9140E766671");
    }

    public static class SectionEnum
    {
        public static readonly Guid eCRAv2 = new Guid("09F5B1AC-C732-4BF5-A12F-27F4F871208F");
        public static readonly Guid ACTIVATION = new Guid("10174DDB-A951-482F-A231-1D9AE0065788");
        public static readonly Guid CRAHeaderMESAD = new Guid("1588E6B9-C99E-44F2-AE7F-AF04E61B981E");
        public static readonly Guid CREDITRISK = new Guid("2EB23B05-20C0-411E-B66E-3E6E9249A71E");
        public static readonly Guid eCRA = new Guid("3E14B5C7-2693-4983-ADBF-3EEDE30853D0");
        public static readonly Guid ATTACHMENT = new Guid("53ED35FF-2E7C-41F2-8405-A89804D12477");
        public static readonly Guid CRALINEMESAD = new Guid("5EE30881-CB66-4B75-AB91-DC45C26E2D99");
        public static readonly Guid UPDATE = new Guid("71F19DE8-470F-498F-AC47-97DA125AFC9D");
        public static readonly Guid eIRA = new Guid("7549CB02-F14C-43DB-8D20-2888DCD1B4FC");
        public static readonly Guid NCCF = new Guid("798174F1-7D6D-4196-9D94-0DCD0C9D53C2");
        public static readonly Guid OPERATION = new Guid("92B6423B-25E5-404C-87EE-B95F3652D32B");
        public static readonly Guid LMS = new Guid("A1008903-15D6-49D5-9BC6-8EA4AE23C250");
        public static readonly Guid CRALINE = new Guid("A9A79979-0299-4267-B5BA-45775054C8D9");
        public static readonly Guid ReleasedToMesad = new Guid("C2832499-8691-4304-9A7F-7AEBF49B28C6");
        public static readonly Guid ReleasedToOFS = new Guid("D0AC5C31-3EF6-4B14-A37B-D1011922C014");
        public static readonly Guid ADMIN = new Guid("EB309E0F-914C-4C60-8F40-4C0EA0E7BCC3");
        public static readonly Guid SFA = new Guid("ED2A60EC-8A82-4B1C-83F3-4984D2F8F288");
    }

    public class CustomerEligibilityBody
    {
        public CheckCustomerEligibilityResponse CheckCustomerEligibilityResponse { get; set; }
    }

    public class CheckCustomerEligibilityResponse
    {
        public ValidationResultList ValidationResultList { get; set; }
    }

    public class ValidationResultList
    {
        public List<ValidationResultRecord> ValidationResultRecord { get; set; }
    }

    public class ValidationResultRecord
    {
        public string MessageCode { get; set; }
        public string RuleChecked { get; set; }
        public string MessageDescription { get; set; }
        public string RuleType { get; set; }
    }

    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class CustomerEligibilityWebService : BaseService
    {
        private readonly CustomerEligibilityService _service;

        public CustomerEligibilityWebService()
        {
            _service = new CustomerEligibilityService(UserConnection);
        }

        [OperationContract]
        [WebInvoke(Method = "POST",
               UriTemplate = "CheckCustomerEligibility",
               RequestFormat = WebMessageFormat.Json,
               ResponseFormat = WebMessageFormat.Json,
               BodyStyle = WebMessageBodyStyle.Bare)]
        public async Task<CustomResponse> CheckCustomerEligibility(CustomerEligibilityRequest request)
        {
            var checkEligibility = SysSettings.GetValue<bool>(UserConnection, "DgCustomerEligibilityCheck", false);

            if (!checkEligibility) {
                return new CustomResponse {
                    Status = "Skip",
                    Message = "Skip Check Eligibility Process"
                };
            }

            return await _service.CheckCustomerEligibility(request);
        }
    }

    // public static async Task Main(string[] args)
    // {
    // var json = @"{
    //     'OrderType': 'Upgrade',
    //     'IdType': 'NationalID',
    //     'IdNumber': 'N123456789',
    //     'Nationality': 'CA',
    //     'SubscriberType': 'Corporate',
    //     'TelecomType': 'FixedLine',
    //     'PayType': 'Postpaid',
    //     'DateOfBirth': '1990-05-15',
    //     'MSISDN': '1234567890',
    //     'SubmissionId': 'd2719b6e-8c6e-4d8f-9b2d-1f2e4d6e5b8f'
    // }";

    //     var request = JsonConvert.DeserializeObject<CustomerEligibilityRequest>(json);

    //     var service = new CustomerEligibilityWebService();
    //     var response = await service.CheckCustomerEligibility(request);

    //     Console.WriteLine($"Status: {response.Status}");
    //     Console.WriteLine($"Message: {response.Message}");
    //     if (response.Data != null)
    //     {
    //         Console.WriteLine($"Data: {JsonConvert.SerializeObject(response.Data, Formatting.Indented)}");
    //     }
    // }
}