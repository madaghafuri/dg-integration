using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData;
using DgSubmission.DgLineDetail;
using DgCSGIntegration.DgOrderFees;
using DgIntegration.DgValidateCorporatePortInService;
using DgIntegration.DgConfirmPortInService;
using ISAEntityHelper.EntityHelper;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using LookupConst = DgMasterData.DgLookupConst;
using ValidateCorporatePortIn_Request = DgIntegration.DgValidateCorporatePortInService.Request;
using ValidateCorporatePortIn_Response = DgIntegration.DgValidateCorporatePortInService.Response;
using ConfirmPortIn_Response = DgIntegration.DgConfirmPortInService.Response;
using SolarisCore;
using System.Security.Cryptography;
using System.Numerics;

namespace DgCSGIntegration
{
    public class CSGService : BaseHttpRequest
    {
        public CSGService(UserConnection UserConnection) : base(UserConnection, BaseHttpRequest.GetBaseUrl(UserConnection, "CSG"))
        {
            this.UserConnection = UserConnection;
        }

        #region OrderFees

        public async Task<OrderFeesResponse> OrderFees(OrderFeesRequest Param)
        {
            var orderFees = new OrderFees(UserConnection);
            return await OrderFees(orderFees, Param);
        }

        public async Task<OrderFeesResponse> OrderFees(OrderFees OrderFees, OrderFeesRequest Param, string Section = "", Guid RecordId = default(Guid))
        {
            bool isSuccess = true;
            var logInfo = new LogInfo()
            {
                LogName = "Order Fees",
                Section = "CSG"
            };

            if (!string.IsNullOrEmpty(Section))
            {
                logInfo.Section += $" ({Section})";
            }

            if (RecordId != Guid.Empty)
            {
                logInfo.RecordId = RecordId.ToString();
            }

            string referenceID = GenerateReferenceId();

            var request = new HttpRequestMessage(HttpMethod.Post, OrderFees.EndpointUrl);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("SourceSystemID", "NCCF");
            request.Headers.Add("ReferenceID", referenceID);
            request.Headers.Add("ChannelMedia", "NCCF2.0");
            request.Headers.Add("Authorization", SolarRest.GenerateBasicAuth(OrderFees.Username, OrderFees.Password));
            request.Content = ConvertToStringContent(Param, SolarRest.JSON);

            var response = await SendRequest<OrderFeesResponse>(request, logInfo);
            try
            {
                string errorResponse = string.Empty;
                if (!OrderFees.IsSuccess(response.Headers))
                {
                    errorResponse = OrderFees.GetErrorResponse(response.Headers);
                    throw new Exception(!string.IsNullOrEmpty(errorResponse) ? errorResponse : $"{response.Message} - {response.StatusCode}: {response.StatusDescription}");
                }
            }
            catch (Exception e)
            {
                isSuccess = false;
                throw;
            }
            finally
            {
                LogHelper.LogACDCTracking(
                    UserConnection: UserConnection,
                    RequestBody: JsonConvert.SerializeObject(Param),
                    ResponseBody: response.RawBody,
                    OrderId: "",
                    TransactionID: referenceID,
                    TransactionType: "",
                    APIName: "Order Fees",
                    MSISDN: Param?.RetrieveFeesForOrderRequest?.MSISDN ?? string.Empty,
                    Status: isSuccess ? "SUCCESS" : "FAIL",
                    ContentType: "JSON"
                );
            }

            return response.Body;
        }

        #endregion

        #region ValidateCorporatePortIn

        public async Task<ValidateCorporatePortIn_Response.Header> ValidateCorporatePortIn(List<LineDetail> Lines, string PortInTransactionID, string PortInMessageID)
        {
            if (Lines == null || (Lines != null && Lines.Count == 0))
            {
                throw new Exception("No data cant be provision to CSG");
            }

            var line = Lines.FirstOrDefault();
            var param = ValidateCorporatePortInService.GetDefaultRequest(UserConnection);

            param.Body = new ValidateCorporatePortIn_Request.Body()
            {
                ValidateCorporatePortInRequest = new ValidateCorporatePortIn_Request.ValidateCorporatePortInRequest()
                {
                    CorporateGroupId = new ValidateCorporatePortIn_Request.CorporateGroupId()
                    {
                        Text = line.SubParentGroupID
                    },
                    MNPInformation = new ValidateCorporatePortIn_Request.MNPInformation()
                    {
                        PortInTransactionId = PortInTransactionID,
                        PortInMessageId = PortInMessageID,
                        DonorNetworkOperator = line.DNO?.CSGCode,
                        ReceivedNetworkOperator = "1",
                        CustomerType = new ValidateCorporatePortIn_Request.CustomerType()
                        {
                            Text = line.DNOIDType?.Name == "BRN" ? "CORPORATE" : "INDIVIDUAL"
                        },
                        Corporate = new ValidateCorporatePortIn_Request.Corporate()
                        {
                            CorporateName = new ValidateCorporatePortIn_Request.CorporateName()
                            {
                                Text = line.DNOCompanyName
                            },
                            DonorBusinessRegistrationNumber = line.DNOIDNo,
                            RecipientBusinessRegistrationNumber = line.SubParentGroupBRN
                        }
                    }
                }
            };

            param.Body.ValidateCorporatePortInRequest.SubscriberList = new ValidateCorporatePortIn_Request.SubscriberList();
            param.Body.ValidateCorporatePortInRequest.SubscriberList.SubscriberRecord = new List<ValidateCorporatePortIn_Request.SubscriberRecord>();

            foreach (var item in Lines)
            {
                param.Body.ValidateCorporatePortInRequest.SubscriberList.SubscriberRecord.Add(new ValidateCorporatePortIn_Request.SubscriberRecord()
                {
                    MSISDN = new ValidateCorporatePortIn_Request.MSISDN()
                    {
                        Text = Helper.GetValidMSISDN(item.MSISDN)
                    },
                    MSISDNType = "POSTPAID",
                    NumberType = "PRINCIPAL"
                });
            }

            if (line.DNOIDType?.Name == "BRN")
            {
                param.Body.ValidateCorporatePortInRequest.MNPInformation.AccountCode = new ValidateCorporatePortIn_Request.AccountCode()
                {
                    Text = line.DNOAccountCode
                };
            }
            else
            {
                string dnoIdType = line.DNOIDType?.Name ?? string.Empty;
                if (dnoIdType == "Armed Force")
                {
                    dnoIdType += "s";
                }

                param.Body.ValidateCorporatePortInRequest.MNPInformation.Individual = new ValidateCorporatePortIn_Request.Individual()
                {
                    CustomerName = line.DNOCompanyName,
                    IdentificationList = new ValidateCorporatePortIn_Request.IdentificationList()
                    {
                        IdentificationRecord = new ValidateCorporatePortIn_Request.IdentificationRecord()
                        {
                            IdType = new ValidateCorporatePortIn_Request.IdType()
                            {
                                Text = dnoIdType.Replace(" ", "").ToUpper()
                            },
                            IdNumber = new ValidateCorporatePortIn_Request.IdNumber()
                            {
                                Text = line.DNOIDNo
                            }
                        }
                    }
                };
            }

            return await ValidateCorporatePortIn(param);
        }

