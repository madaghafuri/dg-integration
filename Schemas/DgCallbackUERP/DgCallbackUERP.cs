using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Globalization;
using Terrasoft.Configuration;
using System.Threading.Tasks;
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
using DgSubmission.DgHistorySubmissionService;
using DgIntegration.DgCommonInventory;
using ISAEntityHelper.EntityHelper;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using DgIntegration.DgAuto3PLProcess;
using DgIntegration.DgAuto3PLProcessDMS;

namespace DgIntegration.DgCreateCustomerSalesOrderResponse
{
    public class CreateCustomerSalesOrderResponseService
	{
        protected UserConnection UserConnection;

		private Guid submissionId;
        public CreateCustomerSalesOrderResponseService(UserConnection UserConnection) 
        {
        	this.UserConnection = UserConnection;
        }

        public Response Init(Request Data)
        {
            var result = new Response();
			
			string errorCode = string.Empty;
			string errorDescription = String.Empty;
			string status = "Success";
            
            var req = Data?.UERPCreateCustomerSalesOrderResponse;
            string soID = req?.OrigSysDocumentRef;
            string orderNumber = req?.UERPOrderNumber;
            string orderDate = req?.UERPSalesOrderCreationDate;
			string uerpMessage = req?.Message;
            string uerpStatus = req?.Status;
            bool isDMS = SysSettings.GetValue<bool>(UserConnection, "DgIs3PLWithDMS", false);
            
            try {
				if(string.IsNullOrEmpty(soID)) {
                    errorCode = "51";	
					throw new Exception("OrigSysDocumentRef cannot be null or empty");
				}
                
                if(string.IsNullOrEmpty(uerpStatus)) {
                    errorCode = "52";
                    throw new Exception("Status cannot be null or empty");
                }

                if(string.IsNullOrEmpty(uerpMessage)) {
                    errorCode = "53";
                    throw new Exception("Message cannot be null or empty");
                }

                var lineDetailSelected = GetLineDetail(soID);
                if(lineDetailSelected.Count == 0) {
                    errorCode = "54";
                    throw new Exception("OrigSysDocumentRef not found");
                }
				
				/*if(!string.IsNullOrEmpty(lineDetailSelected.FirstOrDefault().SODONumber)) {
					errorCode = "55";
                    throw new Exception("UERPOrderNumber is already filled");
				}*/
				if(lineDetailSelected.FirstOrDefault().IsMMAG) {
					errorCode = "57";
					throw new Exception($"SO Number {soID} has been processed to MMAG");
				}

                this.submissionId = lineDetailSelected.FirstOrDefault()?.SubmissionId ?? Guid.Empty;
                
				var lineCommonInventory = lineDetailSelected
                    .Where(item => !string.IsNullOrEmpty(item.StoreID) && !string.IsNullOrEmpty(item.ReservationID) && item.IsReleased3PL)
                    .GroupBy(item => new {
                        item.ReservationID, 
                        item.StoreID
                    })
                    .Select(item => new Dictionary<string, string>() {
                        {"ReservationID", item.Key.ReservationID},
                        {"StoreID", item.Key.StoreID}
                    })
                    .ToList();       

                if(uerpStatus != "Success") {
                    SendFailEmail(lineDetailSelected.FirstOrDefault(), uerpMessage);

                    if (isDMS) {
                        AutoUnreserveDMS(soID);
                    } else {
                        AutoUnreserve(soID, string.Empty, lineCommonInventory);
                    }
					
                    foreach (var lineDetail in lineDetailSelected) {
                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgReleasedToIPL", Column.Parameter(false))
                            .Set("DgReleasedToUERP", Column.Parameter(false))
                            .Set("DgIsUERP", Column.Parameter(false))
                            .Set("DgIsCommon", Column.Parameter(false))
                        .Where("Id").IsEqual(Column.Parameter(lineDetail.Id))
                        .Execute();   
                    }
					
                    errorCode = "55";
                    throw new Exception("SO creation failed in UERP, Sent Email to User");
                }

                if(string.IsNullOrEmpty(orderNumber)) {
                    errorCode = "56";
                    throw new Exception("UERPOrderNumber cannot be null or empty");
                }

                foreach (var lineDetail in lineDetailSelected) {
                    var update = new Update(UserConnection, "DgLineDetail")
                        .Set("DgOFSDoNo", Column.Parameter(orderNumber))
                        .Set("DgIsCallbackUERP", Column.Parameter(true));

                    if(!string.IsNullOrEmpty(orderDate)) {
                        update.Set("DgSODate", Column.Parameter(DateTime.Parse(orderDate)));
                    }

                    update
                        .Where("Id").IsEqual(Column.Parameter(lineDetail.Id))
                        .Execute();
                }

                var lineForMMAG = lineDetailSelected
                    .Where(item => item.IsReleased3PL && string.IsNullOrEmpty(item.SODONumber))
                    .Select(item => item.Id)
                    .ToList();

                if (isDMS) {
                    if (lineCommonInventory.Count > 0)
                    {
                        AutoCreateProduct(soID, orderNumber, lineCommonInventory);
                    }
                } else {
                    if(lineCommonInventory.Count > 0) {
                        AutoCreateDelivery(soID, orderNumber, lineCommonInventory);
                    } else if(lineForMMAG.Count > 0) {
                        AutoMMAG(soID, orderNumber);
                    }
                }

            } catch (Exception error) {
				errorDescription = error.Message;
				status = "Fail";

                if(string.IsNullOrEmpty(errorCode)) {
                    errorCode = "-1";
                    SendErrorEmail(JsonConvert.SerializeObject(Data), error);
                }
            } finally {
				InsertLog(JsonConvert.SerializeObject(Data), JsonConvert.SerializeObject(new Response() {
				    resultStatus = new ResponseBody() {
                        ErrorCode = errorCode,
                        ErrorDescription = errorDescription,
                        Status = status,
                    }
                }), soID, status);
				
                HistorySubmissionService.CallbackUERP(
                    UserConnection: UserConnection,
                    SubmissionId: this.submissionId,
                    SOId: soID,
                    Message: Data?.UERPCreateCustomerSalesOrderResponse?.Message ?? errorDescription
                );
			}

            return new Response() {
				resultStatus = new ResponseBody() {
					ErrorCode = errorCode,
					ErrorDescription = errorDescription,
					Status = status
				}
			};
        }

