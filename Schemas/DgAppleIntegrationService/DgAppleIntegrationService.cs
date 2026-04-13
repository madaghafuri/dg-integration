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
using ISAEntityHelper.EntityHelper;
using DgIntegration.DgEnrollDeviceService;
using DgIntegration.DgOverrideDeviceService;
using DgIntegration.DgVoidDeviceService;
using DgIntegration.DgShowOrderDetailsService;
using DgIntegration.DgCheckTransactionStatusService;
using DgBaseService.DgHelpers;

namespace DgIntegration.DgAppleIntegrationService
{
    public class AppleIntegrationService
    {
		private UserConnection userConnection;
        private bool isCelcom;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private HTTPRequest httpRequest;
		public AppleIntegrationService(UserConnection userConnection_ = null, bool isCelcom_ = false) 
        {
        	userConnection = userConnection_;
            isCelcom = isCelcom_;
        }

        public async Task<GeneralResponse> RequestDeviceEnroll(Guid DEPRegistrationId, string OrderType, string TransactionNumber = "", Guid DEPRegistrationDetailId = default(Guid))
        {
            var result = new GeneralResponse();

            try {
                var transactionNumber = GenerateTransactionID();
    
                switch(OrderType) {
                    case "OR":
                        var isAvailableToEnroll = IsAvailableToEnroll(DEPRegistrationId);

                        if (!isAvailableToEnroll) {
                            UnselectLine(DEPRegistrationId);
                            throw new Exception("Enrollment cannot be processed, IMEI or serial number has already been used.");
                        }

                        var requestEnrollDevice = await EnrollDevice(transactionNumber, DEPRegistrationId);

                        UnselectLine(DEPRegistrationId);
                        result.Message = requestEnrollDevice.Message;
                        result.Success = requestEnrollDevice.Success;
                        break;
                case "OV":
                    var requestOverrideDevice = await OverrideDevice(TransactionNumber, DEPRegistrationDetailId);

                    result.Message = requestOverrideDevice.Message;
                    result.Success = requestOverrideDevice.Success;
                    break;
                case "RE":
                    var requestReturnDevice = await ReturnDevice(TransactionNumber, DEPRegistrationId, DEPRegistrationDetailId);

                    result.Message = requestReturnDevice.Message;
                    result.Success = requestReturnDevice.Success;
                    break;
                case "VD":
                    var requestVoidDevice = await VoidDevice(TransactionNumber, DEPRegistrationDetailId);

                    result.Message = requestVoidDevice.Message;
                    result.Success = requestVoidDevice.Success;
                    break;
                 default:
                    throw new Exception("Order Type Not Found!");
                    break;
                }

            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        protected virtual async Task<GeneralResponse> EnrollDevice(string TransactionNumber, Guid DEPRegistrationId)
        {
            var result = new GeneralResponse();

            try {
                var enrollDevice = new EnrollDeviceService(UserConnection, isCelcom);

                await enrollDevice
                    .SetParam(TransactionNumber, "OR", DEPRegistrationId)
                    .Request();
                
                var requestBody = enrollDevice.GetRequest();
                var responseBody = enrollDevice.GetResponse();

                if(!enrollDevice.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message = responseBody.errorMessage
                    };
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId), 
                    TransactionNumber : TransactionNumber, 
                    OrderType : "OR", 
                    Status : responseBody.enrollDevicesResponse != null ? 
                        responseBody.enrollDevicesResponse.statusCode : responseBody.enrollDeviceErrorResponse.errorCode, 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                if (responseBody.enrollDeviceErrorResponse != null) {
                    return new GeneralResponse() {
                        Message = $"OR | {responseBody.enrollDeviceErrorResponse.errorCode} - {responseBody.enrollDeviceErrorResponse.errorMessage}",
                        Success = false
                    };
                }

                var devices = requestBody.orders[0].deliveries[0].devices;
                foreach (var itemDevices in devices) {
                    var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                        .Set("DgOrderNumber", Column.Parameter(requestBody.orders[0].orderNumber))
                        .Set("DgTransactionNumber", Column.Parameter(TransactionNumber))
                        .Set("DgDeviceEnrollmentTransactionId", Column.Parameter(responseBody.deviceEnrollmentTransactionId))
                        .Set("DgStatus", Column.Parameter("Enrollment request in progress"))
                        .Where("DgDEPRegistrationId").IsEqual(Column.Parameter(DEPRegistrationId))
                        .And("DgIMEINumber").IsEqual(Column.Parameter(itemDevices.deviceId));
                    update.Execute();
                }

                result.Message = $"{requestBody.orders[0].orderNumber} | {responseBody.enrollDevicesResponse.statusMessage}";
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        protected virtual async Task<GeneralResponse> ReturnDevice(string TransactionNumber, Guid DEPRegistrationId, Guid DEPRegistrationDetailId)
        {
            var result = new GeneralResponse();

            try {
                var enrollDevice = new EnrollDeviceService(UserConnection, isCelcom);

                await enrollDevice
                    .SetParam(TransactionNumber, "RE", DEPRegistrationId, DEPRegistrationDetailId)
                    .Request();

                var requestBody = enrollDevice.GetRequest();
                var responseBody = enrollDevice.GetResponse();

                if(!enrollDevice.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message = responseBody.errorMessage
                    };
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId), 
                    TransactionNumber : TransactionNumber, 
                    OrderType : "RE", 
                    Status : responseBody.enrollDevicesResponse != null ? 
                        responseBody.enrollDevicesResponse.statusCode : responseBody.enrollDeviceErrorResponse.errorCode, 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                if (responseBody.enrollDeviceErrorResponse != null)
                {
                    return new GeneralResponse() {
                        Message = $"RE | {responseBody.enrollDeviceErrorResponse.errorCode} - {responseBody.enrollDeviceErrorResponse.errorMessage}",
                        Success = false
                    };
                }

                var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                    .Set("DgDeviceEnrollmentTransactionId", Column.Parameter(responseBody.deviceEnrollmentTransactionId))
                    .Set("DgIsReturn", Column.Parameter(true))
                    .Set("DgOrderNumber", Column.Parameter(requestBody.orders[0].orderNumber))
                    .Set("DgStatus", Column.Parameter("Return request in progress"))
                    .Where("Id").IsEqual(Column.Parameter(DEPRegistrationDetailId));
                update.Execute();

                result.Message = $"RE | {responseBody.enrollDevicesResponse.statusMessage}";
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        protected virtual async Task<GeneralResponse> OverrideDevice(string TransactionNumber, Guid DEPRegistrationDetailId)
        {
            var result = new GeneralResponse();

            try {
                var DEPRegistrationId = GetDEPRegistrationId(DEPRegistrationDetailId);
                var overrideDeviceService = new OverrideDeviceService(UserConnection, isCelcom);

                await overrideDeviceService
                    .SetParam(TransactionNumber, DEPRegistrationDetailId)
                    .Request();

                var requestBody = overrideDeviceService.GetRequest();
                var responseBody = overrideDeviceService.GetResponse();

                if(!overrideDeviceService.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message = responseBody.errorMessage
                    };
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId), 
                    TransactionNumber : TransactionNumber, 
                    OrderType : "OV", 
                    Status : responseBody.enrollDevicesResponse != null ? 
                        responseBody.enrollDevicesResponse.statusCode : responseBody.enrollDeviceErrorResponse.errorCode, 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                if (responseBody.enrollDeviceErrorResponse != null)
                {
                    return new GeneralResponse() {
                        Message = $"OV | {responseBody.enrollDeviceErrorResponse.errorCode} - {responseBody.enrollDeviceErrorResponse.errorMessage}",
                        Success = false
                    };
                }

                var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                    .Set("DgOrderNumber", Column.Parameter(requestBody.orders[0].orderNumber))
                    .Set("DgDeviceEnrollmentTransactionId", Column.Parameter(responseBody.deviceEnrollmentTransactionId))
                    .Set("DgStatus", Column.Parameter("Override request in progress"))
                    .Where("DgTransactionNumber").IsEqual(Column.Parameter(TransactionNumber));
                update.Execute();

                result.Message = $"OV | {responseBody.enrollDevicesResponse.statusMessage}";
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        protected virtual async Task<GeneralResponse> VoidDevice(string TransactionNumber, Guid DEPRegistrationDetailId)
        {
            var result = new GeneralResponse();

            try {
                var DEPRegistrationId = GetDEPRegistrationId(DEPRegistrationDetailId);
                var voidDeviceService = new VoidDeviceService(UserConnection, isCelcom);

                await voidDeviceService
                    .SetParam(TransactionNumber, DEPRegistrationDetailId)
                    .Request();

                var requestBody = voidDeviceService.GetRequest();
                var responseBody = voidDeviceService.GetResponse();

                if(!voidDeviceService.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message = responseBody.errorMessage
                    };
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId),
                    TransactionNumber : TransactionNumber, 
                    OrderType : "VD", 
                    Status : responseBody.enrollDevicesResponse != null ? 
                        responseBody.enrollDevicesResponse.statusCode : responseBody.enrollDeviceErrorResponse.errorCode, 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                if (responseBody.enrollDeviceErrorResponse != null)
                {
                    return new GeneralResponse() {
                        Message = $"VD | {responseBody.enrollDeviceErrorResponse.errorCode} - {responseBody.enrollDeviceErrorResponse.errorMessage}",
                        Success = false
                    };
                }

                var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                    .Set("DgOrderNumber", Column.Parameter(requestBody.orders[0].orderNumber))
                    .Set("DgDeviceEnrollmentTransactionId", Column.Parameter(responseBody.deviceEnrollmentTransactionId))
                    .Set("DgIsVoid", Column.Parameter(true))
                    .Set("DgStatus", Column.Parameter("Void request in progress"))
                    .Where("DgTransactionNumber").IsEqual(Column.Parameter(TransactionNumber));
                update.Execute();

                result.Message = $"VD | {responseBody.enrollDevicesResponse.statusMessage}";
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        public virtual async Task<GeneralResponse> ShowOrderDetails(Guid DEPRegistrationDetailId)
        {
            var result = new GeneralResponse();

            try {
                var DEPRegistrationId = GetDEPRegistrationId(DEPRegistrationDetailId);
                var showOrderDetailsService = new ShowOrderDetailsService(UserConnection, isCelcom);

                await showOrderDetailsService
                    .SetParam(DEPRegistrationDetailId)
                    .Request();

                var requestBody = showOrderDetailsService.GetRequest();
                var responseBody = showOrderDetailsService.GetResponse();

                if(!showOrderDetailsService.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message =  $"{responseBody.orders[0].showOrderStatusCode} - {responseBody.orders[0].showOrderStatusMessage}"
                    };
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId),
                    TransactionNumber : GetTransationNumber(DEPRegistrationDetailId), 
                    OrderType : "SOD", 
                    Status : showOrderDetailsService.GetStatusCode(), 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        public virtual async Task<GeneralResponse> CheckTransactionStatus(Guid DEPRegistrationDetailId)
        {
            var result = new GeneralResponse();

            try {
                var DEPRegistrationId = GetDEPRegistrationId(DEPRegistrationDetailId);
                var checkTransactionStatusService = new CheckTransactionStatusService(UserConnection, isCelcom);

                await checkTransactionStatusService
                    .SetParam(DEPRegistrationDetailId)
                    .Request();

                var requestBody = checkTransactionStatusService.GetRequest();
                var responseBody = checkTransactionStatusService.GetResponse();

                if(!checkTransactionStatusService.IsSuccessResponse()) {
                    return new GeneralResponse() {
                        Success = false,
                        Message = responseBody.errorMessage
                    };
                }

                var voidId = IsVoid(responseBody.deviceEnrollmentTransactionID);

                if (voidId != Guid.Empty) {
                    EntityHelper.UpdateEntity(UserConnection, "DgDEPRegistrationDetail", voidId, new Dictionary<string, object> {
                        {"DgIsVoid", false},
                        {"DgStatus", string.Empty},
                        {"DgDeviceEnrollmentTransactionId", string.Empty},
                        {"DgTransactionNumber", string.Empty},
                        {"DgIsSelect", false}
                    });
                }

                InsertLogAppleOrderTracking(
                    SerialNumber : GetSerialNumber(DEPRegistrationId),
                    TransactionNumber : GetTransationNumber(DEPRegistrationDetailId), 
                    OrderType : "CTS", 
                    Status : checkTransactionStatusService.GetStatusCode(), 
                    RequestBody : JsonConvert.SerializeObject(requestBody), 
                    ResponseBody : JsonConvert.SerializeObject(responseBody)
                );

                if (responseBody.checkTransactionErrorResponse?.FirstOrDefault() != null) {
                    var errorResponse = responseBody.checkTransactionErrorResponse.FirstOrDefault();
                    var errorCodeStatus = errorResponse.errorCode == "DEP-ERR-4003" ? "Request in progress" : errorResponse.errorCode;
                    UpdateStatusDEPLine(errorCodeStatus, DEPRegistrationDetailId);
                    return new GeneralResponse() {
                        Message = $"{errorResponse.errorCode} | {errorResponse.errorMessage}",
                        Success = false
                    };
                } else if (responseBody.enrollDeviceErrorResponse?.FirstOrDefault() != null) {
                    var errorResponses = responseBody.enrollDeviceErrorResponse;
                    var eCode = string.Join(" - ", errorResponses.Select(e => e.errorCode));
                    var eMessage = string.Join(" - ", errorResponses.Select(e => e.errorMessage));

                    UpdateStatusDEPLine(eCode, DEPRegistrationDetailId);
                    return new GeneralResponse() {
                        Message = $"{eCode} | {eMessage}",
                        Success = false
                    };
                } else if (responseBody.statusCode == "ERROR") {
                    var orders = responseBody.orders?.FirstOrDefault();
                    var deliveries = orders?.deliveries?.FirstOrDefault();

                    if (deliveries?.devices?.FirstOrDefault() != null) {
                        var device = deliveries.devices.FirstOrDefault();
                        UpdateStatusDEPLine(device.devicePostStatus, DEPRegistrationDetailId);

                        return new GeneralResponse() {
                            Message = $"{device.devicePostStatus} | {device.devicePostStatusMessage}",
                            Success = false
                        };
                    } else if (deliveries != null) {
                        UpdateStatusDEPLine(deliveries.deliveryPostStatus, DEPRegistrationDetailId);

                        return new GeneralResponse() {
                            Message = $"{deliveries.deliveryPostStatus} | {deliveries.deliveryPostStatusMessage}",
                            Success = false
                        };
                    } else {
                        UpdateStatusDEPLine(orders.orderPostStatus, DEPRegistrationDetailId);
                        return new GeneralResponse() {
                            Message = $"{orders.orderPostStatus} | {orders.orderPostStatusMessage}",
                            Success = false
                        };
                    }
                } else if (responseBody.statusCode == "COMPLETE_WITH_ERRORS") {
                    var deliveries = responseBody.orders?.FirstOrDefault()?.deliveries?.FirstOrDefault();

                    foreach (var itemDevices in deliveries?.devices) {
                        var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                            .Set("DgStatus", Column.Parameter(itemDevices.devicePostStatus == "COMPLETE" ? "COMPLETE" : "COMPLETE_WITH_ERRORS"))
                            .Where("DgIMEINumber").IsEqual(Column.Parameter(itemDevices.deviceId));
                        update.Execute();
                    }

                    return new GeneralResponse() {
                        Message = $"{deliveries.deliveryPostStatus} | {deliveries.deliveryPostStatusMessage}",
                        Success = false
                    };
                }
                /**
                ** Update Single Success Data
                **/
                if (responseBody.orders[0].deliveries == null) {
                    var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                        .Set("DgStatus", Column.Parameter(responseBody.orders[0].orderPostStatus))
                        .Where("DgOrderNumber").IsEqual(Column.Parameter(responseBody.orders[0].orderNumber));
                    update.Execute();
                } else if (responseBody.orders[0].deliveries[0].devices != null) {
                    /**
                    ** Update Bulk Success Data
                    **/
                    foreach(var itemDevices in responseBody.orders[0].deliveries[0].devices)
                    {
                        var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                            .Set("DgStatus", Column.Parameter(itemDevices.devicePostStatus))
                            .Where("DgIMEINumber").IsEqual(Column.Parameter(itemDevices.deviceId));
                        update.Execute();
                    }
                }

                result.Message = responseBody.statusCode;
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }

        public virtual GeneralResponse SubmissionDEP(string SerialNumber, string CustomerID) 
        {
            var result = new GeneralResponse();

            try {
                var submissionBySerialNumber = GetSubmissionBySerialNumber(SerialNumber);

                if (submissionBySerialNumber.Count == 0) {
                    throw new Exception("Serial Number is not match.");
                }

                var customerID = EntityHelper.GetOrCreateEntity(
                    UserConnection, 
                    section: "DgCustomerApple", 
                    searchBy: new Dictionary<string, object>() {
                        {"DgName", CustomerID}
                    },
                    value: new Dictionary<string, object>() {
                        {"DgName", CustomerID}
                    }
                );
                
                var DEPRegistrationId = EntityHelper.GetOrCreateEntity(
                    UserConnection, 
                    section: "DgDEPRegistration", 
                    searchBy: new Dictionary<string, object>() {
                        {"DgSerialNo", SerialNumber}
                    },
                    value: new Dictionary<string, object>() {
                        {"DgName", SerialNumber},
                        {"DgCompanyName", submissionBySerialNumber.FirstOrDefault()?.CompanyName},
                        {"DgSerialNo", SerialNumber},
                        {"DgCustomerId", customerID}
                    }
                );

                foreach (var item in submissionBySerialNumber) 
                {
                    EntityHelper.CreateEntity(
                        UserConnection, 
                        section: "DgDEPRegistrationDetail", 
                        values: new Dictionary<string, object>() {
                            {"DgMSISDN", item.MSISDN},
                            {"DgMobileNumber", item.MobileNumber},
                            {"DgSONumber", item.SONumber},
                            {"DgSoDate", item.SoDate},
                            {"DgPromoCode", item.PromoCode},
                            {"DgIMEINumber", item.IMEINumber},
                            {"DgDEPRegistrationId", DEPRegistrationId}
                        }
                    );
                }

                result.Success = true;
            } catch(Exception e) {
                result.Message = e.Message;
            }

            return result;
        }

        protected virtual bool IsAvailableToEnroll(Guid DEPRegistrationId)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsSelect", true));
            // esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgDeviceEnrollmentTransactionId"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgDEPRegistration", DEPRegistrationId));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgStatus", "COMPLETE"));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            if(entity == null) {
                return true;
            }

            return entity.GetTypedColumnValue<Guid>(columns["Id"].Name) != Guid.Empty ? false : true;
        }

        protected virtual string GenerateTransactionID() 
        {
			string sysSettingsCodeMask = (string)SysSettings.GetValue(UserConnection, "DgTransactionIDCodeMask");
			string result = string.Empty;
			if (GlobalAppSettings.UseDBSequence) {
				var sequenceMap = new SequenceMap(UserConnection);
				var sequence = sequenceMap.GetByNameOrDefault("DgTransactionLastNumber");
				result = string.Format(sysSettingsCodeMask, sequence.GetNextValue());
				return result;
			}
			var coreSysSettings = new SysSettings(UserConnection);
			if (coreSysSettings.FetchFromDB("Code", "DgTransactionLastNumber")) {
				int sysSettingsLastNumber = Convert.ToInt32(SysSettings.GetDefValue(UserConnection, "DgTransactionLastNumber"));
				++sysSettingsLastNumber;
				SysSettings.SetDefValue(UserConnection, "DgTransactionLastNumber", sysSettingsLastNumber);
				result = string.Format(sysSettingsCodeMask, sysSettingsLastNumber);
			}
			return result;
		}

        protected virtual string GetCurrentDateTime()
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);

            return MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        public string GetTransationNumber(Guid DEPRegistrationDetailId)
        {
            var result = String.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("TransactionNumber", esq.AddColumn("DgTransactionNumber"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", DEPRegistrationDetailId));
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result = entity.GetTypedColumnValue<string>(columns["TransactionNumber"].Name);
            }

            return result;
        }

        protected virtual Guid IsVoid(string deviceEnrollmentTransactionID)
        {
            var result = Guid.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgDeviceEnrollmentTransactionId", deviceEnrollmentTransactionID));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsVoid", true));
            
            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            if(entity == null) {
                return Guid.Empty;
            }

            result = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);

            return result;
        }

        protected virtual String GetSerialNumber(Guid DEPRegistrationId)
        {
            var result = String.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistration");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("SerialNo", esq.AddColumn("DgSerialNo"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", DEPRegistrationId));
            
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result = entity.GetTypedColumnValue<string>(columns["SerialNo"].Name);
            }

            return result;
        }

