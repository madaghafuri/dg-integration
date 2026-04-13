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
using Terrasoft.Core.Scheduler;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using Quartz;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgSubmission.DgLineDetail;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;
using RequestModel = DgIntegration.DgUpdateStatusDelivery.Request;
using ResponseModel = DgIntegration.DgUpdateStatusDelivery.Response;

namespace DgIntegration.DgUpdateStatusDelivery
{
    public class UpdateStatusDeliveryService
    {
		protected UserConnection UserConnection;
        public UpdateStatusDeliveryService(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
        }

		public ResponseModel.Envelope Process(Stream Envelope)
		{
			var result = new ResponseModel.Envelope() {
				Body = new ResponseModel.Body() {
					UpdateDeliveryResponse = new ResponseModel.UpdateDeliveryResponse() {
						UpdateDeliveryResult = new ResponseModel.UpdateDeliveryResult()
					}
				}
			};
			
			int code = 0;
			string message = string.Empty;
			string soId = string.Empty;
            string customerName = string.Empty;
            string msisdnList = string.Empty;
			string requestXml = string.Empty;

			try {
				var reader = new StreamReader(Envelope);
				requestXml = reader.ReadToEnd();
				
				var data = HTTPRequest.XmlToObject<RequestModel.Envelope>(requestXml);
                var deliveryItems = data.Body?.UpdateDelivery?.req?.DeliveryItems?.DeliveryItem;
                if(deliveryItems != null) {
                    var deliveryItemMSISDN = deliveryItems
                        .Select(item => item.MSISDN)
                        .Where(item => !string.IsNullOrEmpty(item))
                        .ToArray();
                    msisdnList = string.Join(", ", deliveryItemMSISDN);
                }

                Validation(data);

                var bodyReq = data.Body.UpdateDelivery.req;

                soId = bodyReq.OrderId;
                Guid deliveryStatusId = EntityHelper.GetEntityId(UserConnection, "DgDeliveryStatus", new Dictionary<string, object>() {
                    {"DgCode", bodyReq.DeliveryStatus}
                });

                List<int> lineIDList = deliveryItems
                    .Where(item => !string.IsNullOrEmpty(item.MSISDN))
                    .Select(item => Convert.ToInt32(item.NCCFLineID))
                    .ToList();

                var lineDetailInfo = GetLineDetail(soId, lineIDList);
                deliveryItems = deliveryItems.OrderBy(item => item.NCCFLineID).ToList();
                using (var dbExecutor = UserConnection.EnsureDBConnection())
                {
                    dbExecutor.StartTransaction();
                    try
                    {
                        UpdateStatusLineDetail(dbExecutor, deliveryItems, bodyReq.DONo, deliveryStatusId);
                        dbExecutor.CommitTransaction();
                    }
                    catch (System.Exception e)
                    {
                        dbExecutor.RollbackTransaction();
                        throw new Exception(e.Message);
                    }
                }

                customerName = lineDetailInfo.FirstOrDefault().CustomerName;
                msisdnList = string.Join(", ", lineDetailInfo.Select(item => item.MSISDN).ToArray());

                if(deliveryStatusId == LookupConst.DeliveryStatus.InTransit) {
                    AutoActivation(bodyReq.OrderId);
                } else if(deliveryStatusId == LookupConst.DeliveryStatus.CancelDelivery) {
                    foreach (var lineDetail in lineDetailInfo) {
                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgIsUERP", Column.Parameter(false))
                            .Set("DgIsMMAG", Column.Parameter(false))
                            .Set("DgReservationID", Column.Parameter(string.Empty))
                            .Set("DgCancelItemIMS", Column.Parameter(false))
                            .Set("DgIsCommon", Column.Parameter(false))
                            .Set("DgIsCreateDelivery", Column.Parameter(false))
                            .Set("DgDeviceIMEI", Column.Parameter(string.Empty))
                            .Set("DgSIMCardNumber", Column.Parameter(string.Empty))
                            .Set("DgReleasedToIPL", Column.Parameter(false))
                        .Where("Id").IsEqual(Column.Parameter(lineDetail.Id))
                        .Execute();   
                    }
                }
				
				code = 1;
				message = "Success";
				
				result.Header = new ResponseModel.Header() {
					Action = "http://www.digi.com.my/UpdateDeliveryResponse",
					MessageID = $"urn:uuid:{Guid.NewGuid()}",
					RelatesTo = $"urn:uuid:{Guid.NewGuid()}",
					To = "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous",
					Security = new ResponseModel.Security() {
						Timestamp = new ResponseModel.Timestamp() {
							Created = GetCurrentDateTime(),
							Expires = GetCurrentDateTime(5)
						},
						Id = $"Timestamp-{Guid.NewGuid()}"
					}
				};
			} catch (Exception error) {
				message = error.Message;
				SendEmailError(soId, customerName, msisdnList, message);
			} finally {
				result.Body.UpdateDeliveryResponse.UpdateDeliveryResult.Code = code;
				result.Body.UpdateDeliveryResponse.UpdateDeliveryResult.Message = message;
				
				InsertLog(requestXml, HTTPRequest.XmlToString<ResponseModel.Envelope>(result), soId, code == 1 ? "Success" : "Fail");
			}
			
			return result;
		}