        public List<LineDetailSelected> GetLineDetail(string SONumber)
        {
            var result = new List<LineDetailSelected>();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");

            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("Id", esq.AddColumn("Id"));
			columns.Add("SubmissionId", esq.AddColumn("DgSubmission.Id"));
            columns.Add("SONumber", esq.AddColumn("DgSOID"));
            columns.Add("SODONumber", esq.AddColumn("DgSODoID"));
            columns.Add("ReservationID", esq.AddColumn("DgReservationID"));
            columns.Add("StoreID", esq.AddColumn("Dg3PLService.DgStoreID"));
            columns.Add("ReleasedById", esq.AddColumn("Dg3PLReleasedBy.Id"));
            columns.Add("ReleasedByName", esq.AddColumn("Dg3PLReleasedBy.Name"));
            columns.Add("ReleasedByEmail", esq.AddColumn("Dg3PLReleasedBy.Email"));
            columns.Add("IsReleasedUERP", esq.AddColumn("DgReleasedToUERP"));
            columns.Add("IsReleased3PL", esq.AddColumn("DgReleasedToIPL"));
            columns.Add("DealerID", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerID"));
            columns.Add("DealerEmail", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerEmail"));
			columns.Add("IsMMAG", esq.AddColumn("DgIsMMAG"));

            var filterSelected = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            filterSelected.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToUERP", true));
            filterSelected.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgReleasedToIPL", true));

            esq.Filters.Add(filterSelected);
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSOID", SONumber));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities) {
                var data = new LineDetailSelected() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    SubmissionId = entity.GetTypedColumnValue<Guid>(columns["SubmissionId"].Name),
                    SONumber = entity.GetTypedColumnValue<string>(columns["SONumber"].Name),
                    SODONumber = entity.GetTypedColumnValue<string>(columns["SODONumber"].Name),
					ReservationID = entity.GetTypedColumnValue<string>(columns["ReservationID"].Name),
					StoreID = entity.GetTypedColumnValue<string>(columns["StoreID"].Name),
                    ReleasedById = entity.GetTypedColumnValue<Guid>(columns["ReleasedById"].Name),
                    ReleasedByName = entity.GetTypedColumnValue<string>(columns["ReleasedByName"].Name),
                    ReleasedByEmail = entity.GetTypedColumnValue<string>(columns["ReleasedByEmail"].Name),
                    IsReleasedUERP = entity.GetTypedColumnValue<bool>(columns["IsReleasedUERP"].Name),
                    IsReleased3PL = entity.GetTypedColumnValue<bool>(columns["IsReleased3PL"].Name),
                    DealerID = entity.GetTypedColumnValue<string>(columns["DealerID"].Name),
                    DealerEmail = entity.GetTypedColumnValue<string>(columns["DealerEmail"].Name),
					IsMMAG = entity.GetTypedColumnValue<bool>(columns["IsMMAG"].Name)
                };
                result.Add(data);
            }

            return result;
        }

        protected void InsertLog(string RequestBody, string ResponseBody, string SONumber, string Status) 
        {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = string.Format("{0}_CallbackUpdateDONumber_{1}.txt", currentDateTimeMY, SONumber);
            var log = string.Format("{0}{1}Json Request: {2}{3}{4}{5}Json Response: {6}{7}", 
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
                    {"DgAPIName", "CallbackUpdateDONumber"},
                    {"DgFileName", fileName},
                    {"DgLogFile", log},
                    {"DgOriginalSysDocumentRef", SONumber},
                    {"DgStatus", Status},
                    {"CreatedById", UserConnection.CurrentUser.ContactId}
                }
            );
        }

