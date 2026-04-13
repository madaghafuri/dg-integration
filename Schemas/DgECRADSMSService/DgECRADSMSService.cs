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
    public class ECRADSMSService : ECRAService
    {
        public ECRADSMSService(UserConnection UserConnection, string ImportId) : base(UserConnection, ImportId)
        {}

        protected override void GetCRMGroup()
        {
            string sheetName = "ECRA Pg1";
            SetSheet(sheetName);

            this.crmGroup = new CRMGroup();

            this.crmGroup.GroupName = GetCellValue<string>(sheetName, "A19");
            this.crmGroup.GroupNo = GetCellValue<string>(sheetName, "AA55");
            this.crmGroup.BRN = GetCellValue<string>(sheetName, "A21");

            // throw new Exception(JsonConvert.SerializeObject(new {
            //     crmGroup = this.crmGroup
            // }));

            string[] expiryBRNList = {
                GetCellValue<string>(sheetName, "L21"), 
                GetCellValue<string>(sheetName, "N21"), 
                GetCellValue<string>(sheetName, "P21")
            };
            string expiryBRNString = expiryBRNList
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("-", expiryBRNList) : null;

            try {
                if(!string.IsNullOrEmpty(expiryBRNString)) {
                    this.crmGroup.BRNExpiryDate = GetValidDateTime(expiryBRNString);
                }   
            } catch (Exception e) {
                throw new Exception($"Expiry BRN is invalid ({expiryBRNString})): "+e.Message);
            }

            string[] dateIncorparationList = {
                GetCellValue<string>(sheetName, "J29"), 
                GetCellValue<string>(sheetName, "L29"), 
                GetCellValue<string>(sheetName, "N29")
            };
            string dateIncorparationString = dateIncorparationList
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("-", dateIncorparationList) : null;

            try {
                if(!string.IsNullOrEmpty(dateIncorparationString)) {
                    this.crmGroup.DateIncorparation = GetValidDateTime(dateIncorparationString);
                }
            } catch (Exception e) {
                throw new Exception($"Date Incorparation is invalid ({dateIncorparationString})): "+e.Message);
            }
            
            this.crmGroup.CompanyIncorparation = GetCellValue<string>(sheetName, "L8");
            this.crmGroup.ExistingCustomerMobile = GetCellValue<string>(sheetName, "H9");
            this.crmGroup.DNO = GetCellValue<string>(sheetName, "D11");
            this.crmGroup.Others = GetCellValue<string>(sheetName, "P11");
            this.crmGroup.DNOIdType = GetCellValue<string>(sheetName, "H12");
            this.crmGroup.DNOIdNo = GetCellValue<string>(sheetName, "H13");
            this.crmGroup.DNOCompanyName = GetCellValue<string>(sheetName, "H14");
            this.crmGroup.DNOAccountNo = GetCellValue<string>(sheetName, "H15");

            string[] legalAddressArr = {
                GetCellValue<string>(sheetName, "A23"), 
                GetCellValue<string>(sheetName, "A24")
            };
            string legalAddress = legalAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", legalAddressArr) : legalAddressArr[0];
            this.crmGroup.LegalAddress = new Address() {
                StreetAddress = legalAddress,
                City = GetCellValue<string>(sheetName, "D25"),
                State = GetCellValue<string>(sheetName, "D26"),
                Country = GetCellValue<string>(sheetName, "D27"),
                PostCode = GetCellValue<string>(sheetName, "O25")
            };


            string[] billAddressArr = {
                GetCellValue<string>(sheetName, "A35"), 
                GetCellValue<string>(sheetName, "A36")
            };
            string billAddress = billAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", billAddressArr) : billAddressArr[0];
            this.crmGroup.BillAddress = new Address() {
                StreetAddress = billAddress,
                City = GetCellValue<string>(sheetName, "O36"),
                State = GetCellValue<string>(sheetName, "C37"),
                Country = GetCellValue<string>(sheetName, "C38"),
                PostCode = GetCellValue<string>(sheetName, "O37")
            };

            string[] deliveryAddressArr = {
                GetCellValue<string>(sheetName, "A35"), 
                GetCellValue<string>(sheetName, "A36")
            };
            string deliveryAddress = deliveryAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", deliveryAddressArr) : deliveryAddressArr[0];
            this.crmGroup.DeliveryAddress = new Address() {
                StreetAddress = deliveryAddress,
                City = GetCellValue<string>(sheetName, "O42"),
                State = GetCellValue<string>(sheetName, "C43"),
                Country = GetCellValue<string>(sheetName, "C44"),
                PostCode = GetCellValue<string>(sheetName, "O43")
            };

            this.crmGroup.Admin1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "D44"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "D45"),
                MobileNo = GetCellValue<string>(sheetName, "D46"),
                Designation = GetCellValue<string>(sheetName, "D47"),
                TelNo = GetCellValue<string>(sheetName, "D48")
            };

            this.crmGroup.Admin2 = new PIC() {
                Name = GetCellValue<string>(sheetName, "D53"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "D54"),
                MobileNo = GetCellValue<string>(sheetName, "D55"),
                Designation = GetCellValue<string>(sheetName, "D56"),
                TelNo = GetCellValue<string>(sheetName, "D57")
            };

            this.crmGroup.Auth1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "U42"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "AA43"),
                MobileNo = GetCellValue<string>(sheetName, "AA44"),
                Email = GetCellValue<string>(sheetName, "AA46"),
                Designation = GetCellValue<string>(sheetName, "AA45")
            };

            this.crmGroup.Auth1.TelNo = this.crmGroup.Auth1.MobileNo;

            if(GetCheckboxValue("Check Box 6")) {
                this.crmGroup.Auth2 = new PIC() {
                    Name = this.crmGroup.Auth1.Name,
                    IdType = this.crmGroup.Auth1.IdType,
                    IdNo = this.crmGroup.Auth1.IdNo,
                    MobileNo = this.crmGroup.Auth1.MobileNo,
                    Email = this.crmGroup.Auth1.Email,
                    Designation = this.crmGroup.Auth1.Designation
                };
            }
        
            this.crmGroup.TelNo = GetCellValue<string>(sheetName, "D28");
            this.crmGroup.IndustrialSegment = GetCellValue<string>(sheetName, "J30");
            this.crmGroup.AccountValue = GetCellValue<string>(sheetName, "AA59");
            this.crmGroup.DefaultSMS = GetCheckboxValue("Check Box 47");
            this.crmGroup.PaperChargeableStandard = GetCheckboxValue("Check Box 48");
            this.crmGroup.PaperChargeableItemised = GetCheckboxValue("Check Box 49");
            this.crmGroup.EmailBillWithPDF = GetCheckboxValue("Check Box 50");
            this.crmGroup.AutoBilling = GetCheckboxValue("Check Box 45");
            this.crmGroup.IsDevicePrice = GetCheckboxValue("Check Box 46");
            this.crmGroup.DevicePriceAmount = this.crmGroup.IsDevicePrice ? GetCellValue<decimal>(sheetName, "AF12") : 0;
            this.crmGroup.IsAdvancePaymentDeposit = GetCheckboxValue("Check Box 51");
            this.crmGroup.AdvancePaymentAmount = this.crmGroup.IsAdvancePaymentDeposit ? GetCellValue<decimal>(sheetName, "AF13") : 0;
            this.crmGroup.BillMedium = GetParentBillMedium();
            this.crmGroup.BillType = GetParentBillType();
            this.crmGroup.BillCarrier = GetParentBillCarrier();
            this.crmGroup.BillDetail = GetParentBillDetail();
            this.crmGroup.Language = "English";

            this.crmGroup.PrimaryOfferId = Guid.Parse("e2ea4aae-e3a5-41c8-aa39-d2e2672e57fb");
            this.crmGroup.SuppOffer1Id = Guid.Parse("0242fc0e-d5ca-4e79-9246-b497b1f3817c");
            this.crmGroup.SuppOffer2Id = Guid.Parse("506619aa-630d-4519-b0be-6a57ef0930a1");

            sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);
            this.crmGroup.SalesChannel = GetCellValue<string>(sheetName, "H34");
            this.crmGroup.DealerName = GetCellValue<string>(sheetName, "H35");
            this.crmGroup.DealerCode = GetCellValue<string>(sheetName, "H36");
        }
        
        protected override void GetSubmission()
        {
            string sheetName = "ECRA Pg1";
            SetSheet(sheetName);

            this.submission = new Submission();
            this.submission.SourceId = Lookup.Source.ECRA;
            this.submission.SubmissionType = GetCellValue<string>(sheetName, "H10");
            this.submission.SubscriberType = "Corporate";
            this.submission.CompanyName = this.crmGroup?.GroupName;
            this.submission.CustomerName = this.crmGroup.Admin1.Name;
            this.submission.CMSId = GetCellValue<string>(sheetName, "AA57");

            string[] last4DigitCardNumber = {
                GetCellValue<string>(sheetName, "AB15"), 
                GetCellValue<string>(sheetName, "AD15"),
                GetCellValue<string>(sheetName, "AF15"),
                GetCellValue<string>(sheetName, "AH15")
            };
            this.submission.Last4DigitCardNumber = last4DigitCardNumber
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("", last4DigitCardNumber) : null;

            this.submission.IdNo = this.crmGroup.BRN;
            this.submission.IdType = "BRN";
            this.submission.Title = "Mr";
            this.submission.DateOfBirth = GetValidDateTime("3-6-1956");
            this.submission.Gender = "Male";

            sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);
            this.submission.SubmissionRemark = GetCellValue<string>(sheetName, "K31");
        }

        protected override void GetLineDetail()
        {
            if(this.submission == null) {
                throw new Exception("This method can only be called if the Submission property is already defined");
            }

            string sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);

            this.lineDetail = new List<LineDetail>();
            int startRow = 5;
            int startCheckbox = 14;

            for (int i = 0; i < 10; i++) {                
                var line = new LineDetail();

                if(i == 0) {
                    this.submission.CardType = GetCellValue<string>(sheetName, $"BS{startRow}");
                    this.submission.CardOwnerName = GetCellValue<string>(sheetName, $"BT{startRow}");
                    this.submission.BankIssuer = GetCellValue<string>(sheetName, $"BU{startRow}");
                    this.crmGroup.PaymentMode = GetPaymentMode(GetCellValue<string>(sheetName, $"BD{startRow}"));
                }

                string msisdn = GetCellValue<string>(sheetName, $"C{startRow}");
                if(string.IsNullOrEmpty(msisdn)) {
                    startRow++;
                    startCheckbox++;
                    continue;
                }


                line.No = GetCellValue<int>(sheetName, $"A{startRow}");
                line.Username = GetCellValue<string>(sheetName, $"B{startRow}");
                line.MSISDN = msisdn;
                line.MandatoryOffer1 = GetCellValue<string>(sheetName, $"E{startRow}");
                line.MandatoryOffer2 = GetCellValue<string>(sheetName, $"F{startRow}");
                line.MandatoryOffer3 = GetCellValue<string>(sheetName, $"G{startRow}");
            

                string autoBillingString = GetCellValue<string>(sheetName, $"BC{startRow}");
                line.AutoBilling = string.IsNullOrEmpty(autoBillingString) ? false : autoBillingString == "Yes" ? true : false;

                line.SimCardSerialNo = GetCellValue<string>(sheetName, $"BE{startRow}");
                line.PhoneModel = GetCellValue<string>(sheetName, $"BF{startRow}");
                line.DeviceBundleType = GetCellValue<string>(sheetName, $"BU{startRow}");
                line.P2P = GetCellValue<string>(sheetName, $"BL{startRow}");
                line.BillMedium = GetCellValue<string>(sheetName, $"BN{startRow}");

                string conceptPaperString = GetCellValue<string>(sheetName, $"BO{startRow}");
                line.ConceptPaper = string.IsNullOrEmpty(conceptPaperString) ? false : conceptPaperString == "Yes" ? true : false;

                line.PrMode = GetCellValue<string>(sheetName, $"BA{startRow}");
                line.CreditLimit = GetCellValue<decimal>(sheetName, $"BB{startRow}");
                line.Imsi = GetCellValue<string>(sheetName, $"BG{startRow}");
                line.ImsiPackageCode = GetCellValue<string>(sheetName, $"BH{startRow}");
                line.BufferSim = GetCellValue<string>(sheetName, $"BI{startRow}");
                line.CustomerLevel = GetCellValue<string>(sheetName, $"BK{startRow}");
                
                line.Bundle1 = GetCellValue<string>(sheetName, $"J{startRow}");
                line.Element1 = GetCellValue<string>(sheetName, $"K{startRow}");
                line.Bundle2 = GetCellValue<string>(sheetName, $"L{startRow}");
                line.Element2 = GetCellValue<string>(sheetName, $"M{startRow}");
                line.Bundle3 = GetCellValue<string>(sheetName, $"N{startRow}");

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