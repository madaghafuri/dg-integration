using System;
using System.IO;
using System.Text;
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
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using ISAEntityHelper.EntityHelper;
using ISAHttpRequest.ISAHttpRequest;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using RequestModel = DgIntegration.DgUpdateCreditCardDetails.Request;
using ResponseModel = DgIntegration.DgUpdateCreditCardDetails.Response;

namespace DgIntegration.DgUpdateCreditCardDetails
{
    public class UpdateCreditCardDetailsService
    {
		private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public UpdateCreditCardDetailsService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
        }

        public ResponseModel.Envelope Process(Stream Envelope) 
        {
			var result = new ResponseModel.Envelope();
			result.Body = new ResponseModel.Body();
			result.Body.updateCreditCardDetailsResponse = new ResponseModel.UpdateCreditCardDetailsResponse();
			result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult = new ResponseModel.UpdateCreditCardDetailsResult();
			
			string rawRequest = string.Empty;
            
            try {  
                var reader = new StreamReader(Envelope);
				rawRequest = reader.ReadToEnd();

                var request = HTTPRequest.XmlToObject<RequestModel.Envelope>(rawRequest);
				var data = request.Body.updateCreditCardDetails.nccfUpdateRequest;
				
				var validation = Validation(data);
                if (!validation.Success) 
                {
					result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Code = -1;
					throw new Exception(validation.Message);
                }

                var CustomerTypeId = EntityHelper.GetEntityId(UserConnection, "DgCustomerType", new Dictionary<string, object>() {
                    {"Name", data.CustomerType.Trim()}
                });

                var IDTypeId = EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {"Name", data.CustomerIdType.Trim()}
                });
				
				var cardTypeId = EntityHelper.GetEntityId(UserConnection, "DgCardType", new Dictionary<string, object>() {
                    {"DgCode", data.CardType.Trim()}
                });
				
				var bankId = EntityHelper.GetEntityId(UserConnection, "DgBankIssuer", new Dictionary<string, object>() {
                    {"DgCode", data.BankIssuer.Trim()}
                });
				
				string customerContact = data.CustomerContact?.Trim() ?? string.Empty;
				string customerEmail = data.CustomerEmail?.Trim() ?? string.Empty;
				
				DateTime createdOn = !string.IsNullOrEmpty(data.CreatedDate) ? DateTime.Parse(data.CreatedDate).AddHours(-8) : DateTime.UtcNow;
                EntityHelper.CreateEntity(
                    UserConnection, 
                    section: "DgCreditCardToken", 
                    values: new Dictionary<string, object>() {
						{"DgRefID", data.ReferenceId ?? string.Empty},
                        {"DgCustomerTypeId", CustomerTypeId != Guid.Empty ? CustomerTypeId : Guid.Empty},
                        {"DgIDTypeId", IDTypeId != Guid.Empty ? IDTypeId : Guid.Empty},
                        {"DgIDNo", data.CustomerId},
                        {"DgCustomerName", data.CustomerName},
                        {"DgTransactionType", data.TransactionType},
                        {"DgCardTypeId", cardTypeId},
                        {"DgCardHolderName", data.CardOwner},
                        {"DgCardNumber", data.CardNo},
                        {"DgTokenID", data.TokenId},
                        {"DgExpDate", DateTime.Parse(data.CardExpDate)},
                        {"DgIssuingBankId", bankId},
                        {"DgOwnershipType", data.OwnershipType},
                        {"DgContactNumber", customerContact},
                        {"DgEmail", customerEmail},
						{"CreatedOn", createdOn}
                    }
                );
				
				result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Code = 1;
				result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Message = "Success";
            } catch(Exception error) {
                result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Message = error.Message;
            } finally {
				LogHelper.LogACDCTracking(
					UserConnection: UserConnection,
					RequestBody: HTTPRequest.XmlToString<RequestModel.Envelope>(HTTPRequest.XmlToObject<RequestModel.Envelope>(rawRequest)),
					ResponseBody: HTTPRequest.XmlToString<ResponseModel.Envelope>(result),
					OrderId: string.Empty,
					TransactionID: string.Empty,
					TransactionType: string.Empty,
					APIName: "UpdateCreditCardDetails",
					MSISDN: string.Empty,
					Status: result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Message == "Success" ? "SUCCESS" : "FAIL",
					LineDetailId: Guid.Empty,
					ResultCode: string.Empty,
					ResultMessage: string.Empty,
					Remarks: result.Body.updateCreditCardDetailsResponse.updateCreditCardDetailsResult.Message,
					ContentType: "XML"
				);
			}

			return result;
        }

        protected virtual GeneralResponse Validation(RequestModel.nccfUpdateRequest Data)
        {
            var result = new GeneralResponse();

            try {
                var CustomerTypeId = EntityHelper.GetEntityId(UserConnection, "DgCustomerType", new Dictionary<string, object>() {
                    {"Name", Data.CustomerType.Trim()}
                });

                var IDTypeId = EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {"Name", Data.CustomerIdType.Trim()}
                });
				
                if (string.IsNullOrEmpty(Data.CustomerType?.Trim()) || CustomerTypeId == Guid.Empty)
                {
                    throw new Exception("CustomerType cannot be empty or not found.");
                }

                if (string.IsNullOrEmpty(Data.CustomerIdType?.Trim()) || IDTypeId == Guid.Empty)
                {
                    throw new Exception("CustomerIdType cannot be empty or not found.");
                }

                if (string.IsNullOrEmpty(Data.CustomerId?.Trim()))
                {
                    throw new Exception("CustomerId cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.CustomerName?.Trim()))
                {
                    throw new Exception("CustomerName cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.TransactionType?.Trim()))
                {
                    throw new Exception("TransactionType cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.CardType?.Trim()))
                {
                    throw new Exception("CardType cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.CardOwner?.Trim()))
                {
                    throw new Exception("CardOwner cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.CardNo?.Trim()))
                {
                    throw new Exception("CardNo cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.TokenId?.Trim()))
                {
                    throw new Exception("TokenId cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.CardExpDate?.Trim()))
                {
                    throw new Exception("CardExpDate cannot be empty or null.");       
                }

                if (string.IsNullOrEmpty(Data.BankIssuer?.Trim()))
                {
                    throw new Exception("BankIssuer cannot be empty or null.");
                }

                if (string.IsNullOrEmpty(Data.OwnershipType?.Trim()))
                {
                    throw new Exception("OwnershipType cannot be empty or null.");
                }
				
				/*
                if (string.IsNullOrEmpty(Data.CreatedDate?.Trim()) || Data.CreatedDate == null)
                {
                    throw new Exception("CreatedDate cannot be empty or null.");
                }
				*/

                result.Message = "Validation Success!";
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }

            return result;
        }
	}
}