using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Web.Services3.Security.Tokens;
using Terrasoft.Core;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using DgCRMIntegration;

namespace DgBaseService.DgHelpers
{
    public static class LogHelper
    {
        public static void LogACDCTracking(
			UserConnection UserConnection,
            string RequestBody, 
            string ResponseBody, 
            string OrderId, 
            string TransactionID,
            string TransactionType,
            string APIName,
            string MSISDN,
            string Status,
			Guid LineDetailId = default(Guid),
			string ResultCode = "",
			string ResultMessage = "",
			string Remarks = "",
            string ContentType = "JSON"
        ) {
            DateTime currentDate = DateTime.UtcNow;
            var MYTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var MYTime = TimeZoneInfo.ConvertTimeFromUtc(currentDate, MYTimeZone);
            var currentDateTimeMY = MYTime.ToString("yyyy-MM-ddThh:mm:ssZ");

            var fileName = $"{currentDateTimeMY}_{APIName}_{TransactionType}_{TransactionID}";
            var log = $"{currentDateTimeMY}{System.Environment.NewLine}"
					+ $"{ContentType} Request: {System.Environment.NewLine}"
					+ $"{RequestBody}{System.Environment.NewLine}{System.Environment.NewLine}"
					+ $"{ContentType} Response: {System.Environment.NewLine}"
					+ $"{ResponseBody}";

            EntityHelper.CreateEntity(
                UserConnection, 
                section: "DgACDCOrderTracking", 
                values: new Dictionary<string, object>() {
                    {"DgOrderID", OrderId},
                    {"DgTransactionID", TransactionID},
                    {"DgTransactionType", TransactionType},
                    {"DgAPI", APIName},
                    {"DgMSISDN", MSISDN},
                    {"DgFileName", fileName},
                    {"DgLogFile", log},
                    {"DgStatus", Status},
					{"DgResultCode", ResultCode},
					{"DgResultMessage", ResultMessage},
					{"DgRemarks", Remarks},
                    {"DgDateTimeReleased", MYTime.ToString("dd/MM/yyyy h:mm tt")},
					{"DgLineDetailId", LineDetailId},
                    {"DgCreatedBy", "SYSTEM"}
                }
            );
        }
		
		public static void LogACDCTracking(
			UserConnection UserConnection,
			ISAHttpRequest.ISAIntegrationLogService.IntegrationLog Log,
			ResultOfOperationValue ResultOperationReply,
			string TransactionType,
			string MSISDN,
			string Remarks,
			Guid LineDetailId = default(Guid)
        ) {
			string resultMessage = ResultOperationReply?.resultMessage ?? string.Empty;
			string status = resultMessage.ToLower() == "success" || resultMessage.ToLower() == "successfull" ? "SUCCESS" : "FAIL";
            LogHelper.LogACDCTracking(
				UserConnection: UserConnection,
				RequestBody: Log?.Request?.Body ?? string.Empty,
				ResponseBody: Log?.Response?.Body ?? string.Empty,
				OrderId: ResultOperationReply?.orderId ?? string.Empty,
				TransactionID: ResultOperationReply?.transactionId ?? string.Empty,
				TransactionType: TransactionType,
				APIName: Log?.Name ?? string.Empty,
				MSISDN: MSISDN,
				Status: status,
				LineDetailId: LineDetailId,
				ResultCode: ResultOperationReply?.resultCode ?? string.Empty,
				ResultMessage: resultMessage,
				Remarks: Remarks,
				ContentType: Log?.Type ?? string.Empty
			);
        }
    }
}