        protected virtual Guid GetDEPRegistrationId(Guid DEPRegistrationDetailId)
        {
            var result = Guid.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("DEPRegistrationId", esq.AddColumn("DgDEPRegistration.Id"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", DEPRegistrationDetailId));
            
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result = entity.GetTypedColumnValue<Guid>(columns["DEPRegistrationId"].Name);
            }

            return result;
        }

        protected virtual List<DEPDetail> GetSubmissionBySerialNumber(string SerialNumber)
        {
            var result = new List<DEPDetail>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("LineNumber", esq.AddColumn("DgLineId"));
                columns.Add("MobileNumber", esq.AddColumn("DgMSISDN"));
                columns.Add("SONumber", esq.AddColumn("DgSOID"));
                columns.Add("SoDate", esq.AddColumn("DgSODate"));
                columns.Add("PromoCode", esq.AddColumn("DgDeviceModel"));
                columns.Add("IMEINumber", esq.AddColumn("DgDeviceIMEI"));
                columns.Add("CompanyName", esq.AddColumn("DgSubmission.DgCompanyName"));
                columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.DgSerialNumber", SerialNumber));
            
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                var data = new DEPDetail();

                data.LineNumber = entity.GetTypedColumnValue<string>(columns["LineNumber"].Name);
                data.MobileNumber = Helper.GetValidMSISDN(entity.GetTypedColumnValue<string>(columns["MobileNumber"].Name));
                data.SONumber = entity.GetTypedColumnValue<string>(columns["SONumber"].Name);
                data.SoDate = entity.GetTypedColumnValue<DateTime>(columns["SoDate"].Name);
                data.PromoCode = entity.GetTypedColumnValue<string>(columns["PromoCode"].Name);
                data.IMEINumber = entity.GetTypedColumnValue<string>(columns["IMEINumber"].Name);
                data.CompanyName = entity.GetTypedColumnValue<string>(columns["CompanyName"].Name);
                data.MSISDN = entity.GetTypedColumnValue<string>(columns["MSISDN"].Name);

                result.Add(data);
            }

            return result;
        }