        protected void SendFailEmail(LineDetailSelected LineDetail, string FailMessage)
        {		
			string message = $"Dear {LineDetail.ReleasedByName}, "
                + $"<br><br> SO creation for SO Number <strong>{LineDetail.SONumber}</strong> has been failed, "
                + $"due to the following Exception: <strong>{FailMessage}</strong>. <BR><BR>This message is auto generated by NCCF.";

            var param = new MailParam() {
                Subject = "NCCF UERP SO Creation Failed",
                Message = message,
                To = LineDetail.ReleasedByEmail,
				DefaultFooterMessage = true
                // CC = 
            };
            Mail.Send(UserConnection, "nccf2-uerp-socreation@celcomdigi.com", param);
        }

        protected void SendErrorEmail(string Request, Exception Exception)
        {
            string email = SysSettings.GetValue<string>(UserConnection, "DgEmailErrorCallbackUERPNotification", string.Empty);
            string rawRequestEscape = System.Web.HttpUtility.HtmlEncode(Request);
            string now = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss zz");
            string message = "Dear Admin, We would like to inform you that the system has encountered an error. The details are as follows:<br><br>"
                + $"<b>Error time:</b> {now}<br>"
                + $"<b>Error description:</b> {Exception.Message}<br>"
                + $"<b>Stack trace:</b> {Exception.StackTrace}<br>"
                + $"<b>Raw Request:</b> <br>{rawRequestEscape}<br>"
                + "<br>Please revert back to <b>Order Fulfillment Team</b> for further information"
                + "<br><br>Thank you."
                + "<br><br><b>This message is auto generated by NCCF Web.</b>";

            var param = new MailParam() {
                Subject = "[ERROR] NCCF UERP SO Creation Failed",
                Message = message,
                To = email,
                // CC = 
            };
            Mail.Send(UserConnection, "nccf2-uerp-socreation@celcomdigi.com", param);
        }
        
        protected virtual void AutoMMAG(string SOID, string OFSDoNo)
        {
            string businessProcessName = "DgBPAutoMMAGProcess";
            var parameters = new Dictionary<string, object> {
				{"SubmissionId", this.submissionId},
                {"SOID", SOID},
                {"OFSDoNo", OFSDoNo}
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
		
		protected virtual void AutoCreateDelivery(string SOID, string OFSDoNo, List<Dictionary<string, string>> Data)
        {
            string businessProcessName = "DgBPAutoCreateDeliveryProcess";
            var parameters = new Dictionary<string, object> {
				{"SubmissionId", this.submissionId},
                {"SOID", SOID},
				{"OFSDoNo", OFSDoNo},
                {"LineCommonInventory", JsonConvert.SerializeObject(Data)}
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
		
		protected virtual void AutoUnreserve(string SOID, string OFSDoNo, List<Dictionary<string, string>> Data)
        {
            string businessProcessName = "DgBPAutoUnreserveProcess";
            var parameters = new Dictionary<string, object> {
				{"SubmissionId", this.submissionId},
                {"SOID", SOID},
				{"OFSDoNo", OFSDoNo},
                {"LineCommonInventory", JsonConvert.SerializeObject(Data)}
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

        protected async void AutoUnreserveDMS(string SOID)
        {
            var autoService = new AutoUnreserveDMS(UserConnection, SOID, this.submissionId);
            await autoService.Run();
        }

        protected virtual void AutoCreateProduct(string SOID, string OFSDoNo, List<Dictionary<string, string>> Data)
        {
            string businessProcessName = "DgBPAutoCreateProductProcess";
            var parameters = new Dictionary<string, object> {
                {"SOID", SOID},
                {"OFSDoNo", OFSDoNo},
                {"SubmissionId", this.submissionId},
                {"LineCommonInventory", JsonConvert.SerializeObject(Data)}
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
    }

    public class LineDetailSelected
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string SONumber { get; set; }
        public string SODONumber { get; set; }
        public string StoreID { get; set; }
        public string ReservationID { get; set; }
        public Guid ReleasedById { get; set; }
        public string ReleasedByName { get; set; }
        public string ReleasedByEmail { get; set; }
        public bool IsReleasedUERP { get; set; }
        public bool IsReleased3PL { get; set; }
        public string DealerID { get; set; }
        public string DealerEmail { get; set; }
		public bool IsMMAG { get; set; }
    }

    public class Request
    {
        public RequestBody UERPCreateCustomerSalesOrderResponse { get; set; }
    }

    public class RequestBody
    {
        public string UERPOrderNumber { get; set; }
        public string OrigSysDocumentRef { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string UERPSalesOrderCreationDate { get; set; }
    }

    public class Response
    {
        public ResponseBody resultStatus { get; set; }
    }

    public class ResponseBody
    {
        public string ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public string Status { get; set; }
    }
}