        protected bool IsSONumberExists(string SONumber)
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

		protected virtual string GetCurrentDateTime(int minutes = 0)
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
			var MYTimeAddMinutues = MYTime.AddMinutes(minutes);

            return minutes > 0 ? MYTimeAddMinutues.ToString("yyyy-MM-ddTHH:mm:ssZ") : MYTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

		protected virtual string GetStatusDelivery(string SONumber)
        {
            var result = String.Empty;

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            
            columns.Add("DeliveryStatusCode", esq.AddColumn("DgDeliveryStatus.DgCode"));
			
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", SONumber));
            
            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();

            return entity == null ? string.Empty : entity.GetTypedColumnValue<string>(columns["DeliveryStatusCode"].Name);
        }

        protected void UpdateStatusLineDetail(DBExecutor dbExecutor, List<RequestModel.DeliveryItem> Data, string DONo, Guid DeliveryStatusId)
        {
            var lineDetailIdList = new List<Guid>();
            var lineIdList = Data.Select(item => item.NCCFLineID).ToList();

            var select = $@"
                SELECT
                    Id
                FROM DgLineDetail WITH (UPDLOCK, HOLDLOCK)
                WHERE DgLineDetail.DgLineId IN ({string.Join(", ", lineIdList)})
                ORDER BY DgLineDetail.DgLineId ASC
            ";
            var query = new CustomQuery(UserConnection, select);
            using (var dataReader = query.ExecuteReader(dbExecutor))
            {
                while (dataReader.Read())
                {
                    lineDetailIdList.Add(dataReader.GetColumnValue<Guid>("Id"));
                }
            }

            foreach (var data in Data)
            {
                var prefixItemCode = data.ItemCode.Substring(0, data.ItemCode.IndexOf('_'));
                var statementQuery = new List<string>
                {
                    $"DgPreDeliveryDate = '{DateTime.Now}'",
                    $"DgSODoID = '{DONo}'",
                    $"DgDeliveryStatusId = '{DeliveryStatusId}'"
                };

                if (prefixItemCode == "HST" && !string.IsNullOrEmpty(data.IMEINumber))
                {
                    statementQuery.Add($"DgDeviceIMEI = '{data.IMEINumber}'");
                }

                if (prefixItemCode == "USI" && !string.IsNullOrEmpty(data.SerialNumber))
                {
                    statementQuery.Add($"DgSIMCardNumber = '{data.SerialNumber}'");
                }

                var parsedLineDetailIdList = lineDetailIdList.Select(item => $"'{item}'").ToList();
                var updateQuery = $@"
                    UPDATE DgLineDetail SET {string.Join(", ", statementQuery)}
                    WHERE DgLineId = {data.NCCFLineID}
                ";

                var updateLineQuery = new CustomQuery(UserConnection, updateQuery);
                updateLineQuery.Execute(dbExecutor);
            }
        }
        
		protected void UpdateStatusLineDetail(RequestModel.DeliveryItem Data, string DONo, Guid DeliveryStatusId)
        {
            string prefixItemCode = Data.ItemCode.Substring(0, Data.ItemCode.IndexOf('_'));

            var update = new Update(UserConnection, "DgLineDetail")
                .Set("DgPreDeliveryDate", Column.Parameter(DateTime.Now))
                .Set("DgSODoID", Column.Parameter(DONo))
                .Set("DgMesadRemarks", Column.Parameter(Data.Remarks))
                .Set("DgDeliveryStatusId", Column.Parameter(DeliveryStatusId));

            if (prefixItemCode == "HST" && !string.IsNullOrEmpty(Data.IMEINumber))
            {
                update.Set("DgDeviceIMEI", Column.Parameter(Data.IMEINumber));
            }

            if (prefixItemCode == "USI" && !string.IsNullOrEmpty(Data.SerialNumber))
            {
                update.Set("DgSIMCardNumber", Column.Parameter(Data.SerialNumber));
            }

            update
                .Where("DgLineId").IsEqual(Column.Parameter(Data.NCCFLineID))
                .Execute();
        }
		
		public void AutoActivation(string SONumber) 
        {
            string businessProcessName = "DgBPAutoActivationFromDeliveryStatus";
            var parameters = new Dictionary<string, object> {
				{"SONumber", SONumber}
			};

            JobOptions jobOptions;
			if (!UserConnection.GetIsFeatureEnabled("UseDefaultImportJobOptions")) {
				jobOptions = new JobOptions {
					RequestsRecovery = false
				};
			} else {
				jobOptions = JobOptions.Default;
			}

            UserConnection.RunProcess(
                businessProcessName, 
                MisfireInstruction.SimpleTrigger.FireNow, 
                parameters,
				jobOptions
            );
        }
		