        protected void InsertLogAppleOrderTracking(string SerialNumber, string TransactionNumber, string OrderType, string Status, string RequestBody, string ResponseBody) 
        {
            var fileName = string.Format("{0}_DEP_{1}_{2}.txt", GetCurrentDateTime(), OrderType, TransactionNumber);
            var log = string.Format("{0}{1}Json Request: {2}{3}{4}{5}Json Response: {6}{7}", 
                GetCurrentDateTime(), 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                RequestBody, 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                System.Environment.NewLine, 
                ResponseBody
            );

            EntityHelper.CreateEntity(
                UserConnection, 
                section: "DgDEPOrderTracking", 
                values: new Dictionary<string, object>() {
                    {"DgSerialNumber", SerialNumber},
                    {"DgTransactionNumber", TransactionNumber},
                    {"DgOrderType", OrderType},
                    {"DgStatus", Status},
                    {"DgFileName", fileName},
                    {"DgLog", log}
                }
            );
        }

        protected void UpdateStatusDEPLine(string Status, Guid DEPRegistrationDetailId)
        {
            var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                .Set("DgStatus", Column.Parameter(Status))
                .Where("Id").IsEqual(Column.Parameter(DEPRegistrationDetailId));
            update.Execute();
        }

        protected void UnselectLine(Guid DEPRegistrationId) {
            var update = new Update(UserConnection, "DgDEPRegistrationDetail")
                .Set("DgIsSelect", Column.Parameter(false));
            update.Where("DgDEPRegistrationId").IsEqual(Column.Parameter(DEPRegistrationId));
            update.Execute();
        }
	}

    public class DEPDetail
    {
        public string LineNumber { get; set; }
        public string MobileNumber { get; set; }
        public string SONumber { get; set; }
        public DateTime SoDate { get; set; }
        public string PromoCode { get; set; }
        public string IMEINumber { get; set; }
        public string CompanyName { get; set; }
        public string MSISDN { get; set; }
    }
}