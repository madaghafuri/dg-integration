using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Lookup = DgMasterData.DgLookupConst;
using ISAEntityHelper.EntityHelper;

namespace DgSubmission.DgSubmissionService
{
    public class EIRAService : ECRAService
    {
        public EIRAService(UserConnection UserConnection, string ImportId) : base(UserConnection, ImportId)
        {}

        protected override void GetCRMGroup()
        {
            string sheetName = "eIRA";
            SetSheet(sheetName);

            this.crmGroup = new CRMGroup();

            this.crmGroup.GroupName = GetCellValue<string>(sheetName, "A23");
            this.crmGroup.SubParentGroupNo = GetCellValue<string>(sheetName, "Z55");
            this.crmGroup.SubParentGroupName = GetCellValue<string>(sheetName, "Z54");
            this.crmGroup.BRN = GetCellValue<string>(sheetName, "H24");
            
            this.crmGroup.DNO = GetCellValue<string>(sheetName, "C8");
            this.crmGroup.Others = GetCellValue<string>(sheetName, "P8");
            this.crmGroup.DNOIdType = GetCellValue<string>(sheetName, "E9");
            this.crmGroup.DNOIdNo = GetCellValue<string>(sheetName, "E10");
            this.crmGroup.DNOCompanyName = GetCellValue<string>(sheetName, "H11");
            this.crmGroup.DNOAccountNo = GetCellValue<string>(sheetName, "H12");

            string[] legalAddressArr = {
                GetCellValue<string>(sheetName, "A28"), 
                GetCellValue<string>(sheetName, "A29")
            };
            string legalAddress = legalAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", legalAddressArr) : legalAddressArr[0];
            this.crmGroup.LegalAddress = new Address() {
                StreetAddress = legalAddress,
                City = GetCellValue<string>(sheetName, "O29"),
                State = GetCellValue<string>(sheetName, "C30"),
                Country = GetCellValue<string>(sheetName, "C31"),
                PostCode = GetCellValue<string>(sheetName, "O30")
            };

            this.crmGroup.BillAddress = new Address() {
                StreetAddress = this.crmGroup.LegalAddress.StreetAddress,
                City = this.crmGroup.LegalAddress.City,
                State = this.crmGroup.LegalAddress.State,
                Country = this.crmGroup.LegalAddress.Country,
                PostCode = this.crmGroup.LegalAddress.PostCode
            };

            string[] deliveryAddressArr = {
                GetCellValue<string>(sheetName, "A34"), 
                GetCellValue<string>(sheetName, "A35")
            };
            string deliveryAddress = deliveryAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", deliveryAddressArr) : deliveryAddressArr[0];
            this.crmGroup.DeliveryAddress = new Address() {
                StreetAddress = deliveryAddress,
                City = GetCellValue<string>(sheetName, "O35"),
                State = GetCellValue<string>(sheetName, "C36"),
                Country = GetCellValue<string>(sheetName, "C37"),
                PostCode = GetCellValue<string>(sheetName, "O36")
            };

            this.crmGroup.Admin1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "A17"),
                IdType = GetCellValue<string>(sheetName, "C18"),
                IdNo = GetCellValue<string>(sheetName, "J18"),
                MobileNo = GetCellValue<string>(sheetName, "J21"),
                Email = GetCellValue<string>(sheetName, "J20")
            };
            this.crmGroup.Admin1.TelNo = this.crmGroup.Admin1.MobileNo;

            this.crmGroup.Auth1 = new PIC() {
                Name = this.crmGroup.Admin1.Name,
                IdType = this.crmGroup.Admin1.IdType,
                IdNo = this.crmGroup.Admin1.IdNo,
                MobileNo = this.crmGroup.Admin1.MobileNo,
                TelNo = this.crmGroup.Admin1.TelNo,
                Email = this.crmGroup.Admin1.Email,
                Designation = this.crmGroup.Admin1.Designation
            };
        
