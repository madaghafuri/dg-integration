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
using Enroll_Request = DgIntegration.DgEnrollDeviceService.Request;
using Enroll_Response = DgIntegration.DgEnrollDeviceService.Response;
using ISAIntegrationSetup;
using DgMasterData;
using DgMasterData.DgLookupConst;

namespace DgIntegration.DgEnrollDeviceService
{
    public class EnrollDeviceService
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
        private string orderType { get; set; }
        protected Guid recordId;
        protected Guid recordDetailId;
        protected string transactionNumber;
        protected bool isCelcom;
        protected Enroll_Request.RequestBulkEnrollDevices param;
        private Enroll_Response.ResponseBulkEnrollDevices response;
        private HttpResponseHeaders headerResponse;
        private string errorResponse;
        private HTTPRequest httpRequest;

        public EnrollDeviceService(UserConnection userConnection, bool isCelcom = false) 
        {
            this.userConnection = userConnection;

            var setup = IntegrationSetup.Get(UserConnection, "Apple", "EnrollDevice");
            if(setup == null) {
                throw new Exception("EnrollDevice hasn't been set up for integration");
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
            this.errorResponse = string.Empty;
            this.response = null;
            this.headerResponse = null;

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                var req = await this.httpRequest
                    .SetLogName($"Apple: Enroll Device")
                    .SetLogSection("DEP/ABM Registration")
                    .SetLogRecordId(this.recordId)
                .Post(this.endpoint, this.param);

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
                this.response = JsonConvert.DeserializeObject<Enroll_Response.ResponseBulkEnrollDevices>(res);
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(res) ? res : e.Message;
            }
        }
		
        public virtual EnrollDeviceService SetParam(string Json)
        {
			try {
				return this.SetParam(JsonConvert.DeserializeObject<Enroll_Request.RequestBulkEnrollDevices>(Json));
			} catch (Exception e) {
				throw new Exception($"Json is not valid: {e.Message}");
			}
        }

        public virtual EnrollDeviceService SetParam(Enroll_Request.RequestBulkEnrollDevices Param)
        {
            this.param = Param;
            return this;
        }

        public virtual EnrollDeviceService SetParam(string TransactionNumber, string OrderType, Guid RecordId = default(Guid), Guid RecordDetailId = default(Guid))
        {
            this.recordId = RecordId;
            this.recordDetailId = RecordDetailId;
            this.transactionNumber = TransactionNumber;
            this.orderType = OrderType;

            this.param = BuildRequest();
            return this;
        }

        public virtual Enroll_Request.RequestBulkEnrollDevices GetRequest()
        {
            return this.param;
        }

        public virtual string GetStringRequest()
        {
            return JsonConvert.SerializeObject(this.param);
        }

        public virtual Enroll_Response.ResponseBulkEnrollDevices GetResponse()
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
			
			if(!string.IsNullOrEmpty(this.response.errorCode)) {
                return false;
            }

