using System;
using System.Globalization;
using System.Linq;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Text.RegularExpressions;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using Lookup = DgMasterData.DgLookupConst;
using ISADocumentNumberGeneratorService;
using ISAEntityHelper.EntityHelper;
using DgSubmission.DgHistorySubmissionService;
using DgCRMIntegration;
using SolarisCore;

namespace DgSubmission.DgSubmissionService
{
    public class SubmissionService
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection
        {
            get
            {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

        protected DocumentNumberGeneratorService documentNumberGenerator;
        protected CRMService crmService;
        protected CRMGroup crmGroup;
        protected Submission submission;
        protected List<LineDetail> lineDetail;

        public SubmissionService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
            this.documentNumberGenerator = new DocumentNumberGeneratorService(UserConnection);
            this.crmService = new CRMService(UserConnection);
        }

        public SubmitResponse Submission(SubmitRequest Request)
        {
            var result = new SubmitResponse();

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())
            {
                dbExecutor.StartTransaction();

                try
                {
                    this.submission = Request?.Submission;
                    this.submission.SourceId = Lookup.Source.SFA;
                    this.crmGroup = Request?.CRMGroup;
                    this.lineDetail = Request?.LineDetails;

                    Validation();

                    this.crmGroup.BillMedium = GetParentBillMedium();
                    this.crmGroup.BillType = GetParentBillType();
                    this.crmGroup.BillCarrier = GetParentBillCarrier();
                    this.crmGroup.BillDetail = GetParentBillDetail();

                    if (!string.IsNullOrEmpty(this.crmGroup.GroupNo) || !string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo))
                    {
                        SetParentCRMGroup(dbExecutor);
                        SetSubParentCRMGroup(dbExecutor);
                        SetSubmissionIntegration();
                    }

                    CRMGroupSave();
                    SubmissionSave(dbExecutor);
                    LineDetailSave(dbExecutor);

                    HistorySubmissionService.SubmitFromSFA(
                        UserConnection: UserConnection,
                        SubmissionId: this.submission.Id,
                        CreatedById: UserConnection.CurrentUser.ContactId
                    );

                    result.Success = true;
                    result.SerialNumber = this.submission.SerialNumber;

                    dbExecutor.CommitTransaction();
                }
                catch (Exception e)
                {
                    // RollbackSubmission();
                    dbExecutor.RollbackTransaction();

                    string error = e.InnerException != null ? $"{e.Message}: {e.InnerException.Message}" : e.Message;
                    result.Message = error;
                }
            }