            this.crmGroup.TelNo = this.crmGroup.Admin1.MobileNo;
            this.crmGroup.AutoBilling = GetCheckboxValue("Check Box 15");
            this.crmGroup.IsDevicePrice = GetCheckboxValue("Check Box 16");
            this.crmGroup.DevicePriceAmount = this.crmGroup.IsDevicePrice ? GetCellValue<decimal>(sheetName, "L47") : 0;
            this.crmGroup.IsAdvancePaymentDeposit = GetCheckboxValue("Check Box 17");
            this.crmGroup.AdvancePaymentAmount = this.crmGroup.IsAdvancePaymentDeposit ? GetCellValue<decimal>(sheetName, "L48") : 0;
            this.crmGroup.BillingEmailAddress = this.crmGroup.Admin1.Email;
            this.crmGroup.Language = "English";

            this.crmGroup.PrimaryOfferId = Guid.Parse("e2ea4aae-e3a5-41c8-aa39-d2e2672e57fb");
            this.crmGroup.SuppOffer1Id = Guid.Parse("0242fc0e-d5ca-4e79-9246-b497b1f3817c");
            this.crmGroup.SuppOffer2Id = Guid.Parse("506619aa-630d-4519-b0be-6a57ef0930a1");

            this.crmGroup.SalesChannel = GetCellValue<string>(sheetName, "AA57");
            this.crmGroup.DealerName = GetCellValue<string>(sheetName, "AA59");
            this.crmGroup.DealerCode = GetCellValue<string>(sheetName, "AA60");
        }
        
        protected override void GetSubmission()
        {
            string sheetName = "eIRA";
            SetSheet(sheetName);

            this.submission = new Submission();
            this.submission.SourceId = Lookup.Source.EIRA;
            this.submission.SubmissionType = GetCellValue<string>(sheetName, "H7");
            this.submission.SubscriberType = "CI";
            this.submission.CompanyName = this.crmGroup.GroupName;
            this.submission.CustomerName = this.crmGroup.Admin1.Name;
            this.submission.CMSId = GetCellValue<string>(sheetName, "AC56");

            string[] last4DigitCardNumber = {
                GetCellValue<string>(sheetName, "H50"), 
                GetCellValue<string>(sheetName, "J50"),
                GetCellValue<string>(sheetName, "L50"),
                GetCellValue<string>(sheetName, "N50")
            };
            this.submission.Last4DigitCardNumber = last4DigitCardNumber
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("", last4DigitCardNumber) : null;

            this.submission.IdNo = this.crmGroup.Admin1.IdNo;
            this.submission.IdType = this.crmGroup.Admin1.IdType;
            this.submission.Title = GetCellValue<string>(sheetName, "B15");
            this.submission.DateOfBirth = GetValidDateTime(GetCellValue<string>(sheetName, "J19"), "dd-MMM-yyyy");
            this.submission.Gender = GetCellValue<string>(sheetName, "H15");
            this.submission.Nationality = GetCellValue<string>(sheetName, "O15"); // ini blm ada kolomnya di submission
            this.submission.Region = GetCellValue<string>(sheetName, "Z58");
			this.submission.ReferenceContactName = GetCellValue<string>(sheetName, "D40");
			this.submission.ReferenceContactTelNo = GetCellValue<string>(sheetName, "D41");
			this.submission.ApplicantName = this.submission.CustomerName;

            sheetName = "Line Info";
            SetSheet(sheetName);
            this.submission.SubmissionRemark = GetCellValue<string>(sheetName, "L31");
			
			try {
				this.submission.TotalDevicePrice = GetCellValue<decimal>(sheetName, "CM30");
			} catch(Exception e) {
				throw new Exception($"Value in Sheet '{sheetName}' column 'CM30': Total Device Price is invalid. {e.Message}");
			}
        }

        protected override void GetLineDetail()
        {
            if(this.submission == null) {
                throw new Exception("This method can only be called if the Submission property is already defined");
            }

            string sheetName = "Line Info";
            SetSheet(sheetName);

            this.lineDetail = new List<LineDetail>();
            int startRow = 5;
            int startCheckbox = 29;

            for (int i = 0; i < 10; i++) {                
                var line = new LineDetail();

                if(i == 0) {
                    this.submission.SubscriberType = GetCellValue<string>(sheetName, $"BO{startRow}");
                    this.submission.CardType = GetCellValue<string>(sheetName, $"BK{startRow}");
                    this.submission.CardOwnerName = GetCellValue<string>(sheetName, $"BL{startRow}");
                    this.submission.BankIssuer = GetCellValue<string>(sheetName, $"BM{startRow}");
                    this.crmGroup.BillMedium = GetCellValue<string>(sheetName, $"BW{startRow}");
                    this.crmGroup.BillMediumId = this.crmGroup.BillMediumId = EntityHelper.GetOrCreateEntity(UserConnection, "DgBillMediumName", new Dictionary<string, object>() {
                        {"Name", GetParentBillMedium()}
                    });
                    this.crmGroup.BillType = GetParentBillType();
                    this.crmGroup.BillCarrier = GetParentBillCarrier();
                    this.crmGroup.BillDetail = GetParentBillDetail();

                    this.crmGroup.PaymentMode = GetPaymentMode(GetCellValue<string>(sheetName, $"BJ{startRow}"));

                    string cardExpiredDateString = GetCellValue<string>(sheetName, $"BN{startRow}");
                    try {
                        if(!string.IsNullOrEmpty(cardExpiredDateString)) {
                            DateTime expTemp = GetValidDateTime("01/"+cardExpiredDateString, "dd/MM/yyyy");
                            this.submission.CardExpiredDate = new DateTime(expTemp.Year, expTemp.Month, DateTime.DaysInMonth(expTemp.Year, expTemp.Month));
                        }
                    } catch (Exception e) {
                        throw new Exception($"Value in Sheet '{sheetName}' column 'BN{startRow}': Card Expired Date is invalid ({cardExpiredDateString}): "+e.Message);
                    }
                }

                string msisdn = GetCellValue<string>(sheetName, $"D{startRow}");
                if(string.IsNullOrEmpty(msisdn)) {
                    startRow++;
                    startCheckbox++;
                    continue;
                }

                line.No = GetCellValue<int>(sheetName, $"A{startRow}");
                line.Username = GetCellValue<string>(sheetName, $"C{startRow}");
                line.MSISDN = msisdn;
                line.RatePlan = GetCellValue<string>(sheetName, $"E{startRow}");
                line.AdvancePayment = GetCellValue<string>(sheetName, $"L{startRow}");
                line.Vas = GetCellValue<string>(sheetName, $"Q{startRow}");
                line.Contract = GetCellValue<string>(sheetName, $"AZ{startRow}");

                string autoBillingString = GetCellValue<string>(sheetName, $"BC{startRow}");
                line.AutoBilling = string.IsNullOrEmpty(autoBillingString) ? false : autoBillingString.ToLower() == "yes" ? true : false;

                line.SimCardSerialNo = GetCellValue<string>(sheetName, $"BE{startRow}");
                line.PhoneModel = GetCellValue<string>(sheetName, $"BF{startRow}");
                line.DeviceBundleType = GetCellValue<string>(sheetName, $"BU{startRow}");
                line.Tos = GetCellValue<string>(sheetName, $"BV{startRow}");
                line.BillMedium = GetCellValue<string>(sheetName, $"BW{startRow}");

                string conceptPaperString = GetCellValue<string>(sheetName, $"BY{startRow}");
                line.ConceptPaper = string.IsNullOrEmpty(conceptPaperString) ? false : conceptPaperString.ToLower() == "yes" ? true : false;

                line.Bundle1 = GetCellValue<string>(sheetName, $"CA{startRow}");
                line.Element1 = GetCellValue<string>(sheetName, $"CB{startRow}");
                line.Bundle2 = GetCellValue<string>(sheetName, $"CC{startRow}");
                line.Element2 = GetCellValue<string>(sheetName, $"CD{startRow}");
                line.Bundle3 = GetCellValue<string>(sheetName, $"CE{startRow}");
                line.Element3_1 = GetCellValue<string>(sheetName, $"CF{startRow}");
                line.Element3_2 = GetCellValue<string>(sheetName, $"CG{startRow}");
                line.Element3_3 = GetCellValue<string>(sheetName, $"CH{startRow}");
                line.Remark = GetCellValue<string>(sheetName, $"CI{startRow}");
                line.GoDigiPro = GetCheckboxValue($"Check Box {startCheckbox}");
                line.RemoveIDD = GetCellValue<string>(sheetName, $"CK{startRow}");

                line.PromoCode = GetCellValue<string>(sheetName, $"CL{startRow}");
				
				try {
					line.DigiSellingPrice = GetCellValue<decimal>(sheetName, $"CM{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'CM{startRow}': Digi Selling Price is invalid. {e.Message}");
				}
				
				try {
					line.MonthlyRental = GetCellValue<decimal>(sheetName, $"CN{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'CN{startRow}': Monthly Rental is invalid. {e.Message}");
				}

                line.MandatoryOffer1 = GetCellValue<string>(sheetName, $"F{startRow}");
                line.MandatoryOffer2 = GetCellValue<string>(sheetName, $"G{startRow}");
                line.MandatoryOffer3 = GetCellValue<string>(sheetName, $"H{startRow}");
                line.DataBundle = GetCellValue<string>(sheetName, $"I{startRow}");
                line.DataElement = GetCellValue<string>(sheetName, $"J{startRow}");
                line.AdvBundle = GetCellValue<string>(sheetName, $"K{startRow}");
                line.ContractBundle = GetCellValue<string>(sheetName, $"M{startRow}");
                line.ContractElement = GetCellValue<string>(sheetName, $"N{startRow}");
                line.Automatic10 = GetCellValue<string>(sheetName, $"O{startRow}");
                line.Automatic11 = GetCellValue<string>(sheetName, $"P{startRow}");
                line.CpBundle1 = GetCellValue<string>(sheetName, $"R{startRow}");
                line.CpElement1 = GetCellValue<string>(sheetName, $"S{startRow}");
                line.CpBunlde2 = GetCellValue<string>(sheetName, $"T{startRow}");
                line.CpElement2 = GetCellValue<string>(sheetName, $"U{startRow}");
                line.CpBundle3 = GetCellValue<string>(sheetName, $"V{startRow}");
                line.CpElement31 = GetCellValue<string>(sheetName, $"W{startRow}");
                line.CpElement32 = GetCellValue<string>(sheetName, $"X{startRow}");
                line.CpElement3_3 = GetCellValue<string>(sheetName, $"Y{startRow}");

                line.PrMode = GetCellValue<string>(sheetName, $"BA{startRow}");
				
				try {
					line.CreditLimit = GetCellValue<decimal>(sheetName, $"BB{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'BB{startRow}': Credit Limit is invalid. {e.Message}");
				}
				
				line.Imsi = GetCellValue<string>(sheetName, $"BG{startRow}");
                line.ImsiPackageCode = GetCellValue<string>(sheetName, $"BH{startRow}");
                line.BufferSim = GetCellValue<string>(sheetName, $"BI{startRow}");
                line.CustomerLevel = GetCellValue<string>(sheetName, $"BP{startRow}");

                startCheckbox++;
                startRow++;

                this.lineDetail.Add(line);
            }
        }
		
		protected override void CRMGroupSave(bool isUpdate = false)
		{
			this.crmGroup.Admin2 = null;
			this.crmGroup.Auth2 = null;
			
			base.CRMGroupSave(isUpdate);
		}

        protected override string GetParentBillMedium()
        {
            switch (this.crmGroup.BillMedium) {
                case "SMS":
                    return "Default (SMS)";
                case "Email":
                    return "Email bill without PDF";
                default:
                    break;
            }

            return this.crmGroup.BillMedium;
        }
    }
}