            string status = this.response.enrollDevicesResponse.statusCode;
            return status == "SUCCESS" ? true : false;
        }

        public virtual string GetErrorResponse()
        {
            var result = string.Empty;

            if(this.headerResponse == null) {
                return this.errorResponse ?? string.Empty;
            }

            if (this.response.enrollDeviceErrorResponse != null) {
                result = $"OR | {this.response.enrollDeviceErrorResponse.errorCode} - {this.response.enrollDeviceErrorResponse.errorMessage}";
            }

            return result;
        }

        protected virtual void ValidateSerialNumber(string SerialNumber) 
        {
            var checkImeiExist = IsIMEIExist(SerialNumber);
			
            if (checkImeiExist != Guid.Empty) {
                throw new Exception("Enroll cannot be processed, IMEI or serial number has already been used.");
            }

            if (SerialNumber.Length != 15) {
                throw new Exception("The serial number must consist of 15 digits.");
            }

            foreach (char digit in SerialNumber)
            {
                if (!char.IsDigit(digit)) {
                    throw new Exception("The serial number must consist of numbers only.");
                }
            }
        }

        protected virtual void ValidateShipDate(DateTime SODate, DateTime ShipDate) 
        {
            if (SODate > ShipDate) {
                throw new Exception("The SO Date cannot more than Ship Date.");
            }
        }

        protected virtual Guid IsIMEIExist(string IMEINumber)
        {
            var result = Guid.Empty;
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIMEINumber", IMEINumber));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgStatus", "COMPLETE"));

            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
            }

            return result;
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

        protected virtual Enroll_Request.RequestBulkEnrollDevices BuildRequest()
        {
            var listDevice = new List<Enroll_Request.devices>();
            var reseller = GetReseller();
            var soNumber = String.Empty;
            var customerId = String.Empty;
            var soDate = String.Empty;
            var shipDate = String.Empty;
            string[] numberIncrement = this.transactionNumber.Split('_');

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgDEPRegistrationDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
                columns.Add("SONumber", esq.AddColumn("DgSONumber"));
                columns.Add("SODate", esq.AddColumn("DgSoDate"));
                columns.Add("ShipDate", esq.AddColumn("DgShipDate"));
                columns.Add("IMEINumber", esq.AddColumn("DgIMEINumber"));
                columns.Add("CustomerId", esq.AddColumn("DgDEPRegistration.DgCustomer.DgName"));
                columns.Add("ShipTo", esq.AddColumn("DgReseller.DgShipTo"));
            
            if (this.recordDetailId != Guid.Empty) 
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", this.recordDetailId));
            } else
            {
                // esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgDeviceEnrollmentTransactionId"));
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgDEPRegistration", this.recordId));
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgIsSelect", true));
            }

            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                var devices = new Enroll_Request.devices();

                if (this.orderType == "OR") {
                    ValidateSerialNumber(entity.GetTypedColumnValue<string>(columns["IMEINumber"].Name));
                }
                
                ValidateShipDate(entity.GetTypedColumnValue<DateTime>(columns["SODate"].Name), DateTime.Now);

                soNumber = entity.GetTypedColumnValue<string>(columns["SONumber"].Name);
                soDate = entity.GetTypedColumnValue<DateTime>(columns["SODate"].Name).ToString("yyyy-MM-ddT00:00:00Z");
                // shipDate = entity.GetTypedColumnValue<DateTime>(columns["ShipDate"].Name).ToString("yyyy-MM-ddT00:00:00Z");
                shipDate = DateTime.Now.ToString("yyyy-MM-ddT00:00:00Z");
                customerId = entity.GetTypedColumnValue<string>(columns["CustomerId"].Name);
                devices.deviceId = entity.GetTypedColumnValue<string>(columns["IMEINumber"].Name);
                devices.assetTag = entity.GetTypedColumnValue<string>(columns["IMEINumber"].Name);

                listDevice.Add(devices);

                EntityHelper.UpdateEntity(UserConnection, "DgDEPRegistrationDetail", entity.GetTypedColumnValue<Guid>(columns["Id"].Name), new Dictionary<string, object> {
                    {"DgShipDate", DateTime.Now}
                });
            }

            return new Enroll_Request.RequestBulkEnrollDevices() {
                requestContext = new Enroll_Request.requestContext() {
                    shipTo = reseller.Name,
                    timeZone = "-480",
                    langCode = "en"
                },
                transactionId = this.orderType == "OR" ? this.transactionNumber : $"{this.transactionNumber}_RE",
                depResellerId = reseller.Code,
                orders = new List<Enroll_Request.orders>() {
                    new Enroll_Request.orders() {
                        orderNumber = this.orderType == "OR" ? $"ORDER_{numberIncrement[1]}" : $"ORDER_{numberIncrement[1]}_RE",
                        orderDate = soDate,
                        orderType = this.orderType,
                        customerId = customerId,
                        poNumber = this.orderType == "OR" ? $"PO_{numberIncrement[1]}" : $"PO_{numberIncrement[1]}_RE",
                        deliveries = new List<Enroll_Request.deliveries>() {
                            new Enroll_Request.deliveries() {
                                deliveryNumber = soNumber,
                                shipDate = shipDate,
                                devices = listDevice
                            }
                        }
                    }
                }
            };
        }
    }
}