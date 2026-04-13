using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;
using Request = DgIntegration.DgUpdatePostDelivery.Request;
using Response = DgIntegration.DgUpdatePostDelivery.Response;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgUpdatePostDelivery
{
    public class UpdatePostDeliveryService
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

		public UpdatePostDeliveryService(UserConnection UserConnection) 
        {
            this.userConnection = UserConnection;
        }

        public Response.Envelope Init(Stream Envelope)
        {
            var result = new Response.Envelope() {
                Body = new Response.Body() {
                    UpdatePostDeliveryResponse = new Response.UpdatePostDeliveryResponse() {
                        UpdatePostDeliveryResult = new Response.UpdatePostDeliveryResult()
                    }
                }
            };

            string code = "-1";
			string message = string.Empty;
			string orderId = string.Empty;
			string requestXml = string.Empty;

            try {
                string userName = (string)SysSettings.GetValue(UserConnection, "DgUsernameUpdateDeliveryNCCFv2");
                string userPassword = (string)SysSettings.GetValue(UserConnection, "DgPasswordUpdateDeliveryNCCFv2");

                var reader = new StreamReader(Envelope);
                requestXml = reader.ReadToEnd();

                var data = HTTPRequest.XmlToObject<Request.Envelope>(requestXml);
				orderId = data.Body.UpdatePostDelivery.PostReq.OrderId;
				
                if (data.Header.Security.UsernameToken.Username != userName || data.Header.Security.UsernameToken.Password.Text != userPassword) {
                    code = "-1";
					throw new Exception("A security error was encountered when verifying the message");
                }
                
                var centerId = data.Body.UpdatePostDelivery.PostReq.CenterId;
                var channelId = data.Body.UpdatePostDelivery.PostReq.ChannelId;
                var postDeliveryDateTime = data.Body.UpdatePostDelivery.PostReq.PostDeliveryDateTime;
                var deliveryStatus = data.Body.UpdatePostDelivery.PostReq.DeliveryStatus;
                var deliveryAttempt = data.Body.UpdatePostDelivery.PostReq.DeliveryAttempt;
                var statusRemarks = data.Body.UpdatePostDelivery.PostReq.StatusRemarks;

                if (!IsSONumberExists(orderId)) {
                    code = "-1";
					throw new Exception($"Order ID {orderId} not found.");
                }

                if (string.IsNullOrEmpty(postDeliveryDateTime)) {
                    code = "-1";
					throw new Exception("Post-Delivery DateTime not found.");
                }

                Guid DeliveryStatusId = GetDeliveryStatusId(deliveryStatus);
                UpdatePostDelivery(orderId, DeliveryStatusId, postDeliveryDateTime, statusRemarks, deliveryAttempt);

                code = "1";
				message = "Success";
            } catch(Exception error) {
                message = error.Message;
            } finally {
                result.Body.UpdatePostDeliveryResponse.UpdatePostDeliveryResult.Code = code;
				result.Body.UpdatePostDeliveryResponse.UpdatePostDeliveryResult.Message = message;

                InsertLog(requestXml, HTTPRequest.XmlToString<Response.Envelope>(result), orderId, code == "1" ? "Success" : "Fail");
            }

            return result;
        }
		
		public bool IsSONumberExists(string SONumber)
        {
            int total = 0;
            var select = new Select(UserConnection)
                .Column(Func.Count("Id")).As("Total")
            .From("DgLineDetail")
            .Where("DgLineDetail", "DgSOID").IsEqual(Column.Parameter(SONumber)) as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())  {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        total = dataReader.GetColumnValue<int>("Total");
                    }
                }
            }

            return total > 0 ? true : false;
        }

        public Guid GetDeliveryStatusId(string Code)
        {
            var result = Guid.Empty;
            
            var select = new Select(UserConnection)
                .Top(1)
                .Column("DgDeliveryStatus", "Id").As("Id")
            .From("DgDeliveryStatus")
            .Where("DgDeliveryStatus", "DgCode").IsEqual(Column.Parameter(Code)) as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())  {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result = dataReader.GetColumnValue<Guid>("Id");
                    }
                }
            }

            return result;
        }

        public void UpdatePostDelivery(string OrderId, Guid DeliveryStatusId, string postDeliveryDateTime, string statusRemarks, string deliveryAttempt)
        {
            new Update(UserConnection, "DgLineDetail")
                .Set("DgPostDeliveryDate", Column.Parameter(DateTime.Parse(postDeliveryDateTime).ToUniversalTime()))
                .Set("DgDeliveryStatusId", Column.Parameter(DeliveryStatusId))
                .Set("DgDeviceOrderRemark", Column.Parameter(statusRemarks))
                .Set("DgDeliveryAttempt", Column.Parameter(deliveryAttempt))
            .Where("DgSOID").IsEqual(Column.Parameter(OrderId))
            .Execute();
        }

        protected void InsertLog(string RequestBody, string ResponseBody, string SONumber, string Status) 
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = $"{currentDateTimeMY}_CallbackUpdatePostDelivery_{SONumber}.txt";
            var log = $"{currentDateTimeMY}{Environment.NewLine}XML Request: {Environment.NewLine}{RequestBody}{Environment.NewLine}{Environment.NewLine}XML Response: {Environment.NewLine}{ResponseBody}";

            EntityHelper.CreateEntity(
                UserConnection, 
                section: "DgUERPOrderTracking", 
                values: new Dictionary<string, object>() {
                    {"DgAPIName", "CallbackUpdatePostDelivery"},
                    {"DgFileName", fileName},
                    {"DgLogFile", log},
                    {"DgOriginalSysDocumentRef", SONumber},
                    {"DgStatus", Status},
                    {"CreatedById", UserConnection.CurrentUser.ContactId}
                }
            );
        }
    }
}