            return result;
        }

        public SubmitResponse ReSubmission(SubmitRequest Request)
        {
            var result = new SubmitResponse();
            try
            {
                this.submission = Request?.Submission;
                this.submission.SourceId = Lookup.Source.SFA;
                this.crmGroup = Request?.CRMGroup;
                this.lineDetail = Request?.LineDetails;

                Validation();
                if (this.submission.Id == null || this.submission.Id == Guid.Empty)
                {
                    if (string.IsNullOrEmpty(this.submission.SerialNumber))
                    {
                        throw new Exception("Serial Number is empty");
                    }
                }

                // if(!string.IsNullOrEmpty(this.crmGroup.GroupNo) || !string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo)) {
                //     SetParentCRMGroup();
                //     SetSubParentCRMGroup();
                //     SetSubmissionIntegration();
                // }

                if (this.submission.Id == null || this.submission.Id == Guid.Empty)
                {
                    this.submission.Id = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgSubmission", new Dictionary<string, object>() {
                        {"DgSerialNumber", this.submission.SerialNumber}
                    });
                }

                if (this.submission.Id == Guid.Empty)
                {
                    throw new Exception($"Serial Number {this.submission.SerialNumber} in NCCF 2.0 not found");
                }

                this.submission.ResubmissionDate = DateTime.UtcNow;
                this.submission.SubmissionStatusId = GetResubmissionStatusId(this.submission.ResubmissionNumber);

                CRMGroupSave(true);
                SubmissionSave(null, true);
                LineDetailSave(null, true);

                new Update(UserConnection, "DgSubmission")
                    .Set("DgIsResubmit", Terrasoft.Core.DB.Column.Parameter(true))
                    .Where("Id").IsEqual(Terrasoft.Core.DB.Column.Parameter(this.submission.Id))
                    .Execute();

                HistorySubmissionService.ResubmissionFromSFA(
                    UserConnection: UserConnection,
                    SubmissionId: this.submission.Id,
                    CreatedById: UserConnection.CurrentUser.ContactId
                );

                result.Success = true;
                result.SerialNumber = this.submission.SerialNumber;

            }
            catch (Exception e)
            {
                string error = e.InnerException != null ? $"{e.Message}: {e.InnerException.Message}" : e.Message;
                result.Message = error;
            }

            return result;
        }

        public SubmitResponse Pullback(string SerialNumber)
        {
            var result = new SubmitResponse();
            try
            {
                Guid submissionId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgSubmission", new Dictionary<string, object>() {
                    {"DgSerialNumber", SerialNumber}
                });
                if (submissionId == Guid.Empty)
                {
                    throw new Exception($"Serial Number {SerialNumber} not found");
                }

                var submissionInfo = ISAEntityHelper.EntityHelper.EntityHelper.GetEntity(UserConnection, "DgSubmission", submissionId, new Dictionary<string, string>() {
                    {"DgApprovalId", "guid"},
                    {"DgProgressStatusId", "guid"},
                    {"DgStatusId", "guid"},
                    {"DgSubmissionStatusId", "guid"},
                    {"DgActivationStatusId", "guid"}
                });

                if ((Guid)submissionInfo["DgSubmissionStatusId"] == Lookup.SubmissionStatus.Pullback)
                {
                    throw new Exception("Submission already in pullback status");
                }

                if ((Guid)submissionInfo["DgApprovalId"] == Lookup.CRStatus.Approved)
                {
                    throw new Exception("Credit risk already approved");
                }

                if ((Guid)submissionInfo["DgProgressStatusId"] == Lookup.OPStatus.Completed)
                {
                    throw new Exception("Order Processing already completed");
                }

                if ((Guid)submissionInfo["DgActivationStatusId"] == Lookup.ActivationStatus.Activated)
                {
                    throw new Exception("Submission already activated");
                }

                if ((Guid)submissionInfo["DgStatusId"] == Lookup.Status.Closed)
                {
                    throw new Exception("Submission already closed");
                }

                new Update(UserConnection, "DgSubmission")
                    .Set("DgStatusId", Terrasoft.Core.DB.Column.Parameter(Lookup.Status.Closed))
                    .Set("DgActivationStatusId", Terrasoft.Core.DB.Column.Parameter(Lookup.ActivationStatus.NotActivated))
                    .Set("DgSubmissionStatusId", Terrasoft.Core.DB.Column.Parameter(Lookup.SubmissionStatus.Pullback))
                    .Set("DgPullbackDate", Terrasoft.Core.DB.Column.Parameter(DateTime.UtcNow))
                    .Where("Id").IsEqual(Terrasoft.Core.DB.Column.Parameter(submissionId))
                    .Execute();

                new Update(UserConnection, "DgLineDetail")
                    .Set("DgActivationStatusId", Terrasoft.Core.DB.Column.Parameter(Lookup.ActivationStatus.Cancelled))
                    .Where("DgSubmissionId").IsEqual(Terrasoft.Core.DB.Column.Parameter(submissionId))
                    .Execute();

                HistorySubmissionService.PullbackFromSFA(
                    UserConnection: UserConnection,
                    SubmissionId: submissionId,
                    CreatedById: UserConnection.CurrentUser.ContactId
                );

                result.Success = true;
                result.SerialNumber = SerialNumber;

            }
            catch (Exception e)
            {
                string error = e.InnerException != null ? $"{e.Message}: {e.InnerException.Message}" : e.Message;
                result.Message = error;
            }

            return result;
        }

        protected virtual void Validation()
        {
            if (this.crmGroup == null)
            {
                throw new Exception("CRM Group is empty");
            }

            if (this.submission == null)
            {
                throw new Exception("Submission is empty");
            }

            if (string.IsNullOrEmpty(this.crmGroup.BRN))
            {
                throw new Exception("BRN is empty");
            }

            if (string.IsNullOrEmpty(this.submission.CompanyName))
            {
                throw new Exception("Company Name is empty");
            }

            if (string.IsNullOrEmpty(this.submission.SubmissionType))
            {
                throw new Exception("Submission Type is empty");
            }

            if (string.IsNullOrEmpty(this.submission.IdNo) || string.IsNullOrEmpty(this.submission.IdType))
            {
                throw new Exception("Id No or Id Type is empty");
            }

            if (string.IsNullOrEmpty(this.crmGroup.SalesChannel))
            {
                throw new Exception("Sales Channel is empty");
            }

            if (string.IsNullOrEmpty(this.crmGroup.DealerCode))
            {
                throw new Exception("Dealer Code is empty");
            }

            if (string.IsNullOrEmpty(this.crmGroup.DealerName))
            {
                throw new Exception("Dealer Name is empty");
            }

            if (this.lineDetail == null || (this.lineDetail != null && this.lineDetail.Count == 0))
            {
                throw new Exception("Line detail is empty");
            }

            int anyMSISDNEmpty = this.lineDetail
                .Where(item => string.IsNullOrEmpty(item.MSISDN))
                .ToList()
                .Count;
            if (anyMSISDNEmpty > 0)
            {
                throw new Exception($"There are {anyMSISDNEmpty} lines which have empty MSISDN from {this.lineDetail.Count} lines");
            }
        }

        protected virtual Guid GetResubmissionStatusId(int ResubmissionNumber)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubmissionStatus");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("SubmissionStatus", esq.AddColumn("Id"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.EndWith, "Name", $"submitted #{ResubmissionNumber}"));

            var entity = esq.GetEntityCollection(UserConnection).FirstOrDefault();
            return entity == null ?
                Guid.Parse("3F34120F-0AD3-475C-A96C-D24BEA95818B") :
                entity.GetTypedColumnValue<Guid>(columns["SubmissionStatus"].Name);
        }

        protected void CardNumberValidation(string CardNumber)
        {
            if (string.IsNullOrEmpty(CardNumber))
            {
                throw new Exception($"Card Number is empty");
            }

            if (CardNumber.Length != 4)
            {
                throw new Exception($"Card Number is invalid");
            }

            for (int i = 0; i < CardNumber.Length; i++)
            {
                var digit = CardNumber[i];
                if (!int.TryParse(CardNumber[i].ToString(), out _))
                {
                    throw new Exception($"Card Number is invalid: {digit}");
                }
            }
        }

        protected virtual void CRMGroupSave()
        {
            if (this.crmGroup.DealerId == Guid.Empty)
            {
                this.crmGroup.DealerId = GetDealerId();
            }

            if (!string.IsNullOrEmpty(this.crmGroup.EnterpriseCustomerType))
            {
                switch (this.crmGroup.EnterpriseCustomerType.ToUpper())
                {
                    case "MICRO ACCOUNT":
                    case "SMALL ACCOUNT":
                    case "MEDIUM ACCOUNT":
                    case "LARGE ACCOUNT":
                        this.crmGroup.EnterpriseCustomerType += "S";
                        break;
                    default:
                        this.crmGroup.EnterpriseCustomerType = "MICRO ACCOUNTS";
                        break;
                }
            }
            else
            {
                this.crmGroup.EnterpriseCustomerType = "MICRO ACCOUNTS";
            }

            if (this.crmGroup.PaymentModeId == Guid.Empty)
            {
                this.crmGroup.PaymentModeId = GetPaymentModeId(this.crmGroup.PaymentMode);
            }

            if (this.crmGroup.DNOId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DNO))
            {
                this.crmGroup.DNOId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgDNO", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.DNO}
                });
            }

            if (this.crmGroup.DNOIdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DNOIdType))
            {
                this.crmGroup.DNOIdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.DNOIdType}
                });
            }

            if (this.crmGroup.LegalAddress != null)
            {
                if (this.crmGroup.LegalAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.City))
                {
                    this.crmGroup.LegalAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.City.Length == 4 && this.crmGroup.LegalAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.City}
                    });
                }

                if (this.crmGroup.LegalAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.State))
                {
                    this.crmGroup.LegalAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.LegalAddress.State}
                    });
                }

                if (this.crmGroup.LegalAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.Country))
                {
                    this.crmGroup.LegalAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.Country.Length == 4 && int.TryParse(this.crmGroup.LegalAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.Country}
                    });
                }

                if (this.crmGroup.LegalAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.PostCode))
                {
                    this.crmGroup.LegalAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.LegalAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.BillAddress != null)
            {
                if (this.crmGroup.BillAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.City))
                {
                    this.crmGroup.BillAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.City.Length == 4 && this.crmGroup.BillAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.City}
                    });
                }

                if (this.crmGroup.BillAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.State))
                {
                    this.crmGroup.BillAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.BillAddress.State}
                    });
                }

                if (this.crmGroup.BillAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.Country))
                {
                    this.crmGroup.BillAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.Country.Length == 4 && int.TryParse(this.crmGroup.BillAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.Country}
                    });
                }

                if (this.crmGroup.BillAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.PostCode))
                {
                    this.crmGroup.BillAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.BillAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.DeliveryAddress != null)
            {
                if (this.crmGroup.DeliveryAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.City))
                {
                    this.crmGroup.DeliveryAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.City.Length == 4 && this.crmGroup.DeliveryAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.City}
                    });
                }

                if (this.crmGroup.DeliveryAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.State))
                {
                    this.crmGroup.DeliveryAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.DeliveryAddress.State}
                    });
                }

                if (this.crmGroup.DeliveryAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.Country))
                {
                    this.crmGroup.DeliveryAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.Country.Length == 4 && int.TryParse(this.crmGroup.DeliveryAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.Country}
                    });
                }

                if (this.crmGroup.DeliveryAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.PostCode))
                {
                    this.crmGroup.DeliveryAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.DeliveryAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.Admin1 != null)
            {
                if (this.crmGroup.Admin1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin1.IdType))
                {
                    this.crmGroup.Admin1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin1.IdType}
                    });
                }

                if (this.crmGroup.Admin1.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Admin1.IdNo))
                    {
                        this.crmGroup.Admin1.IdNo = this.crmGroup.Admin1.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Admin2 != null)
            {
                if (this.crmGroup.Admin2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin2.IdType))
                {
                    this.crmGroup.Admin2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin2.IdType}
                    });
                }

                if (this.crmGroup.Admin2.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Admin2.IdNo))
                    {
                        this.crmGroup.Admin2.IdNo = this.crmGroup.Admin2.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Auth1 != null)
            {
                if (this.crmGroup.Auth1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth1.IdType))
                {
                    this.crmGroup.Auth1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth1.IdType}
                    });
                }

                if (this.crmGroup.Auth1.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Auth1.IdNo))
                    {
                        this.crmGroup.Auth1.IdNo = this.crmGroup.Auth1.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Auth2 != null)
            {
                if (this.crmGroup.Auth2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth2.IdType))
                {
                    this.crmGroup.Auth2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth2.IdType}
                    });
                }

                if (this.crmGroup.Auth2.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Auth2.IdNo))
                    {
                        this.crmGroup.Auth2.IdNo = this.crmGroup.Auth2.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.IndustrialSegmentId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.IndustrialSegment))
            {
                this.crmGroup.IndustrialSegmentId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIndustrialSegment", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.IndustrialSegment}
                });
            }

            if (this.crmGroup.EnterpriseCustomerTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.EnterpriseCustomerType))
            {
                this.crmGroup.EnterpriseCustomerTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgEnterpriseCustomerType", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.EnterpriseCustomerType}
                });
            }

            if (this.crmGroup.BillMediumId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillMedium))
            {
                this.crmGroup.BillMediumId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgBillMediumName", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.BillMedium}
                });
            }

            if (this.crmGroup.PaymentMethodId == Guid.Empty)
            {
                if (this.crmGroup.PaymentModeId == Lookup.PaymentMode.Cash)
                {
                    this.crmGroup.PaymentMethodId = Lookup.PaymentMethod.Cash;
                }
                else if (this.crmGroup.PaymentModeId == Lookup.PaymentMode.CreditCard)
                {
                    this.crmGroup.PaymentMethodId = Lookup.PaymentMethod.CreditCard;
                }
            }

            var crmGroupValues = new Dictionary<string, object>();
            crmGroupValues.Add("DgName", this.crmGroup.GroupName);
            crmGroupValues.Add("DgBRN", this.crmGroup.BRN);

            if (this.crmGroup.BRNExpiryDate != null && this.crmGroup.BRNExpiryDate != DateTime.MinValue)
            {
                crmGroupValues.Add("DgBRNExpiredDate", this.crmGroup.BRNExpiryDate);
            }
            crmGroupValues.Add("DgLegalAddress", this.crmGroup.LegalAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityId", this.crmGroup.LegalAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeId", this.crmGroup.LegalAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateId", this.crmGroup.LegalAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryId", this.crmGroup.LegalAddress?.CountryId ?? Guid.Empty);
            crmGroupValues.Add("DgTelNo", this.crmGroup.TelNo);
            crmGroupValues.Add("DgFaxNo", this.crmGroup.TelNo);
            crmGroupValues.Add("DgBillingEmailAddress", this.crmGroup.BillingEmailAddress);
            crmGroupValues.Add("DgCompanyEmail", this.crmGroup.BillingEmailAddress);

            if (this.crmGroup.DateIncorparation != null && this.crmGroup.DateIncorparation != DateTime.MinValue)
            {
                crmGroupValues.Add("DgDateIncorporation", this.crmGroup.DateIncorparation);
            }

            if (!string.IsNullOrEmpty(this.crmGroup.PaymentMode) && this.crmGroup.PaymentMode.Length > 1)
            {
                this.crmGroup.PaymentMode = GetPaymentMode(this.crmGroup.PaymentMode);
            }

            crmGroupValues.Add("DgPaidUpCapital", this.crmGroup.PaidUpCapital ?? string.Empty);
            crmGroupValues.Add("DgSalesTurnover", this.crmGroup.SalesTurnover ?? string.Empty);
            crmGroupValues.Add("DgNoEmployees", this.crmGroup.NoOfEmployees ?? string.Empty);
            crmGroupValues.Add("DgIndustrialSegmentId", this.crmGroup.IndustrialSegmentId != Guid.Empty ? this.crmGroup.IndustrialSegmentId : Guid.Parse("CC78B0C2-31B1-473B-A614-353E9803482D"));
            crmGroupValues.Add("DgNatureBusiness", this.crmGroup.NatureOfBusiness ?? string.Empty);
            crmGroupValues.Add("DgTelcoProvider", this.crmGroup.TelcoProviders ?? string.Empty);
            crmGroupValues.Add("DgSOW", this.crmGroup.SOW ?? string.Empty);
            crmGroupValues.Add("DgEnterpriseCustomerTypeId", this.crmGroup.EnterpriseCustomerTypeId);
            crmGroupValues.Add("DgGeographicalSpreadId", this.crmGroup.GeographicalSpreadId);
            crmGroupValues.Add("DgLanguage", this.crmGroup.Language ?? "English");
            crmGroupValues.Add("DgBillingAddress", this.crmGroup.BillAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityAdmInformationBillingId", this.crmGroup.BillAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeAdmInformationBillingId", this.crmGroup.BillAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateAdmInfoBillingId", this.crmGroup.BillAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryAdmInformationBillingId", this.crmGroup.BillAddress?.CountryId ?? Guid.Empty);
            crmGroupValues.Add("DgDeliveryaddress", this.crmGroup.DeliveryAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateAdmInfoDeliveryId", this.crmGroup.DeliveryAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.CountryId ?? Guid.Empty);
            crmGroupValues.Add("DgAdministrationName1", this.crmGroup.Admin1?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAdm1Id", this.crmGroup.Admin1?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgIdNo1", this.crmGroup.Admin1?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgMobilePhone1", this.crmGroup.Admin1?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAdm1", this.crmGroup.Admin1?.Designation ?? string.Empty);
            crmGroupValues.Add("DgOfficeTelNo1", this.crmGroup.Admin1?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAdministrationEmail1", this.crmGroup.Admin1?.Email ?? string.Empty);
            crmGroupValues.Add("DgAdministrationName2", this.crmGroup.Admin2?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAdm2Id", this.crmGroup.Admin2?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgIdNo2", this.crmGroup.Admin2?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgMobilePhone2", this.crmGroup.Admin2?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAdm2", this.crmGroup.Admin2?.Designation ?? string.Empty);
            crmGroupValues.Add("DgOfficeTelNo2", this.crmGroup.Admin2?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAdministrationEmail2", this.crmGroup.Admin2?.Email ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedName1", this.crmGroup.Auth1?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAuth1Id", this.crmGroup.Auth1?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgAuthorizedIdNo1", this.crmGroup.Auth1?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedMobilePhone1", this.crmGroup.Auth1?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAuth1", this.crmGroup.Auth1?.Designation ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedOfficeTelNo1", this.crmGroup.Auth1?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedEmail1", this.crmGroup.Auth1?.Email ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedName2", this.crmGroup.Auth2?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAuth2Id", this.crmGroup.Auth2?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgAuthorizedIdNo2", this.crmGroup.Auth2?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedMobilePhone2", this.crmGroup.Auth2?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAuth2", this.crmGroup.Auth2?.Designation ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedOfficeTelNo2", this.crmGroup.Auth2?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedEmail2", this.crmGroup.Auth2?.Email ?? string.Empty);
            crmGroupValues.Add("DgSalespersonID", this.crmGroup.DealerCode ?? string.Empty);
            crmGroupValues.Add("DgDealerId", this.crmGroup.DealerId);
            crmGroupValues.Add("DgEnterpriseGroupType", this.crmGroup.EnterpriseGroupType ?? string.Empty);
            crmGroupValues.Add("DgACPaymentResponsible", this.crmGroup.ACPaymentResponsible ?? string.Empty);
            crmGroupValues.Add("DgGroupLevel", this.crmGroup.GroupLevel ?? string.Empty);
            crmGroupValues.Add("DgGroupTariff", this.crmGroup.GroupTariff ?? string.Empty);
            crmGroupValues.Add("DgBillingCycle", string.IsNullOrEmpty(this.crmGroup.BillingCycle) ? "01" : this.crmGroup.BillingCycle);
            crmGroupValues.Add("DgPrimaryOfferId", this.crmGroup.PrimaryOfferId != Guid.Empty ? this.crmGroup.PrimaryOfferId : Guid.Parse("e2ea4aae-e3a5-41c8-aa39-d2e2672e57fb"));
            crmGroupValues.Add("DgSuppOffer1Id", this.crmGroup.SuppOffer1Id != Guid.Empty ? this.crmGroup.SuppOffer1Id : Guid.Parse("0242fc0e-d5ca-4e79-9246-b497b1f3817c"));
            crmGroupValues.Add("DgSuppOffer2Id", this.crmGroup.SuppOffer2Id != Guid.Empty ? this.crmGroup.SuppOffer2Id : Guid.Parse("506619aa-630d-4519-b0be-6a57ef0930a1"));
            crmGroupValues.Add("DgBillMediumNameId", this.crmGroup.BillMediumId);
            crmGroupValues.Add("DgBillType", this.crmGroup.BillType ?? string.Empty);
            crmGroupValues.Add("DgBillCarrier", this.crmGroup.BillCarrier ?? string.Empty);
            crmGroupValues.Add("DgPaymentMode", this.crmGroup.PaymentMode ?? string.Empty);
            crmGroupValues.Add("DgPaymentModeLookupId", this.crmGroup.PaymentModeId);
            crmGroupValues.Add("DgPaymentMethodId", this.crmGroup.PaymentMethodId);
            crmGroupValues.Add("DgBillDetail", this.crmGroup.BillDetail ?? string.Empty);
            crmGroupValues.Add("DgGroupName", this.crmGroup.GroupName ?? string.Empty);
            crmGroupValues.Add("DgGroupNo", this.crmGroup.GroupNo ?? string.Empty);
            crmGroupValues.Add("DgGroupSubParentName", this.crmGroup.SubParentGroupName ?? string.Empty);
            crmGroupValues.Add("DgGroupSubParentNo", this.crmGroup.SubParentGroupNo ?? string.Empty);
            crmGroupValues.Add("DgParentCustomerId", this.crmGroup.CustomerId ?? string.Empty);
            crmGroupValues.Add("DgCustomerCode", this.crmGroup.CustomerCode ?? string.Empty);
            crmGroupValues.Add("DgCorporateNumber", this.crmGroup.CorpNumber ?? string.Empty);
            crmGroupValues.Add("DgAccountId", this.crmGroup.AccountId ?? string.Empty);
            crmGroupValues.Add("DgAccountCode", this.crmGroup.AccountCode ?? string.Empty);
            crmGroupValues.Add("DgPaymentId", this.crmGroup.PaymentId ?? string.Empty);
            crmGroupValues.Add("DgSubscriberId", this.crmGroup.SubscriberId ?? string.Empty);
            crmGroupValues.Add("DgGroupID", this.crmGroup.GroupId ?? string.Empty);
            crmGroupValues.Add("DgSubParentCustomerId", this.crmGroup.SubParentCustomerId ?? string.Empty);
            crmGroupValues.Add("DgSubParentCustomerCode", this.crmGroup.SubParentCustomerCode ?? string.Empty);
            crmGroupValues.Add("DgSubParentCorporateNumber", this.crmGroup.SubParentCorpNumber ?? string.Empty);
            crmGroupValues.Add("DgSubParentAccountId", this.crmGroup.SubParentAccountId ?? string.Empty);
            crmGroupValues.Add("DgSubParentAccountCode", this.crmGroup.SubParentAccountCode ?? string.Empty);
            crmGroupValues.Add("DgSubParentGroupID", this.crmGroup.SubParentGroupId ?? string.Empty);
            crmGroupValues.Add("DgTINNumber", this.crmGroup.TINNumber ?? string.Empty);
            crmGroupValues.Add("DgSST", this.crmGroup.SSTNumber ?? string.Empty);

            this.crmGroup.Id = ISAEntityHelper.EntityHelper.EntityHelper.CreateEntity(UserConnection, "DgCRMGroup", crmGroupValues);
        }

        protected virtual void CRMGroupSave(bool IsResubmission)
        {
            if (this.crmGroup.DNOId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DNO))
            {
                this.crmGroup.DNOId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgDNO", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.DNO}
                });
            }

            if (this.crmGroup.DNOIdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DNOIdType))
            {
                this.crmGroup.DNOIdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.DNOIdType}
                });
            }

            if (this.crmGroup.LegalAddress != null)
            {
                if (this.crmGroup.LegalAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.City))
                {
                    this.crmGroup.LegalAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.City.Length == 4 && this.crmGroup.LegalAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.City}
                    });
                }

                if (this.crmGroup.LegalAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.State))
                {
                    this.crmGroup.LegalAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.LegalAddress.State}
                    });
                }

                if (this.crmGroup.LegalAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.Country))
                {
                    this.crmGroup.LegalAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.Country.Length == 4 && int.TryParse(this.crmGroup.LegalAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.Country}
                    });
                }

                if (this.crmGroup.LegalAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.PostCode))
                {
                    this.crmGroup.LegalAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.LegalAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.BillAddress != null)
            {
                if (this.crmGroup.BillAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.City))
                {
                    this.crmGroup.BillAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.City.Length == 4 && this.crmGroup.BillAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.City}
                    });
                }

                if (this.crmGroup.BillAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.State))
                {
                    this.crmGroup.BillAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.BillAddress.State}
                    });
                }

                if (this.crmGroup.BillAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.Country))
                {
                    this.crmGroup.BillAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.Country.Length == 4 && int.TryParse(this.crmGroup.BillAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.Country}
                    });
                }

                if (this.crmGroup.BillAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.PostCode))
                {
                    this.crmGroup.BillAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.BillAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.DeliveryAddress != null)
            {
                if (this.crmGroup.DeliveryAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.City))
                {
                    this.crmGroup.DeliveryAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.City.Length == 4 && this.crmGroup.DeliveryAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.City}
                    });
                }

                if (this.crmGroup.DeliveryAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.State))
                {
                    this.crmGroup.DeliveryAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.City.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.DeliveryAddress.State}
                    });
                }

                if (this.crmGroup.DeliveryAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.Country))
                {
                    this.crmGroup.DeliveryAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.Country.Length == 4 && int.TryParse(this.crmGroup.DeliveryAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.Country}
                    });
                }

                if (this.crmGroup.DeliveryAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.PostCode))
                {
                    this.crmGroup.DeliveryAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.DeliveryAddress.PostCode}
                    });
                }
            }

            if (this.crmGroup.Admin1 != null)
            {
                if (this.crmGroup.Admin1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin1.IdType))
                {
                    this.crmGroup.Admin1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin1.IdType}
                    });
                }

                if (this.crmGroup.Admin1.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Admin1.IdNo))
                    {
                        this.crmGroup.Admin1.IdNo = this.crmGroup.Admin1.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Admin2 != null)
            {
                if (this.crmGroup.Admin2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin2.IdType))
                {
                    this.crmGroup.Admin2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin2.IdType}
                    });
                }

                if (this.crmGroup.Admin2.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Admin2.IdNo))
                    {
                        this.crmGroup.Admin2.IdNo = this.crmGroup.Admin2.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Auth1 != null)
            {
                if (this.crmGroup.Auth1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth1.IdType))
                {
                    this.crmGroup.Auth1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth1.IdType}
                    });
                }

                if (this.crmGroup.Auth1.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Auth1.IdNo))
                    {
                        this.crmGroup.Auth1.IdNo = this.crmGroup.Auth1.IdNo.Replace("-", "");
                    }
                }
            }

            if (this.crmGroup.Auth2 != null)
            {
                if (this.crmGroup.Auth2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth2.IdType))
                {
                    this.crmGroup.Auth2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth2.IdType}
                    });
                }

                if (this.crmGroup.Auth2.IdTypeId == Lookup.IDType.NRIC)
                {
                    if (!string.IsNullOrEmpty(this.crmGroup.Auth2.IdNo))
                    {
                        this.crmGroup.Auth2.IdNo = this.crmGroup.Auth2.IdNo.Replace("-", "");
                    }
                }
            }

            var crmGroupValues = new Dictionary<string, object>();
            crmGroupValues.Add("DgName", this.crmGroup.GroupName);
            crmGroupValues.Add("DgBRN", this.crmGroup.BRN);

            crmGroupValues.Add("DgLegalAddress", this.crmGroup.LegalAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityId", this.crmGroup.LegalAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeId", this.crmGroup.LegalAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateId", this.crmGroup.LegalAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryId", this.crmGroup.LegalAddress?.CountryId ?? Guid.Empty);
            crmGroupValues.Add("DgTelNo", this.crmGroup.TelNo);
            crmGroupValues.Add("DgFaxNo", this.crmGroup.TelNo);
            crmGroupValues.Add("DgBillingEmailAddress", this.crmGroup.BillingEmailAddress);
            crmGroupValues.Add("DgCompanyEmail", this.crmGroup.BillingEmailAddress);

            crmGroupValues.Add("DgBillingAddress", this.crmGroup.BillAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityAdmInformationBillingId", this.crmGroup.BillAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeAdmInformationBillingId", this.crmGroup.BillAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateAdmInfoBillingId", this.crmGroup.BillAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryAdmInformationBillingId", this.crmGroup.BillAddress?.CountryId ?? Guid.Empty);

            crmGroupValues.Add("DgDeliveryaddress", this.crmGroup.DeliveryAddress?.StreetAddress ?? string.Empty);
            crmGroupValues.Add("DgCityAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.CityId ?? Guid.Empty);
            crmGroupValues.Add("DgPostcodeAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.PostCodeId ?? Guid.Empty);
            crmGroupValues.Add("DgStateAdmInfoDeliveryId", this.crmGroup.DeliveryAddress?.StateId ?? Guid.Empty);
            crmGroupValues.Add("DgCountryAdmInformationDeliveryId", this.crmGroup.DeliveryAddress?.CountryId ?? Guid.Empty);

            crmGroupValues.Add("DgAdministrationName1", this.crmGroup.Admin1?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAdm1Id", this.crmGroup.Admin1?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgIdNo1", this.crmGroup.Admin1?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgMobilePhone1", this.crmGroup.Admin1?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAdm1", this.crmGroup.Admin1?.Designation ?? string.Empty);
            crmGroupValues.Add("DgOfficeTelNo1", this.crmGroup.Admin1?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAdministrationEmail1", this.crmGroup.Admin1?.Email ?? string.Empty);

            crmGroupValues.Add("DgAdministrationName2", this.crmGroup.Admin2?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAdm2Id", this.crmGroup.Admin2?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgIdNo2", this.crmGroup.Admin2?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgMobilePhone2", this.crmGroup.Admin2?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAdm2", this.crmGroup.Admin2?.Designation ?? string.Empty);
            crmGroupValues.Add("DgOfficeTelNo2", this.crmGroup.Admin2?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAdministrationEmail2", this.crmGroup.Admin2?.Email ?? string.Empty);

            crmGroupValues.Add("DgAuthorizedName1", this.crmGroup.Auth1?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAuth1Id", this.crmGroup.Auth1?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgAuthorizedIdNo1", this.crmGroup.Auth1?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedMobilePhone1", this.crmGroup.Auth1?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAuth1", this.crmGroup.Auth1?.Designation ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedOfficeTelNo1", this.crmGroup.Auth1?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedEmail1", this.crmGroup.Auth1?.Email ?? string.Empty);

            crmGroupValues.Add("DgAuthorizedName2", this.crmGroup.Auth2?.Name ?? string.Empty);
            crmGroupValues.Add("DgIDTypeAuth2Id", this.crmGroup.Auth2?.IdTypeId ?? Guid.Empty);
            crmGroupValues.Add("DgAuthorizedIdNo2", this.crmGroup.Auth2?.IdNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedMobilePhone2", this.crmGroup.Auth2?.MobileNo ?? string.Empty);
            crmGroupValues.Add("DgDesignationAuth2", this.crmGroup.Auth2?.Designation ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedOfficeTelNo2", this.crmGroup.Auth2?.TelNo ?? string.Empty);
            crmGroupValues.Add("DgAuthorizedEmail2", this.crmGroup.Auth2?.Email ?? string.Empty);
            crmGroupValues.Add("DgTINNumber", this.crmGroup.TINNumber ?? string.Empty);
            crmGroupValues.Add("DgSST", this.crmGroup.SSTNumber ?? string.Empty);

            if (this.crmGroup.Id == null || this.crmGroup.Id == Guid.Empty)
            {
                if (this.submission.Id == Guid.Empty)
                {
                    this.submission.Id = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgSubmission", new Dictionary<string, object>() {
                        {"DgSerialNumber", this.submission.SerialNumber}
                    });
                }

                var submissionInfo = ISAEntityHelper.EntityHelper.EntityHelper.GetEntity(UserConnection, "DgSubmission", this.submission.Id, new Dictionary<string, string>() {
                    {"DgCRMGroupId", "guid"}
                });
                this.crmGroup.Id = (Guid)submissionInfo["DgCRMGroupId"];
            }

            ISAEntityHelper.EntityHelper.EntityHelper.UpdateEntity(UserConnection, "DgCRMGroup", this.crmGroup.Id, crmGroupValues);
        }

        protected virtual void SubmissionSave(DBExecutor dbExecutor)
        {
            if (this.submission == null)
            {
                throw new Exception("This method can only be called if the Submission property is already defined");
            }

            if (this.submission.SalespersonId == Guid.Empty)
            {
                this.submission.SalespersonId = GetSalespersonId();
            }

            if (this.submission.RegionId == Guid.Empty)
            {
                this.submission.RegionId = GetRegionId(this.submission.SalespersonId);
                if (this.submission.RegionId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Region))
                {
                    this.submission.RegionId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgRegion", new Dictionary<string, object>() {
                        {this.submission.Region.Length == 1 ? "DgCode" : "Name", this.submission.Region}
                    });
                }
            }

            if (this.submission.AreaId == Guid.Empty)
            {
                this.submission.AreaId = GetAreaId(this.crmGroup.LegalAddress?.StateId ?? Guid.Empty);
                if (this.submission.AreaId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Area))
                {
                    this.submission.AreaId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgArea", new Dictionary<string, object>() {
                        {this.submission.Area.Length == 1 ? "DgCode" : "Name", this.submission.Area}
                    });
                }
            }

            if (this.submission.CompanyId == Guid.Empty)
            {
                this.submission.CompanyId = GetCompanyId();
            }

            if (this.submission.IdTypeId == Guid.Empty)
            {
                this.submission.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {this.submission.IdType.Length == 1 ? "DgCode" : "Name", this.submission.IdType}
                });
            }

            string email = string.Empty;
            switch (this.submission.SubscriberType)
            {
                case "CI":
                    this.submission.SubscriberTypeId = Lookup.SubscriberType.CI;
                    email = !string.IsNullOrEmpty(this.crmGroup.Admin1?.Email) ? this.crmGroup.Admin1?.Email : this.crmGroup.Auth1?.Email;
                    break;
                case "Corporate":
                default:
                    this.submission.SubscriberTypeId = Lookup.SubscriberType.Corporate;
                    email = this.crmGroup.BillingEmailAddress;
                    break;
            }

            if (this.submission.GenderId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Gender))
            {
                this.submission.GenderId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "Gender", new Dictionary<string, object>() {
                    {"Name", this.submission.Gender}
                });
            }

            if (this.submission.CardTypeId == Guid.Empty && !string.IsNullOrEmpty(this.submission.CardType))
            {
                this.submission.CardTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgCardType", new Dictionary<string, object>() {
                    {"Name", this.submission.CardType}
                });
            }

            if (this.submission.BankIssuerId == Guid.Empty && !string.IsNullOrEmpty(this.submission.BankIssuer))
            {
                this.submission.BankIssuerId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgBankIssuer", new Dictionary<string, object>() {
                    {"Name", this.submission.BankIssuer}
                });
            }

            if (this.submission.SubmissionTypeId == Guid.Empty && !string.IsNullOrEmpty(this.submission.SubmissionType))
            {
                this.submission.SubmissionTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgSubmissionType", new Dictionary<string, object>() {
                    {"Name", this.submission.SubmissionType}
                });
            }

            if (this.crmGroup.CompanyIncorparationId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.CompanyIncorparation))
            {
                this.crmGroup.CompanyIncorparationId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgCompanyIncorporation", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.CompanyIncorparation}
                });
            }

            // update to search data by name or code, get top 1
            if (this.crmGroup.SalesChannelId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.SalesChannel))
            {
                this.crmGroup.SalesChannelId = dbExecutor != null ? GetSalesChannel(dbExecutor, this.crmGroup.SalesChannel) : GetSalesChannel(this.crmGroup.SalesChannel);
            }

            if (this.submission.TitleId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Title))
            {
                this.submission.TitleId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgTitle", new Dictionary<string, object>() {
                    {"Name", this.submission.Title}
                });
            }

            if (this.crmGroup.AutoBilling)
            {
                CardNumberValidation(this.submission.Last4DigitCardNumber);

                string customerType = this.submission.SubscriberTypeId == Lookup.SubscriberType.CI ? "INDIVIDUAL" : "CORPORATE";

                var idTypeInfo = ISAEntityHelper.EntityHelper.EntityHelper.GetEntity(UserConnection, "DgIDType", this.submission.IdTypeId, new Dictionary<string, string>() {
                    {"Name", "string"}
                });
                string idType = idTypeInfo["Name"]?.ToString();
                var creditCardToken = CRMHelper.GetCreditCardToken(
                    UserConnection,
                    customerType,
                    idType,
                    this.submission.IdNo,
                    this.submission.CustomerName,
                    this.submission.Last4DigitCardNumber
                );

                if (creditCardToken != null)
                {
                    this.submission.CardTypeId = creditCardToken.CardTypeId;
                    this.submission.BankIssuerId = creditCardToken.BankIssuerId;
                    this.submission.CardOwnerName = creditCardToken.CardHolderName;
                    this.submission.CardExpiredDate = creditCardToken.CardExp;
                    this.submission.PlainCardNumber = creditCardToken.CardNumber;
                }
            }

            var now = DateTime.UtcNow;
            var submissionValues = new Dictionary<string, object>();
            submissionValues.Add("DgCustomerId", this.submission.CustomerId ?? string.Empty);
            submissionValues.Add("DgAccountId", this.submission.AccountId ?? string.Empty);
            submissionValues.Add("DgAccountCode", this.submission.AccountCode ?? string.Empty);
            submissionValues.Add("DgGenderId", this.submission.GenderId);
            submissionValues.Add("DgAreaId", this.submission.AreaId);
            submissionValues.Add("DgCompanyId", this.submission.CompanyId);
            submissionValues.Add("DgCompanyName", this.submission.CompanyName ?? string.Empty);
            submissionValues.Add("DgCustomerName", this.submission.CustomerName ?? string.Empty);
            submissionValues.Add("DgRegionId", this.submission.RegionId);
            submissionValues.Add("DgSalespersonId", this.submission.SalespersonId);
            submissionValues.Add("DgDealerId", this.crmGroup.DealerId);
            submissionValues.Add("DgSourceId", this.submission.SourceId);
            submissionValues.Add("DgRemark", this.submission.SubmissionRemark ?? string.Empty);
            submissionValues.Add("DgExposure", this.submission.TotalCreditExposure);
            submissionValues.Add("DgStatusId", Lookup.Status.Open);
            submissionValues.Add("DgSubscriberTypeId", this.submission.SubscriberTypeId);
            submissionValues.Add("DgIDTypeId", this.submission.IdTypeId);
            submissionValues.Add("DgIDNo", this.submission.IdNo ?? string.Empty);
            submissionValues.Add("DgPaymentModeId", this.crmGroup.PaymentModeId);
            submissionValues.Add("DgCardTypeId", this.submission.CardTypeId);
            submissionValues.Add("DgOwnerName", this.submission.CardOwnerName ?? string.Empty);
            submissionValues.Add("DgCardOwner", this.submission.CardOwnerName ?? string.Empty);
            submissionValues.Add("DgBankIssuerId", this.submission.BankIssuerId);
            submissionValues.Add("DgCardExpiredDate", this.submission.CardExpiredDate);
            submissionValues.Add("DgSubmissionTypeId", this.submission.SubmissionTypeId);
            submissionValues.Add("DgCompanyIncorporationId", this.crmGroup.CompanyIncorparationId);
            submissionValues.Add("DgMNP", this.crmGroup.DNO ?? string.Empty);
            submissionValues.Add("DgExistingCustomerMobile", this.crmGroup.ExistingCustomerMobile ?? string.Empty);
            submissionValues.Add("DgOthers", this.crmGroup.Others ?? string.Empty);
            submissionValues.Add("DgChannelId", this.crmGroup.SalesChannelId);
            submissionValues.Add("DgCMSID", this.submission.CMSId ?? string.Empty);
            submissionValues.Add("DgCardNumber", this.submission.Last4DigitCardNumber ?? string.Empty);
            submissionValues.Add("DgPlainCardNumber", this.submission.PlainCardNumber ?? string.Empty);
            submissionValues.Add("DgTitleId", this.submission.TitleId);
            submissionValues.Add("DgCRMGroupId", this.crmGroup.Id);
            submissionValues.Add("DgApplicantName", this.submission.ApplicantName ?? string.Empty);
            submissionValues.Add("DgPaymentSubmittedId", this.submission.PaymentSubmittedId);
            submissionValues.Add("DgReferenceContactName", this.submission.ReferenceContactName ?? string.Empty);
            submissionValues.Add("DgReferenceContactTelNo", this.submission.ReferenceContactTelNo ?? string.Empty);
            submissionValues.Add("DgSubmissionInOrderId", this.submission.SFAId);
            submissionValues.Add("DgBot1", this.submission.FastLane);
            submissionValues.Add("DgFL", this.submission.FastLane);
            submissionValues.Add("DgEmail", email ?? string.Empty);

            if (!string.IsNullOrEmpty(this.submission.Nationality))
            {
                Guid nationality = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                    {"Name", this.submission.Nationality}
                });
                submissionValues.Add("DgNationalityId", nationality);
            }

            if (this.submission.DateOfBirth != null && this.submission.DateOfBirth != DateTime.MinValue)
            {
                submissionValues.Add("DgDateOfBirth", this.submission.DateOfBirth);
            }

            if (this.submission.ResubmissionDate != null && this.submission.ResubmissionDate != DateTime.MinValue)
            {
                submissionValues.Add("DgResubmission", this.submission.ResubmissionDate);
            }

            submissionValues.Add("DgResubmissionNumber", this.submission.ResubmissionNumber);

            /** Hapus saat kolom nya sudah dihapus dari submission */
            Guid billMediumId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgBillMedium", new Dictionary<string, object>() {
                {"Name", GetBillMedium()}
            });
            submissionValues.Add("DgBillMediumId", billMediumId);
            submissionValues.Add("DgCompanyLegalAddress", this.crmGroup.LegalAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgBillingAddress", this.crmGroup.BillAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgDeliveryAddress", this.crmGroup.DeliveryAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgShipCityId", this.crmGroup.DeliveryAddress?.CityId ?? Guid.Empty);
            submissionValues.Add("DgShipPostcodeId", this.crmGroup.DeliveryAddress?.PostCodeId ?? Guid.Empty);
            submissionValues.Add("DgShipStateId", this.crmGroup.DeliveryAddress?.StateId ?? Guid.Empty);
            submissionValues.Add("DgShipCountryId", this.crmGroup.DeliveryAddress?.CountryId ?? Guid.Empty);
            submissionValues.Add("DgAdministration1Name", this.crmGroup.Admin1?.Name ?? string.Empty);
            submissionValues.Add("DgIdNoAdministration1", this.crmGroup.Admin1?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAdministration1", this.crmGroup.Admin1?.MobileNo ?? string.Empty);
            submissionValues.Add("DgOfficeTelNoAdministration1", this.crmGroup.Admin1?.TelNo ?? string.Empty);
            submissionValues.Add("DgAdministration2Name", this.crmGroup.Admin2?.Name ?? string.Empty);
            submissionValues.Add("DgIdNoAdministration2", this.crmGroup.Admin2?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAdministration2", this.crmGroup.Admin2?.MobileNo ?? string.Empty);
            submissionValues.Add("DgOfficeTelnoAdministration2", this.crmGroup.Admin2?.TelNo ?? string.Empty);
            submissionValues.Add("DgBillCityId", this.crmGroup.BillAddress?.CityId ?? Guid.Empty);
            submissionValues.Add("DgBillPostcodeId", this.crmGroup.BillAddress?.PostCodeId ?? Guid.Empty);
            submissionValues.Add("DgBillStateId", this.crmGroup.BillAddress?.StateId ?? Guid.Empty);
            submissionValues.Add("DgBillCountryId", this.crmGroup.BillAddress?.CountryId ?? Guid.Empty);
            submissionValues.Add("DgAuthorized1Name", this.crmGroup.Auth1?.Name ?? string.Empty);
            submissionValues.Add("DgAuthorized1IdNo", this.crmGroup.Auth1?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAuthorized1", this.crmGroup.Auth2?.Name ?? string.Empty);
            submissionValues.Add("DgOfficeTelNoAuthorized1", this.crmGroup.Auth2?.IdNo ?? string.Empty);
            /** End Hapus saat kolom nya sudah dihapus dari submission */

            submissionValues.Add("CreatedById", UserConnection.CurrentUser.ContactId);
            submissionValues.Add("CreatedOn", now);
            submissionValues.Add("DgSubmissionStatusId", Lookup.SubmissionStatus.New);

            if (this.submission.SignUpDate != null && this.submission.SignUpDate != DateTime.MinValue)
            {
                submissionValues.Add("DgSignUp", this.submission.SignUpDate);
            }
            else
            {
                submissionValues.Add("DgSignUp", now);
            }

            if (this.submission.ReceiveDate != null && this.submission.ReceiveDate != DateTime.MinValue)
            {
                submissionValues.Add("DgDateReceived", this.submission.ReceiveDate);
            }
            else
            {
                submissionValues.Add("DgDateReceived", now);
            }

            this.submission.Id = ISAEntityHelper.EntityHelper.EntityHelper.CreateEntity(UserConnection, "DgSubmission", submissionValues);
            var generateSerialNumber = this.documentNumberGenerator.GenerateNumber("DgSubmission", this.submission.Id, true);
            if (!generateSerialNumber.Success || string.IsNullOrEmpty(generateSerialNumber.DocumentNumber))
            {
                throw new Exception(generateSerialNumber.Message);
            }

            this.submission.SerialNumber = generateSerialNumber.DocumentNumber;
            new Update(UserConnection, "DgSubmission")
                .Set("DgName", Column.Parameter(this.submission.SerialNumber))
                .Where("Id").IsEqual(Column.Parameter(this.submission.Id))
                .Execute();
        }

        protected virtual void SubmissionSave(DBExecutor dbExecutor, bool IsResubmission)
        {
            if (this.submission == null)
            {
                throw new Exception("This method can only be called if the Submission property is already defined");
            }

            if (this.submission.CompanyId == Guid.Empty)
            {
                this.submission.CompanyId = GetCompanyId();
            }

            if (this.submission.IdTypeId == Guid.Empty)
            {
                this.submission.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {this.submission.IdType.Length == 1 ? "DgCode" : "Name", this.submission.IdType}
                });
            }

            string email = string.Empty;
            switch (this.submission.SubscriberType)
            {
                case "CI":
                    this.submission.SubscriberTypeId = Lookup.SubscriberType.CI;
                    email = !string.IsNullOrEmpty(this.crmGroup.Admin1?.Email) ? this.crmGroup.Admin1?.Email : this.crmGroup.Auth1?.Email;
                    break;
                case "Corporate":
                default:
                    this.submission.SubscriberTypeId = Lookup.SubscriberType.Corporate;
                    email = this.crmGroup.BillingEmailAddress;
                    break;
            }

            if (this.submission.GenderId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Gender))
            {
                this.submission.GenderId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "Gender", new Dictionary<string, object>() {
                    {"Name", this.submission.Gender}
                });
            }

            if (this.submission.CardTypeId == Guid.Empty && !string.IsNullOrEmpty(this.submission.CardType))
            {
                this.submission.CardTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgCardType", new Dictionary<string, object>() {
                    {"Name", this.submission.CardType}
                });
            }

            if (this.submission.BankIssuerId == Guid.Empty && !string.IsNullOrEmpty(this.submission.BankIssuer))
            {
                this.submission.BankIssuerId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgBankIssuer", new Dictionary<string, object>() {
                    {"Name", this.submission.BankIssuer}
                });
            }

            if (this.crmGroup.CompanyIncorparationId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.CompanyIncorparation))
            {
                this.crmGroup.CompanyIncorparationId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgCompanyIncorporation", new Dictionary<string, object>() {
                    {"Name", this.crmGroup.CompanyIncorparation}
                });
            }

            if (this.crmGroup.SalesChannelId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.SalesChannel))
            {
                this.crmGroup.SalesChannelId = dbExecutor != null ? GetSalesChannel(dbExecutor, this.crmGroup.SalesChannel) : GetSalesChannel(this.crmGroup.SalesChannel);
            }

            if (this.submission.TitleId == Guid.Empty && !string.IsNullOrEmpty(this.submission.Title))
            {
                this.submission.TitleId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgTitle", new Dictionary<string, object>() {
                    {"Name", this.submission.Title}
                });
            }

            if (this.crmGroup.AutoBilling)
            {
                CardNumberValidation(this.submission.Last4DigitCardNumber);

                string customerType = this.submission.SubscriberTypeId == Lookup.SubscriberType.CI ? "INDIVIDUAL" : "CORPORATE";

                var idTypeInfo = ISAEntityHelper.EntityHelper.EntityHelper.GetEntity(UserConnection, "DgIDType", this.submission.IdTypeId, new Dictionary<string, string>() {
                    {"Name", "string"}
                });
                string idType = idTypeInfo["Name"]?.ToString();
                var creditCardToken = CRMHelper.GetCreditCardToken(
                    UserConnection,
                    customerType,
                    idType,
                    this.submission.IdNo,
                    this.submission.CustomerName,
                    this.submission.Last4DigitCardNumber
                );

                if (creditCardToken != null)
                {
                    this.submission.CardTypeId = creditCardToken.CardTypeId;
                    this.submission.BankIssuerId = creditCardToken.BankIssuerId;
                    this.submission.CardOwnerName = creditCardToken.CardHolderName;
                    this.submission.CardExpiredDate = creditCardToken.CardExp;
                    this.submission.PlainCardNumber = creditCardToken.CardNumber;
                }
            }

            var now = DateTime.UtcNow;
            var submissionValues = new Dictionary<string, object>();
            submissionValues.Add("DgGenderId", this.submission.GenderId);
            submissionValues.Add("DgCompanyId", this.submission.CompanyId);
            submissionValues.Add("DgCompanyName", this.submission.CompanyName ?? string.Empty);
            submissionValues.Add("DgCustomerName", this.submission.CustomerName ?? string.Empty);
            submissionValues.Add("DgRemark", this.submission.SubmissionRemark ?? string.Empty);
            submissionValues.Add("DgExposure", this.submission.TotalCreditExposure);
            submissionValues.Add("DgSubscriberTypeId", this.submission.SubscriberTypeId);
            submissionValues.Add("DgIDTypeId", this.submission.IdTypeId);
            submissionValues.Add("DgIDNo", this.submission.IdNo ?? string.Empty);
            submissionValues.Add("DgCardTypeId", this.submission.CardTypeId);
            submissionValues.Add("DgOwnerName", this.submission.CardOwnerName ?? string.Empty);
            submissionValues.Add("DgCardOwner", this.submission.CardOwnerName ?? string.Empty);
            submissionValues.Add("DgBankIssuerId", this.submission.BankIssuerId);
            submissionValues.Add("DgCardExpiredDate", this.submission.CardExpiredDate);
            submissionValues.Add("DgCompanyIncorporationId", this.crmGroup.CompanyIncorparationId);
            submissionValues.Add("DgMNP", this.crmGroup.DNO ?? string.Empty);
            submissionValues.Add("DgExistingCustomerMobile", this.crmGroup.ExistingCustomerMobile ?? string.Empty);
            submissionValues.Add("DgOthers", this.crmGroup.Others ?? string.Empty);
            submissionValues.Add("DgCMSID", this.submission.CMSId ?? string.Empty);
            submissionValues.Add("DgCardNumber", this.submission.Last4DigitCardNumber ?? string.Empty);
            submissionValues.Add("DgPlainCardNumber", this.submission.PlainCardNumber ?? string.Empty);
            submissionValues.Add("DgTitleId", this.submission.TitleId);
            submissionValues.Add("DgApplicantName", this.submission.ApplicantName ?? string.Empty);
            submissionValues.Add("DgBot1", this.submission.FastLane);
            submissionValues.Add("DgFL", this.submission.FastLane);
            submissionValues.Add("DgEmail", email ?? string.Empty);

            if (!string.IsNullOrEmpty(this.submission.Nationality))
            {
                Guid nationality = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                    {"Name", this.submission.Nationality}
                });
                submissionValues.Add("DgNationalityId", nationality);
            }

            if (this.submission.DateOfBirth != null && this.submission.DateOfBirth != DateTime.MinValue)
            {
                submissionValues.Add("DgDateOfBirth", this.submission.DateOfBirth);
            }

            if (this.submission.ResubmissionDate != null && this.submission.ResubmissionDate != DateTime.MinValue)
            {
                submissionValues.Add("DgResubmission", this.submission.ResubmissionDate);
            }

            submissionValues.Add("DgResubmissionNumber", this.submission.ResubmissionNumber);

            /** Hapus saat kolom nya sudah dihapus dari submission */
            Guid billMediumId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgBillMedium", new Dictionary<string, object>() {
                {"Name", GetBillMedium()}
            });
            submissionValues.Add("DgBillMediumId", billMediumId);
            submissionValues.Add("DgCompanyLegalAddress", this.crmGroup.LegalAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgBillingAddress", this.crmGroup.BillAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgDeliveryAddress", this.crmGroup.DeliveryAddress?.StreetAddress ?? string.Empty);
            submissionValues.Add("DgShipCityId", this.crmGroup.DeliveryAddress?.CityId ?? Guid.Empty);
            submissionValues.Add("DgShipPostcodeId", this.crmGroup.DeliveryAddress?.PostCodeId ?? Guid.Empty);
            submissionValues.Add("DgShipStateId", this.crmGroup.DeliveryAddress?.StateId ?? Guid.Empty);
            submissionValues.Add("DgShipCountryId", this.crmGroup.DeliveryAddress?.CountryId ?? Guid.Empty);
            submissionValues.Add("DgAdministration1Name", this.crmGroup.Admin1?.Name ?? string.Empty);
            submissionValues.Add("DgIdNoAdministration1", this.crmGroup.Admin1?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAdministration1", this.crmGroup.Admin1?.MobileNo ?? string.Empty);
            submissionValues.Add("DgOfficeTelNoAdministration1", this.crmGroup.Admin1?.TelNo ?? string.Empty);
            submissionValues.Add("DgAdministration2Name", this.crmGroup.Admin2?.Name ?? string.Empty);
            submissionValues.Add("DgIdNoAdministration2", this.crmGroup.Admin2?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAdministration2", this.crmGroup.Admin2?.MobileNo ?? string.Empty);
            submissionValues.Add("DgOfficeTelnoAdministration2", this.crmGroup.Admin2?.TelNo ?? string.Empty);
            submissionValues.Add("DgBillCityId", this.crmGroup.BillAddress?.CityId ?? Guid.Empty);
            submissionValues.Add("DgBillPostcodeId", this.crmGroup.BillAddress?.PostCodeId ?? Guid.Empty);
            submissionValues.Add("DgBillStateId", this.crmGroup.BillAddress?.StateId ?? Guid.Empty);
            submissionValues.Add("DgBillCountryId", this.crmGroup.BillAddress?.CountryId ?? Guid.Empty);
            submissionValues.Add("DgAuthorized1Name", this.crmGroup.Auth1?.Name ?? string.Empty);
            submissionValues.Add("DgAuthorized1IdNo", this.crmGroup.Auth1?.IdNo ?? string.Empty);
            submissionValues.Add("DgMobilePhoneAuthorized1", this.crmGroup.Auth2?.Name ?? string.Empty);
            submissionValues.Add("DgOfficeTelNoAuthorized1", this.crmGroup.Auth2?.IdNo ?? string.Empty);
            /** End Hapus saat kolom nya sudah dihapus dari submission */

            if (this.submission.SubmissionStatusId != Guid.Empty)
            {
                submissionValues.Add("DgSubmissionStatusId", this.submission.SubmissionStatusId);
            }

            submissionValues.Add("ModifiedById", UserConnection.CurrentUser.ContactId);
            submissionValues.Add("ModifiedOn", now);

            ISAEntityHelper.EntityHelper.EntityHelper.UpdateEntity(UserConnection, "DgSubmission", this.submission.Id, submissionValues);
        }

        protected virtual void LineDetailSave(DBExecutor dbExecutor, bool isUpdate = false)
        {
            if (this.submission == null || (this.lineDetail == null || (this.lineDetail != null && this.lineDetail.Count == 0)))
            {
                throw new Exception("This method can only be called if the Submission and Line property is already defined");
            }

            foreach (LineDetail line in this.lineDetail)
            {
                Guid primaryOfferId = Guid.Empty;
                if (!string.IsNullOrEmpty(line.RatePlan))
                {
                    primaryOfferId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgOffering", new Dictionary<string, object>() {
                        {"DgOfferName", line.RatePlan}
                    });
                }

                Guid prpcId = line.PrMode == "Full" ? Lookup.PRPC.Console : line.PrMode == "No" ? Lookup.PRPC.Private : Guid.Empty;

                Guid DNOId = this.crmGroup.DNOId;
                if (line.DNOId == Guid.Empty && !string.IsNullOrEmpty(line.DNO))
                {
                    line.DNOId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgDNO", new Dictionary<string, object>() {
                        {"Name", line.DNO}
                    });
                    DNOId = line.DNOId;
                }

                Guid DNOIdTypeId = this.crmGroup.DNOIdTypeId;
                if (line.DNOIdTypeId == Guid.Empty && !string.IsNullOrEmpty(line.DNOIdType))
                {
                    line.DNOIdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {"Name", line.DNOIdType}
                    });
                    DNOIdTypeId = line.DNOIdTypeId;
                }

                string DNOIdNo = !string.IsNullOrEmpty(line.DNOIdNo) ? line.DNOIdNo : this.crmGroup.DNOIdNo;
                string DNOCompanyName = !string.IsNullOrEmpty(line.DNOCompanyName) ? line.DNOCompanyName : this.crmGroup.DNOCompanyName;
                string DNOAccountNo = !string.IsNullOrEmpty(line.DNOAccountNo) ? line.DNOAccountNo : this.crmGroup.DNOAccountNo;

                var lineDetailValues = new Dictionary<string, object>() {
                    {"DgNo", line.No},
                    {"DgName", line.MSISDN},
                    {"DgSubmissionId", this.submission.Id},
                    {"DgUsername", line.Username},
                    {"DgMSISDN", line.MSISDN},
                    {"DgPrimaryOfferingId", primaryOfferId},
                    {"DgVAS", line.Vas},
                    {
                        "DgToSId",
                        !string.IsNullOrEmpty(line.Tos) ?
                            ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgToS", new Dictionary<string, object>() {
                                {"Name", line.Tos}
                            }) : Guid.Empty
                    },
                    {"DgPRPCId", prpcId},
                    {
                        "DgTenureId",
                        !string.IsNullOrEmpty(line.Contract) ?
                            ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgTenure", new Dictionary<string, object>() {
                                {"Name", line.Contract}
                            }) : Guid.Empty
                    },
                    {"DgCreditLimit", line.CreditLimit},
                    {"DgAutobilling", line.AutoBilling},
                    {"DgAB", line.AutoBilling},
                    {"DgDeviceModel", line.PhoneModel},
                    {"DgRemark", line.Remark},
                    {"DgDeviceOrderRemark", line.Remark},
                    {
                        "DgOrderIMSITypeId",
                        !string.IsNullOrEmpty(line.Imsi) ?
                            ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(UserConnection, "DgOrderIMSIType", new Dictionary<string, object>() {
                                {"Name", line.Imsi}
                            }) : Guid.Empty
                    },
                    {"DgSIMPackageCode", line.ImsiPackageCode},
                    {"DgSIMCardNumber", line.SimCardSerialNo},
                    {"DgCPFlag", line.ConceptPaper},
                    {"DgDNOId", DNOId},
                    // {"DgDNOCompanyId", DNOCompanyId},
                    {"DgDNOCompanyName", DNOCompanyName},
                    {"DgDNOAccNo", DNOAccountNo},
                    {"DgDNOIDTypeId", DNOIdTypeId},
                    {"DgDNOIdNo", DNOIdNo},
                };

                if (line.SuppOffers == null || (line.SuppOffers != null && line.SuppOffers.Count <= 0))
                {
                    var suppOffer = new List<string>() {
                        line.MandatoryOffer1,
                        line.MandatoryOffer2,
                        line.MandatoryOffer3,
                        line.DataBundle,
                        line.DataElement,
                        line.AdvBundle,
                        line.AdvancePayment,
                        line.ContractBundle,
                        line.ContractElement
                    };

                    bool allowAutomatic10 = true;
                    if (line.Element1 == "digisecure" ||
                        line.Element2 == "digisecure" ||
                        line.Element3_1 == "digisecure" ||
                        line.Element3_2 == "digisecure" ||
                        line.Element3_3 == "digisecure")
                    {

                        allowAutomatic10 = false;
                    }

                    if (allowAutomatic10)
                    {
                        suppOffer.Add(line.Automatic10);
                    }

                    suppOffer.Add(line.Automatic11);
                    suppOffer.Add(line.Vas);
                    suppOffer.Add(line.Bundle1);
                    suppOffer.Add(line.Element1);
                    suppOffer.Add(line.Bundle2);
                    suppOffer.Add(line.Element2);
                    suppOffer.Add(line.Bundle3);
                    suppOffer.Add(line.Element3_1);
                    suppOffer.Add(line.Element3_2);
                    suppOffer.Add(line.Element3_3);

                    if (submission.SubmissionType == "COP")
                    {
                        suppOffer.Add("Vo-LTE");
                    }

                    int suppIndex = 1;
                    foreach (var item in suppOffer)
                    {
                        if (string.IsNullOrEmpty(item))
                        {
                            continue;
                        }

                        Guid suppOfferId = GetOfferId(string.Empty, item, this.submission.SubscriberTypeId);
                        lineDetailValues.Add($"DgSuppOffer{suppIndex}Id", suppOfferId);
                        suppIndex++;
                    }
                }
                else
                {
                    /*
                    int suppIndex = 1;
                    foreach (var item in line.SuppOffers) {						
						Guid suppOfferId = GetOfferId(item.OfferId, item.OfferName, this.submission.SubscriberTypeId);
                        lineDetailValues.Add($"DgSuppOffer{suppIndex}Id", suppOfferId);
                        suppIndex++;
                    }
					*/

                    if (submission.SubmissionType == "COP")
                    {
                        line.SuppOffers.Add(new SuppOffer
                        {
                            OfferName = "Vo-LTE"
                        });
                    }

                    int suppOfferCount = line.SuppOffers.Count;
                    for (int i = 0; i < 20; i++)
                    {
                        int suppIndex = i + 1;

                        Guid suppOfferId = Guid.Empty;
                        if (suppIndex <= suppOfferCount)
                        {
                            var item = line.SuppOffers[i];
                            suppOfferId = GetOfferId(item.OfferId, item.OfferName, this.submission.SubscriberTypeId);
                        }

                        lineDetailValues.Add($"DgSuppOffer{suppIndex}Id", suppOfferId);
                    }
                }

                if (!isUpdate)
                {
                    int lineId = line.LineId;
                    if (line.LineId <= 0)
                    {
                        lineId = dbExecutor != null ?
                            Convert.ToInt32(SolarisCore.EntityHelper.GetCodeMask(UserConnection, dbExecutor, "DgLineIDCodeMask", "DgLineIDLastNumber"))
                            : Convert.ToInt32(SolarisCore.EntityHelper.GetCodeMask(UserConnection, "DgLineIDCodeMask", "DgLineIDLastNumber"));
                    }

                    lineDetailValues.Add("DgLineId", lineId);

                    line.Id = ISAEntityHelper.EntityHelper.EntityHelper.CreateEntity(UserConnection, "DgLineDetail", lineDetailValues);
                }
                else
                {
                    if (line.Id == Guid.Empty)
                    {
                        line.Id = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgLineDetail", new Dictionary<string, object>() {
                            {"DgNo", line.No},
                            {"DgSubmissionId", this.submission.Id}
                        });
                    }

                    ISAEntityHelper.EntityHelper.EntityHelper.UpdateEntity(UserConnection, "DgLineDetail", line.Id, lineDetailValues);
                }
            }
        }

        protected virtual void SetSubmissionIntegration()
        {
            if (string.IsNullOrEmpty(this.submission.IdType) && string.IsNullOrEmpty(this.submission.IdNo))
            {
                return;
            }

            bool isCode = this.submission.IdType.Length == 1;
            if (this.submission.IdTypeId == Guid.Empty)
            {
                this.submission.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                    {isCode ? "DgCode" : "Name", this.submission.IdType}
                });
            }

            var idTypeMap = new Dictionary<string, string>() {
                {"1", "NRIC"},
                {"2", "Armed Force"},
                {"3", "BRN"},
                {"4", "Passport"}
            };
            string idType = isCode ? this.submission.IdType : idTypeMap.FirstOrDefault(x => x.Value == this.submission.IdType).Key;
            string idNo = idType == "1" ? this.submission.IdNo.Replace("-", "") : this.submission.IdNo;

            var customers = this.crmService.GetCustomers(idType, idNo).GetAwaiter().GetResult();
            if (customers == null)
            {
                return;
            }

            var customer = customers.FirstOrDefault();
            this.submission.CustomerId = customer.customerId;
            this.submission.CustomerCode = customer.customerCode;

            /*
            var accounts = this.crmService.GetAccountsByCustomerId(this.submission.CustomerId).GetAwaiter().GetResult();
            if(accounts == null) {
                return;
            }

            var account = accounts.FirstOrDefault();
            this.submission.AccountId = account.accountId;
            this.submission.AccountCode = account.accountCode;
			*/
        }

        protected virtual void SetParentCRMGroup(DBExecutor dbExecutor)
        {
            try
            {
                if (string.IsNullOrEmpty(this.crmGroup.GroupNo) && string.IsNullOrEmpty(this.crmGroup.CustomerId))
                {
                    return;
                }

                // GetCustomer
                var parentCustomer = !string.IsNullOrEmpty(this.crmGroup.GroupNo) ?
                    this.crmService.GetCustomersByGroupNo(this.crmGroup.GroupNo).GetAwaiter().GetResult() :
                    this.crmService.GetCustomersById(this.crmGroup.CustomerId).GetAwaiter().GetResult();
                if (parentCustomer == null)
                {
                    return;
                }

                var result = parentCustomer.FirstOrDefault();
                if (result.corporationInfo.businessRegistrationNumber != this.crmGroup.BRN)
                {
                    throw new Exception($"BRN {this.crmGroup.BRN} does not match with Group No");
                }

                this.crmGroup.CustomerId = result.customerId;
                this.crmGroup.CustomerCode = result.customerCode;
                this.crmGroup.CorpNumber = result.corporationInfo.corpNumber;

                if (string.IsNullOrEmpty(this.crmGroup.IndustrialSegment))
                {
                    this.crmGroup.IndustrialSegment = result.corporationInfo.industrySegment;
                    if (this.crmGroup.IndustrialSegmentId == Guid.Empty)
                    {
                        this.crmGroup.IndustrialSegmentId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIndustrialSegment", new Dictionary<string, object>() {
                            {"DgCode", result.corporationInfo.industrySegment}
                        });
                    }
                }

                if (this.crmGroup.BRNExpiryDate == null || this.crmGroup.BRNExpiryDate == DateTime.MinValue)
                {
                    this.crmGroup.BRNExpiryDate = GetValidDateTime(result.corporationInfo.expiryDateofBRN, "yyyyMMdd");
                }

                if (string.IsNullOrEmpty(this.crmGroup.TelNo))
                {
                    this.crmGroup.TelNo = result.corporationInfo.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.BillingEmailAddress))
                {
                    this.crmGroup.BillingEmailAddress = result.corporationInfo.email;
                }

                if (this.crmGroup.GeographicalSpreadId == Guid.Empty)
                {
                    this.crmGroup.GeographicalSpreadId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgGeographicalSpread", new Dictionary<string, object>() {
                        {"DgCode", result.corporationInfo.geographicalSpread}
                    });
                }

                if (string.IsNullOrEmpty(this.crmGroup.SOW))
                {
                    this.crmGroup.SOW = result.corporationInfo.sow;
                }

                if (string.IsNullOrEmpty(this.crmGroup.AccountValue))
                {
                    var accountValueMap = new Dictionary<string, string>() {
                        {"7401", "Low"},
                        {"7402", "Medium"},
                        {"7403", "High"},
                        {"7404", "New Corporate"}
                    };
                    string accountValue = result.corporationInfo.accountValue ?? string.Empty;
                    if (!string.IsNullOrEmpty(accountValue))
                    {
                        this.crmGroup.AccountValue = accountValueMap[result.corporationInfo.accountValue] ?? string.Empty;
                    }
                }

                if (this.crmGroup.DateIncorparation == null || this.crmGroup.DateIncorparation == DateTime.MinValue)
                {
                    this.crmGroup.DateIncorparation = GetValidDateTime(result.corporationInfo.dateofIncorporation ?? string.Empty, "yyyyMMdd");
                }

                if (string.IsNullOrEmpty(this.crmGroup.NoOfEmployees))
                {
                    this.crmGroup.NoOfEmployees = result.corporationInfo.numberofEmployees;
                }

                var picInfos = result.corporationInfo.picInfos;
                SetPIC(picInfos);

                var addressInfos = result.customerAddressInfos;
                SetAddress(addressInfos);

                if (string.IsNullOrEmpty(this.crmGroup.SalesChannel))
                {
                    this.crmGroup.SalesChannel = result.corporationInfo.accountManagerInfo.name;
                }

                if (this.crmGroup.SalesChannelId == Guid.Empty)
                {
                    this.crmGroup.SalesChannelId = dbExecutor != null ? GetSalesChannel(dbExecutor, this.crmGroup.SalesChannel) : GetSalesChannel(this.crmGroup.SalesChannel);
                }

                if (string.IsNullOrEmpty(this.crmGroup.DealerCode))
                {
                    this.crmGroup.DealerCode = result.corporationInfo.accountManagerInfo.dealerCode;
                }

                if (string.IsNullOrEmpty(this.crmGroup.DealerName))
                {
                    this.crmGroup.DealerName = result.corporationInfo.accountManagerInfo.name;
                }

                if (this.crmGroup.DealerId == Guid.Empty)
                {
                    this.crmGroup.DealerId = GetDealerId();
                }

                if (string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo))
                {
                    this.crmGroup.SubParentCustomerId = result.corporationInfo.subCustomerList.FirstOrDefault().customerId;
                }
                // End GetCustomer

                // GetAccount
                var accounts = this.crmService.GetAccountsByCustomerId(this.crmGroup.CustomerId).GetAwaiter().GetResult();
                if (accounts == null)
                {
                    return;
                }

                var account = accounts.FirstOrDefault();

                this.crmGroup.AccountId = account.accountId;
                this.crmGroup.AccountCode = account.accountCode;
                this.crmGroup.PaymentId = account.paymentModeInfo.paymentId;
                this.crmGroup.SubscriberId = account.relaSubscribers.FirstOrDefault()?.subscriberId;

                if (string.IsNullOrEmpty(this.crmGroup.BillingCycle))
                {
                    this.crmGroup.BillingCycle = account.billcycleType;
                }

                if (string.IsNullOrEmpty(this.crmGroup.PaymentMode))
                {
                    this.crmGroup.PaymentMode = GetPaymentMode(account.paymentModeInfo.paymentMode);
                }

                if (this.crmGroup.PaymentModeId == Guid.Empty)
                {
                    this.crmGroup.PaymentModeId = GetPaymentModeId(this.crmGroup.PaymentMode);
                }
                // End GetAccount

                // QueryVPNSub
                var queryVPNs = !string.IsNullOrEmpty(this.crmGroup.GroupNo) ?
                    this.crmService.QueryVPNGroupSubscriberByGroupNo(this.crmGroup.GroupNo).GetAwaiter().GetResult() :
                    this.crmService.QueryVPNGroupSubscriberByCustomerId(this.crmGroup.CustomerId).GetAwaiter().GetResult();
                if (queryVPNs == null)
                {
                    return;
                }

                var queryVPN = queryVPNs.FirstOrDefault();

                this.crmGroup.GroupNo = queryVPN.groupNumber;
                this.crmGroup.GroupId = queryVPN.groupId;
                this.crmGroup.GroupLevel = queryVPN.groupLevel;
                // End QueryVPNSub

            }
            catch (Exception e)
            {
                throw;
            }
        }

        protected virtual void SetSubParentCRMGroup(DBExecutor dbExecutor)
        {
            try
            {
                if (string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo) && string.IsNullOrEmpty(this.crmGroup.SubParentCustomerId))
                {
                    return;
                }

                var subParentCustomer = !string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo) ?
                    this.crmService.GetCustomersByGroupNo(this.crmGroup.SubParentGroupNo).GetAwaiter().GetResult() :
                    this.crmService.GetCustomersById(this.crmGroup.SubParentCustomerId).GetAwaiter().GetResult();
                if (subParentCustomer == null)
                {
                    return;
                }

                var result = subParentCustomer.FirstOrDefault();
                if (result.corporationInfo.businessRegistrationNumber != this.crmGroup.BRN)
                {
                    throw new Exception($"BRN {this.crmGroup.BRN} does not match with Sub Parent Group No");
                }

                this.crmGroup.SubParentCustomerId = result.customerId;
                this.crmGroup.SubParentCustomerCode = result.customerCode;
                this.crmGroup.SubParentCorpNumber = result.corporationInfo.corpNumber;

                var accounts = this.crmService.GetAccountsByCustomerId(this.crmGroup.SubParentCustomerId).GetAwaiter().GetResult();
                if (accounts == null)
                {
                    return;
                }

                var account = accounts.FirstOrDefault();

                this.crmGroup.SubParentAccountId = account.accountId;
                this.crmGroup.SubParentAccountCode = account.accountCode;

                var queryVPNs = !string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo) ?
                    this.crmService.QueryVPNGroupSubscriberByGroupNo(this.crmGroup.SubParentGroupNo).GetAwaiter().GetResult() :
                    this.crmService.QueryVPNGroupSubscriberByCustomerId(this.crmGroup.SubParentCustomerId).GetAwaiter().GetResult();
                if (queryVPNs == null)
                {
                    return;
                }

                var queryVPN = queryVPNs.FirstOrDefault();

                this.crmGroup.SubParentGroupId = queryVPN.groupId;
                this.crmGroup.SubParentGroupNo = queryVPN.groupNumber;

                if (string.IsNullOrEmpty(this.crmGroup.SubParentGroupName))
                {
                    this.crmGroup.SubParentGroupName = queryVPN.groupName;
                }

                if (string.IsNullOrEmpty(this.crmGroup.GroupNo))
                {
                    this.crmGroup.CustomerId = result.corporationInfo.parentCustomerId;
                    SetParentCRMGroup(dbExecutor);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        protected string GetPaymentMode(string PaymentMode)
        {
            switch (PaymentMode.ToLower())
            {
                case "credit card":
                case "creditcard":
                case "credit_card":
                    return "2";
                    break;
                case "cash":
                default:
                    return "1";
                    break;
            }
        }

        protected Guid GetPaymentModeId(string PaymentMode)
        {
            switch (PaymentMode.ToLower())
            {
                case "credit card":
                case "creditcard":
                case "credit_card":
                case "2":
                    return Lookup.PaymentMode.CreditCard;
                    break;
                case "cash":
                case "1":
                default:
                    return Lookup.PaymentMode.Cash;
                    break;
            }
        }

        protected void SetPIC(List<PICInfoValue> picInfos)
        {
            if (picInfos.Count <= 0)
            {
                return;
            }

            var admin = picInfos.Where(item => item.picType == "0").ToList();
            var auth = picInfos.Where(item => item.picType == "1").ToList();

            var admin1 = admin.ElementAtOrDefault(0) ?? null;
            var admin2 = admin.ElementAtOrDefault(1) ?? null;
            var auth1 = auth.ElementAtOrDefault(0) ?? null;
            var auth2 = auth.ElementAtOrDefault(1) ?? null;

            if (this.crmGroup.Admin1 == null)
            {
                this.crmGroup.Admin1 = new PIC();
            }

            if (this.crmGroup.Admin2 == null)
            {
                this.crmGroup.Admin2 = new PIC();
            }

            if (this.crmGroup.Auth1 == null)
            {
                this.crmGroup.Auth1 = new PIC();
            }

            if (this.crmGroup.Auth2 == null)
            {
                this.crmGroup.Auth2 = new PIC();
            }

            if (admin1 != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.Admin1.Name))
                {
                    this.crmGroup.Admin1.Name = admin1.name;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin1.IdType))
                {
                    this.crmGroup.Admin1.IdType = admin1.idType;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin1.IdNo))
                {
                    this.crmGroup.Admin1.IdNo = admin1.idNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin1.MobileNo))
                {
                    this.crmGroup.Admin1.MobileNo = admin1.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin1.TelNo))
                {
                    this.crmGroup.Admin1.TelNo = admin1.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin1.Email))
                {
                    this.crmGroup.Admin1.Email = admin1.email;
                }

                if (this.crmGroup.Admin1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin1.IdType))
                {
                    this.crmGroup.Admin1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin1.IdType}
                    });
                }
            }

            if (admin2 != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.Admin2.Name))
                {
                    this.crmGroup.Admin2.Name = admin2.name;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin2.IdType))
                {
                    this.crmGroup.Admin2.IdType = admin2.idType;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin2.IdNo))
                {
                    this.crmGroup.Admin2.IdNo = admin2.idNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin2.MobileNo))
                {
                    this.crmGroup.Admin2.MobileNo = admin2.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin2.TelNo))
                {
                    this.crmGroup.Admin2.TelNo = admin2.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Admin2.Email))
                {
                    this.crmGroup.Admin2.Email = admin2.email;
                }

                if (this.crmGroup.Admin2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Admin2.IdType))
                {
                    this.crmGroup.Admin2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Admin2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Admin2.IdType}
                    });
                }
            }

            if (auth1 != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.Auth1.Name))
                {
                    this.crmGroup.Auth1.Name = auth1.name;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth1.IdType))
                {
                    this.crmGroup.Auth1.IdType = auth1.idType;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth1.IdNo))
                {
                    this.crmGroup.Auth1.IdNo = auth1.idNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth1.MobileNo))
                {
                    this.crmGroup.Auth1.MobileNo = auth1.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth1.TelNo))
                {
                    this.crmGroup.Auth1.TelNo = auth1.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth1.Email))
                {
                    this.crmGroup.Auth1.Email = auth1.email;
                }

                if (this.crmGroup.Auth1.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth1.IdType))
                {
                    this.crmGroup.Auth1.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth1.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth1.IdType}
                    });
                }
            }

            if (auth2 != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.Auth2.Name))
                {
                    this.crmGroup.Auth2.Name = auth2.name;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth2.IdType))
                {
                    this.crmGroup.Auth2.IdType = auth2.idType;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth2.IdNo))
                {
                    this.crmGroup.Auth2.IdNo = auth2.idNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth2.MobileNo))
                {
                    this.crmGroup.Auth2.MobileNo = auth2.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth2.TelNo))
                {
                    this.crmGroup.Auth2.TelNo = auth2.phoneNumber;
                }

                if (string.IsNullOrEmpty(this.crmGroup.Auth2.Email))
                {
                    this.crmGroup.Auth2.Email = auth2.email;
                }

                if (this.crmGroup.Auth2.IdTypeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.Auth2.IdType))
                {
                    this.crmGroup.Auth2.IdTypeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgIDType", new Dictionary<string, object>() {
                        {this.crmGroup.Auth2.IdType.Length == 1 ? "DgCode" : "Name", this.crmGroup.Auth2.IdType}
                    });
                }
            }
        }

        protected void SetAddress(List<CustomerAddressValue> customerAddressInfos)
        {
            if (customerAddressInfos.Count <= 0)
            {
                return;
            }

            var legalAddress = customerAddressInfos.Where(item => item.contactType == "0").FirstOrDefault();
            var deliveryAddress = customerAddressInfos.Where(item => item.contactType == "7").FirstOrDefault();
            var billAddress = customerAddressInfos.Where(item => item.contactType == "4" || item.contactType == "8").FirstOrDefault();

            if (this.crmGroup.LegalAddress == null)
            {
                this.crmGroup.LegalAddress = new Address();
            }

            if (this.crmGroup.BillAddress == null)
            {
                this.crmGroup.BillAddress = new Address();
            }

            if (this.crmGroup.DeliveryAddress == null)
            {
                this.crmGroup.DeliveryAddress = new Address();
            }

            if (legalAddress != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.LegalAddress.StreetAddress))
                {
                    this.crmGroup.LegalAddress.StreetAddress = legalAddress.address1;
                }

                if (string.IsNullOrEmpty(this.crmGroup.LegalAddress.PostCode))
                {
                    this.crmGroup.LegalAddress.PostCode = legalAddress.addressPostCode;
                }

                if (string.IsNullOrEmpty(this.crmGroup.LegalAddress.City))
                {
                    this.crmGroup.LegalAddress.City = legalAddress.addressCity;
                }

                if (string.IsNullOrEmpty(this.crmGroup.LegalAddress.State))
                {
                    this.crmGroup.LegalAddress.State = legalAddress.addressProvince;
                }

                if (string.IsNullOrEmpty(this.crmGroup.LegalAddress.Country))
                {
                    this.crmGroup.LegalAddress.Country = legalAddress.addressCountry;
                }

                if (this.crmGroup.LegalAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.PostCode))
                {
                    this.crmGroup.LegalAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.LegalAddress.PostCode}
                    });
                }

                if (this.crmGroup.LegalAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.City))
                {
                    this.crmGroup.LegalAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.City.Length == 4 && this.crmGroup.LegalAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.City}
                    });
                }

                if (this.crmGroup.LegalAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.State))
                {
                    this.crmGroup.LegalAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.State.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.LegalAddress.State}
                    });
                }

                if (this.crmGroup.LegalAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.LegalAddress.Country))
                {
                    this.crmGroup.LegalAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.LegalAddress.Country.Length == 4 && int.TryParse(this.crmGroup.LegalAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.LegalAddress.Country}
                    });
                }
            }

            if (billAddress != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.BillAddress.StreetAddress))
                {
                    this.crmGroup.BillAddress.StreetAddress = billAddress.address1;
                }

                if (string.IsNullOrEmpty(this.crmGroup.BillAddress.PostCode))
                {
                    this.crmGroup.BillAddress.PostCode = billAddress.addressPostCode;
                }

                if (string.IsNullOrEmpty(this.crmGroup.BillAddress.City))
                {
                    this.crmGroup.BillAddress.City = billAddress.addressCity;
                }

                if (string.IsNullOrEmpty(this.crmGroup.BillAddress.State))
                {
                    this.crmGroup.BillAddress.State = billAddress.addressProvince;
                }

                if (string.IsNullOrEmpty(this.crmGroup.BillAddress.Country))
                {
                    this.crmGroup.BillAddress.Country = billAddress.addressCountry;
                }

                if (this.crmGroup.BillAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.PostCode))
                {
                    this.crmGroup.BillAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.BillAddress.PostCode}
                    });
                }

                if (this.crmGroup.BillAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.City))
                {
                    this.crmGroup.BillAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.City.Length == 4 && this.crmGroup.BillAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.City}
                    });
                }

                if (this.crmGroup.BillAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.State))
                {
                    this.crmGroup.BillAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.State.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.BillAddress.State}
                    });
                }

                if (this.crmGroup.BillAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.BillAddress.Country))
                {
                    this.crmGroup.BillAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.BillAddress.Country.Length == 4 && int.TryParse(this.crmGroup.BillAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.BillAddress.Country}
                    });
                }
            }

            if (deliveryAddress != null)
            {
                if (string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.StreetAddress))
                {
                    this.crmGroup.DeliveryAddress.StreetAddress = deliveryAddress.address1;
                }

                if (string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.PostCode))
                {
                    this.crmGroup.DeliveryAddress.PostCode = deliveryAddress.addressPostCode;
                }

                if (string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.City))
                {
                    this.crmGroup.DeliveryAddress.City = deliveryAddress.addressCity;
                }

                if (string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.State))
                {
                    this.crmGroup.DeliveryAddress.State = deliveryAddress.addressProvince;
                }

                if (string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.Country))
                {
                    this.crmGroup.DeliveryAddress.Country = deliveryAddress.addressCountry;
                }

                if (this.crmGroup.DeliveryAddress.PostCodeId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.PostCode))
                {
                    this.crmGroup.DeliveryAddress.PostCodeId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgPostcode", new Dictionary<string, object>() {
                        {"Name", this.crmGroup.DeliveryAddress.PostCode}
                    });
                }

                if (this.crmGroup.DeliveryAddress.CityId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.City))
                {
                    this.crmGroup.DeliveryAddress.CityId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCity", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.City.Length == 4 && this.crmGroup.DeliveryAddress.City.StartsWith("c") ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.City}
                    });
                }

                if (this.crmGroup.DeliveryAddress.StateId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.State))
                {
                    this.crmGroup.DeliveryAddress.StateId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgState", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.State.StartsWith("MYS_") ? "DgCode" : "Name", this.crmGroup.DeliveryAddress.State}
                    });
                }

                if (this.crmGroup.DeliveryAddress.CountryId == Guid.Empty && !string.IsNullOrEmpty(this.crmGroup.DeliveryAddress.Country))
                {
                    this.crmGroup.DeliveryAddress.CountryId = ISAEntityHelper.EntityHelper.EntityHelper.GetEntityId(UserConnection, "DgCountry", new Dictionary<string, object>() {
                        {this.crmGroup.DeliveryAddress.Country.Length == 4 && int.TryParse(this.crmGroup.DeliveryAddress.Country, out _) ?
                            "DgCode" : "Name", this.crmGroup.DeliveryAddress.Country}
                    });
                }
            }
        }

        protected Guid GetRegionId(Guid SalespersonId)
        {
            if (SalespersonId == Guid.Empty)
            {
                return Guid.Empty;
            }

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSalesperson");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("RegionId", esq.AddColumn("DgRegion.Id"));

            var entity = esq.GetEntity(UserConnection, SalespersonId);
            return entity != null ?
                entity.GetTypedColumnValue<Guid>(columns["RegionId"].Name) : Guid.Empty;
        }

        protected Guid GetAreaId(Guid StateId)
        {
            if (StateId == Guid.Empty)
            {
                return Guid.Empty;
            }

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgState");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("AreaId", esq.AddColumn("DgArea.Id"));

            var entity = esq.GetEntity(UserConnection, StateId);
            return entity != null ?
                entity.GetTypedColumnValue<Guid>(columns["AreaId"].Name) : Guid.Empty;
        }

        protected Guid GetSalesChannel(DBExecutor dbExecutor, string SalesChannel)
        {
            var result = Guid.Empty;

            // get by code first
            var sql = $@"SELECT
				TOP 1 Id
			FROM DgChannel
			WHERE DgCode = '{SalesChannel}'";

            var query = new CustomQuery(UserConnection, sql);
            using (IDataReader dataReader = query.ExecuteReader(dbExecutor))
            {
                while (dataReader.Read())
                {
                    result = dataReader.GetColumnValue<Guid>("Id");
                }
            }

            if (result != Guid.Empty)
            {
                return result;
            }

            // get by name
            sql = $@"SELECT
				TOP 1 Id
			FROM DgChannel
			WHERE Name = '{SalesChannel}'";

            query = new CustomQuery(UserConnection, sql);
            using (IDataReader dataReader = query.ExecuteReader(dbExecutor))
            {
                while (dataReader.Read())
                {
                    result = dataReader.GetColumnValue<Guid>("Id");
                }
            }

            return result;
        }

        protected Guid GetSalesChannel(string SalesChannel)
        {
            var result = Guid.Empty;
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())
            {
                result = GetSalesChannel(dbExecutor, SalesChannel);
            }

            return result;
        }

        protected Guid GetDealerId()
        {
            if (string.IsNullOrEmpty(this.crmGroup.DealerCode))
            {
                return Guid.Empty;
            }

            return ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(
                UserConnection,
                "DgDealer",
                new Dictionary<string, object>() {
                    {"DgDealerID", this.crmGroup.DealerCode}
                }
            );
        }

        protected Guid GetSalespersonId()
        {
            if (string.IsNullOrEmpty(this.crmGroup.DealerCode))
            {
                return Guid.Empty;
            }

            return ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(
                UserConnection,
                "DgSalesperson",
                new Dictionary<string, object>() {
                    {"DgSalesCode", this.crmGroup.DealerCode},
                }
            );
        }

        protected Guid GetCompanyId()
        {
            if (string.IsNullOrEmpty(this.submission.CompanyName) || string.IsNullOrEmpty(this.crmGroup.BRN))
            {
                return Guid.Empty;
            }

            var filter = !string.IsNullOrEmpty(this.crmGroup.BRN) ?
                new Dictionary<string, object>() {
                    {"DgRegNo", this.crmGroup.BRN}
                } :
                new Dictionary<string, object>() {
                    {"DgName", this.submission.CompanyName}
                };
            return ISAEntityHelper.EntityHelper.EntityHelper.GetOrCreateEntity(
                UserConnection,
                "DgCompany",
                filter,
                new Dictionary<string, object>() {
                    {"DgName", this.submission.CompanyName},
                    {"DgRegNo", this.crmGroup.BRN},
                    {"DgRegionId", this.submission.RegionId},

                    {"DgPrimaryContactPerson", this.crmGroup.Admin1?.Name ?? string.Empty},
                    {"DgPrimaryContactNRIC", this.crmGroup.Admin1?.IdNo ?? string.Empty},
                    {"DgPrimaryContactMobtel", this.crmGroup.Admin1?.MobileNo ?? string.Empty},
                    {"DgPrimaryContactDesignation", this.crmGroup.Admin1?.Designation ?? string.Empty},
                    {"DgPrimaryContactOfficeTel", this.crmGroup.Admin1?.TelNo ?? string.Empty},
                    {"DgPrimaryContactEmail", this.crmGroup.Admin1?.Email ?? string.Empty},

                    {"DgSecondaryContactPerson", this.crmGroup.Admin2?.Name ?? string.Empty},
                    {"DgSecondaryContactNRIC", this.crmGroup.Admin2?.IdNo ?? string.Empty},
                    {"DgSecondaryContactMobtel", this.crmGroup.Admin2?.MobileNo ?? string.Empty},
                    {"DgSecondaryContactDesignation", this.crmGroup.Admin2?.Designation ?? string.Empty},
                    {"DgSecondaryContactOfficeTel", this.crmGroup.Admin2?.TelNo ?? string.Empty},

                    {"DgBillingStreetAddress", this.crmGroup.BillAddress?.StreetAddress ?? string.Empty},
                    {"DgBillingCity", this.crmGroup.BillAddress?.City ?? string.Empty},
                    {"DgBillingState", this.crmGroup.BillAddress?.State ?? string.Empty},
                    {"DgBillingPostalCode", this.crmGroup.BillAddress?.PostCode ?? string.Empty},
                    {"DgBillingCountry", this.crmGroup.BillAddress?.Country ?? string.Empty},

                    {"DgShippingStreetAddress", this.crmGroup.DeliveryAddress?.StreetAddress ?? string.Empty},
                    {"DgShippingCity", this.crmGroup.DeliveryAddress?.City ?? string.Empty},
                    {"DgShippingState", this.crmGroup.DeliveryAddress?.State ?? string.Empty},
                    {"DgShippingPostalCode", this.crmGroup.DeliveryAddress?.PostCode ?? string.Empty},
                    {"DgShippingCountry", this.crmGroup.DeliveryAddress?.Country ?? string.Empty},

                    {"DgAuthSignatoryName1", this.crmGroup.Auth1?.Name ?? string.Empty},
                    {"DgAuthSignatoryNRIC1", this.crmGroup.Auth1?.IdNo ?? string.Empty},
                    {"DgAuthSignatoryMobtel1", this.crmGroup.Auth1?.MobileNo ?? string.Empty},
                    {"DgAuthSignatoryDesignation1", this.crmGroup.Auth1?.Designation ?? string.Empty},
                    {"DgAuthSignatoryEmail1", this.crmGroup.Auth1?.Email ?? string.Empty}
                }
            );
        }

        protected Guid GetOfferId(string OfferID, string OfferName, Guid SubscriberTypeId)
        {
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgOffering");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("SubscriberTypeId", esq.AddColumn("DgSubscriberType.Id"));

            if (!string.IsNullOrEmpty(OfferID))
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgOfferID", OfferID));
            }

            if (!string.IsNullOrEmpty(OfferName))
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgOfferName", Regex.Replace(OfferName.ToString().Trim(), @"\s+", " ")));
            }

            var filterOfferID = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
            filterOfferID.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgOfferID", ""));
            filterOfferID.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgOfferID"));
            esq.Filters.Add(filterOfferID);

            var filterExpired = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.And);
            filterExpired.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNotNull, "DgExpiryDate"));
            filterExpired.Add(esq.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "DgExpiryDate", DateTime.UtcNow));
            esq.Filters.Add(filterExpired);

            var filterSubscriberType = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);
            filterSubscriberType.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "DgSubscriberType.Id", SubscriberTypeId));
            filterSubscriberType.Add(esq.CreateFilterWithParameters(FilterComparisonType.IsNull, "DgSubscriberType"));
            esq.Filters.Add(filterSubscriberType);

            Guid offerId = Guid.Empty;
            var entities = esq.GetEntityCollection(UserConnection);
            foreach (var entity in entities)
            {
                offerId = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
                Guid subscriberTypeId = entity.GetTypedColumnValue<Guid>(columns["SubscriberTypeId"].Name);
                if (subscriberTypeId != Guid.Empty)
                {
                    break;
                }
            }

            if (offerId == Guid.Empty)
            {
                throw new Exception($"Offer Name {OfferName} not found or expired");
            }

            return offerId;
        }

        protected virtual string GetParentBillMedium()
        {
            if (this.crmGroup.DefaultSMS && this.crmGroup.EmailBillWithPDF && !this.crmGroup.PaperChargeableStandard && !this.crmGroup.PaperChargeableItemised)
            {
                return "Email Bill with PDF";
            }
            else if (this.crmGroup.DefaultSMS && !this.crmGroup.EmailBillWithPDF && !this.crmGroup.PaperChargeableStandard && !this.crmGroup.PaperChargeableItemised)
            {
                return "Default (SMS)";
            }
            else if (this.crmGroup.DefaultSMS && this.crmGroup.EmailBillWithPDF && this.crmGroup.PaperChargeableStandard && !this.crmGroup.PaperChargeableItemised)
            {
                return "Paper Chargeable Standard";
            }
            else if (this.crmGroup.DefaultSMS && this.crmGroup.EmailBillWithPDF && !this.crmGroup.PaperChargeableStandard && this.crmGroup.PaperChargeableItemised)
            {
                return "Paper Chargeable Itemised";
            }
            else if (this.crmGroup.DefaultSMS && !this.crmGroup.EmailBillWithPDF && this.crmGroup.PaperChargeableStandard && !this.crmGroup.PaperChargeableItemised)
            {
                return "Paper Chargeable Standard";
            }
            else if (this.crmGroup.DefaultSMS && !this.crmGroup.EmailBillWithPDF && !this.crmGroup.PaperChargeableStandard && this.crmGroup.PaperChargeableItemised)
            {
                return "Paper Chargeable Itemized";
            }

            return "";
        }

        protected virtual string GetParentBillType()
        {
            switch (GetParentBillMedium())
            {
                case "Email Bill with PDF":
                    return "Email Bill";
                case "Default (SMS)":
                    return "NA";
                case "Paper Chargeable Standard":
                    return "Standard Bill only - Chargable Default";
                case "Paper Chargeable Itemized":
                    return "Standard Bill Chargable + Itemized Bill Chargable";
                default:
                    return string.Empty;
            }
        }

        protected virtual string GetParentBillCarrier()
        {
            switch (GetParentBillMedium())
            {
                case "Email Bill with PDF":
                    return "Email";
                case "Default (SMS)":
                    return "SMS Default";
                case "Paper Chargeable Standard":
                case "Paper Chargeable Itemized":
                    return "Paper";
                default:
                    return string.Empty;
            }
        }

        protected virtual string GetParentBillDetail()
        {
            switch (GetParentBillMedium())
            {
                case "Email Bill with PDF":
                    return "Summary";
                case "Default (SMS)":
                case "Paper Chargeable Itemized":
                    return "Itemised Billing";
                case "Paper Chargeable Standard":
                    return "Standard Billing";
                default:
                    return string.Empty;
            }
        }

        protected virtual string GetBillMedium()
        {
            switch (GetParentBillMedium())
            {
                case "Email Bill with PDF":
                    return "Email";
                case "Default (SMS)":
                    return "SMS";
                case "Paper Bill Chargeable Standard":
                case "Paper Bill Chargeable Itemized":
                    return "Paper Bill";
                default:
                    return "";
            }
            ;
        }

        protected virtual DateTime GetValidDateTime(string DateString, string Format = "d-M-yyyy")
        {
            if (string.IsNullOrEmpty(DateString))
            {
                return DateTime.MinValue;
            }

            bool isSuccess = true;
            var date = DateTime.UtcNow;
            try
            {
                DateTime dt = DateTime.ParseExact(DateString, Format, CultureInfo.InvariantCulture);
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            catch (Exception e)
            {
                isSuccess = false;
            }

            if (!isSuccess)
            {
                try
                {
                    double dateExcel = double.Parse(DateString);
                    DateTime dt = DateTime.FromOADate(dateExcel);
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
                catch (Exception e)
                {
                    throw new Exception($"{DateString}: {e.Message}");
                }
            }

            return date;
        }
    }

    public class SubmitRequest
    {
        public CRMGroup CRMGroup { get; set; }
        public Submission Submission { get; set; }
        public List<LineDetail> LineDetails { get; set; }
    }

    public class SubmitResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string SerialNumber { get; set; }
    }

    public class CRMGroup
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public string GroupNo { get; set; }
        public string GroupId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public string CorpNumber { get; set; }
        public string AccountId { get; set; }
        public string AccountCode { get; set; }
        public string SubParentGroupName { get; set; }
        public string SubParentGroupNo { get; set; }
        public string SubParentGroupId { get; set; }
        public string SubParentCustomerId { get; set; }
        public string SubParentCustomerCode { get; set; }
        public string SubParentCorpNumber { get; set; }
        public string SubParentAccountId { get; set; }
        public string SubParentAccountCode { get; set; }
        public string PaymentId { get; set; }
        public string SubscriberId { get; set; }
        public string BRN { get; set; }
        public DateTime BRNExpiryDate { get; set; }
        public string CompanyIncorparation { get; set; }
        public Guid CompanyIncorparationId { get; set; }
        public DateTime DateIncorparation { get; set; }
        public string ExistingCustomerMobile { get; set; }
        public string DNO { get; set; }
        public Guid DNOId { get; set; }
        public string Others { get; set; }
        public string DNOIdType { get; set; }
        public Guid DNOIdTypeId { get; set; }
        public string DNOIdNo { get; set; }
        public string DNOCompanyName { get; set; }
        public string DNOAccountNo { get; set; }
        public Address LegalAddress { get; set; }
        public Address BillAddress { get; set; }
        public Address DeliveryAddress { get; set; }
        public PIC Admin1 { get; set; }
        public PIC Admin2 { get; set; }
        public PIC Auth1 { get; set; }
        public PIC Auth2 { get; set; }
        public string TelNo { get; set; }
        public string IndustrialSegment { get; set; }
        public Guid IndustrialSegmentId { get; set; }
        public string EnterpriseCustomerType { get; set; }
        public Guid EnterpriseCustomerTypeId { get; set; }
        public string AccountValue { get; set; }
        public bool DefaultSMS { get; set; }
        public bool PaperChargeableStandard { get; set; }
        public bool PaperChargeableItemised { get; set; }
        public bool EmailBillWithPDF { get; set; }
        public bool AutoBilling { get; set; }
        public bool IsDevicePrice { get; set; }
        public decimal DevicePriceAmount { get; set; }
        public bool IsAdvancePaymentDeposit { get; set; }
        public decimal AdvancePaymentAmount { get; set; }
        public string BillMedium { get; set; }
        public Guid BillMediumId { get; set; }
        public string BillType { get; set; }
        public string BillCarrier { get; set; }
        public string BillDetail { get; set; }
        public string SalesChannel { get; set; }
        public Guid SalesChannelId { get; set; }
        public string DealerName { get; set; }
        public string DealerCode { get; set; }
        public Guid DealerId { get; set; }
        public string PaymentMode { get; set; }
        public Guid PaymentModeId { get; set; }
        public Guid PaymentMethodId { get; set; }
        public string BillingEmailAddress { get; set; }
        public string PaidUpCapital { get; set; }
        public string SalesTurnover { get; set; }
        public string NoOfEmployees { get; set; }
        public string NatureOfBusiness { get; set; }
        public string TelcoProviders { get; set; }
        public string SOW { get; set; }
        public Guid GeographicalSpreadId { get; set; }
        public string Language { get; set; }
        public string EnterpriseGroupType { get; set; }
        public string ACPaymentResponsible { get; set; }
        public string GroupLevel { get; set; }
        public string GroupTariff { get; set; }
        public string BillingCycle { get; set; }
        public Guid PrimaryOfferId { get; set; }
        public Guid SuppOffer1Id { get; set; }
        public Guid SuppOffer2Id { get; set; }
        public string TINNumber { get; set; }
        public string SSTNumber { get; set; }
    }

    public class Address
    {
        public string StreetAddress { get; set; }
        public string PostCode { get; set; }
        public Guid PostCodeId { get; set; }
        public string City { get; set; }
        public Guid CityId { get; set; }
        public string State { get; set; }
        public Guid StateId { get; set; }
        public string Country { get; set; }
        public Guid CountryId { get; set; }
    }

    public class PIC
    {
        public string Name { get; set; }
        public string IdType { get; set; }
        public Guid IdTypeId { get; set; }
        public string IdNo { get; set; }
        public string MobileNo { get; set; }
        public string TelNo { get; set; }
        public string Email { get; set; }
        public string Designation { get; set; }
    }

    public class Submission
    {
        public Guid Id { get; set; }
        public string SerialNumber { get; set; }
        public bool FastLane { get; set; }
        public Guid SourceId { get; set; }
        public string SubmissionType { get; set; }
        public Guid SubmissionTypeId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public string AccountId { get; set; }
        public string AccountCode { get; set; }
        public string SubscriberType { get; set; }
        public Guid SubscriberTypeId { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CustomerName { get; set; }
        public string CMSId { get; set; }
        public string CardType { get; set; }
        public Guid CardTypeId { get; set; }
        public DateTime CardExpiredDate { get; set; }
        public string CardOwnerName { get; set; }
        public string BankIssuer { get; set; }
        public Guid BankIssuerId { get; set; }
        public string Last4DigitCardNumber { get; set; }
        public string PlainCardNumber { get; set; }
        public Guid SalespersonId { get; set; }
        public string SubmissionRemark { get; set; }
        public decimal TotalDevicePrice { get; set; }
        public decimal TotalCreditExposure { get; set; }
        public string IdNo { get; set; }
        public string IdType { get; set; }
        public Guid IdTypeId { get; set; }
        public string Title { get; set; }
        public Guid TitleId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public Guid GenderId { get; set; }
        public string Nationality { get; set; }
        public string Region { get; set; }
        public Guid RegionId { get; set; }
        public string Area { get; set; }
        public Guid AreaId { get; set; }
        public string ReferenceContactName { get; set; }
        public string ReferenceContactTelNo { get; set; }
        public string SFAId { get; set; }
        public int ResubmissionNumber { get; set; }
        public Guid SubmissionStatusId { get; set; }
        public string ApplicantName { get; set; }
        public Guid PaymentSubmittedId { get; set; }
        public DateTime SignUpDate { get; set; }
        public DateTime ReceiveDate { get; set; }
        public DateTime ResubmissionDate { get; set; }
    }

    public class LineDetail
    {
        public Guid Id { get; set; }
        public int No { get; set; }
        public int LineId { get; set; }
        public string Username { get; set; }
        public string MSISDN { get; set; }
        public string RatePlan { get; set; } // primary offering
        public List<SuppOffer> SuppOffers { get; set; } // only use from sfa
        public string AdvancePayment { get; set; }
        public string Vas { get; set; }
        public string Tos { get; set; }
        public string Contract { get; set; }
        public string PrMode { get; set; }
        public decimal CreditLimit { get; set; }
        public bool AutoBilling { get; set; }
        public string PhoneModel { get; set; }
        public string Remark { get; set; }
        public string DeviceBundleType { get; set; }
        public string P2P { get; set; }
        public string BillMedium { get; set; }
        public string Imsi { get; set; }
        public string ImsiPackageCode { get; set; }
        public string SimCardSerialNo { get; set; }
        public bool ConceptPaper { get; set; }
        public string Bundle1 { get; set; }
        public string Element1 { get; set; }
        public string Bundle2 { get; set; }
        public string Element2 { get; set; }
        public string Bundle3 { get; set; }
        public string Element3_1 { get; set; }
        public string Element3_2 { get; set; }
        public string Element3_3 { get; set; }
        public bool GoDigiPro { get; set; }
        public string RemoveIDD { get; set; }
        public string PromoCode { get; set; }
        public decimal DigiSellingPrice { get; set; }
        public decimal MonthlyRental { get; set; }
        public decimal CrExposure { get; set; }
        public string MandatoryOffer1 { get; set; }
        public string MandatoryOffer2 { get; set; }
        public string MandatoryOffer3 { get; set; }
        public string DataBundle { get; set; }
        public string DataElement { get; set; }
        public string AdvBundle { get; set; }
        public string ContractBundle { get; set; }
        public string ContractElement { get; set; }
        public string Automatic10 { get; set; }
        public string Automatic11 { get; set; }
        public string CpBundle1 { get; set; }
        public string CpElement1 { get; set; }
        public string CpBunlde2 { get; set; }
        public string CpElement2 { get; set; }
        public string CpBundle3 { get; set; }
        public string CpElement31 { get; set; }
        public string CpElement32 { get; set; }
        public string CpElement3_3 { get; set; }
        public string BufferSim { get; set; }
        public string CustomerLevel { get; set; }
        public string DNO { get; set; }
        public Guid DNOId { get; set; }
        public string DNOIdType { get; set; }
        public Guid DNOIdTypeId { get; set; }
        public string DNOIdNo { get; set; }
        public string DNOCompanyName { get; set; }
        public string DNOAccountNo { get; set; }
    }

    public class SuppOffer
    {
        public string OfferId { get; set; }
        public string OfferName { get; set; }
    }
}