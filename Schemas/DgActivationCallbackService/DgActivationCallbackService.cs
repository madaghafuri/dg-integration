using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Quartz;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Core.Scheduler;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData;
using DgSubmission.DgHistorySubmissionService;
using DgSubmission.DgLineDetail;
using DgIntegration.DgLineActivation;
using DgIntegration.DgSCMSGetDealerInfoService;
using ISAHttpRequest.ISAHttpRequest;
using ISAIntegrationSetup;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;
using DgCRMIntegration.DgAddVPNGroupMembers;
using CRMRequestV1 = DgIntegration.DgActivationCallbackService.CRMRequestV1;
using CRMRequestV2 = DgIntegration.DgActivationCallbackService.CRMRequestV2;
using CSGRequest = DgIntegration.DgActivationCallbackService.CSGRequest;
using CRMResponse = DgIntegration.DgActivationCallbackService.CRMResponse;
using CSGResponse =  DgIntegration.DgActivationCallbackService.CSGResponse;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgActivationCallbackService
{
    public class ActivationCallbackService
    {
		protected UserConnection UserConnection;
        protected ActivationCallbackRequest request;
        protected ActivationCallbackResponse response;
        protected string transactionID;
        protected string errorCode;
        protected string rawRequest;
		private bool isCRM;
		private bool isCSG;

        public ActivationCallbackService(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            this.request = new ActivationCallbackRequest();
			
            this.response = new ActivationCallbackResponse();
			this.response.Header = new Header();
			this.response.Status = new Status();
        }

        public virtual T Process<T>(Stream Param)
        {
            StreamReader reader = new StreamReader(Param);
            string xml = reader.ReadToEnd();
            
            return Process<T>(xml);
        }

        public virtual T Process<T>(string Param)
        {
            bool isSuccess = false;
            string message = string.Empty;

            T result;
            Type itemType = typeof(T);
            this.isCRM = itemType == typeof(CRMResponse.UpdateOrderStatusResponse);
            this.isCSG = itemType == typeof(CSGResponse.Envelope);

            this.rawRequest = Param;

            if(!this.isCRM && !this.isCSG) {
                throw new Exception("Type of response callback activation is not valid");
            }
            
            if(this.isCRM) {

                try {
                    var paramCRMv1 = HTTPRequest.XmlToObject<CRMRequestV1.request>(Param);
                    MapToModel(paramCRMv1);

                    isSuccess = true;
                } catch (Exception e) {
                    message = $"Fail convert XML Request to Model. {e.Message}";
                }

                if(!isSuccess) {
                    try {
                        var paramCRMv2 = HTTPRequest.XmlToObject<CRMRequestV2.Envelope>(Param);
                        MapToModel(paramCRMv2);

                        isSuccess = true;
                    } catch(Exception e) {
                        SendEmailError(e, "Fail convert XML Request to Model");
                        message = $"Fail convert XML Request to Model. {e.Message}";
                    }
                }

                if(!isSuccess) {
					this.response.Status.StatusCode = "2";
					this.response.Status.ErrorCode = "-9999";
					this.response.Status.ErrorDescription = message;
					
                    result = (T)(object) ResponseCRM(message);
                    string resultInString = HTTPRequest.XmlToString<CRMResponse.UpdateOrderStatusResponse>(result);
                    ACDCLog(Guid.Empty, resultInString, false, message);

                    return result;
                }

                return ProcessCRM<T>();

            }

            try {
                var paramCSG = HTTPRequest.XmlToObject<CSGRequest.Envelope>(Param);
                MapToModel(paramCSG);
                                
                isSuccess = true;
            } catch (Exception e) {
                message = $"Fail convert XML Request to Model. {e.Message}";
            }

            if(!isSuccess) {
                try {
                    XDocument xmlDoc = XDocument.Parse(Param);
                    string jsonResponse = JsonConvert.SerializeXNode(xmlDoc);
                    var cleanResponseAsJson = Regex.Replace(jsonResponse, "digi:|digi1:|digi2:|soap:|@xmlns:|ns4:|#","");
                    var requestxmlObj = JsonConvert.DeserializeObject<CSGRequest.RequestData>(cleanResponseAsJson);
                
                    MapToModel(requestxmlObj.Envelope);

                    isSuccess = true;
                } catch (Exception e) {
                    SendEmailError(e, "Fail convert XML Request to Model");
                    message = $"Fail convert XML Request to Model. {e.Message}";
                }
            }

            if(!isSuccess) {
                this.response.Status.StatusCode = "2";
                this.response.Status.ErrorCode = "-9999";
                this.response.Status.ErrorDescription = message;
                
                result = (T)(object) ResponseCSG(message);
                string resultInString = HTTPRequest.XmlToString<CSGResponse.Envelope>(result);
                    
                ACDCLog(Guid.Empty, resultInString, false, message);

                return result;
            }

            return ProcessCSG<T>();
        }

        protected virtual T ProcessCRM<T>()
        {
            T result;
			
			var lines = new List<LineDetail>();
            try {
                Validation();

                this.response.Header.ReferenceId = this.request.Header.ReferenceId;
                this.response.Header.ChannelId = this.request.Header.ChannelId;
                this.response.Header.ChannelMedia = this.request.Header.ChannelMedia;

                var sfaLineActivationTask = new List<Task>();
                Task sfaLineActivationTaskResult = null;

                lines = GetLines();
                foreach(var taskRecord in this.request.TaskList) {
                    LineDetail line = null;

                    bool isSuccess = taskRecord.TaskStatus.ToUpper().Trim() == "SUCCESSFUL";
                    string correlationId = taskRecord.CorrelationId;

                    Guid activationStatusId = LookupConst.ActivationStatus.Fail;
                    string activationStatusString = "Fail";
                    
                    if(isSuccess) {
                        activationStatusId = LookupConst.ActivationStatus.Activated;
                        activationStatusString = "Activated";
                    }
					
                    string remark = isSuccess ? activationStatusString : $"Fail from {this.request.Header.ChannelId}. {this.request.Order.Remark ?? string.Empty}";
                    string errorRemark = string.Empty;
                    string remarkHistory = string.Empty;

                    correlationId = Helper.GetValidMSISDN(taskRecord.CorrelationId);
                    line = lines.Where(item => Helper.GetValidMSISDN(item.MSISDN) == correlationId).FirstOrDefault();
                    if(line == null) {
                        this.errorCode = "-0013";
                        throw new Exception("CorrelationId does not match with NCCF record");
                    }

                    remarkHistory =  $"[UpdateOrderStatus] {line.SubmissionType.Name} {activationStatusString} ChannelID: {this.request.Header.ChannelId} TransactionID: {this.transactionID} MSISDN: {line.MSISDN}. {this.request.Order.Remark ?? string.Empty} {errorRemark}".Trim();
                    remark += $" {errorRemark}";
					
                    UpdateLineDetail(line.Id, remark.Trim(), taskRecord, activationStatusId);					
                    AddHistory(line, remarkHistory.Trim());
                    
                    if(line.Source?.Id == LookupConst.Source.SFA) {
                        sfaLineActivationTask.Add(LineActivation.SFAActivationStatus(UserConnection, line, activationStatusString));
                    }

                    if(sfaLineActivationTask.Count > 0) {
                        try {
                            sfaLineActivationTaskResult = Task.WhenAll(sfaLineActivationTask);
                            sfaLineActivationTaskResult.GetAwaiter().GetResult();
                        } catch(Exception e) {}
                    }

                    if(!isSuccess) {
                        SendEmailDealer(line, activationStatusString, remark.Trim()).GetAwaiter().GetResult();
                    } else if(this.request.Order.OrderType == "13") {
                        ConfirmPortIn(line.Id);
                    }
					
					// new or mnp
					if(this.request.Order.OrderType == "12" || this.request.Order.OrderType == "13") {
						AddVPNGroupMembersQueue.SetDetailToProcess(UserConnection, line.Id, isSuccess ? false : true);
					}
                }

                this.response.Status.StatusCode = "1";
            } catch (Exception e) {
                this.response.Status.ErrorCode = string.IsNullOrEmpty(this.errorCode) ? "-9999" : this.errorCode;
                this.response.Status.StatusCode = "2";
                this.response.Status.ErrorDescription = e.Message;

                SendEmailError(e);
            } finally {
                result = (T)(object) ResponseCRM();
                string resultInString = HTTPRequest.XmlToString<CRMResponse.UpdateOrderStatusResponse>(result);

                ACDCLog(lines.Select(item => item.Id).FirstOrDefault(), resultInString, this.response.Status.StatusCode == "1", this.response.Status.ErrorDescription);
            }

            return result;
        }

        protected Dictionary<string, Dictionary<string, string>> GetErrorCodeCSG()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach(var taskRecord in this.request.TaskList) {
                if(taskRecord.TaskErrorList != null && taskRecord.TaskErrorList.Count > 0) {
                    result.Add(taskRecord.CorrelationId, new Dictionary<string, string>());

                    foreach(var taskErrorRecord in taskRecord.TaskErrorList) {
                        if(taskErrorRecord == null || (taskErrorRecord != null && string.IsNullOrEmpty(taskErrorRecord.ErrorDescription))) {
                            continue;
                        }

                        result[taskRecord.CorrelationId].Add(taskErrorRecord.ErrorCode, taskErrorRecord.ErrorDescription);
                    }
                }
            }

            return result;
        }

        protected string GetErrorRemark(TaskRecord taskRecord)
        {
            string errorRemark = string.Empty;
            if(taskRecord.TaskErrorList != null && taskRecord.TaskErrorList.Count > 0) {
                var taskErrorList = new List<string>();
                foreach(var taskErrorRecord in taskRecord.TaskErrorList) {
                    if(taskErrorRecord == null || (taskErrorRecord != null && string.IsNullOrEmpty(taskErrorRecord.ErrorDescription))) {
                        continue;
                    }

                    taskErrorList.Add($"{taskErrorRecord.ErrorCode}: {taskErrorRecord.ErrorDescription}");
                }

                errorRemark = string.Join(". ", taskErrorList.ToArray());
            }

            return errorRemark;
        }
        
        protected void ProcessCSGFailWithoutMSISDN(List<LineDetail> lines)
        {
            string remark = $"Fail from {this.request.Header.ChannelId}.";
            string activationStatusString = "Fail";

            var sfaLineActivationTask = new List<Task>();
            Task sfaLineActivationTaskResult = null;

            foreach(var item in lines) {
                UpdateLineDetail(item.Id, remark, null, LookupConst.ActivationStatus.Fail);

                string remarkHistory = $"[UpdateOrderStatus] {item.SubmissionType.Name} {activationStatusString} ChannelID: {this.request.Header.ChannelId} TransactionID: {this.transactionID} MSISDN: {item.MSISDN}.";
                AddHistory(item, remarkHistory.Trim());

                sfaLineActivationTask.Add(LineActivation.SFAActivationStatus(UserConnection, item, activationStatusString));
                SendEmailDealer(item, activationStatusString, remark.Trim()).GetAwaiter().GetResult();
            }

            if(sfaLineActivationTask.Count > 0) {
                try {
                    sfaLineActivationTaskResult = Task.WhenAll(sfaLineActivationTask);
                    sfaLineActivationTaskResult.GetAwaiter().GetResult();
                } catch(Exception e) {}
            }
            
            var errorCodeCSG = GetErrorCodeCSG();
            string msisdnAllStr = string.Join(", ", lines.Select(item => item.MSISDN).ToArray());
            UpdateMNPRejected(
                lines.FirstOrDefault().SubmissionId, 
                lines.FirstOrDefault().ReleasedById, 
                $"[UpdateOrderStatus] {lines.FirstOrDefault().SubmissionType.Name} {activationStatusString} ChannelID: {this.request.Header.ChannelId} TransactionID: {this.transactionID} MSISDN: {msisdnAllStr}.".Trim(), 
                errorCodeCSG
            );
        }

        protected virtual T ProcessCSG<T>()
        {
            T result;			
			var lines = new List<LineDetail>();
            try {
                Validation();

                this.response.Header.ReferenceId = this.request.Header.ReferenceId;
                this.response.Header.ChannelId = this.request.Header.ChannelId;
                this.response.Header.ChannelMedia = this.request.Header.ChannelMedia;
                
                lines = GetLines();
                var taskListFailWithoutMSISDN = this.request.TaskList
                    .Where(item => string.IsNullOrEmpty(item.CorrelationId) && item.TaskStatus.ToUpper().Trim() != "SUCCESSFUL")
                    .ToList();
                
                if(taskListFailWithoutMSISDN.Count > 0) {
                    ProcessCSGFailWithoutMSISDN(lines);
                } else {
                    bool isTaskContainFail = this.request.TaskList
                        .Where(item => item.TaskStatus.ToUpper().Trim() != "SUCCESSFUL")
                        .ToList()
                        .Count() > 0 ? true : false;

                    var sfaLineActivationTask = new List<Task>();
                    Task sfaLineActivationTaskResult = null;

                    bool isMNPRejected = false;
                    var errorCodeCSG = GetErrorCodeCSG();

                    foreach(var taskRecord in this.request.TaskList) {
                        LineDetail line = null;

                        bool isSuccess = taskRecord.TaskStatus.ToUpper().Trim() == "SUCCESSFUL";
                        string correlationId = taskRecord.CorrelationId;

                        Guid activationStatusId = LookupConst.ActivationStatus.Fail;
                        string activationStatusString = "Fail";
                        
                        if(isSuccess && !isTaskContainFail) {
                            activationStatusId = LookupConst.ActivationStatus.Approved;
                            activationStatusString = "Approved";

                            if(this.request.Order.OrderType == "32") {
                                activationStatusId = LookupConst.ActivationStatus.Cancelled;
                                activationStatusString = "Cancelled";
                            }
                        }
                        
                        string remark = isSuccess && !isTaskContainFail ? activationStatusString : $"Fail from {this.request.Header.ChannelId}.";
                        string errorRemark = GetErrorRemark(taskRecord);
                        string remarkHistory = string.Empty;

                        if(activationStatusId != LookupConst.ActivationStatus.Approved) {
                            isMNPRejected = true;
                        }

                        correlationId = Helper.GetValidMSISDN(taskRecord.CorrelationId);
                        line = lines.Where(item => Helper.GetValidMSISDN(item.MSISDN) == correlationId).FirstOrDefault();
                        if(line == null) {
                            this.errorCode = "-0013";
                            throw new Exception("CorrelationId does not match with NCCF record");
                        }

                        remarkHistory =  $"[UpdateOrderStatus] {line.SubmissionType.Name} {activationStatusString} ChannelID: {this.request.Header.ChannelId} TransactionID: {this.transactionID} MSISDN: {line.MSISDN}. {errorRemark}".Trim();
                        remark += $" {errorRemark}";

                        UpdateLineDetail(line.Id, remark.Trim(), taskRecord, activationStatusId);
                        AddHistory(line, remarkHistory.Trim());
                        
                        if(line.Source?.Id == LookupConst.Source.SFA) {
                            sfaLineActivationTask.Add(LineActivation.SFAActivationStatus(UserConnection, line, activationStatusString));
                        }

                        if(sfaLineActivationTask.Count > 0) {
                            try {
                                sfaLineActivationTaskResult = Task.WhenAll(sfaLineActivationTask);
                                sfaLineActivationTaskResult.GetAwaiter().GetResult();
                            } catch(Exception e) {}
                        }

                        if(!isSuccess) {
                            SendEmailDealer(line, activationStatusString, remark.Trim()).GetAwaiter().GetResult();
                        } else if(isSuccess && !isTaskContainFail) {
                            PortIn(line.Id);
                        }
                    }

                    if(isMNPRejected) {
                        string errorRemarkMNPRejected = string.Join("<br/>", errorCodeCSG.Select(kvp => {
                            string msisdn = kvp.Key;
                            string errorCode = string.Join(". ", kvp.Value.Select(item => $"{item.Key}: {item.Value}"));
                            
                            return $"{msisdn} - {errorCode}";
                        }));
                                            
                        UpdateMNPRejected(
                            lines.FirstOrDefault().SubmissionId, 
                            lines.FirstOrDefault().ReleasedById, 
                            $"[UpdateOrderStatus] {lines.FirstOrDefault().SubmissionType.Name} Fail ChannelID: {this.request.Header.ChannelId} TransactionID: {this.transactionID}.<br/>{errorRemarkMNPRejected}".Trim(), 
                            errorCodeCSG
                        );
                    }
                }

                this.response.Status.StatusCode = "1";
            } catch (Exception e) {
                this.response.Status.ErrorCode = string.IsNullOrEmpty(this.errorCode) ? "-9999" : this.errorCode;
                this.response.Status.StatusCode = "2";
                this.response.Status.ErrorDescription = e.Message;

                SendEmailError(e);
            } finally {
                result = (T)(object) ResponseCSG();
                string resultInString = HTTPRequest.XmlToString<CSGResponse.Envelope>(result);

                ACDCLog(lines.Select(item => item.Id).FirstOrDefault(), resultInString, this.response.Status.StatusCode == "1", this.response.Status.ErrorDescription);
            }

            return result;
        }

        #region Mapping Request/Response to Model
        
        // CRM v1
        protected void MapToModel(CRMRequestV1.request Param)
        {
            if(Param.requestHeader != null) {
                this.request.Header = new Header();
                this.request.Header.ChannelId = Param.requestHeader.ChannelId;
                this.request.Header.ReferenceId = Param.requestHeader.ReferenceId;
                this.request.Header.ChannelMedia = Param.requestHeader.ChannelMedia;
            }

            if(Param.order != null) {
                this.request.Order = new Order();
                this.request.Order.OrderId = Param.order.OrderId;
                this.request.Order.OrderType = Param.order.OrderType;
                this.request.Order.OrderStatus = Param.order.OrderStatus;
                this.request.Order.StartDate = Param.order.StartDate;
                this.request.Order.EndDate = Param.order.EndDate;
                this.request.Order.Remark = Param.order.Remark;
            }

            if(Param.taskList != null && 
                Param.taskList.TaskRecord != null && 
                (Param.taskList.TaskRecord.TaskRecord != null && Param.taskList.TaskRecord.TaskRecord.Count > 0)) {

                this.request.TaskList = new List<TaskRecord>();
                foreach(var item in Param.taskList.TaskRecord.TaskRecord) {
                    var taskRecord = new TaskRecord();
                    taskRecord.TaskId = item.TaskId;
                    taskRecord.TaskStatus = item.TaskStatus;
                    taskRecord.CorrelationId = item.CorrelationId;

                    if(item.CreatedId != null) {
                        taskRecord.CreatedId = new CreatedId();
                        taskRecord.CreatedId.SubscriberId = item.CreatedId.SubscriberId;
                        taskRecord.CreatedId.AccountId = item.CreatedId.AccountId;
                        taskRecord.CreatedId.CustomerId = item.CreatedId.CustomerId;
                    }

                    this.request.TaskList.Add(taskRecord);
                }
            }
        }

        // CRM v2
        protected void MapToModel(CRMRequestV2.Envelope Param)
        {
            if(Param.Body != null && 
                Param.Body.UpdateOrderStatus != null && 
                Param.Body.UpdateOrderStatus.request != null) {

                var header = Param.Body.UpdateOrderStatus.request.requestHeader;
                if(header != null) {
                    this.request.Header = new Header();
                    this.request.Header.ChannelId = header.ChannelId;
                    this.request.Header.ReferenceId = header.ReferenceId;
                    this.request.Header.ChannelMedia = header.ChannelMedia;
                }

                var order = Param.Body.UpdateOrderStatus.request.order;
                if(order != null) {
                    this.request.Order = new Order();
                    this.request.Order.OrderId = order.OrderId;
                    this.request.Order.OrderType = order.OrderType;
                    this.request.Order.OrderStatus = order.OrderStatus;
                    this.request.Order.StartDate = order.StartDate;
                    this.request.Order.EndDate = order.EndDate;
                    this.request.Order.Remark = order.Remark;
                }

                var taskList = Param.Body.UpdateOrderStatus.request.taskList;
                if(taskList != null && 
                    taskList.TaskRecord != null 
                    && (taskList.TaskRecord.TaskRecord != null 
                    && taskList.TaskRecord.TaskRecord.Count > 0)) {
                    
                    this.request.TaskList = new List<TaskRecord>();
                    foreach(var item in taskList.TaskRecord.TaskRecord) {
                        var taskRecord = new TaskRecord();
                        taskRecord.TaskId = item.TaskId;
                        taskRecord.TaskStatus = item.TaskStatus;
                        taskRecord.CorrelationId = item.CorrelationId;

                        if(item.CreatedId != null) {
                            taskRecord.CreatedId = new CreatedId();
                            taskRecord.CreatedId.SubscriberId = item.CreatedId.SubscriberId;
                            taskRecord.CreatedId.AccountId = item.CreatedId.AccountId;
                            taskRecord.CreatedId.CustomerId = item.CreatedId.CustomerId;
                        }

                        this.request.TaskList.Add(taskRecord);
                    }
                }
            }
        }

        // CSG
        protected void MapToModel(CSGRequest.Envelope Param)
        {
            if(Param.Body != null 
                && Param.Body.UpdateOrderStatus != null 
                && Param.Body.UpdateOrderStatus.Request != null) {
                
                var header = Param.Body.UpdateOrderStatus.Request.RequestHeader;
                if(header != null) {
                    this.request.Header = new Header();
                    this.request.Header.ChannelId = header.ChannelId;
                    this.request.Header.ReferenceId = header.ReferenceId;
                    this.request.Header.ChannelMedia = header.ChannelMedia;
                }

                var order = Param.Body.UpdateOrderStatus.Request.Order;
                if(order != null) {
                    this.request.Order = new Order();
                    this.request.Order.OrderId = order.OrderId;
                    this.request.Order.OrderType = order.OrderType;
                    this.request.Order.OrderStatus = order.OrderStatus;
                    this.request.Order.Remark = order.OrderStatusDescription;
                }

                var taskList = Param.Body.UpdateOrderStatus.Request.TaskList;
                if(taskList != null && 
                    taskList.TaskRecord != null 
                    && (taskList.TaskRecord.TaskRecord != null 
                    && taskList.TaskRecord.TaskRecord.Count > 0)) {
                    
                    this.request.TaskList = new List<TaskRecord>();
                    foreach(var item in taskList.TaskRecord.TaskRecord) {
                        var taskRecord = new TaskRecord();
                        taskRecord.TaskId = item.TaskId;
                        taskRecord.TaskStatus = item.TaskStatus;
                        taskRecord.CorrelationId = item.CorrelationId;
                        taskRecord.PortId = item.PortId;

                        if(item.TaskErrorList != null && item.TaskErrorList.TaskErrorRecord != null && item.TaskErrorList.TaskErrorRecord.Count > 0) {
                            taskRecord.TaskErrorList = new List<TaskErrorRecord>();
                            foreach(var taskError in item.TaskErrorList.TaskErrorRecord) {
                                taskRecord.TaskErrorList.Add(new TaskErrorRecord() {
                                    ErrorCode = taskError.ErrorCode,
                                    ErrorDescription = taskError.ErrorDescription
                                });
                            }
                        }

                        this.request.TaskList.Add(taskRecord);
                    }
                }
            }
        }

        protected CRMResponse.UpdateOrderStatusResponse ResponseCRM(string message = "")
        {
            var result = new CRMResponse.UpdateOrderStatusResponse();
            var UpdateOrderStatusResult = new CRMResponse.UpdateOrderStatusResult();
            var responseHeader = new CRMResponse.responseHeader();
            var resultStatus = new CRMResponse.resultStatus();
            
			if(this.response.Header != null) {
				responseHeader.ReferenceId = this.response.Header.ReferenceId;
				responseHeader.ChannelId = this.response.Header.ChannelId;
				responseHeader.ChannelMedia = this.response.Header.ChannelMedia;
			}

			if(this.response.Status != null) {
				resultStatus.StatusCode = this.response.Status.StatusCode;
				resultStatus.ErrorCode = this.response.Status.ErrorCode;
				resultStatus.ErrorDescription = this.response.Status.ErrorDescription;
			} else {
				resultStatus.ErrorCode = "-9999";
                resultStatus.StatusCode = "2";
                resultStatus.ErrorDescription = string.IsNullOrEmpty(message) ? "Something wrong happen" : message;
			}

            UpdateOrderStatusResult.responseHeader = responseHeader;
            UpdateOrderStatusResult.resultStatus = resultStatus;
            result.UpdateOrderStatusResult = UpdateOrderStatusResult;

            return result;
        }

        protected CSGResponse.Envelope ResponseCSG(string message = "")
        {
            var result = new CSGResponse.Envelope();
            var Body = new CSGResponse.Body();
            var UpdateOrderStatusResponse = new CSGResponse.UpdateOrderStatusResponse();
            var UpdateOrderStatusResult = new CSGResponse.UpdateOrderStatusResult();
            var responseHeader = new CSGResponse.responseHeader();
            var resultStatus = new CSGResponse.resultStatus();

            if(this.response.Header != null) {
				responseHeader.ReferenceId = this.response.Header.ReferenceId;
				responseHeader.ChannelId = this.response.Header.ChannelId;
				responseHeader.ChannelMedia = this.response.Header.ChannelMedia;
			}

			if(this.response.Status != null) {
				resultStatus.StatusCode = this.response.Status.StatusCode;
				resultStatus.ErrorCode = this.response.Status.ErrorCode;
				resultStatus.ErrorDescription = this.response.Status.ErrorDescription;
			} else {
				resultStatus.ErrorCode = "-9999";
				resultStatus.StatusCode = "2";
				resultStatus.ErrorDescription = string.IsNullOrEmpty(message) ? "Something wrong happen" : message;
			}
            
            UpdateOrderStatusResult.responseHeader = responseHeader;
            UpdateOrderStatusResult.resultStatus = resultStatus;
            UpdateOrderStatusResponse.UpdateOrderStatusResult = UpdateOrderStatusResult;
            Body.UpdateOrderStatusResponse = UpdateOrderStatusResponse;
            result.Body = Body;            

            return result;
        }

        #endregion

        protected void Validation()
        {
            if(this.request == null) {
                this.errorCode = "-0001";
                throw new Exception("Request cannot be null or empty");
            }

            if(this.request.Header == null) {
                this.errorCode = "-0002";
                throw new Exception("Request Header cannot be null or empty");
            }

            if(string.IsNullOrEmpty(this.request.Header.ChannelId)) {
                this.errorCode = "-0003";
                throw new Exception("Channel ID cannot be null or empty");
            }

            if(string.IsNullOrEmpty(this.request.Header.ReferenceId)) {
                this.errorCode = "-0004";
                throw new Exception("Reference ID cannot be null or empty");
            }

            if(this.request.Order == null) {
                this.errorCode = "-0005";
                throw new Exception("Request Order cannot be null or empty");
            }

            if(string.IsNullOrEmpty(this.request.Order.OrderType)) {
                this.errorCode = "-0006";
                throw new Exception("Order Type cannot be null or empty");
            }
            
            if(string.IsNullOrEmpty(this.request.Order.OrderStatus)) {
                this.errorCode = "-0007";
                throw new Exception("Order Status cannot be null or empty");
            }

            bool orderTypeIsInt = true;
            try {
                Convert.ToInt32(this.request.Order.OrderType.Trim());
            } catch(Exception e) {
                orderTypeIsInt = false;
            }

            if(!orderTypeIsInt) {
                this.errorCode = "-0008";
                throw new Exception("Invalid Order Type");
            }

            var validOrderType = new List<string>() {
                "9", "10", "12", "13",
                "30", "31", "32"
            };
            bool isOrderTypeValid = validOrderType.FindIndex(item => item == this.request.Order.OrderType) != -1;
            if(!isOrderTypeValid) {
                this.errorCode = "-0010";
                throw new Exception("Unsupported Order Type");
            }

            if(new[] { "9", "10", "12", "13" }.Contains(this.request.Order.OrderType)) {
                this.transactionID = this.request.Order.OrderId;
            } else if(new[] { "30", "31", "32" }.Contains(this.request.Order.OrderType)) {
                this.transactionID = this.request.Header.ReferenceId;
            }

            if(string.IsNullOrEmpty(this.transactionID)) {
                this.errorCode = "-0009";
                throw new Exception("Order Id cannot be null or empty");
            }
            
            if(!IsTransactionIdExists()) {
                this.errorCode = "-0009";
                throw new Exception("Transaction ID does not exist in NCCF");
            }

            if(this.request.TaskList == null || (this.request.TaskList != null && this.request.TaskList.Count == 0)) {
                this.errorCode = "-0011";
                throw new Exception("TaskRecord cannot be null or empty");
            }

            foreach(var taskRecord in this.request.TaskList) {
                if(new[] { "9", "10", "12", "13" }.Contains(this.request.Order.OrderType)) {
                    if(string.IsNullOrEmpty(taskRecord.CorrelationId)) {
                        this.errorCode = "-0012";
                        throw new Exception("CorrelationId cannot be null or empty");
                    }
                }

                if(string.IsNullOrEmpty(taskRecord.TaskStatus)) {
                    this.errorCode = "-0014";
                    throw new Exception("TaskStatus cannot be null or empty");
                }
            }
        }

        // filter by submission type based on order type
        // 30, 31, 32, 13: MNP
        // 12: New
        // 9, 10: COP
        // filter by transaction id
        // 30, 31, 32: reference id
        // 9, 10, 12, 13: order id
        protected bool IsTransactionIdExists()
        {
            if(string.IsNullOrEmpty(this.transactionID)) {
                return false;
            }

            bool isExists = false;
            Guid submissionTypeId = Guid.Empty;
            Guid activationStatusId = LookupConst.ActivationStatus.Released;

            if(new[] { "13", "30", "31", "32" }.Contains(this.request.Order.OrderType)) {
                submissionTypeId = LookupConst.SubmissionType.MNP;
                if(this.request.Order.OrderType == "13") {
                    activationStatusId = LookupConst.ActivationStatus.Approved;
                }
            } else if(new[] { "9", "10" }.Contains(this.request.Order.OrderType)) {
                submissionTypeId = LookupConst.SubmissionType.COP;
            } else if(this.request.Order.OrderType == "12") {
                submissionTypeId = LookupConst.SubmissionType.NEW;
            }
            
            var select = new Select(UserConnection)
                .Column(Func.Count("DgLineDetail", "DgActivationTransactionId")).As("Total")
            .From("DgLineDetail")
            .Join(JoinType.LeftOuter, "DgSubmission")
                .On("DgSubmission", "Id").IsEqual("DgLineDetail", "DgSubmissionId")
            .Where("DgLineDetail", "DgActivationTransactionId").IsEqual(Column.Parameter(this.transactionID)) 
            .And("DgSubmission", "DgSubmissionTypeId").IsEqual(Column.Parameter(submissionTypeId))
            .And("DgLineDetail", "DgActivationStatusId").IsEqual(Column.Parameter(activationStatusId)) as Select;

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                using (IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        int total = dataReader.GetColumnValue<int>("Total");
                        isExists = total > 0;
                    }
                }
            }

            return isExists;
        }

        // filter by submission type based on order type
        // 30, 31, 32, 13: MNP
        // 12: New
        // 9, 10: COP
        // filter by transaction id
        // 30, 31, 32: reference id
        // 9, 10, 12, 13: order id
        protected List<LineDetail> GetLines()
        {
            if(string.IsNullOrEmpty(this.transactionID)) {
                return null;
            }

            var result = new List<LineDetail>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("LineId", esq.AddColumn("DgLineId"));
            columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
            columns.Add("SerialNumber", esq.AddColumn("DgSubmission.DgSerialNumber"));
            columns.Add("SubmissionId", esq.AddColumn("DgSubmission.Id"));
            columns.Add("CustomerName", esq.AddColumn("DgUsername"));
            columns.Add("ReleasedById", esq.AddColumn("DgReleasedBy.Id"));

            columns.Add("SubmissionType_Id", esq.AddColumn("DgSubmission.DgSubmissionType.Id"));
            columns.Add("SubmissionType_Name", esq.AddColumn("DgSubmission.DgSubmissionType.Name"));
            columns.Add("SubmissionType_Code", esq.AddColumn("DgSubmission.DgSubmissionType.DgCode"));

            columns.Add("ActivationTransactionID", esq.AddColumn("DgActivationTransactionId"));
            columns.Add("ActivationOrderID", esq.AddColumn("DgActivationOrderID"));
            columns.Add("ActivationPortInTransactionID", esq.AddColumn("DgPortInTransactionID"));
            columns.Add("ActivationPortInMessageID", esq.AddColumn("DgPortInMessageID"));

            columns.Add("Dealer_Id", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.Id"));
            columns.Add("Dealer_Name", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerName"));
            columns.Add("Dealer_Code", esq.AddColumn("DgSubmission.DgCRMGroup.DgDealer.DgDealerID"));

            columns.Add("Source_Id", esq.AddColumn("DgSubmission.DgSource.Id"));
            columns.Add("Source_Name", esq.AddColumn("DgSubmission.DgSource.Name"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationTransactionId", this.transactionID));

            Guid submissionTypeId = Guid.Empty;
            Guid activationStatusId = LookupConst.ActivationStatus.Released;

            if(new[] { "13", "30", "31", "32" }.Contains(this.request.Order.OrderType)) {
                submissionTypeId = LookupConst.SubmissionType.MNP;
                if(this.request.Order.OrderType == "13") {
                    activationStatusId = LookupConst.ActivationStatus.Approved;
                }
            } else if(new[] { "9", "10" }.Contains(this.request.Order.OrderType)) {
                submissionTypeId = LookupConst.SubmissionType.COP;
            } else if(this.request.Order.OrderType == "12") {
                submissionTypeId = LookupConst.SubmissionType.NEW;
            }

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.DgSubmissionType", submissionTypeId));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgActivationStatus", activationStatusId));

            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(new LineDetail() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    LineId = entity.GetTypedColumnValue<int>(columns["LineId"].Name),
                    MSISDN = entity.GetTypedColumnValue<string>(columns["MSISDN"].Name),
                    SerialNumber = entity.GetTypedColumnValue<string>(columns["SerialNumber"].Name),
                    SubmissionId = entity.GetTypedColumnValue<Guid>(columns["SubmissionId"].Name),
                    CustomerName = entity.GetTypedColumnValue<string>(columns["CustomerName"].Name),
                    SubmissionType = new DgMasterData.Lookup() {
                        Id = entity.GetTypedColumnValue<Guid>(columns["SubmissionType_Id"].Name),
                        Name = entity.GetTypedColumnValue<string>(columns["SubmissionType_Name"].Name),
                        Code = entity.GetTypedColumnValue<string>(columns["SubmissionType_Code"].Name)
                    },
                    ActivationTransactionID = entity.GetTypedColumnValue<string>(columns["ActivationTransactionID"].Name),
                    ActivationOrderID = entity.GetTypedColumnValue<string>(columns["ActivationOrderID"].Name),
                    ActivationPortInTransactionID = entity.GetTypedColumnValue<string>(columns["ActivationPortInTransactionID"].Name),
                    ActivationPortInMessageID = entity.GetTypedColumnValue<string>(columns["ActivationPortInMessageID"].Name),
                    Dealer = new DgMasterData.Lookup() {
                        Id = entity.GetTypedColumnValue<Guid>(columns["Dealer_Id"].Name),
                        Name = entity.GetTypedColumnValue<string>(columns["Dealer_Name"].Name),
                        Code = entity.GetTypedColumnValue<string>(columns["Dealer_Code"].Name)
                    },
                    Source = new DgMasterData.Lookup() {
                        Id = entity.GetTypedColumnValue<Guid>(columns["Source_Id"].Name),
                        Name = entity.GetTypedColumnValue<string>(columns["Source_Name"].Name)
                    },
                    ReleasedById = entity.GetTypedColumnValue<Guid>(columns["ReleasedById"].Name) 
                });
            }

            return result;
        }

        protected void UpdateLineDetail(Guid RecordId, string Remark, TaskRecord TaskRecord, Guid ActivationStatusId)
        {
            var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgLineDetail");
            var entity = schema.CreateEntity(UserConnection);

            entity.FetchFromDB("Id", RecordId);            

            entity.SetColumnValue("DgActivationStatusId", ActivationStatusId);
            entity.SetColumnValue("DgRemark", Remark);
			
			if(ActivationStatusId == LookupConst.ActivationStatus.Activated) {
				entity.SetColumnValue("DgDateTimeActivated", DateTime.UtcNow);
			}

            if(new[] { "30", "31", "32" }.Contains(this.request.Order.OrderType) && ActivationStatusId == LookupConst.ActivationStatus.Approved) {
                entity.SetColumnValue("DgPortInTransactionID", this.request.Order.OrderId);
            }

            if(ActivationStatusId == LookupConst.ActivationStatus.Fail) {
                entity.SetColumnValue("DgReleased", false);
                entity.SetColumnValue("DgActivationOrderID", string.Empty);
            }
			
            if(TaskRecord != null) {
                if(!string.IsNullOrEmpty(TaskRecord.StartDate)) {
                    entity.SetColumnValue("DgActivationStartDate", Convert.ToDateTime(TaskRecord.StartDate));
                }

                if(!string.IsNullOrEmpty(TaskRecord.EndDate)) {
                    entity.SetColumnValue("DgActivationEndDate", Convert.ToDateTime(TaskRecord.EndDate));
                }

                if(!string.IsNullOrEmpty(TaskRecord.PortId)) {
                    entity.SetColumnValue("DgActivationPortId", TaskRecord.PortId);
                }

                if(TaskRecord.CreatedId != null) {
                    if(!string.IsNullOrEmpty(TaskRecord.CreatedId.SubscriberId)) {
                        entity.SetColumnValue("DgActivationSubscriberId", TaskRecord.CreatedId.SubscriberId);
                    }

                    if(!string.IsNullOrEmpty(TaskRecord.CreatedId.AccountId)) {
                        entity.SetColumnValue("DgActivationAccountId", TaskRecord.CreatedId.AccountId);
                    }

                    if(!string.IsNullOrEmpty(TaskRecord.CreatedId.CustomerId)) {
                        entity.SetColumnValue("DgActivationCustomerId", TaskRecord.CreatedId.CustomerId);
                    }
                }
            }

            entity.Save(false);
        }
    	
		protected void UpdateMNPRejected(Guid SubmissionId, Guid ReleasedById, string Remark, Dictionary<string, Dictionary<string, string>> ErrorCode)
		{
            UpdateOPDetail(SubmissionId, ErrorCode);
			
            string OPDetailRemark = GetOPDetailRemark(SubmissionId);
            string OPRemark = $"{OPDetailRemark}<br/><br/>{Remark}";

			var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgSubmission");
			var entity = schema.CreateEntity(UserConnection);
			
			entity.FetchFromDB("Id", SubmissionId);
			
			entity.SetColumnValue("DgProgressStatusId", LookupConst.OPStatus.MNPRejected);
            entity.SetColumnValue("DgOPDatetime", DateTime.UtcNow);
			
			if(ReleasedById != Guid.Empty) {
				entity.SetColumnValue("DgOPPersonalId", ReleasedById);	
			}
			
            entity.SetColumnValue("DgOPRemark", OPRemark);
            entity.SetColumnValue("DgOPRemark2", Remark);

			entity.Save(false);
			
			SendEmailMNPRejected(SubmissionId, ReleasedById, OPRemark).GetAwaiter().GetResult();
		}

        protected void UpdateOPDetail(Guid SubmissionId, Dictionary<string, Dictionary<string, string>> ErrorCode)
        {
            List<DgMasterData.Lookup> opDetailMaster = GetOPDetailMaster();
            List<OPDetail> opDetails = GetOPDetail(SubmissionId);
            
			var errorCodeOnly = ErrorCode
				.SelectMany(item => item.Value, (item, kvp) => kvp.Key)
				.Distinct()
				.ToList();
			
            var updateList = opDetails
                .Where(item => errorCodeOnly.Find(el => el == item.OPCode) != null)
                .ToList();
            foreach (var data in updateList) {
                new Update(UserConnection, "DgOrderProcessingDetail")
                    .Set("DgRemark", Column.Parameter("-"))
                    .Where("Id").IsEqual(Column.Parameter(data.Id))
                .Execute();
            }

            var insertList = errorCodeOnly
                .Except(opDetails.Select(item => item.OPCode))
                .ToList();

            foreach (string code in insertList) {
                var opMaster = opDetailMaster.FirstOrDefault(item => item.Code == code);
				if(opMaster == null) {
					continue;
				}
				
                new Insert(UserConnection)
                    .Into("DgOrderProcessingDetail")
                    .Set("DgSubmissionId", Column.Parameter(SubmissionId))
                    .Set("DgName", Column.Parameter(opMaster.Name))
                    .Set("DgOrderProcessingId", Column.Parameter(opMaster.Id))
                    .Set("DgRemark", Column.Parameter("-"))
                .Execute();
            }
        }

        protected List<OPDetail> GetOPDetail(Guid SubmissionId)
        {
            var result = new List<OPDetail>();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOrderProcessingDetail");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            
            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("OPId", esq.AddColumn("DgOrderProcessing.Id"));
            columns.Add("OPName", esq.AddColumn("DgOrderProcessing.Name"));
            columns.Add("OPCode", esq.AddColumn("DgOrderProcessing.DgCode"));
            columns.Add("Remark", esq.AddColumn("DgRemark"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubmission.Id", SubmissionId));
            
            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                var data = new OPDetail() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    OPId = entity.GetTypedColumnValue<Guid>(columns["OPId"].Name),
                    OPCode = entity.GetTypedColumnValue<string>(columns["OPCode"].Name),
                    OPName = entity.GetTypedColumnValue<string>(columns["OPName"].Name),
                    Remark = entity.GetTypedColumnValue<string>(columns["Remark"].Name)
                };

                result.Add(data);
            }

            return result;
        }

        protected List<DgMasterData.Lookup> GetOPDetailMaster()
        {
            var result = new List<DgMasterData.Lookup>();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOrderProcessing");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            
            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("Name", esq.AddColumn("Name"));
            columns.Add("Code", esq.AddColumn("DgCode"));
            
            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                var data = new DgMasterData.Lookup() {
                    Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name),
                    Name = entity.GetTypedColumnValue<string>(columns["Name"].Name),
                    Code = entity.GetTypedColumnValue<string>(columns["Code"].Name)
                };

                result.Add(data);
            }

            return result;
        }

        protected string GetOPDetailRemark(Guid SubmissionId)
        {
            List<OPDetail> opDetails = GetOPDetail(SubmissionId);
            
            return string.Join(
                "<br/>", 
                opDetails
                    .Select(item => $"{item.OPCode}: {item.OPName}: {item.Remark}")
                    .ToArray()
            );
        }
		
        protected virtual void AddHistory(LineDetail Line, string Remark)
        {
            new Insert(UserConnection)
                .Into("DgHistorySubmission")
                .Set("CreatedOn", Column.Parameter(DateTime.UtcNow))
                .Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                .Set("DgSubmissionId", Column.Parameter(Line.SubmissionId))
                .Set("DgOpsId", Column.Parameter(LookupConst.Ops.UPDATE))
                .Set("DgSectionId", Column.Parameter(LookupConst.Section.CRA_LINE))
                .Set("DgRemark", Column.Parameter(Remark.Trim()))
                .Set("DgMSISDN", Column.Parameter(Line.MSISDN))
                .Set("DgLINE_ID", Column.Parameter(Line.LineId.ToString()))
            .Execute();
        }

        protected virtual void PortIn(Guid RecordId)
        {
            string businessProcessName = "DgBPSendPortIn";
            var parameters = new Dictionary<string, object> {
				{"LineDetailId", RecordId}
			};
            RunBPBackground(businessProcessName, parameters);
        }

        protected virtual void ConfirmPortIn(Guid RecordId)
        {
            string businessProcessName = "DgBPSendConfirmPortIn";
            var parameters = new Dictionary<string, object> {
				{"LineDetailId", RecordId}
			};
            RunBPBackground(businessProcessName, parameters);
        }

        protected virtual void SendEmailError(Exception Exception, string ErrorMessage = "")
        {
            try {                
                string rawRequestEscape = System.Web.HttpUtility.HtmlEncode(this.rawRequest);
                string now = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss zz");
                string errorMessage = !string.IsNullOrEmpty(ErrorMessage) ? $"{ErrorMessage}: {Exception.Message}" : Exception.Message;
                string subject = "[ERROR] Line Status Update";
				if(this.isCSG) {
					subject += " - From PPA";
				}
				
                string message = "Dear Admin, We would like to inform you that the system has encountered an error. The details are as follows:<br><br>"
                        + $"<b>Error time:</b> {now}<br>"
                        + $"<b>Error description:</b> {errorMessage}<br>"
                        + $"<b>Stack trace:</b> <br><pre><code>{Exception.StackTrace}</code></pre><br>"
                        + $"<b>Raw Request:</b> <br><pre><code>{rawRequestEscape}</code></pre><br>"
                        + "<br>Please revert back to <b>Order Fulfillment Team</b> for further information"
                        + "<br><br>Thank you."
                        + "<br><br><b>This message is auto generated by NCCF Web.</b>";
                string email = SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_ErrorActivationCallback", string.Empty);
				if(string.IsNullOrEmpty(email)) {
					return;
				}
				
                var data = new MailParam() {
                    Subject = subject,
                    Message = message,
                    To = email
                };

                Mail.Send(UserConnection, "nccf2-crm@celcomdigi.com", data);
            } catch(Exception e) {}
        }

        protected virtual async Task SendEmailDealer(LineDetail Line, string Status, string Remark)
        {
            try {                
                var scms = new SCMSGetDealerInfoService(UserConnection);
                var smscResult = await scms.GetDealerInfo(Line.Dealer.Code);
                if(!smscResult.Success) {
                    throw new Exception(smscResult.Message);
                }

                string subject = $"[ERROR] Line Status Update - MSISDN {Line.MSISDN}";
				if(this.isCSG) {
					subject += " - From PPA";
				}
				
                string message = $"Dear {Line.Dealer.Name}, <br><br>"
                            + $"<b>Your application</b> {Line.CustomerName} Serial Number: {Line.SerialNumber}, MSISDN: {Line.MSISDN}"
                            + $" is <b>{Status}</b> with remarks: <br>{Remark}<br>"
                            + $"Please revert back to <b>Order Fulfillment Team</b> for further information."
                            + $"<br><br><b>This message is auto generated by NCCF Web.</b>";
                string email = smscResult.Message;
				if(string.IsNullOrEmpty(email)) {
					return;
				}
				
                var data = new MailParam() {
                    Subject = subject,
                    Message = message,
                    To = email
                };

                Mail.Send(UserConnection, "nccf2-crm@celcomdigi.com", data);
            } catch (Exception e) {
				SendEmailError(e, "SendEmailDealer Error");
			}
        }
		
		protected virtual async Task SendEmailMNPRejected(Guid SubmissionId, Guid ReleasedById, string Remark)
		{
			try {
				var submissionInfo = EntityHelper.GetEntity(
					UserConnection, 
					"DgSubmission", 
					SubmissionId, 
					new Dictionary<string, string>() {
						{"DgSalesperson.DgName", "string"},
						{"DgCustomerName", "string"},
						{"DgSerialNumber", "string"},
						{"DgCRMGroup.DgDealer.DgDealerID", "string"},
						{"DgCRMGroup.DgDealer.DgOperatorEmail", "string"}
					}
				);
				
				string salespersonName = submissionInfo["DgSalesperson.DgName"]?.ToString() ?? string.Empty;
				string customerName = submissionInfo["DgCustomerName"]?.ToString() ?? string.Empty;
				string serialNumber = submissionInfo["DgSerialNumber"]?.ToString() ?? string.Empty;
				string dealerCode = submissionInfo["DgCRMGroup.DgDealer.DgDealerID"]?.ToString() ?? string.Empty;
				string operatorEmail = submissionInfo["DgCRMGroup.DgDealer.DgOperatorEmail"]?.ToString() ?? string.Empty;
				
				string releasedByName = string.Empty;
				if(ReleasedById != Guid.Empty) {
					var releasedByInfo = EntityHelper.GetEntity(
						UserConnection, 
						"Contact", 
						ReleasedById, 
						new Dictionary<string, string>() {
							{"Name", "string"}
						}
					);
					releasedByName = releasedByInfo["Name"]?.ToString() ?? string.Empty;
				}
				
                var scms = new SCMSGetDealerInfoService(UserConnection);
                var smscResult = await scms.GetDealerInfo(dealerCode);
                if(!smscResult.Success) {
                    throw new Exception(smscResult.Message);
                }
				
				List<string> email = new List<string>();
				if(!string.IsNullOrEmpty(smscResult.Message)) {
					email.Add(smscResult.Message);
				}
				
				if(!string.IsNullOrEmpty(operatorEmail)) {
					email.Add(operatorEmail);
				}
				
				if(email.Count == 0) {
					return;
				}
				
                string subject = $"Notification Order Processing";
                string message = $"Dear {salespersonName}, <br><br>"
                            + $"<b>Your application</b> {customerName} Serial Number: {serialNumber}"
							+ "<br><br>MNP Rejected<br><br>"
							+ $"{Remark}<br><br>"
                            + $"Please revert back to <b>{releasedByName}</b> for further information."
                            + $"<br><br><br>Thank you.<br><b>This message is auto generated by NCCF Web.</b>";
				
                var data = new MailParam() {
                    Subject = subject,
                    Message = message,
                    To = string.Join("; ", email.ToArray())
                };

                Mail.Send(UserConnection, "nccf2-crm@celcomdigi.com", data);
            } catch (Exception e) {
				SendEmailError(e, "SendEmailMNPRejected Error");
			}
		}

        protected virtual void ACDCLog(Guid LineDetailId, string Response, bool IsSuccess, string Remarks)
        {
            string transacationType = string.Empty;
            if(!string.IsNullOrEmpty(this.request?.Order?.OrderType)) {
                if(new[] { "9", "10", "12", "13" }.Contains(this.request.Order.OrderType)) {
                    if(string.IsNullOrEmpty(this.transactionID)) {
                        this.transactionID = this.request.Order.OrderId;
                    }
                    
                    if(this.request.Order.OrderType == "9" || this.request.Order.OrderType == "10") {
                        transacationType = "COP";
                    } else if(this.request.Order.OrderType == "12") {
                        transacationType = "NEW";
                    } else if(this.request.Order.OrderType == "13") {
                        transacationType = "MNP";
                    }
                } else if(new[] { "30", "31", "32" }.Contains(this.request.Order.OrderType)) {
                    if(string.IsNullOrEmpty(this.transactionID)) {
                        this.transactionID = this.request?.Header?.ReferenceId;
                    }

                    transacationType = "MNP (CSG)";
                }
            }

            var msisdnList = this.request?.TaskList?
                .Where(item => !string.IsNullOrEmpty(item.CorrelationId))
                .Select(item => item.CorrelationId)
                .ToArray();
            string msisdn = msisdnList != null && msisdnList.Length > 0 ? string.Join(", ", msisdnList) : string.Empty;

            LogHelper.LogACDCTracking(
                UserConnection: UserConnection,
                RequestBody: this.rawRequest ?? string.Empty,
                ResponseBody: Response ?? string.Empty,
                OrderId: this.request?.Order?.OrderId ?? string.Empty,
                TransactionID: this.transactionID ?? string.Empty,
                TransactionType: transacationType,
                APIName: "Update Order Status",
                MSISDN: msisdn,
                Status: IsSuccess ? "SUCCESS" : "FAILED",
				LineDetailId: LineDetailId,
                ResultCode: IsSuccess ? this.response.Status.StatusCode : this.response.Status.ErrorCode,
                ResultMessage: Remarks,
                Remarks: Remarks,
                ContentType: "XML"
            );
        }

        protected virtual void RunBPBackground(string ProcessName, Dictionary<string, object> Parameters)
		{
			JobOptions jobOptions;
			if (!UserConnection.GetIsFeatureEnabled("UseDefaultImportJobOptions")) {
				jobOptions = new JobOptions {
					RequestsRecovery = false
				};
			} else {
				jobOptions = JobOptions.Default;
			}

            UserConnection.RunProcess(
                ProcessName, 
                MisfireInstruction.SimpleTrigger.FireNow, 
                Parameters,
				jobOptions
            );
		}
    }

    public class OPDetail
    {
        public Guid Id { get; set; }
        public Guid OPId { get; set; }
        public string OPCode { get; set; }
        public string OPName { get; set; }
        public string Remark { get; set; }
    }
}