        public async Task<ValidateCorporatePortIn_Response.Header> ValidateCorporatePortIn(ValidateCorporatePortIn_Request.Envelope Param)
        {
            if (Param == null)
            {
                throw new Exception("Param cannot be null or empty");
            }

            var service = new ValidateCorporatePortInService(UserConnection);
            try
            {
                await service.SetParam(Param).Request();
                if (!service.IsSuccessResponse())
                {
                    string error = service.GetErrorResponse();
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }

                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                var log = service.GetLog();
                List<string> msisdn = service.GetRequest()?
                    .Body?
                    .ValidateCorporatePortInRequest?
                    .SubscriberList?
                    .SubscriberRecord?
                    .Select(item => item.MSISDN.Text)
                    .ToList() ?? null;
                string transactionId = service.GetRequest()?
                    .Header?
                    .CSGHeader?
                    .ReferenceID ?? string.Empty;

                LogHelper.LogACDCTracking(
                    UserConnection: UserConnection,
                    RequestBody: log?.Request?.Body ?? string.Empty,
                    ResponseBody: log?.Response?.Body ?? string.Empty,
                    OrderId: transactionId,
                    TransactionID: transactionId,
                    TransactionType: "MNP",
                    APIName: log?.Name ?? "Validate Corpotate Port In",
                    MSISDN: msisdn != null ? string.Join(", ", msisdn.ToArray()) : string.Empty,
                    Status: log?.Success ?? false ? "SUCCESS" : "FAIL",
                    ResultMessage: service.GetResponse()?.Header?.CSGHeader?.Status ?? string.Empty,
                    Remarks: service.GetErrorResponse(),
                    ContentType: log?.Type ?? string.Empty
                );
            }

            return service.GetResponse()?.Header;
        }

        #endregion

        #region ConfirmPortIn

        public async Task<ConfirmPortIn_Response.CSGHeader> ConfirmPortIn(Guid RecordId)
        {
            if (RecordId == null || RecordId == Guid.Empty)
            {
                throw new Exception("Record Id cannot be null or empty");
            }

            var service = new ConfirmPortInService(UserConnection);
            try
            {
                await service.SetParam(RecordId).Request();
                if (!service.IsSuccessResponse())
                {
                    string error = service.GetErrorResponse();
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }

                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                var log = service.GetLog();
                string transactionId = service.GetRequest()?
                    .Header?
                    .CSGHeader?
                    .ReferenceID ?? string.Empty;
                string orderId = service.GetRequest()?
                    .Body?
                    .ConfirmPortInRequest?
                    .MNPInformation?
                    .PortInTransactionId ?? string.Empty;

                LogHelper.LogACDCTracking(
                    UserConnection: UserConnection,
                    RequestBody: log?.Request?.Body ?? string.Empty,
                    ResponseBody: log?.Response?.Body ?? string.Empty,
                    OrderId: orderId,
                    TransactionID: transactionId,
                    TransactionType: "MNP",
                    APIName: log?.Name ?? "Confirm Port In",
                    Status: log?.Success ?? false ? "SUCCESS" : "FAIL",
                    ResultMessage: service.GetResponse()?.Header?.CSGHeader?.Status ?? string.Empty,
                    Remarks: service.GetErrorResponse(),
                    MSISDN: string.Empty,
                    LineDetailId: RecordId,
                    ContentType: log?.Type ?? string.Empty
                );
            }

            return service.GetResponse()?.Header?.CSGHeader;
        }

        #endregion

        private string GenerateReferenceId(string Prefix = "NCCF")
        {
            const int digits = 19;

            byte[] randomBytes = new byte[digits];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            byte[] positiveBytes = new byte[randomBytes.Length + 1];
            Buffer.BlockCopy(randomBytes, 0, positiveBytes, 0, randomBytes.Length);

            BigInteger bigInt = new BigInteger(positiveBytes);

            string digit = (bigInt % BigInteger.Pow(10, digits)).ToString($"D{digits}");

            return Prefix + digit;
        }
    }
}