		protected void InsertLog(string RequestBody, string ResponseBody, string SONumber, string Status) 
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = string.Format("{0}_CallbackUpdateStatusDelivery_{1}.txt", currentDateTimeMY, SONumber);
            var log = string.Format("{0}{1}XML Request: {2}{3}{4}{5}XML Response: {6}{7}", 
                currentDateTimeMY, 
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
                section: "DgUERPOrderTracking", 
                values: new Dictionary<string, object>() {
                    {"DgAPIName", "CallbackUpdateStatusDelivery"},
                    {"DgFileName", fileName},
                    {"DgLogFile", log},
                    {"DgOriginalSysDocumentRef", SONumber},
                    {"DgStatus", Status},
                    {"CreatedById", UserConnection.CurrentUser.ContactId}
                }
            );
        }
		
		protected void SendEmailError(string SONumber, string Username, string MSISDN, string ErrorMessage)
		{
			string email = (string)Terrasoft.Core.Configuration.SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_CallbackMMAGFailed", "");
            if(string.IsNullOrEmpty(email)) {
                return;
            }

			string message = $"Please cancel this SO ASAP.<br>"
				+ ErrorMessage
				+ $"<br>Order ID: {SONumber}. MSISDN: {MSISDN}. {Username}"
				+ $"<br><br>This message is auto-generated by NCCF.";

			var param = new MailParam() {
				Subject = $"NCCF-3PL-WS ALERT [UpdateDelivery] {SONumber}",
				Message = message,
				To = email,
				DefaultFooterMessage = true
			};

			Mail.Send(UserConnection, "nccf2-3pl-noreply@celcomdigi.com", param);
		}

        public List<LineDetail> GetLineDetail(string SONumber, List<int> LineIds)
        {
            var result = new List<LineDetail>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("No", esq.AddColumn("DgNo"));
            columns.Add("LineId", esq.AddColumn("DgLineId"));
            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("CustomerName", esq.AddColumn("DgUsername"));

            columns["No"].OrderByAsc(0);
            columns["LineId"].OrderByAsc(1);

            var filterLineId = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            foreach (int lineId in LineIds) {
                filterLineId.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgLineId", lineId));
            }
            esq.Filters.Add(filterLineId);
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", SONumber));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                var data = new LineDetail();
                data.Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
                data.MSISDN = entity.GetTypedColumnValue<string>(columns["MSISDN"].Name);
                data.CustomerName = entity.GetTypedColumnValue<string>(columns["CustomerName"].Name);

                result.Add(data);
            }

            return result;
        }

        public void Validation(RequestModel.Envelope request)
        {
            var bodyReq = request?.Body?.UpdateDelivery?.req;
            if(bodyReq == null) {
                throw new Exception("Request Body is empty");
            }

            string soId = bodyReq?.OrderId;

            string userName = Terrasoft.Core.Configuration.SysSettings.GetValue<string>(UserConnection, "DgUsernameUpdateDeliveryNCCFv2", "");
            string userPassword = Terrasoft.Core.Configuration.SysSettings.GetValue<string>(UserConnection, "DgPasswordUpdateDeliveryNCCFv2", "");

            if(request.Header.Security.UsernameToken.Username != userName || request.Header.Security.UsernameToken.Password.Text != userPassword) {
                throw new Exception("A security error was encountered when verifying the message");
            }

            if(String.IsNullOrEmpty(bodyReq?.OrderId.Trim())) {
                throw new Exception("Order ID not found.");
            }

            if(String.IsNullOrEmpty(bodyReq?.DONo.Trim())) {
                throw new Exception("DO Number not found.");
            }

            if(String.IsNullOrEmpty(bodyReq?.DeliveryStatus.Trim())) {
                throw new Exception("Delivery Status not found.");
            }

            if(String.IsNullOrEmpty(bodyReq?.DeliveryDT)) {
                throw new Exception("Delivery DateTime not found.");
            }
            
            if(bodyReq.DeliveryItems == null) {
                throw new Exception("Delivery Items not found.");
            }
            
            if(bodyReq.DeliveryItems.DeliveryItem == null || (bodyReq.DeliveryItems.DeliveryItem != null && bodyReq.DeliveryItems.DeliveryItem.Count == 0)) {
                throw new Exception("Delivery Items is empty.");
            }

            if(!IsSONumberExists(bodyReq.OrderId)) {
                throw new Exception($"Order ID {bodyReq.OrderId} not found.");
            }

            if(GetStatusDelivery(bodyReq.OrderId) == "02") {
                throw new Exception("Incorrect status update sequence.");
            }
            
            foreach (var itemDelivery in bodyReq.DeliveryItems.DeliveryItem) {
                var errorList = new List<string>();
                if(string.IsNullOrEmpty(itemDelivery.ItemCode)) {
                    errorList.Add("Item Code is empty");
                }
                    
                if(string.IsNullOrEmpty(itemDelivery.SerialNumber)) {
                    errorList.Add("Serial Number is empty");
                }
                
                if(string.IsNullOrEmpty(itemDelivery.IMEINumber)) {
                    errorList.Add("IMEI Number is empty");
                }
                
                if(string.IsNullOrEmpty(itemDelivery.MSISDN)) {
                    errorList.Add("MSISDN is empty");
                }
                
                if(errorList.Count > 0) {
                    throw new Exception("Delivery Items: " + string.Join(", ", errorList.ToArray()));
                }
            }
        }
	}
}